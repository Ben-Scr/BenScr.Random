# Algorithm and reproducibility

`RandomStream.AlgorithmId = xorshift64star-sampling-v1` identifies both the transition and public transforms.

For nonzero 64-bit state `s`, apply `s ^= s >> 12`, `s ^= s << 25`, `s ^= s >> 27`, with left shifts truncated to 64 bits. The raw output is `s * 2685821657736338717 mod 2^64`; the state keeps the unmultiplied value. Zero input seeds map to `0x9E3779B97F4A7C15`. This transition is inherited from RandomCS, with the xorshift* analysis described by [Sebastiano Vigna](https://vigna.di.unimi.it/ftp/papers/xorshift.pdf).

A full state cycle has `M = 2^64 - 1` nonzero raw outputs. Bounded sampling maps `candidate = raw - 1`, giving `[0,M)`. For bound `b`, accept only candidates below `L = M - (M mod b)`, and return `candidate mod b`. The accepted set consists of exactly `L/b` complete residue groups. This avoids introducing modulo bias, including the missing-zero issue that would arise from treating the source as all 2^64 bit patterns. It does not make correlated deterministic draws independent or cryptographically random.

Unit doubles take the upper 53 raw bits and multiply by `2^-53`; singles take the upper 24 bits and multiply by `2^-24`. Floating scaling uses the finite difference when available, otherwise a convex combination. The excluded upper endpoint is clamped to its predecessor after rounding. Byte draws use the bound 256 and byte-buffer fills perform one bounded draw per element. Boolean is `NextUInt64(2) == 0`. Strings sample alphabet indices in increasing output position. Shuffle processes indices from `Length-1` down to 1 with an inclusive choice of swap index from 0..i.

The seed, algorithm identifier and exact sequence of methods/arguments define replay. Persist `CaptureState()` plus the identifier. Arbitrary restored nonzero states are accepted; callers must reject mismatched stored algorithm identifiers themselves. No alternate algorithm is silently selected in this version. Integer output and unit floating output are specified bitwise; scaled floating reproducibility is supported on the tested .NET 9/10 Windows/Linux x64 platforms, without an untested cross-architecture guarantee.

The raw-vector test values were independently calculated with arbitrary-precision integer arithmetic and explicit 64-bit reduction. Seed 1 starts with `5180492295206395165`, `12380297144915551517`, `13389498078930870103`. Tests also compare rejection boundaries with BigInteger division, enumerate every ticket in a weighted fixture and check continuation after forced rejection. No narrow statistical-frequency or timing gate is used.

`SecureRandom` delegates raw bytes and bounded integers to [.NET RandomNumberGenerator](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getint32). Its floating outputs use little-endian byte decoding and the same documented scaling. It has no reproducibility contract.
