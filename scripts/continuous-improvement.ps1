# Module purpose: Provides the bounded, append-only DB-Notifier continuous-improvement control plane.
#Requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateSet(
        'None',
        'Fingerprint',
        'AppendEvent',
        'RecoverLedger',
        'ValidateLedger',
        'NextDecision',
        'Metrics',
        'ValidateCandidate')]
    [string]$Command = 'None',

    [string]$RepositoryRoot,

    [string]$LedgerPath,

    [string]$InputPath,

    [string]$RuleId,

    [string]$RootCause,

    [string[]]$FingerprintPath = @(),

    [string]$ExpectedBaseline,

    [string[]]$ExpectedPath = @(),

    [switch]$RequireIndexEmpty,

    [ValidateRange(1, 4096)]
    [int]$MaximumLedgerEvents = 512,

    [ValidateRange(1, 16)]
    [int]$MaximumAttempts = 3
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:CIWorkflowStates = @(
    'AGENT_DECIDED',
    'AUTOMATED_GATE_PASS',
    'AUTOMATED_GATE_FAIL',
    'LOCAL_COMPLETE',
    'EXTERNAL_PREREQUISITE',
    'BLOCKED_BY_HIGHER_AUTHORITY')
$script:CIEventTypes = @(
    'FINDING_DETECTED',
    'DUPLICATE_SUPPRESSED',
    'TRIAGED',
    'ATTEMPT_STARTED',
    'REVIEW_RECORDED',
    'GATE_FAILED',
    'GATE_PASSED',
    'QUARANTINED',
    'PROMOTED',
    'OBSERVATION_PASSED',
    'OBSERVATION_FAILED',
    'ROLLBACK_REQUIRED',
    'ROLLED_BACK',
    'CLOSED',
    'EXTERNAL_PREREQUISITE_RECORDED',
    'AUTHORITY_BLOCKED')
$script:CIAgentRoles = @(
    'SCOUT',
    'ROOT_CAUSE_ANALYST',
    'IMPLEMENTER',
    'VERIFIER',
    'REVIEWER',
    'INTEGRATOR',
    'OBSERVER')
$script:CIEventPropertyOrder = @(
    'schemaVersion',
    'sequence',
    'eventId',
    'recordedAtUtc',
    'improvementId',
    'eventType',
    'workflowState',
    'baseline',
    'executionKey',
    'executionScopeDigest',
    'acceptanceDigest',
    'candidateDigest',
    'candidateScopeDigest',
    'candidatePaths',
    'causalDeltaDigest',
    'causalFactReceiptDigests',
    'causalDeltaFacts',
    'actorId',
    'actorRole',
    'implementingAgents',
    'reviewingAgents',
    'reviewReceiptDigest',
    'riskClass',
    'governanceChange',
    'riskDomains',
    'reviewFindingCounts',
    'reviewFindings',
    'reviewDispositionDigests',
    'oldGateResult',
    'newGateResult',
    'candidateRevision',
    'candidateTree',
    'lastKnownGoodRevision',
    'lastKnownGoodTree',
    'promotionPreimageRevision',
    'promotionPreimageTree',
    'rollbackRevision',
    'rollbackTree',
    'promotionId',
    'observationWindowClosed',
    'result',
    'reasonCode',
    'evidenceDigests',
    'previousEventHash',
    'eventHash')
$script:CIPendingPropertyOrder = @(
    'schemaVersion',
    'ledgerPathDigest',
    'baseLength',
    'baseDigest',
    'eventByteLength',
    'eventDigest',
    'eventHash',
    'eventBytesBase64',
    'pendingHash')
$script:CIMaximumLedgerBytes = 64MB
$script:CIHighRiskDomains = @(
    'ARCHITECTURE',
    'COMPATIBILITY',
    'DATA_MIGRATION',
    'DATA_RIGHTS_LICENSING',
    'DESTRUCTIVE_OPERATION',
    'EXTERNAL_OPERATION',
    'GOVERNANCE_POLICY',
    'PERSISTENCE',
    'RELEASE_LIFECYCLE',
    'SECURITY')
$script:CIOfficialGovernancePaths = @(
    'agents.md',
    'docs/code-documentation-standards.md',
    'docs/design/db-notifier-design-system.md',
    'plans.md',
    'prompts/foundation/aiops-and-ai-module.md',
    'prompts/foundation/prompt-new-project.md',
    'prompts/foundation/solution-architecture-document.md',
    'prompts/governance/continuous-improvement.md',
    'prompts/governance/conversation-coordination-prompt.md',
    'prompts/governance/governance.md',
    'prompts/governance/language-policy.md',
    'prompts/governance/lifecycle.md',
    'prompts/governance/quality-gates.md',
    'prompts/governance/security-and-access.md',
    'prompts/operations/operational-playbooks.md',
    'prompts/start-here.md',
    'prompts/state/continuous-improvement-backlog.md',
    'prompts/state/current-state.md',
    'prompts/state/state-transition-log.md',
    'prompts/system/ai-software-engineering-master-prompt.md',
    'prompts/system/prompt-system-change-log.md',
    'prompts/templates/templates.md',
    'scripts/ci.ps1',
    'scripts/continuous-improvement.ps1',
    'scripts/development.ps1',
    'scripts/verify-development-flow.ps1',
    'tests/dbnotifier.continuousimprovement.tests.ps1',
    'tests/dbnotifier.developmentflow.tests.ps1')
$script:CIReviewDispositionDecisions = @(
    'ACCEPTED_RISK',
    'DEFERRED_TRACKED',
    'FALSE_POSITIVE')
$script:CIMetricProfile = 'db-notifier-continuous-improvement-metrics'
$script:CIMetricProfileVersion = '1.0.0'

function Get-CISha256Hex {
    <#
    .SYNOPSIS
    Returns a lowercase SHA-256 digest for one UTF-8 string.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Text
    )

    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($Text)
    return [Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-CISha256BytesHex {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [byte[]]$Bytes
    )

    return [Convert]::ToHexString(
        [System.Security.Cryptography.SHA256]::HashData($Bytes)).ToLowerInvariant()
}

function Assert-CINoDuplicateJsonProperties {
    param(
        [Parameter(Mandatory)][System.Text.Json.JsonElement]$Element,
        [Parameter(Mandatory)][string]$Context
    )

    switch ($Element.ValueKind) {
        ([System.Text.Json.JsonValueKind]::Object) {
            $names = [System.Collections.Generic.HashSet[string]]::new(
                [System.StringComparer]::Ordinal)
            foreach ($property in $Element.EnumerateObject()) {
                if (-not $names.Add($property.Name)) {
                    throw "DUPLICATE_JSON_PROPERTY: $Context contains duplicate property '$($property.Name)'."
                }
                Assert-CINoDuplicateJsonProperties `
                    -Element $property.Value `
                    -Context "$Context.$($property.Name)"
            }
        }
        ([System.Text.Json.JsonValueKind]::Array) {
            $index = 0
            foreach ($item in $Element.EnumerateArray()) {
                Assert-CINoDuplicateJsonProperties `
                    -Element $item `
                    -Context "$Context[$index]"
                $index++
            }
        }
    }
}

function Assert-CIStrictJsonText {
    [OutputType([System.Text.Json.JsonDocument])]
    param(
        [Parameter(Mandatory)][string]$Text,
        [Parameter(Mandatory)][string]$Context
    )

    $document = $null
    try {
        $document = [System.Text.Json.JsonDocument]::Parse($Text)
        Assert-CINoDuplicateJsonProperties `
            -Element $document.RootElement `
            -Context $Context
        return $document
    }
    catch {
        if ($null -ne $document) {
            $document.Dispose()
        }
        if ($_.Exception.Message -match 'DUPLICATE_JSON_PROPERTY') {
            throw
        }
        throw "$Context contains malformed JSON."
    }
}

function ConvertTo-CINormalisedText {
    <#
    .SYNOPSIS
    Normalises one identity-bearing text value without retaining presentation noise.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$Value
    )

    $normalised = $Value.Normalize([System.Text.NormalizationForm]::FormKC).Trim()
    $normalised = [regex]::Replace($normalised, '\s+', ' ')
    if ([string]::IsNullOrWhiteSpace($normalised)) {
        throw 'Continuous-improvement identity text must not be empty.'
    }
    return $normalised.ToLowerInvariant()
}

function ConvertTo-CINormalisedPaths {
    <#
    .SYNOPSIS
    Produces one sorted, case-normalised set of safe repository-relative paths.
    #>
    [OutputType([string[]])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Paths
    )

    $set = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            throw 'Continuous-improvement paths must not be empty.'
        }
        $candidate = $path.Replace('\', '/').Trim()
        if ([System.IO.Path]::IsPathFullyQualified($candidate) -or
            $candidate.StartsWith('/', [System.StringComparison]::Ordinal) -or
            $candidate -match '^[A-Za-z]:' -or
            @($candidate.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0) {
            throw "Continuous-improvement path '$path' is not a safe repository-relative path."
        }
        [void]$set.Add($candidate.ToLowerInvariant())
    }
    return @($set | Sort-Object -CaseSensitive)
}

function ConvertTo-CINormalisedAgents {
    <#
    .SYNOPSIS
    Produces one sorted set of canonical agent identities.
    #>
    [OutputType([string[]])]
    param(
        [AllowEmptyCollection()]
        [string[]]$Agents = @()
    )

    $set = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($agent in $Agents) {
        $identity = ConvertTo-CINormalisedText -Value $agent
        if ($identity -notmatch '^/?[a-z0-9][a-z0-9._/@:-]{0,127}$') {
            throw "Agent identity '$agent' is not canonical."
        }
        [void]$set.Add($identity)
    }
    return @($set | Sort-Object -CaseSensitive)
}

function New-CIStableFindingFingerprint {
    <#
    .SYNOPSIS
    Creates a baseline-independent fingerprint for one observed root cause.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$RuleId,

        [Parameter(Mandatory)]
        [string]$RootCause,

        [Parameter(Mandatory)]
        [string[]]$Paths
    )

    $rule = $RuleId.Trim().ToUpperInvariant()
    if ($rule -notmatch '^[A-Z0-9][A-Z0-9._-]{1,63}$') {
        throw 'RuleId must be a stable uppercase identifier.'
    }
    $normalisedPaths = @(ConvertTo-CINormalisedPaths -Paths $Paths)
    if ($normalisedPaths.Count -eq 0) {
        throw 'A finding fingerprint requires at least one affected path.'
    }
    $identity = @(
        'db-notifier-continuous-improvement-finding-v1',
        "rule=$rule",
        "rootCause=$(ConvertTo-CINormalisedText -Value $RootCause)",
        "paths=$($normalisedPaths -join ',')") -join "`n"
    return Get-CISha256Hex -Text ($identity + "`n")
}

function New-CIExecutionKey {
    <#
    .SYNOPSIS
    Binds one stable finding to its exact baseline, scope and acceptance contract.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ImprovementId,

        [Parameter(Mandatory)]
        [string]$Baseline,

        [Parameter(Mandatory)]
        [string]$ScopeDigest,

        [Parameter(Mandatory)]
        [string]$AcceptanceDigest
    )

    foreach ($digest in @($ImprovementId, $ScopeDigest, $AcceptanceDigest)) {
        if ($digest -notmatch '^[0-9a-f]{64}$') {
            throw 'Execution-key digest inputs must be lowercase SHA-256 values.'
        }
    }
    $normalisedBaseline = $Baseline.Trim().ToLowerInvariant()
    if ($normalisedBaseline -notmatch '^[0-9a-f]{40,64}$') {
        throw 'Execution-key baseline must be an exact lowercase revision digest.'
    }
    return Get-CISha256Hex -Text ((@(
                'db-notifier-continuous-improvement-execution-v1',
                "improvement=$ImprovementId",
                "baseline=$normalisedBaseline",
                "scope=$ScopeDigest",
                "acceptance=$AcceptanceDigest") -join "`n") + "`n")
}

function New-CIDispatchKey {
    <#
    .SYNOPSIS
    Creates an idempotency key for one exact tool-backed or local dispatch intent.
    #>
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ExecutionKey,

        [Parameter(Mandatory)]
        [ValidateSet(
            'CONTINUE_CURRENT',
            'DELEGATE_SUBAGENT',
            'RETURN_TO_EXISTING',
            'START_NEW_AUTO_DISPATCH')]
        [string]$Route,

        [Parameter(Mandatory)]
        [string]$Source,

        [Parameter(Mandatory)]
        [string]$Destination
    )

    if ($ExecutionKey -notmatch '^[0-9a-f]{64}$') {
        throw 'Dispatch execution key must be a lowercase SHA-256 value.'
    }
    return Get-CISha256Hex -Text ((@(
                'db-notifier-continuous-improvement-dispatch-v1',
                "execution=$ExecutionKey",
                "route=$Route",
                "source=$(ConvertTo-CINormalisedText -Value $Source)",
                "destination=$(ConvertTo-CINormalisedText -Value $Destination)") -join "`n") + "`n")
}

function Get-CIPropertyValue {
    [OutputType([object])]
    param(
        [Parameter(Mandatory)]
        [object]$Value,

        [Parameter(Mandatory)]
        [string]$Name,

        [object]$Default = $null
    )

    if ($Value -is [System.Collections.IDictionary]) {
        if ($Value.Contains($Name)) {
            return $Value[$Name]
        }
        return $Default
    }
    $property = $Value.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }
    return $property.Value
}

function ConvertTo-CIOptionalDigest {
    [OutputType([object])]
    param([object]$Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        return $null
    }
    $digest = ([string]$Value).Trim().ToLowerInvariant()
    if ($digest -notmatch '^[0-9a-f]{64}$') {
        throw 'Optional digest values must be lowercase SHA-256 values.'
    }
    return $digest
}

function ConvertTo-CIOptionalRevision {
    [OutputType([object])]
    param([object]$Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        return $null
    }
    $revision = ([string]$Value).Trim().ToLowerInvariant()
    if ($revision -notmatch '^[0-9a-f]{40}$') {
        throw 'Optional revisions must be exact lowercase 40-character Git object IDs.'
    }
    return $revision
}

function ConvertTo-CIOptionalGuid {
    [OutputType([object])]
    param([object]$Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string]$Value)) {
        return $null
    }
    $parsed = [guid]::Empty
    if (-not [guid]::TryParseExact(([string]$Value), 'D', [ref]$parsed)) {
        throw 'Optional identifiers must use the canonical GUID D format.'
    }
    return $parsed.ToString('D')
}

function ConvertTo-CIDigestSet {
    [OutputType([string[]])]
    param(
        [AllowEmptyCollection()]
        [object[]]$Values = @(),

        [string]$Label = 'Digest'
    )

    return @(@($Values) | ForEach-Object {
            $digest = ([string]$_).Trim().ToLowerInvariant()
            if ($digest -notmatch '^[0-9a-f]{64}$') {
                throw "$Label values must be lowercase SHA-256 digests."
            }
            $digest
        } | Sort-Object -Unique -CaseSensitive)
}

function New-CICandidateClassification {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string[]]$Paths,

        [AllowEmptyCollection()]
        [string[]]$AdditionalRiskDomains = @()
    )

    $normalisedPaths = @(ConvertTo-CINormalisedPaths -Paths $Paths)
    if ($normalisedPaths.Count -eq 0) {
        throw 'CANDIDATE_CLASSIFICATION_REQUIRED: an attempt requires exact candidate paths.'
    }
    $riskDomainSet = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($path in $normalisedPaths) {
        if ($script:CIOfficialGovernancePaths -ccontains $path -or
            $path.StartsWith('prompts/governance/', [System.StringComparison]::Ordinal)) {
            [void]$riskDomainSet.Add('GOVERNANCE_POLICY')
        }
        if ($path -match '(^|/)security(/|$)' -or
            $path -match '(^|/)(auth|authentication|authorisation|authorization|rbac)(/|[._-])' -or
            $path -match '(^|/)(secret|credential|certificate|cryptography)(s)?(/|[._-])') {
            [void]$riskDomainSet.Add('SECURITY')
        }
        if ($path -match '(^|/)(persistence|storage|repositories)(/|$)' -or
            $path -match '[.](sql|db|sqlite)$') {
            [void]$riskDomainSet.Add('PERSISTENCE')
        }
        if ($path -match '(^|/)(migrations?|schema)(/|$)' -or
            $path -match '(^|/)[0-9]{8,}[_-].*[.](cs|sql)$') {
            [void]$riskDomainSet.Add('DATA_MIGRATION')
        }
        if ($path -match '(^|/)(architecture|adrs?|contracts?|provider-sdk)(/|$)' -or
            $path -match '(^|/)dbnotifier[.]architecture[.]tests') {
            [void]$riskDomainSet.Add('ARCHITECTURE')
        }
        if ($path -match '(^|/)(compatibility|legacy)(/|$)' -or
            $path -match '(^|/)(global[.]json|directory[.](build|packages)[.]props|package-lock[.]json)$') {
            [void]$riskDomainSet.Add('COMPATIBILITY')
        }
        if ($path -match '(^|/)(deploy|deployment|infrastructure|terraform|helm|docker)(/|$)' -or
            $path.StartsWith('.github/workflows/', [System.StringComparison]::Ordinal)) {
            [void]$riskDomainSet.Add('EXTERNAL_OPERATION')
        }
        if ($path -match '(^|/)(release|packaging|installer|installers)(/|$)' -or
            $path.StartsWith('.github/workflows/', [System.StringComparison]::Ordinal)) {
            [void]$riskDomainSet.Add('RELEASE_LIFECYCLE')
        }
        if ($path -match '(^|/)(delete|purge|cleanup|uninstall|rollback)([^/]*)(/|[.])') {
            [void]$riskDomainSet.Add('DESTRUCTIVE_OPERATION')
        }
        if ($path -match '(^|/)(licen[cs]e|notice|third[-_]?party|provenance)([^/]*)(/|[.])') {
            [void]$riskDomainSet.Add('DATA_RIGHTS_LICENSING')
        }
    }
    foreach ($additionalDomain in $AdditionalRiskDomains) {
        $domain = ([string]$additionalDomain).Trim().ToUpperInvariant()
        if ($domain -cnotin $script:CIHighRiskDomains) {
            throw "Candidate additional-risk domain '$domain' is not canonical."
        }
        [void]$riskDomainSet.Add($domain)
    }
    $riskDomains = @($riskDomainSet | Sort-Object -CaseSensitive)
    $governedPath = $riskDomains -ccontains 'GOVERNANCE_POLICY'
    $scopePayload = @(
        'db-notifier-continuous-improvement-scope-v1',
        "paths=$($normalisedPaths -join ',')",
        "riskDomains=$($riskDomains -join ',')") -join "`n"
    return [pscustomobject][ordered]@{
        candidateScopeDigest = Get-CISha256Hex -Text ($scopePayload + "`n")
        governanceChange = $governedPath
        riskClass = if ($riskDomains.Count -gt 0) { 'HIGH' } else { 'STANDARD' }
        riskDomains = $riskDomains
    }
}

