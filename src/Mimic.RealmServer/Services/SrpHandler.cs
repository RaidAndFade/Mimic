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

        private static readonly BigNumber _N
            = BigNumber.FromHexString(
"894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7");

        private static readonly BigNumber _g
            = new BigNumber(7);

        private readonly RandomNumberGenerator _random;
        private readonly BigNumber _v;
        private readonly BigNumber _s;
        private readonly BigNumber _b;
        private readonly BigNumber _B;
        private BigNumber _A;
        private BigNumber _K;
        private BigNumber _Mc;
        private readonly string _I;

        public BigNumber ServerPublicKey
            => _B;
        public BigNumber Generator
            => _g;
        public BigNumber SafePrime
            => _N;
        public BigNumber Salt
            => _s;
        public BigNumber ClientPublicKey
            => _A;
        public BigNumber SessionKey
            => _K;

        public SrpHandler(
            string username,
            byte[] passwordHash)
        {
            if (passwordHash.Length != PasswordHashLength)
                throw new ArgumentException(
                    $"Password hash must be 20 bytes long",
                    nameof(passwordHash));

            _random = RNGCryptoServiceProvider.Create();
            _I = username;

            var x = new BigNumber(passwordHash);

            _v = BigNumber.ModPow(_g, x, _N);
            _s = GenerateRandomNumber(32);
            _b = GenerateRandomNumber(19);

            var genMod = BigNumber.ModPow(_g, _b,
                _N);
            _B = ((_v * 3) + genMod) % _N;
        }

        public bool Authenticate(
            byte[] clientPublicKey,
            byte[] clientProof)
        {
            _A = new BigNumber(clientPublicKey);

            if (_A.IsZero)
                return false;

            if ((_A % _N).IsZero)
                return false;

            using (var sha = SHA1.Create())
            {
                // Compute session key
                ComputeSessionKey(sha);

                var nBytes = _N.ToByteArray();
                var gBytes = _g.ToByteArray();

                var nHash = sha.ComputeHash(nBytes);
                var gHash = sha.ComputeHash(gBytes);

                byte[] t3 = new byte[20];
                for (int i = 0; i < 20; i++)
                    t3[i] = (byte)(nHash[i] ^ gHash[i]);

                var t4 = sha.ComputeHash(Encoding.UTF8.GetBytes(_I));

                var s = _s.ToByteArray();
                var A = _A.ToByteArray();
                var B = _B.ToByteArray();
                var K = _K.ToByteArray();

                var bytesToCopy = new byte[][]
                {
                    t3, t4, s, A, B, K
                };

                byte[] MBytes = new byte[bytesToCopy.Sum(x => x.Length)];

                int offset = 0;
                foreach (var byteArray in bytesToCopy)
                {
                    Buffer.BlockCopy(byteArray, 0, MBytes, offset,
                        byteArray.Length);
                    offset += byteArray.Length;
                }

                _Mc = new BigNumber(sha.ComputeHash(MBytes));
                var realMc = new BigNumber(clientProof);

                return realMc.Equals(_Mc);
            }
        }

        public BigNumber ComputeProof()
        {
            using (var sha = SHA1.Create())
            {
                var A = _A.ToByteArray();
                var M = _Mc.ToByteArray();
                var K = _K.ToByteArray();

                byte[][] bytesToCopy = new byte[][]
                {
                    A, M, K
                };

                byte[] MBytes = new byte[bytesToCopy.Sum(x => x.Length)];

                int offset = 0;
                foreach (var byteArray in bytesToCopy)
                {
                    Buffer.BlockCopy(byteArray, 0, MBytes, offset,
                        byteArray.Length);
                    offset += byteArray.Length;
                }

                return new BigNumber(sha.ComputeHash(MBytes));
            }
        }

        public void Dispose()
        {
            _random?.Dispose();
        }

        public BigNumber GenerateRandomNumber(int numBytes)
        {
            byte[] number = new byte[numBytes];
            _random.GetBytes(number, 0, numBytes);

            return new BigNumber(number);
        }

        private void ComputeSessionKey(SHA1 sha)
        {
            var aBytes = _A.ToByteArray();
            var bBytes = _B.ToByteArray();

            byte[] temporary = new byte[aBytes.Length + bBytes.Length];
            Buffer.BlockCopy(aBytes, 0, temporary,
                0, aBytes.Length);
            Buffer.BlockCopy(bBytes, 0, temporary,
                aBytes.Length, bBytes.Length);

            // scrambling parameter
            BigNumber u = new BigNumber(sha.ComputeHash(temporary));

            var vmod = BigNumber.ModPow(_v, u, _N);
            var S = BigNumber.ModPow(_A * vmod, _b, _N);

            // compute strong session key

            byte[] t = S.ToByteArray();
            byte[] t1 = new byte[16];
            byte[] vK = new byte[40];

            // phase 1

            for (int i = 0; i < 16; i++)
                t1[i] = t[i * 2];

            var hash = sha.ComputeHash(t1);

            for (int i = 0; i < 20; i++)
                vK[i * 2] = hash[i];

            // phase 2

            for (int i = 0; i < 16; i++)
                t1[i] = t[i * 2 + 1];

            hash = sha.ComputeHash(t1);

            for (int i = 0; i < 20; i++)
                vK[i * 2 + 1] = hash[i];

            _K = new BigNumber(vK);
        }
    }
}
