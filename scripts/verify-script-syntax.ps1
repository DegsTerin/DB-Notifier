# Module purpose: Validates tracked and unignored PowerShell and Node scripts without executing repository code.
# It discovers the current Git inventory, confines every path to the repository and reports sanitised syntax evidence.
[CmdletBinding()]
param(
    [string]$RepositoryRoot,

    [string]$NodePath = 'node',

    [switch]$PowerShellOnly,

    [switch]$LegacyCompatibleOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..'
}
if ($LegacyCompatibleOnly -and -not $PowerShellOnly) {
    throw 'LegacyCompatibleOnly requires PowerShellOnly.'
}

function Resolve-ApplicationPath {
    <#
    .SYNOPSIS
    Resolves one required executable without accepting aliases, functions or scripts.

    .PARAMETER Candidate
    Absolute executable path or application name on PATH.

    .OUTPUTS
    System.String containing the absolute executable path.

    .NOTES
    Throws a sanitised error before external execution when resolution fails.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Candidate
    )

    if ([System.IO.Path]::IsPathRooted($Candidate) -or
        $Candidate.Contains([System.IO.Path]::DirectorySeparatorChar) -or
        $Candidate.Contains([System.IO.Path]::AltDirectorySeparatorChar)) {
        if (-not (Test-Path -LiteralPath $Candidate -PathType Leaf)) {
            throw 'A required syntax-check executable path is unavailable.'
        }
        return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Candidate).Path)
    }
    if ([System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Candidate)) {
        throw 'Syntax-check application names must not contain wildcard characters.'
    }

    $command = Get-Command -Name $Candidate -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $command -or
        [string]::IsNullOrWhiteSpace([string]$command.Source) -or
        -not (Test-Path -LiteralPath $command.Source -PathType Leaf)) {
        throw 'A required syntax-check application is unavailable.'
    }
    return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $command.Source).Path)
}

function Invoke-FixedGitCommand {
    <#
    .SYNOPSIS
    Invokes Git with one fixed, non-user-controlled argument string.

    .PARAMETER GitPath
    Absolute Git executable path.

    .PARAMETER WorkingDirectory
    Existing repository directory assigned through ProcessStartInfo rather than the command line.

    .PARAMETER Arguments
    Fixed Git arguments owned by this gate.

    .OUTPUTS
    PSCustomObject containing exit code, stdout and stderr.

    .NOTES
    The caller must not pass repository data through Arguments. Both streams are read concurrently.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$GitPath,

        [Parameter(Mandatory)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory)]
        [string]$Arguments
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $GitPath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.Arguments = $Arguments
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $startInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw 'Git could not start for the script inventory.'
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        [System.Threading.Tasks.Task]::WaitAll(
            [System.Threading.Tasks.Task[]]@($stdoutTask, $stderrTask))
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            StandardOutput = $stdoutTask.Result
            StandardError = $stderrTask.Result
        }
    }
    finally {
        $process.Dispose()
    }
}

function Test-LegacyPowerShellEligibility {
    <#
    .SYNOPSIS
    Determines whether one parsed script declares compatibility with the current PowerShell host.

    .PARAMETER Ast
    Parsed ScriptBlockAst whose script requirements are inspected.

    .OUTPUTS
    System.Boolean. False only when an explicit required PowerShell version exceeds the current host.

    .NOTES
    This is a syntax-gate routing decision, not proof of API or runtime compatibility.
    #>
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.Language.ScriptBlockAst]$Ast
    )

    $requirements = $Ast.ScriptRequirements
    if ($null -eq $requirements) {
        return $true
    }
    $requiredVersion = $requirements.RequiredPSVersion
    if ($null -ne $requiredVersion -and $requiredVersion -gt $PSVersionTable.PSVersion) {
        return $false
    }
    $requiredEditions = @($requirements.RequiredPSEditions)
    return $requiredEditions.Count -eq 0 -or
        $requiredEditions -contains ([string]$PSVersionTable.PSEdition)
}

function ConvertTo-SanitisedDisplayPath {
    <#
    .SYNOPSIS
    Escapes control characters before a Git-provided path is written to a diagnostic stream.

    .PARAMETER Path
    Repository-relative path to render.

    .OUTPUTS
    System.String with control characters represented as four-digit Unicode escapes.

    .NOTES
    Printable path characters remain unchanged; this function does not alter the path used for file access.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $builder = [System.Text.StringBuilder]::new()
    foreach ($character in $Path.ToCharArray()) {
        if ([char]::IsControl($character)) {
            [void]$builder.Append(('\u{0:x4}' -f [int]$character))
        }
        else {
            [void]$builder.Append($character)
        }
    }
    return $builder.ToString()
}

