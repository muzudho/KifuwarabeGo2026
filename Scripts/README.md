# Release scripts

`Invoke-Release.ps1` builds, verifies, packages, and optionally publishes the
Windows x64 release. Run it from any directory; it resolves the repository root
from its own location.

## Prepare release assets

Update the project versions and create the matching release notes first, then run:

```powershell
.\Scripts\Invoke-Release.ps1 -Version 3.16.0
```

This performs the Release build, three smoke tests, publishes Launcher/GUI/Engine,
and creates ZIP and SHA-256 files under `Uploads`. Existing assets are never
overwritten.

## Publish to GitHub

After reviewing the generated files, run the command again. Matching existing
assets are reused only after their SHA-256 values have been verified:

```powershell
.\Scripts\Invoke-Release.ps1 -Version 3.16.0 -Publish -SkipBuild
```

Publishing requires GitHub CLI authentication, a clean working tree, and local
`HEAD` matching its upstream branch. PowerShell asks for confirmation before the
GitHub release is created. Add `-Confirm:$false` only when intentional.

Useful options:

- `-Draft` creates a draft GitHub release.
- `-Prerelease` marks it as a prerelease.
- `-ReleaseNotes <path>` uses another notes file.
- `-SkipBuild` reuses existing publish output.
- `-SkipSmokeTests` skips smoke tests but still builds and publishes.
- `-AllowDirty` permits a dirty working tree for local package preparation.
- `-WhatIf -Publish` checks the workflow without creating a GitHub release.

Authentication tokens are not stored in the script. Sign in separately with
`gh auth login`.
