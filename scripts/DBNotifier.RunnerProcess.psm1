# Module purpose: Provides shell-free process resolution and concurrent output capture for DB-Notifier's local test runners.
# It preserves exact argument boundaries while leaving process ownership, termination policy and residue checks with each caller.
#Requires -Version 7.0

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-DBNotifierRunnerExecutable {
    <#
    .SYNOPSIS
    Resolves one runner executable to an absolute application path.

    .DESCRIPTION
    Selects the supplied candidate or an explicit default, then resolves either a literal file path or an
    application available on PATH. Aliases, functions and PowerShell scripts are not accepted as executable hosts.

    .PARAMETER Candidate
    Optional executable path or application name selected by the caller.

    .PARAMETER DefaultPath
    Optional executable path used only when Candidate is null, empty or whitespace.

    .PARAMETER BaseDirectory
    Existing absolute directory used to resolve an explicitly relative Candidate or DefaultPath. Application names
    without directory separators continue to resolve through PATH.

    .OUTPUTS
    System.String. The absolute executable path.

    .NOTES
    Throws when neither input selects an executable, when an explicit path is unavailable, or when an application
    name cannot be resolved. Resolution performs no process creation or external acquisition.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Candidate,

        [AllowNull()]
        [AllowEmptyString()]
        [string]$DefaultPath,

        [ValidateNotNullOrEmpty()]
        [string]$BaseDirectory = (Get-Location).Path
    )

    if (-not [System.IO.Path]::IsPathRooted($BaseDirectory) -or
        -not (Test-Path -LiteralPath $BaseDirectory -PathType Container)) {
        throw 'The runner executable base directory must be an available absolute directory.'
    }
    $resolvedBaseDirectory = [System.IO.Path]::GetFullPath(
        (Resolve-Path -LiteralPath $BaseDirectory).Path)

    $selected = if ([string]::IsNullOrWhiteSpace($Candidate)) {
        $DefaultPath
    }
    else {
        $Candidate
    }
    if ([string]::IsNullOrWhiteSpace($selected)) {
        throw 'No runner executable was selected.'
    }

    $containsDirectorySeparator =
        $selected.Contains([System.IO.Path]::DirectorySeparatorChar) -or
        $selected.Contains([System.IO.Path]::AltDirectorySeparatorChar)
    if ([System.IO.Path]::IsPathRooted($selected) -or $containsDirectorySeparator) {
        $selectedPath = if ([System.IO.Path]::IsPathRooted($selected)) {
            $selected
        }
        else {
            Join-Path $resolvedBaseDirectory $selected
        }
        if (-not (Test-Path -LiteralPath $selectedPath -PathType Leaf)) {
            throw 'The selected runner executable path is unavailable.'
        }
        return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $selectedPath).Path)
    }
    if ([System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($selected)) {
        throw 'Runner application names must not contain wildcard characters.'
    }

    $application = Get-Command -Name $selected -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -eq $application -or
        [string]::IsNullOrWhiteSpace([string]$application.Source) -or
        -not (Test-Path -LiteralPath $application.Source -PathType Leaf)) {
        throw 'The selected runner application is unavailable.'
    }

    return [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $application.Source).Path)
}

