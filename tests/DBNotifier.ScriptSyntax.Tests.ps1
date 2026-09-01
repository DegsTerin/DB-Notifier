# Module purpose: Exercises the script-syntax gate in disposable Git worktrees without executing repository scripts.
#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gatePath = Join-Path (Join-Path $repositoryRoot 'scripts') 'verify-script-syntax.ps1'
$systemTemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$temporaryLeaf = "DBNotifier-ScriptSyntax-Tests With Spaces-$([guid]::NewGuid().ToString('N'))"
$temporaryRoot = Join-Path $systemTemporaryRoot $temporaryLeaf
$worktreeRoot = Join-Path $temporaryRoot 'fixture repository'
$outsideRoot = Join-Path $temporaryRoot 'not a repository'
$pathComparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
    [StringComparison]::OrdinalIgnoreCase
}
else {
    [StringComparison]::Ordinal
}
$assertionCount = 0

function Assert-Condition {
    <#
    .SYNOPSIS
    Fails the standalone test when one required condition is false.

    .PARAMETER Condition
    Boolean condition that must be true.

    .PARAMETER Message
    Sanitised failure description.

    .OUTPUTS
    None.

    .NOTES
    Throws on failure and increments the assertion count on success.
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

function Invoke-GateCapture {
    <#
    .SYNOPSIS
    Invokes the gate and captures its success state and sanitised streams.

    .PARAMETER GateParameters
    Named parameter dictionary passed to the gate.

    .OUTPUTS
    PSCustomObject containing success and combined output.

    .NOTES
    The helper never prints fixture content and resets the error collection for each invocation.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [hashtable]$GateParameters
    )

    $succeeded = $true
    $captured = @()
    try {
        $captured = @(& $gatePath @GateParameters *>&1)
    }
    catch {
        $succeeded = $false
        $captured += $_
    }
    return [pscustomobject]@{
        Succeeded = $succeeded
        Output = ($captured | Out-String)
    }
}

New-Item -ItemType Directory -Path $worktreeRoot -Force | Out-Null
New-Item -ItemType Directory -Path $outsideRoot -Force | Out-Null

