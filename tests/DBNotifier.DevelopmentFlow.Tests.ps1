# Module purpose: Exercises the development-flow policy without restoring, building, testing product code or using the network.
#Requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$developmentPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'development.ps1'
$ciPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'ci.ps1'
$legacyRunnerPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'run-legacy-tests.ps1'
$legacyTestsPath = Join-Path (Join-Path $repositoryRoot 'tests') 'DBNotifier.Legacy.Tests.ps1'
$environmentPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'development-environment.ps1'
$toolchainPolicyPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'toolchain-version-policy.ps1'
$shutdownPreflightPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'assert-dbnotifier-shutdown.ps1'
$ownedHelperPath = Join-Path (Join-Path $repositoryRoot 'tests/fixtures') 'DBNotifier.DevelopmentFlow.OwnedHelper.ps1'
$workflowPath = Join-Path (Join-Path $repositoryRoot '.github/workflows') 'ci.yml'
$verifierPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'verify-development-flow.ps1'
$rootInstructionsPath = Join-Path $repositoryRoot 'AGENTS.md'
$governancePath = Join-Path (Join-Path $repositoryRoot 'prompts/governance') 'Governance.md'
$lifecyclePath = Join-Path (Join-Path $repositoryRoot 'prompts/governance') 'Lifecycle.md'
$qualityPath = Join-Path (Join-Path $repositoryRoot 'prompts/governance') 'Quality-Gates.md'
$coordinationPath = Join-Path (Join-Path $repositoryRoot 'prompts/governance') 'Conversation-Coordination-Prompt.md'
$continuousImprovementPath = Join-Path (Join-Path $repositoryRoot 'prompts/governance') 'Continuous-Improvement.md'
$continuousBacklogPath = Join-Path (Join-Path $repositoryRoot 'prompts/state') 'Continuous-Improvement-Backlog.md'
$continuousControllerPath = Join-Path (Join-Path $repositoryRoot 'scripts') 'continuous-improvement.ps1'
$continuousTestsPath = Join-Path (Join-Path $repositoryRoot 'tests') 'DBNotifier.ContinuousImprovement.Tests.ps1'
$templatesPath = Join-Path (Join-Path $repositoryRoot 'prompts/templates') 'Templates.md'
$masterPromptPath = Join-Path (Join-Path $repositoryRoot 'prompts/system') 'AI-Software-Engineering-Master-Prompt.md'
$changelogPath = Join-Path (Join-Path $repositoryRoot 'prompts/system') 'Prompt-System-Change-Log.md'
$assertionCount = 0

function Assert-Condition {
    <#
    .SYNOPSIS
    Fails the standalone policy test when one required invariant is false.

    .PARAMETER Condition
    Boolean invariant result.

    .PARAMETER Message
    Sanitised failure description.

    .OUTPUTS
    None.
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

function Assert-Plan {
    <#
    .SYNOPSIS
    Requires two PlanOnly executions to match one exact ordered plan.

    .PARAMETER Task
    Development task to inspect.

    .PARAMETER Offline
    Selects the explicitly offline plan variant.

    .PARAMETER Expected
    Exact versioned plan records.

    .OUTPUTS
    None.

    .NOTES
    PlanOnly returns before process inventory, child creation, restore, build, tests or network access.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Task,

        [switch]$Offline,

        [Parameter(Mandatory)]
        [string[]]$Expected
    )

    $arguments = @{
        Task = $Task
        PlanOnly = $true
    }
    if ($Offline) {
        $arguments['Offline'] = $true
    }
    $first = @(& $developmentPath @arguments)
    $second = @(& $developmentPath @arguments)
    Assert-Condition `
        -Condition ($first.Count -eq $Expected.Count) `
        -Message "$Task PlanOnly returned an unexpected record count."
    Assert-Condition `
        -Condition (-not (Compare-Object -ReferenceObject $Expected -DifferenceObject $first -SyncWindow 0)) `
        -Message "$Task PlanOnly diverged from its exact contract."
    Assert-Condition `
        -Condition (-not (Compare-Object -ReferenceObject $first -DifferenceObject $second -SyncWindow 0)) `
        -Message "$Task PlanOnly is not deterministic."
}

