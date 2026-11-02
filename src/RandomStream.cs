using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace BenScr.Random;

/// <summary>A reproducible xorshift64* stream for simulations and sampling, not cryptographic use.</summary>
/// <remarks>Instances are mutable and require external synchronization. The exact call sequence,
/// seed and algorithm version determine all results. No cross-language implementation is promised.</remarks>
public sealed class RandomStream
{
    /// <summary>Identifies the raw algorithm and all sampling transformations in this release.</summary>
    public const string AlgorithmId = "xorshift64star-sampling-v1";

    /// <summary>The nonzero state substituted for a zero seed.</summary>
    public const ulong ZeroSeedState = 0x9E3779B97F4A7C15UL;

    /// <summary>The maximum supported string length, in UTF-16 code units.</summary>
    public const int MaximumStringLength = RandomRules.MaxStringLength;

    private ulong state;

    /// <summary>Creates a stream from a platform-random seed; subsequent draws are non-cryptographic.</summary>
    public RandomStream()
    {
        Reseed();
    }

    /// <summary>Creates a stream from a seed. Zero is mapped to <see cref="ZeroSeedState"/>.</summary>
    public RandomStream(ulong seed)
    {
        SetSeed(seed);
    }

    /// <summary>Restarts the sequence at a seed, mapping zero to <see cref="ZeroSeedState"/>.</summary>
    public void SetSeed(ulong seed) => state = seed == 0 ? ZeroSeedState : seed;

