using System;

namespace BenScr.Random;

/// <summary>An immutable table for exact nonnegative integer weights, sampled with replacement.</summary>
/// <remarks>Weights are copied. Zero-weight entries are never chosen. The table can be shared across threads,
/// but every supplied RandomStream must be synchronized separately. Floating-point weights are deliberately
/// not accepted, avoiding ambiguous rounding of small probabilities.</remarks>
public sealed class WeightedTable
{
    private readonly ulong[] cumulative;

    /// <summary>Copies weights and prepares their cumulative sum.</summary>
    /// <exception cref="ArgumentException">Weights are empty or all zero.</exception>
    /// <exception cref="OverflowException">The sum exceeds UInt64.MaxValue.</exception>
    public WeightedTable(ReadOnlySpan<ulong> weights)
    {
        if (weights.IsEmpty)
        {
            throw new ArgumentException("At least one weight is required.", nameof(weights));
        }
        cumulative = new ulong[weights.Length];
        ulong total = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            total = checked(total + weights[i]);
            cumulative[i] = total;
        }
        if (total == 0)
        {
            throw new ArgumentException("At least one weight must be positive.", nameof(weights));
        }
    }

    /// <summary>The number of entries, including entries with zero weight.</summary>
    public int Count => cumulative.Length;

    /// <summary>The exact sum of all weights.</summary>
    public ulong TotalWeight => cumulative[^1];

    /// <summary>Selects an index using the supplied stream and this table's integer weight ratios.</summary>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    public int Sample(RandomStream random)
    {
        ArgumentNullException.ThrowIfNull(random);
        return FindIndex(random.NextUInt64(TotalWeight));
    }

    // Upper-bound search skips zero-weight runs, including those at either end.
    internal int FindIndex(ulong ticket)
    {
        int lower = 0;
        int upper = cumulative.Length - 1;
        while (lower < upper)
        {
            int middle = lower + (upper - lower) / 2;
            if (ticket < cumulative[middle])
            {
                upper = middle;
            }
            else
            {
                lower = middle + 1;
            }
        }
        return lower;
    }
}
