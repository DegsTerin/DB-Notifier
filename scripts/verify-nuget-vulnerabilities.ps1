# Module purpose: Verifies complete solution and target-framework coverage before failing the build on any vulnerable direct or transitive NuGet package.
[CmdletBinding()]
param(
    [string]$DotNetPath,
    [string]$SolutionPath,
    [string]$ReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root '.dotnet\dotnet.exe'
}
if ([string]::IsNullOrWhiteSpace($SolutionPath)) {
    $SolutionPath = Join-Path $root 'DBNotifier.sln'
}
$resolvedSolution = (Resolve-Path -LiteralPath $SolutionPath).Path
$solutionDirectory = Split-Path -Path $resolvedSolution -Parent

# Resolves the selected .NET host without changing package sources or acquiring tooling.
function Resolve-DotNetHost([string]$Candidate) {
    if ([System.IO.Path]::IsPathRooted($Candidate) -or $Candidate.Contains([System.IO.Path]::DirectorySeparatorChar)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }

    $command = Get-Command $Candidate -CommandType Application -ErrorAction Stop | Select-Object -First 1
    return $command.Source
}

# Reads evaluated target frameworks from MSBuild so report completeness never depends on optional NuGet finding nodes.
function Get-EvaluatedTargetFrameworks([string]$ProjectPath, [string]$DotNetHost) {
    $output = & $DotNetHost msbuild $ProjectPath -nologo -nodeReuse:false -getProperty:TargetFramework -getProperty:TargetFrameworks
    if ($LASTEXITCODE -ne 0) {
        throw "MSBuild target-framework inventory failed for project: $ProjectPath"
    }

    try {
        $evaluation = ($output -join "`n") | ConvertFrom-Json
    }
    catch {
        throw "MSBuild returned an unreadable target-framework inventory for project: $ProjectPath"
    }
    $properties = if ($null -eq $evaluation) { $null } else { $evaluation.PSObject.Properties['Properties'] }
    if ($null -eq $properties) {
        throw "MSBuild returned no target-framework properties for project: $ProjectPath"
    }

    $plural = [string]$properties.Value.PSObject.Properties['TargetFrameworks'].Value
    $singular = [string]$properties.Value.PSObject.Properties['TargetFramework'].Value
    $rawFrameworks = if (-not [string]::IsNullOrWhiteSpace($plural)) { $plural } else { $singular }
    $frameworks = @($rawFrameworks.Split(';', [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() })
    if ($frameworks.Count -eq 0 -or @($frameworks | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) {
        throw "MSBuild returned no usable target framework for project: $ProjectPath"
    }
    if (@($frameworks | Select-Object -Unique).Count -ne $frameworks.Count) {
        throw "MSBuild returned duplicate target frameworks for project: $ProjectPath"
    }
    return $frameworks
}

$resolvedDotNet = Resolve-DotNetHost $DotNetPath

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $output = & $resolvedDotNet list $resolvedSolution package --vulnerable --include-transitive --format json
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet vulnerability inspection failed with exit code $LASTEXITCODE."
    }

    $reportJson = $output -join "`n"
}
else {
    $reportJson = Get-Content -LiteralPath $ReportPath -Raw -Encoding UTF8
}

$report = $reportJson | ConvertFrom-Json
$versionProperty = if ($null -eq $report) { $null } else { $report.PSObject.Properties['version'] }
if ($null -eq $versionProperty -or $versionProperty.Value -ne 1) {
    throw "NuGet vulnerability report schema is missing or unsupported. Expected version 1."
}
$parametersProperty = $report.PSObject.Properties['parameters']
if ($null -eq $parametersProperty -or
    [string]::IsNullOrWhiteSpace([string]$parametersProperty.Value) -or
    [string]$parametersProperty.Value -notmatch '(?:^|\s)--vulnerable(?:\s|$)' -or
    [string]$parametersProperty.Value -notmatch '(?:^|\s)--include-transitive(?:\s|$)') {
    throw "NuGet vulnerability report parameters do not prove a direct and transitive vulnerability inspection."
}
$sourcesProperty = $report.PSObject.Properties['sources']
if ($null -eq $sourcesProperty -or @($sourcesProperty.Value).Count -eq 0) {
    throw "NuGet vulnerability report contains no package source evidence."
}
$projectsProperty = $report.PSObject.Properties['projects']
if ($null -eq $projectsProperty) {
    throw "NuGet vulnerability report contains no project inventory."
}

# Match report provenance to the repository-owned source configuration so an unrelated or empty advisory feed cannot produce a pass.
$nugetConfigPath = Join-Path $solutionDirectory "NuGet.config"
[xml]$nugetConfig = Get-Content -LiteralPath $nugetConfigPath -Raw -Encoding UTF8
$configuredSources = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($source in @($nugetConfig.configuration.packageSources.add)) {
    $sourceValue = if ($null -eq $source) { '' } else { $source.GetAttribute('value') }
    if ([string]::IsNullOrWhiteSpace($sourceValue)) {
        throw "NuGet.config contains an unnamed or empty package source."
    }
    [void]$configuredSources.Add($sourceValue.TrimEnd('/'))
}
if ($configuredSources.Count -eq 0) {
    throw "NuGet.config contains no canonical package source."
}
$reportedSources = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($source in @($sourcesProperty.Value)) {
    if ([string]::IsNullOrWhiteSpace([string]$source) -or -not $reportedSources.Add(([string]$source).TrimEnd('/'))) {
        throw "NuGet vulnerability report contains an empty or duplicate package source."
    }
}
if ($reportedSources.Count -ne $configuredSources.Count) {
    throw "NuGet vulnerability report source coverage does not match NuGet.config."
}
foreach ($configuredSource in $configuredSources) {
    if (-not $reportedSources.Contains($configuredSource)) {
        throw "NuGet vulnerability report omitted configured source: $configuredSource"
    }
}

