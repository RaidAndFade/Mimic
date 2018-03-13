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
using System.Numerics;
using Mimic.Common;

namespace Mimic.RealmServer
{
    internal class AuthHandler : ISocketHandler, IDisposable
    {
        private const uint GameName = 0x00_57_6f_57; // 'WoW'
        private const string TestPassword = "RAID"; // Password is uppercase

        private bool _run = true;
        private TcpClient _client;
        private Stream _clientStream;
        private AsyncBinaryReader _reader;
        private SrpHandler _authentication = new SrpHandler();

        private ushort _buildNumber;

        private AuthCommand _currentCommand;

        private string accountName;

        public async Task RunAsync(TcpClient client)
        {
            _client = client;
            _client.ReceiveTimeout = 1000;

            _clientStream = _client.GetStream();
            _reader = new AsyncBinaryReader(_clientStream);

            while (_run && _clientStream.CanRead)
            {
                var cmd = (AuthCommand)await _reader.ReadUInt8Async();
                _currentCommand = cmd;
                Debug.WriteLine("OPCODE "+cmd+" received");
                switch (cmd)
                {
                    case AuthCommand.LogonChallenge:
                        await HandleLogonChallengeAsync();
                        break;
                    case AuthCommand.LogonProof:
                        await HandleLogonProofAsync();
                        break;
                    case AuthCommand.RealmList:
                        await HandleRealmListAsync();
                        break;
                    case AuthCommand.ReconnectChallenge:
                        await HandleReconnectChallengeAsync();
                        break;
                    case AuthCommand.ReconnectProof:
                        await HandleReconnectProofAsync();
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

        private async Task HandleLogonChallengeAsync()
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
            accountName = await _reader.ReadStringAsync(accountNameLength);
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


        private async Task HandleReconnectChallengeAsync()
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
            accountName = await _reader.ReadStringAsync(accountNameLength);
            accountName = accountName.ToUpperInvariant();
            
            List<byte> data = new List<byte>();

            data.Add((byte)_currentCommand);

            data.Add((byte)AuthStatus.Success);

            data.AddRange(Enumerable.Repeat((byte)0xfe, 16));

            data.AddRange(Enumerable.Repeat((byte)0, 16));

            data.Add(0); // security flags;

            await _clientStream.WriteAsync(data.ToArray(), 0, data.Count);
        }

        public async Task HandleLogonProofAsync()
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

            //print sessionkey
            Debug.WriteLine(BitConverter.ToString(SrpHandler.BigIntToByteArray(_authentication._K)).Replace("-", ""));

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

        public async Task HandleReconnectProofAsync()
        {
            var R1 = await _reader.ReadBytesAsync(16);
            var R2 = await _reader.ReadBytesAsync(20);
            var R3 = await _reader.ReadBytesAsync(20);
            var keyCount = await _reader.ReadUInt8Async();
            byte[] proof = Enumerable.Repeat((byte)0xfe, 16).ToArray();

            Debug.WriteLine("RPROOF FROM "+accountName);
            try{
            //_authentication.Authenticate(R1, R2);
            //_authentication.ComputeProof();
            }catch(Exception e){
                Debug.WriteLine(e);
            }

            SHA1 sh = SHA1.Create();
            sh.Initialize();
            sh.TransformBlock(Encoding.Default.GetBytes(accountName),0,accountName.Length,Encoding.Default.GetBytes(accountName),0);
            sh.TransformBlock(R1,0,R1.Length,R1,0);
            sh.TransformBlock(proof,0,proof.Length,proof,0);
            Debug.WriteLine(_authentication._K.ToByteArray());
            byte[] sessKey = SrpHandler.BigIntToByteArray(_authentication._K,40);
            sh.TransformBlock(sessKey,0,sessKey.Length,sessKey,0);
            byte[] zer = new byte[0];
            sh.TransformFinalBlock(zer,0,0);
            byte[] hash = sh.Hash;

            Debug.WriteLine(BitConverter.ToString(hash).Replace("-", ""));
            Debug.WriteLine(BitConverter.ToString(R2).Replace("-", ""));

            List<byte> pktdata = new List<byte>();
            pktdata.Add((byte)AuthCommand.ReconnectProof);
            pktdata.Add((byte)0);
            pktdata.AddRange(BitConverter.GetBytes((ushort)0));

            await _clientStream.WriteAsync(pktdata.ToArray(), 0, pktdata.Count);
        }

        public async Task HandleRealmListAsync()
        {
            // 4 empty bytes?
            uint unknown = await _reader.ReadUInt32Async();

            ushort realmCount = 10;

            List<byte> realms = new List<byte>();
            realms.AddRange(BitConverter.GetBytes(0)); // unused/unknown
            realms.AddRange(BitConverter.GetBytes(1)); // number of realms

            realms.Add(0x02); // realm type
            realms.Add(0x00); // lock (0x00 == unlocked)
            realms.Add(0x40); // realm flags (0x40 == recommended)
            realms.AddRange(Encoding.UTF8.GetBytes("TestRealm")); // name
            realms.Add(0); // null-terminator
            realms.AddRange(Encoding.UTF8.GetBytes("99.228.169.83:8085")); // address
            realms.Add(0); // null-terminator
            realms.AddRange(BitConverter.GetBytes(0.5f)); // population level
            realms.Add(0x00); // number of characters
            realms.Add(0x01); // timezone

            realms.Add(0x2C); // unknown

            realms.AddRange(BitConverter.GetBytes((ushort)0x0010)); // unused/unknown

            List<byte> data = new List<byte>();
            data.Add((byte)AuthCommand.RealmList);
            data.AddRange(BitConverter.GetBytes((ushort)realms.Count));
            data.AddRange(realms);

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
