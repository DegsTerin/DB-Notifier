# Module purpose: Proves every solution project has a tracked lockfile and that locked restore leaves repository state unchanged.
[CmdletBinding()]
param(
    [string]$DotNetPath,
    [string]$SolutionPath,
    [string]$NuGetConfigPath
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
$solutionDirectory = Split-Path -Parent $resolvedSolution

# Resolves either an explicit executable path or an application already available on PATH.
function Resolve-DotNetHost([string]$Candidate) {
    if ([System.IO.Path]::IsPathRooted($Candidate) -or $Candidate.Contains([System.IO.Path]::DirectorySeparatorChar)) {
        return (Resolve-Path -LiteralPath $Candidate).Path
    }
    return (Get-Command $Candidate -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
}

$solutionText = Get-Content -LiteralPath $resolvedSolution -Raw -Encoding UTF8
$projectMatches = [regex]::Matches($solutionText, 'Project\("[^\"]+"\)\s*=\s*"[^\"]+",\s*"([^\"]+\.csproj)"')
if ($projectMatches.Count -eq 0) {
    throw 'The solution contains no discoverable .NET projects.'
}
$lockfiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($match in $projectMatches) {
    $projectPath = [System.IO.Path]::GetFullPath((Join-Path $solutionDirectory $match.Groups[1].Value))
    $lockfile = Join-Path (Split-Path -Parent $projectPath) 'packages.lock.json'
    if (-not (Test-Path -LiteralPath $lockfile -PathType Leaf)) {
        throw "A solution project has no lockfile: $projectPath"
    }
    if (-not $lockfiles.Add($lockfile)) {
        throw "Multiple solution projects resolved to one lockfile: $lockfile"
    }
    & git -C $root ls-files --error-unmatch -- $lockfile 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "A solution lockfile is not tracked: $lockfile"
    }
}

$before = @(& git -C $root status --porcelain=v1 --untracked-files=all)
$restoreArguments = @('restore', $resolvedSolution, '--locked-mode')
if (-not [string]::IsNullOrWhiteSpace($NuGetConfigPath)) {
    $restoreArguments += @('--configfile', (Resolve-Path -LiteralPath $NuGetConfigPath).Path)
}
& (Resolve-DotNetHost $DotNetPath) @restoreArguments
if ($LASTEXITCODE -ne 0) {
    throw "Locked restore failed with exit code $LASTEXITCODE."
}
$after = @(& git -C $root status --porcelain=v1 --untracked-files=all)
if (($before -join "`n") -cne ($after -join "`n")) {
    throw 'Locked restore changed repository state.'
}

Write-Output "Lockfile gate passed for $($lockfiles.Count) solution projects; locked restore left repository state unchanged."
