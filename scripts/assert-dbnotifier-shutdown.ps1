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
                    CreationDate = $_.CreationDate
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

function Get-CuaLauncherPrefixDigest {
    <#
    .SYNOPSIS
    Computes the non-expressive identity of an exact CUA launch prefix.
    .PARAMETER Prefix
    Raw executable-to-entry-point span with surrounding whitespace removed; internal bytes are preserved.
    .OUTPUTS
    Lowercase SHA-256 of UTF-8 prefix bytes. No command text is retained, logged or executed.
    #>
    [OutputType([string])]
    param([Parameter(Mandatory)][AllowEmptyString()][string]$Prefix)

    return [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Prefix))).ToLowerInvariant()
}

function Test-SharedCodexCuaHost {
    <#
    .SYNOPSIS
    Recognises a windowless, non-listening Windows CUA host from coherent runtime and live identity evidence.

    .PARAMETER ProcessRecord
    Inventory record whose incidental workspace argument would otherwise imply project ownership.

    .OUTPUTS
    System.Boolean. False retains normal fail-closed ownership checks on missing or conflicting evidence.

    .NOTES
    This is not a caller-configurable process exemption. Commands remain in memory and are never executed
    or logged. Strong product-path evidence is evaluated before this helper is called.
    #>
    [OutputType([bool])]
    param([Parameter(Mandatory)][pscustomobject]$ProcessRecord)

    if (-not $IsWindows -or $ProcessRecord.Name -ine 'node.exe') {
        return $false
    }

    $liveProcess = $null
    try {
        $executable = ([string]$ProcessRecord.ExecutablePath).Replace('\', '/')
        $localData = [Environment]::GetFolderPath(
            [Environment+SpecialFolder]::LocalApplicationData).Replace('\', '/').TrimEnd('/')
        $runtimePrefix = "$localData/OpenAI/Codex/runtimes/cua_node/"
        if ([string]::IsNullOrWhiteSpace($localData) -or
            -not $executable.StartsWith($runtimePrefix, [StringComparison]::OrdinalIgnoreCase) -or
            $executable.Substring($runtimePrefix.Length) -notmatch '^[a-fA-F0-9]{16}/bin/node[.]exe$') {
            return $false
        }

        $parentId = [int]$ProcessRecord.ParentProcessId
        $processId = [int]$ProcessRecord.ProcessId
        if ($parentId -le 0 -or $processId -le 0 -or $parentId -eq $processId) {
            return $false
        }
        $parents = @(Get-CimInstance Win32_Process -Filter "ProcessId = $parentId" -ErrorAction Stop)
        $parentExecutable = $executable.Substring(0, $executable.Length - 'node.exe'.Length) + 'node_repl.exe'
        if ($parents.Count -ne 1 -or $parents[0].ProcessId -ne $parentId -or
            $parents[0].Name -ine 'node_repl.exe' -or
            ([string]$parents[0].ExecutablePath).Replace('\', '/') -ine $parentExecutable -or
            ([string]$parents[0].CommandLine).Replace('\', '/').Trim().Trim('"') -ine $parentExecutable) {
            return $false
        }

        $rawCommand = [string]$ProcessRecord.CommandLine
        $command = $rawCommand.Replace('\', '/')
        $quotedExecutable = [regex]::Escape($executable)
        $executableMatch = [regex]::Match(
            $command, '^(?i:"' + $quotedExecutable + '"|' + $quotedExecutable + ')(?=\s+)')
        if (-not $executableMatch.Success) {
            return $false
        }

        # Accept only the two observed entry-point contracts, not arbitrary Node scripts or trailing options.
        $temporaryRoot = [IO.Path]::GetTempPath().Replace('\', '/').TrimEnd('/') + '/'
        $entryPrefix = [regex]::Escape($temporaryRoot) + '[.]tmp[a-zA-Z0-9]+/'
        $workspace = '(?:"(?<workspace>[a-zA-Z]:/[^"\r\n]+)"|(?<workspace>[a-zA-Z]:/[^"\r\n]+))\s*$'
        $kernel = '(?i)(?:^|\s)(?:"(?<entry>' + $entryPrefix + 'kernel[.]js)"|(?<entry>' +
            $entryPrefix + 'kernel[.]js))\s+--session-id\s+[a-f0-9]{32}\s+--working-dir\s+' + $workspace
        $worker = '(?i)(?:^|\s)(?:"(?<entry>' + $entryPrefix + 'trusted-worker[.]js)"|(?<entry>' +
            $entryPrefix + 'trusted-worker[.]js))\s+' + $workspace
        $role = [regex]::Match($command, $kernel)
        if (-not $role.Success) {
            $role = [regex]::Match($command, $worker)
        }
        if (-not $role.Success -or -not [IO.Directory]::Exists($role.Groups['workspace'].Value.TrimEnd())) {
            return $false
        }

        # Whole-prefix digests identify the two observed bootstrap shapes without copying vendor code.
        # Unknown scripts, preloads, eval bodies or options require a separately reviewed update.
        if ($role.Index -lt $executableMatch.Length) {
            return $false
        }
        $launcherPrefix = $rawCommand.Substring(
            $executableMatch.Length, $role.Index - $executableMatch.Length).Trim()
        $kernelRole = $role.Groups['entry'].Value.EndsWith('/kernel.js', [StringComparison]::OrdinalIgnoreCase)
        $expectedDigest = if ($kernelRole) {
            '10dc4b048f490fea8d9cdb44d640234ba19fac540457f6f4242daec28c11a900'
        } else {
            'd3a43d6ad401c9f35e84c69aefdbf807f49f26da612b9be008a11f63b63ba205'
        }
        if ((Get-CuaLauncherPrefixDigest -Prefix $launcherPrefix) -cne $expectedDigest) {
            return $false
        }

        # Redirected or missing runtime/entry-point files cannot establish shared-host provenance.
        foreach ($file in @($executable, $parentExecutable, $role.Groups['entry'].Value)) {
            $cursor = [IO.Path]::GetFullPath($file)
            $leaf = $true
            while (-not [string]::IsNullOrEmpty($cursor)) {
                $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
                if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or
                    ($leaf -and ($item.Attributes -band [IO.FileAttributes]::Directory) -ne 0)) {
                    return $false
                }
                $leaf = $false
                $cursor = [IO.Path]::GetDirectoryName($cursor)
            }
        }

        # Re-read the complete child identity so a recycled PID cannot inherit an old CUA role.
        $children = @(Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction Stop)
        if ($children.Count -ne 1 -or $children[0].ProcessId -ne $processId -or
            $children[0].ParentProcessId -ne $parentId -or
            $children[0].Name -ine $ProcessRecord.Name -or
            $children[0].ExecutablePath -ine $ProcessRecord.ExecutablePath -or
            $children[0].CommandLine -cne $ProcessRecord.CommandLine -or
            $null -eq $ProcessRecord.CreationDate -or $null -eq $children[0].CreationDate -or
            $children[0].CreationDate -ne $ProcessRecord.CreationDate) {
            return $false
        }
        $liveProcess = Get-Process -Id $processId -ErrorAction Stop
        if ($liveProcess.Id -ne $processId -or $liveProcess.HasExited -or
            ([string]$liveProcess.Path).Replace('\', '/') -ine $executable -or
            $liveProcess.MainWindowHandle -ne [IntPtr]::Zero) {
            return $false
        }
        $listeners = @(Get-NetTCPConnection -State Listen -ErrorAction Stop |
                Where-Object { [int]$_.OwningProcess -eq $processId })
        return $listeners.Count -eq 0
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $liveProcess) {
            $liveProcess.Dispose()
        }
    }
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
    Strong product paths take precedence over shared-host recognition. CUA identity requires
    matching installed-runtime, parent, entry-point and windowless, non-listening evidence.
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

    if (Test-SharedCodexCuaHost -ProcessRecord $ProcessRecord) {
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
