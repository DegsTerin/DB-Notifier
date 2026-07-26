# Module purpose: Runs the bounded R-EGRESS/R-FENCE multiprocess matrix against one pinned, local-only disposable PostgreSQL container.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DotNetPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root '.dotnet\dotnet.exe'
}
if (-not (Test-Path -LiteralPath $DotNetPath -PathType Leaf)) {
    throw 'The requested .NET executable is unavailable.'
}

$resolvedDotNet = (Resolve-Path -LiteralPath $DotNetPath).Path
$sandboxHostPath = [System.IO.Path]::GetFullPath(
    (Join-Path $root (
        'tests\DBNotifier.ServerConcurrency.SandboxHost\bin\Release\net10.0\' +
        'DBNotifier.ServerConcurrency.SandboxHost.dll')))
$dockerCommand = Get-Command docker.exe -CommandType Application -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($null -eq $dockerCommand) {
    throw 'The local Docker command is unavailable.'
}
$docker = $dockerCommand.Path
$imageTag = 'postgres:16-alpine'
$imageDigest = 'sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb'
$runId = [guid]::NewGuid().ToString('N')
$runToken = $runId.Substring(0, 12)
$ownershipLabelName = 'com.db-notifier.r-egress-fence'
$runLabelName = 'com.db-notifier.r-egress-fence.run'
$containerName = "db-notifier-r-egress-fence-$runId"
$networkName = "db-notifier-r-egress-fence-net-$runId"
$volumeName = "db-notifier-r-egress-fence-data-$runId"
$systemTemporaryRoot = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::GetTempPath()).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
$temporaryLeaf = "DBNotifier-R-Egress-Fence-$runId"
$temporaryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $systemTemporaryRoot $temporaryLeaf))
$passwordPath = Join-Path $temporaryRoot 'postgres-password.txt'
$summaryPath = Join-Path $temporaryRoot 'r-egress-postgresql-fence-summary.json'
$expectedTemporaryRoot = Join-Path $systemTemporaryRoot $temporaryLeaf
$containerProvisioningAttempted = $false
$networkProvisioningAttempted = $false
$volumeProvisioningAttempted = $false
$dockerEngineVerified = $false
$executionFailed = $false
$failureStage = 'initial validation'
$testExitCode = 1
$summaryEmitted = $false
$cleanupFailures = [System.Collections.Generic.List[string]]::new()
$password = $null
$connectionString = $null
$testProcess = $null

# Runs a Docker command whose arguments contain no credentials and rejects every non-zero result.
# Parameters: Arguments is the exact bounded Docker argument vector.
# Returns: The command output as an array of strings.
# Failures: Throws a sanitised error without echoing arguments or native error output.
function Invoke-DockerChecked {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = @(& $docker @Arguments 2>$null)
    if ($LASTEXITCODE -ne 0) {
        throw 'A bounded Docker operation failed.'
    }

    return $output
}

# Reads both ownership labels without trusting a resource name as proof of ownership.
# Parameters: ResourceType selects the supported Docker object and ResourceName is its exact generated name.
# Returns: True only when both labels match this runner and this invocation.
# Failures: Docker inspection failures are treated as an unproved resource, never as permission to remove it.
function Test-ExactResourceOwnership {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network', 'volume')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName
    )

    $template = if ($ResourceType -eq 'container') {
        '{{index .Config.Labels "com.db-notifier.r-egress-fence"}}|{{index .Config.Labels "com.db-notifier.r-egress-fence.run"}}'
    }
    else {
        '{{index .Labels "com.db-notifier.r-egress-fence"}}|{{index .Labels "com.db-notifier.r-egress-fence.run"}}'
    }
    $labels = @(
        & $docker $ResourceType inspect --format $template $ResourceName 2>$null
    )

    return $LASTEXITCODE -eq 0 -and
        $labels.Count -eq 1 -and
        $labels[0] -ceq "true|$runId"
}

# Determines whether the exact generated Docker resource name currently exists.
# Parameters: ResourceType and ResourceName identify one bounded object.
# Returns: True only when Docker returns exactly one identifier.
# Failures: Inspection errors return false; the final labelled-resource audit remains authoritative.
function Test-DockerResourceExistence {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network', 'volume')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName
    )

    $template = if ($ResourceType -eq 'volume') { '{{.Name}}' } else { '{{.Id}}' }
    $identity = @(& $docker $ResourceType inspect --format $template $ResourceName 2>$null)
    return $LASTEXITCODE -eq 0 -and
        $identity.Count -eq 1 -and
        -not [string]::IsNullOrWhiteSpace($identity[0])
}

