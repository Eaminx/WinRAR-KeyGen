using System.Numerics;

namespace WinRAR_KeyGen
{
    internal static class BigIntegerHelper
    {
        public static BigInteger FromHex(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            Array.Reverse(bytes);
            byte[] withSign = new byte[bytes.Length + 1];
            Array.Copy(bytes, withSign, bytes.Length);
            return new BigInteger(withSign);
        }

        public static BigInteger FromBytesLE(byte[] data, int byteCount)
        {
            byte[] buf = new byte[byteCount + 1];
            Array.Copy(data, buf, Math.Min(data.Length, byteCount));
            return new BigInteger(buf);
        }

        public static BigInteger FromUint16ArrayLE(ushort[] values)
        {
            byte[] bytes = new byte[values.Length * 2 + 1];
            Buffer.BlockCopy(values, 0, bytes, 0, values.Length * 2);
            return new BigInteger(bytes);
        }

        public static BigInteger FromUint32ArrayLE(uint[] values, int byteCount)
        {
            byte[] src = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, src, 0, src.Length);
            byte[] buf = new byte[byteCount + 1];
            Array.Copy(src, buf, Math.Min(src.Length, byteCount));
            return new BigInteger(buf);
        }

        public static string ToHex(BigInteger value, bool lowerCase = true)
        {
            string s = value.ToString("x");
            if (!lowerCase) s = s.ToUpper();
            return s;
        }

        public static int BitLength(BigInteger value)
        {
            if (value.IsZero) return 0;
            byte[] bytes = value.ToByteArray();
            int lastIdx = bytes.Length - 1;
            while (lastIdx >= 0 && bytes[lastIdx] == 0) lastIdx--;
            if (lastIdx < 0) return 0;
            byte lastByte = bytes[lastIdx];
            int bits = lastIdx * 8;
            while (lastByte > 0)
            {
                bits++;
                lastByte >>= 1;
            }
            return bits;
        }

        public static bool TestBit(BigInteger value, int bitIndex)
        {
            byte[] bytes = value.ToByteArray();
            int byteIndex = bitIndex / 8;
            int bitOffset = bitIndex % 8;
            if (byteIndex >= bytes.Length) return false;
            return (bytes[byteIndex] & (1 << bitOffset)) != 0;
        }

        public static BigInteger SetBit(BigInteger value, int bitIndex)
        {
            return value | (BigInteger.One << bitIndex);
        }

        public static BigInteger Mod(BigInteger a, BigInteger m)
        {
            BigInteger r = a % m;
            if (r.Sign < 0) r += m;
            return r;
        }

        public static uint SwapUint32(uint value)
        {
            return ((value & 0xFF) << 24) | ((value & 0xFF00) << 8) |
                   ((value >> 8) & 0xFF00) | ((value >> 24) & 0xFF);
        }
    }
}