function Start-DBNotifierRunnerProcess {
    <#
    .SYNOPSIS
    Starts one runner-owned process with an exact argument vector and concurrently captured output.

    .DESCRIPTION
    Uses ProcessStartInfo.ArgumentList without a command shell, opens unique stdout and stderr evidence files, and
    drains both child pipes concurrently. The returned handle exposes the Process for caller-specific readiness,
    timeout, ownership and termination logic.

    .PARAMETER FilePath
    Absolute path of the executable to start.

    .PARAMETER ArgumentList
    Ordered arguments supplied exactly as individual process arguments. Empty strings are preserved.

    .PARAMETER WorkingDirectory
    Existing absolute directory used as the child process working directory.

    .PARAMETER StandardOutputPath
    New absolute file that receives standard output incrementally.

    .PARAMETER StandardErrorPath
    New absolute file that receives standard error incrementally.

    .PARAMETER Visible
    Allows a normal visible window. Without this switch the child is configured without a visible console window.

    .OUTPUTS
    PSCustomObject with type name DBNotifier.RunnerProcessHandle. It contains the Process, capture streams, copy
    tasks, completion state and eventual exit code.

    .NOTES
    Throws before process creation when paths are invalid or evidence files already exist. If startup fails after
    process creation, the exact child tree is terminated and all locally opened resources are released.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$FilePath,

        [AllowEmptyCollection()]
        [AllowNull()]
        [string[]]$ArgumentList = @(),

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$WorkingDirectory,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$StandardOutputPath,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$StandardErrorPath,

        [switch]$Visible
    )

    if (-not [System.IO.Path]::IsPathRooted($FilePath) -or
        -not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
        throw 'The runner executable must be an available absolute file path.'
    }
    if (-not [System.IO.Path]::IsPathRooted($WorkingDirectory) -or
        -not (Test-Path -LiteralPath $WorkingDirectory -PathType Container)) {
        throw 'The runner working directory must be an available absolute directory.'
    }
    if (-not [System.IO.Path]::IsPathRooted($StandardOutputPath) -or
        -not [System.IO.Path]::IsPathRooted($StandardErrorPath)) {
        throw 'Runner output paths must be absolute.'
    }

    $resolvedExecutable = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $FilePath).Path)
    $resolvedWorkingDirectory = [System.IO.Path]::GetFullPath(
        (Resolve-Path -LiteralPath $WorkingDirectory).Path)
    $resolvedOutputPath = [System.IO.Path]::GetFullPath($StandardOutputPath)
    $resolvedErrorPath = [System.IO.Path]::GetFullPath($StandardErrorPath)
    $pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
        [StringComparison]::OrdinalIgnoreCase
    }
    else {
        [StringComparison]::Ordinal
    }
    if ($resolvedOutputPath.Equals($resolvedErrorPath, $pathComparison)) {
        throw 'Standard output and standard error require distinct files.'
    }
    if (Test-Path -LiteralPath $resolvedOutputPath) {
        throw 'The standard-output evidence file already exists.'
    }
    if (Test-Path -LiteralPath $resolvedErrorPath) {
        throw 'The standard-error evidence file already exists.'
    }

    $outputParent = Split-Path -Parent $resolvedOutputPath
    $errorParent = Split-Path -Parent $resolvedErrorPath
    if (-not (Test-Path -LiteralPath $outputParent -PathType Container) -or
        -not (Test-Path -LiteralPath $errorParent -PathType Container)) {
        throw 'Runner output directories must exist before process creation.'
    }

    $outputStream = $null
    $errorStream = $null
    $process = $null
    $processStarted = $false
    try {
        $fileOptions =
            [System.IO.FileOptions]::Asynchronous -bor
            [System.IO.FileOptions]::SequentialScan -bor
            [System.IO.FileOptions]::WriteThrough
        # A one-byte FileStream buffer makes short readiness records visible before process exit.
        $outputStream = [System.IO.FileStream]::new(
            $resolvedOutputPath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::ReadWrite,
            1,
            $fileOptions)
        $errorStream = [System.IO.FileStream]::new(
            $resolvedErrorPath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::ReadWrite,
            1,
            $fileOptions)

        $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
        $startInfo.FileName = $resolvedExecutable
        $startInfo.WorkingDirectory = $resolvedWorkingDirectory
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.CreateNoWindow = -not $Visible
        $startInfo.WindowStyle = if ($Visible) {
            [System.Diagnostics.ProcessWindowStyle]::Normal
        }
        else {
            [System.Diagnostics.ProcessWindowStyle]::Hidden
        }
        foreach ($argument in @($ArgumentList)) {
            if ($null -eq $argument) {
                throw 'Runner arguments cannot contain null entries.'
            }
            $startInfo.ArgumentList.Add([string]$argument)
        }

        $process = [System.Diagnostics.Process]::new()
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'The runner-owned process could not be started.'
        }
        $processStarted = $true

        # Both reads begin immediately so neither redirected child pipe can fill while the other is waiting.
        $outputCopyTask = $process.StandardOutput.BaseStream.CopyToAsync($outputStream)
        $errorCopyTask = $process.StandardError.BaseStream.CopyToAsync($errorStream)

        return [pscustomobject]@{
            PSTypeName = 'DBNotifier.RunnerProcessHandle'
            Process = $process
            StandardOutputStream = $outputStream
            StandardErrorStream = $errorStream
            StandardOutputCopyTask = $outputCopyTask
            StandardErrorCopyTask = $errorCopyTask
            Completed = $false
            ExitCode = $null
            CompletionFailure = $null
        }
    }
    catch {
        if ($processStarted -and $null -ne $process) {
            try {
                if (-not $process.HasExited) {
                    $process.Kill($true)
                    [void]$process.WaitForExit(5000)
                }
            }
            catch {
                # Startup cleanup is best-effort here; the original sanitised startup failure remains authoritative.
            }
        }
        if ($null -ne $process) {
            $process.Dispose()
        }
        if ($null -ne $errorStream) {
            $errorStream.Dispose()
        }
        if ($null -ne $outputStream) {
            $outputStream.Dispose()
        }
        throw
    }
}

