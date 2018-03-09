using System;
using System.Globalization;
using System.Numerics;

namespace Mimic.Common
{
    /// <summary>
    /// OpenSSL-compliant big number type
    /// </summary>
    public struct BigNumber : IEquatable<BigNumber>
    {
        private readonly BigInteger _value;

        /// <summary>
        /// Returns true if the value of this instance is zero.
        /// </summary>
        public bool IsZero
            => _value.IsZero;

        /// <summary>
        /// Creates a new BigNumber from the given BigInteger
        /// </summary>
        /// <remarks>
        /// The BigInteger is taken as-is without any byte conversions.
        /// </remarks>
        /// <param name="value">The value of the number</param>
        public BigNumber(BigInteger value)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a new BigNumber from a given byte array
        /// </summary>
        /// <remarks>
        /// The value param is taken to be big endian and positive, complying
        /// with OpenSSL behaviour.
        /// </remarks>
        /// <param name="value">The byte array to convert</param>
        public BigNumber(byte[] value)
        {
            Array.Reverse(value); // Convert to little endian
            Array.Resize(ref value, value.Length + 1); // force positive MSB
            _value = new BigInteger(value);
        }

        /// <summary>
        /// Converts the absolute value of this instance to a byte array
        /// </summary>
        /// <remarks>
        /// The result is given in big endian form
        /// </remarks>
        /// <returns>The converted byte array</returns>
        public byte[] ToByteArray()
        {
            var value = BigInteger.Abs(_value); // absolute value
            var result = value.ToByteArray(); // get bytes

            if (result[result.Length - 1] == 0) // remove forced MSB
                Array.Resize(ref result, result.Length - 1);

            Array.Reverse(result); // convert to big endian

            return result;
        }

        /// <summary>
        /// Parses the input string as a hexadecimal number
        /// </summary>
        /// <param name="hex">the number to parse</param>
        /// <returns>A BigNumber with the parsed value</returns>
        public static BigNumber FromHexString(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = byte.Parse(hex.Substring(i, 2),
                    NumberStyles.HexNumber);
            }

            return new BigNumber(bytes);
        }

        public static BigNumber ModPow(BigNumber value,
            BigNumber exponent,
            BigNumber modulus)
            => new BigNumber(
                BigInteger.ModPow(value._value, exponent._value,
                    modulus._value)
                );

        public static BigNumber operator *(BigNumber lhs, int rhs)
            => new BigNumber(lhs._value * rhs);

        public static BigNumber operator *(BigNumber lhs, BigNumber rhs)
            => new BigNumber(lhs._value * rhs._value);

        public static BigNumber operator+(BigNumber lhs, BigNumber rhs)
            => new BigNumber(lhs._value + rhs._value);

        public static BigNumber operator%(BigNumber lhs, BigNumber rhs)
            => new BigNumber(lhs._value % rhs._value);

        public bool Equals(BigNumber other)
            => _value.Equals(other._value);

        public override bool Equals(object obj)
        {
            if (obj is BigNumber other)
                return Equals(other);

            return false;
        }

        public override int GetHashCode()
            => _value.GetHashCode();
    }
}
