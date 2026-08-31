# Module purpose: Exercises the bounded continuous-improvement controller without restoring or testing product code.
#Requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$controllerPath = Join-Path $repositoryRoot 'scripts/continuous-improvement.ps1'
$assertionCount = 0
$eventCounter = 0
$script:testBaseline = '1' * 40
$script:testAuthorisedRoot = $null
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'dbnotifier-continuous-improvement-tests-' + [guid]::NewGuid().ToString('N'))

function Assert-Condition {
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

function Assert-Throws {
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Action,

        [Parameter(Mandatory)]
        [string]$Pattern,

        [Parameter(Mandatory)]
        [string]$Message
    )

    try {
        & $Action
    }
    catch {
        Assert-Condition `
            -Condition ($_.Exception.Message -match $Pattern) `
            -Message "$Message Observed: $($_.Exception.Message)"
        return
    }
    throw "$Message No exception was raised."
}

function New-TestEventData {
    param(
        [Parameter(Mandatory)]
        [string]$ImprovementId,

        [Parameter(Mandatory)]
        [string]$EventType,

        [Parameter(Mandatory)]
        [string]$WorkflowState,

        [Parameter(Mandatory)]
        [string]$ActorId,

        [Parameter(Mandatory)]
        [string]$ActorRole,

        [Parameter(Mandatory)]
        [string]$Result,

        [Parameter(Mandatory)]
        [string]$ReasonCode,

        [hashtable]$Override = @{}
    )

    $script:eventCounter++
    $eventTimestamp = [DateTimeOffset]::ParseExact(
        '2026-08-30T12:00:00.0000000Z',
        'yyyy-MM-ddTHH:mm:ss.fffffffZ',
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::AssumeUniversal).AddSeconds($script:eventCounter)
    $eventTimestampText = $eventTimestamp.ToString(
        'yyyy-MM-ddTHH:mm:ss.fffffffZ',
        [Globalization.CultureInfo]::InvariantCulture)
    $data = [ordered]@{
        eventId = [guid]::NewGuid().ToString('D')
        recordedAtUtc = $eventTimestampText
        improvementId = $ImprovementId
        eventType = $EventType
        workflowState = $WorkflowState
        baseline = $script:testBaseline
        executionKey = $null
        executionScopeDigest = $null
        acceptanceDigest = $null
        candidateDigest = $null
        actorId = $ActorId
        actorRole = $ActorRole
        implementingAgents = @()
        oldGateResult = 'NOT_APPLICABLE'
        newGateResult = 'NOT_APPLICABLE'
        promotionId = $null
        observationWindowClosed = $false
        result = $Result
        reasonCode = $ReasonCode
        evidenceDigests = @()
    }
    foreach ($name in $Override.Keys) {
        $data[$name] = $Override[$name]
    }
    return $data
}

function New-TestCausalFacts {
    param(
        [Parameter(Mandatory)][string]$CandidateDigest,
        [Parameter(Mandatory)][string]$PredecessorGateEventHash
    )

    return [ordered]@{
        predecessorGateEventHash = $PredecessorGateEventHash
        changedFactDomain = 'CANDIDATE'
        changedFactDigest = $CandidateDigest
        decision = 'RETRY_WITH_CAUSAL_CHANGE'
    }
}

function Add-TestEvent {
    param(
        [Parameter(Mandatory)]
        [string]$LedgerPath,

        [Parameter(Mandatory)]
        [object]$EventData
    )

    return Add-CILedgerEvent `
        -Path $LedgerPath `
        -AuthorisedRoot $script:testAuthorisedRoot `
        -EventData $EventData `
        -MaximumEvents 128
}

function Read-TestLedger {
    param([Parameter(Mandatory)][string]$Path)

    return @(Read-CILedger `
            -Path $Path `
            -AuthorisedRoot $script:testAuthorisedRoot `
            -MaximumEvents 128)
}

function Repair-TestLedger {
    param([Parameter(Mandatory)][string]$Path)

    return Repair-CILedgerPendingAppend `
        -Path $Path `
        -AuthorisedRoot $script:testAuthorisedRoot `
        -MaximumEvents 128
}

function Add-PassingGovernanceChain {
    param(
        [Parameter(Mandatory)]
        [string]$LedgerPath,

        [Parameter(Mandatory)]
        [string]$ImprovementId,

        [Parameter(Mandatory)]
        [string]$ExecutionKey,

        [Parameter(Mandatory)]
        [string]$ExecutionScopeDigest,

        [Parameter(Mandatory)]
        [string]$AcceptanceDigest,

        [Parameter(Mandatory)]
        [string]$CandidateDigest,

        [Parameter(Mandatory)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory)]
        [string]$CandidateRevision,

        [Parameter(Mandatory)]
        [string]$LastKnownGoodRevision,

        [switch]$ObservationFails,

        [switch]$ExerciseOpaqueRevision,

        [switch]$ExerciseIntegratorObserverCollision
    )

    $evidence = Get-CISha256Hex -Text "evidence-$ImprovementId"
    $promotionId = [guid]::NewGuid().ToString('D')
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
            New-TestEventData -ImprovementId $ImprovementId `
                -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
                -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
                -ReasonCode AUDIT_FINDING))
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
            New-TestEventData -ImprovementId $ImprovementId `
                -EventType TRIAGED -WorkflowState AGENT_DECIDED `
                -ActorId '/analyst' -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED `
                -ReasonCode ROOT_CAUSE_CONFIRMED))
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
            New-TestEventData -ImprovementId $ImprovementId `
                -EventType ATTEMPT_STARTED -WorkflowState AGENT_DECIDED `
                -ActorId '/writer' -ActorRole IMPLEMENTER -Result STARTED `
                -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $ExecutionKey
                    executionScopeDigest = $ExecutionScopeDigest
                    acceptanceDigest = $AcceptanceDigest
                    candidateDigest = $CandidateDigest
                    candidatePaths = @('prompts/governance/Continuous-Improvement.md')
                    implementingAgents = @('/writer')
                }))
    foreach ($reviewer in @('/reviewer-a', '/reviewer-b')) {
        $reviewFindings = if ($reviewer -ceq '/reviewer-a') {
            @([ordered]@{
                    severity = 'P2'
                    findingDigest = Get-CISha256Hex -Text "p2-$ImprovementId"
                    dispositionDecision = 'DEFERRED_TRACKED'
                    dispositionEvidenceDigest = Get-CISha256Hex -Text "p2-disposition-$ImprovementId"
                })
        }
        else {
            @()
        }
        [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
                New-TestEventData -ImprovementId $ImprovementId `
                    -EventType REVIEW_RECORDED -WorkflowState AGENT_DECIDED `
                    -ActorId $reviewer -ActorRole REVIEWER -Result PASS `
                    -ReasonCode INDEPENDENT_REVIEW_PASS -Override @{
                        executionKey = $ExecutionKey
                        candidateDigest = $CandidateDigest
                        reviewFindings = $reviewFindings
                        evidenceDigests = @((Get-CISha256Hex -Text "$reviewer-$ImprovementId"))
                    }))
    }
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
            New-TestEventData -ImprovementId $ImprovementId `
                -EventType GATE_PASSED -WorkflowState AUTOMATED_GATE_PASS `
                -ActorId '/verifier' -ActorRole VERIFIER -Result PASS `
                -ReasonCode META_GATE_PASS -Override @{
                    executionKey = $ExecutionKey
                    candidateDigest = $CandidateDigest
                    oldGateResult = 'PASS'
                    newGateResult = 'PASS'
                    evidenceDigests = @($evidence)
                }))
    if ($ExerciseOpaqueRevision) {
        Assert-Throws {
            Add-TestEvent -LedgerPath $LedgerPath -EventData (
                New-TestEventData -ImprovementId $ImprovementId `
                    -EventType PROMOTED -WorkflowState LOCAL_COMPLETE `
                    -ActorId '/integrator' -ActorRole INTEGRATOR -Result PROMOTED `
                    -ReasonCode EXACT_CANDIDATE_PROMOTED -Override @{
                        executionKey = $ExecutionKey
                        candidateDigest = $CandidateDigest
                        repositoryRoot = $RepositoryRoot
                        candidateRevision = ('f' * 40)
                        lastKnownGoodRevision = $LastKnownGoodRevision
                        promotionPreimageRevision = $LastKnownGoodRevision
                        promotionId = [guid]::NewGuid().ToString('D')
                        evidenceDigests = @($evidence)
                    })
        } 'OPAQUE_REVISION_REJECTED' 'An opaque 40-character revision was accepted.'
    }
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (
            New-TestEventData -ImprovementId $ImprovementId `
                -EventType PROMOTED -WorkflowState LOCAL_COMPLETE `
                -ActorId '/integrator' -ActorRole INTEGRATOR -Result PROMOTED `
                -ReasonCode EXACT_CANDIDATE_PROMOTED -Override @{
                    executionKey = $ExecutionKey
                    candidateDigest = $CandidateDigest
                    repositoryRoot = $RepositoryRoot
                    candidateRevision = $CandidateRevision
                    lastKnownGoodRevision = $LastKnownGoodRevision
                    promotionPreimageRevision = $LastKnownGoodRevision
                    promotionId = $promotionId
                    evidenceDigests = @($evidence)
                }))
    $observationType = if ($ObservationFails) { 'OBSERVATION_FAILED' } else { 'OBSERVATION_PASSED' }
    $observationResult = if ($ObservationFails) { 'FAIL' } else { 'PASS' }
    $observationData = {
        param([string]$ActorId)
        New-TestEventData -ImprovementId $ImprovementId `
            -EventType $observationType -WorkflowState $(
                if ($ObservationFails) { 'AUTOMATED_GATE_FAIL' } else { 'LOCAL_COMPLETE' }) `
            -ActorId $ActorId -ActorRole OBSERVER -Result $observationResult `
            -ReasonCode OBSERVATION_WINDOW_CLOSED -Override @{
                executionKey = $ExecutionKey
                candidateDigest = $CandidateDigest
                observationWindowClosed = $true
                evidenceDigests = @($evidence)
            }
    }
    if ($ExerciseIntegratorObserverCollision) {
        Assert-Throws {
            Add-TestEvent -LedgerPath $LedgerPath -EventData (& $observationData '/integrator')
        } 'OBSERVER_INTEGRATOR_COLLISION' 'The promotion integrator was accepted as observer.'
    }
    [void](Add-TestEvent -LedgerPath $LedgerPath -EventData (& $observationData '/observer'))
    return $promotionId
}