function New-CICausalDeltaReceipt {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [object]$Facts,

        [Parameter(Mandatory)][string]$CandidateDigest,
        [Parameter(Mandatory)][object[]]$PriorEvents,
        [Parameter(Mandatory)][string]$ImprovementId,
        [Parameter(Mandatory)][string]$ExecutionKey
    )

    $names = if ($Facts -is [System.Collections.IDictionary]) {
        @($Facts.Keys)
    }
    else {
        @($Facts.PSObject.Properties.Name)
    }
    $expectedNames = @(
        'predecessorGateEventHash',
        'changedFactDomain',
        'changedFactDigest',
        'decision')
    if (Compare-Object -ReferenceObject $expectedNames -DifferenceObject $names -SyncWindow 0) {
        throw 'CAUSAL_DELTA_REQUIRED: causal facts have an unexpected shape.'
    }
    $improvementEvents = @($PriorEvents | Where-Object {
            $_.improvementId -ceq $ImprovementId -and
            $_.eventType -cne 'DUPLICATE_SUPPRESSED'
        })
    if ($improvementEvents.Count -eq 0 -or
        $improvementEvents[-1].eventType -cne 'GATE_FAILED') {
        throw 'CAUSAL_DELTA_REQUIRED: the exact predecessor must be GATE_FAILED.'
    }
    $failedGate = $improvementEvents[-1]
    $gateHash = ([string](Get-CIPropertyValue `
            -Value $Facts `
            -Name 'predecessorGateEventHash')).Trim().ToLowerInvariant()
    if ($gateHash -cne $failedGate.eventHash -or
        $failedGate.executionKey -cne $ExecutionKey) {
        throw 'CAUSAL_DELTA_REQUIRED: the failure receipt does not bind the exact attempt.'
    }
    $attempt = @($improvementEvents | Where-Object {
            $_.eventType -ceq 'ATTEMPT_STARTED'
        } | Select-Object -Last 1)
    if ($attempt.Count -ne 1 -or
        $attempt[0].executionKey -cne $ExecutionKey -or
        $attempt[0].candidateDigest -cne $failedGate.candidateDigest -or
        $attempt[0].candidateDigest -ceq $CandidateDigest) {
        throw 'CAUSAL_DELTA_REQUIRED: the successor does not change the failed candidate.'
    }
    $domain = ([string](Get-CIPropertyValue `
            -Value $Facts `
            -Name 'changedFactDomain')).Trim().ToUpperInvariant()
    if ($domain -cnotin @('CANDIDATE', 'DEPENDENCY', 'ENVIRONMENT', 'CONTROL_PLANE')) {
        throw 'CAUSAL_DELTA_REQUIRED: the changed fact domain is not canonical.'
    }
    $changedFactDigest = ConvertTo-CIOptionalDigest -Value (
        Get-CIPropertyValue -Value $Facts -Name 'changedFactDigest')
    if ($null -eq $changedFactDigest -or
        $domain -cne 'CANDIDATE' -or
        $changedFactDigest -cne $CandidateDigest) {
        throw 'CAUSAL_DELTA_REQUIRED: the changed fact lacks a domain-bound ledger receipt.'
    }
    $decision = ([string](Get-CIPropertyValue `
            -Value $Facts `
            -Name 'decision')).Trim().ToUpperInvariant()
    if ($decision -cne 'RETRY_WITH_CAUSAL_CHANGE') {
        throw 'CAUSAL_DELTA_REQUIRED: the causal decision is not canonical.'
    }
    $canonicalFacts = [ordered]@{
        predecessorGateEventHash = $failedGate.eventHash
        changedFactDomain = $domain
        changedFactDigest = $changedFactDigest
        decision = $decision
    }
    $payload = @(
        'db-notifier-continuous-improvement-causal-delta-v1',
        "improvement=$ImprovementId",
        "execution=$ExecutionKey",
        "predecessorAttempt=$($attempt[0].eventHash)",
        "failedGate=$($failedGate.eventHash)",
        "failedCandidate=$($attempt[0].candidateDigest)",
        "successorCandidate=$CandidateDigest",
        "domain=$domain",
        "changedFact=$changedFactDigest",
        "decision=$decision") -join "`n"
    return [pscustomobject][ordered]@{
        digest = Get-CISha256Hex -Text ($payload + "`n")
        receiptDigests = @($failedGate.eventHash)
        facts = [pscustomobject]$canonicalFacts
    }
}

function New-CIReviewDispositionDigest {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)][string]$ExecutionKey,
        [Parameter(Mandatory)][string]$CandidateDigest,
        [Parameter(Mandatory)][string]$ReviewerId,
        [Parameter(Mandatory)][string]$Severity,
        [Parameter(Mandatory)][string]$FindingDigest,
        [Parameter(Mandatory)][string]$Decision,
        [Parameter(Mandatory)][string]$EvidenceDigest
    )

    return Get-CISha256Hex -Text ((@(
                'db-notifier-continuous-improvement-review-disposition-v1',
                "execution=$ExecutionKey",
                "candidate=$CandidateDigest",
                "reviewer=$(ConvertTo-CINormalisedText -Value $ReviewerId)",
                "severity=$Severity",
                "finding=$FindingDigest",
                "decision=$Decision",
                "evidence=$EvidenceDigest") -join "`n") + "`n")
}

function New-CIReviewAssessment {
    [OutputType([pscustomobject])]
    param(
        [AllowEmptyCollection()][object[]]$Findings = @(),
        [Parameter(Mandatory)][string]$ExecutionKey,
        [Parameter(Mandatory)][string]$CandidateDigest,
        [Parameter(Mandatory)][string]$ReviewerId
    )

    $counts = [ordered]@{ P0 = 0; P1 = 0; P2 = 0; P3 = 0 }
    $normalised = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($finding in @($Findings)) {
        $names = if ($finding -is [System.Collections.IDictionary]) {
            @($finding.Keys)
        }
        else {
            @($finding.PSObject.Properties.Name)
        }
        $expected = @(
            'severity',
            'findingDigest',
            'dispositionDecision',
            'dispositionEvidenceDigest')
        if (Compare-Object -ReferenceObject $expected -DifferenceObject $names -SyncWindow 0) {
            throw 'REVIEW_FINDING_SCHEMA_INVALID: review finding shape differs.'
        }
        $severity = ([string](Get-CIPropertyValue -Value $finding -Name 'severity')).Trim().ToUpperInvariant()
        if ($severity -cnotin @('P0', 'P1', 'P2', 'P3')) {
            throw 'REVIEW_FINDING_SCHEMA_INVALID: severity is not P0-P3.'
        }
        $findingDigest = ConvertTo-CIOptionalDigest -Value (
            Get-CIPropertyValue -Value $finding -Name 'findingDigest')
        if ($null -eq $findingDigest -or -not $seen.Add("$severity`:$findingDigest")) {
            throw 'REVIEW_FINDING_SCHEMA_INVALID: finding digest is missing or duplicated.'
        }
        $decision = [string](Get-CIPropertyValue -Value $finding -Name 'dispositionDecision')
        $dispositionEvidence = ConvertTo-CIOptionalDigest -Value (
            Get-CIPropertyValue -Value $finding -Name 'dispositionEvidenceDigest')
        $dispositionDigest = $null
        if ($severity -in @('P2', 'P3')) {
            $decision = $decision.Trim().ToUpperInvariant()
            if ($decision -cnotin $script:CIReviewDispositionDecisions -or
                $null -eq $dispositionEvidence) {
                throw 'REVIEW_DISPOSITION_REQUIRED: every P2 or P3 needs a decision and evidence.'
            }
            $dispositionDigest = New-CIReviewDispositionDigest `
                -ExecutionKey $ExecutionKey `
                -CandidateDigest $CandidateDigest `
                -ReviewerId $ReviewerId `
                -Severity $severity `
                -FindingDigest $findingDigest `
                -Decision $decision `
                -EvidenceDigest $dispositionEvidence
        }
        elseif (-not [string]::IsNullOrWhiteSpace($decision) -or
            $null -ne $dispositionEvidence) {
            throw 'REVIEW_DISPOSITION_NOT_APPLICABLE: P0 and P1 remain blocking findings.'
        }
        else {
            $decision = $null
        }
        $counts[$severity]++
        $normalised.Add([pscustomobject][ordered]@{
                severity = $severity
                reviewerId = (ConvertTo-CINormalisedText -Value $ReviewerId)
                findingDigest = $findingDigest
                dispositionDecision = $decision
                dispositionEvidenceDigest = $dispositionEvidence
                dispositionDigest = $dispositionDigest
            })
    }
    $orderedFindings = @($normalised | Sort-Object `
            -Property @{ Expression = {
                    [Array]::IndexOf(@('P0', 'P1', 'P2', 'P3'), $_.severity)
                } }, findingDigest)
    return [pscustomobject][ordered]@{
        counts = [pscustomobject]$counts
        findings = $orderedFindings
        dispositionDigests = @(ConvertTo-CIDigestSet `
                -Values @($orderedFindings | ForEach-Object { $_.dispositionDigest } |
                    Where-Object { $null -ne $_ }) `
                -Label 'Review disposition')
    }
}

function New-CIReviewReceiptDigest {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)][string]$ReviewerId,
        [Parameter(Mandatory)][string]$ExecutionKey,
        [Parameter(Mandatory)][string]$CandidateDigest,
        [Parameter(Mandatory)][string]$CandidateScopeDigest,
        [Parameter(Mandatory)][ValidateSet('PASS', 'FAIL')][string]$Result,
        [Parameter(Mandatory)][string[]]$EvidenceDigests,
        [Parameter(Mandatory)][object]$ReviewFindingCounts,
        [AllowEmptyCollection()][object[]]$ReviewFindings = @(),
        [AllowEmptyCollection()][string[]]$ReviewDispositionDigests = @()
    )

    $evidence = @(ConvertTo-CIDigestSet -Values $EvidenceDigests -Label 'Review evidence')
    if ($evidence.Count -eq 0) {
        throw 'REVIEW_RECEIPT_REQUIRED: a review requires SHA evidence.'
    }
    return Get-CISha256Hex -Text ((@(
                'db-notifier-continuous-improvement-review-v1',
                "reviewer=$(ConvertTo-CINormalisedText -Value $ReviewerId)",
                "execution=$ExecutionKey",
                "candidate=$CandidateDigest",
                "scope=$CandidateScopeDigest",
                "result=$Result",
                "findingCounts=$($ReviewFindingCounts | ConvertTo-Json -Compress)",
                "findings=$(@($ReviewFindings) | ConvertTo-Json -Depth 8 -Compress)",
                "dispositions=$(@($ReviewDispositionDigests) -join ',')",
                "evidence=$($evidence -join ',')") -join "`n") + "`n")
}

function Resolve-CIGitCommitReceipt {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Revision
    )

    $root = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $RepositoryRoot).Path)
    Assert-CINoReparsePoint -Path $root
    $gitPath = (Get-Command git -CommandType Application -ErrorAction Stop |
            Select-Object -First 1).Source
    $reportedRoot = ((& $gitPath -C $root rev-parse --show-toplevel 2>$null) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or
        -not [System.IO.Path]::GetFullPath($reportedRoot).Equals(
            $root,
            $(if ($IsWindows) {
                    [System.StringComparison]::OrdinalIgnoreCase
                }
                else {
                    [System.StringComparison]::Ordinal
                }))) {
        throw 'ISOLATION_FAILURE: Git receipt did not resolve the exact repository root.'
    }
    $commit = ((& $gitPath -C $root rev-parse --verify "$Revision`^{commit}" 2>$null) -join '').Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') {
        throw "OPAQUE_REVISION_REJECTED: '$Revision' does not resolve to a Git commit."
    }
    $tree = ((& $gitPath -C $root rev-parse --verify "$commit`^{tree}" 2>$null) -join '').Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $tree -notmatch '^[0-9a-f]{40}$') {
        throw "OPAQUE_REVISION_REJECTED: '$Revision' does not resolve to a Git tree."
    }
    return [pscustomobject][ordered]@{
        repositoryRoot = $root
        commit = $commit
        tree = $tree
    }
}

function Get-CIEventHashPayload {
    [OutputType([System.Collections.Specialized.OrderedDictionary])]
    param(
        [Parameter(Mandatory)]
        [object]$Event
    )

    $payload = [ordered]@{}
    foreach ($name in $script:CIEventPropertyOrder | Where-Object { $_ -cne 'eventHash' }) {
        $payload[$name] = Get-CIPropertyValue -Value $Event -Name $name
    }
    return $payload
}

