using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenScr.Random.Tests;

public sealed class WeightedTableTests
{
    [Fact]
    public void ExactTicketsHonorEveryWeightIncludingZeroRuns()
    {
        ulong[] weights = { 0, 2, 0, 3, 0, 5, 0 };
        var table = new WeightedTable(weights);
        Assert.Equal(7, table.Count);
        Assert.Equal(10UL, table.TotalWeight);
        Assert.Equal(new[] { 1, 1, 3, 3, 3, 5, 5, 5, 5, 5 }, Enumerable.Range(0, 10).Select(ticket => table.FindIndex((ulong)ticket)).ToArray());
        weights[1] = 1000;
        Assert.Equal(10UL, table.TotalWeight);
        Assert.Equal(3, table.FindIndex(2));
    }

    [Fact]
    public void SingleNonzeroWeightIsAlwaysChosen()
    {
        var table = new WeightedTable(new ulong[] { 0, 0, ulong.MaxValue, 0 });
        var random = new RandomStream(0);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(2, table.Sample(random));
        }
        Assert.Equal(ulong.MaxValue, table.TotalWeight);
    }

    [Fact]
    public void InvalidTablesFailExplicitly()
    {
        Assert.Throws<ArgumentException>(() => new WeightedTable(ReadOnlySpan<ulong>.Empty));
        Assert.Throws<ArgumentException>(() => new WeightedTable(new ulong[] { 0, 0 }));
        Assert.Throws<OverflowException>(() => new WeightedTable(new ulong[] { ulong.MaxValue, 1 }));
        Assert.Throws<ArgumentNullException>(() => new WeightedTable(new ulong[] { 1 }).Sample(null!));
    }

    [Fact]
    public void SamplingMatchesDirectTicketIntervals()
    {
        var table = new WeightedTable(new ulong[] { 1, 0, 4 });
        var random = new RandomStream(42);
        var reference = new RandomStream(42);
        for (int i = 0; i < 1000; i++)
        {
            int expected = reference.NextUInt64(5) == 0 ? 0 : 2;
            Assert.Equal(expected, table.Sample(random));
        }
    }

    [Fact]
    public void ImmutableTableCanBeSharedWithIndependentStreams()
    {
        var table = new WeightedTable(new ulong[] { 1, 3, 5 });
        var random = new RandomStream(5);
        int[] reference = Enumerable.Range(0, 64).Select(_ => table.Sample(random)).ToArray();
        Parallel.For(0, 16, _ =>
        {
            var local = new RandomStream(5);
            Assert.Equal(reference, Enumerable.Range(0, 64).Select(_ => table.Sample(local)).ToArray());
        });
    }
}
