[CmdletBinding()]
param(
	[string]$Path,

	[string]$ReportDirectory,

	[string]$Signing,

	[string]$F5Result,

	[string]$Attempt,

	[datetime]$Since,

	[string]$FailureDetails,

	[Alias('?')]
	[switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
	Write-Host 'Usage: Save-SmartAppControlDiagnostic.ps1 -Path <output-directory> -ReportDirectory <directory> -Signing Enabled|Disabled -F5Result Succeeded|Blocked|NotRun -Attempt <name> -Since <datetime> [-FailureDetails <text>]'
	return
}

if ([string]::IsNullOrWhiteSpace($Path) -or [string]::IsNullOrWhiteSpace($ReportDirectory) -or [string]::IsNullOrWhiteSpace($Attempt)) {
	throw 'Specify non-empty -Path, -ReportDirectory, and -Attempt values.'
}

if ($Signing -notin 'Enabled', 'Disabled') {
	throw 'Specify -Signing as Enabled or Disabled.'
}

if ($F5Result -notin 'Succeeded', 'Blocked', 'NotRun') {
	throw 'Specify -F5Result as Succeeded, Blocked, or NotRun.'
}

if ($Since -eq [datetime]::MinValue) {
	throw 'Specify -Since as a date and time.'
}

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
New-Item -ItemType Directory -Path $ReportDirectory -Force | Out-Null

$files = Get-ChildItem -LiteralPath $resolvedPath -Recurse -File | Where-Object { $_.Extension -in '.exe', '.dll' } | ForEach-Object {
	$signature = Get-AuthenticodeSignature -FilePath $_.FullName
	[ordered]@{
		Path = $_.FullName
		Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
		AuthenticodeStatus = $signature.Status.ToString()
		AuthenticodeStatusMessage = $signature.StatusMessage
		SignerSubject = if ($null -eq $signature.SignerCertificate) { $null } else { $signature.SignerCertificate.Subject }
		SignerThumbprint = if ($null -eq $signature.SignerCertificate) { $null } else { $signature.SignerCertificate.Thumbprint }
	}
}

$sacState = $null
try {
	$sacPolicy = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name 'VerifiedAndReputablePolicyState' -ErrorAction Stop
	$sacState = $sacPolicy.VerifiedAndReputablePolicyState
}
catch {
	$sacState = "Unavailable: $($_.Exception.Message)"
}

$codeIntegrityEvents = @()
try {
	$codeIntegrityEvents = Get-WinEvent -FilterHashtable @{ LogName = 'Microsoft-Windows-CodeIntegrity/Operational'; Id = 3033, 3077; StartTime = $Since } -ErrorAction Stop | ForEach-Object {
		[ordered]@{
			TimeCreated = $_.TimeCreated.ToString('o')
			Id = $_.Id
			Level = $_.LevelDisplayName
			Message = $_.Message
		}
	}
}
catch {
	$codeIntegrityEvents = @([ordered]@{ RetrievalError = $_.Exception.Message })
}

$visualStudioProcesses = Get-Process -Name devenv -ErrorAction SilentlyContinue | ForEach-Object {
	[ordered]@{
		Id = $_.Id
		StartTime = $_.StartTime.ToString('o')
		Path = $_.Path
	}
}

$report = [ordered]@{
	CapturedAt = (Get-Date).ToString('o')
	Attempt = $Attempt
	Signing = $Signing
	F5Result = $F5Result
	FailureDetails = $FailureDetails
	Since = $Since.ToString('o')
	OutputPath = $resolvedPath
	OperatingSystem = [System.Environment]::OSVersion.VersionString
	VerifiedAndReputablePolicyState = $sacState
	VisualStudioProcesses = @($visualStudioProcesses)
	Files = @($files)
	CodeIntegrityEvents = @($codeIntegrityEvents)
}

$safeAttempt = $Attempt -replace '[^\p{L}\p{N}_.-]', '_'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reportPath = Join-Path $ReportDirectory "SmartAppControl-$safeAttempt-$timestamp.json"
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding utf8
Write-Host "Smart App Control diagnostic: saved $reportPath"