function Complete-DBNotifierRunnerProcess {
    <#
    .SYNOPSIS
    Completes output capture and releases one already-exited runner process.

    .DESCRIPTION
    Requires the caller to have proved or performed process termination, drains stdout and stderr together under
    one bounded deadline, durably flushes both evidence files, disposes all capture resources and records the exit
    code. This function never decides which process may be killed.

    .PARAMETER Handle
    Handle returned by Start-DBNotifierRunnerProcess.

    .PARAMETER DrainTimeoutMilliseconds
    Total time allowed for both redirected streams to reach end-of-stream after the process has exited.

    .OUTPUTS
    System.Int32. The child process exit code. Repeated calls return the recorded code or rethrow the recorded
    capture-integrity failure without repeating cleanup.

    .NOTES
    Throws when the process is still active, when the handle is invalid, or when capture cannot drain cleanly.
    A timeout closes the local pipe readers and capture streams so inherited handles cannot leak into later work.
    #>
    [CmdletBinding()]
    [OutputType([int])]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNull()]
        [psobject]$Handle,

        [ValidateRange(100, 60000)]
        [int]$DrainTimeoutMilliseconds = 15000
    )

    if ($Handle.PSObject.TypeNames -notcontains 'DBNotifier.RunnerProcessHandle') {
        throw 'The supplied runner process handle is invalid.'
    }
    if ([bool]$Handle.Completed) {
        if (-not [string]::IsNullOrWhiteSpace([string]$Handle.CompletionFailure)) {
            throw [string]$Handle.CompletionFailure
        }
        return [int]$Handle.ExitCode
    }

    $process = [System.Diagnostics.Process]$Handle.Process
    if (-not $process.HasExited) {
        throw 'The runner-owned process remains active; its caller must stop it before completing capture.'
    }

    $exitCode = $process.ExitCode
    $drainTimedOut = $false
    $drainFailed = $false
    try {
        $copyTasks = [System.Threading.Tasks.Task[]]@(
            $Handle.StandardOutputCopyTask,
            $Handle.StandardErrorCopyTask)
        $combinedCopyTask = [System.Threading.Tasks.Task]::WhenAll($copyTasks)
        $timeoutTask = [System.Threading.Tasks.Task]::Delay($DrainTimeoutMilliseconds)
        $completedTask = [System.Threading.Tasks.Task]::WhenAny(
            $combinedCopyTask,
            $timeoutTask).GetAwaiter().GetResult()
        if (-not [object]::ReferenceEquals($completedTask, $combinedCopyTask)) {
            $drainTimedOut = $true

            # Closing the local readers releases blocked copy operations when a descendant retained an inherited pipe.
            $process.StandardOutput.BaseStream.Dispose()
            $process.StandardError.BaseStream.Dispose()
            $boundedRelease = [System.Threading.Tasks.Task]::WhenAny(
                $combinedCopyTask,
                [System.Threading.Tasks.Task]::Delay(2000)).GetAwaiter().GetResult()
            if ([object]::ReferenceEquals($boundedRelease, $combinedCopyTask)) {
                try {
                    $combinedCopyTask.GetAwaiter().GetResult()
                }
                catch {
                    $drainFailed = $true
                }
            }
        }
        else {
            try {
                $combinedCopyTask.GetAwaiter().GetResult()
            }
            catch {
                $drainFailed = $true
            }
        }
    }
    catch {
        $drainFailed = $true
    }
    finally {
        foreach ($stream in @($Handle.StandardOutputStream, $Handle.StandardErrorStream)) {
            try {
                $stream.Flush($true)
            }
            catch {
                $drainFailed = $true
            }
            finally {
                $stream.Dispose()
            }
        }
        try {
            $process.Dispose()
        }
        catch {
            $drainFailed = $true
        }
        $completionFailure = if ($drainTimedOut) {
            'Runner output capture did not reach end-of-stream within its bounded drain period.'
        }
        elseif ($drainFailed) {
            'Runner output capture ended with an unreadable stream failure.'
        }
        else {
            $null
        }
        $Handle.ExitCode = $exitCode
        $Handle.CompletionFailure = $completionFailure
        $Handle.Completed = $true
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$Handle.CompletionFailure)) {
        throw [string]$Handle.CompletionFailure
    }

    return $exitCode
}

Export-ModuleMember -Function @(
    'Resolve-DBNotifierRunnerExecutable',
    'Start-DBNotifierRunnerProcess',
    'Complete-DBNotifierRunnerProcess')
