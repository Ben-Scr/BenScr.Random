using System;
using System.Linq;
using BenScr.Random;

var random = new RandomStream(1);
if (random.NextUInt64() != 5180492295206395165UL)
{
    throw new InvalidOperationException("Raw reference vector mismatch.");
}
ulong checkpoint = random.CaptureState();
string first = random.NextString(32, "abc");
random.RestoreState(checkpoint);
if (random.NextString(32, "abc") != first)
{
    throw new InvalidOperationException("State restoration failed.");
}
int[] deck = Enumerable.Range(1, 52).ToArray();
random.Shuffle<int>(deck);
if (!deck.Order().SequenceEqual(Enumerable.Range(1, 52)))
{
    throw new InvalidOperationException("Shuffle changed the deck.");
}
var table = new WeightedTable(new ulong[] { 0, 5, 0 });
if (table.Sample(random) != 1 || random.NextInt32(4, 5) != 4)
{
    throw new InvalidOperationException("Bounded or weighted selection failed.");
}
byte[] secureBytes = new byte[16];
SecureRandom.FillBytes(secureBytes);
if (SecureRandom.NextString(8, "x") != "xxxxxxxx")
{
    throw new InvalidOperationException("Secure alphabet selection failed.");
}
Console.WriteLine("Raw vector, state replay, shuffle, bounded sampling, weighted table and platform source passed.");
