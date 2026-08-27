# Module purpose: Runs the pinned legacy compatibility suite with explicit skip handling and a proportional coverage floor.
<#
.SYNOPSIS
Runs the pinned Windows PowerShell compatibility suite with the caller-selected .NET host.

.PARAMETER DotNetPath
Exact absolute path to the compatible dotnet host selected by the canonical gate.

.PARAMETER MinimumCoveragePercent
Minimum accepted command-coverage percentage for the legacy module.

.OUTPUTS
Legacy compatibility, skip and coverage evidence.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$DotNetPath,

    [ValidateRange(0, 100)]
    [double]$MinimumCoveragePercent = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$requiredHost = "Windows PowerShell 5.1 Desktop"
if ($PSVersionTable.PSEdition -ne "Desktop" -or $PSVersionTable.PSVersion.Major -ne 5 -or $PSVersionTable.PSVersion.Minor -lt 1) {
    throw "$requiredHost is required for the pinned Pester 3.4.0 compatibility gate. Run this script with powershell.exe, not pwsh."
}
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$testPath = Join-Path $root "tests\DBNotifier.Legacy.Tests.ps1"
$modulePath = Join-Path $root "src\modules\DBNotifier\DBNotifier.psm1"
$requiredPesterVersion = [version]"3.4.0"
$expectedConditionalSkip = "resolves pg_isready from a standard PostgreSQL installation even when it is not on PATH"

if (-not [System.IO.Path]::IsPathRooted($DotNetPath) -or
    -not (Test-Path -LiteralPath $DotNetPath -PathType Leaf)) {
    throw "The legacy compatibility gate requires an existing absolute dotnet host path."
}
$resolvedDotNetPath = (Resolve-Path -LiteralPath $DotNetPath).Path

$pester = Get-Module -ListAvailable -Name Pester |
    Where-Object Version -eq $requiredPesterVersion |
    Select-Object -First 1
if ($null -eq $pester) {
    throw "Pester $requiredPesterVersion is required. Provision it explicitly; this gate never installs tooling."
}

Import-Module $pester.Path -Force
$testScript = @{
    Path = $testPath
    Parameters = @{
        DotNetPath = $resolvedDotNetPath
    }
}
$result = Invoke-Pester -Script $testScript -PassThru -CodeCoverage $modulePath -Quiet
$unexpectedSkips = @($result.TestResult | Where-Object {
    $_.Result -eq "Skipped" -and $_.Name -ne $expectedConditionalSkip
})
$conditionalSkips = @($result.TestResult | Where-Object {
    $_.Result -eq "Skipped" -and $_.Name -eq $expectedConditionalSkip
})

if ($result.FailedCount -gt 0 -or $result.PendingCount -gt 0) {
    $failureDetails = $result.TestResult |
        Where-Object Result -In @("Failed", "Pending") |
        Select-Object Name, Result, FailureMessage |
        Format-List |
        Out-String
    Write-Host $failureDetails
    throw "Legacy compatibility tests failed or remained pending. Failed=$($result.FailedCount), Pending=$($result.PendingCount)."
}
if ($unexpectedSkips.Count -gt 0 -or $conditionalSkips.Count -gt 1) {
    throw "Legacy compatibility tests contain unexpected skipped coverage."
}

$analysed = [int]$result.CodeCoverage.NumberOfCommandsAnalyzed
$executed = [int]$result.CodeCoverage.NumberOfCommandsExecuted
if ($analysed -le 0) {
    throw "Pester returned no analysable command coverage for the legacy module."
}
$coveragePercent = [Math]::Round(($executed / $analysed) * 100, 2)
if ($coveragePercent -lt $MinimumCoveragePercent) {
    throw "Legacy command coverage is $coveragePercent%; the required floor is $MinimumCoveragePercent%."
}

if ($conditionalSkips.Count -eq 1) {
    Write-Output "Conditional skip accepted: $expectedConditionalSkip"
}

Write-Output ("Legacy gate passed on {0}: tests={1}, skipped={2}, coverage={3}% ({4}/{5} commands)." -f `
    $requiredHost,
    $result.PassedCount,
    $result.SkippedCount,
    $coveragePercent,
    $executed,
    $analysed)
