[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string] $Version,

    [string] $ReleaseNotes,

    [switch] $Publish,
    [switch] $Draft,
    [switch] $Prerelease,
    [switch] $SkipBuild,
    [switch] $SkipSmokeTests,
    [switch] $UpdateVersion,
    [switch] $AllowDirty,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repositoryRoot

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(ValueFromRemainingArguments)] [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

function Assert-FileExists {
    param([Parameter(Mandatory)] [string[]] $LiteralPath)

    foreach ($path in $LiteralPath) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required file was not found: $path"
        }
    }
}

function Assert-ProjectVersion {
    param([Parameter(Mandatory)] [string] $ProjectPath)

    $actualVersion = (& dotnet msbuild $ProjectPath -nologo -getProperty:Version).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read the effective version from ${ProjectPath}"
    }
    if ($actualVersion -ne $Version) {
        throw "Version mismatch in ${ProjectPath}: expected $Version, found $actualVersion"
    }
}

function Update-CentralVersion {
    $propsPath = Join-Path $repositoryRoot 'Directory.Build.props'
    $numericVersion = ($Version -split '-', 2)[0]
    $content = Get-Content -Raw -LiteralPath $propsPath
    $content = [regex]::Replace($content, '<Version>[^<]+</Version>', "<Version>$Version</Version>", 1)
    $content = [regex]::Replace($content, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$numericVersion.0</AssemblyVersion>", 1)
    $content = [regex]::Replace($content, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$numericVersion.0</FileVersion>", 1)
    Set-Content -LiteralPath $propsPath -Value $content -Encoding utf8
    Write-Host "Updated central project version to $Version"
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'git was not found on PATH.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found on PATH.'
}

if ($UpdateVersion) { Update-CentralVersion }

$status = @(git status --short)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the Git working tree.'
}
if ($status.Count -gt 0 -and -not $AllowDirty) {
    throw "The Git working tree is not clean. Commit or stash changes, or use -AllowDirty intentionally.`n$($status -join "`n")"
}

$versionProjects = @(
    'KifuwarabeGo2026.Launcher\KifuwarabeGo2026.Launcher.csproj',
    'KifuwarabeGo2026.Launcher.Core\KifuwarabeGo2026.Launcher.Core.csproj',
    'KifuwarabeGo2026.Launcher.Platform\KifuwarabeGo2026.Launcher.Platform.csproj',
    'KifuwarabeGo2026.Launcher.Presentation\KifuwarabeGo2026.Launcher.Presentation.csproj',
    'KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj',
    'KifuwarabeGo2026.GameOasis.Gui\KifuwarabeGo2026.GameOasis.Gui.csproj',
    'KifuwarabeGo2026.Reference.Communication.Gtp.Host\KifuwarabeGo2026.Reference.Communication.Gtp.Host.csproj',
    'KifuwarabeGo2026.Reference.Communication.Gtp\KifuwarabeGo2026.Reference.Communication.Gtp.csproj',
    'KifuwarabeGo2026.Reference.PlayerEngine\KifuwarabeGo2026.Reference.PlayerEngine.csproj',
    'KifuwarabeGo2026.Reference.PlaySpace.Go.Foundation\KifuwarabeGo2026.Reference.PlaySpace.Go.Foundation.csproj',
    'KifuwarabeGo2026.StationeryUI\KifuwarabeGo2026.StationeryUI.csproj',
    'KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos\KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos.csproj'
)
$versionProjects | ForEach-Object { Assert-ProjectVersion -ProjectPath $_ }

if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    # Keep the script compatible with Windows PowerShell 5.1, which can decode a
    # UTF-8 script without BOM using the active ANSI code page. Avoid non-ASCII
    # path literals and discover the uniquely named release notes instead.
    $releaseNoteMatches = @(Get-ChildItem -LiteralPath 'KifuwarabeGo2026.GameOasis.Gui\Docs' -Recurse -File -Filter "RELEASE_NOTES_v$Version.md")
    if ($releaseNoteMatches.Count -ne 1) {
        throw "Expected exactly one release notes file for v$Version, found $($releaseNoteMatches.Count)."
    }
    $ReleaseNotes = $releaseNoteMatches[0].FullName
}
Assert-FileExists -LiteralPath $ReleaseNotes
$releaseNotesText = Get-Content -Raw -LiteralPath $ReleaseNotes
if ($releaseNotesText -notmatch "(?m)^# Kifuwarabe Go 2026 v$([regex]::Escape($Version))\s*$") {
    throw "Release notes do not contain the expected v$Version title: $ReleaseNotes"
}

$launcherPublish = 'KifuwarabeGo2026.Launcher\bin\Release\net8.0\win-x64\publish'
$guiPublish = 'KifuwarabeGo2026.GameOasis.Gui.Windows\bin\Release\net8.0-windows\win-x64\publish'
$enginePublish = 'KifuwarabeGo2026.Reference.Communication.Gtp.Host\bin\Release\net8.0\win-x64\publish'

if (-not $SkipBuild) {
    Invoke-CheckedCommand -Command dotnet -Arguments @('build', 'KifuwarabeGo2026.slnx', '-c', 'Release')

    if (-not $SkipSmokeTests) {
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.LauncherSmoke\KifuwarabeGo2026.LauncherSmoke.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Gui.PortabilitySmoke\KifuwarabeGo2026.Gui.PortabilitySmoke.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.GameOasis.Gui.Windows\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.csproj', '-c', 'Release', '--no-build')
    }

    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Launcher\KifuwarabeGo2026.Launcher.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.Communication.Gtp.Host\KifuwarabeGo2026.Reference.Communication.Gtp.Host.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
}

Assert-FileExists -LiteralPath @(
    "$launcherPublish\KifuwarabeGo2026.Launcher.exe",
    "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.exe",
    "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.dll",
    "$guiPublish\KifuwarabeGo2026.StationeryUI.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlaySpace.Go.Foundation.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos.exe",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos.deps.json",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.GameOasis.Gui.Communication.Cgos.runtimeconfig.json",
    "$enginePublish\KifuwarabeGo2026.Engine.exe",
    "$enginePublish\KifuwarabeGo2026.Reference.PlaySpace.Go.Foundation.dll",
    "$enginePublish\KifuwarabeGo2026.Reference.PlayerEngine.dll",
    "$enginePublish\KifuwarabeGo2026.Reference.Communication.Gtp.dll"
)

$uploads = Join-Path $repositoryRoot 'Uploads'
if (-not (Test-Path -LiteralPath $uploads -PathType Container)) {
    New-Item -ItemType Directory -Path $uploads | Out-Null
}

$assets = @()
$packages = @(
    @{ Name = 'Launcher'; Source = $launcherPublish },
    @{ Name = 'Gui'; Source = $guiPublish },
    @{ Name = 'Engine'; Source = $enginePublish }
)

foreach ($package in $packages) {
    $zipPath = Join-Path $uploads "KifuwarabeGo2026.$($package.Name)-v$Version-win-x64.zip"
    $hashPath = "$zipPath.sha256"
    $zipExists = Test-Path -LiteralPath $zipPath -PathType Leaf
    $hashExists = Test-Path -LiteralPath $hashPath -PathType Leaf
    if ($zipExists -or $hashExists) {
        if (-not ($Publish -and $zipExists -and $hashExists)) {
            throw "A release asset already exists; refusing to overwrite it: $zipPath"
        }
        $expectedHash = ((Get-Content -Raw -LiteralPath $hashPath).Trim() -split '\s+')[0]
        $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash
        if ($expectedHash -ne $actualHash) {
            throw "Existing SHA-256 file does not match its ZIP: $zipPath"
        }
        $assets += $zipPath, $hashPath
        continue
    }

    Compress-Archive -Path "$($package.Source)\*" -DestinationPath $zipPath -CompressionLevel Optimal
    $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath).Hash
    $zipName = Split-Path -Leaf $zipPath
    Set-Content -LiteralPath $hashPath -Value "$hash  $zipName" -Encoding ascii
    $assets += $zipPath, $hashPath
}

Write-Host "Prepared release v$Version"
Get-Item -LiteralPath $assets | Select-Object Name, Length, LastWriteTime
Get-FileHash -Algorithm SHA256 -LiteralPath ($assets | Where-Object { $_ -like '*.zip' })

if (-not $Publish) {
    Write-Host "Assets are ready in $uploads. Re-run with -Publish to create the GitHub release."
    return
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) was not found on PATH.'
}
Invoke-CheckedCommand -Command gh -Arguments @('auth', 'status')

$branch = (git branch --show-current).Trim()
$upstream = (git rev-parse --abbrev-ref --symbolic-full-name '@{u}').Trim()
Invoke-CheckedCommand -Command git -Arguments @('fetch', '--quiet', 'origin', $branch)
$localCommit = (git rev-parse HEAD).Trim()
$upstreamCommit = (git rev-parse $upstream).Trim()
if ($localCommit -ne $upstreamCommit) {
    throw "HEAD ($localCommit) does not match $upstream ($upstreamCommit). Push or synchronize before publishing."
}

$tag = "v$Version"
git rev-parse --verify --quiet "refs/tags/$tag" | Out-Null
if ($LASTEXITCODE -eq 0) {
    throw "Local tag already exists: $tag"
}
$remoteTag = @(git ls-remote --tags origin "refs/tags/$tag" "refs/tags/$tag^{}")
if ($LASTEXITCODE -ne 0) {
    throw "Unable to inspect remote tag: $tag"
}
if ($remoteTag.Count -gt 0) {
    throw "Remote tag already exists: $tag"
}
$previousErrorActionPreference = $ErrorActionPreference
try {
    # A missing release is the expected result. Windows PowerShell 5.1 turns
    # native stderr into an ErrorRecord when ErrorActionPreference is Stop.
    $ErrorActionPreference = 'SilentlyContinue'
    gh release view $tag --repo muzudho/KifuwarabeGo2026 2>$null | Out-Null
    $releaseExists = $LASTEXITCODE -eq 0
}
finally {
    $ErrorActionPreference = $previousErrorActionPreference
}
if ($releaseExists) {
    throw "GitHub release already exists: $tag"
}

if ($Force -or $PSCmdlet.ShouldProcess("GitHub release $tag at $localCommit", 'Create and publish release')) {
    $releaseArguments = @(
        'release', 'create', $tag
    ) + $assets + @(
        '--repo', 'muzudho/KifuwarabeGo2026',
        '--target', $localCommit,
        '--title', "Kifuwarabe Go 2026 $tag",
        '--notes-file', $ReleaseNotes
    )
    if ($Draft) { $releaseArguments += '--draft' }
    if ($Prerelease) { $releaseArguments += '--prerelease' }
    Invoke-CheckedCommand -Command gh -Arguments $releaseArguments
}
