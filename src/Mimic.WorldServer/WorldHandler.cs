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
    internal class WorldHandler : ISocketHandler, IDisposable
    {
        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;
        private BinaryWriter _writer;
        private uint _mseed;

        private AuthSession _authsession;
        private WorldCommand _currentCommand;

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
                var lb1 = (await _reader.ReadBytesAsync(1))[0];
                var lb2 = (await _reader.ReadBytesAsync(1))[0];
                var len = (lb1 << 8) + lb2;
                var cmd = (WorldCommand)await _reader.ReadUInt32Async();
                len -= 6;
                _currentCommand = cmd;
                Debug.WriteLine("INCOMING COM:" + cmd + " SIZE:" + len);
                switch (cmd)
                {
                    case WorldCommand.CMSG_AUTH_SESSION:
                        await HandleAuthSession(len);
                        break;

                    default:
                        break;
                }
            }
        }

        private async Task<string> readStringAsync()
        {
            String s = "";
            byte cb = 0;
            while(_clientStream.CanRead)
            {
                cb = (byte)await _reader.ReadInt8Async();
                if (cb == 0) break;
                s += (char)cb;
            }
            return s;
        }

        private void SendAuthChallenge()
        {
            WorldPacket wp = new WorldPacket(WorldCommand.SMSG_AUTH_CHALLENGE);
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

        private async Task HandleAuthSession(int len)
        {
            _authsession = new AuthSession();
            _authsession.build = await _reader.ReadUInt32Async();
            _authsession.loginServerId = await _reader.ReadUInt32Async();
            _authsession.account = await readStringAsync();
            _authsession.loginServerType = await _reader.ReadUInt32Async();
            _authsession.localChallenge = await _reader.ReadUInt32Async();
            _authsession.regionId = await _reader.ReadUInt32Async();
            _authsession.battlegroupId = await _reader.ReadUInt32Async();
            _authsession.realmId = await _reader.ReadUInt32Async();
            _authsession.dosResponse = await _reader.ReadUInt64Async();
            _authsession.digest = await _reader.ReadBytesAsync(20);
            len -= 4 + 4 + 4 + 4 + 4 + 4 + 4 + 8 + 20 + _authsession.account.Length;
            Debug.WriteLine(BitConverter.ToString(_authsession.digest).Replace("-", ""));
            Debug.WriteLine(len);
            try
            {
                _authsession.addonInfo = await _reader.ReadBytesAsync(len);
            }catch(Exception e)
            {
                Debug.WriteLine(e);
            }
            //_authsession.unk0 = await _reader.ReadBytesAsync(len);


            Debug.WriteLine("Client <"+_authsession.account+"> authed on build "+_authsession.build+"(0x1ED)");
            // Debug.WriteLine(_authsession);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _clientStream?.Dispose();
            _client?.Dispose();
        }

        private async Task HandleLogonChallengeAsync()
        {
          /*  var error = await _reader.ReadUInt8Async(); // always 3
            var size = await _reader.ReadUInt16Async();

            if (_client.Available < size)
            {
                await CloseAsync(AuthStatus.ProtocolError);
                return;
            }

            var gameName = await _reader.ReadUInt32Async();

            if (gameName != GameName)
            {
                await CloseAsync(AuthStatus.ProtocolError);
                return;
            }

            var versionMajor = await _reader.ReadUInt8Async();
            var versionMinor = await _reader.ReadUInt8Async();
            var versionPatch = await _reader.ReadUInt8Async();

            _buildNumber = await _reader.ReadUInt16Async();

            var platform = (Architecture)await _reader.ReadUInt32Async();
            var os = (OperatingSystem)await _reader.ReadUInt32Async();
            var locale = (Locale)await _reader.ReadUInt32Async();

            var timezoneBias = await _reader.ReadUInt32Async();

            var ipAddress = new IPAddress(await _reader.ReadUInt32Async());
            var realAddress = (_client.Client.RemoteEndPoint as IPEndPoint).Address;

            var accountNameLength = await _reader.ReadUInt8Async();
            var accountName = await _reader.ReadStringAsync(accountNameLength);
            accountName = accountName.ToUpperInvariant();

            List<byte> data = new List<byte>();

            data.Add((byte)_currentCommand);
            data.Add(0);

            data.Add((byte)AuthStatus.Success);

            data.AddRange(_authentication.PublicKey); // B

            data.Add((byte)_authentication.Generator.Length);
            data.AddRange(_authentication.Generator); // g

            data.Add((byte)_authentication.SafePrime.Length);
            data.AddRange(_authentication.SafePrime); // N

            data.AddRange(_authentication.Salt); // s

            data.AddRange(Enumerable.Repeat((byte)0, 16));

            data.Add(0); // security flags;

            await _clientStream.WriteAsync(data.ToArray(), 0, data.Count);*/
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
