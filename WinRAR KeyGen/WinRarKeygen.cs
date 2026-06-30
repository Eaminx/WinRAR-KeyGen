using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace WinRAR_KeyGen
{
    internal class WinRarKeygen
    {
        private static readonly BigInteger Order = BigIntegerHelper.FromHex("0x1026dd85081b82314691ced9bbec30547840e4bf72d8b5e0d258442bbcd31");
        private static readonly BigInteger PrivateKey = BigIntegerHelper.FromHex("0x59fe6abcca90bdb95f0105271fa85fb9f11f467450c1ae9044b7fd61d65e");
        private static ECPoint? _g;
        private static ECPoint G
        {
            get
            {
                if (_g == null)
                    _g = ECPoint.GetBasePoint();
                return _g;
            }
        }

        public class RegisterInfo
        {
            public string UserName = "";
            public string LicenseType = "";
            public string UID = "";
            public string[] Items = new string[4];
            public uint Checksum;
            public string HexData = "";
        }

        private struct ECCSignature
        {
            public BigInteger r;
            public BigInteger s;
        }

        private static BigInteger GeneratePrivateKey(byte[] seed)
        {
            uint[] generator = new uint[6];
            ushort[] rawPrivateKey = new ushort[15];

            if (seed != null && seed.Length > 0)
            {
                byte[] sha1Hash;
                using (SHA1 sha1 = SHA1.Create())
                    sha1Hash = sha1.ComputeHash(seed);

                for (int i = 0; i < 5; i++)
                {
                    uint val = (uint)(sha1Hash[i * 4] << 24 | sha1Hash[i * 4 + 1] << 16 |
                                     sha1Hash[i * 4 + 2] << 8 | sha1Hash[i * 4 + 3]);
                    generator[i + 1] = val;
                }
            }
            else
            {
                generator[1] = 0xeb3eb781;
                generator[2] = 0x50265329;
                generator[3] = 0xdc5ef4a3;
                generator[4] = 0x6847b9d5;
                generator[5] = 0xcde43b4c;
            }

            for (uint i = 0; i < 15; i++)
            {
                generator[0] = i + 1;
                byte[] genBytes = new byte[24];
                Buffer.BlockCopy(generator, 0, genBytes, 0, 24);

                byte[] sha1Hash;
                using (SHA1 sha1 = SHA1.Create())
                    sha1Hash = sha1.ComputeHash(genBytes);

                uint firstWord = (uint)(sha1Hash[0] | (sha1Hash[1] << 8) | (sha1Hash[2] << 16) | (sha1Hash[3] << 24));
                uint swapped = BigIntegerHelper.SwapUint32(firstWord);
                rawPrivateKey[i] = (ushort)(swapped & 0xFFFF);
            }

            return BigIntegerHelper.FromUint16ArrayLE(rawPrivateKey);
        }

        private static ECPoint GeneratePublicKey(BigInteger privateKey)
        {
            return G.Multiply(privateKey);
        }

        private static string GeneratePublicKeySM2CompressedFormat(string message)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);
            BigInteger privKey = GeneratePrivateKey(msgBytes);
            ECPoint pubKey = GeneratePublicKey(privKey);
            byte[] pubKeyCompressed = pubKey.DumpCompressed();

            byte[] xBytes = new byte[pubKeyCompressed.Length - 1];
            Array.Copy(pubKeyCompressed, 1, xBytes, 0, xBytes.Length);
            Array.Reverse(xBytes);

            BigInteger xInt = BigIntegerHelper.FromBytesLE(xBytes, xBytes.Length);
            xInt *= 2;

            if (pubKeyCompressed[0] == 0x03)
                xInt = BigIntegerHelper.SetBit(xInt, 0);

            string result = BigIntegerHelper.ToHex(xInt, true);
            if (result.Length > 64)
                result = result.Substring(result.Length - 64);
            if (result.Length < 64)
                result = result.PadLeft(64, '0');

            return result;
        }

        private static BigInteger GenerateRandomInteger()
        {
            Random rng = new Random();
            ushort[] rawRandom = new ushort[15];
            for (int i = 0; i < 15; i++)
                rawRandom[i] = (ushort)rng.Next(0x10000);
            return BigIntegerHelper.FromUint16ArrayLE(rawRandom);
        }

        private static BigInteger GenerateHashInteger(byte[] message)
        {
            byte[] sha1Hash;
            using (SHA1 sha1 = SHA1.Create())
                sha1Hash = sha1.ComputeHash(message);

            uint[] rawHash = new uint[10];
            for (int i = 0; i < 5; i++)
            {
                rawHash[i] = (uint)(sha1Hash[i * 4] << 24 | sha1Hash[i * 4 + 1] << 16 |
                                  sha1Hash[i * 4 + 2] << 8 | sha1Hash[i * 4 + 3]);
            }

            rawHash[5] = 0x0ffd8d43;
            rawHash[6] = 0xb4e33c7c;
            rawHash[7] = 0x53461bd1;
            rawHash[8] = 0x0f27a546;
            rawHash[9] = 0x1050d90d;

            return BigIntegerHelper.FromUint32ArrayLE(rawHash, 30);
        }

        private static ECCSignature Sign(byte[] data)
        {
            ECCSignature sig = new ECCSignature();
            BigInteger hash = GenerateHashInteger(data);

            while (true)
            {
                BigInteger random = GenerateRandomInteger();
                ECPoint randomPoint = G.Multiply(random);
                byte[] xBytes = GF2p15p17.Dump(randomPoint.X);
                sig.r = BigIntegerHelper.FromBytesLE(xBytes, xBytes.Length);
                sig.r = BigIntegerHelper.Mod(sig.r + hash, Order);

                if (sig.r.IsZero || sig.r + random == Order)
                    continue;

                sig.s = BigIntegerHelper.Mod(random - PrivateKey * sig.r, Order);
                if (sig.s.IsZero)
                    continue;

                break;
            }

            return sig;
        }

        private static uint CalculateCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < data.Length; i++)
                crc = (crc >> 8) ^ CRC32Table[(crc ^ data[i]) & 0xFF];
            return ~crc;
        }

        private static readonly uint[] CRC32Table = InitCRC32Table();

        private static uint[] InitCRC32Table()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint result = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((result & 1) != 0)
                        result = (result >> 1) ^ 0xEDB88320;
                    else
                        result >>= 1;
                }
                table[i] = result;
            }
            return table;
        }

        private static void CalculateChecksum(RegisterInfo info)
        {
            byte[] data = new byte[0];
            data = Concat(data, Encoding.UTF8.GetBytes(info.LicenseType));
            data = Concat(data, Encoding.UTF8.GetBytes(info.UserName));
            data = Concat(data, Encoding.UTF8.GetBytes(info.Items[0]));
            data = Concat(data, Encoding.UTF8.GetBytes(info.Items[1]));
            data = Concat(data, Encoding.UTF8.GetBytes(info.Items[2]));
            data = Concat(data, Encoding.UTF8.GetBytes(info.Items[3]));
            info.Checksum = ~CalculateCRC32(data);
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Array.Copy(a, 0, result, 0, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }

        public static RegisterInfo GenerateRegisterInfo(string userName, string licenseType)
        {
            RegisterInfo info = new RegisterInfo();
            info.UserName = userName;
            info.LicenseType = licenseType;

            string temp = GeneratePublicKeySM2CompressedFormat(userName);
            info.Items[3] = "60" + temp.Substring(0, 48);
            info.Items[0] = GeneratePublicKeySM2CompressedFormat(info.Items[3]);
            info.UID = temp.Substring(48, 16) + info.Items[0].Substring(0, 4);

            while (true)
            {
                byte[] licenseBytes = Encoding.UTF8.GetBytes(licenseType);
                ECCSignature sig = Sign(licenseBytes);
                string r = BigIntegerHelper.ToHex(sig.r, true).PadLeft(60, '0');
                string s = BigIntegerHelper.ToHex(sig.s, true).PadLeft(60, '0');

                if (r.Length == 60 && s.Length == 60)
                {
                    info.Items[1] = "60" + s + r;
                    break;
                }
            }

            temp = userName + info.Items[0];
            while (true)
            {
                byte[] userBytes = Encoding.UTF8.GetBytes(temp);
                ECCSignature sig = Sign(userBytes);
                string r = BigIntegerHelper.ToHex(sig.r, true).PadLeft(60, '0');
                string s = BigIntegerHelper.ToHex(sig.s, true).PadLeft(60, '0');

                if (r.Length == 60 && s.Length == 60)
                {
                    info.Items[2] = "60" + s + r;
                    break;
                }
            }

            CalculateChecksum(info);

            info.HexData = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}",
                info.Items[0].Length, info.Items[1].Length, info.Items[2].Length, info.Items[3].Length,
                info.Items[0], info.Items[1], info.Items[2], info.Items[3],
                info.Checksum.ToString("D10"));

            if (info.HexData.Length % 54 != 0)
                throw new Exception("Internal error: register data length incorrect");

            return info;
        }

        public static string BuildRegFileContent(RegisterInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RAR registration data");
            sb.AppendLine(info.UserName);
            sb.AppendLine(info.LicenseType);
            sb.AppendLine("UID=" + info.UID);

            for (int i = 0; i < info.HexData.Length; i += 54)
            {
                int len = Math.Min(54, info.HexData.Length - i);
                sb.AppendLine(info.HexData.Substring(i, len));
            }

            return sb.ToString();
        }
    }
}
