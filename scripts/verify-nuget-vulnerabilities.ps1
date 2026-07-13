# Module purpose: Fails the build when NuGet reports a vulnerable direct or transitive package.
[CmdletBinding()]
param(
    [string]$DotNetPath = (Join-Path $PSScriptRoot '..\.dotnet\dotnet.exe'),
    [string]$SolutionPath = (Join-Path $PSScriptRoot '..\DBNotifier.sln')
)

$ErrorActionPreference = 'Stop'
$output = & $DotNetPath list $SolutionPath package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "NuGet vulnerability inspection failed with exit code $LASTEXITCODE."
}

$report = ($output -join "`n") | ConvertFrom-Json
$findings = [System.Collections.Generic.List[object]]::new()

# NuGet omits framework/package collections when no vulnerabilities exist. Traverse defensively so future schema additions remain fail-closed.
foreach ($project in @($report.projects)) {
    foreach ($framework in @($project.frameworks)) {
        if ($null -eq $framework) { continue }
        foreach ($collectionName in @('topLevelPackages', 'transitivePackages')) {
            foreach ($package in @($framework.$collectionName)) {
                if ($null -ne $package -and $null -ne $package.vulnerabilities -and @($package.vulnerabilities).Count -gt 0) {
                    $findings.Add([pscustomobject]@{
                        Project = $project.path
                        Framework = $framework.framework
                        Package = $package.id
                        ResolvedVersion = $package.resolvedVersion
                        Vulnerabilities = @($package.vulnerabilities).Count
                    })
                }
            }
        }
    }
}

if ($findings.Count -gt 0) {
    $findings | Format-Table -AutoSize | Out-String | Write-Error
    throw "NuGet vulnerability gate failed with $($findings.Count) affected package entry or entries."
}

Write-Output "NuGet vulnerability gate passed for $(@($report.projects).Count) projects."
