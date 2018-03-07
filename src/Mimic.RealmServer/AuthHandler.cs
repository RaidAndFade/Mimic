using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Mimic.Common;

namespace Mimic.RealmServer
{
    internal class AuthHandler : ISocketHandler, IDisposable
    {
        private const uint GameName = 0x00_57_6f_57; // 'WoW'

        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;

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
                        await HandleLogonChallenge(_reader);
                        break;
                    default:
                        Debug.WriteLine($"Unhandled opcode {cmd} (0x{cmd:X})");
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
                await CloseAsync(AuthError.ProtocolError);
                return;
            }

            var gameName = await reader.ReadUInt32Async();

            if (gameName != GameName)
            {
                await CloseAsync(AuthError.ProtocolError);
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

            Debug.WriteLine("Read packet");
        }

        private async Task SendErrorAsync(AuthError errorCode)
        {
            byte[] data = {
                (byte)_currentCommand,
                0x0,
                (byte)errorCode
            };

            await _clientStream.WriteAsync(data, 0, 3);
        }

        private async Task CloseAsync(AuthError errorCode)
        {
            await SendErrorAsync(errorCode);
            await Task.Delay(300); // Give the packet some time to be sent
            _client.Close();
            _run = false;
        }
    }
}