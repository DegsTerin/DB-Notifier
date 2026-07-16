# Module purpose: Runs the pinned legacy compatibility suite with explicit skip handling and a proportional coverage floor.
[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double]$MinimumCoveragePercent = 25
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$testPath = Join-Path $root "tests\DBNotifier.Legacy.Tests.ps1"
$modulePath = Join-Path $root "src\modules\DBNotifier\DBNotifier.psm1"
$requiredPesterVersion = [version]"3.4.0"
$expectedConditionalSkip = "resolves pg_isready from a standard PostgreSQL installation even when it is not on PATH"

$pester = Get-Module -ListAvailable -Name Pester |
    Where-Object Version -eq $requiredPesterVersion |
    Select-Object -First 1
if ($null -eq $pester) {
    throw "Pester $requiredPesterVersion is required. Provision it explicitly; this gate never installs tooling."
}

Import-Module $pester.Path -Force
$result = Invoke-Pester -Script $testPath -PassThru -CodeCoverage $modulePath -Quiet
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

Write-Output ("Legacy gate passed: tests={0}, skipped={1}, coverage={2}% ({3}/{4} commands)." -f `
    $result.PassedCount,
    $result.SkippedCount,
    $coveragePercent,
    $executed,
    $analysed)
