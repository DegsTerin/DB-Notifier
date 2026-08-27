# Module purpose: Fails closed when a DB-Notifier-owned process or listener remains before a technical action.
#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $PSScriptRoot '..'
}
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw 'ISOLATION_FAILURE: The shutdown-preflight repository root is unavailable.'
}
$resolvedRoot = [System.IO.Path]::GetFullPath(
    (Resolve-Path -LiteralPath $RepositoryRoot).Path)

function Get-WindowsProcessInventory {
    <#
    .SYNOPSIS
    Reads the Windows process identity fields required for project ownership checks.

    .OUTPUTS
    PSCustomObject records containing process ID, parent ID, name, executable path and command line.

    .NOTES
    Command lines are held only in memory and are never printed because they may contain sensitive values.
    #>
    [OutputType([pscustomobject])]
    param()

    try {
        return @(Get-CimInstance Win32_Process -ErrorAction Stop | ForEach-Object {
                [pscustomobject]@{
                    ProcessId = [int]$_.ProcessId
                    ParentProcessId = [int]$_.ParentProcessId
                    Name = [string]$_.Name
                    ExecutablePath = [string]$_.ExecutablePath
                    CommandLine = [string]$_.CommandLine
                }
            })
    }
    catch {
        throw 'ISOLATION_FAILURE: The Windows process inventory could not be proved for the shutdown preflight.'
    }
}

function Get-ProcProcessInventory {
    <#
    .SYNOPSIS
    Reads the Linux procfs identity fields required for project ownership checks.

    .OUTPUTS
    PSCustomObject records containing process ID, parent ID, name, executable path and command line.

    .NOTES
    Processes that exit during inventory are ignored; any still-present process whose identity is unreadable fails the preflight.
    #>
    [OutputType([pscustomobject])]
    param()

    if (-not (Test-Path -LiteralPath '/proc' -PathType Container)) {
        throw 'ISOLATION_FAILURE: No supported process inventory is available for the shutdown preflight.'
    }

    $records = [System.Collections.Generic.List[object]]::new()
    $unreadableProcessIds = [System.Collections.Generic.List[int]]::new()
    foreach ($directory in Get-ChildItem -LiteralPath '/proc' -Directory -ErrorAction Stop) {
        $processId = 0
        if (-not [int]::TryParse($directory.Name, [ref]$processId)) {
            continue
        }

        try {
            $statText = [System.IO.File]::ReadAllText(
                (Join-Path $directory.FullName 'stat'))
            $closingParenthesis = $statText.LastIndexOf(')')
            if ($closingParenthesis -lt 0) {
                continue
            }
            $statFields = $statText.Substring($closingParenthesis + 2).Split(' ')
            if ($statFields.Count -lt 2) {
                continue
            }
            $parentProcessId = [int]$statFields[1]
            $commandLineBytes = [System.IO.File]::ReadAllBytes(
                (Join-Path $directory.FullName 'cmdline'))
            $commandLine = [System.Text.Encoding]::UTF8.GetString(
                $commandLineBytes).Replace([char]0, ' ').Trim()
            $name = [System.IO.File]::ReadAllText(
                (Join-Path $directory.FullName 'comm')).Trim()
            $executablePath = ''
            try {
                $executablePath = [string](Get-Item -LiteralPath (
                        Join-Path $directory.FullName 'exe') -Force).Target
            }
            catch {
                $executablePath = ''
            }
            $records.Add([pscustomobject]@{
                    ProcessId = $processId
                    ParentProcessId = $parentProcessId
                    Name = $name
                    ExecutablePath = $executablePath
                    CommandLine = $commandLine
                })
        }
        catch {
            if (Test-Path -LiteralPath $directory.FullName -PathType Container) {
                $unreadableProcessIds.Add($processId)
            }
            continue
        }
    }
    if ($unreadableProcessIds.Count -gt 0) {
        throw (
            'ISOLATION_FAILURE: Linux procfs identity remained unreadable for live process IDs: ' +
            (($unreadableProcessIds | Sort-Object -Unique) -join ','))
    }
    return $records.ToArray()
}

function Get-ProcessInventory {
    <#
    .SYNOPSIS
    Selects the supported operating-system process inventory.

    .OUTPUTS
    PSCustomObject process records used only for local ownership classification.
    #>
    [OutputType([pscustomobject])]
    param()

    if ($IsWindows) {
        return @(Get-WindowsProcessInventory)
    }
    return @(Get-ProcProcessInventory)
}

