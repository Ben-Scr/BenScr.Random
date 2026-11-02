using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace BenScr.Random;

/// <summary>Stateless sampling from the platform's cryptographic random source.</summary>
/// <remarks>No seed or reproducible sequence is exposed. These methods share no mutable library state.
/// They generate data, not password hashes or a complete password policy.</remarks>
public static class SecureRandom
{
    /// <summary>The maximum supported string length, in UTF-16 code units.</summary>
    public const int MaximumStringLength = RandomRules.MaxStringLength;

    /// <summary>Fills a borrowed byte buffer with platform cryptographic random bytes.</summary>
    public static void FillBytes(Span<byte> destination) => RandomNumberGenerator.Fill(destination);

    /// <summary>Returns a Boolean using the platform bounded integer generator.</summary>
    public static bool NextBoolean() => RandomNumberGenerator.GetInt32(2) == 0;

    /// <summary>Returns a byte in [0, 255].</summary>
    public static byte NextByte() => (byte)RandomNumberGenerator.GetInt32(256);

    /// <summary>Returns a byte in [0, exclusiveMaximum); the maximum must be positive.</summary>
    public static byte NextByte(byte exclusiveMaximum) => (byte)RandomNumberGenerator.GetInt32(exclusiveMaximum);

    /// <summary>Returns a byte in [inclusiveMinimum, exclusiveMaximum); bounds must be ordered.</summary>
    public static byte NextByte(byte inclusiveMinimum, byte exclusiveMaximum) => (byte)NextInt32(inclusiveMinimum, exclusiveMaximum);

    /// <summary>Returns an integer in [0, Int32.MaxValue).</summary>
    public static int NextInt32() => RandomNumberGenerator.GetInt32(int.MaxValue);

    /// <summary>Returns an integer in [0, exclusiveMaximum); the maximum must be positive.</summary>
    public static int NextInt32(int exclusiveMaximum) => RandomNumberGenerator.GetInt32(exclusiveMaximum);

    /// <summary>Returns an integer in [inclusiveMinimum, exclusiveMaximum), using the platform's discard-and-retry mapping.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The minimum is not less than the maximum.</exception>
    public static int NextInt32(int inclusiveMinimum, int exclusiveMaximum)
    {
        if (inclusiveMinimum >= exclusiveMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }
        return RandomNumberGenerator.GetInt32(inclusiveMinimum, exclusiveMaximum);
    }

    /// <summary>Returns a multiple of 2^-53 in [0, 1), using the upper bits of eight little-endian random bytes.</summary>
    public static double NextDouble()
    {
        Span<byte> bytes = stackalloc byte[8];
        FillBytes(bytes);
        return (BinaryPrimitives.ReadUInt64LittleEndian(bytes) >> 11) * (1.0 / (1UL << 53));
    }

    /// <summary>Returns a finite double in [0, exclusiveMaximum); the maximum must be finite and positive.</summary>
    public static double NextDouble(double exclusiveMaximum) => NextDouble(0, exclusiveMaximum);

    /// <summary>Returns a finite double in [inclusiveMinimum, exclusiveMaximum), clamping upper-endpoint rounding.</summary>
    /// <remarks>Bounds must be finite and ordered. Floating outputs are quantized, not uniform over representable values.</remarks>
    public static double NextDouble(double inclusiveMinimum, double exclusiveMaximum)
    {
        RandomRules.ValidateRange(inclusiveMinimum, exclusiveMaximum);
        return RandomRules.Scale(NextDouble(), inclusiveMinimum, exclusiveMaximum);
    }

    /// <summary>Returns a multiple of 2^-24 in [0, 1), using the upper bits of four little-endian random bytes.</summary>
    public static float NextSingle()
    {
        Span<byte> bytes = stackalloc byte[4];
        FillBytes(bytes);
        return (BinaryPrimitives.ReadUInt32LittleEndian(bytes) >> 8) * (1.0f / (1 << 24));
    }

    /// <summary>Returns a finite float in [0, exclusiveMaximum); the maximum must be finite and positive.</summary>
    public static float NextSingle(float exclusiveMaximum) => NextSingle(0, exclusiveMaximum);

    /// <summary>Returns a finite float in [inclusiveMinimum, exclusiveMaximum), clamping upper-endpoint rounding.</summary>
    /// <remarks>Bounds must be finite and ordered; floating outputs are quantized.</remarks>
    public static float NextSingle(float inclusiveMinimum, float exclusiveMaximum)
    {
        RandomRules.ValidateRange(inclusiveMinimum, exclusiveMaximum);
        return RandomRules.Scale(NextSingle(), inclusiveMinimum, exclusiveMaximum);
    }

    /// <summary>Samples UTF-16 code units with replacement. Null selects ASCII letters and digits.</summary>
    /// <remarks>Length must be 0..MaximumStringLength and the alphabet nonempty. Duplicate entries retain their weighting.
    /// Surrogate code units are not interpreted as Unicode scalar values.</remarks>
    public static string NextString(int length = 10, string? alphabet = null)
    {
        alphabet ??= RandomRules.Alphabet;
        RandomRules.ValidateString(length, alphabet);
        return string.Create(length, alphabet, static (span, characters) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = characters[RandomNumberGenerator.GetInt32(characters.Length)];
            }
        });
    }
}
