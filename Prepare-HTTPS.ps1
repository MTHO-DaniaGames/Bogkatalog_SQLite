# Kør fra repoets mappe i Visual Studios Developer PowerShell.
# Eksporterer et lokalt udviklingscertifikat. Ændrer ikke Windows' tillid.
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
if (Test-Path -LiteralPath '.env') {
    Write-Host '.env findes allerede. Bevarer adgangskoderne.'
} else {
    function New-DemoPassword {
        $bytes = New-Object byte[] 24
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
        return [Convert]::ToBase64String($bytes)
    }
    $lines = @(('CERT_PASSWORD=' + (New-DemoPassword)),
               ('READER_PASSWORD=' + (New-DemoPassword)),
               ('EDITOR_PASSWORD=' + (New-DemoPassword)))
    [System.IO.File]::WriteAllLines((Join-Path $PSScriptRoot '.env'), $lines)
}
$certLine = Get-Content -LiteralPath '.env' | Where-Object { $_.StartsWith('CERT_PASSWORD=') } | Select-Object -First 1
if (-not $certLine) { throw 'CERT_PASSWORD mangler i .env.' }
$certPassword = $certLine.Substring('CERT_PASSWORD='.Length)
New-Item -ItemType Directory -Force -Path '.certs' | Out-Null
dotnet dev-certs https --export-path .certs/localhost.pfx --password $certPassword
if ($LASTEXITCODE -ne 0) { throw 'Certifikatet kunne ikke eksporteres.' }
Write-Host 'Klar. Kør nu: dotnet dev-certs https --trust'
Write-Host 'Windows kan bede om at godkende det lokale udviklingscertifikat.'
Write-Host 'Åbn .env i editoren for at se øvelsesadgangskoderne. Del ikke filen.'
