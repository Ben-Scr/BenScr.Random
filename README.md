# BenScr.Random

Reproducible random streams, exact integer-weighted selection, shuffling and platform cryptographic randomness for .NET 9 and .NET 10. Created by Ben Schneider — https://github.com/Ben-Scr

```sh
dotnet add package BenScr.Random --version 1.0.0
```

```csharp
using BenScr.Random;

var random = new RandomStream(42);
int roll = random.NextInt32(1, 7);
ulong checkpoint = random.CaptureState();
string label = random.NextString(12, "abcdef0123456789");
random.RestoreState(checkpoint); // reproduces the same label on the next call

int[] deck = { 1, 2, 3, 4, 5 };
random.Shuffle<int>(deck);
var loot = new WeightedTable(new ulong[] { 70, 25, 5 });
int category = loot.Sample(random);

byte[] tokenBytes = new byte[32];
SecureRandom.FillBytes(tokenBytes);
```

## Contracts

- `RandomStream` is a mutable **non-cryptographic** xorshift64* generator. Use it for repeatable simulations and sampling. The algorithm identifier is `xorshift64star-sampling-v1`. Store it with a captured state when persisting a simulation. Each method's draw order and transformations are part of that version.
- Zero seeds map to `0x9E3779B97F4A7C15`; captured states are never zero. `RestoreState(0)` rejects invalid state. The parameterless constructor and `Reseed()` obtain seed bytes from the platform source, but output remains non-cryptographic.
- Raw `NextUInt64()` returns 1 through `ulong.MaxValue`. Bounded integers use rejection sampling that accounts for this missing zero, so complete residue groups have equal representation. A bounded draw can consume several raw values. Every range is lower-inclusive and upper-exclusive; bounds must be strictly ordered.
- Unit doubles use the upper 53 bits and singles the upper 24 bits. Scaled floats require finite bounds, support opposite-sign extremes, and clamp rounding away from the excluded upper endpoint. Floating outputs are quantized; they are not uniform over every representable floating-point value.
- `WeightedTable` copies unsigned integer weights and uses exact checked sums. Empty/all-zero weights and overflow are rejected. Zero weights are never selected, including trailing zeros. `Sample` draws with replacement; the returned index identifies the corresponding caller-owned item. Floating weights are intentionally unsupported: choose integer units suitable for your probabilities.
- Strings sample UTF-16 code units with replacement. The default alphabet is ASCII upper/lowercase letters and digits. Null selects that default; empty alphabets are rejected even for length zero. Duplicate alphabet entries retain their weighting. Length is limited to 1,048,576 code units. This is not Unicode grapheme generation or a password policy.
- `Shuffle` uses descending Fisher-Yates in place. `Choose` requires a nonempty span. Empty byte buffers, empty strings and shuffles shorter than two elements consume no state.
- `RandomStream` requires external synchronization. Immutable `WeightedTable` instances can be shared, with independently synchronized streams. `SecureRandom` shares no mutable library state and delegates entropy to `RandomNumberGenerator`; it offers no seed or replay facility.

The generator's short state and linear structure make it inappropriate for secrets and demanding statistical studies. No throughput, statistical certification, cross-language sequence, native AOT or trimming support is claimed.

## Build and verification

The repository pins .NET SDK 10.0.303; .NET 9 is also required to run its framework tests. Tests use xUnit v3 4.0.2 and Microsoft.Testing.Platform. No runtime dependencies are shipped.

```powershell
pwsh -File tools/Verify.ps1
# After publishing, install from nuget.org into a separate empty cache:
pwsh -File tools/VerifyConsumer.ps1 -Published
```

Verification builds and formats the library/tests/example, compares the public API baseline, runs contract tests on both frameworks, audits the exact package and symbols, and executes a consumer using only PackageReference and an empty cache. Windows and Linux CI repeat the checks. Fixed raw vectors, exact rejection boundaries, full integer spans, floating extremes, state replay and weighted ticket partitions provide deterministic regression coverage. Secure-source tests assert contracts without probabilistic frequency gates.

See [migration](https://github.com/Ben-Scr/BenScr.Random/blob/master/MIGRATION.md), [algorithm](https://github.com/Ben-Scr/BenScr.Random/blob/master/docs/ALGORITHM.md), [provenance](https://github.com/Ben-Scr/BenScr.Random/blob/master/docs/PROVENANCE.md) and [release procedure](https://github.com/Ben-Scr/BenScr.Random/blob/master/RELEASE.md).

