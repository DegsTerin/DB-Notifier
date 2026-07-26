# Module purpose: Exercises the runner-process support contract with local PowerShell children and disposable evidence only.
#Requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$modulePath = Join-Path (Join-Path $root 'scripts') 'DBNotifier.RunnerProcess.psm1'
Import-Module -Name $modulePath -Force -ErrorAction Stop

$systemTemporaryRoot = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::GetTempPath()).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
$temporaryLeaf = "DBNotifier-RunnerProcess-Tests With Spaces-$([guid]::NewGuid().ToString('N'))"
$temporaryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $systemTemporaryRoot $temporaryLeaf))
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}
$handles = [System.Collections.Generic.List[object]]::new()
$assertionCount = 0

function Assert-Condition {
    <#
    .SYNOPSIS
    Fails the standalone test when a required condition is false.

    .PARAMETER Condition
    Boolean condition that must be true.

    .PARAMETER Message
    Sanitised failure message describing the violated contract.

    .OUTPUTS
    None.

    .NOTES
    Throws the supplied message on failure and increments the assertion count on success.
    #>
    param(
        [Parameter(Mandatory)]
        [bool]$Condition,

        [Parameter(Mandatory)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
    $script:assertionCount++
}

function Assert-ExactValue {
    <#
    .SYNOPSIS
    Requires two scalar values to be exactly equal.

    .PARAMETER Expected
    Expected scalar value.

    .PARAMETER Actual
    Observed scalar value.

    .PARAMETER Message
    Sanitised failure message describing the comparison.

    .OUTPUTS
    None.

    .NOTES
    Uses PowerShell's case-sensitive exact comparison and throws on mismatch.
    #>
    param(
        [AllowNull()]
        [object]$Expected,

        [AllowNull()]
        [object]$Actual,

        [Parameter(Mandatory)]
        [string]$Message
    )

    Assert-Condition -Condition ($Expected -ceq $Actual) -Message $Message
}

function Assert-Throws {
    <#
    .SYNOPSIS
    Requires one operation to fail closed.

    .PARAMETER Operation
    Script block expected to throw.

    .PARAMETER Message
    Sanitised failure message used when the operation unexpectedly succeeds.

    .OUTPUTS
    None.

    .NOTES
    The thrown implementation detail is deliberately not printed because only failure classification matters.
    #>
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Operation,

        [Parameter(Mandatory)]
        [string]$Message
    )

    $threw = $false
    try {
        & $Operation
    }
    catch {
        $threw = $true
    }
    Assert-Condition -Condition $threw -Message $Message
}

function Wait-ForCapturedText {
    <#
    .SYNOPSIS
    Waits for one exact marker to become visible in an incrementally written evidence file.

    .PARAMETER Path
    Evidence file opened by the runner-process module.

    .PARAMETER Marker
    Text that must appear before the bounded deadline.

    .PARAMETER TimeoutMilliseconds
    Total polling budget.

    .OUTPUTS
    System.Boolean indicating whether the marker became visible.

    .NOTES
    Transient sharing and partial-write failures are retried without exposing evidence content.
    #>
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Marker,

        [ValidateRange(100, 30000)]
        [int]$TimeoutMilliseconds = 5000
    )

    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    while ($watch.ElapsedMilliseconds -lt $TimeoutMilliseconds) {
        try {
            if (Test-Path -LiteralPath $Path -PathType Leaf) {
                # Match the writer's sharing contract while observing an incomplete, still-growing evidence file.
                $readStream = [System.IO.FileStream]::new(
                    $Path,
                    [System.IO.FileMode]::Open,
                    [System.IO.FileAccess]::Read,
                    [System.IO.FileShare]::ReadWrite)
                try {
                    $reader = [System.IO.StreamReader]::new(
                        $readStream,
                        [System.Text.UTF8Encoding]::new($false),
                        $true,
                        4096,
                        $true)
                    try {
                        $capturedText = $reader.ReadToEnd()
                    }
                    finally {
                        $reader.Dispose()
                    }
                }
                finally {
                    $readStream.Dispose()
                }
                if ($capturedText.Contains($Marker, [StringComparison]::Ordinal)) {
                    return $true
                }
            }
        }
        catch [System.IO.IOException] {
            # The next bounded poll observes data after any transient sharing race.
        }
        Start-Sleep -Milliseconds 25
    }
    return $false
}

