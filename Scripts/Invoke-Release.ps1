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

function Clear-PublishDirectory {
    param([Parameter(Mandatory)] [string] $LiteralPath)

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $LiteralPath))
    $expectedRoot = [System.IO.Path]::GetFullPath($repositoryRoot) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($expectedRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not $fullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar + 'publish', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clear an unexpected publish directory: $fullPath"
    }
    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
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
    'KifuwarabeGo2026.InstallerGui\KifuwarabeGo2026.InstallerGui.csproj',
    'KifuwarabeGo2026.InstallerGui.Platform\KifuwarabeGo2026.InstallerGui.Platform.csproj',
    'KifuwarabeGo2026.InstallerGui.Presentation\KifuwarabeGo2026.InstallerGui.Presentation.csproj',
    'KifuwarabeGo2026.InstallerEngine\KifuwarabeGo2026.InstallerEngine.csproj',
    'KifuwarabeGo2026.InstallerEngine.Platform\KifuwarabeGo2026.InstallerEngine.Platform.csproj',
    'KifuwarabeGo2026.InstallerEngine.JsonLines\KifuwarabeGo2026.InstallerEngine.JsonLines.csproj',
    'KifuwarabeGo2026.InstallerEngine.JsonLinesHost\KifuwarabeGo2026.InstallerEngine.JsonLinesHost.csproj',
    'KifuwarabeGo2026.LobbyEngine.JsonLines\KifuwarabeGo2026.LobbyEngine.JsonLines.csproj',
    'KifuwarabeGo2026.LobbyEngine.JsonLinesHost\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.csproj',
    'KifuwarabeGo2026.PlayRoomGui.JsonLines\KifuwarabeGo2026.PlayRoomGui.JsonLines.csproj',
    'KifuwarabeGo2026.Reference.MatchRunner.Go\KifuwarabeGo2026.Reference.MatchRunner.Go.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.csproj',
    'KifuwarabeGo2026.PlayRoomEngine.JsonLines\KifuwarabeGo2026.PlayRoomEngine.JsonLines.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.csproj',
    'KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.csproj',
    'KifuwarabeGo2026.PlayRoomEngine.Conformance\KifuwarabeGo2026.PlayRoomEngine.Conformance.csproj',
    'Samples\External.PlayRoomEngine.Counter\External.PlayRoomEngine.Counter.csproj',
    'KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj',
    'KifuwarabeGo2026.GameOasis.Gui\KifuwarabeGo2026.GameOasis.Gui.csproj',
    'KifuwarabeGo2026.FormalAdapter.Cgos\KifuwarabeGo2026.FormalAdapter.Cgos.csproj',
    'KifuwarabeGo2026.FormalAdapter.Gtp\KifuwarabeGo2026.FormalAdapter.Gtp.csproj',
    'KifuwarabeGo2026.FormalAdapter.Sgf\KifuwarabeGo2026.FormalAdapter.Sgf.csproj',
    'KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host.csproj',
    'KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.csproj',
    'KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine\KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine.csproj',
    'KifuwarabeGo2026.Reference.PlayerEngine\KifuwarabeGo2026.Reference.PlayerEngine.csproj',
    'KifuwarabeGo2026.Reference.PlayDomain.Go\KifuwarabeGo2026.Reference.PlayDomain.Go.csproj',
    'KifuwarabeGo2026.StationeryUI\KifuwarabeGo2026.StationeryUI.csproj',
    'KifuwarabeGo2026.Reference.Communication.Cgos.Host\KifuwarabeGo2026.Reference.Communication.Cgos.Host.csproj'
)
$versionProjects | ForEach-Object { Assert-ProjectVersion -ProjectPath $_ }

