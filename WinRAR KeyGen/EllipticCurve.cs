using System.Numerics;

namespace WinRAR_KeyGen
{
    internal class ECPoint
    {
        private static ushort[] _a;
        private static ushort[] _b;

        public ushort[] X;
        public ushort[] Y;

        static ECPoint()
        {
            _a = GF2p15p17.Zero();
            ushort[] bItems = new ushort[17];
            bItems[0] = 161;
            _b = GF2p15p17.FromElements(bItems);
        }

        public ECPoint()
        {
            X = GF2p15p17.Zero();
            Y = GF2p15p17.Zero();
        }

        public ECPoint(ushort[] x, ushort[] y)
        {
            X = x;
            Y = y;
        }

        public bool IsAtInfinity()
        {
            return GF2p15p17.IsZero(X) && GF2p15p17.IsZero(Y);
        }

        public ECPoint Negate()
        {
            return new ECPoint(X, GF2p15p17.Add(X, Y));
        }

        public ECPoint Double()
        {
            if (IsAtInfinity())
                return new ECPoint();

            ushort[] m = GF2p15p17.Add(GF2p15p17.Divide(Y, X), X);
            ushort[] newX = GF2p15p17.Add(GF2p15p17.Add(GF2p15p17.Square(m), m), _a);
            ushort[] newY = GF2p15p17.Add(GF2p15p17.Multiply(GF2p15p17.AddOne(m), newX), GF2p15p17.Square(X));

            return new ECPoint(newX, newY);
        }

        public ECPoint Add(ECPoint other)
        {
            if (IsAtInfinity()) return other;
            if (other.IsAtInfinity()) return this;

            if (GF2p15p17.IsEqual(X, other.X))
            {
                if (GF2p15p17.IsEqual(Y, other.Y))
                    return Double();
                else
                    return new ECPoint();
            }

            ushort[] m = GF2p15p17.Divide(GF2p15p17.Add(Y, other.Y), GF2p15p17.Add(X, other.X));
            ushort[] newX = GF2p15p17.Add(GF2p15p17.Add(GF2p15p17.Add(GF2p15p17.Add(GF2p15p17.Square(m), m), X), other.X), _a);
            ushort[] newY = GF2p15p17.Add(GF2p15p17.Add(GF2p15p17.Multiply(m, GF2p15p17.Add(X, newX)), newX), Y);

            return new ECPoint(newX, newY);
        }

        public ECPoint Multiply(BigInteger n)
        {
            ECPoint result = new ECPoint();
            ECPoint temp = new ECPoint(X, Y);
            int bitLength = BigIntegerHelper.BitLength(n);

            for (int i = 0; i < bitLength; i++)
            {
                if (BigIntegerHelper.TestBit(n, i))
                    result = result.Add(temp);
                temp = temp.Double();
            }

            return result;
        }

        public byte[] DumpCompressed()
        {
            if (IsAtInfinity())
                return new byte[] { 0x00 };

            byte[] xBytes = GF2p15p17.Dump(X);
            byte[] zBytes = GF2p15p17.Dump(GF2p15p17.Divide(Y, X));

            Array.Reverse(xBytes);

            byte[] result = new byte[1 + xBytes.Length];
            result[0] = (zBytes[0] & 1) == 1 ? (byte)0x03 : (byte)0x02;
            Array.Copy(xBytes, 0, result, 1, xBytes.Length);

            return result;
        }

        public static ECPoint GetBasePoint()
        {
            ushort[] gx = GF2p15p17.FromElements(new ushort[]
            {
                0x38CC, 0x052F, 0x2510, 0x45AA,
                0x1B89, 0x4468, 0x4882, 0x0D67,
                0x4FEB, 0x55CE, 0x0025, 0x4CB7,
                0x0CC2, 0x59DC, 0x289E, 0x65E3,
                0x56FD
            });

            ushort[] gy = GF2p15p17.FromElements(new ushort[]
            {
                0x31A7, 0x65F2, 0x18C4, 0x3412,
                0x7388, 0x54C1, 0x539B, 0x4A02,
                0x4D07, 0x12D6, 0x7911, 0x3B5E,
                0x4F0E, 0x216F, 0x2BF2, 0x1974,
                0x20DA
            });

            return new ECPoint(gx, gy);
        }
    }
}
