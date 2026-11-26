# Creates a new self-signed HTTPS certificate for the client's static IP:
#   deploy\cert\shopex.pfx  (import on the server, protected by the password you type)
#   deploy\cert\shopex.cer  (install on every user PC as a trusted root)
param(
    [string]$Ip = '82.129.144.54',
    [string]$OutDir = (Join-Path $PSScriptRoot 'cert'),
    [securestring]$Password
)
$ErrorActionPreference = 'Stop'
if (-not $Password) { $Password = Read-Host 'Choose a password for shopex.pfx (write it down)' -AsSecureString }
if ($Password.Length -eq 0) { throw 'Password cannot be empty' }
New-Item $OutDir -ItemType Directory -Force | Out-Null

# keep the old files instead of overwriting them
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
foreach ($name in 'shopex.pfx', 'shopex.cer') {
    $file = Join-Path $OutDir $name
    if (Test-Path $file) { Rename-Item $file "$stamp-old-$name" }
}

$cert = New-SelfSignedCertificate -Type SSLServerAuthentication -Subject "CN=$Ip" -FriendlyName 'ShopEx' `
    -TextExtension "2.5.29.17={text}IPAddress=$Ip&DNS=localhost" `
    -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy Exportable `
    -NotAfter (Get-Date).AddYears(5) -CertStoreLocation Cert:\CurrentUser\My
try {
    # Export-PfxCertificate defaults to TripleDES, which old Windows Server versions can import
    Export-PfxCertificate -Cert $cert -FilePath (Join-Path $OutDir 'shopex.pfx') -Password $Password | Out-Null
    Export-Certificate -Cert $cert -FilePath (Join-Path $OutDir 'shopex.cer') | Out-Null
}
finally {
    # the private key is only needed inside the pfx, not on this PC
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -DeleteKey
}
Write-Host "Done -> $OutDir" -ForegroundColor Green
