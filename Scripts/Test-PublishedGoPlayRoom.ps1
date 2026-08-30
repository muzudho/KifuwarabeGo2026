[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$publishRoot = [System.IO.Path]::GetFullPath($PublishDirectory)
$hostPath = Join-Path $publishRoot 'KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows.exe'
if (-not (Test-Path -LiteralPath $hostPath -PathType Leaf)) {
    throw "Published Go Play Room Host was not found: $hostPath"
}

$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("KifuwarabeGo2026-published-play-room-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
try {
    $requestPath = Join-Path $temporaryDirectory 'launch-request.json'
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    $request = @{
        version = 1
        requestId = 'published-layout-smoke'
        roomTypeId = 'match'
        gameId = 'io.github.muzudho.kifuwarabego2026.games.go'
        playSpaceTypeId = @{ value = 'io.github.muzudho.kifuwarabego2026.games.go' }
        configuration = @{
            mediaType = 'application/json'
            schemaId = 'io.github.muzudho.kifuwarabego2026.games.go.configuration.v1'
            content = '{"version":1,"boardSize":9,"komi":7.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[],"mainTimeMilliseconds":0}'
        }
        initialPosition = $null
        participants = @()
    }
    function Write-LaunchRequest {
        [System.IO.File]::WriteAllText($requestPath, ($request | ConvertTo-Json -Depth 8), $utf8WithoutBom)
    }

    Write-LaunchRequest

    function Invoke-HostContractSmoke {
        param(
            [int] $ExpectedExitCode,
            [string] $Mode = '--contract-smoke'
        )

        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $hostPath
        $startInfo.Arguments = "--launch-request `"$requestPath`" $Mode"
        $startInfo.WorkingDirectory = $publishRoot
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $process = [System.Diagnostics.Process]::Start($startInfo)
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne $ExpectedExitCode) {
            throw "Published Go Play Room Host exited with $($process.ExitCode), expected ${ExpectedExitCode}: $stderr"
        }
        return @{ Stdout = $stdout; Stderr = $stderr }
    }

    $first = Invoke-HostContractSmoke -ExpectedExitCode 0
    $ready = $first.Stdout | ConvertFrom-Json
    if (-not $ready.ready -or $ready.requestId -ne 'published-layout-smoke' -or $ready.code -ne 'ready') {
        throw "Published Go Play Room Host returned an invalid readiness notification: $($first.Stdout)"
    }

    $failed = Invoke-HostContractSmoke -ExpectedExitCode 5 -Mode '--contract-smoke-fail-after-ready'
    $failedReady = $failed.Stdout | ConvertFrom-Json
    if (-not $failedReady.ready -or $failed.Stderr -notmatch 'contract-smoke-failure-after-ready') {
        throw "Published Go Play Room Host did not preserve its post-readiness failure contract: $($failed.Stderr)"
    }

    $request.roomTypeId = 'review'
    Write-LaunchRequest
    $rejected = Invoke-HostContractSmoke -ExpectedExitCode 4
    if ($rejected.Stderr -notmatch 'unsupported-host-room-type') {
        throw "Published Go Play Room Host did not preserve its rejection diagnostic: $($rejected.Stderr)"
    }

    $request.roomTypeId = 'match'
    $request.requestId = 'published-layout-restart'
    Write-LaunchRequest
    $restart = Invoke-HostContractSmoke -ExpectedExitCode 0
    $restartReady = $restart.Stdout | ConvertFrom-Json
    if (-not $restartReady.ready -or $restartReady.requestId -ne 'published-layout-restart') {
        throw "Published Go Play Room Host could not restart from the published layout: $($restart.Stdout)"
    }

    Write-Host 'PASS: Published Go Play Room Host layout, readiness, normal exit, abnormal exit, rejection, and restart checks passed.'
}
finally {
    if (Test-Path -LiteralPath $temporaryDirectory) {
        Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force
    }
}