function Get-AncestorProcessIds {
    <#
    .SYNOPSIS
    Identifies the current preflight process and its hosting parent chain.

    .PARAMETER Inventory
    Process inventory used to follow parent identifiers without another operating-system query.

    .OUTPUTS
    System.Int32 identifiers that must not be classified as residual product processes.
    #>
    [OutputType([int])]
    param(
        [Parameter(Mandatory)]
        [object[]]$Inventory
    )

    $byId = @{}
    foreach ($record in $Inventory) {
        $byId[[int]$record.ProcessId] = $record
    }
    $ancestors = [System.Collections.Generic.HashSet[int]]::new()
    $cursor = [int]$PID
    while ($cursor -gt 0 -and $ancestors.Add($cursor)) {
        if (-not $byId.ContainsKey($cursor)) {
            if ($cursor -eq $PID) {
                throw 'ISOLATION_FAILURE: The shutdown preflight could not identify its own process in the process inventory.'
            }
            break
        }
        $cursor = [int]$byId[$cursor].ParentProcessId
    }
    return @($ancestors)
}

function Test-ProjectOwnedProcess {
    <#
    .SYNOPSIS
    Classifies a process only when path, command-line or product-executable evidence proves DB-Notifier ownership.

    .PARAMETER ProcessRecord
    Sanitised in-memory process record to classify.

    .PARAMETER Root
    Exact repository root used as the strongest ownership marker.

    .OUTPUTS
    System.Boolean indicating whether the process is project-owned.

    .NOTES
    Common IDE, terminal and Codex hosts are never classified by name or workspace text alone.
    #>
    [OutputType([bool])]
    param(
        [Parameter(Mandatory)]
        [pscustomobject]$ProcessRecord,

        [Parameter(Mandatory)]
        [string]$Root
    )

    $normalisedRoot = $Root.Replace('\', '/').TrimEnd('/')
    $normalisedExecutable = ([string]$ProcessRecord.ExecutablePath).Replace('\', '/')
    $normalisedCommandLine = ([string]$ProcessRecord.CommandLine).Replace('\', '/')
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }

    $processNameLeaf = [System.IO.Path]::GetFileNameWithoutExtension(
        [string]$ProcessRecord.Name)
    $executablePathLeaf = if ([string]::IsNullOrWhiteSpace($normalisedExecutable)) {
        ''
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($normalisedExecutable)
    }
    if ($executablePathLeaf -match '^(?i:DBNotifier|DB-Notifier)(?:[.]|$)') {
        return $true
    }

    if ($normalisedExecutable.StartsWith(
            "$normalisedRoot/",
            $comparison)) {
        return $true
    }

    if ($processNameLeaf -match '^(?i:code|devenv|rider64|codex|windowsterminal|openai)$') {
        return $false
    }

    if ($normalisedCommandLine -match '(?i)(?:DBNotifier[.-]|DB-Notifier)') {
        return $true
    }

    return $normalisedCommandLine.Contains($normalisedRoot, $comparison)
}

function Get-OwnedListenerPorts {
    <#
    .SYNOPSIS
    Resolves local TCP listener ports attributed to already proven project-owned process IDs.

    .PARAMETER ProcessIds
    Project-owned process identifiers.

    .OUTPUTS
    System.Int32 local port numbers.

    .NOTES
    Absence of the platform cmdlet is acceptable when there is no project-owned process to attribute.
    #>
    [OutputType([int])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [int[]]$ProcessIds
    )

    if ($ProcessIds.Count -eq 0) {
        return @()
    }
    $listenerCommand = Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue
    if ($null -eq $listenerCommand) {
        return @()
    }
    try {
        return @(Get-NetTCPConnection -State Listen -ErrorAction Stop |
                Where-Object { $ProcessIds -contains [int]$_.OwningProcess } |
                Select-Object -ExpandProperty LocalPort -Unique |
                Sort-Object)
    }
    catch {
        throw 'ISOLATION_FAILURE: Listener ownership could not be proved for the DB-Notifier shutdown preflight.'
    }
}

$inventory = @(Get-ProcessInventory)
$ancestorIds = @(Get-AncestorProcessIds -Inventory $inventory)
$ownedProcesses = @($inventory | Where-Object {
        $ancestorIds -notcontains [int]$_.ProcessId -and
        (Test-ProjectOwnedProcess -ProcessRecord $_ -Root $resolvedRoot)
    })
$ownedProcessIds = @($ownedProcesses | ForEach-Object { [int]$_.ProcessId })
$ownedListenerPorts = @(Get-OwnedListenerPorts -ProcessIds $ownedProcessIds)

if ($ownedProcesses.Count -gt 0) {
    foreach ($ownedProcess in $ownedProcesses | Sort-Object ProcessId) {
        $processName = [System.IO.Path]::GetFileName([string]$ownedProcess.Name)
        Write-Output (
            'BLOCKED|shutdown-preflight|pid={0}|process={1}' -f
            $ownedProcess.ProcessId,
            $processName)
    }
    if ($ownedListenerPorts.Count -gt 0) {
        Write-Output (
            'BLOCKED|shutdown-preflight|owned-listeners={0}' -f
            ($ownedListenerPorts -join ','))
    }
    throw (
        'ISOLATION_FAILURE: The shutdown preflight found DB-Notifier-owned residue. Close only the identified project processes, verify cleanup and retry.')
}

Write-Output 'PASS|shutdown-preflight|matching-processes=0|owned-listeners=0'
