# Run this script once to create a self-signed signing certificate.
# Must be run as Administrator.

$subject = "CN=wowfood"
$certStorePath = "Cert:\CurrentUser\My"

Write-Host "Creating self-signed certificate for MSIX signing..."

$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $subject `
    -KeyUsage DigitalSignature `
    -FriendlyName "SonglistSpinner" `
    -CertStoreLocation $certStorePath `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
    -NotAfter (Get-Date).AddYears(10)

$thumbprint = $cert.Thumbprint
Write-Host ""
Write-Host "Certificate thumbprint: $thumbprint"

# Patch the thumbprint into SonglistSpinner.csproj
$csprojPath = Join-Path $PSScriptRoot "SonglistSpinner.csproj"
$xml = [xml](Get-Content $csprojPath)
$node = $xml.SelectSingleNode("//PackageCertificateThumbprint")
if ($node) {
    $node.InnerText = $thumbprint
    $xml.Save($csprojPath)
    Write-Host "Updated SonglistSpinner.csproj with thumbprint."
}

# Export the public .cer for distribution to target machines
$cerPath = Join-Path $PSScriptRoot "SonglistSpinner.cer"
Export-Certificate -Cert "$certStorePath\$thumbprint" -FilePath $cerPath | Out-Null
Write-Host "Exported public certificate to: $cerPath"
Write-Host ""
Write-Host "DONE. You can now build/publish the MSIX."
Write-Host ""
Write-Host "To install on other machines, run as Administrator on the target:"
Write-Host "  Import-Certificate -FilePath SonglistSpinner.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