$developmentScript = Get-Content -LiteralPath $developmentPath -Raw
$ciScript = Get-Content -LiteralPath $ciPath -Raw
$legacyRunnerScript = Get-Content -LiteralPath $legacyRunnerPath -Raw
$legacyTestsScript = Get-Content -LiteralPath $legacyTestsPath -Raw
$shutdownScript = Get-Content -LiteralPath $shutdownPreflightPath -Raw
$workflow = Get-Content -LiteralPath $workflowPath -Raw
$verifierScript = Get-Content -LiteralPath $verifierPath -Raw
$rootInstructions = Get-Content -LiteralPath $rootInstructionsPath -Raw
$governance = Get-Content -LiteralPath $governancePath -Raw
$lifecycle = Get-Content -LiteralPath $lifecyclePath -Raw
$quality = Get-Content -LiteralPath $qualityPath -Raw
$coordination = Get-Content -LiteralPath $coordinationPath -Raw
$continuousImprovement = Get-Content -LiteralPath $continuousImprovementPath -Raw
$continuousBacklog = Get-Content -LiteralPath $continuousBacklogPath -Raw
$continuousController = Get-Content -LiteralPath $continuousControllerPath -Raw
$continuousTests = Get-Content -LiteralPath $continuousTestsPath -Raw
$templates = Get-Content -LiteralPath $templatesPath -Raw
$masterPrompt = Get-Content -LiteralPath $masterPromptPath -Raw
$changelog = Get-Content -LiteralPath $changelogPath -Raw
. $environmentPath
. $toolchainPolicyPath

Assert-Condition `
    -Condition ($verifierScript.Contains(
            'ls-files --cached --others --exclude-standard -- $requiredFiles',
            [System.StringComparison]::Ordinal) -and
        $verifierScript -notmatch '(?m)ls-files --cached --others --exclude-standard\s*\)') `
    -Message 'The policy verifier must inventory only its explicit allowlist and never the repository root.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('## Autonomous delivery mandate', [System.StringComparison]::Ordinal) -and
        $governance.Contains('## Autonomia prospectiva', [System.StringComparison]::Ordinal) -and
        $lifecycle.Contains('`AUTOMATED_GATE_PASS`', [System.StringComparison]::Ordinal)) `
    -Message 'Autonomous prospective delivery is not aligned across the root, governance and lifecycle authorities.'
Assert-Condition `
    -Condition ($coordination.Contains('- Revisão: `2.1.0`', [System.StringComparison]::Ordinal) -and
        $coordination.Contains('`START_NEW_AUTO_DISPATCH`', [System.StringComparison]::Ordinal) -and
        $coordination.Contains('chave de deduplicação', [System.StringComparison]::Ordinal) -and
        $coordination.Contains('Receipt', [System.StringComparison]::OrdinalIgnoreCase)) `
    -Message 'The autonomous coordination authority lacks revision, routing, receipt or deduplication controls.'
Assert-Condition `
    -Condition ($coordination.Contains('`CONTINUE_CURRENT` usa um registro local factual', [System.StringComparison]::Ordinal) -and
        $coordination.Contains('exigem confirmação factual da ferramenta', [System.StringComparison]::Ordinal) -and
        $quality.Contains('Para `CONTINUE_CURRENT`, exigir registro local factual', [System.StringComparison]::Ordinal)) `
    -Message 'Local continuation and tool-backed dispatch receipts are not distinguished.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('existing currently valid credential', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('available capable tool', [System.StringComparison]::Ordinal) -and
        $governance -match 'credencial\s+existente e válida' -and
        $governance.Contains('ferramenta disponível e apta', [System.StringComparison]::Ordinal)) `
    -Message 'External-action prerequisites do not require a valid credential and capable tool.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('broad path, unresolved variable, substitution, glob or unresolved target', [System.StringComparison]::Ordinal) -and
        $governance -match 'caminho amplo, variável,\s+substituição, glob ou identificador ainda não resolvido' -and
        $quality.Contains('raiz de workspace/home/caminho amplo/variável/glob', [System.StringComparison]::Ordinal)) `
    -Message 'The destructive Automated Safety Gate does not fail closed on broad or unresolved targets.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('protected-work preservation, recoverable checkpoint, rollback, no safer alternative, objective necessity and independent review', [System.StringComparison]::Ordinal) -and
        $governance -match '(?s)preservação de WIP.*checkpoint recuperável e plano de rollback.*validação de rollback.*alternativa materialmente mais segura.*necessidade objetiva e revisão independente' -and
        $quality -match '(?s)preservação de WIP, checkpoint recuperável,\s+rollback, validação aplicável, inexistência de alternativa mais segura,\s+necessidade objetiva e revisão independente') `
    -Message 'The destructive Automated Safety Gate does not preserve WIP, recovery, safer-alternative, necessity and review controls.'
Assert-Condition `
    -Condition ($rootInstructions.Contains('exact account and environment', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('defined cost boundary when applicable', [System.StringComparison]::Ordinal) -and
        $rootInstructions.Contains('objective success criterion and safe verification or reversal', [System.StringComparison]::Ordinal) -and
        $governance -match '(?s)conta, organização, ambiente e\s+alvo exatos.*limite de custo definido.*critério\s+objetivo de sucesso.*verificação segura ou reversão' -and
        $quality -match '(?s)conta e\s+ambiente exatos.*limite de custo.*critério objetivo de sucesso.*verificação segura ou reversão') `
    -Message 'The external-action gate does not preserve account, environment, cost, success and verification or reversal controls.'
