[CmdletBinding()]
param(
    [string]$Python = "C:\Users\brunn\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSScriptRoot -Parent
$app = Join-Path -Path $PSScriptRoot -ChildPath "app.py"
$distRoot = Join-Path -Path $root -ChildPath "dist\pixel-ui"
$workPath = Join-Path -Path $root -ChildPath "build\pixel-ui-pyinstaller"
$specPath = Join-Path -Path $root -ChildPath "build\pixel-ui-spec"
$exePath = Join-Path -Path $distRoot -ChildPath "DBNotifierPixelUI.exe"

if (-not (Test-Path -LiteralPath $Python -PathType Leaf)) {
    throw "Python executable not found: $Python"
}

if (Test-Path -LiteralPath $distRoot) {
    Remove-Item -LiteralPath $distRoot -Recurse -Force
}

New-Item -Path $distRoot -ItemType Directory -Force | Out-Null
New-Item -Path $workPath -ItemType Directory -Force | Out-Null
New-Item -Path $specPath -ItemType Directory -Force | Out-Null

& $Python -m PyInstaller `
    --noconfirm `
    --clean `
    --onefile `
    --windowed `
    --name DBNotifierPixelUI `
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

Write-Host ("Executable created: {0}" -f $exePath)