function New-CILedgerEvent {
    <#
    .SYNOPSIS
    Builds one canonical hash-chained event from caller-supplied semantic data.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [object]$EventData,

        [Parameter(Mandatory)]
        [long]$Sequence,

        [Parameter(Mandatory)]
        [string]$PreviousEventHash,

        [AllowEmptyCollection()]
        [object[]]$PriorEvents = @()
    )

    $recordedAtValue = Get-CIPropertyValue -Value $EventData -Name 'recordedAtUtc'
    $recordedAt = if ($null -eq $recordedAtValue) {
        [DateTimeOffset]::UtcNow
    }
    else {
        $parsed = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse(
                [string]$recordedAtValue,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::AssumeUniversal,
                [ref]$parsed)) {
            throw 'recordedAtUtc is not a valid UTC timestamp.'
        }
        $parsed.ToUniversalTime()
    }
    $eventIdValue = Get-CIPropertyValue -Value $EventData -Name 'eventId'
    $eventId = if ($null -eq $eventIdValue) {
        [guid]::NewGuid().ToString('D')
    }
    else {
        ConvertTo-CIOptionalGuid -Value $eventIdValue
    }
    $baseline = ([string](Get-CIPropertyValue -Value $EventData -Name 'baseline')).Trim().ToLowerInvariant()
    if ($baseline -notmatch '^[0-9a-f]{40}$') {
        throw 'Ledger events require an exact lowercase 40-character baseline.'
    }
    $improvementId = ([string](Get-CIPropertyValue -Value $EventData -Name 'improvementId')).Trim().ToLowerInvariant()
    if ($improvementId -notmatch '^[0-9a-f]{64}$') {
        throw 'Ledger events require a stable lowercase improvement fingerprint.'
    }
    $actorId = @(ConvertTo-CINormalisedAgents -Agents @(
            [string](Get-CIPropertyValue -Value $EventData -Name 'actorId')))
    if ($actorId.Count -ne 1) {
        throw 'Ledger events require exactly one canonical actor identity.'
    }
    $eventType = [string](Get-CIPropertyValue -Value $EventData -Name 'eventType')
    $actorRole = [string](Get-CIPropertyValue -Value $EventData -Name 'actorRole')
    $result = [string](Get-CIPropertyValue -Value $EventData -Name 'result')
    $evidenceDigests = @(ConvertTo-CIDigestSet `
            -Values @(Get-CIPropertyValue -Value $EventData -Name 'evidenceDigests' -Default @()) `
            -Label 'Evidence reference')
    $improvementEvents = @($PriorEvents | Where-Object {
            $_.improvementId -ceq $improvementId -and
            $_.eventType -cne 'DUPLICATE_SUPPRESSED'
        })
    $previous = if ($improvementEvents.Count -eq 0) { $null } else { $improvementEvents[-1] }

    foreach ($callerOwnedClassification in @(
            'candidateScopeDigest',
            'governanceChange',
            'riskDomains',
            'riskClass')) {
        $callerValue = Get-CIPropertyValue `
            -Value $EventData `
            -Name $callerOwnedClassification
        if ($null -ne $callerValue -and
            -not [string]::IsNullOrWhiteSpace([string]$callerValue)) {
            throw 'CALLER_SUPPLIED_CLASSIFICATION: candidate risk is derived from governed scope and contract facts.'
        }
    }
    $legacyRiskFacts = Get-CIPropertyValue -Value $EventData -Name 'contractRiskFacts'
    if ($null -ne $legacyRiskFacts) {
        throw 'CALLER_SUPPLIED_CLASSIFICATION: contractRiskFacts is not an authority surface.'
    }
    $callerCausalDigest = Get-CIPropertyValue -Value $EventData -Name 'causalDeltaDigest'
    if ($null -ne $callerCausalDigest -and
        -not [string]::IsNullOrWhiteSpace([string]$callerCausalDigest)) {
        throw 'CALLER_SUPPLIED_CAUSAL_DELTA: causal identity must be derived from structured facts and receipts.'
    }
    $callerReviewReceipt = Get-CIPropertyValue -Value $EventData -Name 'reviewReceiptDigest'
    if ($null -ne $callerReviewReceipt -and
        -not [string]::IsNullOrWhiteSpace([string]$callerReviewReceipt)) {
        throw 'CALLER_SUPPLIED_REVIEW_RECEIPT: reviewer receipts are controller-derived.'
    }
    foreach ($callerOwnedReviewFact in @(
            'reviewFindingCounts',
            'reviewDispositionDigests')) {
        if ($null -ne (Get-CIPropertyValue -Value $EventData -Name $callerOwnedReviewFact)) {
            throw 'CALLER_SUPPLIED_REVIEW_SUMMARY: review summaries are controller-derived.'
        }
    }

    $executionKey = ConvertTo-CIOptionalDigest -Value (
        Get-CIPropertyValue -Value $EventData -Name 'executionKey')
    $executionScopeValue = Get-CIPropertyValue `
        -Value $EventData `
        -Name 'executionScopeDigest'
    $acceptanceValue = Get-CIPropertyValue `
        -Value $EventData `
        -Name 'acceptanceDigest'
    $executionScopeDigest = if ($null -ne $executionScopeValue) {
        ConvertTo-CIOptionalDigest -Value $executionScopeValue
    }
    elseif ($null -ne $previous) {
        $previous.executionScopeDigest
    }
    else {
        $null
    }
    $acceptanceDigest = if ($null -ne $acceptanceValue) {
        ConvertTo-CIOptionalDigest -Value $acceptanceValue
    }
    elseif ($null -ne $previous) {
        $previous.acceptanceDigest
    }
    else {
        $null
    }
    if ($null -eq $executionKey) {
        if ($null -ne $executionScopeDigest -or $null -ne $acceptanceDigest) {
            throw 'EXECUTION_KEY_IDENTITY_INVALID: execution inputs cannot exist without an execution key.'
        }
    }
    elseif ($null -eq $executionScopeDigest -or $null -eq $acceptanceDigest) {
        throw 'EXECUTION_KEY_IDENTITY_INVALID: execution key replay requires scope and acceptance digests.'
    }
    else {
        $expectedExecutionKey = New-CIExecutionKey `
            -ImprovementId $improvementId `
            -Baseline $baseline `
            -ScopeDigest $executionScopeDigest `
            -AcceptanceDigest $acceptanceDigest
        if ($executionKey -cne $expectedExecutionKey) {
            throw 'EXECUTION_KEY_IDENTITY_INVALID: execution key does not replay from its declared identity.'
        }
    }

    $candidatePaths = if ($eventType -ceq 'ATTEMPT_STARTED') {
        @(ConvertTo-CINormalisedPaths -Paths @(
                Get-CIPropertyValue -Value $EventData -Name 'candidatePaths' -Default @()))
    }
    elseif ($null -ne $previous) {
        @($previous.candidatePaths)
    }
    else {
        @()
    }
    $classification = if ($eventType -ceq 'ATTEMPT_STARTED') {
        New-CICandidateClassification `
            -Paths $candidatePaths `
            -AdditionalRiskDomains @(Get-CIPropertyValue -Value $EventData -Name 'additionalRiskDomains' -Default @())
    }
    elseif ($null -ne $previous) {
        [pscustomobject]@{
            candidateScopeDigest = $previous.candidateScopeDigest
            governanceChange = $previous.governanceChange
            riskClass = $previous.riskClass
            riskDomains = @($previous.riskDomains)
        }
    }
    else {
        [pscustomobject]@{
            candidateScopeDigest = $null
            governanceChange = $false
            riskClass = 'STANDARD'
            riskDomains = @()
        }
    }

    $candidateDigest = ConvertTo-CIOptionalDigest -Value (
        Get-CIPropertyValue -Value $EventData -Name 'candidateDigest')
    $causalDeltaDigest = $null
    $causalFactReceiptDigests = @()
    $canonicalCausalFacts = $null
    $causalFacts = Get-CIPropertyValue -Value $EventData -Name 'causalDeltaFacts'
    if ($eventType -ceq 'ATTEMPT_STARTED' -and $null -ne $causalFacts) {
        $causalReceipt = New-CICausalDeltaReceipt `
            -Facts $causalFacts `
            -CandidateDigest $candidateDigest `
            -PriorEvents $PriorEvents `
            -ImprovementId $improvementId `
            -ExecutionKey $executionKey
        $causalDeltaDigest = $causalReceipt.digest
        $causalFactReceiptDigests = @($causalReceipt.receiptDigests)
        $canonicalCausalFacts = $causalReceipt.facts
        $evidenceDigests = @(ConvertTo-CIDigestSet `
                -Values @($evidenceDigests + $causalFactReceiptDigests) `
                -Label 'Evidence reference')
    }

    $implementingAgents = if ($eventType -ceq 'ATTEMPT_STARTED') {
        @(ConvertTo-CINormalisedAgents -Agents @(
                Get-CIPropertyValue -Value $EventData -Name 'implementingAgents' -Default @()))
    }
    elseif ($null -ne $previous) {
        @($previous.implementingAgents)
    }
    else {
        @()
    }
    $callerReviewers = @(Get-CIPropertyValue `
            -Value $EventData `
            -Name 'reviewingAgents' `
            -Default @())
    if ($eventType -in @('REVIEW_RECORDED', 'GATE_FAILED', 'GATE_PASSED') -and
        $callerReviewers.Count -gt 0) {
        throw 'PHANTOM_REVIEWER_REJECTED: reviewer lists cannot substitute for review events.'
    }
    $attempt = @($improvementEvents | Where-Object {
            $_.eventType -ceq 'ATTEMPT_STARTED'
        } | Select-Object -Last 1)
    $reviewEvents = if ($attempt.Count -eq 1) {
        @($improvementEvents | Where-Object {
                $_.eventType -ceq 'REVIEW_RECORDED' -and
                $_.sequence -gt $attempt[0].sequence
            })
    }
    else {
        @()
    }
    $reviewingAgents = if ($eventType -ceq 'REVIEW_RECORDED') {
        @($actorId[0])
    }
    elseif ($eventType -in @('GATE_FAILED', 'GATE_PASSED')) {
        @(ConvertTo-CINormalisedAgents -Agents @(
                $reviewEvents | ForEach-Object { $_.actorId }))
    }
    elseif ($null -ne $previous) {
        @($previous.reviewingAgents)
    }
    else {
        @()
    }

    $reviewReceiptDigest = $null
    $reviewFindingCounts = [pscustomobject][ordered]@{ P0 = 0; P1 = 0; P2 = 0; P3 = 0 }
    $reviewFindings = @()
    $reviewDispositionDigests = @()
    if ($eventType -ceq 'REVIEW_RECORDED') {
        $assessment = New-CIReviewAssessment `
            -Findings @(Get-CIPropertyValue -Value $EventData -Name 'reviewFindings' -Default @()) `
            -ExecutionKey $executionKey `
            -CandidateDigest $candidateDigest `
            -ReviewerId $actorId[0]
        $reviewFindingCounts = $assessment.counts
        $reviewFindings = @($assessment.findings)
        $reviewDispositionDigests = @($assessment.dispositionDigests)
        if ($result -ceq 'PASS' -and
            ($reviewFindingCounts.P0 -ne 0 -or $reviewFindingCounts.P1 -ne 0)) {
            throw 'BLOCKING_REVIEW_FINDING: PASS cannot carry P0 or P1 findings.'
        }
        $reviewReceiptDigest = New-CIReviewReceiptDigest `
            -ReviewerId $actorId[0] `
            -ExecutionKey $executionKey `
            -CandidateDigest $candidateDigest `
            -CandidateScopeDigest $classification.candidateScopeDigest `
            -Result $result `
            -EvidenceDigests $evidenceDigests `
            -ReviewFindingCounts $reviewFindingCounts `
            -ReviewFindings $reviewFindings `
            -ReviewDispositionDigests $reviewDispositionDigests
    }
    if ($eventType -in @('GATE_FAILED', 'GATE_PASSED')) {
        $reviewReceipts = @($reviewEvents | ForEach-Object {
                $_.reviewReceiptDigest
            } | Where-Object { $null -ne $_ })
        $evidenceDigests = @(ConvertTo-CIDigestSet `
                -Values @($evidenceDigests + $reviewReceipts) `
                -Label 'Gate evidence')
        $aggregateCounts = [ordered]@{ P0 = 0; P1 = 0; P2 = 0; P3 = 0 }
        foreach ($reviewEvent in $reviewEvents) {
            foreach ($severity in @('P0', 'P1', 'P2', 'P3')) {
                $aggregateCounts[$severity] += [int]$reviewEvent.reviewFindingCounts.$severity
            }
        }
        $reviewFindingCounts = [pscustomobject]$aggregateCounts
        $reviewFindings = @($reviewEvents | ForEach-Object { @($_.reviewFindings) } |
                Sort-Object -Property severity, reviewerId, findingDigest)
        $reviewDispositionDigests = @(ConvertTo-CIDigestSet `
                -Values @($reviewEvents | ForEach-Object {
                        @($_.reviewDispositionDigests)
                    }) `
                -Label 'Review disposition')
        if ($eventType -ceq 'GATE_PASSED' -and
            ($reviewFindingCounts.P0 -ne 0 -or $reviewFindingCounts.P1 -ne 0)) {
            throw 'BLOCKING_REVIEW_FINDING: promotion gate requires zero P0 and P1 findings.'
        }
    }
    elseif ($eventType -in @(
            'PROMOTED',
            'OBSERVATION_PASSED',
            'OBSERVATION_FAILED',
            'ROLLBACK_REQUIRED',
            'ROLLED_BACK',
            'CLOSED') -and $null -ne $previous) {
        $reviewFindingCounts = $previous.reviewFindingCounts
        $reviewFindings = @($previous.reviewFindings)
        $reviewDispositionDigests = @($previous.reviewDispositionDigests)
    }

    foreach ($callerOwnedGitFact in @(
            'candidateTree',
            'lastKnownGoodTree',
            'promotionPreimageTree',
            'rollbackTree')) {
        if ($null -ne (Get-CIPropertyValue -Value $EventData -Name $callerOwnedGitFact)) {
            throw 'CALLER_SUPPLIED_GIT_FACT: commit trees are resolved by Git.'
        }
    }
    $candidateRevision = if ($null -ne $previous) { $previous.candidateRevision } else { $null }
    $candidateTree = if ($null -ne $previous) { $previous.candidateTree } else { $null }
    $lastKnownGoodRevision = if ($null -ne $previous) { $previous.lastKnownGoodRevision } else { $null }
    $lastKnownGoodTree = if ($null -ne $previous) { $previous.lastKnownGoodTree } else { $null }
    $promotionPreimageRevision = if ($null -ne $previous) { $previous.promotionPreimageRevision } else { $null }
    $promotionPreimageTree = if ($null -ne $previous) { $previous.promotionPreimageTree } else { $null }
    $rollbackRevision = if ($null -ne $previous) { $previous.rollbackRevision } else { $null }
    $rollbackTree = if ($null -ne $previous) { $previous.rollbackTree } else { $null }
    if ($eventType -ceq 'PROMOTED') {
        $repositoryRoot = [string](Get-CIPropertyValue -Value $EventData -Name 'repositoryRoot')
        $candidateReceipt = Resolve-CIGitCommitReceipt `
            -RepositoryRoot $repositoryRoot `
            -Revision ([string](Get-CIPropertyValue -Value $EventData -Name 'candidateRevision'))
        $lkgReceipt = Resolve-CIGitCommitReceipt `
            -RepositoryRoot $repositoryRoot `
            -Revision ([string](Get-CIPropertyValue -Value $EventData -Name 'lastKnownGoodRevision'))
        $preimageReceipt = Resolve-CIGitCommitReceipt `
            -RepositoryRoot $repositoryRoot `
            -Revision ([string](Get-CIPropertyValue -Value $EventData -Name 'promotionPreimageRevision'))
        $candidateRevision = $candidateReceipt.commit
        $candidateTree = $candidateReceipt.tree
        $lastKnownGoodRevision = $lkgReceipt.commit
        $lastKnownGoodTree = $lkgReceipt.tree
        $promotionPreimageRevision = $preimageReceipt.commit
        $promotionPreimageTree = $preimageReceipt.tree
    }
    if ($eventType -ceq 'ROLLED_BACK') {
        $rollbackReceipt = Resolve-CIGitCommitReceipt `
            -RepositoryRoot ([string](Get-CIPropertyValue -Value $EventData -Name 'repositoryRoot')) `
            -Revision ([string](Get-CIPropertyValue -Value $EventData -Name 'rollbackRevision'))
        $rollbackRevision = $rollbackReceipt.commit
        $rollbackTree = $rollbackReceipt.tree
    }

    $promotionId = if ($eventType -ceq 'PROMOTED') {
        ConvertTo-CIOptionalGuid -Value (
            Get-CIPropertyValue -Value $EventData -Name 'promotionId')
    }
    elseif ($null -ne $previous) {
        $previous.promotionId
    }
    else {
        $null
    }
    $event = [ordered]@{
        schemaVersion = 1
        sequence = $Sequence
        eventId = $eventId
        recordedAtUtc = $recordedAt.ToString(
            'yyyy-MM-ddTHH:mm:ss.fffffffZ',
            [Globalization.CultureInfo]::InvariantCulture)
        improvementId = $improvementId
        eventType = $eventType
        workflowState = [string](Get-CIPropertyValue -Value $EventData -Name 'workflowState')
        baseline = $baseline
        executionKey = $executionKey
        executionScopeDigest = $executionScopeDigest
        acceptanceDigest = $acceptanceDigest
        candidateDigest = $candidateDigest
        candidateScopeDigest = $classification.candidateScopeDigest
        candidatePaths = @($candidatePaths)
        causalDeltaDigest = $causalDeltaDigest
        causalFactReceiptDigests = @($causalFactReceiptDigests)
        causalDeltaFacts = $canonicalCausalFacts
        actorId = $actorId[0]
        actorRole = $actorRole
        implementingAgents = @($implementingAgents)
        reviewingAgents = @($reviewingAgents)
        reviewReceiptDigest = $reviewReceiptDigest
        riskClass = $classification.riskClass
        governanceChange = $classification.governanceChange
        riskDomains = @($classification.riskDomains)
        reviewFindingCounts = $reviewFindingCounts
        reviewFindings = @($reviewFindings)
        reviewDispositionDigests = @($reviewDispositionDigests)
        oldGateResult = [string](Get-CIPropertyValue -Value $EventData -Name 'oldGateResult' -Default 'NOT_APPLICABLE')
        newGateResult = [string](Get-CIPropertyValue -Value $EventData -Name 'newGateResult' -Default 'NOT_APPLICABLE')
        candidateRevision = $candidateRevision
        candidateTree = $candidateTree
        lastKnownGoodRevision = $lastKnownGoodRevision
        lastKnownGoodTree = $lastKnownGoodTree
        promotionPreimageRevision = $promotionPreimageRevision
        promotionPreimageTree = $promotionPreimageTree
        rollbackRevision = $rollbackRevision
        rollbackTree = $rollbackTree
        promotionId = $promotionId
        observationWindowClosed = [bool](Get-CIPropertyValue -Value $EventData -Name 'observationWindowClosed' -Default $false)
        result = $result
        reasonCode = [string](Get-CIPropertyValue -Value $EventData -Name 'reasonCode')
        evidenceDigests = $evidenceDigests
        previousEventHash = $PreviousEventHash
        eventHash = $null
    }
    $payloadJson = (Get-CIEventHashPayload -Event $event) |
        ConvertTo-Json -Depth 12 -Compress
    $event.eventHash = Get-CISha256Hex -Text $payloadJson
    return [pscustomobject]$event
}

function Assert-CIDisjointAgents {
    param(
        [Parameter(Mandatory)]
        [string[]]$ImplementingAgents,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$ReviewingAgents,

        [string]$AdditionalAgent
    )

    $implementers = [System.Collections.Generic.HashSet[string]]::new(
        $ImplementingAgents,
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($reviewer in $ReviewingAgents) {
        if ($implementers.Contains($reviewer)) {
            throw "Agent '$reviewer' cannot implement and independently review the same candidate."
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($AdditionalAgent) -and
        ($implementers.Contains($AdditionalAgent) -or
            @($ReviewingAgents | Where-Object {
                    $_.Equals($AdditionalAgent, [System.StringComparison]::OrdinalIgnoreCase)
                }).Count -gt 0)) {
        throw "Agent '$AdditionalAgent' is not disjoint from candidate implementation and review."
    }
}

function Assert-CIReviewEvidenceShape {
    param([Parameter(Mandatory)][object]$Event)

    $countNames = @($Event.reviewFindingCounts.PSObject.Properties.Name)
    if (Compare-Object `
            -ReferenceObject @('P0', 'P1', 'P2', 'P3') `
            -DifferenceObject $countNames `
            -SyncWindow 0) {
        throw "Ledger event $($Event.sequence) has invalid review finding counts."
    }
    $actualCounts = [ordered]@{ P0 = 0; P1 = 0; P2 = 0; P3 = 0 }
    $dispositions = [System.Collections.Generic.List[string]]::new()
    $seenFindings = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    $sortKeys = [System.Collections.Generic.List[string]]::new()
    foreach ($finding in @($Event.reviewFindings)) {
        $findingNames = @($finding.PSObject.Properties.Name)
        if (Compare-Object `
                -ReferenceObject @(
                    'severity',
                    'reviewerId',
                    'findingDigest',
                    'dispositionDecision',
                    'dispositionEvidenceDigest',
                    'dispositionDigest') `
                -DifferenceObject $findingNames `
                -SyncWindow 0) {
            throw "Ledger event $($Event.sequence) has an invalid review finding shape."
        }
        if ($finding.severity -cnotin @('P0', 'P1', 'P2', 'P3') -or
            $finding.findingDigest -notmatch '^[0-9a-f]{64}$' -or
            $finding.reviewerId -cne (ConvertTo-CINormalisedText -Value $finding.reviewerId) -or
            $finding.reviewerId -cnotin @($Event.reviewingAgents)) {
            throw "Ledger event $($Event.sequence) has a non-canonical review finding."
        }
        $identity = "$($finding.reviewerId):$($finding.severity):$($finding.findingDigest)"
        if (-not $seenFindings.Add($identity)) {
            throw "Ledger event $($Event.sequence) duplicates a review finding."
        }
        $actualCounts[$finding.severity]++
        $sortKeys.Add("$($finding.severity):$($finding.reviewerId):$($finding.findingDigest)")
        if ($finding.severity -in @('P2', 'P3')) {
            if ($finding.dispositionDecision -cnotin $script:CIReviewDispositionDecisions -or
                $finding.dispositionEvidenceDigest -notmatch '^[0-9a-f]{64}$') {
                throw 'REVIEW_DISPOSITION_REQUIRED: stored P2/P3 finding lacks canonical disposition evidence.'
            }
            $expectedDisposition = New-CIReviewDispositionDigest `
                -ExecutionKey $Event.executionKey `
                -CandidateDigest $Event.candidateDigest `
                -ReviewerId $finding.reviewerId `
                -Severity $finding.severity `
                -FindingDigest $finding.findingDigest `
                -Decision $finding.dispositionDecision `
                -EvidenceDigest $finding.dispositionEvidenceDigest
            if ($finding.dispositionDigest -cne $expectedDisposition) {
                throw 'REVIEW_DISPOSITION_INVALID: stored disposition is not candidate-bound.'
            }
            $dispositions.Add($finding.dispositionDigest)
        }
        elseif ($null -ne $finding.dispositionDecision -or
            $null -ne $finding.dispositionEvidenceDigest -or
            $null -ne $finding.dispositionDigest) {
            throw 'REVIEW_DISPOSITION_NOT_APPLICABLE: stored P0/P1 finding cannot be disposed.'
        }
    }
    $orderedKeys = @($sortKeys | Sort-Object -CaseSensitive)
    if (Compare-Object -ReferenceObject @($sortKeys) -DifferenceObject $orderedKeys -SyncWindow 0) {
        throw "Ledger event $($Event.sequence) has non-canonical review finding order."
    }
    foreach ($severity in @('P0', 'P1', 'P2', 'P3')) {
        $storedCount = $Event.reviewFindingCounts.$severity
        if (($storedCount -isnot [int] -and $storedCount -isnot [long]) -or
            $storedCount -lt 0 -or $storedCount -ne $actualCounts[$severity]) {
            throw "Ledger event $($Event.sequence) has inconsistent $severity review count."
        }
    }
    $canonicalDispositions = @(ConvertTo-CIDigestSet `
            -Values @($dispositions) `
            -Label 'Review disposition')
    if (Compare-Object `
            -ReferenceObject @($Event.reviewDispositionDigests) `
            -DifferenceObject $canonicalDispositions `
            -SyncWindow 0) {
        throw "Ledger event $($Event.sequence) has inconsistent review dispositions."
    }
}

function Assert-CIEventShape {
    param(
        [Parameter(Mandatory)]
        [object]$Event,

        [Parameter(Mandatory)]
        [long]$ExpectedSequence,

        [Parameter(Mandatory)]
        [string]$ExpectedPreviousHash
    )

    $names = @($Event.PSObject.Properties.Name)
    if ($names.Count -ne $script:CIEventPropertyOrder.Count) {
        throw "Ledger event $ExpectedSequence has an unexpected property count."
    }
    for ($index = 0; $index -lt $script:CIEventPropertyOrder.Count; $index++) {
        if ($names[$index] -cne $script:CIEventPropertyOrder[$index]) {
            throw "Ledger event $ExpectedSequence property order differs at index $index."
        }
    }
    if ($Event.schemaVersion -ne 1 -or $Event.sequence -ne $ExpectedSequence) {
        throw "Ledger event $ExpectedSequence has an invalid schema or sequence."
    }
    if ($Event.previousEventHash -cne $ExpectedPreviousHash) {
        throw "Ledger event $ExpectedSequence breaks the append-only hash chain."
    }
    foreach ($digest in @($Event.improvementId, $Event.previousEventHash, $Event.eventHash)) {
        if ($digest -notmatch '^[0-9a-f]{64}$') {
            throw "Ledger event $ExpectedSequence contains a malformed required digest."
        }
    }
    foreach ($optionalDigest in @(
            $Event.executionKey,
            $Event.executionScopeDigest,
            $Event.acceptanceDigest,
            $Event.candidateDigest,
            $Event.candidateScopeDigest,
            $Event.causalDeltaDigest,
            $Event.reviewReceiptDigest)) {
        if ($null -ne $optionalDigest -and $optionalDigest -notmatch '^[0-9a-f]{64}$') {
            throw "Ledger event $ExpectedSequence contains a malformed optional digest."
        }
    }
    if ($null -eq $Event.executionKey) {
        if ($null -ne $Event.executionScopeDigest -or
            $null -ne $Event.acceptanceDigest) {
            throw "Ledger event $ExpectedSequence has execution inputs without an execution key."
        }
    }
    elseif ($null -eq $Event.executionScopeDigest -or
        $null -eq $Event.acceptanceDigest) {
        throw "Ledger event $ExpectedSequence lacks declared execution-key inputs."
    }
    else {
        $expectedExecutionKey = New-CIExecutionKey `
            -ImprovementId $Event.improvementId `
            -Baseline $Event.baseline `
            -ScopeDigest $Event.executionScopeDigest `
            -AcceptanceDigest $Event.acceptanceDigest
        if ($Event.executionKey -cne $expectedExecutionKey) {
            throw "EXECUTION_KEY_IDENTITY_INVALID: ledger event $ExpectedSequence does not replay from its declared identity."
        }
    }
    foreach ($digestSetName in @(
            'causalFactReceiptDigests',
            'reviewDispositionDigests',
            'evidenceDigests')) {
        $digestSet = @(ConvertTo-CIDigestSet `
                -Values @(Get-CIPropertyValue -Value $Event -Name $digestSetName) `
                -Label $digestSetName)
        if (Compare-Object `
                -ReferenceObject @(Get-CIPropertyValue -Value $Event -Name $digestSetName) `
                -DifferenceObject $digestSet `
                -SyncWindow 0) {
            throw "Ledger event $ExpectedSequence contains a non-canonical digest set."
        }
    }
    foreach ($revision in @(
            $Event.candidateRevision,
            $Event.candidateTree,
            $Event.lastKnownGoodRevision,
            $Event.lastKnownGoodTree,
            $Event.promotionPreimageRevision,
            $Event.promotionPreimageTree,
            $Event.rollbackRevision,
            $Event.rollbackTree)) {
        if ($null -ne $revision -and $revision -notmatch '^[0-9a-f]{40}$') {
            throw "Ledger event $ExpectedSequence contains a malformed Git receipt."
        }
    }
    if ($Event.baseline -notmatch '^[0-9a-f]{40}$' -or
        $Event.eventType -cnotin $script:CIEventTypes -or
        $Event.workflowState -cnotin $script:CIWorkflowStates -or
        $Event.actorRole -cnotin $script:CIAgentRoles -or
        $Event.riskClass -cnotin @('STANDARD', 'HIGH') -or
        $Event.oldGateResult -cnotin @('NOT_APPLICABLE', 'PASS', 'FAIL') -or
        $Event.newGateResult -cnotin @('NOT_APPLICABLE', 'PASS', 'FAIL') -or
        $Event.reasonCode -notmatch '^[A-Z][A-Z0-9_]{1,95}$') {
        throw "Ledger event $ExpectedSequence contains a non-canonical enum or identity."
    }
    if ($Event.governanceChange -isnot [bool] -or
        $Event.observationWindowClosed -isnot [bool]) {
        throw "Ledger event $ExpectedSequence contains a non-Boolean control fact."
    }
    $canonicalRiskDomains = @(@($Event.riskDomains) | ForEach-Object {
            ([string]$_).Trim().ToUpperInvariant()
        } | Sort-Object -Unique -CaseSensitive)
    if (@($canonicalRiskDomains | Where-Object {
                $_ -cnotin $script:CIHighRiskDomains
            }).Count -gt 0 -or
        (Compare-Object `
            -ReferenceObject @($Event.riskDomains) `
            -DifferenceObject $canonicalRiskDomains `
            -SyncWindow 0) -or
        (($canonicalRiskDomains.Count -gt 0) -ne ($Event.riskClass -ceq 'HIGH')) -or
        (($canonicalRiskDomains -ccontains 'GOVERNANCE_POLICY') -ne $Event.governanceChange)) {
        throw "Ledger event $ExpectedSequence contains an inconsistent derived risk classification."
    }
    $canonicalCandidatePaths = @(ConvertTo-CINormalisedPaths -Paths @($Event.candidatePaths))
    if (Compare-Object `
            -ReferenceObject @($Event.candidatePaths) `
            -DifferenceObject $canonicalCandidatePaths `
            -SyncWindow 0) {
        throw "Ledger event $ExpectedSequence contains non-canonical candidate paths."
    }
    if ($canonicalCandidatePaths.Count -gt 0) {
        $replayedClassification = New-CICandidateClassification `
            -Paths $canonicalCandidatePaths `
            -AdditionalRiskDomains @($Event.riskDomains)
        if ($Event.candidateScopeDigest -cne $replayedClassification.candidateScopeDigest -or
            $Event.riskClass -cne $replayedClassification.riskClass -or
            $Event.governanceChange -ne $replayedClassification.governanceChange -or
            (Compare-Object `
                -ReferenceObject @($Event.riskDomains) `
                -DifferenceObject @($replayedClassification.riskDomains) `
                -SyncWindow 0)) {
            throw "Ledger event $ExpectedSequence risk does not replay from candidate paths."
        }
    }
    elseif ($null -ne $Event.candidateScopeDigest) {
        throw "Ledger event $ExpectedSequence has a candidate scope without candidate paths."
    }
    if ($null -eq $Event.causalDeltaDigest) {
        if ($null -ne $Event.causalDeltaFacts -or $Event.causalFactReceiptDigests.Count -ne 0) {
            throw "Ledger event $ExpectedSequence has causal facts without a derived receipt."
        }
    }
    elseif ($null -eq $Event.causalDeltaFacts -or $Event.causalFactReceiptDigests.Count -eq 0) {
        throw "Ledger event $ExpectedSequence lacks causal facts for its derived receipt."
    }
    Assert-CIReviewEvidenceShape -Event $Event
    if ($null -ne $Event.promotionId) {
        $parsedPromotionId = [guid]::Empty
        if (-not [guid]::TryParseExact($Event.promotionId, 'D', [ref]$parsedPromotionId) -or
            $Event.promotionId -cne $parsedPromotionId.ToString('D')) {
            throw "Ledger event $ExpectedSequence contains a non-canonical promotion ID."
        }
    }
    $parsedEventId = [guid]::Empty
    if (-not [guid]::TryParseExact($Event.eventId, 'D', [ref]$parsedEventId) -or
        $Event.eventId -cne $parsedEventId.ToString('D')) {
        throw "Ledger event $ExpectedSequence contains a non-canonical event ID."
    }
    $parsedTimestamp = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParseExact(
            $Event.recordedAtUtc,
            'yyyy-MM-ddTHH:mm:ss.fffffffZ',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal,
            [ref]$parsedTimestamp)) {
        throw "Ledger event $ExpectedSequence contains a non-canonical UTC timestamp '$($Event.recordedAtUtc)'."
    }
    $normalisedImplementers = @(ConvertTo-CINormalisedAgents -Agents @($Event.implementingAgents))
    $normalisedReviewers = @(ConvertTo-CINormalisedAgents -Agents @($Event.reviewingAgents))
    if (Compare-Object -ReferenceObject @($Event.implementingAgents) -DifferenceObject $normalisedImplementers -SyncWindow 0) {
        throw "Ledger event $ExpectedSequence contains non-canonical implementing agents."
    }
    if (Compare-Object -ReferenceObject @($Event.reviewingAgents) -DifferenceObject $normalisedReviewers -SyncWindow 0) {
        throw "Ledger event $ExpectedSequence contains non-canonical reviewing agents."
    }
    $expectedHash = Get-CISha256Hex -Text (
        (Get-CIEventHashPayload -Event $Event | ConvertTo-Json -Depth 12 -Compress))
    if ($Event.eventHash -cne $expectedHash) {
        throw "Ledger event $ExpectedSequence has been modified after append."
    }
}

function Assert-CIEventSemantics {
    param(
        [Parameter(Mandatory)]
        [object]$Event,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PriorEvents,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    $expectedState = @{
        FINDING_DETECTED = 'AGENT_DECIDED'
        DUPLICATE_SUPPRESSED = 'AGENT_DECIDED'
        TRIAGED = 'AGENT_DECIDED'
        ATTEMPT_STARTED = 'AGENT_DECIDED'
        REVIEW_RECORDED = 'AGENT_DECIDED'
        GATE_FAILED = 'AUTOMATED_GATE_FAIL'
        GATE_PASSED = 'AUTOMATED_GATE_PASS'
        QUARANTINED = 'AUTOMATED_GATE_FAIL'
        PROMOTED = 'LOCAL_COMPLETE'
        OBSERVATION_PASSED = 'LOCAL_COMPLETE'
        OBSERVATION_FAILED = 'AUTOMATED_GATE_FAIL'
        ROLLBACK_REQUIRED = 'AUTOMATED_GATE_FAIL'
        ROLLED_BACK = 'LOCAL_COMPLETE'
        CLOSED = 'LOCAL_COMPLETE'
        EXTERNAL_PREREQUISITE_RECORDED = 'EXTERNAL_PREREQUISITE'
        AUTHORITY_BLOCKED = 'BLOCKED_BY_HIGHER_AUTHORITY'
    }
    if ($Event.workflowState -cne $expectedState[$Event.eventType]) {
        throw "Event '$($Event.eventType)' has an invalid workflow state."
    }
    $improvementEvents = @($PriorEvents | Where-Object {
            $_.improvementId -ceq $Event.improvementId -and
            $_.eventType -cne 'DUPLICATE_SUPPRESSED'
        })
    $previous = if ($improvementEvents.Count -eq 0) {
        $null
    }
    else {
        $improvementEvents[-1]
    }
    if ($Event.eventType -ceq 'ATTEMPT_STARTED') {
        if ($null -eq $Event.executionKey -or
            $null -eq $Event.executionScopeDigest -or
            $null -eq $Event.acceptanceDigest) {
            throw 'EXECUTION_KEY_IDENTITY_INVALID: an attempt requires its complete execution identity.'
        }
    }
    elseif ($null -eq $previous -or $null -eq $previous.executionKey) {
        if ($null -ne $Event.executionKey -or
            $null -ne $Event.executionScopeDigest -or
            $null -ne $Event.acceptanceDigest) {
            throw 'EXECUTION_KEY_IDENTITY_INVALID: execution identity can begin only at an attempt.'
        }
    }
    elseif ($Event.executionKey -cne $previous.executionKey -or
        $Event.executionScopeDigest -cne $previous.executionScopeDigest -or
        $Event.acceptanceDigest -cne $previous.acceptanceDigest) {
        throw 'EXECUTION_KEY_DRIFT: a finding chain cannot change its canonical execution identity.'
    }
    if ($Event.eventType -ceq 'DUPLICATE_SUPPRESSED') {
        if ($null -eq $previous) {
            throw 'Duplicate suppression requires an existing canonical finding.'
        }
        return
    }
    $allowedPredecessors = @{
        FINDING_DETECTED = @()
        TRIAGED = @('FINDING_DETECTED')
        ATTEMPT_STARTED = @('TRIAGED', 'GATE_FAILED')
        REVIEW_RECORDED = @('ATTEMPT_STARTED', 'REVIEW_RECORDED')
        GATE_FAILED = @('ATTEMPT_STARTED', 'REVIEW_RECORDED')
        GATE_PASSED = @('ATTEMPT_STARTED', 'REVIEW_RECORDED')
        QUARANTINED = @('GATE_FAILED')
        PROMOTED = @('GATE_PASSED')
        OBSERVATION_PASSED = @('PROMOTED')
        OBSERVATION_FAILED = @('PROMOTED')
        ROLLBACK_REQUIRED = @('OBSERVATION_FAILED')
        ROLLED_BACK = @('ROLLBACK_REQUIRED')
        CLOSED = @('OBSERVATION_PASSED', 'ROLLED_BACK', 'QUARANTINED')
        EXTERNAL_PREREQUISITE_RECORDED = @(
            'FINDING_DETECTED', 'TRIAGED', 'GATE_FAILED', 'QUARANTINED')
        AUTHORITY_BLOCKED = @(
            'FINDING_DETECTED', 'TRIAGED', 'GATE_FAILED', 'QUARANTINED')
    }
    if ($Event.eventType -ceq 'FINDING_DETECTED') {
        if ($null -ne $previous) {
            throw 'A stable finding fingerprint can be detected only once.'
        }
    }
    elseif ($null -eq $previous -or
        $previous.eventType -cnotin $allowedPredecessors[$Event.eventType]) {
        $previousType = if ($null -eq $previous) { 'NONE' } else { $previous.eventType }
        throw "Event '$($Event.eventType)' cannot follow '$previousType'."
    }
    if ($null -ne $previous -and $Event.baseline -cne $previous.baseline) {
        throw 'One improvement event chain cannot silently change baseline.'
    }

    if ($Event.eventType -ceq 'ATTEMPT_STARTED') {
        if ($null -eq $Event.executionKey -or
            $null -eq $Event.candidateDigest -or
            $null -eq $Event.candidateScopeDigest) {
            throw 'An attempt requires exact execution, candidate and scope digests.'
        }
        if ($Event.actorRole -cne 'IMPLEMENTER' -or
            $Event.implementingAgents.Count -lt 1 -or
            $Event.actorId -cnotin $Event.implementingAgents -or
            $Event.reviewingAgents.Count -ne 0) {
            throw 'An attempt requires an identified implementer and no reviewer claim.'
        }
        $attempts = @($improvementEvents | Where-Object {
                $_.eventType -ceq 'ATTEMPT_STARTED'
            })
        if ($attempts.Count -ge $MaximumAttempts) {
            throw "ATTEMPT_BUDGET_EXHAUSTED: no more than $MaximumAttempts candidates are admitted."
        }
        if ($attempts.Count -gt 0 -and
            $Event.executionKey -cne $attempts[0].executionKey) {
            throw 'EXECUTION_KEY_DRIFT: a finding chain cannot reset its attempt identity or budget.'
        }
        if (@($attempts | Where-Object {
                    $_.candidateDigest -ceq $Event.candidateDigest
                }).Count -gt 0) {
            throw 'SAME_CANDIDATE_RETRY: an identical candidate can never be attempted twice.'
        }
        if ($attempts.Count -gt 0) {
            if ($null -eq $Event.causalDeltaDigest -or
                $Event.causalFactReceiptDigests.Count -lt 1 -or
                @($attempts | Where-Object {
                        $_.causalDeltaDigest -ceq $Event.causalDeltaDigest
                    }).Count -gt 0) {
                throw 'CAUSAL_DELTA_REQUIRED: a successor attempt requires one new causal delta.'
            }
            $expectedCausal = New-CICausalDeltaReceipt `
                -Facts $Event.causalDeltaFacts `
                -CandidateDigest $Event.candidateDigest `
                -PriorEvents $PriorEvents `
                -ImprovementId $Event.improvementId `
                -ExecutionKey $Event.executionKey
            if ($Event.causalDeltaDigest -cne $expectedCausal.digest -or
                (Compare-Object `
                    -ReferenceObject @($Event.causalFactReceiptDigests) `
                    -DifferenceObject @($expectedCausal.receiptDigests) `
                    -SyncWindow 0) -or
                (($Event.causalDeltaFacts | ConvertTo-Json -Compress) -cne
                    ($expectedCausal.facts | ConvertTo-Json -Compress))) {
                throw 'CAUSAL_DELTA_REQUIRED: successor receipt does not replay from ledger facts.'
            }
        }
        elseif ($null -ne $Event.causalDeltaDigest -or
            $Event.causalFactReceiptDigests.Count -ne 0 -or
            $null -ne $Event.causalDeltaFacts) {
            throw 'CAUSAL_DELTA_NOT_APPLICABLE: a first attempt cannot claim successor facts.'
        }
    }

    $latestAttempt = @($improvementEvents | Where-Object {
            $_.eventType -ceq 'ATTEMPT_STARTED'
        } | Select-Object -Last 1)
    if ($Event.eventType -ceq 'REVIEW_RECORDED') {
        if ($latestAttempt.Count -ne 1 -or
            $Event.executionKey -cne $latestAttempt[0].executionKey -or
            $Event.candidateDigest -cne $latestAttempt[0].candidateDigest -or
            $Event.candidateScopeDigest -cne $latestAttempt[0].candidateScopeDigest -or
            $Event.governanceChange -ne $latestAttempt[0].governanceChange -or
            $Event.riskClass -cne $latestAttempt[0].riskClass -or
            (Compare-Object `
                -ReferenceObject @($Event.riskDomains) `
                -DifferenceObject @($latestAttempt[0].riskDomains) `
                -SyncWindow 0)) {
            throw 'A review receipt must bind to the latest exact candidate and derived scope.'
        }
        if (Compare-Object `
                -ReferenceObject @($latestAttempt[0].implementingAgents) `
                -DifferenceObject @($Event.implementingAgents) `
                -SyncWindow 0) {
            throw 'A review receipt must preserve the exact candidate implementer set.'
        }
        if ($Event.actorRole -cne 'REVIEWER' -or
            $Event.result -cnotin @('PASS', 'FAIL') -or
            $Event.reviewingAgents.Count -ne 1 -or
            $Event.reviewingAgents[0] -cne $Event.actorId -or
            $null -eq $Event.reviewReceiptDigest -or
            $Event.evidenceDigests.Count -lt 1) {
            throw 'A review requires one factual reviewer event, result and SHA receipt.'
        }
        $expectedReviewReceipt = New-CIReviewReceiptDigest `
            -ReviewerId $Event.actorId `
            -ExecutionKey $Event.executionKey `
            -CandidateDigest $Event.candidateDigest `
            -CandidateScopeDigest $Event.candidateScopeDigest `
            -Result $Event.result `
            -EvidenceDigests @($Event.evidenceDigests) `
            -ReviewFindingCounts $Event.reviewFindingCounts `
            -ReviewFindings @($Event.reviewFindings) `
            -ReviewDispositionDigests @($Event.reviewDispositionDigests)
        if ($Event.reviewReceiptDigest -cne $expectedReviewReceipt -or
            ($Event.result -ceq 'PASS' -and
                ($Event.reviewFindingCounts.P0 -ne 0 -or
                    $Event.reviewFindingCounts.P1 -ne 0))) {
            throw 'REVIEW_RECEIPT_INVALID: review result and structured findings do not replay.'
        }
        Assert-CIDisjointAgents `
            -ImplementingAgents @($Event.implementingAgents) `
            -ReviewingAgents @($Event.reviewingAgents)
        $reviewsForAttempt = @($improvementEvents | Where-Object {
                $_.eventType -ceq 'REVIEW_RECORDED' -and
                $_.sequence -gt $latestAttempt[0].sequence
            })
        if (@($reviewsForAttempt | Where-Object {
                    $_.actorId -ceq $Event.actorId -or
                    $_.reviewReceiptDigest -ceq $Event.reviewReceiptDigest
                }).Count -gt 0) {
            throw 'DUPLICATE_REVIEW_RECEIPT: a reviewer must record exactly one review per attempt.'
        }
    }

    if ($Event.eventType -in @('GATE_FAILED', 'GATE_PASSED')) {
        if ($latestAttempt.Count -ne 1 -or
            $Event.executionKey -cne $latestAttempt[0].executionKey -or
            $Event.candidateDigest -cne $latestAttempt[0].candidateDigest -or
            $Event.candidateScopeDigest -cne $latestAttempt[0].candidateScopeDigest -or
            $Event.governanceChange -ne $latestAttempt[0].governanceChange -or
            $Event.riskClass -cne $latestAttempt[0].riskClass -or
            (Compare-Object `
                -ReferenceObject @($Event.riskDomains) `
                -DifferenceObject @($latestAttempt[0].riskDomains) `
                -SyncWindow 0)) {
            throw 'A gate result must bind to the latest exact candidate and derived scope.'
        }
        if (Compare-Object `
                -ReferenceObject @($latestAttempt[0].implementingAgents) `
                -DifferenceObject @($Event.implementingAgents) `
                -SyncWindow 0) {
            throw 'A gate result must preserve the exact candidate implementer set.'
        }
        $reviewsForAttempt = @($improvementEvents | Where-Object {
                $_.eventType -ceq 'REVIEW_RECORDED' -and
                $_.sequence -gt $latestAttempt[0].sequence
            })
        $factualReviewers = @(ConvertTo-CINormalisedAgents -Agents @(
                $reviewsForAttempt | ForEach-Object { $_.actorId }))
        if (Compare-Object `
                -ReferenceObject $factualReviewers `
                -DifferenceObject @($Event.reviewingAgents) `
                -SyncWindow 0) {
            throw 'PHANTOM_REVIEWER_REJECTED: gate reviewers require individual review events.'
        }
        $expectedCounts = [ordered]@{ P0 = 0; P1 = 0; P2 = 0; P3 = 0 }
        foreach ($review in $reviewsForAttempt) {
            foreach ($severity in @('P0', 'P1', 'P2', 'P3')) {
                $expectedCounts[$severity] += [int]$review.reviewFindingCounts.$severity
            }
        }
        $expectedFindings = @($reviewsForAttempt | ForEach-Object {
                @($_.reviewFindings)
            } | Sort-Object -Property severity, reviewerId, findingDigest)
        $expectedDispositions = @(ConvertTo-CIDigestSet `
                -Values @($reviewsForAttempt | ForEach-Object {
                        @($_.reviewDispositionDigests)
                    }) `
                -Label 'Review disposition')
        if (($Event.reviewFindingCounts | ConvertTo-Json -Compress) -cne
                ([pscustomobject]$expectedCounts | ConvertTo-Json -Compress) -or
            (@($Event.reviewFindings) | ConvertTo-Json -Depth 8 -Compress) -cne
                ($expectedFindings | ConvertTo-Json -Depth 8 -Compress) -or
            (Compare-Object `
                -ReferenceObject @($Event.reviewDispositionDigests) `
                -DifferenceObject $expectedDispositions `
                -SyncWindow 0)) {
            throw 'REVIEW_SUMMARY_INVALID: gate review evidence does not replay from factual reviews.'
        }
        Assert-CIDisjointAgents `
            -ImplementingAgents @($Event.implementingAgents) `
            -ReviewingAgents @($Event.reviewingAgents) `
            -AdditionalAgent $Event.actorId
    }
    if ($Event.eventType -ceq 'GATE_PASSED') {
        $minimumReviewers = if ($Event.governanceChange -or $Event.riskClass -ceq 'HIGH') { 2 } else { 1 }
        if ($Event.result -cne 'PASS' -or
            $Event.newGateResult -cne 'PASS' -or
            $Event.actorRole -cne 'VERIFIER' -or
            $Event.implementingAgents.Count -lt 1 -or
            $Event.reviewingAgents.Count -lt $minimumReviewers -or
            @($reviewsForAttempt | Where-Object { $_.result -cne 'PASS' }).Count -gt 0 -or
            $Event.reviewFindingCounts.P0 -ne 0 -or
            $Event.reviewFindingCounts.P1 -ne 0 -or
            $Event.evidenceDigests.Count -lt 1) {
            throw 'A passing gate lacks mandatory implementation or independent-review evidence.'
        }
        foreach ($review in $reviewsForAttempt) {
            if ($null -eq $review.reviewReceiptDigest -or
                $review.reviewReceiptDigest -cnotin $Event.evidenceDigests) {
                throw 'REVIEW_RECEIPT_REQUIRED: the passing gate does not carry every factual review receipt.'
            }
        }
        if ($Event.governanceChange -and
            ($Event.oldGateResult -cne 'PASS' -or $Event.riskClass -cne 'HIGH')) {
            throw 'Governance changes require the old and new meta-gates plus high-risk review.'
        }
        if (-not $Event.governanceChange -and $Event.oldGateResult -cne 'NOT_APPLICABLE') {
            throw 'A non-governance candidate must not fabricate an old meta-gate result.'
        }
    }
    elseif ($Event.eventType -ceq 'GATE_FAILED') {
        $failedChecksAreFactual = if ($Event.governanceChange) {
            $Event.riskClass -ceq 'HIGH' -and
            $Event.oldGateResult -cin @('PASS', 'FAIL') -and
            $Event.newGateResult -cin @('PASS', 'FAIL') -and
            ($Event.oldGateResult -ceq 'FAIL' -or $Event.newGateResult -ceq 'FAIL')
        }
        else {
            $Event.oldGateResult -ceq 'NOT_APPLICABLE' -and
            $Event.newGateResult -ceq 'FAIL'
        }
        if ($Event.result -cne 'FAIL' -or
            $Event.actorRole -cne 'VERIFIER' -or
            $Event.evidenceDigests.Count -lt 1 -or
            -not $failedChecksAreFactual) {
            throw 'A failed gate must preserve literal FAIL, verifier identity, evidence and an applicable failed old/new check.'
        }
    }

    if ($Event.eventType -ceq 'PROMOTED') {
        if ($Event.actorRole -cne 'INTEGRATOR' -or
            $null -eq $Event.candidateRevision -or
            $null -eq $Event.candidateTree -or
            $null -eq $Event.lastKnownGoodRevision -or
            $null -eq $Event.lastKnownGoodTree -or
            $null -eq $Event.promotionPreimageRevision -or
            $null -eq $Event.promotionPreimageTree -or
            $null -eq $Event.promotionId -or
            $Event.result -cne 'PROMOTED' -or
            $Event.executionKey -cne $previous.executionKey -or
            $Event.candidateDigest -cne $previous.candidateDigest -or
            $Event.baseline -cne $Event.lastKnownGoodRevision -or
            $Event.promotionPreimageRevision -cne $Event.lastKnownGoodRevision -or
            $Event.promotionPreimageTree -cne $Event.lastKnownGoodTree -or
            $Event.candidateRevision -ceq $Event.lastKnownGoodRevision -or
            $Event.candidateTree -ceq $Event.lastKnownGoodTree -or
            $previous.reviewFindingCounts.P0 -ne 0 -or
            $previous.reviewFindingCounts.P1 -ne 0 -or
            $Event.evidenceDigests.Count -lt 1) {
            throw 'Promotion requires Git-proven candidate, preimage, LKG, promotion ID and integrator receipts.'
        }
        Assert-CIDisjointAgents `
            -ImplementingAgents @($previous.implementingAgents) `
            -ReviewingAgents @($previous.reviewingAgents) `
            -AdditionalAgent $Event.actorId
    }
    if ($Event.eventType -in @('OBSERVATION_PASSED', 'OBSERVATION_FAILED')) {
        if ($Event.actorRole -cne 'OBSERVER' -or
            -not $Event.observationWindowClosed -or
            $Event.promotionId -cne $previous.promotionId -or
            $Event.candidateRevision -cne $previous.candidateRevision -or
            $Event.candidateTree -cne $previous.candidateTree -or
            $Event.lastKnownGoodRevision -cne $previous.lastKnownGoodRevision -or
            $Event.lastKnownGoodTree -cne $previous.lastKnownGoodTree -or
            $Event.promotionPreimageRevision -cne $previous.promotionPreimageRevision -or
            $Event.promotionPreimageTree -cne $previous.promotionPreimageTree -or
            $Event.executionKey -cne $previous.executionKey -or
            $Event.candidateDigest -cne $previous.candidateDigest -or
            $Event.evidenceDigests.Count -lt 1) {
            throw 'Observation must close the exact promoted candidate window independently.'
        }
        $gate = @($improvementEvents | Where-Object { $_.eventType -ceq 'GATE_PASSED' })[-1]
        Assert-CIDisjointAgents `
            -ImplementingAgents @($gate.implementingAgents) `
            -ReviewingAgents @($gate.reviewingAgents) `
            -AdditionalAgent $Event.actorId
        if ($Event.actorId.Equals(
                $previous.actorId,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'OBSERVER_INTEGRATOR_COLLISION: observation must be independent from promotion integration.'
        }
        if (($Event.eventType -ceq 'OBSERVATION_PASSED' -and $Event.result -cne 'PASS') -or
            ($Event.eventType -ceq 'OBSERVATION_FAILED' -and $Event.result -cne 'FAIL')) {
            throw 'Observation must preserve its literal PASS or FAIL result.'
        }
    }
    if ($Event.eventType -ceq 'ROLLBACK_REQUIRED') {
        if ($Event.result -cne 'ROLLBACK_REQUIRED' -or
            $Event.promotionId -cne $previous.promotionId -or
            $Event.candidateRevision -cne $previous.candidateRevision -or
            $Event.candidateTree -cne $previous.candidateTree -or
            $Event.lastKnownGoodRevision -cne $previous.lastKnownGoodRevision -or
            $Event.lastKnownGoodTree -cne $previous.lastKnownGoodTree -or
            $Event.promotionPreimageRevision -cne $previous.promotionPreimageRevision -or
            $Event.promotionPreimageTree -cne $previous.promotionPreimageTree -or
            $Event.executionKey -cne $previous.executionKey -or
            $Event.candidateDigest -cne $previous.candidateDigest) {
            throw 'Rollback routing must retain the failed promotion and exact LKG target.'
        }
    }
    if ($Event.eventType -ceq 'ROLLED_BACK') {
        if ($Event.actorRole -cne 'INTEGRATOR' -or
            $Event.result -cne 'ROLLED_BACK' -or
            $Event.newGateResult -cne 'PASS' -or
            $Event.promotionId -cne $previous.promotionId -or
            $Event.candidateRevision -cne $previous.candidateRevision -or
            $Event.candidateTree -cne $previous.candidateTree -or
            $Event.lastKnownGoodRevision -cne $previous.lastKnownGoodRevision -or
            $Event.lastKnownGoodTree -cne $previous.lastKnownGoodTree -or
            $Event.promotionPreimageRevision -cne $previous.promotionPreimageRevision -or
            $Event.promotionPreimageTree -cne $previous.promotionPreimageTree -or
            $null -eq $Event.rollbackRevision -or
            $null -eq $Event.rollbackTree -or
            $Event.rollbackRevision -cne $Event.lastKnownGoodRevision -or
            $Event.rollbackTree -cne $Event.lastKnownGoodTree -or
            $Event.executionKey -cne $previous.executionKey -or
            $Event.candidateDigest -cne $previous.candidateDigest -or
            $Event.evidenceDigests.Count -lt 1) {
            throw 'A completed rollback requires exact LKG, gate and evidence records.'
        }
        $gate = @($improvementEvents | Where-Object {
                $_.eventType -ceq 'GATE_PASSED'
            })[-1]
        Assert-CIDisjointAgents `
            -ImplementingAgents @($gate.implementingAgents) `
            -ReviewingAgents @($gate.reviewingAgents) `
            -AdditionalAgent $Event.actorId
    }
}

function Assert-CILedgerEvents {
    <#
    .SYNOPSIS
    Validates the complete chain, schema, transitions and retry invariants.
    #>
    param(
        [AllowEmptyCollection()]
        [object[]]$Events = @(),

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    if ($Events.Count -gt $MaximumEvents) {
        throw "LEDGER_BOUND_EXCEEDED: the ledger contains more than $MaximumEvents events."
    }
    $previousHash = '0' * 64
    $seenEventIds = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::Ordinal)
    $previousTimestamp = [DateTimeOffset]::MinValue
    for ($index = 0; $index -lt $Events.Count; $index++) {
        $event = $Events[$index]
        Assert-CIEventShape `
            -Event $event `
            -ExpectedSequence ($index + 1) `
            -ExpectedPreviousHash $previousHash
        if (-not $seenEventIds.Add($event.eventId)) {
            throw "Ledger event $($event.sequence) reuses an event ID."
        }
        $timestamp = [DateTimeOffset]::ParseExact(
            $event.recordedAtUtc,
            'yyyy-MM-ddTHH:mm:ss.fffffffZ',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::AssumeUniversal)
        if ($timestamp -lt $previousTimestamp) {
            throw "Ledger event $($event.sequence) moves UTC time backwards."
        }
        $priorEvents = @()
        if ($index -gt 0) {
            $priorEvents = @($Events[0..($index - 1)])
        }
        Assert-CIEventSemantics `
            -Event $event `
            -PriorEvents $priorEvents `
            -MaximumAttempts $MaximumAttempts
        $previousTimestamp = $timestamp
        $previousHash = $event.eventHash
    }
}

function ConvertFrom-CILedgerText {
    [OutputType([object[]])]
    param(
        [AllowEmptyString()]
        [string]$Text,

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    if ([string]::IsNullOrEmpty($Text)) {
        return @()
    }
    if ($Text.Contains([char]13) -or -not $Text.EndsWith("`n")) {
        throw 'The append-only ledger must use LF and end with one complete event line.'
    }
    $lines = @($Text.TrimEnd("`n").Split("`n"))
    if ($lines.Count -gt $MaximumEvents) {
        throw "LEDGER_BOUND_EXCEEDED: the ledger contains more than $MaximumEvents events."
    }
    $events = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            throw 'The append-only ledger contains an empty event line.'
        }
        $document = $null
        try {
            $document = Assert-CIStrictJsonText `
                -Text $line `
                -Context 'Ledger event JSON'
            try {
                $timestampElement = $document.RootElement.GetProperty('recordedAtUtc')
                if ($timestampElement.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
                    throw 'recordedAtUtc must be a JSON string.'
                }
                $rawTimestamp = $timestampElement.GetString()
            }
            finally {
                $document.Dispose()
            }
            $event = $line | ConvertFrom-Json -Depth 20
            # ConvertFrom-Json may materialise ISO strings as DateTime; retain the signed JSON value.
            $event.recordedAtUtc = $rawTimestamp
            $events.Add($event)
        }
        catch {
            if ($_.Exception.Message -match 'DUPLICATE_JSON_PROPERTY') {
                throw
            }
            throw 'The append-only ledger contains malformed JSON.'
        }
    }
    Assert-CILedgerEvents `
        -Events @($events) `
        -MaximumEvents $MaximumEvents `
        -MaximumAttempts $MaximumAttempts
    return @($events)
}

function Assert-CINoReparsePoint {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $current = [System.IO.Path]::GetFullPath($Path)
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if ([System.IO.File]::Exists($current) -or
            [System.IO.Directory]::Exists($current)) {
            $attributes = [System.IO.File]::GetAttributes($current)
            if (($attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "UNSAFE_PENDING_PATH: reparse point '$current' is not allowed."
            }
        }
        $parent = [System.IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or
            $parent.Equals($current, [System.StringComparison]::OrdinalIgnoreCase)) {
            break
        }
        $current = $parent
    }
}

function Assert-CIRegularFilePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$Label
    )

    Assert-CINoReparsePoint -Path $Path
    if ([System.IO.Directory]::Exists($Path)) {
        throw "UNSAFE_PENDING_PATH: $Label resolves to a directory."
    }
    if ([System.IO.File]::Exists($Path)) {
        $attributes = [System.IO.File]::GetAttributes($Path)
        if (($attributes -band [System.IO.FileAttributes]::Directory) -ne 0 -or
            ($attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "UNSAFE_PENDING_PATH: $Label is not a regular file."
        }
    }
}

function Assert-CIPathContained {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Label
    )

    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $comparison = if ($IsWindows) {
        [System.StringComparison]::OrdinalIgnoreCase
    }
    else {
        [System.StringComparison]::Ordinal
    }
    $rootPrefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($rootPrefix, $comparison)) {
        throw "PATH_OUTSIDE_AUTHORISED_ROOT: $Label is outside the exact authorised root."
    }
    Assert-CINoReparsePoint -Path $fullRoot
    Assert-CINoReparsePoint -Path $fullPath
    return $fullPath
}

function Get-CILedgerPathSet {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$AuthorisedRoot,

        [switch]$CreateDirectory
    )

    $fullAuthorisedRoot = [System.IO.Path]::GetFullPath($AuthorisedRoot)
    Assert-CINoReparsePoint -Path $fullAuthorisedRoot
    if ($CreateDirectory -and -not [System.IO.Directory]::Exists($fullAuthorisedRoot)) {
        [void][System.IO.Directory]::CreateDirectory($fullAuthorisedRoot)
    }
    if (-not [System.IO.Directory]::Exists($fullAuthorisedRoot)) {
        throw 'PATH_OUTSIDE_AUTHORISED_ROOT: the authorised ledger root does not exist.'
    }
    Assert-CINoReparsePoint -Path $fullAuthorisedRoot
    $fullPath = Assert-CIPathContained `
        -Path $Path `
        -Root $fullAuthorisedRoot `
        -Label 'ledger'
    $leaf = [System.IO.Path]::GetFileName($fullPath)
    if ([string]::IsNullOrWhiteSpace($leaf) -or
        -not $leaf.EndsWith('.jsonl', [System.StringComparison]::OrdinalIgnoreCase) -or
        $leaf.EndsWith('.pending.jsonl', [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'UNSAFE_PENDING_PATH: a ledger must be an explicit .jsonl regular-file path.'
    }
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if ([string]::IsNullOrWhiteSpace($directory)) {
        throw 'UNSAFE_PENDING_PATH: the ledger parent directory is unresolved.'
    }
    Assert-CINoReparsePoint -Path $directory
    if ($CreateDirectory -and -not [System.IO.Directory]::Exists($directory)) {
        [void][System.IO.Directory]::CreateDirectory($directory)
    }
    if (-not [System.IO.Directory]::Exists($directory)) {
        throw 'UNSAFE_PENDING_PATH: the ledger parent directory does not exist.'
    }
    Assert-CINoReparsePoint -Path $directory
    Assert-CIRegularFilePath -Path $fullPath -Label 'ledger'
    $pendingPath = $fullPath + '.pending'
    $stagingPath = $pendingPath + '.new'
    Assert-CIRegularFilePath -Path $pendingPath -Label 'pending journal'
    Assert-CIRegularFilePath -Path $stagingPath -Label 'pending staging journal'
    return [pscustomobject][ordered]@{
        ledger = $fullPath
        pending = $pendingPath
        staging = $stagingPath
    }
}

function Get-CILedgerPathDigest {
    [OutputType([string])]
    param([Parameter(Mandatory)][string]$Path)

    $identity = [System.IO.Path]::GetFullPath($Path)
    if ($IsWindows) {
        $identity = $identity.ToLowerInvariant()
    }
    return Get-CISha256Hex -Text $identity
}

function Read-CIStreamBytes {
    [OutputType([byte[]])]
    param([Parameter(Mandatory)][System.IO.FileStream]$Stream)

    if ($Stream.Length -gt $script:CIMaximumLedgerBytes) {
        throw "LEDGER_BOUND_EXCEEDED: the ledger exceeds $script:CIMaximumLedgerBytes bytes."
    }
    $length = [int]$Stream.Length
    $bytes = [byte[]]::new($length)
    [void]$Stream.Seek(0, [System.IO.SeekOrigin]::Begin)
    $offset = 0
    while ($offset -lt $length) {
        $read = $Stream.Read($bytes, $offset, $length - $offset)
        if ($read -eq 0) {
            throw 'The ledger ended before its reported length.'
        }
        $offset += $read
    }
    Write-Output -NoEnumerate $bytes
}

function ConvertFrom-CIUtf8Bytes {
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [byte[]]$Bytes
    )

    if ($Bytes.Length -ge 3 -and
        $Bytes[0] -eq 0xEF -and
        $Bytes[1] -eq 0xBB -and
        $Bytes[2] -eq 0xBF) {
        throw 'UTF8_BOM_FORBIDDEN: continuous-improvement journals require UTF-8 without BOM.'
    }
    return [System.Text.UTF8Encoding]::new($false, $true).GetString($Bytes)
}

function Get-CIPendingHashPayload {
    [OutputType([System.Collections.Specialized.OrderedDictionary])]
    param([Parameter(Mandatory)][object]$Journal)

    $payload = [ordered]@{}
    foreach ($name in $script:CIPendingPropertyOrder | Where-Object { $_ -cne 'pendingHash' }) {
        $payload[$name] = Get-CIPropertyValue -Value $Journal -Name $name
    }
    return $payload
}

function New-CIPendingJournal {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$LedgerPath,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [byte[]]$BaseBytes,

        [Parameter(Mandatory)]
        [object]$Event
    )

    $eventText = ($Event | ConvertTo-Json -Depth 12 -Compress) + "`n"
    $eventBytes = [System.Text.UTF8Encoding]::new($false).GetBytes($eventText)
    $journal = [ordered]@{
        schemaVersion = 1
        ledgerPathDigest = Get-CILedgerPathDigest -Path $LedgerPath
        baseLength = [long]$BaseBytes.Length
        baseDigest = Get-CISha256BytesHex -Bytes $BaseBytes
        eventByteLength = [long]$eventBytes.Length
        eventDigest = Get-CISha256BytesHex -Bytes $eventBytes
        eventHash = $Event.eventHash
        eventBytesBase64 = [Convert]::ToBase64String($eventBytes)
        pendingHash = $null
    }
    $journal.pendingHash = Get-CISha256Hex -Text (
        (Get-CIPendingHashPayload -Journal $journal | ConvertTo-Json -Depth 8 -Compress))
    return [pscustomobject]$journal
}

function ConvertFrom-CIPendingJournalText {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$Text,

        [Parameter(Mandatory)]
        [string]$LedgerPath
    )

    if ($Text.Contains([char]13) -or -not $Text.EndsWith("`n") -or
        $Text.TrimEnd("`n").Contains("`n")) {
        throw 'PENDING_APPEND_CORRUPT: pending journal framing is invalid.'
    }
    $journalDocument = $null
    try {
        $journalText = $Text.TrimEnd("`n")
        $journalDocument = Assert-CIStrictJsonText `
            -Text $journalText `
            -Context 'Pending journal JSON'
        $journal = $journalText | ConvertFrom-Json -Depth 12
    }
    catch {
        if ($_.Exception.Message -match 'DUPLICATE_JSON_PROPERTY') {
            throw
        }
        throw 'PENDING_APPEND_CORRUPT: pending journal JSON is invalid.'
    }
    finally {
        if ($null -ne $journalDocument) {
            $journalDocument.Dispose()
        }
    }
    $names = @($journal.PSObject.Properties.Name)
    if ($names.Count -ne $script:CIPendingPropertyOrder.Count) {
        throw 'PENDING_APPEND_CORRUPT: pending journal property count is invalid.'
    }
    for ($index = 0; $index -lt $script:CIPendingPropertyOrder.Count; $index++) {
        if ($names[$index] -cne $script:CIPendingPropertyOrder[$index]) {
            throw 'PENDING_APPEND_CORRUPT: pending journal property order is invalid.'
        }
    }
    if ($journal.schemaVersion -ne 1 -or
        $journal.ledgerPathDigest -cne (Get-CILedgerPathDigest -Path $LedgerPath) -or
        $journal.baseLength -lt 0 -or
        $journal.eventByteLength -lt 1 -or
        $journal.baseLength -gt $script:CIMaximumLedgerBytes -or
        $journal.eventByteLength -gt $script:CIMaximumLedgerBytes -or
        $journal.baseDigest -notmatch '^[0-9a-f]{64}$' -or
        $journal.eventDigest -notmatch '^[0-9a-f]{64}$' -or
        $journal.eventHash -notmatch '^[0-9a-f]{64}$' -or
        $journal.pendingHash -notmatch '^[0-9a-f]{64}$') {
        throw 'PENDING_APPEND_CORRUPT: pending journal identity is invalid.'
    }
    $expectedPendingHash = Get-CISha256Hex -Text (
        (Get-CIPendingHashPayload -Journal $journal | ConvertTo-Json -Depth 8 -Compress))
    if ($journal.pendingHash -cne $expectedPendingHash) {
        throw 'PENDING_APPEND_CORRUPT: pending journal hash is invalid.'
    }
    try {
        $eventBytes = [Convert]::FromBase64String($journal.eventBytesBase64)
    }
    catch {
        throw 'PENDING_APPEND_CORRUPT: pending event bytes are invalid.'
    }
    if ($eventBytes.Length -ne $journal.eventByteLength -or
        (Get-CISha256BytesHex -Bytes $eventBytes) -cne $journal.eventDigest) {
        throw 'PENDING_APPEND_CORRUPT: pending event integrity is invalid.'
    }
    $eventText = ConvertFrom-CIUtf8Bytes -Bytes $eventBytes
    if ($eventText.Contains([char]13) -or -not $eventText.EndsWith("`n") -or
        $eventText.TrimEnd("`n").Contains("`n")) {
        throw 'PENDING_APPEND_CORRUPT: pending event framing is invalid.'
    }
    $eventDocument = $null
    try {
        $embeddedText = $eventText.TrimEnd("`n")
        $eventDocument = Assert-CIStrictJsonText `
            -Text $embeddedText `
            -Context 'Pending event JSON'
        $embeddedEvent = $embeddedText | ConvertFrom-Json -Depth 20
    }
    catch {
        if ($_.Exception.Message -match 'DUPLICATE_JSON_PROPERTY') {
            throw
        }
        throw 'PENDING_APPEND_CORRUPT: pending event JSON is invalid.'
    }
    finally {
        if ($null -ne $eventDocument) {
            $eventDocument.Dispose()
        }
    }
    if ($embeddedEvent.eventHash -cne $journal.eventHash) {
        throw 'PENDING_APPEND_CORRUPT: pending event hash identity is invalid.'
    }
    return [pscustomobject][ordered]@{
        journal = $journal
        eventBytes = $eventBytes
    }
}

function Read-CIPendingJournal {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$LedgerPath
    )

    Assert-CIRegularFilePath -Path $Path -Label 'pending journal'
    try {
        $stream = [System.IO.File]::Open(
            $Path,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw 'MUTABLE_RESOURCE_COLLISION: the pending journal is locked.'
    }
    try {
        if ($stream.Length -gt $script:CIMaximumLedgerBytes) {
            throw 'PENDING_APPEND_CORRUPT: pending journal is unbounded.'
        }
        $bytes = Read-CIStreamBytes -Stream $stream
    }
    finally {
        $stream.Dispose()
    }
    return ConvertFrom-CIPendingJournalText `
        -Text (ConvertFrom-CIUtf8Bytes -Bytes $bytes) `
        -LedgerPath $LedgerPath
}

function Remove-CISafeSidecar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$ExpectedPath
    )

    $resolved = [System.IO.Path]::GetFullPath($Path)
    if (-not $resolved.Equals(
            [System.IO.Path]::GetFullPath($ExpectedPath),
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'UNSAFE_PENDING_PATH: sidecar cleanup target is not exact.'
    }
    Assert-CIRegularFilePath -Path $resolved -Label 'sidecar cleanup target'
    if ([System.IO.File]::Exists($resolved)) {
        [System.IO.File]::Delete($resolved)
    }
}

