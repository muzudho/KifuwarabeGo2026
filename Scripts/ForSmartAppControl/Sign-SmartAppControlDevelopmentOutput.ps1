[CmdletBinding()]
param(
	[string]$Path,

	[string]$CertificateThumbprint = $env:KIFUWARABEGO2026_SAC_SIGNING_CERTIFICATE_THUMBPRINT,

	[Alias('?')]
	[switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
	Write-Host 'Usage: Sign-SmartAppControlDevelopmentOutput.ps1 -Path <output-directory> [-CertificateThumbprint <thumbprint>]'
	Write-Host 'The certificate must be in Cert:\CurrentUser\My, have an accessible private key, and permit code signing.'
	return
}

if ([string]::IsNullOrWhiteSpace($Path)) {
	throw 'Specify -Path as the output directory containing the EXE or DLL files to sign.'
}

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
	throw 'Set KIFUWARABEGO2026_SAC_SIGNING_CERTIFICATE_THUMBPRINT to the thumbprint of a code-signing certificate in Cert:\CurrentUser\My.'
}

$certificateThumbprint = $CertificateThumbprint -replace '\s', ''
$certificate = Get-Item -Path "Cert:\CurrentUser\My\$certificateThumbprint" -ErrorAction Stop

if (-not $certificate.HasPrivateKey) {
	throw "The certificate '$certificateThumbprint' does not have an accessible private key."
}

$codeSigningOid = '1.3.6.1.5.5.7.3.3'
$hasCodeSigningUsage = $certificate.EnhancedKeyUsageList | Where-Object { $_.ObjectId -eq $codeSigningOid }
if (-not $hasCodeSigningUsage) {
	throw "The certificate '$certificateThumbprint' is not enabled for code signing."
}

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
$files = Get-ChildItem -LiteralPath $resolvedPath -Recurse -File | Where-Object { $_.Extension -in '.exe', '.dll' }

if ($files.Count -eq 0) {
	Write-Warning "No EXE or DLL files were found under '$resolvedPath'."
	exit 0
}

foreach ($file in $files) {
	$signature = Set-AuthenticodeSignature -FilePath $file.FullName -Certificate $certificate
	if ($signature.Status -ne 'Valid') {
		throw "Signing '$($file.FullName)' failed: $($signature.Status) $($signature.StatusMessage)"
	}

	Write-Host "Smart App Control diagnostic: signed $($file.FullName)."
}
