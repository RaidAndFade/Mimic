using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Mimic.Common;

namespace Mimic.RealmServer
{
    internal class AuthHandler : ISocketHandler, IDisposable
    {
        private const uint GameName = 0x00_57_6f_57; // 'WoW'
        private const string TestPassword = "TEST:Password";

        private static readonly BigInteger N;
        private static readonly BigInteger g;

        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;
        private SrpHandler _authentication;

        private AuthCommand _currentCommand;

        static AuthHandler()
        {
            // TODO: pass these from a config file
            N = BigInteger.Parse(
"894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7",
                NumberStyles.HexNumber);
            g = new BigInteger(7);
        }

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
                        await HandleLogonChallenge(_reader);
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

        private async Task HandleLogonChallenge(AsyncBinaryReader reader)
        {
            var error = await reader.ReadUInt8Async(); // always 3
            var size = await reader.ReadUInt16Async();

            if (_client.Available < size)
            {
                await CloseAsync(AuthStatus.ProtocolError);
                return;
            }

            var gameName = await reader.ReadUInt32Async();

            if (gameName != GameName)
            {
                await CloseAsync(AuthStatus.ProtocolError);
                return;
            }

            var versionMajor = await reader.ReadUInt8Async();
            var versionMinor = await reader.ReadUInt8Async();
            var versionPatch = await reader.ReadUInt8Async();

            var buildNumber = await reader.ReadUInt16Async();

            var platform = (Architecture)await reader.ReadUInt32Async();
            var os = (OperatingSystem)await reader.ReadUInt32Async();
            var locale = (Locale)await reader.ReadUInt32Async();

            var timezoneBias = await reader.ReadUInt32Async();

            var ipAddress = new IPAddress(await reader.ReadUInt32Async());
            var realAddress = (_client.Client.RemoteEndPoint as IPEndPoint).Address;

            var accountNameLength = await reader.ReadUInt8Async();
            var accountName = await reader.ReadStringAsync(accountNameLength);

            using (var sha = SHA1.Create())
            {
                var pw = Encoding.UTF8.GetBytes(TestPassword);
                var hash = sha.ComputeHash(pw);

                _authentication = new SrpHandler(
                    accountName.ToUpperInvariant(),
                    hash);
            }

            var unk3 = _authentication.GenerateRandomNumber(16);

            // TODO: AsyncBinaryWriter?
            List<byte> data = new List<byte>();

            data.Add((byte)_currentCommand);
            data.Add(0);
            data.Add((byte)AuthStatus.Success);

            // B
            var publicKey = _authentication.PublicKey.ToByteArray();
            if (publicKey.Length < 32)
                Array.Resize(ref publicKey, 32);

            data.AddRange(publicKey);

            // g
            var generator = _authentication.Generator.ToByteArray();
            data.Add((byte)generator.Length);
            data.AddRange(generator);

            // N
            var safePrime = _authentication.SafePrime.ToByteArray();
            data.Add((byte)safePrime.Length);
            data.AddRange(safePrime);

            // s
            var salt = _authentication.Salt.ToByteArray();
            data.AddRange(salt);

            // ???
            data.AddRange(unk3.ToByteArray());

            data.Add(0);

            data[1] = (byte)data.Count; // packet length
            await _clientStream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        private async Task SendErrorAsync(AuthStatus errorCode)
        {
            // TODO: make sure this is a valid packet
            // The client disconnects when we send this, which is good but not
            // what we want.
            byte[] data = {
                (byte)_currentCommand,
                0x0,
                0x0,
                (byte)errorCode
            };

            await _clientStream.WriteAsync(data, 0, 4);
        }

        private async Task CloseAsync(AuthStatus errorCode)
        {
            await SendErrorAsync(errorCode);
            await Task.Delay(300); // Give the packet some time to be sent
            _client.Close();
            _run = false;
        }

        private byte[] GetRandomBytes(int count)
        {
            var result = new byte[count];

            using (var random = RNGCryptoServiceProvider.Create())
            {
                random.GetBytes(result);
            }

            return result;
        }
    }
}
