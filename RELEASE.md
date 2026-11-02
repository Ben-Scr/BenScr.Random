# Release procedure

1. Use a clean `master` checkout with the pinned SDK and both tested runtimes. Run `pwsh -File tools/Verify.ps1`. Its versioned output directory contains the exact nupkg, snupkg, test reports, API/package audit, empty-cache consumer evidence and release manifest.
2. Ensure Windows and Linux CI pass for the intended commit. Audit final archives, source mappings and the unchanged icon. Resolve `packages/BenScr.Random/1.0.0` to the same commit as the embedded nuspec repository field; tag only that checked commit.
3. Publish precisely the recorded `BenScr.Random.1.0.0.nupkg` and corresponding `.snupkg`. Do not rebuild and substitute unchecked files. Record the CI URL, tag and package hashes in the manifest.
4. After feed indexing, run `pwsh -File tools/VerifyConsumer.ps1 -ArtifactDirectory <verified-artifact-directory> -Published`. It restores from nuget.org into a fresh cache, executes both frameworks and compares installed payloads with the verified archive. Verify repository signing and public symbols separately, then inspect the gallery icon.
5. Create the GitHub release for the existing tag with exact artifacts and manifest. Report publication as verified only after the public consumer and required checks pass. Immutable tags and NuGet versions are never overwritten.

The verification script deliberately rejects dirty release checkouts. Its `-AllowDirty` option exists for development diagnosis only and marks output as `tested_local_dirty`; such artifacts are not releases.