function Move-CIStagingJournalToPending {
    param([Parameter(Mandatory)][object]$Paths)

    $hasPending = [System.IO.File]::Exists($Paths.pending) -or
        [System.IO.Directory]::Exists($Paths.pending)
    $hasStaging = [System.IO.File]::Exists($Paths.staging) -or
        [System.IO.Directory]::Exists($Paths.staging)
    if ($hasPending -and $hasStaging) {
        throw 'MUTABLE_RESOURCE_COLLISION: pending and staging journals both exist.'
    }
    if (-not $hasStaging) {
        return
    }
    [void](Read-CIPendingJournal -Path $Paths.staging -LedgerPath $Paths.ledger)
    Assert-CIRegularFilePath -Path $Paths.pending -Label 'pending journal destination'
    [System.IO.File]::Move($Paths.staging, $Paths.pending, $false)
}

function Invoke-CIPendingRecovery {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][object]$Paths,
        [Parameter(Mandatory)][System.IO.FileStream]$LedgerStream,
        [ValidateRange(1, 4096)][int]$MaximumEvents = 512,
        [ValidateRange(1, 16)][int]$MaximumAttempts = 3
    )

    Move-CIStagingJournalToPending -Paths $Paths
    if (-not [System.IO.File]::Exists($Paths.pending)) {
        return [pscustomobject][ordered]@{
            recovered = $false
            disposition = 'NO_PENDING_APPEND'
            event = $null
        }
    }
    $pending = Read-CIPendingJournal -Path $Paths.pending -LedgerPath $Paths.ledger
    $journal = $pending.journal
    $currentBytes = Read-CIStreamBytes -Stream $LedgerStream
    $baseMatches = $currentBytes.Length -eq $journal.baseLength -and
        (Get-CISha256BytesHex -Bytes $currentBytes) -ceq $journal.baseDigest
    $expectedAppliedLength = $journal.baseLength + $journal.eventByteLength
    $alreadyApplied = $false
    $partiallyApplied = $false
    if (-not $baseMatches -and $currentBytes.Length -eq $expectedAppliedLength) {
        $baseBytes = [byte[]]::new([int]$journal.baseLength)
        if ($baseBytes.Length -gt 0) {
            [Array]::Copy($currentBytes, 0, $baseBytes, 0, $baseBytes.Length)
        }
        $eventBytes = [byte[]]::new([int]$journal.eventByteLength)
        [Array]::Copy(
            $currentBytes,
            $baseBytes.Length,
            $eventBytes,
            0,
            $eventBytes.Length)
        $alreadyApplied = (Get-CISha256BytesHex -Bytes $baseBytes) -ceq $journal.baseDigest -and
            (Get-CISha256BytesHex -Bytes $eventBytes) -ceq $journal.eventDigest
    }
    elseif (-not $baseMatches -and
        $currentBytes.Length -gt $journal.baseLength -and
        $currentBytes.Length -lt $expectedAppliedLength) {
        $baseBytes = [byte[]]::new([int]$journal.baseLength)
        if ($baseBytes.Length -gt 0) {
            [Array]::Copy($currentBytes, 0, $baseBytes, 0, $baseBytes.Length)
        }
        $partialLength = [int]($currentBytes.Length - $journal.baseLength)
        $partialBytes = [byte[]]::new($partialLength)
        [Array]::Copy(
            $currentBytes,
            $baseBytes.Length,
            $partialBytes,
            0,
            $partialBytes.Length)
        $expectedPrefix = [byte[]]::new($partialLength)
        [Array]::Copy($pending.eventBytes, 0, $expectedPrefix, 0, $expectedPrefix.Length)
        $partiallyApplied = (Get-CISha256BytesHex -Bytes $baseBytes) -ceq $journal.baseDigest -and
            [System.Linq.Enumerable]::SequenceEqual(
                [byte[]]$partialBytes,
                [byte[]]$expectedPrefix)
    }
    if (-not $baseMatches -and -not $alreadyApplied -and -not $partiallyApplied) {
        throw 'PENDING_APPEND_CORRUPT: ledger state does not match pending base or applied event.'
    }
    if ($baseMatches -or $partiallyApplied) {
        $baseLength = [int]$journal.baseLength
        $combinedBytes = [byte[]]::new($baseLength + $pending.eventBytes.Length)
        if ($baseLength -gt 0) {
            [Array]::Copy($currentBytes, 0, $combinedBytes, 0, $baseLength)
        }
        [Array]::Copy(
            $pending.eventBytes,
            0,
            $combinedBytes,
            $baseLength,
            $pending.eventBytes.Length)
        $events = @(ConvertFrom-CILedgerText `
                -Text (ConvertFrom-CIUtf8Bytes -Bytes $combinedBytes) `
                -MaximumEvents $MaximumEvents `
                -MaximumAttempts $MaximumAttempts)
        if ($partiallyApplied) {
            $LedgerStream.SetLength($baseLength)
            $disposition = 'PENDING_APPEND_TRUNCATED_RECOVERED'
        }
        else {
            $disposition = 'PENDING_APPEND_APPLIED'
        }
        [void]$LedgerStream.Seek($baseLength, [System.IO.SeekOrigin]::Begin)
        $LedgerStream.Write($pending.eventBytes, 0, $pending.eventBytes.Length)
        $LedgerStream.Flush($true)
    }
    else {
        $events = @(ConvertFrom-CILedgerText `
                -Text (ConvertFrom-CIUtf8Bytes -Bytes $currentBytes) `
                -MaximumEvents $MaximumEvents `
                -MaximumAttempts $MaximumAttempts)
        $disposition = 'PENDING_APPEND_ALREADY_APPLIED'
    }
    if ($events.Count -eq 0 -or $events[-1].eventHash -cne $journal.eventHash) {
        throw 'PENDING_APPEND_CORRUPT: recovered event does not close the ledger chain.'
    }
    Remove-CISafeSidecar -Path $Paths.pending -ExpectedPath ($Paths.ledger + '.pending')
    return [pscustomobject][ordered]@{
        recovered = $true
        disposition = $disposition
        event = $events[-1]
    }
}