if ([string]::IsNullOrWhiteSpace($ReleaseNotes)) {
    # Keep the script compatible with Windows PowerShell 5.1, which can decode a
    # UTF-8 script without BOM using the active ANSI code page. Avoid non-ASCII
    # path literals and discover the uniquely named release notes instead.
    $releaseNoteMatches = @(Get-ChildItem -LiteralPath 'Docs' -Recurse -File -Filter "RELEASE_NOTES_v$Version.md")
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

$launcherPublish = 'KifuwarabeGo2026.InstallerGui\bin\Release\net8.0\win-x64\publish'
$guiPublish = 'KifuwarabeGo2026.GameOasis.Gui.Windows\bin\Release\net8.0-windows\win-x64\publish'
$enginePublish = 'KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host\bin\Release\net8.0\win-x64\publish'

if (-not $SkipBuild) {
    Invoke-CheckedCommand -Command dotnet -Arguments @('build', 'KifuwarabeGo2026.slnx', '-c', 'Release')

    if (-not $SkipSmokeTests) {
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.InstallerEngine\KifuwarabeGo2026.Tests.InstallerEngine.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.GameOasis.Gui.Portability\KifuwarabeGo2026.Tests.GameOasis.Gui.Portability.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.GameOasis.Gui.Windows\KifuwarabeGo2026.Tests.GameOasis.Gui.Windows.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.PlayRoomGui.JsonLines\KifuwarabeGo2026.Tests.PlayRoomGui.JsonLines.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.Reference.MatchRunner.Go\KifuwarabeGo2026.Tests.Reference.MatchRunner.Go.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.PlayRoomEngine.JsonLines\KifuwarabeGo2026.Tests.PlayRoomEngine.JsonLines.csproj', '-c', 'Release', '--no-build')
        Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.PlayRoomEngine.Conformance\KifuwarabeGo2026.PlayRoomEngine.Conformance.csproj', '-c', 'Release', '--no-build')
    }

    Clear-PublishDirectory -LiteralPath $launcherPublish
    Clear-PublishDirectory -LiteralPath $guiPublish
    Clear-PublishDirectory -LiteralPath $enginePublish

    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.InstallerGui\KifuwarabeGo2026.InstallerGui.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.InstallerEngine.JsonLinesHost\KifuwarabeGo2026.InstallerEngine.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', $launcherPublish)
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.GameOasis.Gui.Windows\KifuwarabeGo2026.GameOasis.Gui.Windows.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', $guiPublish)
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.LobbyEngine.JsonLinesHost\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\LobbyEngine")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\PlayRoom\BoardEditor")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\PlayRoom\Review")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\PlayRoom\Match")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\PlaySpace")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\PlaySpace")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.PlayRoomEngine.Conformance\KifuwarabeGo2026.PlayRoomEngine.Conformance.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\Conformance\ProtocolS")
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'Samples\External.PlayRoomEngine.Counter\External.PlayRoomEngine.Counter.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false', '-o', "$guiPublish\Tools\Conformance\ProtocolS\Samples\Counter")
    Copy-Item -LiteralPath 'Conformance\ProtocolS\v1' -Destination "$guiPublish\Tools\Conformance\ProtocolS\Vectors" -Recurse
    Invoke-CheckedCommand -Command dotnet -Arguments @('publish', 'KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.Host.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'false')

    # v3 launchers start KifuwarabeGo2026.Gui.exe. Keep that public entry point
    # as an alias of the v4 Windows host throughout the v4.x.x transition.
    Copy-Item -LiteralPath "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.Windows.exe" -Destination "$guiPublish\KifuwarabeGo2026.Gui.exe"
    Copy-Item -LiteralPath "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.Windows.deps.json" -Destination "$guiPublish\KifuwarabeGo2026.Gui.deps.json"
    Copy-Item -LiteralPath "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.Windows.runtimeconfig.json" -Destination "$guiPublish\KifuwarabeGo2026.Gui.runtimeconfig.json"
}

# Old clients validate the Launcher-named files and request the Launcher ZIP.
# The alias apphost still loads Installer.dll, included beside both entry points.
foreach ($extension in @('exe', 'dll', 'deps.json', 'runtimeconfig.json')) {
    Copy-Item -LiteralPath "$launcherPublish\KifuwarabeGo2026.Installer.$extension" -Destination "$launcherPublish\KifuwarabeGo2026.Launcher.$extension" -Force
}