# Removes one resource only after both exact labels prove ownership by this invocation.
# Parameters: ResourceType and ResourceName identify the object; ProvisioningAttempted records whether creation began.
# Returns: No value; cleanup findings are accumulated for final fail-closed reporting.
# Failures: A present but unowned or unremovable resource is retained and reported without revealing native output.
function Remove-OwnedDockerResource {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network', 'volume')]
        [string]$ResourceType,

        [Parameter(Mandatory)]
        [string]$ResourceName,

        [Parameter(Mandatory)]
        [bool]$ProvisioningAttempted
    )

    if (-not $ProvisioningAttempted) {
        return
    }

    if (-not (Test-ExactResourceOwnership $ResourceType $ResourceName)) {
        if (Test-DockerResourceExistence $ResourceType $ResourceName) {
            $cleanupFailures.Add(
                "Ownership of the generated $ResourceType could not be proved.")
        }
        return
    }

    if ($PSCmdlet.ShouldProcess($ResourceName, "Remove owned $ResourceType")) {
        if ($ResourceType -eq 'container') {
            & $docker container rm --force $ResourceName 2>$null | Out-Null
        }
        elseif ($ResourceType -eq 'volume') {
            & $docker volume rm $ResourceName 2>$null | Out-Null
        }
        else {
            & $docker network rm $ResourceName 2>$null | Out-Null
        }
    }

    if ($LASTEXITCODE -ne 0 -or
        (Test-DockerResourceExistence $ResourceType $ResourceName)) {
        $cleanupFailures.Add("The owned $ResourceType could not be removed.")
    }
}

# Returns only sandbox-host process records bearing this invocation's exact unguessable command-line token.
# Returns: Current Win32 process records; an inventory error is converted into a cleanup finding.
function Get-OwnedSandboxProcess {
    try {
        return @(
            Get-CimInstance Win32_Process -ErrorAction Stop |
                Where-Object {
                    $commandLine = [string]$_.CommandLine
                    -not [string]::IsNullOrWhiteSpace($commandLine) -and
                    $commandLine.Contains(
                        $sandboxHostPath,
                        [StringComparison]::OrdinalIgnoreCase) -and
                    $commandLine.Contains(
                        '--sandbox-r-egress-fence',
                        [StringComparison]::Ordinal) -and
                    $commandLine.Contains(
                        "dbn-ref-$runToken-",
                        [StringComparison]::Ordinal)
                }
        )
    }
    catch {
        $cleanupFailures.Add(
            'Owned sandbox child-process inventory failed during final cleanup.')
        return @()
    }
}

# Stops every process still carrying this invocation's exact host, marker and token, then proves zero residue.
# Failures: Never stops an unproved PID and accumulates bounded findings after attempting every owned process.
function Stop-OwnedSandboxProcess {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    foreach ($record in @(Get-OwnedSandboxProcess)) {
        $processId = [int]$record.ProcessId
        try {
            $current = @(
                Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction Stop |
                    Where-Object {
                        $commandLine = [string]$_.CommandLine
                        -not [string]::IsNullOrWhiteSpace($commandLine) -and
                        $commandLine.Contains(
                            $sandboxHostPath,
                            [StringComparison]::OrdinalIgnoreCase) -and
                        $commandLine.Contains(
                            '--sandbox-r-egress-fence',
                            [StringComparison]::Ordinal) -and
                        $commandLine.Contains(
                            "dbn-ref-$runToken-",
                            [StringComparison]::Ordinal)
                    }
            )
            if ($current.Count -eq 0) {
                continue
            }
            if ($current.Count -ne 1) {
                $cleanupFailures.Add(
                    'Owned sandbox child-process identity became ambiguous during cleanup.')
                continue
            }

            $process = [System.Diagnostics.Process]::GetProcessById($processId)
            try {
                if (-not $process.HasExited -and
                    $PSCmdlet.ShouldProcess(
                        "PID $processId",
                        'Stop exact owned sandbox process tree')) {
                    $process.Kill($true)
                }
                if (-not $process.WaitForExit(10000)) {
                    $cleanupFailures.Add(
                        'An exact owned sandbox child did not exit during final cleanup.')
                }
            }
            finally {
                $process.Dispose()
            }
        }
        catch {
            $stillOwned = @(
                Get-OwnedSandboxProcess |
                    Where-Object { [int]$_.ProcessId -eq $processId }
            )
            if ($stillOwned.Count -ne 0) {
                $cleanupFailures.Add(
                    'An exact owned sandbox child could not be revalidated during final cleanup.')
            }
        }
    }

    if (@(Get-OwnedSandboxProcess).Count -ne 0) {
        $cleanupFailures.Add(
            'Owned sandbox child-process residue remained after final cleanup.')
    }
}

