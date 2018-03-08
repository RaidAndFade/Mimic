using System;
using System.Numerics;
using System.Globalization;
using System.Security.Cryptography;

namespace Mimic.RealmServer
{
    public class SrpHandler : IDisposable
    {
        private const int PasswordHashLength = 20;

        private static readonly BigInteger _SafePrime
            = BigInteger.Parse(
"894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7",
                NumberStyles.HexNumber);

        private static readonly BigInteger _Generator = new BigInteger(7);

        private readonly RandomNumberGenerator _random;
        private readonly BigInteger _passwordVerifier;
        private readonly BigInteger _salt;
        private readonly BigInteger _privateKey;
        private readonly BigInteger _publicKey;

        public BigInteger PublicKey => _publicKey; // B
        public BigInteger Generator => _Generator; // g
        public BigInteger SafePrime => _SafePrime; // N
        public BigInteger Salt => _salt; // s

        public SrpHandler(
            string username,
            byte[] passwordHash)
        {
            if (passwordHash.Length != PasswordHashLength)
                throw new ArgumentException(
                    $"Password hash must be 20 bytes long",
                    nameof(passwordHash));

            _random = RNGCryptoServiceProvider.Create();

            Array.Reverse(passwordHash);
            Array.Resize(ref passwordHash, passwordHash.Length + 1);
            passwordHash[passwordHash.Length - 1] = 0;

            var x = new BigInteger(passwordHash);

            _passwordVerifier = BigInteger.ModPow(_Generator, x, _SafePrime);
            _salt = GenerateRandomNumber(32);
            _privateKey = GenerateRandomNumber(19);

            var genMod = BigInteger.ModPow(_Generator, _privateKey, _SafePrime);
            _publicKey = ((_passwordVerifier * 3) + genMod) % _SafePrime;
        }

        public void Dispose()
        {
            _random?.Dispose();
        }

        public BigInteger GenerateRandomNumber(int numBytes,
            bool positive = true)
        {
            byte[] number;

            if (positive)
                number = new byte[numBytes + 1];
            else
                number = new byte[numBytes];

            _random.GetBytes(number, 0, numBytes);

            return new BigInteger(number);
        }
    }
}
