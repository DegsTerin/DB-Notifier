[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

try {
    Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
}
catch {
}

$root = Split-Path -Path $PSScriptRoot -Parent
$distDir = Join-Path -Path $root -ChildPath "dist\desktop"
$sourceScript = Join-Path -Path $PSScriptRoot -ChildPath "DBNotifier.Desktop.ps1"
$xamlSource = Join-Path -Path $PSScriptRoot -ChildPath "App.xaml"
$mockSource = Join-Path -Path $PSScriptRoot -ChildPath "mock.instances.json"
$exePath = Join-Path -Path $distDir -ChildPath "DBNotifierDesktop.exe"

if (Test-Path -LiteralPath $distDir) {
    Remove-Item -LiteralPath $distDir -Recurse -Force
}

New-Item -Path $distDir -ItemType Directory -Force | Out-Null

$ps2exe = Get-Module -ListAvailable -Name ps2exe | Sort-Object Version -Descending | Select-Object -First 1
if (-not $ps2exe) {
    throw "The ps2exe module is required. Install it with: Install-Module ps2exe -Scope CurrentUser"
}

Import-Module $ps2exe -Force

Invoke-PS2EXE `
    -InputFile $sourceScript `
    -OutputFile $exePath `
    -NoConsole `
    -STA `
    -Title "DB-Notifier Desktop" `
    -Description "DB-Notifier desktop WPF preview" `
    -Product "DB-Notifier Desktop" `
    -Company "DegsTerin" `
    -Version "1.0.0" `
    -RequireAdmin:$false

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "ps2exe finished without creating the expected executable: $exePath"
}

Copy-Item -LiteralPath $xamlSource -Destination (Join-Path -Path $distDir -ChildPath "App.xaml") -Force
Copy-Item -LiteralPath $mockSource -Destination (Join-Path -Path $distDir -ChildPath "mock.instances.json") -Force

Write-Host ("Desktop executable created: {0}" -f $exePath)