# Executes the marker-gated integration test under an external total watchdog with redirected output.
# Parameters: DotNetPath and ProjectPath select exact local files; SecretValue must never reach emitted output.
# Returns: Exit code plus buffered stdout/stderr after both streams have been checked for the secret.
# Failures: Kills only the exact owned process tree on timeout and preserves its handle if exit cannot be proved.
function Invoke-BoundedDotNetTest {
    param(
        [Parameter(Mandatory)]
        [string]$DotNetPath,

        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$SecretValue,

        [ValidateRange(30, 900)]
        [int]$TimeoutSeconds = 600
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $DotNetPath
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in @(
            'test',
            $ProjectPath,
            '--configuration',
            'Release',
            '--no-build',
            '--no-restore',
            '--filter',
            'FullyQualifiedName~REgressPostgreSqlMultiprocessFenceTests',
            '--logger',
            'console;verbosity=minimal'
        )) {
        $startInfo.ArgumentList.Add($argument)
    }

    $script:testProcess = [System.Diagnostics.Process]::new()
    $script:testProcess.StartInfo = $startInfo
    $testProcessStarted = $false
    try {
        if (-not $script:testProcess.Start()) {
            throw 'The bounded integration test process could not be started.'
        }
        $testProcessStarted = $true

        $standardOutputTask = $script:testProcess.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $script:testProcess.StandardError.ReadToEndAsync()
        $timeoutMilliseconds = [int]($TimeoutSeconds * 1000)
        $timedOut = -not $script:testProcess.WaitForExit($timeoutMilliseconds)
        if ($timedOut) {
            try {
                $script:testProcess.Kill($true)
            }
            catch {
                throw 'The bounded integration test process could not be terminated after timeout.'
            }
            if (-not $script:testProcess.WaitForExit(10000)) {
                throw 'The bounded integration test process exit could not be proved after timeout.'
            }
        }

        $standardOutput = $standardOutputTask.GetAwaiter().GetResult()
        $standardError = $standardErrorTask.GetAwaiter().GetResult()
        if ($standardOutput.Length -gt 2097152 -or $standardError.Length -gt 2097152) {
            throw 'The bounded integration test output exceeded its retained limit.'
        }
        $combinedOutput = $standardOutput + [Environment]::NewLine + $standardError
        if ($combinedOutput.Contains($SecretValue, [StringComparison]::Ordinal) -or
            $combinedOutput -match '(?i)(DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION|password\s*=)') {
            throw 'The bounded integration test output contains prohibited connection material.'
        }

        $result = [pscustomobject]@{
            ExitCode = $script:testProcess.ExitCode
            StandardOutput = $standardOutput
            StandardError = $standardError
        }
        $script:testProcess.Dispose()
        $script:testProcess = $null
        if ($timedOut) {
            throw 'The bounded integration test exceeded its total watchdog.'
        }
        return $result
    }
    catch {
        if ($null -ne $script:testProcess -and
            (-not $testProcessStarted -or $script:testProcess.HasExited)) {
            $script:testProcess.Dispose()
            $script:testProcess = $null
        }
        throw
    }
}

# Converts a JSON number to a bounded integer while refusing strings, booleans and non-finite values.
# Parameters: Value is the parsed JSON value, Name identifies the sanitised metric, and Positive requires a value above zero.
# Returns: A validated Int64 suitable for deterministic summary reconstruction.
# Failures: Throws when the metric is missing, fractional, negative, non-finite or outside Int64.
function ConvertTo-SummaryInteger {
    param(
        [Parameter(Mandatory)]
        [object]$Value,

        [Parameter(Mandatory)]
        [string]$Name,

        [switch]$Positive
    )

    $valueType = $Value.GetType()
    if ($Value -is [bool] -or
        $Value -is [char] -or
        (-not $valueType.IsPrimitive -and $Value -isnot [decimal])) {
        throw "Summary metric $Name is not an integer."
    }

    $number = [double]$Value
    if ([double]::IsNaN($number) -or
        [double]::IsInfinity($number) -or
        $number -lt 0 -or
        $number -ne [Math]::Floor($number) -or
        $number -gt [long]::MaxValue) {
        throw "Summary metric $Name is outside its allowed range."
    }
    if ($Positive -and $number -le 0) {
        throw "Summary metric $Name must be positive."
    }

    return [long]$number
}

# Converts a JSON number to a finite non-negative double without accepting string coercion.
# Parameters: Value is the parsed JSON value, Name identifies the sanitised metric, and Positive requires a value above zero.
# Returns: A validated Double suitable for deterministic summary reconstruction.
# Failures: Throws when the metric is not numeric, finite or within the accepted range.
function ConvertTo-SummaryNumber {
    param(
        [Parameter(Mandatory)]
        [object]$Value,

        [Parameter(Mandatory)]
        [string]$Name,

        [switch]$Positive
    )

    $valueType = $Value.GetType()
    if ($Value -is [bool] -or
        $Value -is [char] -or
        (-not $valueType.IsPrimitive -and $Value -isnot [decimal])) {
        throw "Summary metric $Name is not numeric."
    }

    $number = [double]$Value
    if ([double]::IsNaN($number) -or
        [double]::IsInfinity($number) -or
        $number -lt 0) {
        throw "Summary metric $Name is outside its allowed range."
    }
    if ($Positive -and $number -le 0) {
        throw "Summary metric $Name must be positive."
    }

    return $number
}

