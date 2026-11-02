using System;
using System.Linq;
using System.Numerics;
using Xunit;

namespace BenScr.Random.Tests;

public sealed class RandomStreamTests
{
    [Fact]
    public void RawSequenceMatchesIndependentIntegerReferenceVectors()
    {
        var random = new RandomStream(1);
        ulong[] expected = { 5180492295206395165, 12380297144915551517, 13389498078930870103, 5599127315341312413, 1036278371763004928, 14440594066559445721 };
        Assert.Equal(expected, expected.Select(_ => random.NextUInt64()).ToArray());
        random.SetSeed(42);
        Assert.Equal(6255019084209693600UL, random.NextUInt64());
        Assert.Equal(14430073426741505498UL, random.NextUInt64());
        random.SetSeed(0);
        Assert.Equal(973819730272012410UL, random.NextUInt64());
        Assert.Equal("xorshift64star-sampling-v1", RandomStream.AlgorithmId);
    }

    [Fact]
    public void CaptureAndRestoreContinueMixedDrawsExactly()
    {
        var random = new RandomStream(9876);
        random.NextDouble();
        ulong state = random.CaptureState();
        string expected = random.NextString(91);
        double expectedDouble = random.NextDouble(-100, 78);
        random.RestoreState(state);
        Assert.Equal(expected, random.NextString(91));
        Assert.Equal(expectedDouble, random.NextDouble(-100, 78));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.RestoreState(0));
        Assert.NotEqual(0UL, random.CaptureState());
    }

    [Fact]
    public void ZeroSeedMapsToDocumentedStateAndReseedingRestarts()
    {
        var first = new RandomStream(0);
        var second = new RandomStream(RandomStream.ZeroSeedState);
        Assert.Equal(RandomStream.ZeroSeedState, first.CaptureState());
        Assert.Equal(first.NextUInt64(), second.NextUInt64());
        first.SetSeed(0);
        Assert.Equal(RandomStream.ZeroSeedState, first.CaptureState());
        first.Reseed();
        Assert.NotEqual(0UL, first.CaptureState());
        Assert.NotEqual(0UL, new RandomStream().CaptureState());
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    [InlineData(10UL)]
    [InlineData(256UL)]
    [InlineData(4294967295UL)]
    [InlineData(9223372036854775809UL)]
    [InlineData(18446744073709551615UL)]
    public void RejectionBoundaryMatchesExactPopulationPartition(ulong bound)
    {
        BigInteger population = ulong.MaxValue;
        BigInteger completeGroups = population / bound;
        ulong limit = (ulong)(completeGroups * bound);
        ulong[] candidates = { 0, 1, limit - 1, ulong.MaxValue - 1 };
        foreach (ulong candidate in candidates)
        {
            bool accepted = RandomRules.TryMap(candidate + 1, bound, out ulong result);
            Assert.Equal(new BigInteger(candidate) < completeGroups * bound, accepted);
            Assert.Equal((ulong)(new BigInteger(candidate) % bound), result);
        }
        if (limit < ulong.MaxValue)
        {
            Assert.False(RandomRules.TryMap(limit + 1, bound, out _));
        }
    }

    [Fact]
    public void BoundedSamplingActuallyRetriesRejectedRawOutput()
    {
        const ulong bound = 9223372036854775809;
        var reference = new RandomStream(42);
        reference.NextUInt64();
        ulong checkpoint = reference.CaptureState();
        int draws = 0;
        ulong raw;
        do
        {
            raw = reference.NextUInt64();
            draws++;
        }
        while (raw - 1 >= bound);
        var actual = new RandomStream(checkpoint);
        Assert.Equal(raw - 1, actual.NextUInt64(bound));
        Assert.True(draws > 1);
        Assert.Equal(reference.CaptureState(), actual.CaptureState());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 0)]
    [InlineData(-80, -2)]
    [InlineData(0, int.MaxValue)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void IntegerRangesAlwaysRespectHalfOpenBounds(int minimum, int maximum)
    {
        var random = new RandomStream(128);
        for (int i = 0; i < 1000; i++)
        {
            int value = random.NextInt32(minimum, maximum);
            Assert.InRange(value, minimum, maximum - 1);
        }
    }

    [Fact]
    public void FullIntegerRangeReachesBothSigns()
    {
        var random = new RandomStream(123);
        int[] values = Enumerable.Range(0, 100).Select(_ => random.NextInt32(int.MinValue, int.MaxValue)).ToArray();
        Assert.Contains(values, value => value < 0);
        Assert.Contains(values, value => value > 0);
    }

    [Fact]
    public void IntegerAndByteInvalidRangesDoNotAdvanceState()
    {
        var random = new RandomStream(55);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextUInt64(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt32(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt32(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt32(5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextByte(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextByte(20, 1));
        Assert.Equal(55UL, random.CaptureState());
    }

    [Fact]
    public void PrimitiveDrawsAndByteBufferHonorTheirContracts()
    {
        var random = new RandomStream(33);
        byte[] bytes = new byte[2000];
        random.FillBytes(bytes);
        Assert.Contains((byte)0, bytes);
        Assert.Contains((byte)255, bytes);
        var replay = new RandomStream(33);
        Assert.Equal(bytes, bytes.Select(_ => replay.NextByte()).ToArray());
        ulong saved = random.CaptureState();
        random.FillBytes(Span<byte>.Empty);
        Assert.Equal(saved, random.CaptureState());
        Assert.InRange(random.NextByte(20), (byte)0, (byte)19);
        Assert.Equal((byte)254, random.NextByte(254, 255));
        Assert.InRange(random.NextInt32(), 0, int.MaxValue - 1);
        bool[] booleans = Enumerable.Range(0, 32).Select(_ => random.NextBoolean()).ToArray();
        Assert.Contains(true, booleans);
        Assert.Contains(false, booleans);
    }

    [Fact]
    public void UnitFloatingValuesPreserveLegacyRawBitMapping()
    {
        var doubles = new RandomStream(1);
        Assert.Equal((5180492295206395165UL >> 11) / 9007199254740992.0, doubles.NextDouble());
        var singles = new RandomStream(1);
        Assert.Equal((5180492295206395165UL >> 40) / 16777216.0f, singles.NextSingle());
        for (int i = 0; i < 2000; i++)
        {
            Assert.InRange(doubles.NextDouble(), 0, Math.BitDecrement(1.0));
            Assert.InRange(singles.NextSingle(), 0, MathF.BitDecrement(1.0f));
        }
    }

    [Theory]
    [InlineData(-double.MaxValue, double.MaxValue)]
    [InlineData(0, double.Epsilon)]
    [InlineData(-double.Epsilon, 0)]
    [InlineData(1e300, 1.1e300)]
    [InlineData(-100, 100)]
    public void ExtremeDoubleBoundsRemainFiniteAndHalfOpen(double minimum, double maximum)
    {
        var random = new RandomStream(7);
        for (int i = 0; i < 1000; i++)
        {
            Assert.InRange(random.NextDouble(minimum, maximum), minimum, Math.BitDecrement(maximum));
        }
        Assert.InRange(RandomRules.Scale(Math.BitDecrement(1.0), minimum, maximum), minimum, Math.BitDecrement(maximum));
        Assert.Equal(minimum, RandomRules.Scale(0, minimum, maximum));
    }

    [Theory]
    [InlineData(-float.MaxValue, float.MaxValue)]
    [InlineData(0, float.Epsilon)]
    [InlineData(-float.Epsilon, 0)]
    [InlineData(1e30f, 1.1e30f)]
    public void ExtremeSingleBoundsRemainFiniteAndHalfOpen(float minimum, float maximum)
    {
        var random = new RandomStream(7);
        for (int i = 0; i < 500; i++)
        {
            Assert.InRange(random.NextSingle(minimum, maximum), minimum, MathF.BitDecrement(maximum));
        }
    }

    [Theory]
    [InlineData(double.NaN, 1)]
    [InlineData(0, double.NaN)]
    [InlineData(double.NegativeInfinity, 1)]
    [InlineData(0, double.PositiveInfinity)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void InvalidFloatingBoundsThrowWithoutDrawing(double minimum, double maximum)
    {
        var random = new RandomStream(123);
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextDouble(minimum, maximum));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextSingle((float)minimum, (float)maximum));
        Assert.Equal(123UL, random.CaptureState());
    }

    [Fact]
    public void FloatingMaximumOverloadsValidateAndScale()
    {
        var random = new RandomStream(97);
        Assert.InRange(random.NextDouble(100), 0, Math.BitDecrement(100.0));
        Assert.InRange(random.NextSingle(100), 0, MathF.BitDecrement(100.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextDouble(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextSingle(-2));
    }

    [Fact]
    public void StringsValidateLengthAndAlphabetAndConsumeNothingForEmptyOutput()
    {
        var random = new RandomStream(1);
        Assert.Equal(string.Empty, random.NextString(0));
        Assert.Equal(1UL, random.CaptureState());
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextString(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextString(RandomStream.MaximumStringLength + 1));
        Assert.Throws<ArgumentException>(() => random.NextString(0, ""));
        Assert.Equal(1UL, random.CaptureState());
        Assert.Equal(new string('X', 40), random.NextString(40, "X"));
        Assert.All(random.NextString(512, "aab"), c => Assert.Contains(c, "ab"));
        Assert.Equal(10, random.NextString().Length);
        Assert.All(random.NextString(100), c => Assert.Contains(c, RandomRules.Alphabet));
    }

    [Fact]
    public void ShuffleIsReproducibleAndPreservesMultiset()
    {
        int[] first = Enumerable.Range(0, 100).ToArray();
        int[] second = (int[])first.Clone();
        new RandomStream(99).Shuffle<int>(first);
        new RandomStream(99).Shuffle<int>(second);
        Assert.Equal(first, second);
        Assert.Equal(Enumerable.Range(0, 100), first.Order());
        Assert.NotEqual(Enumerable.Range(0, 100).ToArray(), first);
        var random = new RandomStream(4);
        random.Shuffle<int>(Span<int>.Empty);
        random.Shuffle<int>(new[] { 3 });
        Assert.Equal(4UL, random.CaptureState());
    }

    [Fact]
    public void ChoiceSelectsAnExistingElementAndRejectsEmptyInput()
    {
        var random = new RandomStream(7);
        Assert.Equal("only", random.Choose<string>(new[] { "only" }));
        int[] choices = { 10, 20, 30 };
        for (int i = 0; i < 100; i++)
        {
            Assert.Contains(random.Choose<int>(choices), choices);
        }
        ulong saved = random.CaptureState();
        Assert.Throws<ArgumentException>(() => random.Choose<int>(ReadOnlySpan<int>.Empty));
        Assert.Equal(saved, random.CaptureState());
    }
}
