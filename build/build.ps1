# Module purpose: Validates the canonical compatibility bundle and gates packaging on an approved, hashed local toolchain.
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
$toolchainManifestPath = Join-Path -Path $PSScriptRoot -ChildPath "compatibility-toolchain.json"
$bundlePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-bundle-{0}.ps1" -f [guid]::NewGuid().ToString("N"))
$exePath = Join-Path -Path $packagePath -ChildPath "DBNotifier.exe"
$canonicalIconDirectory = Join-Path -Path $root -ChildPath "src\DBNotifier.Desktop.Wpf\Assets"
$iconPath = Join-Path -Path $canonicalIconDirectory -ChildPath "DBNotifier.ico"
$runtimeIconNames = @(
    "DBNotifier.Healthy.ico"
    "DBNotifier.Warning.ico"
    "DBNotifier.Critical.ico"
    "DBNotifier.Unknown.ico"
)

if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "Generate the canonical DB-Notifier icon before building compatibility packaging."
}

foreach ($runtimeIconName in $runtimeIconNames) {
    $runtimeIconPath = Join-Path -Path $canonicalIconDirectory -ChildPath $runtimeIconName
    if (-not (Test-Path -LiteralPath $runtimeIconPath -PathType Leaf)) {
        throw "Generate the complete canonical DB-Notifier semantic icon family before building compatibility packaging. Missing: $runtimeIconName"
    }
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

    $toolchain = Get-Content -LiteralPath $toolchainManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($toolchain.schemaVersion -ne "dbnotifier.compatibility-toolchain.v1" -or $toolchain.status -ne "approved") {
        throw "Compatibility packaging is blocked until build/compatibility-toolchain.json records an explicitly approved, locally verified toolchain. Bundle validation remains available with -ValidateOnly."
    }
    foreach ($propertyName in @("ps2exeVersion", "ps2exeManifestSha256", "ps2exeRootModuleSha256")) {
        if ([string]::IsNullOrWhiteSpace([string]$toolchain.$propertyName)) {
            throw "The approved compatibility toolchain is missing $propertyName."
        }
    }
    $ps2exe = Get-Module -ListAvailable -Name ps2exe |
        Where-Object Version -eq ([version]$toolchain.ps2exeVersion) |
        Select-Object -First 1
    if (-not $ps2exe) {
        throw "The approved ps2exe version $($toolchain.ps2exeVersion) is unavailable. Provision it explicitly; this build never installs tooling."
    }
    if ([string]::IsNullOrWhiteSpace([string]$ps2exe.RootModule)) {
        throw "The approved ps2exe module does not declare a root module."
    }
    $ps2exeManifestPath = Join-Path $ps2exe.ModuleBase "$($ps2exe.Name).psd1"
    $ps2exeRootModulePath = Join-Path $ps2exe.ModuleBase $ps2exe.RootModule
    foreach ($requiredToolPath in @($ps2exeManifestPath, $ps2exeRootModulePath)) {
        if (-not (Test-Path -LiteralPath $requiredToolPath -PathType Leaf)) {
            throw "The approved ps2exe toolchain file is missing: $requiredToolPath"
        }
    }
    $observedManifestHash = (Get-FileHash -LiteralPath $ps2exeManifestPath -Algorithm SHA256).Hash
    $observedRootModuleHash = (Get-FileHash -LiteralPath $ps2exeRootModulePath -Algorithm SHA256).Hash
    if ($observedManifestHash -ne [string]$toolchain.ps2exeManifestSha256 -or
        $observedRootModuleHash -ne [string]$toolchain.ps2exeRootModuleSha256) {
        throw "The installed ps2exe files do not match the approved compatibility toolchain hashes."
    }

    $iscc = $null
    if (-not $SkipInstaller) {
        foreach ($propertyName in @("innoSetupVersion", "innoCompilerSha256")) {
            if ([string]::IsNullOrWhiteSpace([string]$toolchain.$propertyName)) {
                throw "The approved compatibility toolchain is missing $propertyName."
            }
        }
        $iscc = Get-Command -Name "ISCC.exe" -CommandType Application -ErrorAction SilentlyContinue
        if (-not $iscc) {
            throw "The approved Inno Setup compiler is unavailable. Provision it explicitly; this build never installs tooling."
        }
        $observedInnoVersion = (Get-Item -LiteralPath $iscc.Source).VersionInfo.ProductVersion
        $observedInnoHash = (Get-FileHash -LiteralPath $iscc.Source -Algorithm SHA256).Hash
        if ($observedInnoVersion -ne [string]$toolchain.innoSetupVersion -or
            $observedInnoHash -ne [string]$toolchain.innoCompilerSha256) {
            throw "The installed Inno Setup compiler does not match the approved compatibility toolchain version and hash."
        }
    }

    $distributionRoot = Join-Path -Path $root -ChildPath "dist"
    if (Test-Path -LiteralPath $distributionRoot) {
        $resolvedDistributionRoot = [System.IO.Path]::GetFullPath($distributionRoot)
        $resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($root).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
        $expectedDistributionRoot = $resolvedRepositoryRoot + [System.IO.Path]::DirectorySeparatorChar + "dist"
        if (-not $resolvedDistributionRoot.Equals($expectedDistributionRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "The compatibility distribution path escaped the repository root."
        }
        $distributionItem = Get-Item -LiteralPath $resolvedDistributionRoot -Force
        if (($distributionItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "The compatibility distribution path is a reparse point and cannot be removed safely."
        }
        Remove-Item -LiteralPath $resolvedDistributionRoot -Recurse -Force
    }
    New-Item -Path $packagePath -ItemType Directory -Force | Out-Null

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
        -Version "1.1.1" `
        -IconFile $iconPath `
        -RequireAdmin:$false
}
finally {
    Remove-Item -LiteralPath $bundlePath -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
    throw "ps2exe finished without creating the expected executable: $exePath"
}

Copy-Item -LiteralPath (Join-Path -Path $root -ChildPath "README.md") -Destination $packagePath -Force
$packageIconDirectory = Join-Path -Path $packagePath -ChildPath "Assets"
New-Item -Path $packageIconDirectory -ItemType Directory -Force | Out-Null
foreach ($runtimeIconName in $runtimeIconNames) {
    Copy-Item -LiteralPath (Join-Path -Path $canonicalIconDirectory -ChildPath $runtimeIconName) -Destination $packageIconDirectory -Force
}
$packageConfigPath = Join-Path -Path $packagePath -ChildPath "config"
New-Item -Path $packageConfigPath -ItemType Directory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path -Path $root -ChildPath "config\appsettings.json") -Destination $packageConfigPath -Force

if (-not $SkipInstaller) {
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