function Write-CIPendingJournal {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][object]$Paths,
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$BaseBytes,
        [Parameter(Mandatory)][object]$Event
    )

    if ([System.IO.File]::Exists($Paths.pending) -or
        [System.IO.Directory]::Exists($Paths.pending) -or
        [System.IO.File]::Exists($Paths.staging) -or
        [System.IO.Directory]::Exists($Paths.staging)) {
        throw 'MUTABLE_RESOURCE_COLLISION: pending journal path is already occupied.'
    }
    $journal = New-CIPendingJournal `
        -LedgerPath $Paths.ledger `
        -BaseBytes $BaseBytes `
        -Event $Event
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes(
        (($journal | ConvertTo-Json -Depth 12 -Compress) + "`n"))
    Assert-CIRegularFilePath -Path $Paths.staging -Label 'pending staging journal'
    $stream = [System.IO.File]::Open(
        $Paths.staging,
        [System.IO.FileMode]::CreateNew,
        [System.IO.FileAccess]::Write,
        [System.IO.FileShare]::None)
    try {
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush($true)
    }
    finally {
        $stream.Dispose()
    }
    [void](Read-CIPendingJournal -Path $Paths.staging -LedgerPath $Paths.ledger)
    [System.IO.File]::Move($Paths.staging, $Paths.pending, $false)
    return $journal
}

