using System;
using System.Security.Cryptography;
using Mimic.Common;

namespace Mimic.WorldServer
{
    public class AuthCrypt
    {
        RC4 serverEncrypt, clientDecrypt;

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

            serverEncrypt = new RC4(encHash);
            clientDecrypt = new RC4(decHash);

            byte[] pass = new byte[1024];
            pass = serverEncrypt.process(pass);
            clientDecrypt.process(pass);
        }

        public byte[] decrypt(byte[] d)
        {
            if (clientDecrypt == null)
                return d;
            return clientDecrypt.process(d);
        }

        public byte[] encrypt(byte[] d)
        {
            if (serverEncrypt == null)
                return d;
            return serverEncrypt.process(d);
        }
    }
}
