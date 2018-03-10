using System;
using System.Numerics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Mimic.Common;
using System.Linq;

namespace Mimic.RealmServer
{
    public class SrpHandler : IDisposable
    {
        private const int PasswordHashLength = 20;

        private static readonly BigInteger _N // safe prime
            = BigIntFromHexString(
"894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7");
        private static readonly BigInteger _g // generator
            = new BigInteger(7);

        private readonly RandomNumberGenerator _random;
        private readonly SHA1 _sha;

        private string _I; // identifying username
        private BigInteger _s; // salt
        private BigInteger _v; // password verifier
        private BigInteger _b; // server private key
        private BigInteger _B; // server public key
        private BigInteger _A; // client public key
        private BigInteger _K; // strong session key
        private BigInteger _M1; // client proof

        public byte[] PublicKey
            => BigIntToByteArray(_B);
        public byte[] Generator
            => BigIntToByteArray(_g);
        public byte[] SafePrime
            => BigIntToByteArray(_N);
        public byte[] Salt
            => BigIntToByteArray(_s);

        public SrpHandler()
        {
            _random = RNGCryptoServiceProvider.Create();
            _sha = SHA1.Create();
        }

        public void Dispose()
        {
            _random?.Dispose();
            _sha?.Dispose();
        }


        public void ComputePrivateFields(string username, byte[] passwordHash)
        {
            if (passwordHash.Length < PasswordHashLength)
                throw new ArgumentException(
                    $"Password hash must be {PasswordHashLength} bytes",
                    nameof(passwordHash));

            _I = username;
            _s = BigIntFromRandom(32);

            var x = BigIntFromByteArray(HashArrays(
                BigIntToByteArray(_s),
                passwordHash
            ));

            _v = BigInteger.ModPow(_g, x, _N);
            _b = BigIntFromRandom(19);
            _B = ((_v * 3) + BigInteger.ModPow(_g, _b, _N)) % _N;
        }

        public bool Authenticate(
            byte[] clientPublicKey,
            byte[] clientProof)
        {
            _A = BigIntFromByteArray(clientPublicKey);
            _M1 = BigIntFromByteArray(clientProof);

            if (_A.IsZero || (_A % _N).IsZero)
                return false;

            var u = BigIntFromByteArray(HashArrays(
                BigIntToByteArray(_A),
                BigIntToByteArray(_B)
            ));
            var S = BigInteger.ModPow(
                _A * BigInteger.ModPow(_v, u, _N),
                _b,
                _N);

            _K = ComputeSessionKey(S);

            var nHash = HashArrays(BigIntToByteArray(_N));
            var gHash = HashArrays(BigIntToByteArray(_g));

            var ngHashXor = nHash.Zip(gHash, (x, y) => (byte)(x ^ y))
                .ToArray();
            var iHash = HashArrays(Encoding.UTF8.GetBytes(_I));

            var clientProofCheck = HashArrays(
                ngHashXor, iHash,
                BigIntToByteArray(_s),
                BigIntToByteArray(_A),
                BigIntToByteArray(_B),
                BigIntToByteArray(_K)
            );

            var diff = clientProof.Length ^ clientProofCheck.Length;
            diff |= clientProof
                .Zip(clientProofCheck, (x, y) => x ^ y)
                .Aggregate((x, y) => x | y);

            return diff == 0;
        }

        public byte[] ComputeProof()
            => HashArrays(
                BigIntToByteArray(_A),
                BigIntToByteArray(_M1),
                BigIntToByteArray(_K)
            );

        private BigInteger ComputeSessionKey(BigInteger S)
        {
            byte[] data = BigIntToByteArray(S, 32);
            byte[] hashInput = new byte[16];
            byte[] sessionResult = new byte[40];
            byte[] hash;

            for (int i = 0; i < 16; i++)
                hashInput[i] = data[i * 2];

            hash = HashArrays(hashInput);
            for (int i = 0; i < 20; i++)
                sessionResult[i * 2] = hash[i];

            for (int i = 0; i < 16; i++)
                hashInput[i] = data[i * 2 + 1];

            hash = HashArrays(hashInput);
            for (int i = 0; i < 20; i++)
                sessionResult[i * 2 + 1] = hash[i];

            return BigIntFromByteArray(sessionResult);
        }

        private byte[] HashArrays(params byte[][] arrays)
        {
            byte[] totalBytes = new byte[arrays.Sum(x => x.Length)];

            int offset = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                Buffer.BlockCopy(array, 0, totalBytes, offset, array.Length);
                offset += array.Length;
            }

            return _sha.ComputeHash(totalBytes);
        }

        private BigInteger BigIntFromRandom(int bytes)
        {
            byte[] data = new byte[bytes + 1];
            _random.GetBytes(data, 0, bytes);

            return new BigInteger(data);
        }

        private static BigInteger BigIntFromHexString(string hex)
            => BigInteger.Parse($"00{hex}", NumberStyles.HexNumber);

        private static BigInteger BigIntFromByteArray(byte[] bytes)
        {
            Array.Resize(ref bytes, bytes.Length + 1); // force MSB = 0
            return new BigInteger(bytes);
        }

        private static byte[] BigIntToByteArray(BigInteger value,
            int minNumBytes = 0)
        {
            var result = value.ToByteArray();

            if (result.Length > minNumBytes)
            {
                if (result[result.Length - 1] == 0) // remove forced MSB = 0
                    Array.Resize(ref result, result.Length - 1);
            }
            else
            {
                Array.Resize(ref result, minNumBytes);
            }

            return result;
        }
    }
}