# Validates one bounded load profile and reconstructs only its approved numeric metrics.
# Parameters: LoadProfile is the parsed profile object and Name identifies its same-Agent or distinct-Agent source.
# Returns: An ordered dictionary with operation, contention, latency and throughput evidence.
# Failures: Rejects unexpected fields, invalid values, non-monotonic percentiles and any reported deadlock.
function ConvertTo-SanitisedLoadProfile {
    param(
        [Parameter(Mandatory)]
        [System.Collections.IDictionary]$LoadProfile,

        [Parameter(Mandatory)]
        [string]$Name
    )

    $expectedProfileKeys = @(
        'deadlockCount',
        'latencyMilliseconds',
        'maximumBlockedCount',
        'operationCount',
        'retryableCount',
        'serializationFailureCount',
        'throughputPerSecond'
    )
    $actualProfileKeys = @($LoadProfile.Keys | ForEach-Object { [string]$_ } | Sort-Object)
    $profileKeyDifference = @(
        Compare-Object `
            -ReferenceObject ($expectedProfileKeys | Sort-Object) `
            -DifferenceObject $actualProfileKeys `
            -CaseSensitive
    )
    if ($profileKeyDifference.Count -ne 0) {
        throw "Summary profile $Name contains an unexpected schema."
    }

    $operationCount = ConvertTo-SummaryInteger `
        $LoadProfile['operationCount'] `
        "$Name.operationCount" `
        -Positive
    $serializationFailureCount = ConvertTo-SummaryInteger `
        $LoadProfile['serializationFailureCount'] `
        "$Name.serializationFailureCount"
    $retryableCount = ConvertTo-SummaryInteger `
        $LoadProfile['retryableCount'] `
        "$Name.retryableCount"
    $deadlockCount = ConvertTo-SummaryInteger `
        $LoadProfile['deadlockCount'] `
        "$Name.deadlockCount"
    $maximumBlockedCount = ConvertTo-SummaryInteger `
        $LoadProfile['maximumBlockedCount'] `
        "$Name.maximumBlockedCount" `
        -Positive
    $throughputPerSecond = ConvertTo-SummaryNumber `
        $LoadProfile['throughputPerSecond'] `
        "$Name.throughputPerSecond" `
        -Positive
    if ($deadlockCount -ne 0) {
        throw "Summary profile $Name reports a deadlock."
    }
    if ($operationCount -ne 100) {
        throw "Summary profile $Name does not contain the exact bounded operation count."
    }
    if ($retryableCount -gt $operationCount) {
        throw "Summary profile $Name reports more retryable results than operations."
    }

    $latency = $LoadProfile['latencyMilliseconds']
    if ($latency -isnot [System.Collections.IDictionary]) {
        throw "Summary profile $Name has an invalid latency object."
    }
    $expectedLatencyKeys = @('max', 'p50', 'p95', 'p99')
    $actualLatencyKeys = @($latency.Keys | ForEach-Object { [string]$_ } | Sort-Object)
    $latencyKeyDifference = @(
        Compare-Object `
            -ReferenceObject ($expectedLatencyKeys | Sort-Object) `
            -DifferenceObject $actualLatencyKeys `
            -CaseSensitive
    )
    if ($latencyKeyDifference.Count -ne 0) {
        throw "Summary profile $Name has an unexpected latency schema."
    }

    $p50 = ConvertTo-SummaryNumber $latency['p50'] "$Name.latencyMilliseconds.p50"
    $p95 = ConvertTo-SummaryNumber $latency['p95'] "$Name.latencyMilliseconds.p95"
    $p99 = ConvertTo-SummaryNumber $latency['p99'] "$Name.latencyMilliseconds.p99"
    $maximum = ConvertTo-SummaryNumber $latency['max'] "$Name.latencyMilliseconds.max"
    if ($p50 -gt $p95 -or $p95 -gt $p99 -or $p99 -gt $maximum) {
        throw "Summary profile $Name has non-monotonic latency percentiles."
    }

    return [ordered]@{
        operationCount = $operationCount
        serializationFailureCount = $serializationFailureCount
        retryableCount = $retryableCount
        deadlockCount = $deadlockCount
        maximumBlockedCount = $maximumBlockedCount
        latencyMilliseconds = [ordered]@{
            p50 = $p50
            p95 = $p95
            p99 = $p99
            max = $maximum
        }
        throughputPerSecond = $throughputPerSecond
    }
}

# Validates the atomic test summary and reconstructs an allowlisted representation for console evidence.
# Parameters: Path is the exact owned summary path and SecretValue is rejected if it appears in the file.
# Returns: Compact JSON containing only the approved numeric schema in a stable property order.
# Failures: Rejects links, oversized or malformed content, unexpected fields, unsafe values and failed correctness counters.
function Read-SanitisedFenceSummary {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$SecretValue
    )

    $expectedPath = [System.IO.Path]::GetFullPath($summaryPath)
    $candidatePath = [System.IO.Path]::GetFullPath($Path)
    $ownedPrefix = $temporaryRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    if (-not $candidatePath.Equals($expectedPath, [StringComparison]::OrdinalIgnoreCase) -or
        -not $candidatePath.StartsWith($ownedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The PostgreSQL fence summary path is outside the owned temporary root.'
    }
    if (-not (Test-Path -LiteralPath $candidatePath -PathType Leaf)) {
        throw 'The PostgreSQL fence summary was not produced.'
    }

    $summaryItem = Get-Item -LiteralPath $candidatePath -Force
    if (($summaryItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        $summaryItem.Length -lt 2 -or
        $summaryItem.Length -gt 65536) {
        throw 'The PostgreSQL fence summary is not a bounded regular file.'
    }

    $summaryText = [System.IO.File]::ReadAllText(
        $candidatePath,
        [System.Text.UTF8Encoding]::new($false, $true))
    if ($summaryText.Contains($SecretValue, [StringComparison]::Ordinal) -or
        $summaryText -match '(?i)(password|pwd|connection\s*string|DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION)') {
        throw 'The PostgreSQL fence summary contains prohibited connection material.'
    }

    $summary = $summaryText | ConvertFrom-Json -AsHashtable -NoEnumerate
    if ($summary -isnot [System.Collections.IDictionary]) {
        throw 'The PostgreSQL fence summary root is invalid.'
    }

    $expectedKeys = @(
        'acceptedCount',
        'blockingObservationCount',
        'convergenceOperationCount',
        'deadlockCount',
        'distinctAgentLoad',
        'rejectedCount',
        'residualResourceCount',
        'revocationCompletionPosition',
        'retryableCount',
        'sameAgentLoad',
        'scenarioCount',
        'schemaVersion',
        'serializationFailureCount',
        'unclassifiedFailureCount'
    )
    $actualKeys = @($summary.Keys | ForEach-Object { [string]$_ } | Sort-Object)
    $keyDifference = @(
        Compare-Object `
            -ReferenceObject ($expectedKeys | Sort-Object) `
            -DifferenceObject $actualKeys `
            -CaseSensitive
    )
    if ($keyDifference.Count -ne 0) {
        throw 'The PostgreSQL fence summary contains an unexpected schema.'
    }

    $schemaVersion = ConvertTo-SummaryInteger $summary['schemaVersion'] 'schemaVersion'
    $scenarioCount = ConvertTo-SummaryInteger $summary['scenarioCount'] 'scenarioCount' -Positive
    $blockingObservationCount = ConvertTo-SummaryInteger `
        $summary['blockingObservationCount'] `
        'blockingObservationCount' `
        -Positive
    $serializationFailureCount = ConvertTo-SummaryInteger `
        $summary['serializationFailureCount'] `
        'serializationFailureCount' `
        -Positive
    $acceptedCount = ConvertTo-SummaryInteger $summary['acceptedCount'] 'acceptedCount' -Positive
    $rejectedCount = ConvertTo-SummaryInteger `
        $summary['rejectedCount'] `
        'rejectedCount' `
        -Positive
    $retryableCount = ConvertTo-SummaryInteger `
        $summary['retryableCount'] `
        'retryableCount' `
        -Positive
    $convergenceOperationCount = ConvertTo-SummaryInteger `
        $summary['convergenceOperationCount'] `
        'convergenceOperationCount' `
        -Positive
    $deadlockCount = ConvertTo-SummaryInteger $summary['deadlockCount'] 'deadlockCount'
    $unclassifiedFailureCount = ConvertTo-SummaryInteger `
        $summary['unclassifiedFailureCount'] `
        'unclassifiedFailureCount'
    $revocationCompletionPosition = ConvertTo-SummaryInteger `
        $summary['revocationCompletionPosition'] `
        'revocationCompletionPosition'
    $residualResourceCount = ConvertTo-SummaryInteger `
        $summary['residualResourceCount'] `
        'residualResourceCount'

    if ($schemaVersion -ne 2 -or
        $scenarioCount -ne 8 -or
        $revocationCompletionPosition -gt 24 -or
        $retryableCount -ne $convergenceOperationCount -or
        $deadlockCount -ne 0 -or
        $unclassifiedFailureCount -ne 0 -or
        $residualResourceCount -ne 0) {
        throw 'The PostgreSQL fence summary reports a failed correctness invariant.'
    }

    $sameAgentLoad = $summary['sameAgentLoad']
    $distinctAgentLoad = $summary['distinctAgentLoad']
    if ($sameAgentLoad -isnot [System.Collections.IDictionary] -or
        $distinctAgentLoad -isnot [System.Collections.IDictionary]) {
        throw 'The PostgreSQL fence load profiles are invalid.'
    }
    $sanitisedSameAgentLoad = ConvertTo-SanitisedLoadProfile `
        $sameAgentLoad `
        'sameAgentLoad'
    $sanitisedDistinctAgentLoad = ConvertTo-SanitisedLoadProfile `
        $distinctAgentLoad `
        'distinctAgentLoad'

    $sanitised = [ordered]@{
        schemaVersion = $schemaVersion
        scenarioCount = $scenarioCount
        blockingObservationCount = $blockingObservationCount
        serializationFailureCount = $serializationFailureCount
        acceptedCount = $acceptedCount
        rejectedCount = $rejectedCount
        retryableCount = $retryableCount
        convergenceOperationCount = $convergenceOperationCount
        deadlockCount = $deadlockCount
        unclassifiedFailureCount = $unclassifiedFailureCount
        sameAgentLoad = $sanitisedSameAgentLoad
        distinctAgentLoad = $sanitisedDistinctAgentLoad
        revocationCompletionPosition = $revocationCompletionPosition
        residualResourceCount = $residualResourceCount
    }
    return ($sanitised | ConvertTo-Json -Compress -Depth 3)
}

# Counts resources bearing the exact lot label without treating names as trusted evidence.
# Parameters: ResourceType selects containers, networks or volumes; CurrentRunOnly also requires this invocation's run label.
# Returns: The number of matching Docker resources.
# Failures: Throws when Docker cannot prove the audit result.
function Get-LabelledResourceCount {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('container', 'network', 'volume')]
        [string]$ResourceType,

        [switch]$CurrentRunOnly
    )

    $arguments = if ($ResourceType -eq 'container') {
        @('ps', '-a')
    }
    elseif ($ResourceType -eq 'network') {
        @('network', 'ls')
    }
    else {
        @('volume', 'ls')
    }
    $arguments += @('--filter', "label=$ownershipLabelName=true")
    if ($CurrentRunOnly) {
        $arguments += @('--filter', "label=$runLabelName=$runId")
    }
    $format = if ($ResourceType -eq 'volume') { '{{.Name}}' } else { '{{.ID}}' }
    $arguments += @('--format', $format)

    $resources = @(& $docker @arguments 2>$null)
    if ($LASTEXITCODE -ne 0) {
        throw 'The final labelled-resource audit could not be completed.'
    }

    return $resources.Count
}

try {
    if (-not $temporaryRoot.Equals(
            [System.IO.Path]::GetFullPath($expectedTemporaryRoot),
            [StringComparison]::OrdinalIgnoreCase) -or
        -not ([System.IO.Path]::GetDirectoryName($temporaryRoot)).Equals(
            $systemTemporaryRoot,
            [StringComparison]::OrdinalIgnoreCase) -or
        -not ([System.IO.Path]::GetFileName($temporaryRoot)).Equals(
            $temporaryLeaf,
            [StringComparison]::Ordinal)) {
        throw 'The generated temporary root failed validation.'
    }

    $failureStage = 'Docker engine validation'
    $serverVersion = @(& $docker version --format '{{.Server.Version}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or
        $serverVersion.Count -ne 1 -or
        [string]::IsNullOrWhiteSpace($serverVersion[0])) {
        throw 'The local Docker engine is unavailable.'
    }
    $dockerEngineVerified = $true

    $failureStage = 'pinned image validation'
    $imageIdentity = @(& $docker image inspect $imageTag --format '{{.Id}}' 2>$null)
    $imageTagsJson = @(& $docker image inspect $imageTag --format '{{json .RepoTags}}' 2>$null)
    $imageDigestsJson = @(& $docker image inspect $imageTag --format '{{json .RepoDigests}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or
        $imageIdentity.Count -ne 1 -or
        $imageIdentity[0] -cne $imageDigest -or
        $imageTagsJson.Count -ne 1 -or
        $imageDigestsJson.Count -ne 1) {
        throw 'The exact pinned PostgreSQL image is not available locally.'
    }
    $localTags = @($imageTagsJson[0] | ConvertFrom-Json)
    $localDigests = @($imageDigestsJson[0] | ConvertFrom-Json)
    if ($localTags -cnotcontains $imageTag -or
        $localDigests -cnotcontains "postgres@$imageDigest") {
        throw 'The local PostgreSQL tag does not identify the pinned digest.'
    }

    $failureStage = 'temporary secret preparation'
    if (Test-Path -LiteralPath $temporaryRoot) {
        throw 'The generated temporary root already exists.'
    }
    [System.IO.Directory]::CreateDirectory($temporaryRoot) | Out-Null
    $temporaryItem = Get-Item -LiteralPath $temporaryRoot -Force
    if (($temporaryItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'The generated temporary root is not a regular directory.'
    }
    $password = [Convert]::ToBase64String(
        [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    [System.IO.File]::WriteAllText(
        $passwordPath,
        $password,
        [System.Text.UTF8Encoding]::new($false))

    $failureStage = 'isolated network creation'
    $networkProvisioningAttempted = $true
    Invoke-DockerChecked @(
        'network', 'create',
        '--driver', 'bridge',
        '--label', "$ownershipLabelName=true",
        '--label', "$runLabelName=$runId",
        $networkName
    ) | Out-Null
    if (-not (Test-ExactResourceOwnership 'network' $networkName)) {
        throw 'The disposable network ownership labels were not proved.'
    }

    $failureStage = 'isolated volume creation'
    $volumeProvisioningAttempted = $true
    Invoke-DockerChecked @(
        'volume', 'create',
        '--label', "$ownershipLabelName=true",
        '--label', "$runLabelName=$runId",
        $volumeName
    ) | Out-Null
    if (-not (Test-ExactResourceOwnership 'volume' $volumeName)) {
        throw 'The disposable volume ownership labels were not proved.'
    }

    $failureStage = 'disposable PostgreSQL creation'
    $containerProvisioningAttempted = $true
    Invoke-DockerChecked @(
        'run', '--detach',
        '--pull', 'never',
        '--name', $containerName,
        '--label', "$ownershipLabelName=true",
        '--label', "$runLabelName=$runId",
        '--network', $networkName,
        '--mount', "type=volume,source=$volumeName,target=/var/lib/postgresql/data",
        '--mount', "type=bind,source=$passwordPath,target=/run/secrets/r-egress-fence-password,readonly",
        '--env', 'POSTGRES_USER=dbnotifier_r_egress_fence',
        '--env', 'POSTGRES_DB=dbnotifier_r_egress_fence',
        '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/r-egress-fence-password',
        '--publish', '127.0.0.1:0:5432',
        '--cpus', '1',
        '--memory', '512m',
        '--pids-limit', '256',
        '--stop-timeout', '5',
        '--health-cmd', 'pg_isready -U dbnotifier_r_egress_fence -d dbnotifier_r_egress_fence',
        '--health-interval', '1s',
        '--health-timeout', '3s',
        '--health-retries', '30',
        $imageDigest
    ) | Out-Null
    if (-not (Test-ExactResourceOwnership 'container' $containerName)) {
        throw 'The disposable PostgreSQL container ownership labels were not proved.'
    }

    $failureStage = 'bounded PostgreSQL health wait'
    $healthy = $false
    for ($attempt = 1; $attempt -le 40; $attempt++) {
        $health = @(
            & $docker container inspect `
                --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}' `
                $containerName 2>$null
        )
        if ($LASTEXITCODE -ne 0 -or $health.Count -ne 1) {
            throw 'The disposable PostgreSQL health state became unavailable.'
        }
        if ($health[0] -ceq 'healthy') {
            $healthy = $true
            break
        }
        if ($health[0] -ceq 'unhealthy') {
            throw 'The disposable PostgreSQL container became unhealthy.'
        }
        Start-Sleep -Seconds 1
    }
    if (-not $healthy) {
        throw 'The disposable PostgreSQL container did not become healthy within the bounded wait.'
    }

    $failureStage = 'loopback port validation'
    $portBinding = @(& $docker port $containerName '5432/tcp' 2>$null)
    if ($LASTEXITCODE -ne 0 -or
        $portBinding.Count -ne 1 -or
        $portBinding[0] -notmatch '^127\.0\.0\.1:(\d{1,5})$') {
        throw 'The disposable PostgreSQL port is not bound exactly once to IPv4 loopback.'
    }
    $hostPort = [int]$Matches[1]
    if ($hostPort -lt 1 -or $hostPort -gt 65535) {
        throw 'The disposable PostgreSQL loopback port is outside the valid range.'
    }

    $previousActivation = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_R_EGRESS_POSTGRESQL_LAB',
        'Process')
    $previousConnection = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION',
        'Process')
    $previousSummary = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_R_EGRESS_POSTGRESQL_SUMMARY',
        'Process')
    $previousDotNetHost = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_R_EGRESS_POSTGRESQL_DOTNET_HOST',
        'Process')
    $previousRunToken = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_R_EGRESS_POSTGRESQL_RUN_TOKEN',
        'Process')
    try {
        $failureStage = 'multiprocess integration matrix'
        $passwordKey = 'Pass' + 'word'
        $connectionString =
            "Host=127.0.0.1;Port=$hostPort;Database=dbnotifier_r_egress_fence;" +
            "Username=dbnotifier_r_egress_fence;$passwordKey=$password;SSL Mode=Disable;" +
            "Timeout=5;Command Timeout=15;Include Error Detail=false;Pooling=false;" +
            "Application Name=DBNotifier-R-Egress-Fence-Local-Test"
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_LAB',
            'local-test',
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION',
            $connectionString,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_SUMMARY',
            $summaryPath,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_DOTNET_HOST',
            $resolvedDotNet,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_RUN_TOKEN',
            $runToken,
            'Process')

        $testResult = Invoke-BoundedDotNetTest `
            $resolvedDotNet `
            (Join-Path $root 'tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj') `
            $password
        $testExitCode = $testResult.ExitCode
        if (-not [string]::IsNullOrWhiteSpace($testResult.StandardOutput)) {
            Write-Output $testResult.StandardOutput.TrimEnd()
        }
        if (-not [string]::IsNullOrWhiteSpace($testResult.StandardError)) {
            Write-Output $testResult.StandardError.TrimEnd()
        }

        $failureStage = 'sanitised summary validation'
        $sanitisedSummary = Read-SanitisedFenceSummary $summaryPath $password
        Write-Output "R-EGRESS PostgreSQL fence lab test summary: $sanitisedSummary"
        $summaryEmitted = $true
        Remove-Item -LiteralPath $summaryPath -Force
        if (Test-Path -LiteralPath $summaryPath) {
            throw 'The owned PostgreSQL fence summary could not be removed.'
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_SUMMARY',
            $previousSummary,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_CONNECTION',
            $previousConnection,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_LAB',
            $previousActivation,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_DOTNET_HOST',
            $previousDotNetHost,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_R_EGRESS_POSTGRESQL_RUN_TOKEN',
            $previousRunToken,
            'Process')
        $connectionString = $null
        $password = $null
    }
}
catch {
    $executionFailed = $true
}
finally {
    $connectionString = $null
    $password = $null

    if ($null -ne $testProcess) {
        try {
            if (-not $testProcess.HasExited) {
                try {
                    $testProcess.Kill($true)
                }
                catch {
                    if (-not $testProcess.HasExited) {
                        throw
                    }
                }
            }
            if (-not $testProcess.WaitForExit(10000)) {
                $cleanupFailures.Add(
                    'The exact bounded integration test process did not exit during final cleanup.')
            }
            else {
                $testProcess.Dispose()
                $testProcess = $null
            }
        }
        catch {
            $cleanupFailures.Add(
                'The exact bounded integration test process could not be revalidated during final cleanup.')
        }
    }

    Stop-OwnedSandboxProcess

    if ($dockerEngineVerified) {
        Remove-OwnedDockerResource `
            'container' `
            $containerName `
            $containerProvisioningAttempted
        Remove-OwnedDockerResource `
            'volume' `
            $volumeName `
            $volumeProvisioningAttempted
        Remove-OwnedDockerResource `
            'network' `
            $networkName `
            $networkProvisioningAttempted
    }

    if (Test-Path -LiteralPath $temporaryRoot) {
        $safeTemporaryRoot =
            $temporaryRoot.Equals(
                [System.IO.Path]::GetFullPath($expectedTemporaryRoot),
                [StringComparison]::OrdinalIgnoreCase) -and
            ([System.IO.Path]::GetDirectoryName($temporaryRoot)).Equals(
                $systemTemporaryRoot,
                [StringComparison]::OrdinalIgnoreCase) -and
            ([System.IO.Path]::GetFileName($temporaryRoot)).Equals(
                $temporaryLeaf,
                [StringComparison]::Ordinal)
        $temporaryRootItem = Get-Item -LiteralPath $temporaryRoot -Force
        $reparseEntries = @(
            Get-ChildItem -LiteralPath $temporaryRoot -Force -Recurse |
                Where-Object {
                    ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
                }
        )
        if (-not $safeTemporaryRoot -or
            ($temporaryRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
            $reparseEntries.Count -ne 0) {
            $cleanupFailures.Add(
                'The owned temporary root could not be validated for recursive removal.')
        }
        else {
            Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
            if (Test-Path -LiteralPath $temporaryRoot) {
                $cleanupFailures.Add('The owned temporary root could not be removed.')
            }
        }
    }
}

