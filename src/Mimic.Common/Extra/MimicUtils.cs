using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Numerics;

namespace Mimic.Common
{
    public class MimicUtils
    {
        
        public static byte[] HexStringToByteArray(string hex){
            return Enumerable.Range(0, hex.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                 .ToArray();
        }
        public static byte[] HexStringToByteArray(string hex, int minNumBytes=0)
        {
        //     var res = Enumerable.Range(0, hex.Length)
        //          .Where(x => x % 2 == 0)
        //          .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
        //          .ToArray();
        //     while(res.Length<minNumBytes){
        //         byte[] tmp = new byte[res.Length + 1];
        //         res.CopyTo(tmp, 1);
        //         tmp[0] = (byte)0;
        //         res = tmp;
        //     }
            return BigIntToByteArray(BigIntFromHexString(hex),minNumBytes);
        }
        public static BigInteger BigIntFromHexString(string hex)
            => BigInteger.Parse($"00{hex}", NumberStyles.HexNumber);

        public static BigInteger BigIntFromByteArray(byte[] bytes)
        {
            Array.Resize(ref bytes, bytes.Length + 1); // force MSB = 0
            return new BigInteger(bytes);
        }

        public static byte[] BigIntToByteArray(BigInteger value,
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