try {
    & git -C $worktreeRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'The disposable syntax-test repository could not be initialised.' }
    & git -C $worktreeRoot config user.email 'dbnotifier-syntax@example.invalid'
    & git -C $worktreeRoot config user.name 'DB-Notifier Syntax Test'
    & git -C $worktreeRoot config core.autocrlf false

    $validPowerShell = Join-Path $worktreeRoot 'valid script.ps1'
    $validNode = Join-Path $worktreeRoot 'valid script.mjs'
    $untrackedPowerShell = Join-Path $worktreeRoot 'untracked valid.psm1'
    [System.IO.File]::WriteAllText(
        $validPowerShell,
        "# Module purpose: Provides one valid disposable PowerShell fixture.`n`$value = 1`n",
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $validNode,
        "/** Module purpose: Provides one valid disposable Node fixture. */`nconst value = 1;`n",
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $untrackedPowerShell,
        "# Module purpose: Provides one untracked valid PowerShell fixture.`n`$value = 2`n",
        [System.Text.UTF8Encoding]::new($false))
    $expectedPowerShellInventory = 2
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Linux)) {
        [System.IO.File]::WriteAllText(
            (Join-Path $worktreeRoot 'CaseSensitive.ps1'),
            "# Module purpose: Proves one Linux case-distinct fixture.`n`$value = 3`n",
            [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText(
            (Join-Path $worktreeRoot 'casesensitive.ps1'),
            "# Module purpose: Proves the second Linux case-distinct fixture.`n`$value = 4`n",
            [System.Text.UTF8Encoding]::new($false))
        $expectedPowerShellInventory = 4
    }
    & git -C $worktreeRoot add -- 'valid script.ps1' 'valid script.mjs'
    if ($LASTEXITCODE -ne 0) { throw 'The disposable syntax fixtures could not be staged.' }

    $validResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        NodePath = (Get-Command node -CommandType Application -ErrorAction Stop).Source
    }
    Assert-Condition $validResult.Succeeded (
        "Valid tracked and untracked fixtures did not pass. Sanitised gate classification: $($validResult.Output.Trim())")
    Assert-Condition (
        $validResult.Output -match "powershell=$expectedPowerShellInventory" -and
        $validResult.Output -match 'node=1'
    ) 'The dynamic tracked and unignored inventory was incomplete.'

    $secretMarker = 'DO_NOT_ECHO_FIXTURE_CONTENT_9D1A'
    $brokenPowerShellLeaf = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') {
        'broken syntax with spaces.ps1'
    }
    else {
        "broken`nINJECTED syntax.ps1"
    }
    $brokenPowerShell = Join-Path $worktreeRoot $brokenPowerShellLeaf
    [System.IO.File]::WriteAllText(
        $brokenPowerShell,
        "# $secretMarker`nfunction Broken {`n",
        [System.Text.UTF8Encoding]::new($false))
    $brokenPowerShellResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        PowerShellOnly = $true
    }
    Assert-Condition (-not $brokenPowerShellResult.Succeeded) (
        'An invalid PowerShell fixture did not fail closed.')
    Assert-Condition (
        $brokenPowerShellResult.Output -notmatch [regex]::Escape($secretMarker)
    ) 'The PowerShell failure output exposed fixture content.'
    $gateTokens = $null
    $gateErrors = $null
    $gateAst = [System.Management.Automation.Language.Parser]::ParseFile(
        $gatePath,
        [ref]$gateTokens,
        [ref]$gateErrors)
    $sanitiserAst = @($gateAst.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -eq 'ConvertTo-SanitisedDisplayPath'
    }, $true)) | Select-Object -First 1
    Assert-Condition (
        $gateErrors.Count -eq 0 -and $null -ne $sanitiserAst
    ) 'The control-character path sanitiser was unavailable for direct testing.'
    . ([scriptblock]::Create($sanitiserAst.Extent.Text))
    Assert-Condition (
        (ConvertTo-SanitisedDisplayPath -Path "broken`nINJECTED") -ceq
        'broken\u000aINJECTED'
    ) 'A control character in a Git-provided path was not escaped for diagnostics.'
    if ([System.IO.Path]::DirectorySeparatorChar -ne '\') {
        Assert-Condition (
            $brokenPowerShellResult.Output -match 'broken\\u000aINJECTED' -and
            $brokenPowerShellResult.Output -notmatch "broken`r?`nINJECTED"
        ) 'The Git inventory did not preserve and sanitise a control-character path.'
    }
    [System.IO.File]::Delete($brokenPowerShell)

    $brokenNode = Join-Path $worktreeRoot 'broken syntax with spaces.mjs'
    [System.IO.File]::WriteAllText(
        $brokenNode,
        "/* $secretMarker */`nconst = ;`n",
        [System.Text.UTF8Encoding]::new($false))
    $brokenNodeResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        NodePath = (Get-Command node -CommandType Application -ErrorAction Stop).Source
    }
    Assert-Condition (-not $brokenNodeResult.Succeeded) 'An invalid Node fixture did not fail closed.'
    Assert-Condition (
        $brokenNodeResult.Output -notmatch [regex]::Escape($secretMarker)
    ) 'The Node failure output exposed fixture content.'
    [System.IO.File]::Delete($brokenNode)

    $missingNodeResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        NodePath = (Join-Path $worktreeRoot 'missing node.exe')
    }
    Assert-Condition (-not $missingNodeResult.Succeeded) 'A missing Node executable did not fail closed.'
    $wildcardNodeResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        NodePath = 'no*'
    }
    Assert-Condition (-not $wildcardNodeResult.Succeeded) 'A wildcard Node executable did not fail closed.'

    $outsideResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $outsideRoot
        PowerShellOnly = $true
    }
    Assert-Condition (-not $outsideResult.Succeeded) 'A directory outside a Git worktree did not fail closed.'

    $modernScript = Join-Path $worktreeRoot 'modern only.ps1'
    [System.IO.File]::WriteAllText(
        $modernScript,
        "# Module purpose: Proves explicit legacy routing.`n#Requires -Version 99.0`n`$value = 3`n",
        [System.Text.UTF8Encoding]::new($false))
    $desktopScript = Join-Path $worktreeRoot 'desktop edition only.ps1'
    [System.IO.File]::WriteAllText(
        $desktopScript,
        "# Module purpose: Proves explicit edition routing.`n#Requires -PSEdition Desktop`n`$value = 4`n",
        [System.Text.UTF8Encoding]::new($false))
    $legacyResult = Invoke-GateCapture -GateParameters @{
        RepositoryRoot = $worktreeRoot
        PowerShellOnly = $true
        LegacyCompatibleOnly = $true
    }
    Assert-Condition $legacyResult.Succeeded 'The explicit legacy-compatible routing failed.'
    Assert-Condition (
        $legacyResult.Output -match 'legacy-skipped=2'
    ) 'A script requiring a newer version or different edition was not explicitly skipped.'

    Write-Output "Script-syntax gate tests passed: assertions=$assertionCount; disposable-residue=0."
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        $candidate = [System.IO.Path]::GetFullPath($temporaryRoot)
        $systemRoot = $systemTemporaryRoot.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
        $safeLeaf = [System.IO.Path]::GetFileName($candidate).Equals(
            $temporaryLeaf,
            [StringComparison]::Ordinal)
        $reparseEntries = @(
            Get-ChildItem -LiteralPath $candidate -Force -Recurse -ErrorAction SilentlyContinue |
                Where-Object {
                    ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
                })
        if (-not $candidate.StartsWith($systemRoot, $pathComparison) -or
            -not $safeLeaf -or
            $reparseEntries.Count -ne 0) {
            throw 'The disposable syntax-test root failed its cleanup ownership check.'
        }
        Get-ChildItem -LiteralPath $candidate -Force -Recurse | ForEach-Object {
            $_.Attributes = [System.IO.FileAttributes]::Normal
        }
        (Get-Item -LiteralPath $candidate -Force).Attributes =
            [System.IO.FileAttributes]::Directory
        [System.IO.Directory]::Delete($candidate, $true)
    }
    if (Test-Path -LiteralPath $temporaryRoot) {
        throw 'The disposable syntax-test root remains after cleanup.'
    }
}
