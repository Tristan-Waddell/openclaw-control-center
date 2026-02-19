param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

if (-not (Test-Path $Path)) {
    throw "Artifact not found: $Path"
}

$signature = Get-AuthenticodeSignature -FilePath $Path
if ($signature.Status -ne 'Valid') {
    throw "Artifact signature invalid: $($signature.Status)"
}

Write-Host "Signature valid for $Path"
