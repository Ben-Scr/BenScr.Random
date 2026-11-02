using System;

namespace BenScr.Random;

internal static class RandomRules
{
    internal const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    internal const int MaxStringLength = 1_048_576;

    internal static void ValidateString(int length, string alphabet)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, MaxStringLength);
        ArgumentException.ThrowIfNullOrEmpty(alphabet);
    }

    internal static void ValidateRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum))
        {
            throw new ArgumentOutOfRangeException(nameof(minimum), "The lower bound must be finite.");
        }
        if (!double.IsFinite(maximum) || minimum >= maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum), "The upper bound must be finite and greater than the lower bound.");
        }
    }

    internal static double Scale(double unit, double minimum, double maximum)
    {
        double difference = maximum - minimum;
        double value = double.IsFinite(difference)
            ? minimum + unit * difference
            : (1.0 - unit) * minimum + unit * maximum;
        return Math.Max(minimum, value >= maximum ? Math.BitDecrement(maximum) : value);
    }

    internal static float Scale(float unit, float minimum, float maximum)
    {
        float value = (float)((double)minimum + unit * ((double)maximum - minimum));
        return Math.Max(minimum, value >= maximum ? MathF.BitDecrement(maximum) : value);
    }

    // xorshift64* has 2^64 - 1 outputs: zero is absent. Translate its population
    // to [0, 2^64 - 1) before discarding an incomplete final residue group.
    internal static bool TryMap(ulong raw, ulong exclusiveMaximum, out ulong value)
    {
        ulong candidate = raw - 1;
        ulong limit = ulong.MaxValue - ulong.MaxValue % exclusiveMaximum;
        value = candidate % exclusiveMaximum;
        return candidate < limit;
    }
}
