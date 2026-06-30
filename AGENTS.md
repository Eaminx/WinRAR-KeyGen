# WinRAR KeyGen

C# implementation of WinRAR license key generator.

## Build Commands

```bash
# Build the project
dotnet build

# Run the application
dotnet run --project "WinRAR KeyGen"

# Publish as standalone executable
dotnet publish -c Release -r win-x64 --self-contained
```

## Project Structure

- `Program.cs` - Main application and UI (Spectre.Console)
- `WinRarKeygen.cs` - Core key generation algorithm
- `EllipticCurve.cs` - Elliptic curve operations
- `GaloisField.cs` - GF((2^15)^17) arithmetic
- `BigIntegerHelper.cs` - BigInteger utilities

## Key Algorithm

The keygen uses elliptic curve cryptography over GF((2^15)^17):
1. Generate private key from username using SHA-1
2. Compute public key via EC scalar multiplication
3. Generate ECDSA-like signatures for license type and username
4. Calculate CRC32 checksum
5. Assemble registration data into rarreg.key format

## Dependencies

- .NET 8.0
- Spectre.Console (for UI)
