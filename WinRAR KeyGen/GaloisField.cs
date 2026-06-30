namespace WinRAR_KeyGen
{
    internal static class GF2p15p17
    {
        public const int ElementCount = 17;
        private const int Order = 0x7FFF;
        private const ushort Poly = 0x8003;

        private static ushort[] _expTable;
        private static ushort[] _logTable;

        static GF2p15p17()
        {
            _expTable = new ushort[Order + 1];
            _logTable = new ushort[Order + 1];

            _expTable[0] = 1;
            for (int i = 1; i < Order; i++)
            {
                uint temp = (uint)_expTable[i - 1] * 2;
                if ((temp & 0x8000) != 0)
                    temp ^= Poly;
                _expTable[i] = (ushort)temp;
            }
            _expTable[Order] = (ushort)~_expTable[Order];

            for (int i = 0; i < Order; i++)
                _logTable[_expTable[i]] = (ushort)i;
        }

        public static ushort[] Zero()
        {
            return new ushort[ElementCount];
        }

        public static ushort[] One()
        {
            ushort[] v = new ushort[ElementCount];
            v[0] = 1;
            return v;
        }

        public static ushort[] FromElements(ushort[] items)
        {
            if (items.Length != ElementCount)
                throw new ArgumentException("Must have 17 elements");
            ushort[] r = new ushort[ElementCount];
            Array.Copy(items, r, ElementCount);
            return r;
        }

        public static bool IsZero(ushort[] a)
        {
            for (int i = 0; i < ElementCount; i++)
                if (a[i] != 0) return false;
            return true;
        }

        public static bool IsOne(ushort[] a)
        {
            if (a[0] != 1) return false;
            for (int i = 1; i < ElementCount; i++)
                if (a[i] != 0) return false;
            return true;
        }

        public static bool IsEqual(ushort[] a, ushort[] b)
        {
            for (int i = 0; i < ElementCount; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        public static ushort[] Add(ushort[] a, ushort[] b)
        {
            ushort[] r = new ushort[ElementCount];
            for (int i = 0; i < ElementCount; i++)
                r[i] = (ushort)(a[i] ^ b[i]);
            return r;
        }

        public static void AddAssign(ushort[] a, ushort[] b)
        {
            for (int i = 0; i < ElementCount; i++)
                a[i] ^= b[i];
        }

        public static ushort[] AddOne(ushort[] a)
        {
            ushort[] r = new ushort[ElementCount];
            Array.Copy(a, r, ElementCount);
            r[0] ^= 0x0001;
            return r;
        }

        public static void AddOneAssign(ushort[] a)
        {
            a[0] ^= 0x0001;
        }

        public static ushort[] Multiply(ushort[] a, ushort[] b)
        {
            ushort[] temp = new ushort[33];
            FullMultiplySchoolBook(17, 17, temp, a, b);
            ModularReduction(33, temp);
            ushort[] r = new ushort[ElementCount];
            Array.Copy(temp, r, ElementCount);
            return r;
        }

        public static void MultiplyAssign(ushort[] a, ushort[] b)
        {
            ushort[] r = Multiply(a, b);
            Array.Copy(r, a, ElementCount);
        }

        private static void FullMultiplySchoolBook(int m, int n, ushort[] result, ushort[] a, ushort[] b)
        {
            Array.Clear(result, 0, m + n - 1);
            for (int i = 0; i < m; i++)
            {
                if (a[i] != 0)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (b[j] != 0)
                        {
                            int g = _logTable[a[i]] + _logTable[b[j]];
                            if (g >= Order) g -= Order;
                            result[i + j] ^= _expTable[g];
                        }
                    }
                }
            }
        }

        private static void ModularReduction(int n, ushort[] a)
        {
            for (int i = n - 1; i > 16; i--)
            {
                if (a[i] != 0)
                {
                    a[i - 17 + 0] ^= a[i];
                    a[i - 17 + 3] ^= a[i];
                    a[i] = 0;
                }
            }
        }

        public static ushort[] Square(ushort[] a)
        {
            ushort[] temp = new ushort[33];
            for (int i = 0; i < 17; i++)
            {
                if (a[i] != 0)
                {
                    int g = _logTable[a[i]] * 2;
                    if (g >= Order) g -= Order;
                    temp[2 * i] = _expTable[g];
                }
            }
            ModularReduction(33, temp);
            ushort[] r = new ushort[ElementCount];
            Array.Copy(temp, r, ElementCount);
            return r;
        }

        public static ushort[] Inverse(ushort[] a)
        {
            if (IsZero(a))
                throw new InvalidOperationException("Zero has no inverse");

            ushort[] B = new ushort[34];
            ushort[] C = new ushort[34];
            ushort[] F = new ushort[34];
            ushort[] G = new ushort[34];

            B[0] = 1;
            int degB = 0;
            int degC = 0;

            int degF = 0;
            bool isZero = true;
            for (int i = 0; i < 17; i++)
            {
                F[i] = a[i];
                if (F[i] != 0)
                {
                    isZero = false;
                    degF = i;
                }
            }
            if (isZero)
                throw new InvalidOperationException("Zero has no inverse");

            int degG = 17;
            G[0] = 1;
            G[3] = 1;
            G[17] = 1;

            ushort[] lpF = F, lpG = G, lpB = B, lpC = C;
            int dF = degF, dG = degG, dB = degB, dC = degC;

            while (true)
            {
                if (dF == 0)
                {
                    ushort[] result = new ushort[ElementCount];
                    ushort logF0 = _logTable[lpF[0]];
                    for (int i = 0; i <= dB && i < ElementCount; i++)
                    {
                        if (lpB[i] != 0)
                        {
                            int g2 = _logTable[lpB[i]] - logF0;
                            if (g2 < 0) g2 += Order;
                            result[i] = _expTable[g2];
                        }
                    }
                    return result;
                }

                if (dF < dG)
                {
                    var tmpF = lpF; lpF = lpG; lpG = tmpF;
                    var tmpD = dF; dF = dG; dG = tmpD;
                    var tmpB = lpB; lpB = lpC; lpC = tmpB;
                    tmpD = dB; dB = dC; dC = tmpD;
                }

                int j = dF - dG;
                int g = _logTable[lpF[dF]] - _logTable[lpG[dG]];
                if (g < 0) g += Order;
                ushort alpha = _expTable[g];

                AddScale(lpF, ref dF, alpha, j, lpG, dG);
                AddScale(lpB, ref dB, alpha, j, lpC, dC);
            }
        }

        private static void AddScale(ushort[] a, ref int degA, ushort alpha, int j, ushort[] b, int degB)
        {
            int logAlpha = _logTable[alpha];
            for (int i = 0; i <= degB; i++)
            {
                if (b[i] != 0)
                {
                    int g = logAlpha + _logTable[b[i]];
                    if (g >= Order) g -= Order;
                    a[j + i] ^= _expTable[g];
                    if (a[j + i] != 0 && i + j > degA)
                        degA = i + j;
                }
            }
            while (degA > 0 && a[degA] == 0) degA--;
        }

        public static ushort[] Divide(ushort[] a, ushort[] b)
        {
            ushort[] invB = Inverse(b);
            return Multiply(a, invB);
        }

        public static byte[] Dump(ushort[] val)
        {
            int dumpSize = 32;
            byte[] result = new byte[dumpSize];
            int pbWritePtr = 0;
            int leftBits = 8;

            for (int i = 0; i < 17; i++)
            {
                byte low8 = (byte)(val[i]);
                byte high7 = (byte)(val[i] >> 8);

                if (leftBits == 8)
                {
                    result[pbWritePtr] = low8;
                    pbWritePtr++;
                }
                else
                {
                    result[pbWritePtr] |= (byte)(low8 << (8 - leftBits));
                    pbWritePtr++;
                    result[pbWritePtr] = (byte)(low8 >> leftBits);
                }

                if (leftBits == 8)
                {
                    result[pbWritePtr] = high7;
                    leftBits = 1;
                }
                else if (leftBits == 7)
                {
                    result[pbWritePtr] |= (byte)(high7 << 1);
                    pbWritePtr++;
                    leftBits = 8;
                }
                else
                {
                    result[pbWritePtr] |= (byte)(high7 << (8 - leftBits));
                    pbWritePtr++;
                    result[pbWritePtr] = (byte)(high7 >> leftBits);
                    leftBits = 8 - (7 - leftBits);
                }
            }

            return result;
        }
    }
}