function Repair-CILedgerPendingAppend {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$AuthorisedRoot,
        [ValidateRange(1, 4096)][int]$MaximumEvents = 512,
        [ValidateRange(1, 16)][int]$MaximumAttempts = 3
    )

    $paths = Get-CILedgerPathSet `
        -Path $Path `
        -AuthorisedRoot $AuthorisedRoot `
        -CreateDirectory
    try {
        $stream = [System.IO.File]::Open(
            $paths.ledger,
            [System.IO.FileMode]::OpenOrCreate,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw 'MUTABLE_RESOURCE_COLLISION: the continuous-improvement ledger is locked.'
    }
    try {
        return Invoke-CIPendingRecovery `
            -Paths $paths `
            -LedgerStream $stream `
            -MaximumEvents $MaximumEvents `
            -MaximumAttempts $MaximumAttempts
    }
    finally {
        $stream.Dispose()
    }
}

function Read-CILedger {
    <#
    .SYNOPSIS
    Reads and validates one bounded ledger without modifying it.
    #>
    [OutputType([object[]])]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$AuthorisedRoot,

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    $paths = Get-CILedgerPathSet -Path $Path -AuthorisedRoot $AuthorisedRoot
    if (-not [System.IO.File]::Exists($paths.ledger)) {
        if ([System.IO.File]::Exists($paths.pending) -or
            [System.IO.File]::Exists($paths.staging)) {
            throw 'PENDING_APPEND_RECOVERY_REQUIRED: recover the pending append before reading.'
        }
        return @()
    }
    try {
        $stream = [System.IO.File]::Open(
            $paths.ledger,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw 'MUTABLE_RESOURCE_COLLISION: the continuous-improvement ledger is locked.'
    }
    try {
        if ([System.IO.File]::Exists($paths.pending) -or
            [System.IO.File]::Exists($paths.staging)) {
            throw 'PENDING_APPEND_RECOVERY_REQUIRED: recover the pending append before reading.'
        }
        $bytes = Read-CIStreamBytes -Stream $stream
    }
    finally {
        $stream.Dispose()
    }
    return @(ConvertFrom-CILedgerText `
            -Text (ConvertFrom-CIUtf8Bytes -Bytes $bytes) `
            -MaximumEvents $MaximumEvents `
            -MaximumAttempts $MaximumAttempts)
}