function Stop-TestHandle {
    <#
    .SYNOPSIS
    Stops and completes one exact test child handle.

    .PARAMETER Handle
    Handle returned by Start-DBNotifierRunnerProcess.

    .OUTPUTS
    System.Int32 containing the retained child exit code.

    .NOTES
    This helper owns only child processes started by this test and uses a bounded process-tree termination.
    #>
    [OutputType([int])]
    param(
        [Parameter(Mandatory)]
        [psobject]$Handle
    )

    if (-not [bool]$Handle.Completed) {
        if (-not $Handle.Process.HasExited) {
            $Handle.Process.Kill($true)
            if (-not $Handle.Process.WaitForExit(5000)) {
                throw 'The exact runner-process test child did not stop within its cleanup budget.'
            }
        }
        return Complete-DBNotifierRunnerProcess -Handle $Handle
    }
    return [int]$Handle.ExitCode
}

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
$childScriptPath = Join-Path $temporaryRoot 'Child Fixture With Spaces.ps1'
$childSource = @'
# Module purpose: Emits deterministic process arguments and bounded output for the runner-process support test.
#Requires -Version 7.0
param(
    [Parameter(Position = 0, Mandatory)]
    [string]$Mode,

    [Parameter(Position = 1, Mandatory)]
    [string]$Marker,

    [Parameter(Position = 2, ValueFromRemainingArguments)]
    [AllowEmptyCollection()]
    [AllowEmptyString()]
    [string[]]$Payload = @()
)

$ErrorActionPreference = 'Stop'
$payloadJson = ConvertTo-Json -InputObject @($Payload) -Compress
$payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payloadJson))
[Console]::Out.WriteLine("READY|$Marker|$payloadBase64")
[Console]::Out.Flush()
[Console]::Error.WriteLine("ERROR|$Marker")
[Console]::Error.Flush()

switch ($Mode) {
    'incremental' {
        Start-Sleep -Milliseconds 1500
        exit 0
    }
    'high-volume' {
        $chunk = 'x' * 512
        for ($index = 0; $index -lt 1024; $index++) {
            [Console]::Out.WriteLine("OUT-$index-$chunk")
            [Console]::Error.WriteLine("ERR-$index-$chunk")
        }
        [Console]::Out.Flush()
        [Console]::Error.Flush()
        exit 23
    }
    'sleep' {
        Start-Sleep -Seconds 30
        exit 0
    }
    default {
        exit 64
    }
}
'@
[System.IO.File]::WriteAllText(
    $childScriptPath,
    $childSource,
    [System.Text.UTF8Encoding]::new($false))