if ($dockerEngineVerified) {
    try {
        $currentRunResidue =
            (Get-LabelledResourceCount 'container' -CurrentRunOnly) +
            (Get-LabelledResourceCount 'network' -CurrentRunOnly) +
            (Get-LabelledResourceCount 'volume' -CurrentRunOnly)
        $lotResidue =
            (Get-LabelledResourceCount 'container') +
            (Get-LabelledResourceCount 'network') +
            (Get-LabelledResourceCount 'volume')
        if ($currentRunResidue -ne 0 -or $lotResidue -ne 0) {
            $cleanupFailures.Add(
                'One or more exactly labelled R-EGRESS/R-FENCE Docker resources remain.')
        }
    }
    catch {
        $cleanupFailures.Add('The final labelled-resource audit could not be completed.')
    }
}

if (Test-Path -LiteralPath $temporaryRoot) {
    $cleanupFailures.Add('The exact owned temporary root remains after cleanup.')
}
if ($cleanupFailures.Count -ne 0) {
    throw 'R-EGRESS/R-FENCE cleanup or zero-residue validation failed.'
}
if ($executionFailed) {
    throw "The R-EGRESS/R-FENCE PostgreSQL lab failed during $failureStage."
}
if ($testExitCode -ne 0) {
    throw "The R-EGRESS/R-FENCE integration matrix failed with exit code $testExitCode."
}
if (-not $summaryEmitted) {
    throw 'The sanitised R-EGRESS/R-FENCE test summary was not emitted.'
}

Write-Output (
    "R-EGRESS/R-FENCE PostgreSQL lab passed with pinned image $imageDigest; " +
    'containers=0, networks=0, volumes=0 and owned temporary roots=0.')