function Add-CILedgerEvent {
    <#
    .SYNOPSIS
    Appends exactly one validated event while holding an exclusive file lock.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$AuthorisedRoot,

        [Parameter(Mandatory)]
        [object]$EventData,

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3,

        [ValidateSet('None', 'AfterPendingFlush', 'AfterLedgerFlush')]
        [string]$TestFaultPoint = 'None'
    )

    $paths = Get-CILedgerPathSet `
        -Path $Path `
        -AuthorisedRoot $AuthorisedRoot `
        -CreateDirectory
    try {
        $stream = [System.IO.File]::Open(
            $paths.ledger,
            [System.IO.FileMode]::OpenOrCreate,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
    }
    catch [System.IO.IOException] {
        throw 'MUTABLE_RESOURCE_COLLISION: the continuous-improvement ledger is locked.'
    }
    try {
        $recovery = Invoke-CIPendingRecovery `
            -Paths $paths `
            -LedgerStream $stream `
            -MaximumEvents $MaximumEvents `
            -MaximumAttempts $MaximumAttempts
        $baseBytes = Read-CIStreamBytes -Stream $stream
        $text = ConvertFrom-CIUtf8Bytes -Bytes $baseBytes
        $events = @(ConvertFrom-CILedgerText `
                -Text $text `
                -MaximumEvents $MaximumEvents `
                -MaximumAttempts $MaximumAttempts)
        if ($recovery.recovered) {
            if ($events.Count -eq 0 -or
                $events[-1].eventHash -cne $recovery.event.eventHash) {
                throw 'PENDING_APPEND_CORRUPT: recovered event is not the ledger tail.'
            }
            $priorEvents = @()
            if ($events.Count -gt 1) {
                $priorEvents = @($events[0..($events.Count - 2)])
            }
            $requestedRecoveryEvent = $null
            try {
                $requestedRecoveryEvent = New-CILedgerEvent `
                    -EventData $EventData `
                    -Sequence $recovery.event.sequence `
                    -PreviousEventHash $recovery.event.previousEventHash `
                    -PriorEvents $priorEvents
            }
            catch {
                $requestedRecoveryEvent = $null
            }
            if ($null -ne $requestedRecoveryEvent -and
                $requestedRecoveryEvent.eventHash -ceq $recovery.event.eventHash) {
                return $recovery.event
            }
        }
        if ($events.Count -ge $MaximumEvents) {
            throw "LEDGER_BOUND_EXCEEDED: no event can be appended beyond $MaximumEvents."
        }
        $previousHash = if ($events.Count -eq 0) { '0' * 64 } else { $events[-1].eventHash }
        $event = New-CILedgerEvent `
            -EventData $EventData `
            -Sequence ($events.Count + 1) `
            -PreviousEventHash $previousHash `
            -PriorEvents $events
        Assert-CILedgerEvents `
            -Events @($events + $event) `
            -MaximumEvents $MaximumEvents `
            -MaximumAttempts $MaximumAttempts
        [void](Write-CIPendingJournal `
                -Paths $paths `
                -BaseBytes $baseBytes `
                -Event $event)
        if ($TestFaultPoint -ceq 'AfterPendingFlush') {
            throw 'TEST_CRASH_AFTER_PENDING_FLUSH'
        }
        $pending = Read-CIPendingJournal `
            -Path $paths.pending `
            -LedgerPath $paths.ledger
        [void]$stream.Seek(0, [System.IO.SeekOrigin]::End)
        $stream.Write($pending.eventBytes, 0, $pending.eventBytes.Length)
        $stream.Flush($true)
        if ($TestFaultPoint -ceq 'AfterLedgerFlush') {
            throw 'TEST_CRASH_AFTER_LEDGER_FLUSH'
        }
        Remove-CISafeSidecar `
            -Path $paths.pending `
            -ExpectedPath ($paths.ledger + '.pending')
        return $event
    }
    finally {
        $stream.Dispose()
    }
}

function Get-CIAttemptAdmission {
    <#
    .SYNOPSIS
    Decides whether one candidate may start without retrying unchanged work.
    #>
    [OutputType([pscustomobject])]
    param(
        [AllowEmptyCollection()]
        [object[]]$Events = @(),

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

        [object]$CausalDeltaDigest,

        [object]$CausalDeltaFacts,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    Assert-CILedgerEvents `
        -Events $Events `
        -MaximumEvents ([Math]::Max(1, $Events.Count)) `
        -MaximumAttempts $MaximumAttempts
    $improvementEvents = @($Events | Where-Object {
            $_.improvementId -ceq $ImprovementId -and
            $_.eventType -cne 'DUPLICATE_SUPPRESSED'
        })
    $attempts = @($improvementEvents | Where-Object {
            $_.eventType -ceq 'ATTEMPT_STARTED'
        })
    $rejection = {
        param([string]$ReasonCode)
        return [pscustomobject][ordered]@{
            disposition = 'QUARANTINE'
            reasonCode = $ReasonCode
            attempts = $attempts.Count
            causalDeltaDigest = $null
            causalFactReceiptDigests = @()
            causalDeltaFacts = $null
        }
    }
    if ($null -ne $CausalDeltaDigest -and
        -not [string]::IsNullOrWhiteSpace([string]$CausalDeltaDigest)) {
        return & $rejection 'CALLER_SUPPLIED_CAUSAL_DELTA'
    }
    if ($improvementEvents.Count -eq 0) {
        return & $rejection 'ATTEMPT_PREDECESSOR_INVALID'
    }
    $latest = $improvementEvents[-1]
    try {
        $expectedExecutionKey = New-CIExecutionKey `
            -ImprovementId $ImprovementId `
            -Baseline $latest.baseline `
            -ScopeDigest $ExecutionScopeDigest `
            -AcceptanceDigest $AcceptanceDigest
    }
    catch {
        return & $rejection 'EXECUTION_KEY_IDENTITY_INVALID'
    }
    if ($ExecutionKey -cne $expectedExecutionKey) {
        return & $rejection 'EXECUTION_KEY_IDENTITY_INVALID'
    }
    if (($attempts.Count -eq 0 -and $latest.eventType -cne 'TRIAGED') -or
        ($attempts.Count -gt 0 -and $latest.eventType -cne 'GATE_FAILED')) {
        return & $rejection 'ATTEMPT_PREDECESSOR_INVALID'
    }
    if ($attempts.Count -gt 0 -and $ExecutionKey -cne $attempts[0].executionKey) {
        return & $rejection 'EXECUTION_KEY_DRIFT'
    }
    if (@($attempts | Where-Object { $_.candidateDigest -ceq $CandidateDigest }).Count -gt 0) {
        return & $rejection 'SAME_CANDIDATE_RETRY'
    }
    if ($attempts.Count -ge $MaximumAttempts) {
        return & $rejection 'ATTEMPT_BUDGET_EXHAUSTED'
    }
    $causalReceipt = $null
    if ($attempts.Count -gt 0) {
        try {
            if ($null -eq $CausalDeltaFacts) {
                throw 'CAUSAL_DELTA_REQUIRED'
            }
            $causalReceipt = New-CICausalDeltaReceipt `
                -Facts $CausalDeltaFacts `
                -CandidateDigest $CandidateDigest `
                -PriorEvents $Events `
                -ImprovementId $ImprovementId `
                -ExecutionKey $ExecutionKey
        }
        catch {
            return & $rejection 'CAUSAL_DELTA_REQUIRED'
        }
        if (@($attempts | Where-Object {
                    $_.causalDeltaDigest -ceq $causalReceipt.digest
                }).Count -gt 0) {
            return & $rejection 'CAUSAL_DELTA_REQUIRED'
        }
    }
    elseif ($null -ne $CausalDeltaFacts) {
        return & $rejection 'CAUSAL_DELTA_NOT_APPLICABLE'
    }
    return [pscustomobject][ordered]@{
        disposition = 'ADMIT'
        reasonCode = if ($attempts.Count -eq 0) {
            'INITIAL_ATTEMPT_ACCEPTED'
        }
        else {
            'CAUSAL_DELTA_ACCEPTED'
        }
        attempts = $attempts.Count
        causalDeltaDigest = if ($null -eq $causalReceipt) { $null } else { $causalReceipt.digest }
        causalFactReceiptDigests = if ($null -eq $causalReceipt) { @() } else { @($causalReceipt.receiptDigests) }
        causalDeltaFacts = if ($null -eq $causalReceipt) { $null } else { $causalReceipt.facts }
    }
}

