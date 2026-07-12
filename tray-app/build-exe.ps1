# Module purpose: Provides build exe for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
[CmdletBinding()]
param(
    [string]$Python = "C:\Users\brunn\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSScriptRoot -Parent
$app = Join-Path -Path $PSScriptRoot -ChildPath "app.py"
$assetsDir = Join-Path -Path $PSScriptRoot -ChildPath "assets"
$iconPath = Join-Path -Path $assetsDir -ChildPath "postgres.png"
$distRoot = Join-Path -Path $root -ChildPath "dist\tray-app"
$workPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-tray-pyinstaller-{0}" -f ([guid]::NewGuid().ToString("N")))
$specPath = Join-Path -Path $root -ChildPath "build\tray-app-spec"
$exePath = Join-Path -Path $distRoot -ChildPath "DBNotifierTray.exe"

if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    & (Join-Path $PSScriptRoot "download-postgres-icon.ps1")
}

if (-not (Test-Path -LiteralPath $Python -PathType Leaf)) {
    throw "Python executable not found: $Python"
}

& $Python -m PyInstaller --version | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "PyInstaller is required. Install it explicitly in the selected Python environment before building."
}

if (Test-Path -LiteralPath $distRoot) {
    Remove-Item -LiteralPath $distRoot -Recurse -Force
}

New-Item -Path $distRoot -ItemType Directory -Force | Out-Null
New-Item -Path $specPath -ItemType Directory -Force | Out-Null

& $Python -m PyInstaller `
    --noconfirm `
    --clean `
    --onefile `
    --windowed `
    --name DBNotifierTray `
    --add-data "$assetsDir;assets" `
    --distpath $distRoot `
    --workpath $workPath `
    --specpath $specPath `
    $app

if ($LASTEXITCODE -ne 0) {
    throw "PyInstaller failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    throw "Expected executable was not created: $exePath"
}

Copy-Item -LiteralPath $iconPath -Destination (Join-Path -Path $distRoot -ChildPath "postgres.png") -Force
Write-Host ("Executable created: {0}" -f $exePath)
