<#
.SYNOPSIS
	Create a pfx file from a self-signed certificate
#>

# Self elevating
$user = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (!$user.IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
	#Don't have time to get self elevating to the same working directory working...
	Write-Host "This must be run from an administrator power shell"
	#Write-Host $pwd
	#Start-Process PowerShell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -WorkingDirectory $pwd"
	exit
}

# Create the certificate
Write-Host "Output Directory: " $pwd
$name = Read-Host -Prompt "Certificate File Name"
$dn = Read-Host -Prompt "Domain name (e.g. cert.example.com)"
$pass = Read-Host -Prompt "Password" -AsSecureString
$cert = New-SelfSignedCertificate -CertStoreLocation Cert:\LocalMachine\My -DnsName $dn -KeyExportPolicy Exportable -KeyAlgorithm RSA -KeyLength 4096
$path = 'Cert:\LocalMachine\My\' + $cert.Thumbprint
Export-PfxCertificate -Cert $path -FilePath .\$name.pfx -Password $pass
#Export-Certificate -Cert $path -FilePath ($name+'.crt') -Type CERT