if (-not [System.IO.File]::Exists($controllerPath)) {
    throw 'The continuous-improvement controller is missing.'
}
. $controllerPath

Assert-Throws {
    & $controllerPath -Command ValidateLedger -RepositoryRoot $repositoryRoot
} 'explicit -LedgerPath' 'Ledger commands accepted an implicit repository path.'

$tempParent = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar)
$resolvedTestRoot = [System.IO.Path]::GetFullPath($testRoot)
if (-not $resolvedTestRoot.StartsWith(
        $tempParent + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase) -or
    [System.IO.Path]::GetFileName($resolvedTestRoot) -notmatch
        '^dbnotifier-continuous-improvement-tests-[0-9a-f]{32}$') {
    throw 'Disposable test-root safety check failed.'
}
    [void][System.IO.Directory]::CreateDirectory($resolvedTestRoot)
    $script:testAuthorisedRoot = $resolvedTestRoot

try {
    $promotionRepository = Join-Path $resolvedTestRoot 'promotion-repository'
    [void][System.IO.Directory]::CreateDirectory($promotionRepository)
    & git -C $promotionRepository init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Promotion Git fixture could not be initialised.' }
    & git -C $promotionRepository config core.autocrlf false
    if ($LASTEXITCODE -ne 0) { throw 'Promotion Git fixture line-ending policy could not be set.' }
    $promotionTrackedPath = Join-Path $promotionRepository 'candidate.txt'
    [System.IO.File]::WriteAllText(
        $promotionTrackedPath,
        "last-known-good`n",
        [System.Text.UTF8Encoding]::new($false))
    & git -C $promotionRepository add -- candidate.txt
    & git -C $promotionRepository -c user.name=Policy-Test `
        -c user.email=policy-test.invalid@local commit --quiet -m last-known-good
    if ($LASTEXITCODE -ne 0) { throw 'Promotion LKG fixture could not be committed.' }
    $script:testBaseline = ((& git -C $promotionRepository rev-parse HEAD) -join '').Trim().ToLowerInvariant()
    [System.IO.File]::WriteAllText(
        $promotionTrackedPath,
        "candidate`n",
        [System.Text.UTF8Encoding]::new($false))
    & git -C $promotionRepository add -- candidate.txt
    & git -C $promotionRepository -c user.name=Policy-Test `
        -c user.email=policy-test.invalid@local commit --quiet -m candidate
    if ($LASTEXITCODE -ne 0) { throw 'Promotion candidate fixture could not be committed.' }
    $promotionCandidateRevision = ((& git -C $promotionRepository rev-parse HEAD) -join '').Trim().ToLowerInvariant()

    $fingerprintA = New-CIStableFindingFingerprint `
        -RuleId 'CI-001' `
        -RootCause '  Mutable   retry state ' `
        -Paths @('Prompts\State\Current-State.md', 'scripts/ci.ps1')
    $fingerprintB = New-CIStableFindingFingerprint `
        -RuleId 'CI-001' `
        -RootCause 'mutable retry state' `
        -Paths @('scripts/CI.ps1', 'prompts/state/current-state.md')
    $fingerprintC = New-CIStableFindingFingerprint `
        -RuleId 'CI-001' `
        -RootCause 'different cause' `
        -Paths @('scripts/ci.ps1', 'prompts/state/current-state.md')
    Assert-Condition ($fingerprintA -ceq $fingerprintB) 'Finding fingerprints are not stable.'
    Assert-Condition ($fingerprintA -cne $fingerprintC) 'Finding fingerprints ignore root cause.'
    Assert-Condition ($fingerprintA -match '^[0-9a-f]{64}$') 'Finding fingerprint is malformed.'
    $securityClassification = New-CICandidateClassification `
        -Paths @('src/DBNotifier.Infrastructure/Security/TokenStore.cs')
    Assert-Condition ($securityClassification.riskClass -ceq 'HIGH') `
        'A Security path was not derived as high risk.'
    Assert-Condition ($securityClassification.riskDomains -ccontains 'SECURITY') `
        'A Security path did not derive the SECURITY risk domain.'
    $officialControlPathCases = @(
        [ordered]@{ Path = 'AGENTS.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'PLANS.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/Start-Here.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/foundation/Prompt-New-Project.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/foundation/Solution-Architecture-Document.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/foundation/AIOps-And-AI-Module.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/governance/Continuous-Improvement.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Conversation-Coordination-Prompt.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Governance.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Language-Policy.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Lifecycle.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Quality-Gates.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/governance/Security-And-Access.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/operations/Operational-Playbooks.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/state/Current-State.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/state/Continuous-Improvement-Backlog.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/state/State-Transition-Log.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'prompts/system/AI-Software-Engineering-Master-Prompt.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/system/Prompt-System-Change-Log.md'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'prompts/templates/Templates.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'docs/Code-Documentation-Standards.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'docs/design/DB-Notifier-Design-System.md'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'scripts/development.ps1'; WasPreviouslyMissed = $true },
        [ordered]@{ Path = 'scripts/ci.ps1'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'scripts/continuous-improvement.ps1'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'scripts/verify-development-flow.ps1'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'tests/DBNotifier.ContinuousImprovement.Tests.ps1'; WasPreviouslyMissed = $false },
        [ordered]@{ Path = 'tests/DBNotifier.DevelopmentFlow.Tests.ps1'; WasPreviouslyMissed = $false })
    Assert-Condition ($officialControlPathCases.Count -eq 28) `
        'The official authority/state/development-control matrix is not exactly 28 paths.'
    Assert-Condition (@($officialControlPathCases | Where-Object {
                $_.WasPreviouslyMissed
            }).Count -eq 11) `
        'The table does not identify the 11 previously false STANDARD paths.'
    foreach ($case in $officialControlPathCases) {
        $classification = New-CICandidateClassification -Paths @($case.Path)
        $caseLabel = if ($case.WasPreviouslyMissed) {
            'previous false STANDARD path'
        }
        else {
            'official control path'
        }
        Assert-Condition ($classification.riskClass -ceq 'HIGH' -and
            $classification.governanceChange -and
            $classification.riskDomains -ccontains 'GOVERNANCE_POLICY') `
            "$caseLabel '$($case.Path)' was not derived as HIGH governance."
    }
    foreach ($productPath in @(
            'README.md',
            'src/DBNotifier.Application/Health/HealthService.cs',
            'src/DBNotifier.Dashboard.Web/src/components/StatusCard.tsx')) {
        $classification = New-CICandidateClassification -Paths @($productPath)
        Assert-Condition ($classification.riskClass -ceq 'STANDARD' -and
            -not $classification.governanceChange -and
            $classification.riskDomains.Count -eq 0) `
            "Non-authority product path '$productPath' was promoted to governance risk."
    }
    $additiveClassification = New-CICandidateClassification `
        -Paths @('README.md') `
        -AdditionalRiskDomains @('PERSISTENCE')
    Assert-Condition ($additiveClassification.riskClass -ceq 'HIGH' -and
        $additiveClassification.riskDomains -ccontains 'PERSISTENCE') `
        'Additional risk facts lowered or failed to extend derived risk.'

    $scopeA = Get-CISha256Hex -Text 'scope-a'
    $scopeB = Get-CISha256Hex -Text 'scope-b'
    $acceptance = Get-CISha256Hex -Text 'acceptance'
    $executionA = New-CIExecutionKey -ImprovementId $fingerprintA `
        -Baseline $script:testBaseline -ScopeDigest $scopeA -AcceptanceDigest $acceptance
    $executionB = New-CIExecutionKey -ImprovementId $fingerprintA `
        -Baseline $script:testBaseline -ScopeDigest $scopeB -AcceptanceDigest $acceptance
    Assert-Condition ($executionA -cne $executionB) 'Execution key does not bind exact scope.'
    $dispatchA = New-CIDispatchKey -ExecutionKey $executionA `
        -Route CONTINUE_CURRENT -Source '/root' -Destination '/worker'
    $dispatchB = New-CIDispatchKey -ExecutionKey $executionA `
        -Route CONTINUE_CURRENT -Source ' /ROOT ' -Destination '/WORKER'
    $dispatchC = New-CIDispatchKey -ExecutionKey $executionA `
        -Route DELEGATE_SUBAGENT -Source '/root' -Destination '/worker'
    Assert-Condition ($dispatchA -ceq $dispatchB) 'Dispatch key is not canonical.'
    Assert-Condition ($dispatchA -cne $dispatchC) 'Dispatch key does not bind route.'

    $ledger = Join-Path $resolvedTestRoot 'valid-ledger.jsonl'
    $candidateA = Get-CISha256Hex -Text 'candidate-a'
    $promotionId = Add-PassingGovernanceChain -LedgerPath $ledger `
        -ImprovementId $fingerprintA -ExecutionKey $executionA `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateA -RepositoryRoot $promotionRepository `
        -CandidateRevision $promotionCandidateRevision `
        -LastKnownGoodRevision $script:testBaseline `
        -ExerciseOpaqueRevision -ExerciseIntegratorObserverCollision
    $events = @(Read-TestLedger -Path $ledger)
    Assert-Condition ($events.Count -eq 8) 'Valid lifecycle did not append eight factual events.'
    Assert-Condition ($events[-1].eventType -ceq 'OBSERVATION_PASSED') 'Valid lifecycle did not close observation.'
    Assert-Condition ($events[-1].previousEventHash -ceq $events[-2].eventHash) 'Ledger chain is not linked.'
    Assert-Condition ($events[-2].promotionId -ceq $promotionId) 'Promotion identity changed.'
    $passingGate = @($events | Where-Object { $_.eventType -ceq 'GATE_PASSED' })[0]
    Assert-Condition ($passingGate.reviewFindingCounts.P2 -eq 1 -and
        $passingGate.reviewDispositionDigests.Count -eq 1) `
        'Passing gate did not preserve candidate-bound P2 disposition evidence.'
    $validAttempt = @($events | Where-Object { $_.eventType -ceq 'ATTEMPT_STARTED' })[0]
    $forgedRiskAttempt = $validAttempt | Select-Object *
    $forgedRiskAttempt.candidatePaths = @('src/DBNotifier.Infrastructure/Security/TokenStore.cs')
    $forgedRiskAttempt.eventHash = Get-CISha256Hex -Text (
        (Get-CIEventHashPayload -Event $forgedRiskAttempt | ConvertTo-Json -Depth 12 -Compress))
    Assert-Throws {
        Assert-CIEventShape `
            -Event $forgedRiskAttempt `
            -ExpectedSequence $forgedRiskAttempt.sequence `
            -ExpectedPreviousHash $forgedRiskAttempt.previousEventHash
    } 'risk does not replay from candidate paths' `
        'A re-hashed Security scope omitted its derived risk during replay.'

    $decision = Get-CINextDecision -Events $events -MaximumEvents 128
    Assert-Condition ($decision.action -ceq 'CLOSE') 'Event-driven continuation is not bounded to one close action.'
    Assert-Condition ($decision.eventsRead -eq 8) 'Event-driven continuation did not report its bound.'
    Assert-Throws { Get-CINextDecision -Events $events -MaximumEvents 5 } `
        'LEDGER_BOUND_EXCEEDED' 'The event bound was not enforced.'

    $duplicate = New-TestEventData -ImprovementId $fingerprintA `
        -EventType DUPLICATE_SUPPRESSED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result SUPPRESSED `
        -ReasonCode STABLE_FINGERPRINT_DUPLICATE -Override @{
            executionKey = $executionA
            executionScopeDigest = $scopeA
            acceptanceDigest = $acceptance
        }
    [void](Add-TestEvent -LedgerPath $ledger -EventData $duplicate)
    $events = @(Read-TestLedger -Path $ledger)
    $metrics = Get-CIMetrics -Events $events -MaximumEvents 128
    Assert-Condition ($metrics.uniqueFindings -eq 1) 'Duplicate changed the unique-finding numerator.'
    Assert-Condition ($metrics.duplicateSuppressions -eq 1) 'Duplicate suppression was not counted.'
    Assert-Condition ($metrics.firstPassRate.numerator -eq 1) 'First-pass numerator is incorrect.'
    Assert-Condition ($metrics.firstPassRate.denominator -eq 1) 'First-pass denominator is incorrect.'
    Assert-Condition ($metrics.escapeRate.denominator -eq 1) 'Observed-promotion denominator is incorrect.'
    Assert-Condition ($metrics.observationCoverage.ratio -eq 1) 'Observation coverage is incorrect.'
    Assert-Condition ($metrics.duplicateRate.numerator -eq 1 -and
        $metrics.duplicateRate.denominator -eq 2) 'Duplicate rate lacks exact numerator and denominator.'
    Assert-Condition ($metrics.profile -ceq 'db-notifier-continuous-improvement-metrics' -and
        $metrics.profileVersion -ceq '1.0.0') 'Metrics lack a bound profile and version.'
    Assert-Condition ($metrics.firstPassRate.source.ledgerHeadHash -ceq $events[-1].eventHash -and
        $metrics.firstPassRate.window.endSequence -eq $events[-1].sequence) `
        'Rate source or event window is not ledger-bound.'
    Assert-Condition ($metrics.firstPassRate.direction -ceq 'HIGHER_IS_BETTER' -and
        $metrics.firstPassRate.unit -ceq 'RATIO') 'Rate direction or unit is ambiguous.'

    $emptyMetrics = Get-CIMetrics -Events @() -MaximumEvents 1
    Assert-Condition ($null -eq $emptyMetrics.firstPassRate.ratio) 'Zero-denominator first-pass rate must be null.'
    Assert-Condition ($null -eq $emptyMetrics.escapeRate.ratio) 'Zero-denominator escape rate must be null.'
    Assert-Condition ($null -eq $emptyMetrics.observationCoverage.ratio) 'Zero-denominator coverage must be null.'

    $lockedLedger = Join-Path $resolvedTestRoot 'locked-ledger.jsonl'
    [System.IO.File]::WriteAllText($lockedLedger, '', [System.Text.UTF8Encoding]::new($false))
    $lock = [System.IO.File]::Open($lockedLedger, [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        Assert-Throws {
            Add-TestEvent -LedgerPath $lockedLedger -EventData (
                New-TestEventData -ImprovementId $fingerprintC `
                    -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
                    -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
                    -ReasonCode AUDIT_FINDING)
        } 'MUTABLE_RESOURCE_COLLISION' 'Concurrent append was not rejected.'
    }
    finally {
        $lock.Dispose()
    }

    $validText = [System.IO.File]::ReadAllText($ledger)
    $duplicateResultPath = Join-Path $resolvedTestRoot 'duplicate-result.jsonl'
    [System.IO.File]::WriteAllText(
        $duplicateResultPath,
        [regex]::Replace(
            $validText,
            '"result":"PASS"',
            '"result":"PASS","result":"PASS"',
            1),
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $duplicateResultPath } `
        'DUPLICATE_JSON_PROPERTY' `
        'A canonical valid-hash event with duplicate result properties was accepted.'
    $nestedDuplicatePath = Join-Path $resolvedTestRoot 'nested-duplicate.jsonl'
    [System.IO.File]::WriteAllText(
        $nestedDuplicatePath,
        [regex]::Replace(
            $validText,
            '"reviewFindingCounts":\{"P0":0',
            '"reviewFindingCounts":{"P0":0,"P0":0',
            1),
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $nestedDuplicatePath } `
        'DUPLICATE_JSON_PROPERTY' 'A recursively duplicated JSON property was accepted.'
    $tamperedPath = Join-Path $resolvedTestRoot 'tampered.jsonl'
    [System.IO.File]::WriteAllText(
        $tamperedPath,
        [regex]::Replace($validText, '"result":"PASS"', '"result":"FAIL"', 1),
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $tamperedPath } `
        'modified after append' 'Ledger tampering was not detected.'
    $lines = @($validText.TrimEnd("`n").Split("`n"))
    $reorderedPath = Join-Path $resolvedTestRoot 'reordered.jsonl'
    [System.IO.File]::WriteAllText(
        $reorderedPath,
        (@($lines[1], $lines[0]) + $lines[2..($lines.Count - 1)] -join "`n") + "`n",
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $reorderedPath } `
        'schema or sequence' 'Ledger reordering was not detected.'
    $deletedPath = Join-Path $resolvedTestRoot 'deleted.jsonl'
    [System.IO.File]::WriteAllText(
        $deletedPath,
        (@($lines[0]) + $lines[2..($lines.Count - 1)] -join "`n") + "`n",
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $deletedPath } `
        'schema or sequence' 'Ledger deletion was not detected.'
    $partialPath = Join-Path $resolvedTestRoot 'partial.jsonl'
    [System.IO.File]::WriteAllText(
        $partialPath,
        $validText.TrimEnd("`n"),
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Read-TestLedger -Path $partialPath } `
        'complete event line' 'Partial append was not detected.'

    $bomPath = Join-Path $resolvedTestRoot 'bom-ledger.jsonl'
    $bomEncoding = [System.Text.UTF8Encoding]::new($true)
    $bomBytes = [byte[]]($bomEncoding.GetPreamble() +
        [System.Text.UTF8Encoding]::new($false).GetBytes($validText))
    [System.IO.File]::WriteAllBytes($bomPath, $bomBytes)
    Assert-Throws { Read-TestLedger -Path $bomPath } `
        'UTF8_BOM_FORBIDDEN' 'A UTF-8 BOM was accepted by the ledger reader.'
    Assert-Throws {
        Read-CILedger `
            -Path (Join-Path $tempParent 'outside-authorised-ledger.jsonl') `
            -AuthorisedRoot $resolvedTestRoot `
            -MaximumEvents 128
    } 'PATH_OUTSIDE_AUTHORISED_ROOT' 'A ledger outside its authorised root was accepted.'

    $pendingFinding = New-TestEventData -ImprovementId $fingerprintC `
        -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
        -ReasonCode PENDING_RECOVERY_FINDING
    $pendingBeforeLedger = Join-Path $resolvedTestRoot 'pending-before-ledger.jsonl'
    Assert-Throws {
        Add-CILedgerEvent -Path $pendingBeforeLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $pendingFinding `
            -MaximumEvents 128 -TestFaultPoint AfterPendingFlush
    } 'TEST_CRASH_AFTER_PENDING_FLUSH' 'The pre-append crash point did not preserve a pending journal.'
    Assert-Condition ([System.IO.File]::Exists($pendingBeforeLedger + '.pending')) `
        'The flushed pending journal was not preserved after the simulated crash.'
    Assert-Throws { Read-TestLedger -Path $pendingBeforeLedger } `
        'PENDING_APPEND_RECOVERY_REQUIRED' 'Read silently repaired a pending append.'
    $validPendingText = [System.IO.File]::ReadAllText($pendingBeforeLedger + '.pending')
    $pendingObject = $validPendingText | ConvertFrom-Json
    $duplicatePendingText = $validPendingText.Replace(
        '"pendingHash":"' + $pendingObject.pendingHash + '"',
        '"pendingHash":"' + $pendingObject.pendingHash + '","pendingHash":"' +
            $pendingObject.pendingHash + '"')
    [System.IO.File]::WriteAllText(
        $pendingBeforeLedger + '.pending',
        $duplicatePendingText,
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws { Repair-TestLedger -Path $pendingBeforeLedger } `
        'DUPLICATE_JSON_PROPERTY' 'A pending journal with duplicate JSON properties was accepted.'
    [System.IO.File]::WriteAllText(
        $pendingBeforeLedger + '.pending',
        $validPendingText,
        [System.Text.UTF8Encoding]::new($false))
    $pendingBeforeRecovery = Repair-TestLedger -Path $pendingBeforeLedger
    Assert-Condition ($pendingBeforeRecovery.disposition -ceq 'PENDING_APPEND_APPLIED') `
        'An intact pending append was not applied exactly once.'
    Assert-Condition (@(Read-TestLedger -Path $pendingBeforeLedger).Count -eq 1) `
        'Recovered pending append did not produce exactly one event.'
    Assert-Condition (-not [System.IO.File]::Exists($pendingBeforeLedger + '.pending')) `
        'Applied pending journal was not safely cleaned up.'

    $idempotentPendingLedger = Join-Path $resolvedTestRoot 'pending-idempotent-ledger.jsonl'
    $idempotentPendingFinding = New-TestEventData -ImprovementId (
        New-CIStableFindingFingerprint -RuleId 'CI-PENDING-IDEMPOTENT' `
            -RootCause 'idempotent pending recovery' -Paths @('scripts/continuous-improvement.ps1')) `
        -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
        -ReasonCode IDEMPOTENT_PENDING_RECOVERY
    Assert-Throws {
        Add-CILedgerEvent -Path $idempotentPendingLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $idempotentPendingFinding `
            -MaximumEvents 128 -TestFaultPoint AfterPendingFlush
    } 'TEST_CRASH_AFTER_PENDING_FLUSH' 'The idempotent-recovery fixture did not preserve a pending journal.'
    $idempotentRecovered = Add-CILedgerEvent -Path $idempotentPendingLedger `
        -AuthorisedRoot $script:testAuthorisedRoot -EventData $idempotentPendingFinding `
        -MaximumEvents 128
    $idempotentEvents = @(Read-TestLedger -Path $idempotentPendingLedger)
    Assert-Condition ($idempotentRecovered.eventId -ceq $idempotentPendingFinding.eventId) `
        'An exact pending retry did not return its repaired event receipt.'
    Assert-Condition ($idempotentEvents.Count -eq 1 -and
        $idempotentEvents[0].eventId -ceq $idempotentPendingFinding.eventId) `
        'An exact pending retry duplicated the recovered event.'

    $orderedPendingLedger = Join-Path $resolvedTestRoot 'pending-successor-ledger.jsonl'
    $orderedPendingFinding = New-TestEventData -ImprovementId (
        New-CIStableFindingFingerprint -RuleId 'CI-PENDING-SUCCESSOR' `
            -RootCause 'ordered pending successor' -Paths @('scripts/continuous-improvement.ps1')) `
        -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
        -ReasonCode PENDING_SUCCESSOR_FINDING
    Assert-Throws {
        Add-CILedgerEvent -Path $orderedPendingLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $orderedPendingFinding `
            -MaximumEvents 128 -TestFaultPoint AfterPendingFlush
    } 'TEST_CRASH_AFTER_PENDING_FLUSH' 'The ordered-successor fixture did not preserve event A.'
    $orderedSuccessor = New-TestEventData -ImprovementId $orderedPendingFinding.improvementId `
        -EventType TRIAGED -WorkflowState AGENT_DECIDED `
        -ActorId '/analyst' -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED `
        -ReasonCode ROOT_CAUSE_CONFIRMED
    $returnedSuccessor = Add-CILedgerEvent -Path $orderedPendingLedger `
        -AuthorisedRoot $script:testAuthorisedRoot -EventData $orderedSuccessor `
        -MaximumEvents 128
    $orderedEvents = @(Read-TestLedger -Path $orderedPendingLedger)
    Assert-Condition ($returnedSuccessor.eventId -ceq $orderedSuccessor.eventId -and
        $returnedSuccessor.eventType -ceq 'TRIAGED') `
        'Recovery reported success for pending event A instead of requested event B.'
    Assert-Condition ($orderedEvents.Count -eq 2 -and
        $orderedEvents[0].eventId -ceq $orderedPendingFinding.eventId -and
        $orderedEvents[1].eventId -ceq $orderedSuccessor.eventId -and
        $orderedEvents[1].previousEventHash -ceq $orderedEvents[0].eventHash) `
        'Pending event A and requested event B were not stored once in causal order.'

    $pendingAfterLedger = Join-Path $resolvedTestRoot 'pending-after-ledger.jsonl'
    $postFlushFinding = New-TestEventData -ImprovementId (
        New-CIStableFindingFingerprint -RuleId 'CI-PENDING-POST' `
            -RootCause 'post flush recovery' -Paths @('scripts/continuous-improvement.ps1')) `
        -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
        -ReasonCode POST_FLUSH_RECOVERY
    Assert-Throws {
        Add-CILedgerEvent -Path $pendingAfterLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $postFlushFinding `
            -MaximumEvents 128 -TestFaultPoint AfterLedgerFlush
    } 'TEST_CRASH_AFTER_LEDGER_FLUSH' 'The post-append crash point did not preserve a pending journal.'
    $postFlushRecovery = Repair-TestLedger -Path $pendingAfterLedger
    Assert-Condition ($postFlushRecovery.disposition -ceq 'PENDING_APPEND_ALREADY_APPLIED') `
        'A fully appended pending event was duplicated during recovery.'
    Assert-Condition (@(Read-TestLedger -Path $pendingAfterLedger).Count -eq 1) `
        'Post-flush recovery did not retain exactly one event.'

    $truncatedLedger = Join-Path $resolvedTestRoot 'pending-truncated-ledger.jsonl'
    $truncatedFinding = New-TestEventData -ImprovementId (
        New-CIStableFindingFingerprint -RuleId 'CI-PENDING-TRUNCATED' `
            -RootCause 'truncated append recovery' -Paths @('scripts/continuous-improvement.ps1')) `
        -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
        -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
        -ReasonCode TRUNCATED_APPEND_RECOVERY
    Assert-Throws {
        Add-CILedgerEvent -Path $truncatedLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $truncatedFinding `
            -MaximumEvents 128 -TestFaultPoint AfterPendingFlush
    } 'TEST_CRASH_AFTER_PENDING_FLUSH' 'Truncated-recovery fixture did not preserve a pending journal.'
    $truncatedJournal = [System.IO.File]::ReadAllText($truncatedLedger + '.pending') |
        ConvertFrom-Json
    $truncatedEventBytes = [Convert]::FromBase64String($truncatedJournal.eventBytesBase64)
    $truncatedLength = [Math]::Max(1, [Math]::Floor($truncatedEventBytes.Length / 2))
    $truncatedBytes = [byte[]]::new($truncatedLength)
    [Array]::Copy($truncatedEventBytes, $truncatedBytes, $truncatedLength)
    [System.IO.File]::WriteAllBytes($truncatedLedger, $truncatedBytes)
    $truncatedRecovery = Repair-TestLedger -Path $truncatedLedger
    Assert-Condition ($truncatedRecovery.disposition -ceq 'PENDING_APPEND_TRUNCATED_RECOVERED') `
        'A hash-matched truncated append was not recovered atomically.'
    Assert-Condition (@(Read-TestLedger -Path $truncatedLedger).Count -eq 1) `
        'Truncated append recovery did not produce exactly one event.'

    $corruptPendingLedger = Join-Path $resolvedTestRoot 'pending-corrupt-ledger.jsonl'
    [System.IO.File]::WriteAllText(
        $corruptPendingLedger,
        '',
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $corruptPendingLedger + '.pending',
        "{corrupt}`n",
        [System.Text.UTF8Encoding]::new($false))
    Assert-Throws {
        Repair-TestLedger -Path $corruptPendingLedger
    } 'PENDING_APPEND_CORRUPT' 'A corrupt pending journal did not fail closed.'
    Assert-Condition ([System.IO.File]::Exists($corruptPendingLedger + '.pending')) `
        'A corrupt pending journal was deleted instead of quarantined.'

    $occupiedPendingLedger = Join-Path $resolvedTestRoot 'pending-directory-ledger.jsonl'
    [System.IO.File]::WriteAllText(
        $occupiedPendingLedger,
        '',
        [System.Text.UTF8Encoding]::new($false))
    [void][System.IO.Directory]::CreateDirectory($occupiedPendingLedger + '.pending')
    Assert-Throws {
        Repair-TestLedger -Path $occupiedPendingLedger
    } 'UNSAFE_PENDING_PATH' 'A directory was accepted as a pending journal.'
    Assert-Condition ([System.IO.Directory]::Exists($occupiedPendingLedger + '.pending')) `
        'Unsafe pending-path rejection removed the occupied directory.'

    $executionPendingLedger = Join-Path $resolvedTestRoot 'pending-execution-ledger.jsonl'
    $executionPendingId = New-CIStableFindingFingerprint -RuleId 'CI-PENDING-EXECUTION' `
        -RootCause 'canonical execution identity survives pending recovery' `
        -Paths @('scripts/continuous-improvement.ps1')
    $executionPendingKey = New-CIExecutionKey -ImprovementId $executionPendingId `
        -Baseline $script:testBaseline -ScopeDigest $scopeA -AcceptanceDigest $acceptance
    [void](Add-TestEvent -LedgerPath $executionPendingLedger -EventData (
            New-TestEventData -ImprovementId $executionPendingId `
                -EventType FINDING_DETECTED -WorkflowState AGENT_DECIDED `
                -ActorId '/scout' -ActorRole SCOUT -Result RECORDED `
                -ReasonCode PENDING_EXECUTION_FINDING))
    [void](Add-TestEvent -LedgerPath $executionPendingLedger -EventData (
            New-TestEventData -ImprovementId $executionPendingId `
                -EventType TRIAGED -WorkflowState AGENT_DECIDED `
                -ActorId '/analyst' -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED `
                -ReasonCode ROOT_CAUSE_CONFIRMED))
    $executionPendingAttempt = New-TestEventData -ImprovementId $executionPendingId `
        -EventType ATTEMPT_STARTED -WorkflowState AGENT_DECIDED `
        -ActorId '/writer' -ActorRole IMPLEMENTER -Result STARTED `
        -ReasonCode CANDIDATE_STARTED -Override @{
            executionKey = $executionPendingKey
            executionScopeDigest = $scopeA
            acceptanceDigest = $acceptance
            candidateDigest = $candidateA
            candidatePaths = @('scripts/continuous-improvement.ps1')
            implementingAgents = @('/writer')
        }
    Assert-Throws {
        Add-CILedgerEvent -Path $executionPendingLedger `
            -AuthorisedRoot $script:testAuthorisedRoot -EventData $executionPendingAttempt `
            -MaximumEvents 128 -TestFaultPoint AfterPendingFlush
    } 'TEST_CRASH_AFTER_PENDING_FLUSH' `
        'The canonical execution fixture did not preserve a pending journal.'
    $executionPendingRecovered = Add-CILedgerEvent -Path $executionPendingLedger `
        -AuthorisedRoot $script:testAuthorisedRoot -EventData $executionPendingAttempt `
        -MaximumEvents 128
    $executionPendingEvents = @(Read-TestLedger -Path $executionPendingLedger)
    Assert-Condition ($executionPendingRecovered.eventId -ceq $executionPendingAttempt.eventId) `
        'An exact pending execution retry did not return its repaired receipt.'
    Assert-Condition ($executionPendingEvents.Count -eq 3 -and
        @($executionPendingEvents | Where-Object {
                $_.eventType -ceq 'ATTEMPT_STARTED'
            }).Count -eq 1) `
        'Pending recovery duplicated or omitted the canonical execution attempt.'
    Assert-Condition ($executionPendingEvents[-1].executionKey -ceq $executionPendingKey -and
        $executionPendingEvents[-1].executionScopeDigest -ceq $scopeA -and
        $executionPendingEvents[-1].acceptanceDigest -ceq $acceptance) `
        'Pending recovery changed the canonical execution identity inputs.'
    Assert-Condition (-not [System.IO.File]::Exists($executionPendingLedger + '.pending')) `
        'Canonical execution recovery did not safely clean up the pending journal.'

    $retryLedger = Join-Path $resolvedTestRoot 'retry-ledger.jsonl'
    $retryId = New-CIStableFindingFingerprint -RuleId 'CI-RETRY' `
        -RootCause 'unchanged candidate retry' -Paths @('scripts/ci.ps1')
    $retryExecution = New-CIExecutionKey -ImprovementId $retryId `
        -Baseline $script:testBaseline -ScopeDigest $scopeA -AcceptanceDigest $acceptance
    $retryDriftExecution = New-CIExecutionKey -ImprovementId $retryId `
        -Baseline $script:testBaseline -ScopeDigest $scopeB -AcceptanceDigest $acceptance
    $arbitraryExecutionKey = Get-CISha256Hex -Text 'arbitrary-well-formed-execution-key'
    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType FINDING_DETECTED `
                -WorkflowState AGENT_DECIDED -ActorId '/scout' -ActorRole SCOUT `
                -Result RECORDED -ReasonCode AUDIT_FINDING))
    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType TRIAGED `
                -WorkflowState AGENT_DECIDED -ActorId '/analyst' `
                -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED `
                -ReasonCode ROOT_CAUSE_CONFIRMED))
    $initialAdmission = Get-CIAttemptAdmission `
        -Events @(Read-TestLedger -Path $retryLedger) `
        -ImprovementId $retryId `
        -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA `
        -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateA `
        -MaximumAttempts 3
    Assert-Condition ($initialAdmission.disposition -ceq 'ADMIT' -and
        $initialAdmission.reasonCode -ceq 'INITIAL_ATTEMPT_ACCEPTED') `
        'An exact TRIAGED initial candidate was not admitted.'
    $arbitraryExecutionAdmission = Get-CIAttemptAdmission `
        -Events @(Read-TestLedger -Path $retryLedger) `
        -ImprovementId $retryId `
        -ExecutionKey $arbitraryExecutionKey `
        -ExecutionScopeDigest $scopeA `
        -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateA `
        -MaximumAttempts 3
    Assert-Condition ($arbitraryExecutionAdmission.reasonCode -ceq
        'EXECUTION_KEY_IDENTITY_INVALID') `
        'An arbitrary well-formed SHA-256 execution key was admitted.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $arbitraryExecutionKey
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateA
                    candidatePaths = @('scripts/ci.ps1')
                    implementingAgents = @('/writer')
                })
    } 'EXECUTION_KEY_IDENTITY_INVALID' `
        'Append admission accepted an arbitrary well-formed execution key.'
    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $retryExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateA
                    candidatePaths = @('scripts/ci.ps1')
                    implementingAgents = @('/writer')
                }))
    Assert-Throws {
        Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType REVIEW_RECORDED `
                -WorkflowState AGENT_DECIDED -ActorId '/reviewer-a' -ActorRole REVIEWER `
                -Result PASS -ReasonCode INDEPENDENT_REVIEW_PASS -Override @{
                    executionKey = $arbitraryExecutionKey
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateA
                    evidenceDigests = @((Get-CISha256Hex -Text 'arbitrary-review-key'))
                })
    } 'EXECUTION_KEY_IDENTITY_INVALID' `
        'A later event admitted an arbitrary well-formed execution key.'
    $failedGateA = Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType GATE_FAILED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/verifier' -ActorRole VERIFIER `
                -Result FAIL -ReasonCode TEST_FAILURE -Override @{
                    executionKey = $retryExecution
                    candidateDigest = $candidateA
                    implementingAgents = @('/writer')
                    oldGateResult = 'PASS'
                    newGateResult = 'FAIL'
                    evidenceDigests = @((Get-CISha256Hex -Text 'failure'))
                })
    $retryEvents = @(Read-TestLedger -Path $retryLedger)
    $forgedExecutionAttempt = $retryEvents[2] | Select-Object *
    $forgedExecutionAttempt.executionKey = $arbitraryExecutionKey
    $forgedExecutionAttempt.eventHash = Get-CISha256Hex -Text (
        (Get-CIEventHashPayload -Event $forgedExecutionAttempt |
            ConvertTo-Json -Depth 12 -Compress))
    Assert-Throws {
        Assert-CILedgerEvents `
            -Events @($retryEvents[0], $retryEvents[1], $forgedExecutionAttempt) `
            -MaximumEvents 128
    } 'EXECUTION_KEY_IDENTITY_INVALID' `
        'Full ledger replay accepted a valid-hash arbitrary execution key.'
    $sameCandidate = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateA -MaximumAttempts 3
    Assert-Condition ($sameCandidate.reasonCode -ceq 'SAME_CANDIDATE_RETRY') 'Same candidate was not quarantined.'
    $candidateB = Get-CISha256Hex -Text 'candidate-b'
    $missingDelta = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateB -MaximumAttempts 3
    Assert-Condition ($missingDelta.reasonCode -ceq 'CAUSAL_DELTA_REQUIRED') 'Missing causal delta was not quarantined.'
    $arbitraryDelta = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateB -CausalDeltaDigest ([guid]::NewGuid().ToString('D')) `
        -MaximumAttempts 3
    Assert-Condition ($arbitraryDelta.reasonCode -ceq 'CALLER_SUPPLIED_CAUSAL_DELTA') `
        'An arbitrary caller-supplied causal GUID bypassed derived receipts.'
    $forgedShaFacts = [ordered]@{
        predecessorGateEventHash = $failedGateA.eventHash
        changedFactDomain = 'DEPENDENCY'
        changedFactDigest = Get-CISha256Hex -Text 'unreceipted-arbitrary-sha'
        decision = 'RETRY_WITH_CAUSAL_CHANGE'
    }
    $forgedShaAdmission = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateB -CausalDeltaFacts $forgedShaFacts `
        -MaximumAttempts 3
    Assert-Condition ($forgedShaAdmission.reasonCode -ceq 'CAUSAL_DELTA_REQUIRED') `
        'Arbitrary SHA strings without a domain-bound ledger receipt were admitted.'
    $causalFactsA = New-TestCausalFacts `
        -CandidateDigest $candidateB `
        -PredecessorGateEventHash $failedGateA.eventHash
    $admitted = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $candidateB -CausalDeltaFacts $causalFactsA -MaximumAttempts 3
    Assert-Condition ($admitted.disposition -ceq 'ADMIT') 'New causal delta was not admitted.'
    Assert-Condition ($admitted.causalDeltaDigest -match '^[0-9a-f]{64}$') `
        'Admitted causal facts did not produce a controller-derived digest.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $retryExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateA
                    candidatePaths = @('scripts/ci.ps1')
                    implementingAgents = @('/writer')
                })
    } 'SAME_CANDIDATE_RETRY' 'Append path accepted a same-candidate retry.'

    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $retryExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    candidatePaths = @('scripts/ci.ps1')
                    causalDeltaFacts = $causalFactsA
                    implementingAgents = @('/writer')
                }))
    $attemptInProgressAdmission = Get-CIAttemptAdmission `
        -Events @(Read-TestLedger -Path $retryLedger) `
        -ImprovementId $retryId `
        -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA `
        -AcceptanceDigest $acceptance `
        -CandidateDigest (Get-CISha256Hex -Text 'candidate-in-progress-successor') `
        -CausalDeltaFacts $causalFactsA `
        -MaximumAttempts 3
    Assert-Condition ($attemptInProgressAdmission.reasonCode -ceq 'ATTEMPT_PREDECESSOR_INVALID') `
        'A latest ATTEMPT_STARTED event admitted another candidate.'
    $failedGateB = Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType GATE_FAILED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/verifier' -ActorRole VERIFIER `
                -Result FAIL -ReasonCode TEST_FAILURE -Override @{
                    executionKey = $retryExecution
                    candidateDigest = $candidateB
                    implementingAgents = @('/writer')
                    oldGateResult = 'PASS'
                    newGateResult = 'FAIL'
                    evidenceDigests = @((Get-CISha256Hex -Text 'failure-b'))
                })
    $candidateC = Get-CISha256Hex -Text 'candidate-c'
    $causalFactsB = New-TestCausalFacts `
        -CandidateDigest $candidateC `
        -PredecessorGateEventHash $failedGateB.eventHash
    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $retryExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateC
                    candidatePaths = @('scripts/ci.ps1')
                    causalDeltaFacts = $causalFactsB
                    implementingAgents = @('/writer')
                }))
    [void](Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType GATE_FAILED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/verifier' -ActorRole VERIFIER `
                -Result FAIL -ReasonCode TEST_FAILURE -Override @{
                    executionKey = $retryExecution
                    candidateDigest = $candidateC
                    implementingAgents = @('/writer')
                    oldGateResult = 'PASS'
                    newGateResult = 'FAIL'
                    evidenceDigests = @((Get-CISha256Hex -Text 'failure-c'))
                }))
    $retryEvents = @(Read-TestLedger -Path $retryLedger)
    $budgetResult = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest (Get-CISha256Hex -Text 'candidate-d') `
        -MaximumAttempts 3
    Assert-Condition ($budgetResult.reasonCode -ceq 'ATTEMPT_BUDGET_EXHAUSTED') 'Attempt budget did not quarantine a fourth candidate.'
    $driftResult = Get-CIAttemptAdmission -Events $retryEvents `
        -ImprovementId $retryId -ExecutionKey $retryDriftExecution `
        -ExecutionScopeDigest $scopeB -AcceptanceDigest $acceptance `
        -CandidateDigest (Get-CISha256Hex -Text 'candidate-d') `
        -MaximumAttempts 3
    Assert-Condition ($driftResult.reasonCode -ceq 'EXECUTION_KEY_DRIFT') 'Execution-key drift reset the attempt budget.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $retryLedger -EventData (
            New-TestEventData -ImprovementId $retryId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $retryExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = (Get-CISha256Hex -Text 'candidate-d')
                    candidatePaths = @('scripts/ci.ps1')
                    implementingAgents = @('/writer')
                })
    } 'ATTEMPT_BUDGET_EXHAUSTED' 'Ledger append accepted a fourth candidate attempt.'

    $overlapLedger = Join-Path $resolvedTestRoot 'overlap-ledger.jsonl'
    [void](Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType FINDING_DETECTED `
                -WorkflowState AGENT_DECIDED -ActorId '/scout' -ActorRole SCOUT `
                -Result RECORDED -ReasonCode AUDIT_FINDING))
    [void](Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType TRIAGED `
                -WorkflowState AGENT_DECIDED -ActorId '/analyst' `
                -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED -ReasonCode ROOT_CAUSE_CONFIRMED))
    $overlapExecution = New-CIExecutionKey -ImprovementId $fingerprintC `
        -Baseline $script:testBaseline -ScopeDigest $scopeB -AcceptanceDigest $acceptance
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    candidatePaths = @('prompts/governance/Continuous-Improvement.md')
                    implementingAgents = @('/writer')
                    governanceChange = $false
                    riskClass = 'STANDARD'
                })
    } 'CALLER_SUPPLIED_CLASSIFICATION' `
        'Caller-supplied false/STANDARD classification bypassed governed-path risk.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    candidatePaths = @('README.md')
                    contractRiskFacts = @('caller-low-risk')
                    implementingAgents = @('/writer')
                })
    } 'contractRiskFacts is not an authority surface' `
        'Legacy ContractRiskFacts remained an optional authority surface.'
    [void](Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    candidatePaths = @('prompts/governance/Continuous-Improvement.md')
                    implementingAgents = @('/writer')
                }))
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType REVIEW_RECORDED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole REVIEWER `
                -Result PASS -ReasonCode INDEPENDENT_REVIEW_PASS -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    evidenceDigests = @((Get-CISha256Hex -Text 'overlap-evidence'))
                })
    } 'cannot implement and independently review' 'Role overlap was not rejected.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType GATE_FAILED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/writer' -ActorRole VERIFIER `
                -Result FAIL -ReasonCode META_GATE_FAIL -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    oldGateResult = 'PASS'
                    newGateResult = 'FAIL'
                    evidenceDigests = @((Get-CISha256Hex -Text 'writer-verifier-evidence'))
                })
    } 'not disjoint from candidate implementation and review' `
        'A failed gate accepted the candidate implementer as verifier.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType GATE_FAILED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/verifier' -ActorRole VERIFIER `
                -Result FAIL -ReasonCode META_GATE_FAIL -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    oldGateResult = 'PASS'
                    newGateResult = 'PASS'
                    evidenceDigests = @((Get-CISha256Hex -Text 'pass-pass-failure-evidence'))
                })
    } 'applicable failed old/new check' `
        'A governance failed gate accepted factual PASS/PASS old and new checks.'
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType GATE_PASSED `
                -WorkflowState AUTOMATED_GATE_PASS -ActorId '/verifier' -ActorRole VERIFIER `
                -Result PASS -ReasonCode META_GATE_PASS -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    reviewingAgents = @('/reviewer-a', '/reviewer-b')
                    oldGateResult = 'PASS'
                    newGateResult = 'PASS'
                    evidenceDigests = @((Get-CISha256Hex -Text 'phantom-review-evidence'))
                })
    } 'PHANTOM_REVIEWER_REJECTED' 'A phantom reviewer list was accepted without review events.'
    foreach ($reviewer in @('/reviewer-a', '/reviewer-b')) {
        [void](Add-TestEvent -LedgerPath $overlapLedger -EventData (
                New-TestEventData -ImprovementId $fingerprintC `
                    -EventType REVIEW_RECORDED -WorkflowState AGENT_DECIDED `
                    -ActorId $reviewer -ActorRole REVIEWER -Result PASS `
                    -ReasonCode INDEPENDENT_REVIEW_PASS -Override @{
                        executionKey = $overlapExecution
                        executionScopeDigest = $scopeB
                        acceptanceDigest = $acceptance
                        candidateDigest = $candidateB
                        evidenceDigests = @((Get-CISha256Hex -Text "overlap-$reviewer"))
                    }))
    }
    Assert-Throws {
        Add-TestEvent -LedgerPath $overlapLedger -EventData (
            New-TestEventData -ImprovementId $fingerprintC -EventType GATE_PASSED `
                -WorkflowState AUTOMATED_GATE_PASS -ActorId '/verifier' -ActorRole VERIFIER `
                -Result PASS -ReasonCode META_GATE_PASS -Override @{
                    executionKey = $overlapExecution
                    executionScopeDigest = $scopeB
                    acceptanceDigest = $acceptance
                    candidateDigest = $candidateB
                    oldGateResult = 'FAIL'
                    newGateResult = 'PASS'
                    evidenceDigests = @((Get-CISha256Hex -Text 'old-gate-evidence'))
                })
    } 'old and new meta-gates' 'Failed old meta-gate was not rejected.'

    $blockingLedger = Join-Path $resolvedTestRoot 'blocking-review-ledger.jsonl'
    $blockingId = New-CIStableFindingFingerprint -RuleId 'CI-BLOCKING-REVIEW' `
        -RootCause 'blocking review finding' -Paths @('scripts/continuous-improvement.ps1')
    $blockingExecution = New-CIExecutionKey -ImprovementId $blockingId `
        -Baseline $script:testBaseline -ScopeDigest $scopeA -AcceptanceDigest $acceptance
    $blockingCandidate = Get-CISha256Hex -Text 'blocking-candidate'
    [void](Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType FINDING_DETECTED `
                -WorkflowState AGENT_DECIDED -ActorId '/scout' -ActorRole SCOUT `
                -Result RECORDED -ReasonCode AUDIT_FINDING))
    [void](Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType TRIAGED `
                -WorkflowState AGENT_DECIDED -ActorId '/analyst' `
                -ActorRole ROOT_CAUSE_ANALYST -Result TRIAGED -ReasonCode ROOT_CAUSE_CONFIRMED))
    [void](Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType ATTEMPT_STARTED `
                -WorkflowState AGENT_DECIDED -ActorId '/writer' -ActorRole IMPLEMENTER `
                -Result STARTED -ReasonCode CANDIDATE_STARTED -Override @{
                    executionKey = $blockingExecution
                    executionScopeDigest = $scopeA
                    acceptanceDigest = $acceptance
                    candidateDigest = $blockingCandidate
                    candidatePaths = @('scripts/continuous-improvement.ps1')
                    implementingAgents = @('/writer')
                }))
    [void](Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType REVIEW_RECORDED `
                -WorkflowState AGENT_DECIDED -ActorId '/reviewer-a' -ActorRole REVIEWER `
                -Result FAIL -ReasonCode BLOCKING_FINDING -Override @{
                    executionKey = $blockingExecution
                    candidateDigest = $blockingCandidate
                    reviewFindings = @([ordered]@{
                            severity = 'P0'
                            findingDigest = Get-CISha256Hex -Text 'blocking-p0'
                            dispositionDecision = $null
                            dispositionEvidenceDigest = $null
                        })
                    evidenceDigests = @((Get-CISha256Hex -Text 'blocking-review-a'))
                }))
    [void](Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType REVIEW_RECORDED `
                -WorkflowState AGENT_DECIDED -ActorId '/reviewer-b' -ActorRole REVIEWER `
                -Result PASS -ReasonCode INDEPENDENT_REVIEW_PASS -Override @{
                    executionKey = $blockingExecution
                    candidateDigest = $blockingCandidate
                    evidenceDigests = @((Get-CISha256Hex -Text 'blocking-review-b'))
                }))
    Assert-Throws {
        Add-TestEvent -LedgerPath $blockingLedger -EventData (
            New-TestEventData -ImprovementId $blockingId -EventType GATE_PASSED `
                -WorkflowState AUTOMATED_GATE_PASS -ActorId '/verifier' -ActorRole VERIFIER `
                -Result PASS -ReasonCode META_GATE_PASS -Override @{
                    executionKey = $blockingExecution
                    candidateDigest = $blockingCandidate
                    oldGateResult = 'PASS'
                    newGateResult = 'PASS'
                    evidenceDigests = @((Get-CISha256Hex -Text 'blocking-gate'))
                })
    } 'BLOCKING_REVIEW_FINDING' 'A gate passed with a factual P0 review finding.'

    $rollbackLedger = Join-Path $resolvedTestRoot 'rollback-ledger.jsonl'
    $rollbackId = New-CIStableFindingFingerprint -RuleId 'CI-ROLLBACK' `
        -RootCause 'observed regression' -Paths @('prompts/governance/Governance.md')
    $rollbackExecution = New-CIExecutionKey -ImprovementId $rollbackId `
        -Baseline $script:testBaseline -ScopeDigest $scopeA -AcceptanceDigest $acceptance
    $rollbackCandidate = Get-CISha256Hex -Text 'rollback-candidate'
    $rollbackPromotion = Add-PassingGovernanceChain -LedgerPath $rollbackLedger `
        -ImprovementId $rollbackId -ExecutionKey $rollbackExecution `
        -ExecutionScopeDigest $scopeA -AcceptanceDigest $acceptance `
        -CandidateDigest $rollbackCandidate -RepositoryRoot $promotionRepository `
        -CandidateRevision $promotionCandidateRevision `
        -LastKnownGoodRevision $script:testBaseline -ObservationFails
    $rollbackEvidence = Get-CISha256Hex -Text 'rollback-evidence'
    [void](Add-TestEvent -LedgerPath $rollbackLedger -EventData (
            New-TestEventData -ImprovementId $rollbackId -EventType ROLLBACK_REQUIRED `
                -WorkflowState AUTOMATED_GATE_FAIL -ActorId '/observer' -ActorRole OBSERVER `
                -Result ROLLBACK_REQUIRED -ReasonCode OBSERVED_REGRESSION -Override @{
                    executionKey = $rollbackExecution
                    candidateDigest = $rollbackCandidate
                    evidenceDigests = @($rollbackEvidence)
                }))
    [void](Add-TestEvent -LedgerPath $rollbackLedger -EventData (
            New-TestEventData -ImprovementId $rollbackId -EventType ROLLED_BACK `
                -WorkflowState LOCAL_COMPLETE -ActorId '/integrator' -ActorRole INTEGRATOR `
                -Result ROLLED_BACK -ReasonCode LKG_RESTORED -Override @{
                    executionKey = $rollbackExecution
                    candidateDigest = $rollbackCandidate
                    repositoryRoot = $promotionRepository
                    rollbackRevision = $script:testBaseline
                    newGateResult = 'PASS'
                    evidenceDigests = @($rollbackEvidence)
                }))
    $rollbackEvents = @(Read-TestLedger -Path $rollbackLedger)
    Assert-Condition ($rollbackEvents[-1].lastKnownGoodRevision -ceq $script:testBaseline) `
        'Rollback target changed from exact LKG.'
    Assert-Condition ($rollbackEvents[-1].rollbackTree -ceq $rollbackEvents[-1].lastKnownGoodTree) `
        'Rollback did not resolve to the Git-proven LKG tree.'
    $rollbackMetrics = Get-CIMetrics -Events $rollbackEvents -MaximumEvents 128
    Assert-Condition ($rollbackMetrics.escapeRate.numerator -eq 1 -and
        $rollbackMetrics.escapeRate.denominator -eq 1) 'Observed escape metric is incorrect.'
    Assert-Condition ($rollbackMetrics.rollbacks -eq 1) 'Completed rollback was not counted.'

    $fixtureRoot = Join-Path $resolvedTestRoot 'candidate-fixture'
    [void][System.IO.Directory]::CreateDirectory($fixtureRoot)
    & git -C $fixtureRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Disposable Git fixture could not be initialised.' }
    & git -C $fixtureRoot config core.autocrlf false
    if ($LASTEXITCODE -ne 0) { throw 'Disposable Git fixture line-ending policy could not be set.' }
    $trackedPath = Join-Path $fixtureRoot 'tracked.txt'
    [System.IO.File]::WriteAllText($trackedPath, "baseline`n", [System.Text.UTF8Encoding]::new($false))
    & git -C $fixtureRoot add -- tracked.txt
    & git -C $fixtureRoot -c user.name=Policy-Test -c user.email=policy-test.invalid@local `
        commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Disposable Git fixture baseline could not be committed.' }
    $fixtureBaseline = ((& git -C $fixtureRoot rev-parse HEAD) -join '').Trim()
    [System.IO.File]::WriteAllText($trackedPath, "candidate`n", [System.Text.UTF8Encoding]::new($false))
    $newPath = Join-Path $fixtureRoot 'new.txt'
    [System.IO.File]::WriteAllText($newPath, "new`n", [System.Text.UTF8Encoding]::new($false))
    $candidateProof = Assert-CIExactCandidate -RepositoryRoot $fixtureRoot `
        -ExpectedBaseline $fixtureBaseline -ExpectedPaths @('tracked.txt', 'new.txt') `
        -RequireIndexEmpty
    Assert-Condition ($candidateProof.pathCount -eq 2) 'Exact candidate did not include tracked and untracked paths.'
    Assert-Condition ($candidateProof.indexCount -eq 0) 'Exact candidate did not prove an empty index.'
    Assert-Condition ($candidateProof.candidateDigest -match '^[0-9a-f]{64}$') 'Candidate digest is malformed.'
    Assert-Condition ($candidateProof.candidatePaths[0] -ceq 'new.txt' -and
        $candidateProof.candidatePaths[1] -ceq 'tracked.txt') `
        'Exact candidate proof did not preserve Git-reported path casing.'
    Assert-Throws {
        Assert-CIExactCandidate -RepositoryRoot $fixtureRoot `
            -ExpectedBaseline $fixtureBaseline -ExpectedPaths @('TRACKED.txt', 'new.txt') `
            -RequireIndexEmpty
    } 'CANDIDATE_PATH_CASING_MISMATCH' 'Expected candidate casing differed from Git without rejection.'
    Assert-Throws {
        Assert-CIExactCandidate -RepositoryRoot $fixtureRoot `
            -ExpectedBaseline $fixtureBaseline -ExpectedPaths @('tracked.txt') `
            -RequireIndexEmpty
    } 'SCOPE_OVERLAP' 'Unexpected non-ignored candidate path was not rejected.'
    $junctionTarget = Join-Path $resolvedTestRoot 'junction-target'
    [void][System.IO.Directory]::CreateDirectory($junctionTarget)
    [System.IO.File]::WriteAllText(
        (Join-Path $junctionTarget 'receipt.txt'),
        "outside`n",
        [System.Text.UTF8Encoding]::new($false))
    $junctionPath = Join-Path $fixtureRoot 'reparse-candidate'
    try {
        [void](New-Item -ItemType Junction -Path $junctionPath -Target $junctionTarget)
        Assert-Throws {
            Assert-CIPathContained `
                -Path (Join-Path $junctionPath 'receipt.txt') `
                -Root $fixtureRoot `
                -Label 'candidate reparse fixture'
        } 'UNSAFE_PENDING_PATH' 'A reparse-backed candidate path was accepted before hashing.'
    }
    finally {
        if ([System.IO.Directory]::Exists($junctionPath)) {
            Remove-Item -LiteralPath $junctionPath -Force
        }
    }
    & git -C $fixtureRoot add -- new.txt
    Assert-Throws {
        Assert-CIExactCandidate -RepositoryRoot $fixtureRoot `
            -ExpectedBaseline $fixtureBaseline -ExpectedPaths @('tracked.txt', 'new.txt') `
            -RequireIndexEmpty
    } 'empty index' 'A staged candidate was not rejected.'
}
finally {
    if ([System.IO.Directory]::Exists($resolvedTestRoot)) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}

Write-Output "Continuous-improvement policy tests passed with $assertionCount assertions."