Assert-Condition `
    -Condition ($quality.Contains('## Agent Gate', [System.StringComparison]::Ordinal) -and
        $quality.Contains('zero `P0` e zero `P1`', [System.StringComparison]::Ordinal) -and
        $templates.Contains('## Agent Gate', [System.StringComparison]::Ordinal) -and
        $templates.Contains('## Receipt de despacho', [System.StringComparison]::Ordinal)) `
    -Message 'Agent Gate and dispatch receipt contracts are incomplete.'
Assert-Condition `
    -Condition ($changelog -match '(?s)## Versão atual\s+- Versão: `9[.]1[.]0`' -and
        $masterPrompt.Contains('`EXTERNAL_PREREQUISITE`', [System.StringComparison]::Ordinal) -and
        -not $masterPrompt.Contains('WAITING_APPROVAL', [System.StringComparison]::Ordinal)) `
    -Message 'The corpus version or autonomous master method is incomplete.'
$officialContinuousImprovementPaths = @(
    'AGENTS.md',
    'PLANS.md',
    'prompts/Start-Here.md',
    'prompts/foundation/Prompt-New-Project.md',
    'prompts/foundation/Solution-Architecture-Document.md',
    'prompts/foundation/AIOps-And-AI-Module.md',
    'prompts/governance/Continuous-Improvement.md',
    'prompts/governance/Conversation-Coordination-Prompt.md',
    'prompts/governance/Governance.md',
    'prompts/governance/Language-Policy.md',
    'prompts/governance/Lifecycle.md',
    'prompts/governance/Quality-Gates.md',
    'prompts/governance/Security-And-Access.md',
    'prompts/operations/Operational-Playbooks.md',
    'prompts/state/Current-State.md',
    'prompts/state/Continuous-Improvement-Backlog.md',
    'prompts/state/State-Transition-Log.md',
    'prompts/system/AI-Software-Engineering-Master-Prompt.md',
    'prompts/system/Prompt-System-Change-Log.md',
    'prompts/templates/Templates.md',
    'docs/Code-Documentation-Standards.md',
    'docs/design/DB-Notifier-Design-System.md',
    'scripts/development.ps1',
    'scripts/ci.ps1',
    'scripts/continuous-improvement.ps1',
    'scripts/verify-development-flow.ps1',
    'tests/DBNotifier.ContinuousImprovement.Tests.ps1',
    'tests/DBNotifier.DevelopmentFlow.Tests.ps1')
$continuousImprovementProjectionSources = @(
    $continuousImprovement,
    $continuousController,
    $continuousTests)
