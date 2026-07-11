[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSCommandPath -Parent
$assets = Join-Path -Path $root -ChildPath "assets"
$target = Join-Path -Path $assets -ChildPath "postgres.png"
$source = "https://wiki.postgresql.org/images/9/9a/PostgreSQL_logo.3colors.540x557.png"

New-Item -Path $assets -ItemType Directory -Force | Out-Null

Invoke-WebRequest -Uri $source -OutFile $target

if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
    throw "PostgreSQL icon download failed: $target"
}

Write-Host ("Downloaded official PostgreSQL icon from PostgreSQL Wiki: {0}" -f $target)
