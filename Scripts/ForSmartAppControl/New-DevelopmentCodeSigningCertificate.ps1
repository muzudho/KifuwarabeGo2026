[CmdletBinding()]
param(
	[string]$Subject = 'CN=KifuwarabeGo2026 Smart App Control Development',

	[Alias('?')]
	[switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
	Write-Host 'Usage: New-DevelopmentCodeSigningCertificate.ps1 [-Subject <distinguished-name>]'
	Write-Host 'Creates a local self-signed code-signing certificate and trusts its public certificate for the current user.'
	Write-Host 'A self-signed certificate does not guarantee Smart App Control approval.'
	return
}

$certificate = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert |
	Where-Object { $_.Subject -eq $Subject -and $_.HasPrivateKey } |
	Sort-Object -Property NotAfter -Descending |
	Select-Object -First 1

if ($null -eq $certificate) {
	$certificateParameters = @{
		Type = 'CodeSigningCert'
		Subject = $Subject
		FriendlyName = 'KifuwarabeGo2026 Smart App Control Development'
		CertStoreLocation = 'Cert:\CurrentUser\My'
		KeyAlgorithm = 'RSA'
		KeyLength = 3072
		HashAlgorithm = 'SHA256'
		KeyExportPolicy = 'NonExportable'
		NotAfter = (Get-Date).AddYears(2)
	}
	$certificate = New-SelfSignedCertificate @certificateParameters
}

$certificateFile = Join-Path ([System.IO.Path]::GetTempPath()) "$($certificate.Thumbprint).cer"
try {
	Export-Certificate -Cert $certificate -FilePath $certificateFile -Force | Out-Null
	Import-Certificate -FilePath $certificateFile -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
	Import-Certificate -FilePath $certificateFile -CertStoreLocation Cert:\CurrentUser\TrustedPublisher | Out-Null
}
finally {
	Remove-Item -LiteralPath $certificateFile -Force -ErrorAction SilentlyContinue
}

Write-Host "Smart App Control diagnostic: development code-signing certificate thumbprint is $($certificate.Thumbprint)."
Write-Host 'Set KIFUWARABEGO2026_SAC_SIGNING_CERTIFICATE_THUMBPRINT to this thumbprint before building.'
