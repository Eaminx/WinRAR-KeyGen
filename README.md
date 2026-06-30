# WinRAR KeyGen

A C# implementation of WinRAR license key generator based on elliptic curve cryptography.

## Overview

This tool generates valid WinRAR license files (`rarreg.key`) by implementing the same cryptographic algorithm used by WinRAR for license verification. The implementation is based on the [original C++ keygen by bitcookies](https://github.com/bitcookies/winrar-keygen).

**Disclaimer:** This project is for educational purposes only. WinRAR is commercial software - please consider purchasing a license to support the developers.

## How It Works

### Cryptographic Foundation

WinRAR uses a custom elliptic curve signature scheme over a composite finite field to verify licenses. The signature scheme is based on:

- **Base Field:** GF(2^15) with irreducible polynomial x^15 + x + 1
- **Extension Field:** GF((2^15)^17) with irreducible polynomial y^17 + y^3 + 1
- **Elliptic Curve:** y² + xy = x³ + ax² + b over GF((2^15)^17)

### Key Generation Process

The license generation involves several cryptographic steps:

#### 1. Private Key Generation

A deterministic private key is generated from the username using SHA-1:

```
1. Hash the username with SHA-1
2. Use the hash to initialize a generator
3. Generate 15 iterations of SHA-1 hashes
4. Extract 16-bit values from each hash
5. Concatenate to form a 240-bit private key
```

#### 2. Public Key Computation

The public key is computed by scalar multiplication on the elliptic curve:

```
PublicKey = PrivateKey × G
```

Where G is the base point (generator point) of the curve.

#### 3. SM2 Compressed Format

The public key is converted to a compressed SM2 format:

1. Take the x-coordinate of the public key point
2. Multiply by 2
3. Set the LSB based on the y/x ratio parity
4. Convert to hexadecimal string

#### 4. License Signature

Two ECDSA-like signatures are generated:

- **License Type Signature:** Signs the license type string
- **Username Signature:** Signs the username combined with derived data

Each signature produces (r, s) pairs using:

```
r = (k × G).x + Hash(message) mod Order
s = k - PrivateKey × r mod Order
```

Where k is a random integer for each signature.

#### 5. Checksum Calculation

A CRC32 checksum is computed over all license data fields:

```
Checksum = ~CRC32(LicenseType + UserName + Items[0..3])
```

#### 6. Registration Data Assembly

The final `rarreg.key` file contains:

```
RAR registration data
<Username>
<License Type>
UID=<hex>
<length><length><length><length><Items[0]><Items[1]><Items[2]><Items[3]><Checksum>
```

## Technical Details

### Elliptic Curve Parameters

```
Order = 0x1026dd85081b82314691ced9bbec30547840e4bf72d8b5e0d258442bbcd31
PrivateKey = 0x59fe6abcca90bdb95f0105271fa85fb9f11f467450c1ae9044b7fd61d65e
```

### Base Point G

The generator point G has coordinates in GF((2^15)^17):

```
Gx = {0x38CC, 0x052F, 0x2510, 0x45AA, 0x1B89, 0x4468, 0x4882, 0x0D67,
      0x4FEB, 0x55CE, 0x0025, 0x4CB7, 0x0CC2, 0x59DC, 0x289E, 0x65E3, 0x56FD}

Gy = {0x31A7, 0x65F2, 0x18C4, 0x3412, 0x7388, 0x54C1, 0x539B, 0x4A02,
      0x4D07, 0x12D6, 0x7911, 0x3B5E, 0x4F0E, 0x216F, 0x2BF2, 0x1974, 0x20DA}
```

### Galois Field Arithmetic

The implementation uses lookup tables for efficient multiplication in GF(2^15):

- **Exp Table:** Precomputed powers of the generator
- **Log Table:** Precomputed discrete logarithms
- **Multiplication:** Uses log-exp table method: `a × b = exp[log[a] + log[b]]`

## Building

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

### Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/winrar-keygen.git
cd winrar-keygen

# Build the project
dotnet build

# Run the application
dotnet run --project "WinRAR KeyGen"
```

### Publish as Standalone Executable

```bash
# Publish for Windows x64
dotnet publish -c Release -r win-x64 --self-contained

# Publish with AOT compilation (smaller, faster)
dotnet publish -c Release -r win-x64 /p:PublishAot=true
```

## Usage

1. Run the application
2. Enter your name when prompted
3. Enter the license type (or press Enter for default)
4. The tool will:
   - Locate your WinRAR installation
   - Close WinRAR if it's running
   - Generate the license key
   - Write `rarreg.key` to both the WinRAR directory and `%APPDATA%\WinRAR`
   - Launch WinRAR to verify activation

## Project Structure

```
WinRAR KeyGen/
├── Program.cs              # Main application entry point and UI
├── WinRarKeygen.cs         # Core key generation algorithm
├── EllipticCurve.cs        # Elliptic curve point operations
├── GaloisField.cs          # GF((2^15)^17) arithmetic
├── BigIntegerHelper.cs     # BigInteger utility functions
└── WinRAR KeyGen.csproj    # Project configuration
```

## Implementation Notes

### C# vs C++ Implementation

This C# port uses `System.Numerics.BigInteger` instead of GMP/MPIR libraries. Key differences:

- **Byte Order:** .NET BigInteger uses little-endian internally
- **Modular Arithmetic:** C# `%` operator can return negative values; use `BigIntegerHelper.Mod()` for proper modular reduction
- **Bit Operations:** Use `BigIntegerHelper.TestBit()` and `SetBit()` for bit manipulation

### Common Pitfalls

1. **SHA1 Byte Order:** When extracting uint32 from SHA1 digest, use big-endian byte order
2. **Modular Reduction:** Always ensure positive results: `r = a % m; if (r < 0) r += m;`
3. **Hex String Padding:** Signature r and s values must be exactly 60 hex characters

## Credits

- Original algorithm and C++ implementation: [bitcookies/winrar-keygen](https://github.com/bitcookies/winrar-keygen)
- UI framework: [Spectre.Console](https://spectreconsole.net/)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Disclaimer

This software is provided for educational purposes only. The authors are not responsible for any misuse of this software. WinRAR is a commercial product - please support the developers by purchasing a license if you find it useful.