# Compare the report to the solution rather than accepting a syntactically valid but incomplete JSON envelope.
$solutionText = Get-Content -LiteralPath $resolvedSolution -Raw -Encoding UTF8
$projectMatches = [regex]::Matches($solutionText, 'Project\("[^\"]+"\)\s*=\s*"[^\"]+",\s*"([^\"]+\.csproj)"')
$expectedProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$expectedFrameworks = @{}
foreach ($match in $projectMatches) {
    $projectPath = [System.IO.Path]::GetFullPath((Join-Path $solutionDirectory $match.Groups[1].Value))
    if (-not $expectedProjects.Add($projectPath)) {
        throw "The solution contains a duplicate project path: $projectPath"
    }
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "The solution references a missing project: $projectPath"
    }
    $expectedFrameworks[$projectPath] = @(Get-EvaluatedTargetFrameworks $projectPath $resolvedDotNet)
}
if ($expectedProjects.Count -eq 0) {
    throw "The solution contains no discoverable .NET projects; vulnerability coverage cannot be proved."
}

$actualProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($project in @($projectsProperty.Value)) {
    $pathProperty = if ($null -eq $project) { $null } else { $project.PSObject.Properties['path'] }
    if ($null -eq $pathProperty -or [string]::IsNullOrWhiteSpace([string]$pathProperty.Value)) {
        throw "NuGet vulnerability report contains a project without a path."
    }
    $reportedProjectPath = [string]$pathProperty.Value
    $projectPath = if ([System.IO.Path]::IsPathRooted($reportedProjectPath)) {
        [System.IO.Path]::GetFullPath($reportedProjectPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $solutionDirectory $reportedProjectPath))
    }
    if (-not $actualProjects.Add($projectPath)) {
        throw "NuGet vulnerability report contains a duplicate project: $projectPath"
    }

    # A no-finding SDK report may omit frameworks completely. When present, its optional detail must match MSBuild exactly.
    $frameworksProperty = $project.PSObject.Properties['frameworks']
    if ($null -ne $frameworksProperty -and @($frameworksProperty.Value).Count -gt 0) {
        $reportedFrameworks = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($framework in @($frameworksProperty.Value)) {
            $frameworkNameProperty = if ($null -eq $framework) { $null } else { $framework.PSObject.Properties['framework'] }
            if ($null -eq $frameworkNameProperty -or [string]::IsNullOrWhiteSpace([string]$frameworkNameProperty.Value) -or
                -not $reportedFrameworks.Add(([string]$frameworkNameProperty.Value).Trim())) {
                throw "NuGet vulnerability report contains an unnamed or duplicate framework for project: $projectPath"
            }
        }
        $evaluatedFrameworks = @($expectedFrameworks[$projectPath])
        if ($reportedFrameworks.Count -ne $evaluatedFrameworks.Count -or
            @($evaluatedFrameworks | Where-Object { -not $reportedFrameworks.Contains($_) }).Count -gt 0) {
            throw "NuGet vulnerability report framework coverage does not match MSBuild for project: $projectPath"
        }
    }
}
if ($actualProjects.Count -ne $expectedProjects.Count) {
    throw "NuGet vulnerability report covered $($actualProjects.Count) projects; the solution requires $($expectedProjects.Count)."
}
foreach ($expectedProject in $expectedProjects) {
    if (-not $actualProjects.Contains($expectedProject)) {
        throw "NuGet vulnerability report omitted solution project: $expectedProject"
    }
}

$findings = [System.Collections.Generic.List[object]]::new()

# NuGet omits framework/package collections when no vulnerabilities exist. Traverse only recognised version-one collections.
foreach ($project in @($projectsProperty.Value)) {
    $frameworksProperty = $project.PSObject.Properties['frameworks']
    if ($null -eq $frameworksProperty) { continue }
    foreach ($framework in @($frameworksProperty.Value)) {
        foreach ($collectionName in @('topLevelPackages', 'transitivePackages')) {
            $collectionProperty = $framework.PSObject.Properties[$collectionName]
            if ($null -eq $collectionProperty) { continue }
            foreach ($package in @($collectionProperty.Value)) {
                $vulnerabilitiesProperty = if ($null -eq $package) { $null } else { $package.PSObject.Properties['vulnerabilities'] }
                if ($null -ne $vulnerabilitiesProperty -and @($vulnerabilitiesProperty.Value).Count -gt 0) {
                    $packageIdProperty = $package.PSObject.Properties['id']
                    $resolvedVersionProperty = $package.PSObject.Properties['resolvedVersion']
                    if ($null -eq $packageIdProperty -or [string]::IsNullOrWhiteSpace([string]$packageIdProperty.Value) -or
                        $null -eq $resolvedVersionProperty -or [string]::IsNullOrWhiteSpace([string]$resolvedVersionProperty.Value)) {
                        throw "NuGet vulnerability report contains an affected package without identity or resolved version."
                    }
                    $findings.Add([pscustomobject]@{
                        Project = $project.PSObject.Properties['path'].Value
                        Framework = $framework.PSObject.Properties['framework'].Value
                        Package = $packageIdProperty.Value
                        ResolvedVersion = $resolvedVersionProperty.Value
                        Vulnerabilities = @($vulnerabilitiesProperty.Value).Count
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

Write-Output "NuGet vulnerability gate passed for $($actualProjects.Count) solution projects with independently evaluated target frameworks."
