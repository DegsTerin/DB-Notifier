// Module purpose: Verifies the trusted MOD-12 canonical telemetry adapter, fail-closed data opt-in policy and governed offline evaluation boundary.
using System.Security.Cryptography;
using DBNotifier.Application.AIOps;
using DBNotifier.Domain;

namespace DBNotifier.UnitTests;

/// <summary>Protects the authorised STATE-06 MOD-12 increment without activating a runtime mode or external dependency.</summary>
public sealed class AIOpsIntegrationTests
{
    private static readonly Guid InstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherInstanceId = Guid.Parse("11111111-1111-1111-1111-111111111112");
    private static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherAgentId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    private static readonly Guid ScopeId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Start = new(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Confirms explicit opt-in, exact source binding and immutable non-collection/non-persistence restrictions.</summary>
    [Fact]
    public void DataPolicyFailsClosedAndValidatesPurposeSpecificScope()
    {
        ObserverDataScope source = new(InstanceId, AgentId);
        ObserverDataPolicy disabled = DataPolicy(ObserverDataOptInState.Disabled, [source]);

        Assert.Equal(ObserverDataOptInState.Disabled, disabled.OptInState);
        Assert.Equal(ObserverDataUse.RuntimeAnalysis, disabled.DataUse);
        Assert.Equal(ObserverDataClassification.OperationalTelemetry, disabled.Classification);
        Assert.Equal(ObserverRedactionStatus.Sanitised, disabled.RedactionStatus);
        Assert.Equal(ObserverRetentionClass.EphemeralAnalysisOnly, disabled.RetentionClass);
        Assert.Equal(ObserverPermittedPurpose.NonMutatingObserverAnalysis, disabled.PermittedPurpose);
        Assert.False(disabled.AllowsCollection);
        Assert.False(disabled.AllowsPersistence);
        Assert.False(disabled.ActivatesObserverMode);
        Assert.Throws<ArgumentException>(() => DataPolicy(ObserverDataOptInState.Enabled, []));
        Assert.Throws<ArgumentException>(() => new ObserverDataPolicy(
            "policy.runtime",
            "1.0.0",
            ObserverDataOptInState.Enabled,
            ObserverDataUse.RuntimeAnalysis,
            ScopeId,
            Start.AddHours(-1),
            Start.AddHours(1),
            [source],
            allowSyntheticEvidence: true));
        Assert.Throws<ArgumentException>(() => DataPolicy(
            ObserverDataOptInState.Enabled,
            [source, new ObserverDataScope(InstanceId, AgentId)]));
    }

    /// <summary>Verifies that the adapter derives all handling fields and copies only provider-neutral duration evidence.</summary>
    [Fact]
    public void AdapterDerivesSanitisedEvidenceFromCanonicalObservation()
    {
        ObserverDataPolicy policy = DataPolicy(ObserverDataOptInState.Enabled, [new(InstanceId, AgentId)]);
        ObserverCanonicalHealthTelemetry telemetry = Telemetry(
            1,
            durationMilliseconds: 125,
            observedAt: Start.AddMinutes(-1),
            evidenceLevel: EvidenceLevel.ProviderAuthenticated,
            status: HealthStatus.Degraded,
            error: new NormalizedError(
                "probe.fixture",
                ErrorCategory.Provider,
                Retryability.Backoff,
                "Sanitised diagnostic that must not cross the adapter."));

        PolicyEvidence evidence = CreatePolicyEvidence(policy);
        ObserverTelemetryAdaptationResult result = CanonicalObserverTelemetryAdapter.Adapt(
            telemetry,
            evidence.Context,
            evidence.TrustConfiguration,
            Start);

        Assert.Equal(ObserverTelemetryAdaptationDisposition.Accepted, result.Disposition);
        Assert.Equal("aiops.observer.adapter.accepted", result.Code);
        ObserverMetricSample sample = Assert.IsType<ObserverMetricSample>(result.Sample);
        Assert.Equal(telemetry.Observation.ObservationId, sample.EvidenceId);
        Assert.Equal(InstanceId, sample.InstanceId);
        Assert.Equal(ScopeId, sample.AuthorisationScopeId);
        Assert.Equal(CanonicalObserverTelemetryAdapter.MetricKey, sample.MetricKey);
        Assert.Equal(CanonicalObserverTelemetryAdapter.Unit, sample.Unit);
        Assert.Equal(125, sample.Value);
        Assert.Equal(ObserverEvidenceQuality.Verified, sample.Quality);
        Assert.Equal("canonical.health-observation", sample.SourceKind);
        Assert.Equal("health-observation.v1", sample.SchemaVersion);
        Assert.Equal("observer-duration.v1", sample.TransformationVersion);
        Assert.DoesNotContain("fixture", sample.SourceKind, StringComparison.Ordinal);
        Assert.DoesNotContain("probe", sample.SourceKind, StringComparison.Ordinal);
    }

    /// <summary>Verifies sanitised fail-closed outcomes for disabled, unauthorised, future and weak evidence.</summary>
    /// <param name="scenario">Trust-boundary failure scenario.</param>
    /// <param name="expectedCode">Expected stable rejection code.</param>
    [Theory]
    [InlineData("disabled", "aiops.observer.adapter.policy_disabled")]
    [InlineData("offline-purpose", "aiops.observer.adapter.policy_purpose_mismatch")]
    [InlineData("not-effective", "aiops.observer.adapter.policy_not_effective")]
    [InlineData("expired", "aiops.observer.adapter.policy_expired")]
    [InlineData("source", "aiops.observer.adapter.source_denied")]
    [InlineData("future", "aiops.observer.adapter.timestamp_invalid")]
    [InlineData("reversed", "aiops.observer.adapter.timestamp_invalid")]
    [InlineData("non-utc", "aiops.observer.adapter.timestamp_invalid")]
    [InlineData("pre-policy", "aiops.observer.adapter.evidence_outside_policy_period")]
    [InlineData("unknown", "aiops.observer.adapter.evidence_unknown")]
    [InlineData("synthetic", "aiops.observer.adapter.synthetic_denied")]
    [InlineData("healthy-transport", "aiops.observer.adapter.contract_invalid")]
    [InlineData("excessive-duration", "aiops.observer.adapter.contract_invalid")]
    public void AdapterRejectsUntrustedTelemetry(string scenario, string expectedCode)
    {
        ObserverDataPolicy policy = scenario switch
        {
            "disabled" => DataPolicy(ObserverDataOptInState.Disabled, [new(InstanceId, AgentId)]),
            "offline-purpose" => OfflinePolicy(
                "policy.offline.wrong-entry-point",
                ObserverDataOptInState.Enabled,
                [new ObserverDataScope(InstanceId, AgentId)]),
            "not-effective" => new ObserverDataPolicy(
                "policy.future",
                "1.0.0",
                ObserverDataOptInState.Enabled,
                ObserverDataUse.RuntimeAnalysis,
                ScopeId,
                Start.AddMinutes(1),
                Start.AddHours(1),
                [new ObserverDataScope(InstanceId, AgentId)]),
            "expired" => new ObserverDataPolicy(
                "policy.expired",
                "1.0.0",
                ObserverDataOptInState.Enabled,
                ObserverDataUse.RuntimeAnalysis,
                ScopeId,
                Start.AddHours(-1),
                Start,
                [new ObserverDataScope(InstanceId, AgentId)]),
            "source" => DataPolicy(ObserverDataOptInState.Enabled, [new(OtherInstanceId, OtherAgentId)]),
            _ => DataPolicy(ObserverDataOptInState.Enabled, [new(InstanceId, AgentId)]),
        };
        ObserverCanonicalHealthTelemetry telemetry = scenario switch
        {
            "future" => Telemetry(2, 10, Start.AddMinutes(1)),
            "reversed" => Telemetry(2, 10, Start, receivedAt: Start.AddMinutes(-1)),
            "non-utc" => Telemetry(
                2,
                10,
                new DateTimeOffset(2026, 7, 17, 13, 0, 0, TimeSpan.FromHours(1))),
            "pre-policy" => Telemetry(2, 10, Start.AddHours(-2)),
            "unknown" => Telemetry(2, 10, Start, EvidenceLevel.Unknown),
            "synthetic" => Telemetry(2, 10, Start, EvidenceLevel.Synthetic),
            "healthy-transport" => Telemetry(
                2,
                10,
                Start,
                EvidenceLevel.TransportOnly,
                HealthStatus.Healthy),
            "excessive-duration" => Telemetry(2, TimeSpan.FromMinutes(6).TotalMilliseconds, Start),
            _ => Telemetry(2, 10, Start),
        };

        PolicyEvidence evidence = CreatePolicyEvidence(policy);
        ObserverTelemetryAdaptationResult result = CanonicalObserverTelemetryAdapter.Adapt(
            telemetry,
            evidence.Context,
            evidence.TrustConfiguration,
            Start);

        Assert.Equal(ObserverTelemetryAdaptationDisposition.Rejected, result.Disposition);
        Assert.Equal(expectedCode, result.Code);
        Assert.Null(result.Sample);
    }

    /// <summary>Verifies fail-closed separation of provenance identity, integrity, revocation and freshness.</summary>
    /// <param name="scenario">Authenticated-provenance failure scenario.</param>
    /// <param name="expectedCode">Expected sanitised boundary code.</param>
    [Theory]
    [InlineData("identity", "aiops.observer.adapter.provenance_identity_untrusted")]
    [InlineData("self-signed", "aiops.observer.adapter.provenance_integrity_invalid")]
    [InlineData("grant-tampered", "aiops.observer.adapter.provenance_integrity_invalid")]
    [InlineData("revocation-tampered", "aiops.observer.adapter.provenance_integrity_invalid")]
    [InlineData("revoked", "aiops.observer.adapter.provenance_revoked")]
    [InlineData("key-revoked", "aiops.observer.adapter.provenance_revoked")]
    [InlineData("stale", "aiops.observer.adapter.provenance_revocation_stale")]
    public void AdapterRejectsUntrustedPolicyProvenance(string scenario, string expectedCode)
    {
        ObserverDataPolicy policy = DataPolicy(
            ObserverDataOptInState.Enabled,
            [new ObserverDataScope(InstanceId, AgentId)]);
        PolicyEvidence evidence = CreatePolicyEvidence(
            policy,
            revokeGrant: scenario == "revoked",
            revokeSigningKey: scenario == "key-revoked",
            tamperGrantSignature: scenario == "grant-tampered",
            tamperRevocationSignature: scenario == "revocation-tampered",
            staleRevocation: scenario == "stale",
            untrustedIdentity: scenario == "identity",
            untrustedGrantKeyMaterial: scenario == "self-signed");

        ObserverTelemetryAdaptationResult result = CanonicalObserverTelemetryAdapter.Adapt(
            Telemetry(7, 125, Start),
            evidence.Context,
            evidence.TrustConfiguration,
            Start);

        Assert.Equal(ObserverTelemetryAdaptationDisposition.Rejected, result.Disposition);
        Assert.Equal(expectedCode, result.Code);
        Assert.Null(result.Sample);
    }

    /// <summary>Runs a versioned synthetic corpus with reference and adversarial cases entirely offline.</summary>
    [Fact]
    public void OfflineEvaluationMeasuresDetectionAndRejectsAdversarialCases()
    {
        ObserverOfflineEvaluationDataset dataset = ReferenceDataset();

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(dataset, GenerousBudget(), Start);

        Assert.True(report.DatasetAccepted);
        Assert.Equal("aiops.observer.offline.completed", report.Code);
        Assert.True(report.Passed);
        Assert.Equal(9, report.PassedCaseCount);
        Assert.Equal(0, report.FailedCaseCount);
        Assert.Equal(3, report.TruePositiveCount);
        Assert.Equal(3, report.TrueNegativeCount);
        Assert.Equal(0, report.FalsePositiveCount);
        Assert.Equal(0, report.FalseNegativeCount);
        Assert.Equal(0, report.UnscoredDetectionCaseCount);
        Assert.Equal(1, report.Precision);
        Assert.Equal(1, report.Recall);
        Assert.Equal(3, report.RejectedAdversarialCaseCount);
        Assert.Equal(0, report.AcceptedAdversarialCaseCount);
        Assert.Equal(3, dataset.Segments.Count);
        Assert.Equal(3, dataset.Segments.Select(item => item.ProviderType).Distinct(StringComparer.Ordinal).Count());
        Assert.True(report.ProcessingCompleted);
        Assert.False(report.BackpressureApplied);
        Assert.Equal(33, report.ConsumedWorkUnits);
        Assert.True(report.ConsumedWorkUnits <= dataset.MaximumRequiredWorkUnits);
        Assert.Equal(3, report.SegmentResults.Count);
        Assert.All(report.SegmentResults, result => Assert.True(result.Passed));
        Assert.Equal(ObserverOfflineEvaluationDataset.CurrentSchemaVersion, report.DatasetSchemaVersion);
        Assert.Equal("state-06.local-evaluation", report.AuthorityReference);
        Assert.Equal("deterministic.synthetic.v1", report.ProvenanceReference);
    }

    /// <summary>Confirms that dataset expiry blocks all case execution and grants no success.</summary>
    [Fact]
    public void OfflineEvaluationRejectsExpiredDatasetBeforeReadingCases()
    {
        ObserverOfflineEvaluationDataset dataset = ReferenceDataset();

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(
            dataset,
            GenerousBudget(),
            dataset.ExpiresAt);

        Assert.False(report.DatasetAccepted);
        Assert.False(report.Passed);
        Assert.Equal("aiops.observer.offline.dataset_expired", report.Code);
        Assert.Empty(report.CaseResults);
    }

    /// <summary>Prevents a governed case from mislabelling the provider or version represented by its telemetry.</summary>
    [Fact]
    public void OfflineCaseRejectsMismatchedProviderSegment()
    {
        ObserverDataPolicy policy = OfflinePolicy(
            "policy.offline.segment",
            ObserverDataOptInState.Enabled,
            [new ObserverDataScope(InstanceId, AgentId)]);

        Assert.Throws<ArgumentException>(() => EvaluationCase(
            "adversarial.segment-mismatch",
            90,
            ObserverOfflineCaseKind.Adversarial,
            new ObserverOfflineEvaluationSegment("other-db", "1.0.0", "offline-windows"),
            CreatePolicyEvidence(policy),
            [Telemetry(90, 1_000, Start, EvidenceLevel.Synthetic)],
            DurationRule(),
            ObserverOfflineExpectedDisposition.AdaptationRejected,
            "aiops.observer.offline.segment_invalid"));
    }

    /// <summary>Ensures a refused binary case fails exact matching without inflating the true-negative metric.</summary>
    [Fact]
    public void OfflineEvaluationLeavesRefusedBinaryCaseUnscored()
    {
        ObserverDataPolicy disabled = OfflinePolicy(
            "policy.offline.unscored",
            ObserverDataOptInState.Disabled,
            [new ObserverDataScope(InstanceId, AgentId)]);
        ObserverOfflineEvaluationCase evaluationCase = EvaluationCase(
            "reference.refused-negative",
            91,
            ObserverOfflineCaseKind.Reference,
            new ObserverOfflineEvaluationSegment("synthetic-db", "1.0.0", "offline-windows"),
            CreatePolicyEvidence(disabled),
            [Telemetry(91, 500, Start, EvidenceLevel.Synthetic)],
            DurationRule(),
            ObserverOfflineExpectedDisposition.FindingNotDetected,
            "aiops.observer.threshold.not_detected");
        ObserverOfflineEvaluationDataset dataset = new(
            "observer.unscored-corpus",
            "1.0.0",
            "deterministic.synthetic.v1",
            "state-06.local-evaluation",
            ObserverDataClassification.OperationalTelemetry,
            Start.AddDays(-1),
            Start.AddDays(1),
            [evaluationCase]);

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(dataset, GenerousBudget(), Start);

        Assert.False(report.Passed);
        Assert.Equal(1, report.FailedCaseCount);
        Assert.Equal(0, report.TrueNegativeCount);
        Assert.Equal(1, report.UnscoredDetectionCaseCount);
        Assert.Null(report.Precision);
        Assert.Null(report.Recall);
    }

    /// <summary>Confirms cancellation is honoured before a bounded offline run begins.</summary>
    [Fact]
    public void OfflineEvaluationHonoursCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ObserverOfflineEvaluationRunner.Evaluate(
                ReferenceDataset(),
                GenerousBudget(),
                Start,
                cancellationToken: cancellation.Token));
    }

    /// <summary>Confirms case, sample and work admission plus elapsed-time enforcement produce typed backpressure.</summary>
    /// <param name="scenario">Budget boundary to exhaust.</param>
    /// <param name="expectedCode">Expected stable backpressure code.</param>
    [Theory]
    [InlineData("cases", "aiops.observer.offline.case_budget_exhausted")]
    [InlineData("samples", "aiops.observer.offline.sample_budget_exhausted")]
    [InlineData("work", "aiops.observer.offline.work_budget_exhausted")]
    [InlineData("time", "aiops.observer.offline.processing_time_exhausted")]
    public void OfflineEvaluationAppliesBoundedBackpressure(string scenario, string expectedCode)
    {
        ObserverOfflineEvaluationDataset dataset = ReferenceDataset();
        ObserverProcessingBudget budget = scenario switch
        {
            "cases" => new(8, 1_000, 2_000, TimeSpan.FromSeconds(30)),
            "samples" => new(100, dataset.TotalSampleCount - 1, 2_000, TimeSpan.FromSeconds(30)),
            "work" => new(100, 1_000, dataset.MaximumRequiredWorkUnits - 1, TimeSpan.FromSeconds(30)),
            _ => new(100, 1_000, 2_000, TimeSpan.FromTicks(1)),
        };
        TimeProvider? clock = scenario == "time" ? new AdvancingTimeProvider() : null;

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(
            dataset,
            budget,
            Start,
            clock);

        Assert.True(report.DatasetAccepted);
        Assert.False(report.ProcessingCompleted);
        Assert.True(report.BackpressureApplied);
        Assert.False(report.Passed);
        Assert.Equal(expectedCode, report.Code);
        Assert.True(report.ConsumedWorkUnits < report.MaximumRequiredWorkUnits);
    }

    /// <summary>Builds the governed nine-case, three-segment synthetic corpus used by the offline quality regression.</summary>
    /// <returns>A current, provenance-aware and segmented offline dataset.</returns>
    private static ObserverOfflineEvaluationDataset ReferenceDataset()
    {
        ObserverDataScope source = new(InstanceId, AgentId);
        ObserverDataScope otherSource = new(OtherInstanceId, OtherAgentId);
        ObserverDataPolicy enabled = OfflinePolicy(
            "policy.offline.enabled",
            ObserverDataOptInState.Enabled,
            [source]);
        ObserverDataPolicy disabled = OfflinePolicy(
            "policy.offline.disabled",
            ObserverDataOptInState.Disabled,
            [source]);
        ObserverDataPolicy multiSource = OfflinePolicy(
            "policy.offline.multi-source",
            ObserverDataOptInState.Enabled,
            [source, otherSource]);
        PolicyEvidence enabledEvidence = CreatePolicyEvidence(enabled);
        PolicyEvidence disabledEvidence = CreatePolicyEvidence(disabled);
        PolicyEvidence multiSourceEvidence = CreatePolicyEvidence(multiSource);
        ObserverOfflineEvaluationSegment relational = new("fixture-relational", "1.0.0", "offline-windows");
        ObserverOfflineEvaluationSegment document = new("fixture-document", "2.0.0", "offline-linux");
        ObserverOfflineEvaluationSegment keyValue = new("fixture-keyvalue", "3.0.0", "offline-container");
        ObserverThresholdRule rule = DurationRule();
        ObserverOfflineEvaluationCase[] cases =
        [
            EvaluationCase(
                "reference.detected",
                1,
                ObserverOfflineCaseKind.Reference,
                relational,
                enabledEvidence,
                [
                    Telemetry(11, 500, Start.AddMinutes(-20), EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                    Telemetry(12, 1_100, Start.AddMinutes(-10), EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                    Telemetry(13, 1_200, Start, EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingDetected,
                "aiops.observer.threshold.detected"),
            EvaluationCase(
                "reference.not-detected",
                2,
                ObserverOfflineCaseKind.Reference,
                relational,
                enabledEvidence,
                [
                    Telemetry(21, 400, Start.AddMinutes(-20), EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                    Telemetry(22, 500, Start.AddMinutes(-10), EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                    Telemetry(23, 600, Start, EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingNotDetected,
                "aiops.observer.threshold.not_detected"),
            EvaluationCase(
                "adversarial.future",
                3,
                ObserverOfflineCaseKind.Adversarial,
                relational,
                enabledEvidence,
                [Telemetry(31, 2_000, Start.AddMinutes(1), EvidenceLevel.Synthetic, providerType: relational.ProviderType, providerVersion: relational.ProviderVersion)],
                rule,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.adapter.timestamp_invalid"),
            EvaluationCase(
                "reference.document-detected",
                4,
                ObserverOfflineCaseKind.Reference,
                document,
                enabledEvidence,
                [
                    Telemetry(41, 1_100, Start.AddMinutes(-1), EvidenceLevel.Synthetic, providerType: document.ProviderType, providerVersion: document.ProviderVersion),
                    Telemetry(42, 1_200, Start, EvidenceLevel.Synthetic, providerType: document.ProviderType, providerVersion: document.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingDetected,
                "aiops.observer.threshold.detected"),
            EvaluationCase(
                "reference.document-not-detected",
                5,
                ObserverOfflineCaseKind.Reference,
                document,
                enabledEvidence,
                [
                    Telemetry(51, 300, Start.AddMinutes(-1), EvidenceLevel.Synthetic, providerType: document.ProviderType, providerVersion: document.ProviderVersion),
                    Telemetry(52, 400, Start, EvidenceLevel.Synthetic, providerType: document.ProviderType, providerVersion: document.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingNotDetected,
                "aiops.observer.threshold.not_detected"),
            EvaluationCase(
                "adversarial.opt-in-disabled",
                6,
                ObserverOfflineCaseKind.Adversarial,
                document,
                disabledEvidence,
                [Telemetry(61, 2_000, Start, EvidenceLevel.Synthetic, providerType: document.ProviderType, providerVersion: document.ProviderVersion)],
                rule,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.adapter.policy_disabled"),
            EvaluationCase(
                "reference.keyvalue-detected",
                7,
                ObserverOfflineCaseKind.Reference,
                keyValue,
                enabledEvidence,
                [
                    Telemetry(71, 1_300, Start.AddMinutes(-1), EvidenceLevel.Synthetic, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                    Telemetry(72, 1_400, Start, EvidenceLevel.Synthetic, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingDetected,
                "aiops.observer.threshold.detected"),
            EvaluationCase(
                "reference.keyvalue-not-detected",
                8,
                ObserverOfflineCaseKind.Reference,
                keyValue,
                enabledEvidence,
                [
                    Telemetry(81, 700, Start.AddMinutes(-1), EvidenceLevel.Synthetic, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                    Telemetry(82, 800, Start, EvidenceLevel.Synthetic, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingNotDetected,
                "aiops.observer.threshold.not_detected"),
            EvaluationCase(
                "adversarial.cross-instance",
                9,
                ObserverOfflineCaseKind.Adversarial,
                keyValue,
                multiSourceEvidence,
                [
                    Telemetry(91, 2_000, Start.AddMinutes(-1), EvidenceLevel.Synthetic, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                    Telemetry(92, 2_000, Start, EvidenceLevel.Synthetic, instanceId: OtherInstanceId, agentId: OtherAgentId, providerType: keyValue.ProviderType, providerVersion: keyValue.ProviderVersion),
                ],
                rule,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.offline.evidence_boundary_invalid"),
        ];
        return new ObserverOfflineEvaluationDataset(
            "observer.reference-corpus",
            "1.0.0",
            "deterministic.synthetic.v1",
            "state-06.local-evaluation",
            ObserverDataClassification.OperationalTelemetry,
            Start.AddDays(-1),
            Start.AddDays(1),
            cases);
    }

    /// <summary>Builds one immutable offline evaluation case with a stable analysis identifier.</summary>
    /// <param name="caseId">Stable case identifier.</param>
    /// <param name="analysisOrdinal">Small positive analysis identifier ordinal.</param>
    /// <param name="kind">Reference or adversarial kind.</param>
    /// <param name="segment">Provider/version/platform reporting segment.</param>
    /// <param name="policyEvidence">Signed evidence and separately configured public trust.</param>
    /// <param name="telemetry">Canonical telemetry candidates.</param>
    /// <param name="rule">Deterministic duration threshold.</param>
    /// <param name="expectedDisposition">Exact expected disposition.</param>
    /// <param name="expectedCode">Exact expected stable code.</param>
    /// <returns>One reproducible offline case.</returns>
    private static ObserverOfflineEvaluationCase EvaluationCase(
        string caseId,
        int analysisOrdinal,
        ObserverOfflineCaseKind kind,
        ObserverOfflineEvaluationSegment segment,
        PolicyEvidence policyEvidence,
        IReadOnlyCollection<ObserverCanonicalHealthTelemetry> telemetry,
        ObserverThresholdRule rule,
        ObserverOfflineExpectedDisposition expectedDisposition,
        string expectedCode) =>
        new(
            caseId,
            Guid.Parse($"55555555-5555-5555-5555-{analysisOrdinal:D12}"),
            kind,
            segment,
            policyEvidence.Context,
            policyEvidence.TrustConfiguration,
            Start,
            telemetry,
            rule,
            expectedDisposition,
            expectedCode);

    /// <summary>Builds one exact-source runtime-analysis data policy.</summary>
    /// <param name="state">Explicit enabled or disabled state.</param>
    /// <param name="sources">Exact source pairs.</param>
    /// <returns>A bounded non-synthetic policy.</returns>
    private static ObserverDataPolicy DataPolicy(
        ObserverDataOptInState state,
        IReadOnlyCollection<ObserverDataScope> sources) =>
        new(
            "policy.runtime",
            "1.0.0",
            state,
            ObserverDataUse.RuntimeAnalysis,
            ScopeId,
            Start.AddHours(-1),
            Start.AddHours(1),
            sources);

    /// <summary>Builds one synthetic-enabled policy usable only by the isolated offline runner.</summary>
    /// <param name="policyId">Stable policy identifier.</param>
    /// <param name="state">Explicit enabled or disabled state.</param>
    /// <param name="sources">Exact source pairs.</param>
    /// <returns>A bounded offline-evaluation policy.</returns>
    private static ObserverDataPolicy OfflinePolicy(
        string policyId,
        ObserverDataOptInState state,
        IReadOnlyCollection<ObserverDataScope> sources) =>
        new(
            policyId,
            "1.0.0",
            state,
            ObserverDataUse.OfflineEvaluation,
            ScopeId,
            Start.AddHours(-1),
            Start.AddHours(1),
            sources,
            allowSyntheticEvidence: true);

    /// <summary>Builds one canonical duration telemetry candidate without provider-native payloads.</summary>
    /// <param name="ordinal">Stable observation identifier ordinal.</param>
    /// <param name="durationMilliseconds">Non-negative probe duration.</param>
    /// <param name="observedAt">UTC source observation instant.</param>
    /// <param name="evidenceLevel">Canonical evidential strength.</param>
    /// <param name="status">Canonical status.</param>
    /// <param name="instanceId">Optional database-instance override.</param>
    /// <param name="agentId">Optional Agent override.</param>
    /// <param name="error">Optional sanitised normalised error.</param>
    /// <param name="receivedAt">Optional authoritative receipt instant override.</param>
    /// <param name="providerType">Synthetic provider-segment identifier.</param>
    /// <param name="providerVersion">Synthetic provider-version segment.</param>
    /// <returns>One typed canonical health telemetry envelope.</returns>
    private static ObserverCanonicalHealthTelemetry Telemetry(
        int ordinal,
        double durationMilliseconds,
        DateTimeOffset observedAt,
        EvidenceLevel evidenceLevel = EvidenceLevel.ProviderAuthenticated,
        HealthStatus status = HealthStatus.Degraded,
        Guid? instanceId = null,
        Guid? agentId = null,
        NormalizedError? error = null,
        DateTimeOffset? receivedAt = null,
        string providerType = "synthetic-db",
        string providerVersion = "1.0.0")
    {
        HealthObservation observation = new(
            Guid.Parse($"66666666-6666-6666-6666-{ordinal:D12}"),
            instanceId ?? InstanceId,
            agentId ?? AgentId,
            ProviderType.Parse(providerType),
            providerVersion,
            status,
            "offline-fixture",
            observedAt,
            TimeSpan.FromMilliseconds(durationMilliseconds),
            new ObservationQuality(evidenceLevel, 1, []),
            error);
        return new ObserverCanonicalHealthTelemetry(observation, receivedAt ?? observedAt);
    }

    /// <summary>Creates an in-memory signed policy context with independent grant and revocation keys.</summary>
    /// <param name="policy">Exact immutable policy to authorise.</param>
    /// <param name="revokeGrant">Whether the current authenticated snapshot revokes this grant.</param>
    /// <param name="revokeSigningKey">Whether the current authenticated snapshot revokes the grant-signing key.</param>
    /// <param name="tamperGrantSignature">Whether to corrupt the grant signature after signing.</param>
    /// <param name="tamperRevocationSignature">Whether to corrupt the revocation signature after signing.</param>
    /// <param name="staleRevocation">Whether the snapshot expires exactly at evaluation time.</param>
    /// <param name="untrustedIdentity">Whether the configured grant anchor names another issuer.</param>
    /// <param name="untrustedGrantKeyMaterial">Whether configured trust uses another key with the asserted identity.</param>
    /// <returns>One caller-owned verification context containing public keys and signatures but no private material.</returns>
    private static PolicyEvidence CreatePolicyEvidence(
        ObserverDataPolicy policy,
        bool revokeGrant = false,
        bool revokeSigningKey = false,
        bool tamperGrantSignature = false,
        bool tamperRevocationSignature = false,
        bool staleRevocation = false,
        bool untrustedIdentity = false,
        bool untrustedGrantKeyMaterial = false)
    {
        const string issuerId = "state-06.policy-authority";
        const string grantKeyId = "policy-signing-key.v1";
        const string revocationKeyId = "revocation-signing-key.v1";
        string grantId = $"grant.{policy.PolicyId}";
        ObserverDataPolicyGrant grant = new(
            grantId,
            issuerId,
            grantKeyId,
            Start.AddHours(-1),
            Start.AddHours(1),
            policy);
        ObserverPolicyRevocationSnapshot revocation = new(
            "policy-revocations",
            "1.0.0",
            issuerId,
            revocationKeyId,
            Start.AddHours(-1),
            staleRevocation ? Start : Start.AddHours(1),
            revokeGrant ? [grantId] : [],
            revokeSigningKey ? [grantKeyId] : []);

        using ECDsa grantSigner = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa revocationSigner = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using ECDsa alternativeGrantTrust = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        byte[] grantSignature = grantSigner.SignData(
            ObserverPolicyProvenancePayload.CreateGrant(grant),
            HashAlgorithmName.SHA256);
        byte[] revocationSignature = revocationSigner.SignData(
            ObserverPolicyProvenancePayload.CreateRevocation(revocation),
            HashAlgorithmName.SHA256);
        if (tamperGrantSignature)
        {
            grantSignature[0] ^= 0x01;
        }

        if (tamperRevocationSignature)
        {
            revocationSignature[0] ^= 0x01;
        }

        ObserverPolicyTrustAnchor grantAnchor = new(
            untrustedIdentity ? "other.policy-authority" : issuerId,
            grantKeyId,
            untrustedGrantKeyMaterial
                ? alternativeGrantTrust.ExportSubjectPublicKeyInfo()
                : grantSigner.ExportSubjectPublicKeyInfo());
        ObserverPolicyTrustAnchor revocationAnchor = new(
            issuerId,
            revocationKeyId,
            revocationSigner.ExportSubjectPublicKeyInfo());
        ObserverDataPolicyVerificationContext context = new(
            grant,
            grantSignature,
            revocation,
            revocationSignature);
        ObserverPolicyTrustConfiguration trustConfiguration = new(grantAnchor, revocationAnchor);
        return new PolicyEvidence(context, trustConfiguration);
    }

    /// <summary>Creates a budget comfortably above the bounded reference corpus.</summary>
    /// <returns>A local-only case, sample, work-unit and elapsed-time budget.</returns>
    private static ObserverProcessingBudget GenerousBudget() =>
        new(100, 1_000, 2_000, TimeSpan.FromSeconds(30));

    /// <summary>Builds the deterministic two-sample duration threshold used by the reference corpus.</summary>
    /// <returns>A bounded threshold over the trusted adapter's sole provider-neutral metric.</returns>
    private static ObserverThresholdRule DurationRule() =>
        new(
            "database.probe.slow",
            "1.0.0",
            CanonicalObserverTelemetryAdapter.MetricKey,
            CanonicalObserverTelemetryAdapter.Unit,
            ObserverThresholdComparison.GreaterThanOrEqual,
            1_000,
            ObserverFindingSeverity.Warning,
            2,
            TimeSpan.FromHours(1),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(15));

    /// <summary>Advances two ticks per timestamp read to deterministically exercise elapsed-time backpressure.</summary>
    private sealed class AdvancingTimeProvider : TimeProvider
    {
        private long timestamp;

        /// <summary>Gets the TimeSpan tick frequency used by this deterministic test clock.</summary>
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        /// <summary>Returns a monotonically advancing timestamp without wall-clock access.</summary>
        /// <returns>The next deterministic timestamp.</returns>
        public override long GetTimestamp() => Interlocked.Add(ref timestamp, 2);
    }

    /// <summary>Pairs untrusted signed policy evidence with separately configured public trust in tests.</summary>
    /// <param name="Context">Caller-supplied signed policy and revocation evidence.</param>
    /// <param name="TrustConfiguration">Application-configured public trust.</param>
    private sealed record PolicyEvidence(
        ObserverDataPolicyVerificationContext Context,
        ObserverPolicyTrustConfiguration TrustConfiguration);
}
