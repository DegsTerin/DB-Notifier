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
$toolchainManifestPath = Join-Path -Path $PSScriptRoot -ChildPath "compatibility-toolchain.json"
$bundlePath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-bundle-{0}.ps1" -f [guid]::NewGuid().ToString("N"))
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
    if ($toolchain.schemaVersion -ne "dbnotifier.compatibility-toolchain.v2" -or
        $toolchain.status -ne "approved" -or
        $toolchain.completeExecutableClosure -ne $true -or
        @($toolchain.components).Count -eq 0) {
        throw "Compatibility packaging is blocked. Every executable and transitive toolchain dependency requires an approved version, SHA-256 digest and official provenance. Bundle validation remains available with -ValidateOnly."
    }

    $declaredPaths = @{}
    foreach ($component in @($toolchain.components)) {
        if ([string]::IsNullOrWhiteSpace([string]$component.id) -or
            [string]::IsNullOrWhiteSpace([string]$component.version) -or
            $component.dependencyClosureComplete -ne $true -or
            [string]::IsNullOrWhiteSpace([string]$component.provenance.officialRepository) -or
            [string]$component.provenance.commitSha -notmatch '^[0-9a-fA-F]{40}$' -or
            @($component.files).Count -eq 0) {
            throw "The compatibility toolchain has incomplete version, provenance or dependency-closure evidence."
        }
        foreach ($file in @($component.files)) {
            $relativePath = [string]$file.relativePath
            if ([string]::IsNullOrWhiteSpace($relativePath) -or
                [System.IO.Path]::IsPathRooted($relativePath) -or
                $relativePath -match '(^|[\\/])\.\.([\\/]|$)' -or
                [string]$file.sha256 -notmatch '^[0-9a-fA-F]{64}$' -or
                [string]$file.role -notin @("executable", "transitive")) {
                throw "A compatibility toolchain file lacks a canonical path, SHA-256 digest or dependency role."
            }
            $portablePath = $relativePath.Replace("\", "/").ToLowerInvariant()
            if ($declaredPaths.ContainsKey($portablePath)) {
                throw "The compatibility toolchain contains a duplicate or case-colliding file path."
            }
            $declaredPaths[$portablePath] = $true
        }
    }

    # R5 validates the closure contract but deliberately grants no authority to generate distributable artefacts.
    throw "Compatibility packaging remains unavailable in R5 even when a complete toolchain manifest is present."
}
finally {
    Remove-Item -LiteralPath $bundlePath -Force -ErrorAction SilentlyContinue
}