$missingContinuousImprovementPaths = @(
    foreach ($path in $officialContinuousImprovementPaths) {
        foreach ($source in $continuousImprovementProjectionSources) {
            if (-not $source.Contains(
                    $path,
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                $path
                break
            }
        }
    })
Assert-Condition `
    -Condition ($officialContinuousImprovementPaths.Count -eq 28 -and
        @($officialContinuousImprovementPaths | Sort-Object -Unique).Count -eq 28 -and
        $missingContinuousImprovementPaths.Count -eq 0 -and
        [regex]::Matches(
            $continuousTests,
            'WasPreviouslyMissed = \$true').Count -eq 11) `
    -Message 'The exact 28-path governance matrix or its 11 prior counterexamples are not aligned across authority, controller and tests.'
Assert-Condition `
    -Condition ($continuousImprovement.Contains('Revision: `1.0.0`', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('EXECUTION_KEY_IDENTITY_INVALID', [System.StringComparison]::Ordinal) -and
        $continuousImprovement -match '(?s)same candidate digest can never be\s+attempted twice' -and
        $continuousImprovement.Contains('old gate', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('new gate', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('PENDING_APPEND_RECOVERY_REQUIRED', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('DUPLICATE_JSON_PROPERTY', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('PATH_OUTSIDE_AUTHORISED_ROOT', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('legacy `ContractRiskFacts`', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('P0-P3 counts', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('caller-supplied reviewer list', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('recovery never reports event A as successful completion of requested event B', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('Verifier disjointness applies identically to `GATE_FAILED` and `GATE_PASSED`', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('rejects `PASS`/`PASS`', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('promotion preimage commit and tree', [System.StringComparison]::Ordinal) -and
        $continuousImprovement.Contains('UTF8_BOM_FORBIDDEN', [System.StringComparison]::Ordinal) -and
        $continuousBacklog.Contains('`DBN-CI-0003`', [System.StringComparison]::Ordinal) -and
        $continuousBacklog.Contains('protected predecessor matrix drift', [System.StringComparison]::Ordinal)) `
    -Message 'The continuous-improvement authority and backlog do not preserve retry, meta-gate or predecessor-WIP controls.'
Assert-Condition `
    -Condition ($continuousController.Contains('Get-CINextDecision', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('executionScopeDigest', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('acceptanceDigest', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('EXECUTION_KEY_IDENTITY_INVALID', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('LEDGER_BOUND_EXCEEDED', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('MUTABLE_RESOURCE_COLLISION', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('function Repair-CILedgerPendingAppend', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('PENDING_APPEND_TRUNCATED_RECOVERED', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('UTF8_BOM_FORBIDDEN', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('REVIEW_RECORDED', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('CALLER_SUPPLIED_CLASSIFICATION', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('function Resolve-CIGitCommitReceipt', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('Assert-CIDisjointAgents', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('function Assert-CINoDuplicateJsonProperties', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('function Assert-CIPathContained', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('reviewFindingCounts', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('$failedChecksAreFactual', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('$requestedRecoveryEvent.eventHash -ceq $recovery.event.eventHash', [System.StringComparison]::Ordinal) -and
        $continuousController.Contains('profileVersion', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('An arbitrary caller-supplied causal GUID', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('Arbitrary SHA strings without a domain-bound ledger receipt', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('canonical valid-hash event with duplicate result', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('ledger outside its authorised root', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('latest ATTEMPT_STARTED event', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('A gate passed with a factual P0', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('reparse-backed candidate path', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('A phantom reviewer list', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('A failed gate accepted the candidate implementer as verifier', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('A governance failed gate accepted factual PASS/PASS', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('Recovery reported success for pending event A instead of requested event B', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('An arbitrary well-formed SHA-256 execution key was admitted', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('A later event admitted an arbitrary well-formed execution key', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('An exact pending execution retry did not return its repaired receipt', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('Non-authority product path', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('PENDING_APPEND_TRUNCATED_RECOVERED', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('UTF8_BOM_FORBIDDEN', [System.StringComparison]::Ordinal) -and
        $continuousTests.Contains('Continuous-improvement policy tests passed with', [System.StringComparison]::Ordinal)) `
    -Message 'The continuous-improvement executable policy is incomplete.'

$canonicalToolchains = Get-DBNotifierToolchainPolicy
$dotNetPolicy = Get-DBNotifierDotNetSdkPolicy `
    -GlobalJsonPath (Join-Path $repositoryRoot 'global.json')
$dashboardPolicy = Get-DBNotifierDashboardToolchainPolicy -RepositoryRoot $repositoryRoot
Assert-Condition `
    -Condition ($dotNetPolicy.Range -ceq $canonicalToolchains.DotNetRange -and
        $dashboardPolicy.NodeRange -ceq $canonicalToolchains.NodeRange -and
        $dashboardPolicy.NpmRange -ceq $canonicalToolchains.NpmRange) `
    -Message 'The repository toolchain manifests diverge from the canonical bounded ranges.'

$emptyNameDictionary = [ordered]@{ '' = 'root-package-metadata' }
Assert-Condition `
    -Condition ((Get-DBNotifierRequiredProperty `
            -InputObject $emptyNameDictionary `
            -Name '' `
            -Context 'empty-name regression fixture') -ceq 'root-package-metadata') `
    -Message 'The toolchain policy must preserve npm root package metadata with an empty key.'
$missingEmptyNameFailedClosed = $false
try {
    [void](Get-DBNotifierRequiredProperty `
            -InputObject ([ordered]@{}) `
            -Name '' `
            -Context 'missing empty-name regression fixture')
}
catch {
    $missingEmptyNameFailedClosed = $_.Exception.Message -ceq `
        "missing empty-name regression fixture is missing required property ''."
}
Assert-Condition `
    -Condition $missingEmptyNameFailedClosed `
    -Message 'The toolchain policy must fail closed when npm root package metadata is absent.'

# Exercise both edges of each inclusive-lower and exclusive-upper stable range
# without invoking or changing a host toolchain.
foreach ($rangeCase in @(
        @{
            Name = '.NET SDK'
            Range = $canonicalToolchains.DotNetRange
            Accepted = @('10.0.302', '10.0.303', '10.0.400', '10.0.999')
            Rejected = @('10.0.301', '10.1.0', '11.0.100', '10.0.400-preview.1', '10.0')
        },
        @{
            Name = 'Node.js'
            Range = $canonicalToolchains.NodeRange
            Accepted = @('24.18.0', '24.19.0', '24.99.999')
            Rejected = @('24.17.999', '25.0.0', '23.99.999', '24.19.0-rc.1', 'v24.19.0')
        },
        @{
            Name = 'npm'
            Range = $canonicalToolchains.NpmRange
            Accepted = @('11.16.0', '11.17.0', '11.99.999')
            Rejected = @('11.15.999', '12.0.0', '10.99.999', '11.17.0-beta.1', '11.17')
        })) {
    foreach ($acceptedVersion in $rangeCase.Accepted) {
        Assert-Condition `
            -Condition (Test-DBNotifierVersionInRange `
                    -Version $acceptedVersion `
                    -Range $rangeCase.Range) `
            -Message "$($rangeCase.Name) '$acceptedVersion' should satisfy '$($rangeCase.Range)'."
    }
    foreach ($rejectedVersion in $rangeCase.Rejected) {
        Assert-Condition `
            -Condition (-not (Test-DBNotifierVersionInRange `
                        -Version $rejectedVersion `
                        -Range $rangeCase.Range)) `
            -Message "$($rangeCase.Name) '$rejectedVersion' should not satisfy '$($rangeCase.Range)'."
    }
}

foreach ($secretName in @('OPENAI_API_KEY', 'AZURE_OPENAI_API_KEY')) {
    Assert-Condition `
        -Condition ($developmentScript -notmatch (
                '(?i)GetEnvironmentVariable\s*\([^)]*' + [regex]::Escape($secretName)) -and
            $developmentScript -notmatch (
                '(?i)\$env:' + [regex]::Escape($secretName))) `
        -Message "The development entry point must not read '$secretName'."
}

$childCredentialRemoval = $developmentScript.IndexOf(
    'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
    [System.StringComparison]::Ordinal)
$childStart = $developmentScript.IndexOf(
    '$process.Start()',
    [System.StringComparison]::Ordinal)
$taskDispatch = $developmentScript.IndexOf(
    'switch ($Task)',
    [System.StringComparison]::Ordinal)
$internalCredentialScrub = $developmentScript.IndexOf(
    '[System.Environment]::SetEnvironmentVariable(',
    $childStart,
    [System.StringComparison]::Ordinal)
Assert-Condition `
    -Condition ($childCredentialRemoval -ge 0 -and
        $childStart -gt $childCredentialRemoval -and
        $internalCredentialScrub -gt $childStart -and
        $taskDispatch -gt $internalCredentialScrub) `
    -Message 'Credential removal must precede child start and internal task dispatch.'
Assert-Condition `
    -Condition ($developmentScript.Contains('ProcessStartInfo', [System.StringComparison]::Ordinal)) `
    -Message 'The development entry point must use an isolated shell-free child process.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $developmentScript,
            '(?m)^\s*& \$ciEntrypoint @ciArguments\s*$').Count -eq 1) `
    -Message 'Full must delegate to the canonical CI entry point exactly once.'
Assert-Condition `
    -Condition ($developmentScript.Contains('Resolve-CompatibleDotNetHost', [System.StringComparison]::Ordinal) -and
        $ciScript.Contains('Resolve-CompatibleDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $developmentScript.Contains('Resolve-PinnedDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $ciScript.Contains('Resolve-PinnedDotNetHost', [System.StringComparison]::Ordinal) -and
        -not $developmentScript.Contains("'.dotnet') 'toolchains'", [System.StringComparison]::Ordinal) -and
        -not $ciScript.Contains("'.dotnet') 'toolchains'", [System.StringComparison]::Ordinal)) `
    -Message 'Both executable entry points must enforce compatible .NET resolution.'

$parentSentinelName = 'DBNOTIFIER_DEVELOPMENT_TEST_SENTINEL'
[System.Environment]::SetEnvironmentVariable(
    $parentSentinelName,
    'parent-preserved',
    [System.EnvironmentVariableTarget]::Process)
try {
    $isolatedStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $isolatedStartInfo.UseShellExecute = $false
    $isolatedStartInfo.Environment['DBNOTIFIER_R4B_POSTGRESQL_LAB'] = 'local-test'
    $isolatedStartInfo.Environment['DBNOTIFIER_R4B_POSTGRESQL_CONNECTION'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['ConnectionStrings__ServerDatabase'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['DashboardTvSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['DashboardTvSignalRSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['DOTNET_ENVIRONMENT'] = 'DashboardTvSandbox'
    $isolatedStartInfo.Environment['Kestrel__Endpoints__Http__Url'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['ReconciledLocalNotificationSandbox__Enabled'] = 'true'
    $isolatedStartInfo.Environment['VITE_DB_NOTIFIER_TV_SANDBOX'] = 'local-test'
    $isolatedStartInfo.Environment['OPENAI_API_KEY'] = 'must-not-be-read'
    $isolatedStartInfo.Environment['DB_NOTIFIER_SAFE_SENTINEL'] = 'preserved'

    Remove-DBNotifierInheritedEnvironment -StartInfo $isolatedStartInfo

    foreach ($removedName in @(
            $parentSentinelName,
            'DBNOTIFIER_R4B_POSTGRESQL_LAB',
            'DBNOTIFIER_R4B_POSTGRESQL_CONNECTION',
            'ConnectionStrings__ServerDatabase',
            'DashboardTvSandbox__Enabled',
            'DashboardTvSignalRSandbox__Enabled',
            'DOTNET_ENVIRONMENT',
            'Kestrel__Endpoints__Http__Url',
            'ReconciledLocalNotificationSandbox__Enabled',
            'VITE_DB_NOTIFIER_TV_SANDBOX',
            'OPENAI_API_KEY')) {
        Assert-Condition `
            -Condition (-not $isolatedStartInfo.Environment.ContainsKey($removedName)) `
            -Message "The isolated environment retained hazardous variable '$removedName'."
    }
    Assert-Condition `
        -Condition ($isolatedStartInfo.Environment['DB_NOTIFIER_SAFE_SENTINEL'] -ceq 'preserved') `
        -Message 'The environment boundary removed an unrelated variable.'
    Assert-Condition `
        -Condition ([System.Environment]::GetEnvironmentVariable(
                $parentSentinelName,
                [System.EnvironmentVariableTarget]::Process) -ceq 'parent-preserved') `
        -Message 'Child-environment sanitisation mutated the parent process environment.'
}
finally {
    [System.Environment]::SetEnvironmentVariable(
        $parentSentinelName,
        $null,
        [System.EnvironmentVariableTarget]::Process)
}

Assert-Plan -Task 'Doctor' -Expected @(
    'PLAN|version=1|task=Doctor|mode=Online|classification=WORKFLOW',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root and required files',
    'STEP|3|Validate PowerShell, Git, .NET, Node.js and npm toolchains',
    'STEP|4|Validate tracked NuGet and npm lock files',
    'STEP|5|Report whether restored dependencies are ready'
)

Assert-Plan -Task 'Setup' -Offline -Expected @(
    'PLAN|version=1|task=Setup|mode=Offline|classification=WORKFLOW',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root, toolchains and lock files',
    'STEP|3|dotnet restore DBNotifier.sln --configfile scripts/NuGet.Offline.config --locked-mode',
    'STEP|4|npm ci --offline --ignore-scripts --no-audit --no-fund',
    'STEP|5|Verify lockfile stability and restored dependency readiness'
)

Assert-Plan -Task 'Quick' -Expected @(
    'PLAN|version=1|task=Quick|mode=Online|classification=NON_GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|Validate repository root, toolchains, lock files and restored dependencies',
    'STEP|3|dotnet format DBNotifier.sln --verify-no-changes --no-restore',
    'STEP|4|dotnet build DBNotifier.sln --configuration Release --no-restore',
    'STEP|5|dotnet test tests/DBNotifier.UnitTests/DBNotifier.UnitTests.csproj --configuration Release --no-build --no-restore',
    'STEP|6|dotnet test tests/DBNotifier.Architecture.Tests/DBNotifier.Architecture.Tests.csproj --configuration Release --no-build --no-restore',
    'STEP|7|Verify Dashboard assets, types, documentation and Markdown',
    'STEP|8|npm test',
    'STEP|9|Verify development-flow and script policies',
    'STEP|10|git diff --check'
)

Assert-Plan -Task 'Full' -Expected @(
    'PLAN|version=1|task=Full|mode=Online|classification=GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|scripts/ci.ps1'
)

Assert-Plan -Task 'Full' -Offline -Expected @(
    'PLAN|version=1|task=Full|mode=Offline|classification=GATE',
    'STEP|1|Assert the mandatory DB-Notifier shutdown preflight',
    'STEP|2|scripts/ci.ps1 -Offline'
)

# Prove that the preflight blocks a disposable, exactly attributable helper
# and returns to a clean result after that owned child has ended.
$ownedHelper = [System.Diagnostics.Process]::new()
$ownedHelperStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$ownedHelperStartInfo.FileName = (Get-Process -Id $PID).Path
$ownedHelperStartInfo.UseShellExecute = $false
foreach ($argument in @(
        '-NoLogo',
        '-NoProfile',
        '-NonInteractive',
        '-File',
        $ownedHelperPath)) {
    $ownedHelperStartInfo.ArgumentList.Add($argument)
}
$ownedHelper.StartInfo = $ownedHelperStartInfo
try {
    Assert-Condition `
        -Condition $ownedHelper.Start() `
        -Message 'The disposable shutdown-preflight helper did not start.'
    $preflightBlocked = $false
    try {
        & $shutdownPreflightPath -RepositoryRoot $repositoryRoot | Out-Null
    }
    catch {
        $preflightBlocked = $true
    }
    Assert-Condition `
        -Condition $preflightBlocked `
        -Message 'The shutdown preflight did not block an exactly attributable project helper.'
}
finally {
    if (-not $ownedHelper.HasExited) {
        $ownedHelper.Kill($true)
        $ownedHelper.WaitForExit()
    }
    $ownedHelper.Dispose()
}
$cleanPreflight = @(& $shutdownPreflightPath -RepositoryRoot $repositoryRoot)
Assert-Condition `
    -Condition ($cleanPreflight -contains 'PASS|shutdown-preflight|matching-processes=0|owned-listeners=0') `
    -Message 'The shutdown preflight did not return to a clean result after owned-helper cleanup.'
Assert-Condition `
    -Condition ($shutdownScript.Contains(
            "`$executablePathLeaf -match '^(?i:DBNotifier|DB-Notifier)(?:[.]|`$)'",
            [System.StringComparison]::Ordinal) -and
        -not $shutdownScript.Contains(
            "`$processNameLeaf -match '^(?i:DBNotifier|DB-Notifier)(?:[.]|`$)'",
            [System.StringComparison]::Ordinal)) `
    -Message 'Product-process ownership must use verified executable-path evidence rather than a process name alone.'
Assert-Condition `
    -Condition ($shutdownScript.Contains(
            '$unreadableProcessIds.Add($processId)',
            [System.StringComparison]::Ordinal) -and
        $shutdownScript.Contains(
            'Linux procfs identity remained unreadable for live process IDs',
            [System.StringComparison]::Ordinal)) `
    -Message 'Linux process inventory must fail closed for a still-present unreadable identity.'

$quickBody = [regex]::Match(
    $developmentScript,
    '(?s)function Invoke-Quick \{(?<body>.*?)\n\}').Groups['body'].Value
foreach ($forbiddenQuickOperation in @(
        'DBNotifier.IntegrationTests',
        'verify-dotnet-coverage.ps1',
        'verify-nuget-vulnerabilities.ps1',
        'npm audit',
        'run-state05',
        'run-state06')) {
    Assert-Condition `
        -Condition (-not $quickBody.Contains(
                $forbiddenQuickOperation,
                [System.StringComparison]::OrdinalIgnoreCase)) `
        -Message "Quick contains forbidden gate operation '$forbiddenQuickOperation'."
}

Assert-Condition `
    -Condition ([regex]::Matches(
            $ciScript,
            [regex]::Escape('DBNotifier.DevelopmentFlow.Tests.ps1')).Count -eq 1) `
    -Message 'The canonical gate must invoke this development-flow policy test exactly once.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $ciScript,
            [regex]::Escape('DBNotifier.ContinuousImprovement.Tests.ps1')).Count -eq 1) `
    -Message 'The canonical gate must invoke the continuous-improvement policy test exactly once.'
Assert-Condition `
    -Condition ($ciScript -notmatch 'if \(\$Stage -ceq ''Dashboard''\)' -and
        $ciScript -match 'if \(\$Stage -eq ''Dashboard''\)') `
    -Message 'The validated CI stage must dispatch case-insensitively.'
Assert-Condition `
    -Condition ($ciScript.IndexOf(
            'verify-secrets.ps1',
            [System.StringComparison]::Ordinal) -lt
        $ciScript.IndexOf(
            'Invoke-PolicyChecks -NodeExecutable $nodeExecutable',
            [System.StringComparison]::Ordinal)) `
    -Message 'Secret scanning must precede executable repository policy and product gates.'
Assert-Condition `
    -Condition ($ciScript.Contains(
            'DISPOSITION|$failureDisposition|stage=$canonicalStage|stop=$stopCode',
            [System.StringComparison]::Ordinal) -and
        $ciScript.Contains("'GATE_FAILURE'", [System.StringComparison]::Ordinal)) `
    -Message 'The canonical gate must preserve a machine-readable blocked or failed disposition.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            '(?m)^\s*run:\s*[.]\\scripts\\ci[.]ps1 -Stage All\b').Count -eq 1) `
    -Message 'The CI workflow must delegate its canonical Windows gate exactly once.'

$approvedActionPins = @{
    'actions/checkout' = '11d5960a326750d5838078e36cf38b85af677262'
    'actions/setup-dotnet' = '67a3573c9a986a3f9c594539f4ab511d57bb3ce9'
    'actions/setup-node' = '49933ea5288caeca8642d1e84afbd3f7d6820020'
    'actions/upload-artifact' = 'ea165f8d65b6e75b540449e92b4886f43607fa02'
}
$actionUses = [regex]::Matches(
    $workflow,
    '(?m)^\s*(?:-\s+)?uses:\s*(?<action>[^@\s]+)@(?<revision>[^\s#]+)')
Assert-Condition `
    -Condition ($actionUses.Count -gt 0 -and
        @($actionUses | Where-Object {
                $action = $_.Groups['action'].Value
                -not $approvedActionPins.ContainsKey($action) -or
                $approvedActionPins[$action] -cne $_.Groups['revision'].Value
            }).Count -eq 0) `
    -Message 'The CI workflow uses an unapproved or mutable Action revision.'
Assert-Condition `
    -Condition ($workflow -match '(?m)^permissions:\s*\r?\n\s*contents:\s*read\s*$' -and
        $workflow -match '(?m)^\s*fetch-depth:\s*0\s*$' -and
        [regex]::Matches(
            $workflow,
            '(?m)^\s*persist-credentials:\s*false\s*$').Count -eq 2 -and
        [regex]::Matches(
            $workflow,
            '(?m)^\s*node-version-file:\s*[.]nvmrc\s*$').Count -eq 2 -and
        [regex]::Matches(
            $workflow,
            '(?m)^\s*check-latest:\s*true\s*$').Count -eq 2) `
    -Message 'The CI workflow diverged from its least-privilege or compatible-toolchain boundary.'
Assert-Condition `
    -Condition ([regex]::Matches(
            $workflow,
            "(?m)^\s*dotnet-version:\s*'10[.]0[.]x'\s*$").Count -eq 1 -and
        [regex]::Matches(
            $workflow,
            "(?m)^\s*dotnet-quality:\s*'ga'\s*$").Count -eq 1 -and
        -not $workflow.Contains('global-json-file:', [System.StringComparison]::Ordinal)) `
    -Message 'The CI workflow must install the current stable .NET 10.0 SDK.'
Assert-Condition `
    -Condition ($workflow -match '(?m)^\s*timeout-minutes:\s*135\s*$') `
    -Message 'The sequential canonical CI job lacks its bounded capacity envelope.'

foreach ($requiredGateReference in @(
        'verify-dotnet-lockfiles.ps1',
        'verify-dotnet-coverage.ps1',
        'verify-nuget-vulnerabilities.ps1',
        'audit-fail-closed-runtime.ps1',
        'run-legacy-tests.ps1',
        'run-state05-dashboard-audit.ps1',
        'run-state06-consolidated-e2e.ps1',
        'verify-secrets.ps1',
        'DBNotifier.RunnerProcess.Tests.ps1',
        'DBNotifier.ScriptSyntax.Tests.ps1')) {
    Assert-Condition `
        -Condition ([regex]::Matches(
                $ciScript,
                [regex]::Escape($requiredGateReference)).Count -eq 1) `
        -Message "The canonical gate must retain '$requiredGateReference' exactly once."
}
Assert-Condition `
    -Condition ($ciScript -match '(?s)function Invoke-LegacyChecks\s*\{.*?\[Parameter\(Mandatory\)\]\s*\[string\]\$DotNetExecutable.*?run-legacy-tests[.]ps1.*?-DotNetPath\s+\$DotNetExecutable' -and
        [regex]::Matches(
            $ciScript,
            '(?m)^\s*Invoke-LegacyChecks -DotNetExecutable \$dotnetExecutable\s*$').Count -eq 1) `
    -Message 'The canonical gate must propagate its exact dotnet host through the legacy boundary.'
Assert-Condition `
    -Condition ($legacyRunnerScript -match '(?s)\[Parameter\(Mandatory\)\]\s*\[ValidateNotNullOrEmpty\(\)\]\s*\[string\]\$DotNetPath.*?Parameters\s*=\s*@\{\s*DotNetPath\s*=\s*\$resolvedDotNetPath' -and
        -not $legacyRunnerScript.Contains('.dotnet\dotnet.exe', [System.StringComparison]::OrdinalIgnoreCase)) `
    -Message 'The legacy runner must require and bind the caller-selected dotnet host to Pester.'
$legacyVerifierInvocations = [regex]::Matches(
    $legacyTestsScript,
    '(?m)^\s*\{\s*& \$gatePath(?<arguments>[^\r\n]+)')
Assert-Condition `
    -Condition ($legacyVerifierInvocations.Count -gt 0 -and
        $legacyVerifierInvocations.Count -eq [regex]::Matches($legacyTestsScript, '& \$gatePath').Count -and
        @($legacyVerifierInvocations | Where-Object {
                -not $_.Groups['arguments'].Value.Contains(
                    '-DotNetPath $DotNetPath',
                    [System.StringComparison]::Ordinal)
            }).Count -eq 0) `
    -Message 'Every legacy vulnerability regression must receive the exact caller-selected dotnet host.'
Assert-Condition `
    -Condition ($legacyTestsScript -match '(?s)\[Parameter\(Mandatory\)\]\s*\[ValidateNotNullOrEmpty\(\)\]\s*\[string\]\$DotNetPath' -and
        -not $legacyTestsScript.Contains('.dotnet\dotnet.exe', [System.StringComparison]::OrdinalIgnoreCase)) `
    -Message 'The legacy test suite must require toolchain identity without defining a repository fallback.'
Assert-Condition `
    -Condition ($ciScript -notmatch '(?i)GetEnvironmentVariable\s*\([^)]*(?:OPENAI|AZURE_OPENAI)' -and
        $ciScript -notmatch '(?i)\$env:(?:OPENAI|AZURE_OPENAI)' -and
        $ciScript.IndexOf(
            'Remove-DBNotifierInheritedEnvironment -StartInfo $startInfo',
            [System.StringComparison]::Ordinal) -lt
        $ciScript.IndexOf(
            '$process.Start()',
            [System.StringComparison]::Ordinal)) `
    -Message 'The canonical gate must scrub inherited credentials and activators before child execution without reading them.'
Assert-Condition `
    -Condition ($ciScript -notmatch '(?i)Invoke-Expression|Start-Process') `
    -Message 'The canonical gate must preserve shell-free native process boundaries.'

Write-Output "Development-flow policy tests passed with $assertionCount assertions."
