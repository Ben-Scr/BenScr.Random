using System;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.Random.Tests;

public sealed class SecureRandomTests
{
    [Fact]
    public void PrimitiveResultsStayInsideDocumentedBounds()
    {
        for (int i = 0; i < 512; i++)
        {
            Assert.InRange(SecureRandom.NextInt32(), 0, int.MaxValue - 1);
            Assert.InRange(SecureRandom.NextInt32(int.MinValue, int.MaxValue), int.MinValue, int.MaxValue - 1);
            Assert.InRange(SecureRandom.NextInt32(17), 0, 16);
            Assert.Equal(-7, SecureRandom.NextInt32(-7, -6));
            Assert.InRange(SecureRandom.NextByte(), (byte)0, (byte)255);
            Assert.InRange(SecureRandom.NextByte(21), (byte)0, (byte)20);
            Assert.Equal((byte)254, SecureRandom.NextByte(254, 255));
            Assert.InRange(SecureRandom.NextDouble(), 0, Math.BitDecrement(1.0));
            Assert.InRange(SecureRandom.NextDouble(123), 0, Math.BitDecrement(123.0));
            Assert.InRange(SecureRandom.NextSingle(), 0, MathF.BitDecrement(1.0f));
            Assert.InRange(SecureRandom.NextSingle(123), 0, MathF.BitDecrement(123.0f));
            _ = SecureRandom.NextBoolean();
        }
    }

    [Fact]
    public void ExtremeFloatingRangesAndAdjacentEndpointsWork()
    {
        for (int i = 0; i < 512; i++)
        {
            Assert.InRange(SecureRandom.NextDouble(-double.MaxValue, double.MaxValue), -double.MaxValue, Math.BitDecrement(double.MaxValue));
            Assert.InRange(SecureRandom.NextSingle(-float.MaxValue, float.MaxValue), -float.MaxValue, MathF.BitDecrement(float.MaxValue));
            Assert.Equal(0.0, SecureRandom.NextDouble(double.Epsilon));
            Assert.Equal(0.0f, SecureRandom.NextSingle(float.Epsilon));
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 3)]
    [InlineData(double.NaN, 1)]
    [InlineData(0, double.NaN)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(1, double.PositiveInfinity)]
    public void InvalidFloatingRangesThrow(double minimum, double maximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextDouble(minimum, maximum));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextSingle((float)minimum, (float)maximum));
    }

    [Fact]
    public void InvalidIntegralRangesThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextInt32(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextInt32(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextInt32(1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextByte(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextByte(10, 2));
    }

    [Fact]
    public void StringsUseOnlyRequestedCharactersAndEnforceLimits()
    {
        Assert.Equal(10, SecureRandom.NextString().Length);
        Assert.Equal(string.Empty, SecureRandom.NextString(0));
        Assert.Equal(new string('Z', 200), SecureRandom.NextString(200, "Z"));
        Assert.All(SecureRandom.NextString(1000, "abc"), c => Assert.Contains(c, "abc"));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextString(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SecureRandom.NextString(SecureRandom.MaximumStringLength + 1));
        Assert.Throws<ArgumentException>(() => SecureRandom.NextString(0, ""));
    }

    [Fact]
    public void BufferFillRespectsBorrowedSlice()
    {
        byte[] bytes = new byte[32];
        Array.Fill(bytes, (byte)101);
        SecureRandom.FillBytes(bytes.AsSpan(8, 16));
        Assert.All(bytes.AsSpan(0, 8).ToArray(), value => Assert.Equal((byte)101, value));
        Assert.All(bytes.AsSpan(24, 8).ToArray(), value => Assert.Equal((byte)101, value));
        SecureRandom.FillBytes(Span<byte>.Empty);
    }

    [Fact]
    public void IndependentConcurrentCallsRespectContracts()
    {
        Parallel.For(0, 64, _ =>
        {
            Assert.InRange(SecureRandom.NextInt32(100), 0, 99);
            Assert.All(SecureRandom.NextString(12, "01"), c => Assert.Contains(c, "01"));
        });
    }
}

