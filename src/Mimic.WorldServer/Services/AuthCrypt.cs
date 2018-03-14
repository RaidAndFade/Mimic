using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using System.Diagnostics;
using Mimic.Common;

namespace Mimic.WorldServer
{
    public class AuthCrypt
    {
        RC4Engine serverEncrypt = new RC4Engine(), clientDecrypt = new RC4Engine();

        bool _ready = false;

        public AuthCrypt() {}

        public AuthCrypt(byte[] K)
        {
            byte[] encKey = { 0xCC, 0x98, 0xAE, 0x04, 0xE8, 0x97, 0xEA, 0xCA, 0x12, 0xDD, 0xC0, 0x93, 0x42, 0x91, 0x53, 0x57 };
            HMACSHA1 serverHmac = new HMACSHA1(encKey);
            byte[] encHash = serverHmac.ComputeHash(K);

            byte[] decKey = { 0xC2, 0xB3, 0x72, 0x3C, 0xC6, 0xAE, 0xD9, 0xB5, 0x34, 0x3C, 0x53, 0xEE, 0x2F, 0x43, 0x67, 0xCE };
            HMACSHA1 clientHmac = new HMACSHA1(decKey);
            byte[] decHash = clientHmac.ComputeHash(K);

            if (encHash.Length != 20 || decHash.Length != 20)
            {
                throw new Exception("InvalidDigestSize");
            }

            Debug.WriteLine(BitConverter.ToString(decHash).Replace("-", ""));
            Debug.WriteLine(BitConverter.ToString(encHash).Replace("-", ""));

            clientDecrypt.Init(true, new RC2Parameters(decHash));
            serverEncrypt.Init(true, new RC2Parameters(encHash));

            byte[] pass = new byte[1024];
            serverEncrypt.ProcessBytes(pass,0,pass.Length,pass,0);
            pass = new byte[1024];
            clientDecrypt.ProcessBytes(pass,0,pass.Length,pass,0);
            _ready = true;
        }

        public byte[] decrypt(byte[] d)
        {
            if (!_ready)
                return d;
            byte[] res = new byte[d.Length];
            clientDecrypt.ProcessBytes(d,0,d.Length,res,0);
            Debug.WriteLine("DEC");
            Debug.WriteLine(BitConverter.ToString(d).Replace("-", ""));
            Debug.WriteLine(BitConverter.ToString(res).Replace("-", ""));
            return res;
        }

        public byte[] encrypt(byte[] d)
        {
            if (!_ready)
                return d;
            byte[] res = new byte[d.Length];
            serverEncrypt.ProcessBytes(d, 0, d.Length, res, 0);
            Debug.WriteLine("ENC");
            Debug.WriteLine(BitConverter.ToString(d).Replace("-", ""));
            Debug.WriteLine(BitConverter.ToString(res).Replace("-", ""));
            return res;
        }
    }
}