try {
    $pwshCommand = Get-Command pwsh -CommandType Application -ErrorAction Stop |
        Select-Object -First 1
    $pwshPath = [System.IO.Path]::GetFullPath(
        (Resolve-Path -LiteralPath $pwshCommand.Source).Path)

    $resolvedExplicit = Resolve-DBNotifierRunnerExecutable -Candidate $pwshPath
    Assert-ExactValue $pwshPath $resolvedExplicit 'An explicit executable path was not preserved.'
    $resolvedDefault = Resolve-DBNotifierRunnerExecutable -DefaultPath $pwshPath
    Assert-ExactValue $pwshPath $resolvedDefault 'The explicit default executable was not selected.'
    $relativePwshPath = [System.IO.Path]::GetRelativePath($temporaryRoot, $pwshPath)
    $resolvedRelative = Resolve-DBNotifierRunnerExecutable `
        -Candidate $relativePwshPath `
        -BaseDirectory $temporaryRoot
    Assert-ExactValue $pwshPath $resolvedRelative (
        'A relative executable path was not resolved against its explicit base directory.')
    $resolvedApplication = Resolve-DBNotifierRunnerExecutable -Candidate 'pwsh'
    Assert-Condition (
        (Test-Path -LiteralPath $resolvedApplication -PathType Leaf) -and
        [System.IO.Path]::IsPathRooted($resolvedApplication)
    ) 'An application name did not resolve to an available absolute executable.'

    Assert-Throws {
        Resolve-DBNotifierRunnerExecutable -Candidate (
            Join-Path $temporaryRoot 'missing executable.exe')
    } 'A missing explicit executable path did not fail closed.'
    Assert-Throws {
        Resolve-DBNotifierRunnerExecutable -Candidate (
            "dbnotifier-missing-command-$([guid]::NewGuid().ToString('N'))")
    } 'A missing application name did not fail closed.'
    Assert-Throws {
        Resolve-DBNotifierRunnerExecutable -Candidate 'pw*'
    } 'A wildcard application name did not fail closed.'
    Assert-Throws {
        Resolve-DBNotifierRunnerExecutable
    } 'An empty executable selection did not fail closed.'
    Assert-Throws {
        Resolve-DBNotifierRunnerExecutable `
            -Candidate $relativePwshPath `
            -BaseDirectory (Join-Path $temporaryRoot 'missing base')
    } 'An unavailable executable base directory did not fail closed.'

    $marker = [guid]::NewGuid().ToString('D')
    $sentinelPath = Join-Path $temporaryRoot 'shell-must-not-create-this.txt'
    $dangerousArguments = @(
        'plain',
        'value with spaces',
        'quote"inside',
        'asterisk*value',
        'semi;colon',
        '$(throw "shell-expanded")',
        "& [IO.File]::WriteAllText('$sentinelPath','unsafe')",
        '')
    $incrementalOutput = Join-Path $temporaryRoot 'incremental stdout.log'
    $incrementalError = Join-Path $temporaryRoot 'incremental stderr.log'
    $incrementalHandle = Start-DBNotifierRunnerProcess `
        -FilePath $pwshPath `
        -ArgumentList (@(
            '-NoProfile',
            '-NonInteractive',
            '-File',
            $childScriptPath,
            'incremental',
            $marker) + $dangerousArguments) `
        -WorkingDirectory $temporaryRoot `
        -StandardOutputPath $incrementalOutput `
        -StandardErrorPath $incrementalError
    $handles.Add($incrementalHandle)

    $readyMarker = "READY|$marker|"
    Assert-Condition (
        Wait-ForCapturedText -Path $incrementalOutput -Marker $readyMarker
    ) 'Standard output was not captured incrementally.'
    Assert-Condition (
        -not $incrementalHandle.Process.HasExited
    ) 'The incremental marker was observed only after process exit.'
    Assert-Condition (
        Wait-ForCapturedText -Path $incrementalError -Marker "ERROR|$marker"
    ) 'Standard error was not captured incrementally.'
    Assert-Condition (
        -not (Test-Path -LiteralPath $sentinelPath)
    ) 'A dangerous argument was interpreted by a command shell.'

    if (-not $incrementalHandle.Process.WaitForExit(10000)) {
        throw 'The incremental child exceeded its bounded execution period.'
    }
    $incrementalExit = Complete-DBNotifierRunnerProcess -Handle $incrementalHandle
    Assert-ExactValue 0 $incrementalExit 'The natural child exit code was not retained.'
    Assert-ExactValue 0 (
        Complete-DBNotifierRunnerProcess -Handle $incrementalHandle
    ) 'Repeated completion was not idempotent.'

    $readyLine = @(
        Get-Content -LiteralPath $incrementalOutput |
            Where-Object { $_.StartsWith($readyMarker, [StringComparison]::Ordinal) }
    ) | Select-Object -First 1
    Assert-Condition (
        -not [string]::IsNullOrWhiteSpace($readyLine)
    ) 'The exact argument evidence line was unavailable.'
    $encodedPayload = $readyLine.Split('|', 3)[2]
    $payloadJson = [Text.Encoding]::UTF8.GetString(
        [Convert]::FromBase64String($encodedPayload))
    $observedArguments = @($payloadJson | ConvertFrom-Json)
    Assert-ExactValue $dangerousArguments.Count $observedArguments.Count (
        "The dangerous argument count changed across process creation; expected=$($dangerousArguments.Count), " +
        "observed=$($observedArguments.Count).")
    for ($index = 0; $index -lt $dangerousArguments.Count; $index++) {
        Assert-ExactValue $dangerousArguments[$index] $observedArguments[$index] (
            "Dangerous argument $index did not preserve its exact value.")
    }

    $highVolumeOutput = Join-Path $temporaryRoot 'high volume stdout.log'
    $highVolumeError = Join-Path $temporaryRoot 'high volume stderr.log'
    $highVolumeHandle = Start-DBNotifierRunnerProcess `
        -FilePath $pwshPath `
        -ArgumentList @(
            '-NoProfile',
            '-NonInteractive',
            '-File',
            $childScriptPath,
            'high-volume',
            [guid]::NewGuid().ToString('D')) `
        -WorkingDirectory $temporaryRoot `
        -StandardOutputPath $highVolumeOutput `
        -StandardErrorPath $highVolumeError
    $handles.Add($highVolumeHandle)
    if (-not $highVolumeHandle.Process.WaitForExit(30000)) {
        throw 'The high-volume child exceeded its bounded execution period.'
    }
    $highVolumeExit = Complete-DBNotifierRunnerProcess -Handle $highVolumeHandle
    Assert-ExactValue 23 $highVolumeExit 'A non-zero child exit code was not retained.'
    Assert-Condition (
        (Get-Item -LiteralPath $highVolumeOutput).Length -gt 262144
    ) 'High-volume standard output was truncated or blocked.'
    Assert-Condition (
        (Get-Item -LiteralPath $highVolumeError).Length -gt 262144
    ) 'High-volume standard error was truncated or blocked.'

    $sleepOutput = Join-Path $temporaryRoot 'sleep stdout.log'
    $sleepError = Join-Path $temporaryRoot 'sleep stderr.log'
    $sleepHandle = Start-DBNotifierRunnerProcess `
        -FilePath $pwshPath `
        -ArgumentList @(
            '-NoProfile',
            '-NonInteractive',
            '-File',
            $childScriptPath,
            'sleep',
            [guid]::NewGuid().ToString('D')) `
        -WorkingDirectory $temporaryRoot `
        -StandardOutputPath $sleepOutput `
        -StandardErrorPath $sleepError
    $handles.Add($sleepHandle)
    Assert-Condition (
        Wait-ForCapturedText -Path $sleepOutput -Marker 'READY|'
    ) 'The cleanup child did not become ready.'
    $stoppedExit = Stop-TestHandle -Handle $sleepHandle
    Assert-ExactValue $stoppedExit (
        Complete-DBNotifierRunnerProcess -Handle $sleepHandle
    ) 'Completion after caller-owned termination was not idempotent.'

    $failedCaptureOutput = Join-Path $temporaryRoot 'failed capture stdout.log'
    $failedCaptureError = Join-Path $temporaryRoot 'failed capture stderr.log'
    $failedCaptureHandle = Start-DBNotifierRunnerProcess `
        -FilePath $pwshPath `
        -ArgumentList @(
            '-NoProfile',
            '-NonInteractive',
            '-Command',
            '[Console]::Out.WriteLine("capture"); exit 0') `
        -WorkingDirectory $temporaryRoot `
        -StandardOutputPath $failedCaptureOutput `
        -StandardErrorPath $failedCaptureError
    $handles.Add($failedCaptureHandle)
    if (-not $failedCaptureHandle.Process.WaitForExit(10000)) {
        throw 'The capture-failure child exceeded its bounded execution period.'
    }
    $failedCaptureHandle.StandardOutputStream.Dispose()
    Assert-Throws {
        Complete-DBNotifierRunnerProcess -Handle $failedCaptureHandle
    } 'A capture-integrity failure did not fail closed.'
    Assert-Throws {
        Complete-DBNotifierRunnerProcess -Handle $failedCaptureHandle
    } 'A repeated completion hid the recorded capture-integrity failure.'

    $caseOutput = Join-Path $temporaryRoot 'case-sensitive.log'
    $caseError = Join-Path $temporaryRoot 'CASE-SENSITIVE.log'
    if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
        Assert-Throws {
            Start-DBNotifierRunnerProcess `
                -FilePath $pwshPath `
                -ArgumentList @('-NoProfile', '-NonInteractive', '-Command', 'exit 0') `
                -WorkingDirectory $temporaryRoot `
                -StandardOutputPath $caseOutput `
                -StandardErrorPath $caseError
        } 'Windows did not reject case-equivalent evidence paths.'
    }
    else {
        $caseHandle = Start-DBNotifierRunnerProcess `
            -FilePath $pwshPath `
            -ArgumentList @('-NoProfile', '-NonInteractive', '-Command', 'exit 0') `
            -WorkingDirectory $temporaryRoot `
            -StandardOutputPath $caseOutput `
            -StandardErrorPath $caseError
        $handles.Add($caseHandle)
        if (-not $caseHandle.Process.WaitForExit(10000)) {
            throw 'The case-sensitive child exceeded its bounded execution period.'
        }
        Assert-ExactValue 0 (
            Complete-DBNotifierRunnerProcess -Handle $caseHandle
        ) 'A case-sensitive filesystem rejected distinct evidence paths.'
        Assert-Condition (
            (Test-Path -LiteralPath $caseOutput -PathType Leaf) -and
            (Test-Path -LiteralPath $caseError -PathType Leaf)
        ) 'Distinct case-sensitive evidence files were not created.'
    }

    $existingOutput = Join-Path $temporaryRoot 'existing stdout.log'
    [System.IO.File]::WriteAllText(
        $existingOutput,
        'preserve',
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws {
        Start-DBNotifierRunnerProcess `
            -FilePath $pwshPath `
            -ArgumentList @('-NoProfile', '-Command', 'exit 0') `
            -WorkingDirectory $temporaryRoot `
            -StandardOutputPath $existingOutput `
            -StandardErrorPath (Join-Path $temporaryRoot 'unused stderr.log')
    } 'An existing evidence file did not fail closed before process creation.'
    Assert-ExactValue 'preserve' (
        [System.IO.File]::ReadAllText($existingOutput)
    ) 'A fail-closed start overwrote existing evidence.'

    $humanRunnerPath = Join-Path $root 'scripts/run-state06-final-human-review.ps1'
    $humanRunnerTokens = $null
    $humanRunnerErrors = $null
    $humanRunnerAst = [System.Management.Automation.Language.Parser]::ParseFile(
        $humanRunnerPath,
        [ref]$humanRunnerTokens,
        [ref]$humanRunnerErrors)
    Assert-ExactValue 0 $humanRunnerErrors.Count 'The visible review runner did not parse for relay testing.'
    $relayFunctionAst = @($humanRunnerAst.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -eq 'Write-NewPresenterLines'
    }, $true)) | Select-Object -First 1
    Assert-Condition ($null -ne $relayFunctionAst) 'The completed-line presenter relay was unavailable.'
    . ([scriptblock]::Create($relayFunctionAst.Extent.Text))

    $relayPath = Join-Path $temporaryRoot 'presenter incremental stdout.log'
    [System.IO.File]::WriteAllText(
        $relayPath,
        "first`npartial",
        [System.Text.UTF8Encoding]::new($false))
    $relayCursor = 0
    $firstRelay = @(Write-NewPresenterLines -Path $relayPath -NextCharacter ([ref]$relayCursor))
    Assert-Condition (
        $firstRelay.Count -eq 1 -and $firstRelay[0] -ceq 'first'
    ) 'The presenter relay did not retain its incomplete final line.'
    [System.IO.File]::AppendAllText(
        $relayPath,
        "-rest`n",
        [System.Text.UTF8Encoding]::new($false))
    $secondRelay = @(Write-NewPresenterLines -Path $relayPath -NextCharacter ([ref]$relayCursor))
    Assert-Condition (
        $secondRelay.Count -eq 1 -and $secondRelay[0] -ceq 'partial-rest'
    ) 'The presenter relay lost the completion of a previously partial line.'
    [System.IO.File]::AppendAllText(
        $relayPath,
        'final-tail',
        [System.Text.UTF8Encoding]::new($false))
    $finalRelay = @(
        Write-NewPresenterLines `
            -Path $relayPath `
            -NextCharacter ([ref]$relayCursor) `
            -EndOfStream)
    Assert-Condition (
        $finalRelay.Count -eq 1 -and $finalRelay[0] -ceq 'final-tail'
    ) 'The presenter relay did not emit its final unterminated line at end-of-stream.'

    $runnerPaths = @(
        'scripts/run-o4-observer-browser-audit.ps1',
        'scripts/run-state06-consolidated-e2e.ps1',
        'scripts/run-state06-dashboard-tv-browser-e2e.ps1',
        'scripts/run-state06-final-human-review.ps1')
    foreach ($runnerPath in $runnerPaths) {
        $runnerSource = [System.IO.File]::ReadAllText((Join-Path $root $runnerPath))
        $runnerTokens = $null
        $runnerErrors = $null
        $runnerAst = [System.Management.Automation.Language.Parser]::ParseInput(
            $runnerSource,
            [ref]$runnerTokens,
            [ref]$runnerErrors)
        Assert-ExactValue 0 $runnerErrors.Count "$runnerPath does not parse for structural inspection."
        $runnerCommands = @($runnerAst.FindAll({
            param($node)
            $node -is [System.Management.Automation.Language.CommandAst]
        }, $true))
        Assert-Condition (
            $runnerSource.Contains(
                "[string]`$DotNetPath = '.dotnet\dotnet.exe'",
                [StringComparison]::Ordinal)
        ) "$runnerPath does not expose the repository-local SDK default."
        Assert-Condition (
            $runnerSource.Contains(
                "Import-Module (Join-Path `$PSScriptRoot 'DBNotifier.RunnerProcess.psm1')",
                [StringComparison]::Ordinal)
        ) "$runnerPath does not import the shared process boundary."
        $sharedLaunches = @($runnerCommands | Where-Object {
            $_.GetCommandName() -eq 'Start-DBNotifierRunnerProcess'
        })
        Assert-ExactValue 3 $sharedLaunches.Count (
            "$runnerPath does not route its host, browser and Node processes through the shared boundary.")
        $forbiddenLaunches = @($runnerCommands | Where-Object {
            $_.GetCommandName() -in @('Start-Process', 'Invoke-Expression')
        })
        Assert-ExactValue 0 $forbiddenLaunches.Count (
            "$runnerPath retains a shell-like or flattened process launcher.")
        $directResolvedNodeInvocations = @($runnerCommands | Where-Object {
            $_.InvocationOperator -eq [System.Management.Automation.Language.TokenKind]::Ampersand -and
            $_.CommandElements.Count -gt 0 -and
            $_.CommandElements[0] -is [System.Management.Automation.Language.VariableExpressionAst] -and
            $_.CommandElements[0].VariablePath.UserPath -eq 'resolvedNode'
        })
        Assert-ExactValue 0 $directResolvedNodeInvocations.Count (
            "$runnerPath bypasses the owned process boundary for its Node child.")
        Assert-Condition (
            -not $runnerSource.Contains(
                '''"--host-resolver-rules=',
                [StringComparison]::Ordinal)
        ) "$runnerPath retains literal quotes inside one Chromium argument."
    }

    Write-Output (
        "Runner-process support tests passed: assertions=$assertionCount; " +
        'dangerous-arguments=8; concurrent-streams=2; owned-residue=0.')
}
finally {
    $cleanupFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($handle in $handles) {
        try {
            [void](Stop-TestHandle -Handle $handle)
        }
        catch {
            $cleanupFailures.Add('One exact runner-process test child could not be released.')
        }
    }

    if (Test-Path -LiteralPath $temporaryRoot) {
        $safeRoot =
            $temporaryRoot.StartsWith(
                $systemTemporaryRoot + [System.IO.Path]::DirectorySeparatorChar,
                $pathComparison) -and
            ([System.IO.Path]::GetFileName($temporaryRoot)).Equals(
                $temporaryLeaf,
                [StringComparison]::Ordinal)
        $rootItem = Get-Item -LiteralPath $temporaryRoot -Force
        $reparseEntries = @(
            Get-ChildItem -LiteralPath $temporaryRoot -Force -Recurse |
                Where-Object {
                    ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
                }
        )
        if (-not $safeRoot -or
            ($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
            $reparseEntries.Count -ne 0) {
            $cleanupFailures.Add('The exact test root failed its recursive-removal ownership check.')
        }
        else {
            [System.IO.Directory]::Delete($temporaryRoot, $true)
        }
    }

    if (Test-Path -LiteralPath $temporaryRoot) {
        $cleanupFailures.Add('The exact runner-process test root remains after cleanup.')
    }
    if ($cleanupFailures.Count -ne 0) {
        throw 'Runner-process support test cleanup failed.'
    }
}
