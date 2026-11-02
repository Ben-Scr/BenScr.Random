# RandomCS migration

Source: public `Ben-Scr/RandomCS`, branch `master`, commit `8d2a40f680e26c1bf235d72ca59dede9c6e59738`; package `RandomCS` 1.0.2, `net9.0`. The new package ID is `BenScr.Random` 1.0.0, supporting `net9.0;net10.0`. This is an explicit API migration, not a binary-compatible replacement.

| Old public surface | Replacement |
| --- | --- |
| `BenScr.Random.RandomCS(ulong)` / `RandomCS()` | `BenScr.Random.RandomStream(ulong)` / `RandomStream()` |
| `SetSeed(ulong)` | `RandomStream.SetSeed(ulong)` |
| `RemoveSeed()` | `RandomStream.Reseed()` |
| `GetSeed()` | `RandomStream.CaptureState()`; this was already current state, not the initial seed |
| `NextBool()` | `NextBoolean()` |
| `NextByte()`, `NextByte(byte)`, `NextByte(byte,byte)` | Same overloads on `RandomStream` |
| `NextInt()`, `NextInt(int)`, `NextInt(int,int)` | `NextInt32()` and corresponding overloads |
| `NextDouble()`, `NextDouble(double)`, `NextDouble(double,double)` | Same overloads on `RandomStream` |
| `NextFloat()`, `NextFloat(float)`, `NextFloat(float,float)` | `NextSingle()` and corresponding overloads |
| `NextString(int=10,string?=null)` | `RandomStream.NextString(int=10,string?=null)`; named argument `charset` becomes `alphabet` |
| `Next<T>()` | Call the concrete method: int→NextInt32, byte→NextByte, float→NextSingle, double→NextDouble, bool→NextBoolean, string→NextString |
| `Next<T>(T,T)` | Call the corresponding typed int/byte/float/double overload; unsupported types no longer compile through a runtime dispatcher |
| `BenScr.Security.Cryptography.RandomSecure()` | Use static `BenScr.Random.SecureRandom`; no disposable RNG instance is retained |
| All `RandomSecure.NextBool/NextByte/NextInt/NextFloat/NextDouble/NextString` overloads | Same mapping as above, on `SecureRandom` |
| `RandomSecure.Next<T>()` / `Next<T>(T,T)` | Same concrete-method mapping, on `SecureRandom` |
| `RandomSecure.GenerateBytes(byte[])` | `SecureRandom.FillBytes(Span<byte>)`; arrays convert to spans |
| `RandomHandler.SetSeed/RemoveSeed/NextBool/NextByte/NextInt/NextFloat/NextDouble/NextString` | Keep an explicitly owned `RandomStream` and call the mappings above |
| `RandomHandler.Next<T>()` / `Next<T>(T,T)` | Explicitly owned stream and concrete methods above |
| `RandomHandler.Secure.NextBool/NextByte/NextInt/NextFloat/NextDouble/NextString/GenerateBytes` | Corresponding `SecureRandom` methods above |
| `RandomHandler.Secure.Next<T>()` / `Next<T>(T,T)` | Concrete `SecureRandom` methods above |

`RandomHandler` and its mutable process-wide generator are intentionally removed; ownership and synchronization are explicit. `Characters` was internal and its ASCII alphabet is preserved.

## Intentional behavior changes

- Raw xorshift64* state transition, zero-seed mapping and standalone unit double/single bit extraction preserve the source algorithm. All bounded integer, Boolean, byte, string and shuffle sequences use a new mapping and should be considered changed. Mixing changed methods also changes subsequent raw/floating outputs when rejection occurs.
- Old bounded byte/int sampling used modulo directly. Old full integer spans wider than `int.MaxValue` could not reach their upper half. New bounded draws cover the entire requested range and discard incomplete residue groups. `NextInt32()` now excludes `int.MaxValue`, consistently with the range API.
- Old floating ranges accepted reversed/equal/nonfinite bounds in some overloads and could overflow their subtraction. All bounds are now validated before drawing; finite extreme ranges are supported.
- Negative string lengths now throw instead of returning an empty string. Empty alphabets are consistently rejected, and excessive lengths fail before allocation. Duplicate characters continue to represent repeated choices.
- Auto-seeding now uses platform random bytes instead of combining clocks, GUID hash codes and a timer. It does not confer cryptographic security on the deterministic generator.
- Secure integer/byte mapping delegates to the platform's discard-and-retry implementation, replacing biased modulo arithmetic and fixing wide-range support.

The original tests' seed, range, zero-state, strings and generic-dispatch scenarios are mapped to typed regression tests. Probabilistic frequency tests are replaced by fixed vectors, exact ticket/rejection boundaries and invariant checks. New tests cover state restoration, extreme floats, weighted selection, shuffle ownership and platform source boundaries. The existing solution is renamed BenScr.Random.sln locally and ignored; builds and CI use explicit project files. Legacy icons remain historical assets, while packages embed only the common logo.


