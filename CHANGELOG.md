# Changelog

## 1.0.0

- Migrate the public RandomCS API to `BenScr.Random` for .NET 9/10, with explicit typed methods and generator ownership.
- Version xorshift64* sampling and expose current-state capture/restore.
- Correct modulo bias and full-width integer ranges using rejection over the generator's actual population.
- Add in-place shuffle, element choice, byte-buffer fills and immutable exact integer-weighted tables.
- Validate finite floating ranges and string lengths; handle extreme ranges and excluded upper endpoints.
- Retain secure sampling through stateless platform-backed methods.
- Add deterministic boundary/regression tests, portable symbols, API baseline, isolated package consumers and Windows/Linux verification.
