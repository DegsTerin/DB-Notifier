// Module purpose: Verifies the trusted MOD-12 canonical telemetry adapter, fail-closed data opt-in policy and governed offline evaluation boundary.
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

        ObserverTelemetryAdaptationResult result = CanonicalObserverTelemetryAdapter.Adapt(
            telemetry,
            policy,
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

        ObserverTelemetryAdaptationResult result = CanonicalObserverTelemetryAdapter.Adapt(
            telemetry,
            policy,
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

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(dataset, Start);

        Assert.True(report.DatasetAccepted);
        Assert.Equal("aiops.observer.offline.completed", report.Code);
        Assert.True(report.Passed);
        Assert.Equal(5, report.PassedCaseCount);
        Assert.Equal(0, report.FailedCaseCount);
        Assert.Equal(1, report.TruePositiveCount);
        Assert.Equal(1, report.TrueNegativeCount);
        Assert.Equal(0, report.FalsePositiveCount);
        Assert.Equal(0, report.FalseNegativeCount);
        Assert.Equal(0, report.UnscoredDetectionCaseCount);
        Assert.Equal(1, report.Precision);
        Assert.Equal(1, report.Recall);
        Assert.Equal(3, report.RejectedAdversarialCaseCount);
        Assert.Equal(0, report.AcceptedAdversarialCaseCount);
        Assert.Single(dataset.Segments);
        Assert.Equal("synthetic-db", dataset.Segments[0].ProviderType);
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
            policy,
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
            disabled,
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

        ObserverOfflineEvaluationReport report = ObserverOfflineEvaluationRunner.Evaluate(dataset, Start);

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
            ObserverOfflineEvaluationRunner.Evaluate(ReferenceDataset(), Start, cancellation.Token));
    }

    /// <summary>Builds the governed five-case synthetic corpus used by the offline quality regression.</summary>
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
        ObserverOfflineEvaluationSegment segment = new("synthetic-db", "1.0.0", "offline-windows");
        ObserverThresholdRule rule = DurationRule();
        ObserverOfflineEvaluationCase[] cases =
        [
            EvaluationCase(
                "reference.detected",
                1,
                ObserverOfflineCaseKind.Reference,
                segment,
                enabled,
                [
                    Telemetry(11, 500, Start.AddMinutes(-20), EvidenceLevel.Synthetic),
                    Telemetry(12, 1_100, Start.AddMinutes(-10), EvidenceLevel.Synthetic),
                    Telemetry(13, 1_200, Start, EvidenceLevel.Synthetic),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingDetected,
                "aiops.observer.threshold.detected"),
            EvaluationCase(
                "reference.not-detected",
                2,
                ObserverOfflineCaseKind.Reference,
                segment,
                enabled,
                [
                    Telemetry(21, 400, Start.AddMinutes(-20), EvidenceLevel.Synthetic),
                    Telemetry(22, 500, Start.AddMinutes(-10), EvidenceLevel.Synthetic),
                    Telemetry(23, 600, Start, EvidenceLevel.Synthetic),
                ],
                rule,
                ObserverOfflineExpectedDisposition.FindingNotDetected,
                "aiops.observer.threshold.not_detected"),
            EvaluationCase(
                "adversarial.future",
                3,
                ObserverOfflineCaseKind.Adversarial,
                segment,
                enabled,
                [Telemetry(31, 2_000, Start.AddMinutes(1), EvidenceLevel.Synthetic)],
                rule,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.adapter.timestamp_invalid"),
            EvaluationCase(
                "adversarial.opt-in-disabled",
                4,
                ObserverOfflineCaseKind.Adversarial,
                segment,
                disabled,
                [Telemetry(41, 2_000, Start, EvidenceLevel.Synthetic)],
                rule,
                ObserverOfflineExpectedDisposition.AdaptationRejected,
                "aiops.observer.adapter.policy_disabled"),
            EvaluationCase(
                "adversarial.cross-instance",
                5,
                ObserverOfflineCaseKind.Adversarial,
                segment,
                multiSource,
                [
                    Telemetry(51, 2_000, Start.AddMinutes(-1), EvidenceLevel.Synthetic),
                    Telemetry(
                        52,
                        2_000,
                        Start,
                        EvidenceLevel.Synthetic,
                        instanceId: OtherInstanceId,
                        agentId: OtherAgentId),
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
    /// <param name="policy">Explicit offline data policy.</param>
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
        ObserverDataPolicy policy,
        IReadOnlyCollection<ObserverCanonicalHealthTelemetry> telemetry,
        ObserverThresholdRule rule,
        ObserverOfflineExpectedDisposition expectedDisposition,
        string expectedCode) =>
        new(
            caseId,
            Guid.Parse($"55555555-5555-5555-5555-{analysisOrdinal:D12}"),
            kind,
            segment,
            policy,
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
        DateTimeOffset? receivedAt = null)
    {
        HealthObservation observation = new(
            Guid.Parse($"66666666-6666-6666-6666-{ordinal:D12}"),
            instanceId ?? InstanceId,
            agentId ?? AgentId,
            ProviderType.Parse("synthetic-db"),
            "1.0.0",
            status,
            "offline-fixture",
            observedAt,
            TimeSpan.FromMilliseconds(durationMilliseconds),
            new ObservationQuality(evidenceLevel, 1, []),
            error);
        return new ObserverCanonicalHealthTelemetry(observation, receivedAt ?? observedAt);
    }

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
}
