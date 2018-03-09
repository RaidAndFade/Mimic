using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Mimic.Common
{
    public class AsyncBinaryReader : IDisposable
    {
        private readonly Stream _underlyingStream;
        private readonly Encoding _textEncoding;
        private readonly bool _closeOnDispose;

        public AsyncBinaryReader(Stream underlying)
            : this(underlying, Encoding.UTF8, true)
        { }

        public AsyncBinaryReader(Stream underlying,
            Encoding encoding)
            : this(underlying, encoding, true)
        { }

        public AsyncBinaryReader(Stream underlying,
            Encoding encoding,
            bool closeOnDispose)
        {
            _underlyingStream = underlying;
            _textEncoding = encoding;
            _closeOnDispose = closeOnDispose;
        }

        public async Task<byte[]> ReadBytesAsync(int count)
        {
            var buffer = new byte[count];

            var bytesRead = await _underlyingStream.ReadAsync(buffer, 0, count)
                .ConfigureAwait(false);

            if (bytesRead != count)
                throw new IOException(
                    "Not all requested bytes were available");

            return buffer;
        }

        public async Task<sbyte> ReadInt8Async()
        {
            var buffer = await ReadBytesAsync(1)
                .ConfigureAwait(false);

            return (sbyte)buffer[0];
        }

        public async Task<byte> ReadUInt8Async()
        {
            var buffer = await ReadBytesAsync(1)
                .ConfigureAwait(false);

            return buffer[0];
        }

        public async Task<short> ReadInt16Async()
        {
            var buffer = await ReadBytesAsync(sizeof(short))
                .ConfigureAwait(false);

            return BitConverter.ToInt16(buffer, 0);
        }

        public async Task<ushort> ReadUInt16Async()
        {
            var buffer = await ReadBytesAsync(sizeof(ushort))
                .ConfigureAwait(false);

            return BitConverter.ToUInt16(buffer, 0);
        }

        public async Task<int> ReadInt32Async()
        {
            var buffer = await ReadBytesAsync(sizeof(int))
                .ConfigureAwait(false);

            return BitConverter.ToInt32(buffer, 0);
        }

        public async Task<uint> ReadUInt32Async()
        {
            var buffer = await ReadBytesAsync(sizeof(uint))
                .ConfigureAwait(false);

            return BitConverter.ToUInt32(buffer, 0);
        }

        public async Task<long> ReadInt64Async()
        {
            var buffer = await ReadBytesAsync(sizeof(long))
                .ConfigureAwait(false);

            return BitConverter.ToInt64(buffer, 0);
        }

        public async Task<ulong> ReadUInt64Async()
        {
            var buffer = await ReadBytesAsync(sizeof(ulong))
                .ConfigureAwait(false);

            return BitConverter.ToUInt64(buffer, 0);
        }

        public async Task<string> ReadStringAsync(int bytes)
        {
            var buffer = await ReadBytesAsync(bytes)
                .ConfigureAwait(false);

            return _textEncoding.GetString(buffer, 0, bytes);
        }

        public void Dispose()
        {
            if (_closeOnDispose)
                _underlyingStream.Close();
        }
    }
}
