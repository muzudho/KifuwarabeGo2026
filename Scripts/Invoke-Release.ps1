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
    [switch] $AllowDirty
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

    [xml] $project = Get-Content -Raw -LiteralPath $ProjectPath
    $actualVersion = @(
        $project.Project.PropertyGroup |
            ForEach-Object { $_.Version } |
            Where-Object { $null -ne $_ } |
            Select-Object -First 1
    )[0]
    if ($actualVersion -ne $Version) {
        throw "Version mismatch in ${ProjectPath}: expected $Version, found $actualVersion"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'git was not found on PATH.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found on PATH.'
}

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
    'KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj',
    'KifuwarabeGo2026.Gui\KifuwarabeGo2026.Gui.Core.csproj',
    'KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj',
    'KifuwarabeGo2026.Shared\KifuwarabeGo2026.Shared.csproj',
    'KifuwarabeGo2026.StationeryUI\KifuwarabeGo2026.StationeryUI.csproj',
    'KifuwarabeGo2026.Gui.Communication.Cgos\KifuwarabeGo2026.Gui.Communication.Cgos.csproj'
)
$versionProjects | ForEach-Object { Assert-ProjectVersion -ProjectPath $_ }

if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    $ReleaseNotes = "KifuwarabeGo2026.Gui\Docs\開発\リリースノート\RELEASE_NOTES_v$Version.md"
}
Assert-FileExists -LiteralPath $ReleaseNotes

$launcherPublish = 'KifuwarabeGo2026.Launcher\bin\Release\net8.0\win-x64\publish'
$guiPublish = 'KifuwarabeGo2026.Gui.Windows\bin\Release\net8.0-windows\win-x64\publish'
$enginePublish = 'KifuwarabeGo2026.Engine\bin\Release\net8.0\win-x64\publish'

if (-not $SkipBuild) {
    Invoke-CheckedCommand -Command dotnet -Arguments @('build', 'KifuwarabeGo2026.slnx', '-c', 'Release')

    if (-not $SkipSmokeTests) {
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.LauncherSmoke\KifuwarabeGo2026.LauncherSmoke.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Gui.PortabilitySmoke\KifuwarabeGo2026.Gui.PortabilitySmoke.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Gui.WindowsSmoke\KifuwarabeGo2026.Gui.WindowsSmoke.csproj', '-c', 'Release', '--no-build')
    }

    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Launcher\KifuwarabeGo2026.Launcher.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Gui.Windows\KifuwarabeGo2026.Gui.Windows.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Engine\KifuwarabeGo2026.Engine.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
}

Assert-FileExists -LiteralPath @(
    "$launcherPublish\KifuwarabeGo2026.Launcher.exe",
    "$guiPublish\KifuwarabeGo2026.Gui.exe",
    "$guiPublish\KifuwarabeGo2026.Gui.Core.dll",
    "$guiPublish\KifuwarabeGo2026.StationeryUI.dll",
    "$guiPublish\KifuwarabeGo2026.Shared.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Gui.Communication.Cgos.exe",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Gui.Communication.Cgos.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Gui.Communication.Cgos.deps.json",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Gui.Communication.Cgos.runtimeconfig.json",
    "$enginePublish\KifuwarabeGo2026.Engine.exe",
    "$enginePublish\KifuwarabeGo2026.Shared.dll"
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
gh release view $tag --repo muzudho/KifuwarabeGo2026 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    throw "GitHub release already exists: $tag"
}

if ($PSCmdlet.ShouldProcess("GitHub release $tag at $localCommit", 'Create and publish release')) {
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
