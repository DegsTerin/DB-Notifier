# Module purpose: Enforces proportional .NET unit-test line and branch coverage using a temporary Cobertura report.
[CmdletBinding()]
param(
    [string]$DotNetPath,
    [string]$ProjectPath,
    [string]$Configuration = "Release",
    [ValidateRange(0, 100)]
    [double]$MinimumLineCoveragePercent = 70,
    [ValidateRange(0, 100)]
    [double]$MinimumBranchCoveragePercent = 45
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root ".dotnet\dotnet.exe"
}
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $root "tests\DBNotifier.UnitTests\DBNotifier.UnitTests.csproj"
}
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-DotNet-Coverage-{0}" -f [guid]::NewGuid().ToString("N"))

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    & $DotNetPath test $ProjectPath `
        --configuration $Configuration `
        --no-build `
        --no-restore `
        --collect:"XPlat Code Coverage" `
        --results-directory $temporaryRoot
    if ($LASTEXITCODE -ne 0) {
        throw "The .NET coverage test run failed with exit code $LASTEXITCODE."
    }

    $reports = @(Get-ChildItem -LiteralPath $temporaryRoot -Recurse -Filter "coverage.cobertura.xml" -File)
    if ($reports.Count -ne 1) {
        throw "Expected exactly one Cobertura report but found $($reports.Count)."
    }

    [xml]$coverage = Get-Content -LiteralPath $reports[0].FullName -Raw -Encoding UTF8
    if ($null -eq $coverage.coverage -or
        $null -eq $coverage.coverage.'line-rate' -or
        $null -eq $coverage.coverage.'branch-rate') {
        throw "The Cobertura report does not contain the required line and branch rates."
    }

    $lineCoverage = [Math]::Round(([double]::Parse(
        [string]$coverage.coverage.'line-rate',
        [Globalization.CultureInfo]::InvariantCulture) * 100), 2)
    $branchCoverage = [Math]::Round(([double]::Parse(
        [string]$coverage.coverage.'branch-rate',
        [Globalization.CultureInfo]::InvariantCulture) * 100), 2)
    if ($lineCoverage -lt $MinimumLineCoveragePercent) {
        throw "Line coverage is $lineCoverage%; the required floor is $MinimumLineCoveragePercent%."
    }
    if ($branchCoverage -lt $MinimumBranchCoveragePercent) {
        throw "Branch coverage is $branchCoverage%; the required floor is $MinimumBranchCoveragePercent%."
    }

    Write-Output "Coverage gate passed: lines=$lineCoverage%, branches=$branchCoverage%."
}
finally {
    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    $resolvedSystemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $expectedPrefix = $resolvedSystemTemp + [System.IO.Path]::DirectorySeparatorChar + "DBNotifier-DotNet-Coverage-"
    if ($resolvedTemporaryRoot.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}
