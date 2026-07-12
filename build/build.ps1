[CmdletBinding()]
param(
    [switch]$SkipInstaller,
    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSScriptRoot -Parent
$appPath = Join-Path -Path $root -ChildPath "src\app\DBNotifier.ps1"
$modulePath = Join-Path -Path $root -ChildPath "src\modules\DBNotifier\DBNotifier.psm1"
$packagePath = Join-Path -Path $root -ChildPath "dist\package"
$installerPath = Join-Path -Path $root -ChildPath "dist\installers"
$bundlePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-bundle-{0}.ps1" -f [guid]::NewGuid().ToString("N"))
$exePath = Join-Path -Path $packagePath -ChildPath "DBNotifier.exe"

if (-not $ValidateOnly -and (Test-Path -LiteralPath (Join-Path -Path $root -ChildPath "dist"))) {
    Remove-Item -LiteralPath (Join-Path -Path $root -ChildPath "dist") -Recurse -Force
}

if (-not $ValidateOnly) {
    New-Item -Path $packagePath -ItemType Directory -Force | Out-Null
}

$appContent = Get-Content -LiteralPath $appPath -Raw -Encoding UTF8
$moduleContent = Get-Content -LiteralPath $modulePath -Raw -Encoding UTF8
$importBlock = @'
$modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\modules\DBNotifier\DBNotifier.psm1"
Import-Module $modulePath -Force
'@

if (-not $appContent.Contains($importBlock)) {
    throw "The canonical DB-Notifier import block was not found; refusing to create an incomplete bundle."
}

$bundleContent = $appContent.Replace($importBlock, $moduleContent)
Set-Content -LiteralPath $bundlePath -Value $bundleContent -Encoding UTF8

try {
    $tokens = $null
    $parseErrors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($bundlePath, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) {
        throw ("Generated bundle is invalid: {0}" -f (($parseErrors | ForEach-Object Message) -join "; "))
    }

    if ($ValidateOnly) {
        Write-Host "DB-Notifier bundle validation passed."
        return
    }

    $ps2exe = Get-Module -ListAvailable -Name ps2exe | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $ps2exe) {
        throw "The ps2exe module is required. Install it explicitly before running this build."
    }

    Import-Module $ps2exe -Force
    Invoke-PS2EXE `
        -InputFile $bundlePath `
        -OutputFile $exePath `
        -NoConsole `
        -STA `
        -Title "DB-Notifier" `
        -Description "DB-Notifier PostgreSQL compatibility monitor" `
        -Product "DB-Notifier" `
        -Company "DegsTerin" `
        -Version "1.1.0" `
        -RequireAdmin:$false
}
finally {
    Remove-Item -LiteralPath $bundlePath -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    throw "ps2exe finished without creating the expected executable: $exePath"
}

Copy-Item -LiteralPath (Join-Path -Path $root -ChildPath "README.md") -Destination $packagePath -Force
$packageConfigPath = Join-Path -Path $packagePath -ChildPath "config"
New-Item -Path $packageConfigPath -ItemType Directory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path -Path $root -ChildPath "config\appsettings.json") -Destination $packageConfigPath -Force

if (-not $SkipInstaller) {
    $iscc = Get-Command -Name "ISCC.exe" -CommandType Application -ErrorAction SilentlyContinue
    if (-not $iscc) {
        throw "Inno Setup 6 (ISCC.exe) is required to build the installer."
    }

    New-Item -Path $installerPath -ItemType Directory -Force | Out-Null
    & $iscc.Source `
        "/DSourceDir=$packagePath" `
        "/DOutputDir=$installerPath" `
        (Join-Path -Path $root -ChildPath "packaging\inno\DBNotifier.iss")

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup failed with exit code $LASTEXITCODE."
    }
}

Write-Host ("DB-Notifier package created: {0}" -f $packagePath)
