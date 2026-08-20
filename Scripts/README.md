# Release scripts

`Invoke-Release.ps1` builds, verifies, packages, and optionally publishes the
Windows x64 release. Run it from any directory; it resolves the repository root
from its own location.

## Prepare release assets

Update the project versions and create the matching release notes first, then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\Invoke-Release.ps1 -Version 3.17.0
```

The explicit execution policy makes this work on Windows installations that
disable direct `.ps1` execution. It applies only to this PowerShell process and
does not change the machine or user policy.

This performs the Release build, three smoke tests, publishes Launcher/GUI/Engine,
and creates ZIP and SHA-256 files under `Uploads`. Existing assets are never
overwritten.

## Publish to GitHub

After reviewing the generated files, run the command again. Matching existing
assets are reused only after their SHA-256 values have been verified:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\Invoke-Release.ps1 -Version 3.17.0 -Publish -SkipBuild
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