    /// <summary>Replaces the state with eight bytes from the platform cryptographic source.</summary>
    /// <remarks>This does not make this stream's output suitable for secrets.</remarks>
    public void Reseed()
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        SetSeed(BinaryPrimitives.ReadUInt64LittleEndian(bytes));
    }

    /// <summary>Returns the current nonzero state, not the original seed. Persist it with <see cref="AlgorithmId"/>.</summary>
    public ulong CaptureState() => state;

    /// <summary>Restores a previously captured state. Zero is rejected without changing the stream.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The state is zero.</exception>
    public void RestoreState(ulong savedState)
    {
        ArgumentOutOfRangeException.ThrowIfZero(savedState);
        state = savedState;
    }

    /// <summary>Returns the next raw xorshift64* output in [1, UInt64.MaxValue].</summary>
    /// <remarks>The shifts are 12 right, 25 left and 27 right; multiplication wraps modulo 2^64.</remarks>
    public ulong NextUInt64()
    {
        state ^= state >> 12;
        state ^= state << 25;
        state ^= state >> 27;
        return unchecked(state * 2685821657736338717UL);
    }

    /// <summary>Returns an integer in [0, exclusiveMaximum), discarding an incomplete residue group.</summary>
    /// <remarks>Accounts for the raw generator's missing zero. May consume multiple raw outputs.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The maximum is zero.</exception>
    public ulong NextUInt64(ulong exclusiveMaximum)
    {
        ArgumentOutOfRangeException.ThrowIfZero(exclusiveMaximum);
        while (true)
        {
            if (RandomRules.TryMap(NextUInt64(), exclusiveMaximum, out ulong result))
            {
                return result;
            }
        }
    }

    /// <summary>Returns an integer in [0, Int32.MaxValue).</summary>
    public int NextInt32() => NextInt32(int.MaxValue);

    /// <summary>Returns an integer in [0, exclusiveMaximum). The maximum must be positive.</summary>
    public int NextInt32(int exclusiveMaximum) => NextInt32(0, exclusiveMaximum);

    /// <summary>Returns an integer in [inclusiveMinimum, exclusiveMaximum), supporting spans wider than Int32.MaxValue.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The minimum is not less than the maximum.</exception>
    public int NextInt32(int inclusiveMinimum, int exclusiveMaximum)
    {
        if (inclusiveMinimum >= exclusiveMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }
        ulong width = (ulong)((long)exclusiveMaximum - inclusiveMinimum);
        return (int)(inclusiveMinimum + (long)NextUInt64(width));
    }

    /// <summary>Returns a Boolean, using the bounded two-value mapping.</summary>
    public bool NextBoolean() => NextUInt64(2) == 0;

    /// <summary>Returns a byte in [0, 255].</summary>
    public byte NextByte() => (byte)NextUInt64(256);

    /// <summary>Returns a byte in [0, exclusiveMaximum). The maximum must be positive.</summary>
    public byte NextByte(byte exclusiveMaximum) => (byte)NextInt32(exclusiveMaximum);

    /// <summary>Returns a byte in [inclusiveMinimum, exclusiveMaximum). The minimum must be less than the maximum.</summary>
    public byte NextByte(byte inclusiveMinimum, byte exclusiveMaximum) => (byte)NextInt32(inclusiveMinimum, exclusiveMaximum);

    /// <summary>Fills a borrowed buffer by calling <see cref="NextByte()"/> for each element; an empty span consumes no state.</summary>
    public void FillBytes(Span<byte> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] = NextByte();
        }
    }

    /// <summary>Returns a multiple of 2^-53 in [0, 1), using the upper 53 raw output bits.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Returns a finite double in [0, exclusiveMaximum). The maximum must be finite and positive.</summary>
    public double NextDouble(double exclusiveMaximum) => NextDouble(0, exclusiveMaximum);

    /// <summary>Returns a finite double in [inclusiveMinimum, exclusiveMaximum), clamping rounding at the upper endpoint.</summary>
    /// <remarks>Both bounds must be finite and ordered. Floating-point outputs are quantized, not uniform over representable values.</remarks>
    public double NextDouble(double inclusiveMinimum, double exclusiveMaximum)
    {
        RandomRules.ValidateRange(inclusiveMinimum, exclusiveMaximum);
        return RandomRules.Scale(NextDouble(), inclusiveMinimum, exclusiveMaximum);
    }

    /// <summary>Returns a multiple of 2^-24 in [0, 1), using the upper 24 raw output bits.</summary>
    public float NextSingle() => (NextUInt64() >> 40) * (1.0f / (1 << 24));

    /// <summary>Returns a finite float in [0, exclusiveMaximum). The maximum must be finite and positive.</summary>
    public float NextSingle(float exclusiveMaximum) => NextSingle(0, exclusiveMaximum);

    /// <summary>Returns a finite float in [inclusiveMinimum, exclusiveMaximum), clamping rounding at the upper endpoint.</summary>
    /// <remarks>Both bounds must be finite and ordered. Floating-point outputs are quantized.</remarks>
    public float NextSingle(float inclusiveMinimum, float exclusiveMaximum)
    {
        RandomRules.ValidateRange(inclusiveMinimum, exclusiveMaximum);
        return RandomRules.Scale(NextSingle(), inclusiveMinimum, exclusiveMaximum);
    }

    /// <summary>Samples UTF-16 code units from an alphabet with replacement. Default alphabet is ASCII letters and digits.</summary>
    /// <remarks>Length must be 0..MaximumStringLength. The alphabet must be nonempty; duplicates give repeated entries more weight.
    /// Null selects the default alphabet. Surrogate code units are not interpreted as Unicode scalar values.</remarks>
    public string NextString(int length = 10, string? alphabet = null)
    {
        alphabet ??= RandomRules.Alphabet;
        RandomRules.ValidateString(length, alphabet);
        return string.Create(length, (Stream: this, Alphabet: alphabet), static (span, input) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = input.Alphabet[input.Stream.NextInt32(input.Alphabet.Length)];
            }
        });
    }

    /// <summary>Selects one element from a borrowed nonempty span without modifying it.</summary>
    /// <exception cref="ArgumentException">The span is empty.</exception>
    public T Choose<T>(ReadOnlySpan<T> values)
    {
        if (values.IsEmpty)
        {
            throw new ArgumentException("At least one value is required.", nameof(values));
        }
        return values[NextInt32(values.Length)];
    }

    /// <summary>Shuffles a borrowed span in place using descending Fisher-Yates and bounded integer draws.</summary>
    /// <remarks>Spans with fewer than two elements consume no state. Elements are never cloned.</remarks>
    public void Shuffle<T>(Span<T> values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int other = NextInt32(i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }
}
