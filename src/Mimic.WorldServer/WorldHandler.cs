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
        private uint _mseed;
        public AuthCrypt _ac = new AuthCrypt();

        private byte[] sessionKey = StringToByteArray("210567DD31D09585168D33C25CB6171ACED2C978CB4A0C17C85DCADBD17DAD900468C9AF2A32AD87");

        private AuthSession _authsession;
        private WorldCommand _currentCommand;

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public async Task RunAsync(TcpClient client)
        {
            _client = client;
            var ipaddr = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            _client.ReceiveTimeout = 1000;

            var seedArr = new Byte[4];
            new Random().NextBytes(seedArr);
            _mseed = BitConverter.ToUInt32(seedArr,0);

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
                Debug.WriteLine(lb1 + "|" + lb2);
                len -= 2;
                var wp = new WorldPacket(await _reader.ReadBytesAsync(len), this);
                var cmd = (WorldCommand)wp.ReadInt32();
                _currentCommand = cmd;
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
            Debug.WriteLine(BitConverter.ToString(wp.result()).Replace("-", ""));
        }

        private void SendAuthChallenge()
        {
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_AUTH_CHALLENGE,this);
            wp.append((UInt32)1);
            wp.append((UInt32)_mseed);
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
            _authsession.localChallenge = wp.ReadUInt32();
            _authsession.regionId = wp.ReadUInt32();
            _authsession.battlegroupId = wp.ReadUInt32();
            _authsession.realmId = wp.ReadUInt32();
            _authsession.dosResponse = wp.ReadUInt64();
            _authsession.digest = wp.ReadBytes(20);
            _authsession.addonInfo = wp.ReadBytes(wp.Length-wp._rpos);
            //_authsession.unk0 = await _reader.ReadBytesAsync(len);

            _ac = new AuthCrypt(sessionKey);

            Debug.WriteLine("Client <"+_authsession.account+"> authed on build "+_authsession.build+"(0x1ED)");
            // Debug.WriteLine(_authsession);

            WorldPacket pck = new WorldPacket(WorldCommand.SMSG_AUTH_RESPONSE, this);

            SHA1 s = SHA1.Create();
            List<byte> i = new List<byte>();
            i.AddRange(Encoding.ASCII.GetBytes(_authsession.account));
            i.AddRange(BitConverter.GetBytes((UInt32)0));
            i.AddRange(BitConverter.GetBytes(_authsession.localChallenge));
            i.AddRange(BitConverter.GetBytes(_mseed));
            i.AddRange(sessionKey);
            byte[] d = s.ComputeHash(i.ToArray());


            if (d != _authsession.digest) //authed
            {
                Debug.WriteLine(BitConverter.ToString(d).Replace("-", ""));
                Debug.WriteLine(BitConverter.ToString(_authsession.digest).Replace("-", ""));
                Debug.WriteLine("Didn't auth properly");
                pck.append((byte)14);
            }
            else
            {
                byte[] res = { 0x0C, 0x30, 0x78, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
                pck.append(res);
            }

            _writer.Write(pck.result());
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
            public UInt32 loginServerType, localChallenge, regionId, battlegroupId, realmId;
            public UInt64 dosResponse;
            public byte[] digest, addonInfo;
        }
    }
}
