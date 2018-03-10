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

namespace Mimic.RealmServer
{
    internal class AuthHandler : ISocketHandler, IDisposable
    {
        private const uint GameName = 0x00_57_6f_57; // 'WoW'
        private const string TestPassword = "Password";

        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;
        private SrpHandler _authentication = new SrpHandler();

        private ushort _buildNumber;

        private AuthCommand _currentCommand;

        public async Task RunAsync(TcpClient client)
        {
            _client = client;
            _client.ReceiveTimeout = 1000;

            _clientStream = _client.GetStream();
            _reader = new AsyncBinaryReader(_clientStream);

            while (_run)
            {
                var cmd = (AuthCommand)await _reader.ReadUInt8Async();
                _currentCommand = cmd;
                switch (cmd)
                {
                    case AuthCommand.LogonChallenge:
                        await HandleLogonChallenge();
                        break;
                    case AuthCommand.LogonProof:
                        await HandleLogonProof();
                        break;
                    default:
                        Debug.WriteLine($"Unhandled opcode {cmd} (0x{cmd:X})");
                        await CloseAsync(AuthStatus.Unimplemented);
                        break;
                }
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _clientStream?.Dispose();
            _client?.Dispose();
        }

        private async Task HandleLogonChallenge()
        {
            var error = await _reader.ReadUInt8Async(); // always 3
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

            using (var sha = SHA1.Create())
            {
                var pw = Encoding.UTF8.GetBytes(
                    $"{accountName}:{TestPassword}");
                var hash = sha.ComputeHash(pw);

                _authentication.ComputePrivateFields(accountName, hash);
            }

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

            await _clientStream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        public async Task HandleLogonProof()
        {
            var clientPublicKey = await _reader.ReadBytesAsync(32);
            var clientProof = await _reader.ReadBytesAsync(20);
            var crc = await _reader.ReadBytesAsync(20);
            var keyCount = await _reader.ReadUInt8Async();
            var securityFlags = await _reader.ReadUInt8Async();

            if (!_authentication.Authenticate(clientPublicKey, clientProof))
            {
                await SendErrorAsync(AuthStatus.IncorrectPassword);
                return;
            }

            var proof = _authentication.ComputeProof();

            // TODO: check build number and send back appropriate packet
            // (assuming WotLK right now, 3.3.5a, build 12340)

            List<byte> data = new List<byte>();
            data.Add((byte)_currentCommand); // cmd
            data.Add(0); // error
            data.AddRange(proof); // server proof
            data.AddRange(Enumerable.Repeat((byte)0, 4)); //accountFlags
            data.AddRange(Enumerable.Repeat((byte)0, 4)); //surveyId
            data.AddRange(Enumerable.Repeat((byte)0, 2)); //unkFlags

            await _clientStream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        private async Task SendErrorAsync(AuthStatus errorCode)
        {
            // TODO: make sure this is a valid packet
            // The client disconnects when we send this, which is good but not
            // what we want.
            byte[] data = {
                (byte)_currentCommand,
                (byte)errorCode
            };

            await _clientStream.WriteAsync(data, 0, 2);
        }

        private async Task CloseAsync(AuthStatus errorCode)
        {
            await SendErrorAsync(errorCode);
            await Task.Delay(300); // Give the packet some time to be sent
            _client.Close();
            _run = false;
        }
    }
}