function Invoke-NodeSyntaxCheck {
    <#
    .SYNOPSIS
    Runs Node's parser for one confined JavaScript file without a command shell.

    .PARAMETER NodeExecutable
    Absolute Node executable path.

    .PARAMETER ScriptPath
    Absolute JavaScript path already confined to the repository.

    .OUTPUTS
    System.Boolean indicating whether Node accepted the file.

    .NOTES
    Requires ProcessStartInfo.ArgumentList and therefore runs only in the primary PowerShell 7 gate.
    Child diagnostics are consumed but never echoed because they may contain source text.
    #>
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$NodeExecutable,

        [Parameter(Mandatory)]
        [string]$ScriptPath
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    if ($null -eq $startInfo.PSObject.Properties['ArgumentList']) {
        throw 'Node syntax validation requires a PowerShell host backed by modern .NET.'
    }
    $startInfo.FileName = $NodeExecutable
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $startInfo.ArgumentList.Add('--check')
    $startInfo.ArgumentList.Add('--')
    $startInfo.ArgumentList.Add($ScriptPath)

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            throw 'Node could not start for syntax validation.'
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        [System.Threading.Tasks.Task]::WaitAll(
            [System.Threading.Tasks.Task[]]@($stdoutTask, $stderrTask))
        return $process.ExitCode -eq 0
    }
    finally {
        $process.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw 'The requested script-syntax repository root is unavailable.'
}
$resolvedRoot = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $RepositoryRoot).Path)
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}
$rootPrefix = $resolvedRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$gitPath = Resolve-ApplicationPath -Candidate 'git'

$topLevelResult = Invoke-FixedGitCommand `
    -GitPath $gitPath `
    -WorkingDirectory $resolvedRoot `
    -Arguments 'rev-parse --show-toplevel'
if ($topLevelResult.ExitCode -ne 0) {
    throw 'The requested script-syntax root is not a Git worktree.'
}
$reportedTopLevel = $topLevelResult.StandardOutput.Trim()
if ([string]::IsNullOrWhiteSpace($reportedTopLevel) -or
    -not (Test-Path -LiteralPath $reportedTopLevel -PathType Container) -or
    -not ([System.IO.Path]::GetFullPath(
        (Resolve-Path -LiteralPath $reportedTopLevel).Path)).Equals(
            $resolvedRoot,
            $pathComparison)) {
    throw 'The requested script-syntax root is not the exact Git worktree root.'
}

$pathspecs = '*.ps1 *.psm1 *.psd1'
if (-not $PowerShellOnly) {
    $pathspecs += ' *.js *.cjs *.mjs'
}
$inventoryResult = Invoke-FixedGitCommand `
    -GitPath $gitPath `
    -WorkingDirectory $resolvedRoot `
    -Arguments "ls-files --cached --others --exclude-standard -z -- $pathspecs"
if ($inventoryResult.ExitCode -ne 0) {
    throw 'Git could not enumerate the script-syntax inventory.'
}

$relativePaths = @(
    $inventoryResult.StandardOutput.Split([char]0) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object)
$failures = [System.Collections.Generic.List[string]]::new()
$powerShellParsed = 0
$powerShellSkipped = 0
$nodeParsed = 0
$nodeExecutable = $null
if (-not $PowerShellOnly) {
    $nodeExecutable = Resolve-ApplicationPath -Candidate $NodePath
}

foreach ($relativePath in $relativePaths) {
    if ([System.IO.Path]::IsPathRooted($relativePath)) {
        $failures.Add('[inventory]:0:0:path-is-rooted')
        continue
    }
    $absolutePath = [System.IO.Path]::GetFullPath((Join-Path $resolvedRoot $relativePath))
    if (-not $absolutePath.StartsWith($rootPrefix, $pathComparison)) {
        $failures.Add('[inventory]:0:0:path-escaped-root')
        continue
    }
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }
    $scriptItem = Get-Item -LiteralPath $absolutePath -Force
    $linkTypeProperty = $scriptItem.PSObject.Properties['LinkType']
    if ($null -ne $linkTypeProperty -and
        -not [string]::IsNullOrWhiteSpace([string]$linkTypeProperty.Value)) {
        $failures.Add('[inventory]:0:0:reparse-script-refused')
        continue
    }

    $normalisedRelativePath = $relativePath.Replace(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $displayRelativePath = ConvertTo-SanitisedDisplayPath -Path $normalisedRelativePath
    $extension = [System.IO.Path]::GetExtension($absolutePath).ToLowerInvariant()
    if ($extension -in @('.ps1', '.psm1', '.psd1')) {
        $tokens = $null
        $parseErrors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile(
            $absolutePath,
            [ref]$tokens,
            [ref]$parseErrors)
        if ($LegacyCompatibleOnly -and -not (Test-LegacyPowerShellEligibility -Ast $ast)) {
            $powerShellSkipped++
            continue
        }
        $powerShellParsed++
        foreach ($parseError in @($parseErrors)) {
            $failureId = if ([string]::IsNullOrWhiteSpace([string]$parseError.ErrorId)) {
                'parse-error'
            }
            else {
                [string]$parseError.ErrorId
            }
            $failures.Add((
                '{0}:{1}:{2}:powershell:{3}' -f
                $displayRelativePath,
                $parseError.Extent.StartLineNumber,
                $parseError.Extent.StartColumnNumber,
                $failureId))
        }
        continue
    }

    if (-not (Invoke-NodeSyntaxCheck -NodeExecutable $nodeExecutable -ScriptPath $absolutePath)) {
        $failures.Add(('{0}:0:0:node:syntax-error' -f $displayRelativePath))
    }
    $nodeParsed++
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error -Message $failure -ErrorAction Continue
    }
    throw "Script syntax gate failed with $($failures.Count) sanitised error(s)."
}

Write-Output (
    "Script syntax gate passed: powershell=$powerShellParsed; " +
    "legacy-skipped=$powerShellSkipped; node=$nodeParsed.")