Assert-FileExists -LiteralPath @(
    "$launcherPublish\KifuwarabeGo2026.Installer.exe",
    "$launcherPublish\KifuwarabeGo2026.Installer.dll",
    "$launcherPublish\KifuwarabeGo2026.Installer.deps.json",
    "$launcherPublish\KifuwarabeGo2026.Installer.runtimeconfig.json",
    "$launcherPublish\KifuwarabeGo2026.Launcher.exe",
    "$launcherPublish\KifuwarabeGo2026.Launcher.dll",
    "$launcherPublish\KifuwarabeGo2026.Launcher.deps.json",
    "$launcherPublish\KifuwarabeGo2026.Launcher.runtimeconfig.json",
    "$launcherPublish\KifuwarabeGo2026.InstallerEngine.JsonLinesHost.dll",
    "$launcherPublish\KifuwarabeGo2026.InstallerEngine.JsonLinesHost.deps.json",
    "$launcherPublish\KifuwarabeGo2026.InstallerEngine.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.Windows.exe",
    "$guiPublish\KifuwarabeGo2026.Gui.exe",
    "$guiPublish\KifuwarabeGo2026.Gui.deps.json",
    "$guiPublish\KifuwarabeGo2026.Gui.runtimeconfig.json",
    "$guiPublish\KifuwarabeGo2026.GameOasis.Gui.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.exe",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.deps.json",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.runtimeconfig.json",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Common.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.MatchRunner.Go.dll",
    "$guiPublish\KifuwarabeGo2026.PlayRoomGui.JsonLines.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame.dll",
    "$guiPublish\KifuwarabeGo2026.FormalAdapter.Cgos.dll",
    "$guiPublish\KifuwarabeGo2026.FormalAdapter.Gtp.dll",
    "$guiPublish\KifuwarabeGo2026.FormalAdapter.Sgf.dll",
    "$guiPublish\KifuwarabeGo2026.StationeryUI.dll",
    "$guiPublish\KifuwarabeGo2026.Reference.PlayDomain.Go.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Reference.Communication.Cgos.Host.exe",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Reference.Communication.Cgos.Host.dll",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Reference.Communication.Cgos.Host.deps.json",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.Reference.Communication.Cgos.Host.runtimeconfig.json",
    "$guiPublish\Tools\Cgos\KifuwarabeGo2026.FormalAdapter.Cgos.dll",
    "$guiPublish\Tools\LobbyEngine\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.exe",
    "$guiPublish\Tools\LobbyEngine\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.dll",
    "$guiPublish\Tools\LobbyEngine\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.deps.json",
    "$guiPublish\Tools\LobbyEngine\KifuwarabeGo2026.LobbyEngine.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlayRoom\BoardEditor\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.exe",
    "$guiPublish\Tools\PlayRoom\BoardEditor\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.dll",
    "$guiPublish\Tools\PlayRoom\BoardEditor\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.deps.json",
    "$guiPublish\Tools\PlayRoom\BoardEditor\KifuwarabeGo2026.Reference.PlayRoomGui.BoardEditor.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlayRoom\Review\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.exe",
    "$guiPublish\Tools\PlayRoom\Review\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.dll",
    "$guiPublish\Tools\PlayRoom\Review\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.deps.json",
    "$guiPublish\Tools\PlayRoom\Review\KifuwarabeGo2026.Reference.PlayRoomGui.Review.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlayRoom\Match\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.exe",
    "$guiPublish\Tools\PlayRoom\Match\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.dll",
    "$guiPublish\Tools\PlayRoom\Match\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.deps.json",
    "$guiPublish\Tools\PlayRoom\Match\KifuwarabeGo2026.Reference.PlayRoomGui.Match.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.exe",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.dll",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.deps.json",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Go.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.exe",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.dll",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.deps.json",
    "$guiPublish\Tools\PlaySpace\KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki.JsonLinesHost.runtimeconfig.json",
    "$guiPublish\Tools\PlaySpace\go.playspace.json",
    "$guiPublish\Tools\PlaySpace\ponnuki.playspace.json",
    "$guiPublish\Tools\Conformance\ProtocolS\KifuwarabeGo2026.PlayRoomEngine.Conformance.exe",
    "$guiPublish\Tools\Conformance\ProtocolS\KifuwarabeGo2026.PlayRoomEngine.Conformance.dll",
    "$guiPublish\Tools\Conformance\ProtocolS\Vectors\conformance-vector.schema.json",
    "$guiPublish\Tools\Conformance\ProtocolS\Vectors\playspace-host-manifest.schema.json",
    "$guiPublish\Tools\Conformance\ProtocolS\Samples\Counter\External.PlayRoomEngine.Counter.dll",
    "$guiPublish\Tools\Conformance\ProtocolS\Samples\Counter\counter.playspace.json",
    "$enginePublish\KifuwarabeGo2026.Engine.exe",
    "$enginePublish\KifuwarabeGo2026.Reference.PlayDomain.Go.dll",
    "$enginePublish\KifuwarabeGo2026.FormalAdapter.Gtp.dll",
    "$enginePublish\KifuwarabeGo2026.Reference.PlayerEngine.dll",
    "$enginePublish\KifuwarabeGo2026.Reference.PlayerEngine.Go.Gtp.dll"
)

if (-not $SkipSmokeTests) {
    & "$PSScriptRoot\Test-PublishedGoPlayRoom.ps1" -PublishDirectory $guiPublish
    Invoke-CheckedCommand -Command dotnet -Arguments @('run', '--project', 'KifuwarabeGo2026.Tests.Reference.MatchRunner.Go\KifuwarabeGo2026.Tests.Reference.MatchRunner.Go.csproj', '-c', 'Release', '--no-build', '--', '--real-host', "$guiPublish\KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.dll")
}

$uploads = Join-Path $repositoryRoot 'Uploads'
if (-not (Test-Path -LiteralPath $uploads -PathType Container)) {
    New-Item -ItemType Directory -Path $uploads | Out-Null
}

$assets = @()
$packages = @(
    @{ Name = 'Installer'; Source = $launcherPublish },
    @{ Name = 'Launcher'; Source = $launcherPublish }, # Compatibility for existing update clients.
    @{ Name = 'Gui'; Source = $guiPublish },
    @{ Name = 'GameOasis.Gui'; Source = $guiPublish },
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