function Get-CINextDecision {
    <#
    .SYNOPSIS
    Returns one bounded event-driven continuation without polling or executing work.
    #>
    [OutputType([pscustomobject])]
    param(
        [AllowEmptyCollection()]
        [object[]]$Events = @(),

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    Assert-CILedgerEvents `
        -Events $Events `
        -MaximumEvents $MaximumEvents `
        -MaximumAttempts $MaximumAttempts
    $open = @($Events | Where-Object { $_.eventType -cne 'DUPLICATE_SUPPRESSED' } |
        Group-Object -Property improvementId |
        ForEach-Object {
            $latest = $_.Group[-1]
            if ($latest.eventType -cnotin @(
                    'CLOSED',
                    'QUARANTINED',
                    'EXTERNAL_PREREQUISITE_RECORDED',
                    'AUTHORITY_BLOCKED')) {
                [pscustomobject]@{
                    firstSequence = $_.Group[0].sequence
                    latest = $latest
                    events = @($_.Group)
                    attempts = @($_.Group | Where-Object {
                            $_.eventType -ceq 'ATTEMPT_STARTED'
                        }).Count
                }
            }
        } | Sort-Object -Property firstSequence)
    if ($open.Count -eq 0) {
        return [pscustomobject][ordered]@{
            action = 'WAIT_FOR_EVENT'
            route = 'CONTINUE_CURRENT'
            improvementId = $null
            reasonCode = 'NO_OPEN_IMPROVEMENT'
            eventsRead = $Events.Count
            maximumEvents = $MaximumEvents
        }
    }
    $selected = $open[0]
    $action = switch ($selected.latest.eventType) {
        'FINDING_DETECTED' { 'TRIAGE' }
        'TRIAGED' { 'IMPLEMENT' }
        'ATTEMPT_STARTED' { 'REVIEW' }
        'REVIEW_RECORDED' {
            $latestAttempt = @($selected.events | Where-Object {
                    $_.eventType -ceq 'ATTEMPT_STARTED'
                } | Select-Object -Last 1)[0]
            $reviews = @($selected.events | Where-Object {
                    $_.eventType -ceq 'REVIEW_RECORDED' -and
                    $_.sequence -gt $latestAttempt.sequence
                })
            $minimumReviewers = if (
                $latestAttempt.governanceChange -or
                $latestAttempt.riskClass -ceq 'HIGH') { 2 } else { 1 }
            if ($reviews.Count -lt $minimumReviewers) { 'REVIEW' } else { 'VERIFY' }
        }
        'GATE_FAILED' {
            if ($selected.attempts -ge $MaximumAttempts) { 'QUARANTINE' }
            else { 'ROOT_CAUSE_ANALYSIS' }
        }
        'GATE_PASSED' { 'PROMOTE' }
        'PROMOTED' { 'OBSERVE' }
        'OBSERVATION_PASSED' { 'CLOSE' }
        'OBSERVATION_FAILED' { 'ROLLBACK' }
        'ROLLBACK_REQUIRED' { 'ROLLBACK' }
        'ROLLED_BACK' { 'CLOSE' }
        default { 'QUARANTINE' }
    }
    return [pscustomobject][ordered]@{
        action = $action
        route = 'CONTINUE_CURRENT'
        improvementId = $selected.latest.improvementId
        reasonCode = "EVENT_$($selected.latest.eventType)"
        eventsRead = $Events.Count
        maximumEvents = $MaximumEvents
    }
}

function New-CIRate {
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [int]$Numerator,

        [Parameter(Mandatory)]
        [int]$Denominator
        ,
        [Parameter(Mandatory)][string]$MetricId,
        [Parameter(Mandatory)][ValidateSet('HIGHER_IS_BETTER', 'LOWER_IS_BETTER')]
        [string]$Direction,
        [Parameter(Mandatory)][object]$Source,
        [Parameter(Mandatory)][object]$Window
    )

    return [pscustomobject][ordered]@{
        profile = $script:CIMetricProfile
        profileVersion = $script:CIMetricProfileVersion
        metricId = $MetricId
        source = $Source
        window = $Window
        direction = $Direction
        unit = 'RATIO'
        numerator = $Numerator
        denominator = $Denominator
        ratio = if ($Denominator -eq 0) {
            $null
        }
        else {
            [Math]::Round(($Numerator / $Denominator), 6)
        }
    }
}

function Get-CIMetrics {
    <#
    .SYNOPSIS
    Derives anti-gaming metrics exclusively from validated immutable events.
    #>
    [OutputType([pscustomobject])]
    param(
        [AllowEmptyCollection()]
        [object[]]$Events = @(),

        [ValidateRange(1, 4096)]
        [int]$MaximumEvents = 512,

        [ValidateRange(1, 16)]
        [int]$MaximumAttempts = 3
    )

    Assert-CILedgerEvents `
        -Events $Events `
        -MaximumEvents $MaximumEvents `
        -MaximumAttempts $MaximumAttempts
    $groups = @($Events | Where-Object { $_.eventType -cne 'DUPLICATE_SUPPRESSED' } |
        Group-Object -Property improvementId)
    $firstPassCount = 0
    $firstPassDenominator = 0
    foreach ($group in $groups) {
        $firstGate = @($group.Group | Where-Object {
                $_.eventType -in @('GATE_FAILED', 'GATE_PASSED')
            } | Select-Object -First 1)
        if ($firstGate.Count -eq 1) {
            $firstPassDenominator++
            if ($firstGate[0].eventType -ceq 'GATE_PASSED') {
                $firstPassCount++
            }
        }
    }
    $promotions = @($Events | Where-Object { $_.eventType -ceq 'PROMOTED' })
    $observations = @($Events | Where-Object {
            $_.eventType -in @('OBSERVATION_PASSED', 'OBSERVATION_FAILED') -and
            $_.observationWindowClosed
        })
    $observationFailures = @($observations | Where-Object {
            $_.eventType -ceq 'OBSERVATION_FAILED'
        })
    $source = [pscustomobject][ordered]@{
        kind = 'VALIDATED_APPEND_ONLY_LEDGER'
        eventCount = $Events.Count
        ledgerHeadHash = if ($Events.Count -eq 0) { '0' * 64 } else { $Events[-1].eventHash }
    }
    $window = [pscustomobject][ordered]@{
        startSequence = if ($Events.Count -eq 0) { $null } else { $Events[0].sequence }
        endSequence = if ($Events.Count -eq 0) { $null } else { $Events[-1].sequence }
        startUtc = if ($Events.Count -eq 0) { $null } else { $Events[0].recordedAtUtc }
        endUtc = if ($Events.Count -eq 0) { $null } else { $Events[-1].recordedAtUtc }
    }
    return [pscustomobject][ordered]@{
        schemaVersion = 1
        profile = $script:CIMetricProfile
        profileVersion = $script:CIMetricProfileVersion
        source = $source
        window = $window
        events = $Events.Count
        uniqueFindings = @($Events | Where-Object {
                $_.eventType -ceq 'FINDING_DETECTED'
            }).Count
        duplicateSuppressions = @($Events | Where-Object {
                $_.eventType -ceq 'DUPLICATE_SUPPRESSED'
            }).Count
        attempts = @($Events | Where-Object {
                $_.eventType -ceq 'ATTEMPT_STARTED'
            }).Count
        quarantines = @($Events | Where-Object {
                $_.eventType -ceq 'QUARANTINED'
            }).Count
        promotions = $promotions.Count
        observedPromotions = $observations.Count
        rollbacks = @($Events | Where-Object {
                $_.eventType -ceq 'ROLLED_BACK'
            }).Count
        firstPassRate = New-CIRate `
            -Numerator $firstPassCount `
            -Denominator $firstPassDenominator `
            -MetricId 'FIRST_PASS_RATE' `
            -Direction 'HIGHER_IS_BETTER' `
            -Source $source `
            -Window $window
        duplicateRate = New-CIRate `
            -Numerator @($Events | Where-Object {
                    $_.eventType -ceq 'DUPLICATE_SUPPRESSED'
                }).Count `
            -Denominator @($Events | Where-Object {
                    $_.eventType -in @('FINDING_DETECTED', 'DUPLICATE_SUPPRESSED')
                }).Count `
            -MetricId 'DUPLICATE_RATE' `
            -Direction 'LOWER_IS_BETTER' `
            -Source $source `
            -Window $window
        escapeRate = New-CIRate `
            -Numerator $observationFailures.Count `
            -Denominator $observations.Count `
            -MetricId 'ESCAPE_RATE' `
            -Direction 'LOWER_IS_BETTER' `
            -Source $source `
            -Window $window
        observationCoverage = New-CIRate `
            -Numerator $observations.Count `
            -Denominator $promotions.Count `
            -MetricId 'OBSERVATION_COVERAGE' `
            -Direction 'HIGHER_IS_BETTER' `
            -Source $source `
            -Window $window
    }
}

function Assert-CIExactCandidate {
    <#
    .SYNOPSIS
    Proves that one isolated worktree contains exactly the expected candidate paths.
    #>
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory)]
        [string]$ExpectedBaseline,

        [Parameter(Mandatory)]
        [string[]]$ExpectedPaths,

        [switch]$RequireIndexEmpty
    )

    $root = [System.IO.Path]::GetFullPath(
        (Resolve-Path -LiteralPath $RepositoryRoot).Path)
    Assert-CINoReparsePoint -Path $root
    $gitPath = (Get-Command git -CommandType Application -ErrorAction Stop |
            Select-Object -First 1).Source
    $reportedRoot = ((& $gitPath -C $root rev-parse --show-toplevel) -join '').Trim()
    if ($LASTEXITCODE -ne 0 -or
        -not [System.IO.Path]::GetFullPath($reportedRoot).Equals(
            $root,
            $(if ($IsWindows) {
                    [System.StringComparison]::OrdinalIgnoreCase
                }
                else {
                    [System.StringComparison]::Ordinal
                }))) {
        throw 'ISOLATION_FAILURE: candidate validation did not resolve the exact worktree root.'
    }
    $head = ((& $gitPath -C $root rev-parse HEAD) -join '').Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $head -cne $ExpectedBaseline.ToLowerInvariant()) {
        throw 'BASELINE_DRIFT: candidate validation observed a different HEAD.'
    }
    $expected = @(ConvertTo-CINormalisedPaths -Paths $ExpectedPaths)
    $expectedPathByNormalised = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($expectedPath in $ExpectedPaths) {
        $normalisedExpected = @(ConvertTo-CINormalisedPaths -Paths @($expectedPath))
        if ($normalisedExpected.Count -ne 1 -or
            $expectedPathByNormalised.ContainsKey($normalisedExpected[0])) {
            throw 'Candidate validation received a duplicate or malformed expected path.'
        }
        $expectedPathByNormalised.Add(
            $normalisedExpected[0],
            ([string]$expectedPath).Trim().Replace('\', '/'))
    }
    $tracked = @(& $gitPath -C $root diff HEAD --name-only --no-ext-diff --)
    if ($LASTEXITCODE -ne 0) {
        throw 'Candidate validation could not enumerate tracked changes.'
    }
    $untracked = @(& $gitPath -C $root ls-files --others --exclude-standard --)
    if ($LASTEXITCODE -ne 0) {
        throw 'Candidate validation could not enumerate non-ignored candidate paths.'
    }
    $actualPathByNormalised = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::Ordinal)
    foreach ($reportedPath in @($tracked + $untracked)) {
        $normalisedReportedPath = @(ConvertTo-CINormalisedPaths -Paths @($reportedPath))
        if ($normalisedReportedPath.Count -ne 1 -or
            $actualPathByNormalised.ContainsKey($normalisedReportedPath[0])) {
            throw 'Candidate validation observed a duplicate or malformed changed path.'
        }
        $actualPathByNormalised.Add(
            $normalisedReportedPath[0],
            $reportedPath.Replace('\', '/'))
        if ($expectedPathByNormalised.ContainsKey($normalisedReportedPath[0]) -and
            $expectedPathByNormalised[$normalisedReportedPath[0]] -cne
                $reportedPath.Replace('\', '/')) {
            throw 'CANDIDATE_PATH_CASING_MISMATCH: expected scope casing differs from Git.'
        }
    }
    $actual = @($actualPathByNormalised.Keys | Sort-Object -CaseSensitive)
    if (Compare-Object -ReferenceObject $expected -DifferenceObject $actual -SyncWindow 0) {
        throw 'SCOPE_OVERLAP: the isolated worktree does not contain the exact candidate path set.'
    }
    $staged = @(& $gitPath -C $root diff --cached --name-only --)
    if ($LASTEXITCODE -ne 0) {
        throw 'Candidate validation could not enumerate the index.'
    }
    if ($RequireIndexEmpty -and $staged.Count -ne 0) {
        throw 'SCOPE_OVERLAP: candidate validation requires an empty index.'
    }
    $contentLines = [System.Collections.Generic.List[string]]::new()
    foreach ($relativePath in $expected) {
        $reportedPath = $actualPathByNormalised[$relativePath]
        $absolutePath = Assert-CIPathContained `
            -Path (Join-Path $root $reportedPath) `
            -Root $root `
            -Label "candidate path '$relativePath'"
        if (-not [System.IO.File]::Exists($absolutePath)) {
            throw "Candidate path '$relativePath' is not a regular file."
        }
        Assert-CIRegularFilePath -Path $absolutePath -Label "candidate path '$relativePath'"
        $resolvedCandidate = [System.IO.Path]::GetFullPath(
            (Resolve-Path -LiteralPath $absolutePath).Path)
        [void](Assert-CIPathContained `
                -Path $resolvedCandidate `
                -Root $root `
                -Label "resolved candidate path '$relativePath'")
        $digest = (Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash.ToLowerInvariant()
        $contentLines.Add("$digest  $relativePath")
    }
    $classification = New-CICandidateClassification -Paths $actual
    return [pscustomobject][ordered]@{
        baseline = $head
        pathCount = $actual.Count
        indexCount = $staged.Count
        candidatePaths = @($actual | ForEach-Object {
                $actualPathByNormalised[$_]
            })
        candidateDigest = Get-CISha256Hex -Text (($contentLines -join "`n") + "`n")
        candidateScopeDigest = $classification.candidateScopeDigest
        riskClass = $classification.riskClass
        governanceChange = $classification.governanceChange
        riskDomains = @($classification.riskDomains)
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
        $RepositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    }
    if ($Command -in @('AppendEvent', 'RecoverLedger', 'ValidateLedger', 'NextDecision', 'Metrics') -and
        [string]::IsNullOrWhiteSpace($LedgerPath)) {
        throw "$Command requires an explicit -LedgerPath owned by the current task."
    }
    $authorisedLedgerRoot = Join-Path $RepositoryRoot '.dotnet/continuous-improvement'
    switch ($Command) {
        'None' {
            Write-Output 'Continuous-improvement controller loaded; select an explicit command.'
        }
        'Fingerprint' {
            Write-Output (New-CIStableFindingFingerprint `
                    -RuleId $RuleId `
                    -RootCause $RootCause `
                    -Paths $FingerprintPath)
        }
        'AppendEvent' {
            if ([string]::IsNullOrWhiteSpace($InputPath)) {
                throw 'AppendEvent requires -InputPath.'
            }
            Assert-CIRegularFilePath -Path $InputPath -Label 'event input'
            $eventText = ConvertFrom-CIUtf8Bytes -Bytes (
                [System.IO.File]::ReadAllBytes(
                    [System.IO.Path]::GetFullPath($InputPath)))
            $eventDocument = Assert-CIStrictJsonText `
                -Text $eventText `
                -Context 'Event input JSON'
            try {
                $eventData = $eventText | ConvertFrom-Json -AsHashtable -Depth 20
            }
            finally {
                $eventDocument.Dispose()
            }
            Add-CILedgerEvent `
                -Path $LedgerPath `
                -AuthorisedRoot $authorisedLedgerRoot `
                -EventData $eventData `
                -MaximumEvents $MaximumLedgerEvents `
                -MaximumAttempts $MaximumAttempts |
                ConvertTo-Json -Depth 12 -Compress
        }
        'RecoverLedger' {
            Repair-CILedgerPendingAppend `
                -Path $LedgerPath `
                -AuthorisedRoot $authorisedLedgerRoot `
                -MaximumEvents $MaximumLedgerEvents `
                -MaximumAttempts $MaximumAttempts |
                ConvertTo-Json -Depth 12 -Compress
        }
        'ValidateLedger' {
            $events = @(Read-CILedger `
                    -Path $LedgerPath `
                    -AuthorisedRoot $authorisedLedgerRoot `
                    -MaximumEvents $MaximumLedgerEvents `
                    -MaximumAttempts $MaximumAttempts)
            Write-Output "PASS|continuous-improvement-ledger|events=$($events.Count)"
        }
        'NextDecision' {
            $events = @(Read-CILedger `
                    -Path $LedgerPath `
                    -AuthorisedRoot $authorisedLedgerRoot `
                    -MaximumEvents $MaximumLedgerEvents `
                    -MaximumAttempts $MaximumAttempts)
            Get-CINextDecision `
                -Events $events `
                -MaximumEvents $MaximumLedgerEvents `
                -MaximumAttempts $MaximumAttempts |
                ConvertTo-Json -Depth 8 -Compress
        }
        'Metrics' {
            $events = @(Read-CILedger `
                    -Path $LedgerPath `
                    -AuthorisedRoot $authorisedLedgerRoot `
                    -MaximumEvents $MaximumLedgerEvents `
                    -MaximumAttempts $MaximumAttempts)
            Get-CIMetrics `
                -Events $events `
                -MaximumEvents $MaximumLedgerEvents `
                -MaximumAttempts $MaximumAttempts |
                ConvertTo-Json -Depth 8 -Compress
        }
        'ValidateCandidate' {
            Assert-CIExactCandidate `
                -RepositoryRoot $RepositoryRoot `
                -ExpectedBaseline $ExpectedBaseline `
                -ExpectedPaths $ExpectedPath `
                -RequireIndexEmpty:$RequireIndexEmpty |
                ConvertTo-Json -Depth 8 -Compress
        }
    }
}
