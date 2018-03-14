using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Mimic.Common;

namespace Mimic.WorldServer
{
    public class WorldHandler : ISocketHandler, IDisposable
    {
        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;
        private BinaryWriter _writer;
        private byte[] _mseed;
        public AuthCrypt _ac = new AuthCrypt();

        private AccountInfo _info;

        private AuthSession _authsession;


        public async Task RunAsync(TcpClient client)
        {
            _client = client;
            var ipaddr = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            _client.ReceiveTimeout = 1000;

            var seedArr = new byte[4];
            new Random().NextBytes(seedArr);
            _mseed = seedArr;

            _clientStream = _client.GetStream();
            _reader = new AsyncBinaryReader(_clientStream);
            _writer = new BinaryWriter(_clientStream);

            Debug.WriteLine("Connection from " + ipaddr);

            SendAuthChallenge();

            while (_run)
            {
                var lb1 = _ac.decrypt((await _reader.ReadBytesAsync(1)))[0];
                var lb2 = _ac.decrypt((await _reader.ReadBytesAsync(1)))[0];
                var len = (lb1 << 8) + lb2;
                Debug.WriteLine(lb1 + "|" + lb2+" ->| "+len);
                var wp = new WorldPacket(await _reader.ReadBytesAsync(len), this);
                var cmd = (WorldCommand)wp.ReadInt32();
                Debug.WriteLine("INCOMING COM:" + cmd + " SIZE:" + len);
                switch (cmd)
                {
                    case WorldCommand.CMSG_AUTH_SESSION:
                        await HandleAuthSession(wp);
                        break;
                    case WorldCommand.CMSG_PING:
                        await HandlePing(wp);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandlePing(WorldPacket wp)
        {
            uint ping = wp.ReadUInt32();
            uint latency = wp.ReadUInt32();
            WorldPacket pck = new WorldPacket(WorldCommand.SMSG_PONG,this);
            pck.append(ping);
            _writer.Write(pck.result());
        }

        private void SendAuthChallenge()
        {
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_AUTH_CHALLENGE,this);
            wp.append((UInt32)1);
            wp.append(_mseed);
            wp.append((UInt32)0xffeeddcc);
            wp.append((UInt32)0xdeadbeef);
            wp.append((UInt32)0x00babe00);
            wp.append((UInt32)0xaabbccdd);

            Debug.WriteLine("Client connected, sending authchal (0x1EC)");
            var res = wp.result();
            Debug.WriteLine(BitConverter.ToString(res).Replace("-", ""));

            _writer.Write(res);
        }

        private async Task HandleAuthSession(WorldPacket wp)
        {
            _authsession = new AuthSession();
            _authsession.build = wp.ReadUInt32();
            _authsession.loginServerId = wp.ReadUInt32();
            _authsession.account = wp.ReadString();
            _authsession.loginServerType = wp.ReadUInt32();
            _authsession.localChallenge = wp.ReadBytes(4, true);
            _authsession.regionId = wp.ReadUInt32();
            _authsession.battlegroupId = wp.ReadUInt32();
            _authsession.realmId = wp.ReadUInt32();
            _authsession.dosResponse = wp.ReadUInt64();
            _authsession.digest = wp.ReadBytes(20);
            _authsession.addonInfo = wp.ReadBytes(wp.Length-wp._rpos);
            //_authsession.unk0 = await _reader.ReadBytesAsync(len);

            _info = await Program.authDatabase.AsyncFetchAccountByName(_authsession.account);
            var sessionKey = MimicUtils.HexStringToByteArray(_info.sessionkey,40);
            Debug.WriteLine(_info.sessionkey);

            _ac = new AuthCrypt(sessionKey);
            // Debug.WriteLine(_authsession);


            SHA1 sh = SHA1.Create();
            sh.Initialize();
            byte[] username = Encoding.UTF8.GetBytes(_authsession.account);
            sh.TransformBlock(username,0, username.Length,username,0);
            byte[] pad = new byte[4];
            sh.TransformBlock(pad,0, pad.Length,pad,0);
            byte[] localChal = _authsession.localChallenge;
            sh.TransformBlock(localChal,0,localChal.Length,localChal,0);
            sh.TransformBlock(_mseed,0,_mseed.Length,_mseed,0);
            sh.TransformBlock(sessionKey,0,sessionKey.Length,sessionKey,0);
            byte[] zer = new byte[0];
            sh.TransformFinalBlock(zer,0,0);
            byte[] d = sh.Hash;

            WorldPacket pck = new WorldPacket(WorldCommand.SMSG_AUTH_RESPONSE, this);
            if (!d.SequenceEqual(_authsession.digest)) //Didn't auth properly
            {
                Debug.WriteLine(BitConverter.ToString(d).Replace("-", ""));
                Debug.WriteLine(BitConverter.ToString(_authsession.digest).Replace("-", ""));
                Debug.WriteLine("Didn't auth properly");
                pck.append((byte)14);
            }
            else
            {
                Debug.WriteLine("Client <"+_authsession.account+"> authed on build "+_authsession.build+" (0x1ED)");
                pck.append((byte)12);
            }

            byte[] pdata = pck.result();
            _writer.Write(pdata);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _clientStream?.Dispose();
            _client?.Dispose();
        }

        private class AuthSession
        {
            public UInt32 build, loginServerId;
            public String account;
            public byte[] localChallenge;
            public UInt32 loginServerType, regionId, battlegroupId, realmId;
            public UInt64 dosResponse;
            public byte[] digest, addonInfo;
        }
    }
}
