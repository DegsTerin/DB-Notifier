// Module purpose: Verifies the inactive MOD-12 Observer foundation, its evidence boundary and its non-mutating deterministic outcomes.
using System.Collections;
using DBNotifier.Application.AIOps;

namespace DBNotifier.UnitTests;

/// <summary>Protects the evidence-first, fail-closed and non-mutating behaviour of the initial MOD-12 foundation.</summary>
public sealed class AIOpsObserverTests
{
    private static readonly Guid InstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ScopeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Start = new(2026, 7, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Confirms validation of finite values, safe identifiers and caller-declared handling fields.</summary>
    [Fact]
    public void MetricSampleValidatesContractShapeAndDeclaredHandling()
    {
        ObserverMetricSample valid = Sample(9, 1);

        Assert.Equal(ObserverDataClassification.OperationalTelemetry, valid.Classification);
        Assert.Equal(ObserverRedactionStatus.Sanitised, valid.RedactionStatus);
        Assert.Equal(ObserverRetentionClass.EphemeralAnalysisOnly, valid.RetentionClass);
        Assert.Equal(ObserverPermittedPurpose.NonMutatingObserverAnalysis, valid.PermittedPurpose);
        Assert.Equal(ObserverMissingness.Present, valid.Missingness);
        Assert.Equal(ObserverMetricCardinality.SingleSeries, valid.Cardinality);
        Assert.Throws<ArgumentException>(() => Sample(1, 1, metricKey: "Database Password"));
        Assert.Throws<ArgumentException>(() => Sample(1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            classification: (ObserverDataClassification)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            redactionStatus: (ObserverRedactionStatus)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            retentionClass: (ObserverRetentionClass)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            permittedPurpose: (ObserverPermittedPurpose)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            quality: (ObserverEvidenceQuality)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            missingness: (ObserverMissingness)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample(
            1,
            1,
            cardinality: (ObserverMetricCardinality)0));
    }

    /// <summary>Confirms that one analysis cannot mix instances, scopes or repeated evidence identifiers.</summary>
    [Fact]
    public void AnalysisRequestRejectsBoundaryCrossingAndDuplicateEvidence()
    {
        ObserverMetricSample canonical = Sample(1, 10);
        ObserverMetricSample otherInstance = Sample(2, 20, instanceId: Guid.NewGuid());
        ObserverMetricSample otherScope = Sample(3, 30, scopeId: Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => Request([canonical, otherInstance]));
        Assert.Throws<ArgumentException>(() => Request([canonical, otherScope]));
        Assert.Throws<ArgumentException>(() => Request([canonical, canonical]));
    }

    /// <summary>Verifies consecutive-sample debounce and immutable evidence linkage for a detected threshold.</summary>
    [Fact]
    public void ThresholdDetectsOnlyAfterRequiredConsecutiveEvidence()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 91, hours: 0),
            Sample(2, 95, hours: 1),
            Sample(3, 97, hours: 2),
        ],
        asOf: Start.AddHours(2));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(request, ThresholdRule());

        Assert.Equal(ObserverFindingDisposition.Detected, result.Disposition);
        Assert.Equal("aiops.observer.threshold.detected", result.Code);
        Assert.Equal(97, result.ObservedValue);
        Assert.Equal(Start.AddHours(2), result.ObservedAt);
        Assert.Equal(Start.AddHours(3), result.ValidUntil);
        Assert.Equal(request.AnalysisId, result.AnalysisId);
        Assert.Equal(request.AuthorisationScopeId, result.AuthorisationScopeId);
        Assert.Equal(ObserverFindingSeverity.Critical, result.PolicySeverity);
        Assert.Equal([EvidenceId(1), EvidenceId(2), EvidenceId(3)], result.EvidenceIds);
        Assert.Empty(result.LimitationCodes);
    }

    /// <summary>Verifies that sufficient evidence can explicitly report absence rather than a false positive.</summary>
    [Fact]
    public void ThresholdReportsNotDetectedWhenDebounceWindowDoesNotMatch()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 96, hours: 0),
            Sample(2, 80, hours: 1),
            Sample(3, 97, hours: 2),
        ],
        asOf: Start.AddHours(2));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(request, ThresholdRule());

        Assert.Equal(ObserverFindingDisposition.NotDetected, result.Disposition);
        Assert.Equal("aiops.observer.threshold.not_detected", result.Code);
    }

    /// <summary>Verifies every supported comparison without relying on string interpretation.</summary>
    /// <param name="comparison">Typed comparison under test.</param>
    /// <param name="value">Finite evidence value.</param>
    /// <param name="expectedDetected">Expected deterministic detection outcome.</param>
    [Theory]
    [InlineData(ObserverThresholdComparison.GreaterThan, 11, true)]
    [InlineData(ObserverThresholdComparison.GreaterThan, 10, false)]
    [InlineData(ObserverThresholdComparison.GreaterThanOrEqual, 10, true)]
    [InlineData(ObserverThresholdComparison.LessThan, 9, true)]
    [InlineData(ObserverThresholdComparison.LessThan, 10, false)]
    [InlineData(ObserverThresholdComparison.LessThanOrEqual, 10, true)]
    public void ThresholdAppliesTypedComparison(
        ObserverThresholdComparison comparison,
        double value,
        bool expectedDetected)
    {
        ObserverThresholdRule rule = new(
            "typed.comparison",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            comparison,
            10,
            ObserverFindingSeverity.Information,
            1,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(
            Request([Sample(1, value)], Start),
            rule);

        Assert.Equal(expectedDetected ? ObserverFindingDisposition.Detected : ObserverFindingDisposition.NotDetected, result.Disposition);
    }

    /// <summary>Verifies that stale, future, reversed and unknown evidence fail closed.</summary>
    /// <param name="scenario">Fixture scenario selecting stale, future, reversed or unknown evidence.</param>
    [Theory]
    [InlineData("stale")]
    [InlineData("future")]
    [InlineData("reversed")]
    [InlineData("unknown")]
    public void ThresholdRefusesUntrustworthyEvidence(string scenario)
    {
        DateTimeOffset asOf = Start.AddHours(2);
        ObserverMetricSample sample = scenario switch
        {
            "stale" => Sample(1, 95, hours: -3),
            "future" => Sample(1, 95, hours: 3),
            "reversed" => Sample(1, 95, hours: 1, receivedAt: Start),
            "unknown" => Sample(1, 95, hours: 1, quality: ObserverEvidenceQuality.Unknown),
            _ => throw new InvalidOperationException("Unexpected fixture scenario."),
        };
        ObserverThresholdRule rule = new(
            "resource.pressure",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThanOrEqual,
            90,
            ObserverFindingSeverity.Critical,
            1,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(Request([sample], asOf), rule);

        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition);
        Assert.StartsWith("aiops.observer.", result.Code, StringComparison.Ordinal);
        Assert.NotEmpty(result.LimitationCodes);
    }

    /// <summary>Verifies explicit insufficiency for too few or temporally disconnected samples.</summary>
    [Fact]
    public void ThresholdRefusesInsufficientOrGappedSamples()
    {
        ObserverThresholdRule rule = ThresholdRule();
        ObserverThresholdResult insufficient = DeterministicThresholdAnalyser.Analyse(
            Request([Sample(1, 95)], Start),
            rule);
        ObserverThresholdResult gapped = DeterministicThresholdAnalyser.Analyse(
            Request(
            [
                Sample(1, 95, hours: 0),
                Sample(2, 96, hours: 1),
                Sample(3, 97, hours: 3),
            ],
            Start.AddHours(3)),
            rule);

        Assert.Equal("aiops.observer.threshold.insufficient_samples", insufficient.Code);
        Assert.Equal("aiops.observer.threshold.sample_gap", gapped.Code);
        Assert.All(
            [insufficient, gapped],
            result => Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition));
    }

    /// <summary>Verifies that degraded evidence remains visible as a limitation rather than being silently upgraded.</summary>
    [Fact]
    public void ThresholdPreservesDegradedEvidenceLimitation()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 91, quality: ObserverEvidenceQuality.Degraded),
            Sample(2, 92, hours: 0.25),
            Sample(3, 93, hours: 0.5),
        ],
        Start.AddHours(0.5));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(request, ThresholdRule());

        Assert.Equal(ObserverFindingDisposition.Detected, result.Disposition);
        Assert.Contains("aiops.observer.evidence.degraded", result.LimitationCodes);
    }

    /// <summary>Verifies that degraded evidence outside the selected debounce set does not create an unlinked limitation.</summary>
    [Fact]
    public void ThresholdAttributesLimitationsOnlyToReturnedEvidence()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 91, quality: ObserverEvidenceQuality.Degraded),
            Sample(2, 95, hours: 2),
        ],
        Start.AddHours(2));
        ObserverThresholdRule rule = new(
            "resource.pressure",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThanOrEqual,
            90,
            ObserverFindingSeverity.Warning,
            1,
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

        ObserverThresholdResult result = DeterministicThresholdAnalyser.Analyse(request, rule);

        Assert.Equal([EvidenceId(2)], result.EvidenceIds);
        Assert.Empty(result.LimitationCodes);
    }

    /// <summary>Verifies source-contract conflict only within the evidence window actually analysed.</summary>
    [Fact]
    public void ThresholdRejectsMixedCurrentSourceContractsButIgnoresExpiredVersion()
    {
        ObserverThresholdRule rule = new(
            "resource.pressure",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThanOrEqual,
            90,
            ObserverFindingSeverity.Warning,
            1,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
        ObserverThresholdResult conflicted = DeterministicThresholdAnalyser.Analyse(
            Request(
            [
                Sample(1, 91, hours: 1.5, schemaVersion: "1.0.0"),
                Sample(2, 92, hours: 2, schemaVersion: "2.0.0"),
            ],
            Start.AddHours(2)),
            rule);
        ObserverThresholdResult current = DeterministicThresholdAnalyser.Analyse(
            Request(
            [
                Sample(3, 91, hours: 0, schemaVersion: "1.0.0"),
                Sample(4, 92, hours: 2, schemaVersion: "2.0.0"),
            ],
            Start.AddHours(2)),
            rule);

        Assert.Equal("aiops.observer.evidence.source_contract_conflict", conflicted.Code);
        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, conflicted.Disposition);
        Assert.Equal(ObserverFindingDisposition.Detected, current.Disposition);
        Assert.Equal([EvidenceId(4)], current.EvidenceIds);
    }

    /// <summary>Confirms that provider-origin metadata cannot alter provider-neutral threshold semantics.</summary>
    [Fact]
    public void ThresholdSemanticsAreProviderNeutral()
    {
        ObserverMetricSample postgres = Sample(1, 95, sourceKind: "canonical.postgresql.metric");
        ObserverMetricSample mysql = Sample(2, 95, sourceKind: "canonical.mysql.metric");
        ObserverThresholdRule rule = new(
            "resource.pressure",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThan,
            90,
            ObserverFindingSeverity.Warning,
            1,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

        ObserverThresholdResult first = DeterministicThresholdAnalyser.Analyse(Request([postgres], Start), rule);
        ObserverThresholdResult second = DeterministicThresholdAnalyser.Analyse(Request([mysql], Start), rule);

        Assert.Equal(first.Disposition, second.Disposition);
        Assert.Equal(first.Code, second.Code);
        Assert.Equal(first.ObservedValue, second.ObservedValue);
    }

    /// <summary>Verifies an exact linear trend, its reproducible fit and its bounded capacity date.</summary>
    [Fact]
    public void CapacityForecastPredictsLinearExhaustionWithExplainableEvidence()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 10, hours: 0, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(2, 20, hours: 1, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(3, 30, hours: 2, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(4, 40, hours: 3, metricKey: "storage.used.bytes", unit: "bytes"),
        ],
        Start.AddHours(3));

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(request, ForecastPolicy());

        Assert.Equal(ObserverFindingDisposition.Detected, result.Disposition);
        Assert.Equal("aiops.observer.forecast.capacity_within_horizon", result.Code);
        Assert.Equal("ordinary-least-squares", result.ModelId);
        Assert.Equal("1.0.0", result.ModelVersion);
        Assert.True(result.ModelApplied);
        Assert.Equal(10, result.GrowthPerHour!.Value, precision: 10);
        Assert.Equal(10, result.GrowthRateLowerBound95!.Value, precision: 10);
        Assert.Equal(10, result.GrowthRateUpperBound95!.Value, precision: 10);
        Assert.Equal(1, result.FitQuality);
        Assert.Equal(0, result.RootMeanSquaredError);
        Assert.Equal(Start.AddHours(9), result.PredictedAt);
        Assert.Equal(result.PredictedAt, result.EarliestSensitivityAt);
        Assert.Equal(result.PredictedAt, result.LatestSensitivityAt);
        Assert.Equal(4, result.SampleCount);
        Assert.Equal([EvidenceId(1), EvidenceId(2), EvidenceId(3), EvidenceId(4)], result.EvidenceIds);
        Assert.Equal(request.AnalysisId, result.AnalysisId);
        Assert.Equal(request.AuthorisationScopeId, result.AuthorisationScopeId);
        Assert.Contains("aiops.observer.forecast.time_bounds_are_sensitivity", result.LimitationCodes);
    }

    /// <summary>Verifies that the threshold date uses the complete fitted OLS line rather than a latest-value anchor.</summary>
    [Fact]
    public void CapacityForecastUsesFittedInterceptForPointEstimate()
    {
        ObserverAnalysisRequest request = StorageRequest([10, 20, 30, 45]);

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(request, ForecastPolicy());

        Assert.Equal(ObserverFindingDisposition.Detected, result.Disposition);
        Assert.Equal(Start.AddHours(91d / 11.5d), result.PredictedAt);
    }

    /// <summary>Verifies that changing only the numeric unit scale does not change fit or disposition.</summary>
    [Fact]
    public void CapacityForecastFitIsScaleInvariant()
    {
        ObserverCapacityForecastResult baseline = CapacityForecastAnalyser.Analyse(
            StorageRequest([10, 20, 30, 40]),
            ForecastPolicy(capacityLimit: 100));
        ObserverCapacityForecastResult scaled = CapacityForecastAnalyser.Analyse(
            StorageRequest([1e-8, 2e-8, 3e-8, 4e-8]),
            ForecastPolicy(capacityLimit: 1e-7));

        Assert.Equal(baseline.Disposition, scaled.Disposition);
        Assert.Equal(baseline.FitQuality, scaled.FitQuality);
        Assert.Equal(baseline.PredictedAt, scaled.PredictedAt);
    }

    /// <summary>Verifies that a flat trend produces a factual absence rather than a capacity alarm.</summary>
    [Fact]
    public void CapacityForecastReportsNoDetectedGrowthForFlatEvidence()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 20, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(2, 20, hours: 1, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(3, 20, hours: 2, metricKey: "storage.used.bytes", unit: "bytes"),
        ],
        Start.AddHours(2));

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(request, ForecastPolicy());

        Assert.Equal(ObserverFindingDisposition.NotDetected, result.Disposition);
        Assert.Equal("aiops.observer.forecast.growth_not_detected", result.Code);
        Assert.Null(result.PredictedAt);
    }

    /// <summary>Verifies that low fit and a confidence interval spanning no growth remain insufficient evidence.</summary>
    [Fact]
    public void CapacityForecastExposesUnreliableAndUncertainGrowth()
    {
        ObserverAnalysisRequest unreliableRequest = StorageRequest([10, 50, 20, 60]);
        ObserverCapacityForecastPolicy strictFit = ForecastPolicy(minimumFitQuality: 0.95);
        ObserverCapacityForecastResult unreliable = CapacityForecastAnalyser.Analyse(unreliableRequest, strictFit);

        ObserverAnalysisRequest uncertainRequest = StorageRequest([10, 30, 11, 31]);
        ObserverCapacityForecastPolicy permissiveFit = ForecastPolicy(minimumFitQuality: 0);
        ObserverCapacityForecastResult uncertain = CapacityForecastAnalyser.Analyse(uncertainRequest, permissiveFit);

        Assert.Equal("aiops.observer.forecast.fit_below_policy", unreliable.Code);
        Assert.Equal("aiops.observer.forecast.growth_uncertain", uncertain.Code);
        Assert.All(
            [unreliable, uncertain],
            result => Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition));
        Assert.Null(uncertain.LatestSensitivityAt);

        ObserverCapacityForecastResult zeroSlopeNoisy = CapacityForecastAnalyser.Analyse(
            StorageRequest([10, 100, 10]),
            ForecastPolicy(minimumFitQuality: 0.8));
        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, zeroSlopeNoisy.Disposition);
        Assert.Equal("aiops.observer.forecast.fit_below_policy", zeroSlopeNoisy.Code);
    }

    /// <summary>Verifies current capacity and out-of-horizon outcomes without treating either as model authority.</summary>
    [Fact]
    public void CapacityForecastDistinguishesReachedAndOutsideHorizon()
    {
        ObserverAnalysisRequest reachedRequest = StorageRequest([80, 90, 100]);
        ObserverCapacityForecastResult reached = CapacityForecastAnalyser.Analyse(reachedRequest, ForecastPolicy());

        ObserverAnalysisRequest slowRequest = StorageRequest([10, 11, 12]);
        ObserverCapacityForecastResult outside = CapacityForecastAnalyser.Analyse(
            slowRequest,
            ForecastPolicy(forecastHorizon: TimeSpan.FromHours(24)));

        Assert.Equal("aiops.observer.forecast.capacity_reached", reached.Code);
        Assert.Equal(ObserverFindingDisposition.Detected, reached.Disposition);
        Assert.Equal("aiops.observer.forecast.capacity_outside_horizon", outside.Code);
        Assert.Equal(ObserverFindingDisposition.NotDetected, outside.Disposition);
        Assert.NotNull(outside.PredictedAt);

        ObserverCapacityForecastResult reachedFromOneSample = CapacityForecastAnalyser.Analyse(
            Request(
                [Sample(20, 100, metricKey: "storage.used.bytes", unit: "bytes")],
                Start),
            ForecastPolicy());
        Assert.Equal("aiops.observer.forecast.capacity_reached", reachedFromOneSample.Code);
        Assert.Equal(1, reachedFromOneSample.SampleCount);
        Assert.False(reachedFromOneSample.ModelApplied);
    }

    /// <summary>Verifies explicit uncertainty when the slope-sensitivity dates straddle the policy horizon.</summary>
    [Fact]
    public void CapacityForecastRefusesHorizonStraddledBySensitivityBounds()
    {
        ObserverAnalysisRequest request = StorageRequest([10, 20, 29, 41, 50]);

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(
            request,
            ForecastPolicy(minimumFitQuality: 0.8, forecastHorizon: TimeSpan.FromHours(5)));

        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition);
        Assert.Equal("aiops.observer.forecast.horizon_uncertain", result.Code);
        Assert.True(result.EarliestSensitivityAt <= Start.AddHours(9));
        Assert.True(result.LatestSensitivityAt > Start.AddHours(9));
    }

    /// <summary>Verifies refusal of missing, repeated-time and outdated statistical evidence.</summary>
    [Fact]
    public void CapacityForecastFailsClosedForInadequateEvidence()
    {
        ObserverCapacityForecastPolicy policy = ForecastPolicy();
        ObserverCapacityForecastResult missing = CapacityForecastAnalyser.Analyse(Request([], Start), policy);
        ObserverCapacityForecastResult duplicateTimestamp = CapacityForecastAnalyser.Analyse(
            Request(
            [
                Sample(1, 10, metricKey: "storage.used.bytes", unit: "bytes"),
                Sample(2, 20, metricKey: "storage.used.bytes", unit: "bytes"),
                Sample(3, 30, hours: 1, metricKey: "storage.used.bytes", unit: "bytes"),
            ],
            Start.AddHours(1)),
            policy);
        ObserverCapacityForecastResult stale = CapacityForecastAnalyser.Analyse(
            Request(
            [
                Sample(4, 10, hours: -5, metricKey: "storage.used.bytes", unit: "bytes"),
                Sample(5, 20, hours: -4, metricKey: "storage.used.bytes", unit: "bytes"),
                Sample(6, 30, hours: -3, metricKey: "storage.used.bytes", unit: "bytes"),
            ],
            Start),
            policy);

        Assert.Equal("aiops.observer.evidence.missing", missing.Code);
        Assert.Equal("aiops.observer.evidence.duplicate_timestamp", duplicateTimestamp.Code);
        Assert.Equal("aiops.observer.evidence.stale", stale.Code);
        Assert.All(
            [missing, duplicateTimestamp, stale],
            result => Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition));
    }

    /// <summary>Verifies that a point estimate already preceding the analysis instant is reported as conflicting.</summary>
    [Fact]
    public void CapacityForecastRefusesProjectionThatPredatesAnalysis()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 0, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(2, 40, hours: 1, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(3, 80, hours: 2, metricKey: "storage.used.bytes", unit: "bytes"),
        ],
        Start.AddHours(3));

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(
            request,
            ForecastPolicy(maximumSampleAge: TimeSpan.FromHours(2)));

        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition);
        Assert.Equal("aiops.observer.forecast.projection_conflict", result.Code);
    }

    /// <summary>Verifies that finite source values whose arithmetic would overflow fail with an explicit limitation.</summary>
    [Fact]
    public void CapacityForecastRefusesNonFiniteStatisticalIntermediates()
    {
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 1e308, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(2, -1e308, hours: 1, metricKey: "storage.used.bytes", unit: "bytes"),
            Sample(3, 1e308, hours: 2, metricKey: "storage.used.bytes", unit: "bytes"),
        ],
        Start.AddHours(2));
        ObserverCapacityForecastPolicy policy = ForecastPolicy(capacityLimit: double.MaxValue);

        ObserverCapacityForecastResult result = CapacityForecastAnalyser.Analyse(request, policy);

        Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, result.Disposition);
        Assert.Equal("aiops.observer.forecast.numeric_overflow", result.Code);
        Assert.All(
            new[]
            {
                result.GrowthPerHour,
                result.GrowthRateLowerBound95,
                result.GrowthRateUpperBound95,
                result.FitQuality,
                result.RootMeanSquaredError,
            },
            value => Assert.Null(value));
    }

    /// <summary>Verifies deterministic policy ordering and the hard no-LLM/no-recommendation/no-execution profile.</summary>
    [Fact]
    public void ObserverReportIsDeterministicAndCannotRecommendPlanOrExecute()
    {
        ObserverThresholdRule laterRule = ThresholdRule(ruleId: "z.resource.pressure");
        ObserverThresholdRule earlierRule = ThresholdRule(ruleId: "a.resource.pressure");
        ObserverAnalysisPolicy policy = new(
            [laterRule, earlierRule],
            [ForecastPolicy(policyId: "z.capacity"), ForecastPolicy(policyId: "a.capacity")]);
        ObserverAnalysisRequest request = Request(
        [
            Sample(1, 95),
            Sample(2, 96, hours: 0.25),
            Sample(3, 97, hours: 0.5),
        ],
        Start.AddHours(0.5));

        ObserverAnalysisReport report = ObserverAnalysisService.Analyse(request, policy);

        Assert.Equal("aiops.observer.analysis.v1", report.SchemaVersion);
        Assert.Equal("OBSERVER", report.Capabilities.Mode);
        Assert.False(report.Capabilities.CollectsEvidence);
        Assert.False(report.Capabilities.PersistsEvidence);
        Assert.False(report.Capabilities.UsesLlm);
        Assert.False(report.Capabilities.CanRecommend);
        Assert.False(report.Capabilities.CanPlan);
        Assert.False(report.Capabilities.CanExecute);
        Assert.Equal(["a.resource.pressure", "z.resource.pressure"], report.ThresholdResults.Select(result => result.RuleId));
        Assert.Equal(["a.capacity", "z.capacity"], report.CapacityForecasts.Select(result => result.PolicyId));
    }

    /// <summary>Verifies cancellation before bounded analyser work begins.</summary>
    [Fact]
    public void ObserverAnalysisHonoursCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => ObserverAnalysisService.Analyse(
            Request([], Start),
            new ObserverAnalysisPolicy([], []),
            cancellation.Token));
    }

    /// <summary>Verifies that duplicate policy identifiers are rejected before analysis.</summary>
    [Fact]
    public void ObserverPolicyRejectsAmbiguousIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new ObserverAnalysisPolicy(
            [ThresholdRule(), ThresholdRule()],
            []));
        Assert.Throws<ArgumentException>(() => new ObserverAnalysisPolicy(
            [],
            [ForecastPolicy(), ForecastPolicy()]));
        Assert.Throws<ArgumentException>(() => new ObserverAnalysisPolicy(
            [null!],
            []));
        Assert.Throws<ArgumentException>(() => new ObserverAnalysisPolicy(
            [],
            [null!]));
    }

    /// <summary>Verifies real enumeration limits even when a custom collection reports a false count.</summary>
    [Fact]
    public void ObserverContractsDoNotTrustCollectionCount()
    {
        ObserverMetricSample repeated = Sample(1, 1);
        LyingReadOnlyCollection<ObserverMetricSample> excessiveSamples = new(
            Enumerable.Repeat(repeated, ObserverAnalysisRequest.MaximumSampleCount + 1));
        LyingReadOnlyCollection<ObserverThresholdRule> excessiveRules = new(
            Enumerable.Repeat(ThresholdRule(), ObserverAnalysisPolicy.MaximumPolicyCount + 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => Request(excessiveSamples));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObserverAnalysisPolicy(excessiveRules, []));
    }

    /// <summary>Verifies safe lookback underflow and validity overflow at the DateTimeOffset boundaries.</summary>
    [Fact]
    public void ObserverTemporalBoundsSaturateSafely()
    {
        ObserverMetricSample earliest = Sample(
            1,
            95,
            observedAt: DateTimeOffset.MinValue,
            receivedAt: DateTimeOffset.MinValue);
        ObserverThresholdRule threshold = new(
            "resource.pressure",
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThanOrEqual,
            90,
            ObserverFindingSeverity.Warning,
            1,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));
        ObserverThresholdResult atMinimum = DeterministicThresholdAnalyser.Analyse(
            Request([earliest], DateTimeOffset.MinValue),
            threshold);

        ObserverMetricSample latest = Sample(
            2,
            100,
            metricKey: "storage.used.bytes",
            unit: "bytes",
            observedAt: DateTimeOffset.MaxValue,
            receivedAt: DateTimeOffset.MaxValue);
        ObserverCapacityForecastResult atMaximum = CapacityForecastAnalyser.Analyse(
            Request([latest], DateTimeOffset.MaxValue),
            ForecastPolicy());

        Assert.Equal(DateTimeOffset.MinValue.AddHours(1), atMinimum.ValidUntil);
        Assert.Equal(DateTimeOffset.MaxValue, atMaximum.ValidUntil);
    }

    /// <summary>Builds a deterministic storage-series request with one-hour sample spacing.</summary>
    /// <param name="values">Ordered storage values.</param>
    /// <returns>A scope-bound request evaluated at the final sample instant.</returns>
    private static ObserverAnalysisRequest StorageRequest(IReadOnlyList<double> values)
    {
        ObserverMetricSample[] samples = values
            .Select((value, index) => Sample(
                index + 1,
                value,
                hours: index,
                metricKey: "storage.used.bytes",
                unit: "bytes"))
            .ToArray();
        return Request(samples, Start.AddHours(values.Count - 1));
    }

    /// <summary>Builds one single-instance, single-scope analysis request.</summary>
    /// <param name="samples">Sanitised fixture evidence.</param>
    /// <param name="asOf">Optional authoritative analysis instant.</param>
    /// <returns>A validated deterministic request.</returns>
    private static ObserverAnalysisRequest Request(
        IReadOnlyCollection<ObserverMetricSample> samples,
        DateTimeOffset? asOf = null) =>
        new(Guid.Parse("33333333-3333-3333-3333-333333333333"), InstanceId, ScopeId, asOf ?? Start, samples);

    /// <summary>Builds one explicitly sanitised numeric evidence fixture.</summary>
    /// <param name="ordinal">Stable evidence identifier ordinal.</param>
    /// <param name="value">Finite metric value.</param>
    /// <param name="hours">Offset from the fixture origin.</param>
    /// <param name="metricKey">Provider-neutral metric identifier.</param>
    /// <param name="unit">Canonical metric unit.</param>
    /// <param name="quality">Declared evidence quality.</param>
    /// <param name="sourceKind">Canonical source kind.</param>
    /// <param name="schemaVersion">Canonical schema version.</param>
    /// <param name="transformationVersion">Sanitising transformation version.</param>
    /// <param name="observedAt">Optional absolute observation instant overriding <paramref name="hours"/>.</param>
    /// <param name="receivedAt">Optional receipt instant override.</param>
    /// <param name="instanceId">Optional instance-boundary override.</param>
    /// <param name="scopeId">Optional authorisation-boundary override.</param>
    /// <param name="missingness">Declared present-value state.</param>
    /// <param name="cardinality">Declared single-series cardinality.</param>
    /// <param name="classification">Declared operational classification.</param>
    /// <param name="redactionStatus">Declared sanitisation result.</param>
    /// <param name="retentionClass">Declared ephemeral retention class.</param>
    /// <param name="permittedPurpose">Declared non-mutating purpose.</param>
    /// <returns>One immutable metric evidence fixture.</returns>
    private static ObserverMetricSample Sample(
        int ordinal,
        double value,
        double hours = 0,
        string metricKey = "resource.usage.percent",
        string unit = "percent",
        ObserverEvidenceQuality quality = ObserverEvidenceQuality.Verified,
        string sourceKind = "canonical.metric",
        string schemaVersion = "1.0.0",
        string transformationVersion = "1.0.0",
        DateTimeOffset? observedAt = null,
        DateTimeOffset? receivedAt = null,
        Guid? instanceId = null,
        Guid? scopeId = null,
        ObserverMissingness missingness = ObserverMissingness.Present,
        ObserverMetricCardinality cardinality = ObserverMetricCardinality.SingleSeries,
        ObserverDataClassification classification = ObserverDataClassification.OperationalTelemetry,
        ObserverRedactionStatus redactionStatus = ObserverRedactionStatus.Sanitised,
        ObserverRetentionClass retentionClass = ObserverRetentionClass.EphemeralAnalysisOnly,
        ObserverPermittedPurpose permittedPurpose = ObserverPermittedPurpose.NonMutatingObserverAnalysis)
    {
        DateTimeOffset observationInstant = observedAt ?? Start.AddHours(hours);
        return new ObserverMetricSample(
            EvidenceId(ordinal),
            instanceId ?? InstanceId,
            scopeId ?? ScopeId,
            metricKey,
            unit,
            value,
            observationInstant,
            receivedAt ?? observationInstant,
            quality,
            missingness,
            cardinality,
            sourceKind,
            schemaVersion,
            transformationVersion,
            classification,
            redactionStatus,
            retentionClass,
            permittedPurpose);
    }

    /// <summary>Creates a stable non-empty evidence identifier from a small fixture ordinal.</summary>
    /// <param name="ordinal">Small positive fixture ordinal.</param>
    /// <returns>A stable non-empty GUID.</returns>
    private static Guid EvidenceId(int ordinal) =>
        Guid.Parse($"44444444-4444-4444-4444-{ordinal:D12}");

    /// <summary>Builds the standard three-sample pressure rule used by threshold regressions.</summary>
    /// <param name="ruleId">Optional stable rule identifier.</param>
    /// <returns>A bounded threshold rule.</returns>
    private static ObserverThresholdRule ThresholdRule(string ruleId = "resource.pressure") =>
        new(
            ruleId,
            "1.0.0",
            "resource.usage.percent",
            "percent",
            ObserverThresholdComparison.GreaterThanOrEqual,
            90,
            ObserverFindingSeverity.Critical,
            3,
            TimeSpan.FromHours(4),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

    /// <summary>Builds a bounded capacity forecast policy for storage fixtures.</summary>
    /// <param name="policyId">Stable policy identifier.</param>
    /// <param name="minimumFitQuality">Minimum accepted coefficient of determination.</param>
    /// <param name="forecastHorizon">Optional future horizon.</param>
    /// <param name="maximumSampleAge">Optional newest-sample freshness limit.</param>
    /// <param name="capacityLimit">Finite storage boundary.</param>
    /// <param name="minimumGrowthPerHour">Minimum meaningful hourly growth.</param>
    /// <returns>A bounded capacity forecast policy.</returns>
    private static ObserverCapacityForecastPolicy ForecastPolicy(
        string policyId = "storage.capacity",
        double minimumFitQuality = 0.8,
        TimeSpan? forecastHorizon = null,
        TimeSpan? maximumSampleAge = null,
        double capacityLimit = 100,
        double minimumGrowthPerHour = 0) =>
        new(
            policyId,
            "1.0.0",
            "storage.used.bytes",
            "bytes",
            capacityLimit,
            ObserverFindingSeverity.Warning,
            3,
            TimeSpan.FromDays(30),
            maximumSampleAge ?? TimeSpan.FromHours(2),
            forecastHorizon ?? TimeSpan.FromDays(30),
            minimumFitQuality,
            minimumGrowthPerHour);

    /// <summary>Supplies a deliberately false count while preserving the wrapped enumeration for boundary tests.</summary>
    /// <typeparam name="T">Fixture element type.</typeparam>
    /// <param name="items">Wrapped sequence whose true enumeration exceeds the reported count.</param>
    private sealed class LyingReadOnlyCollection<T>(IEnumerable<T> items) : IReadOnlyCollection<T>
    {
        /// <summary>Gets a deliberately incorrect count used to verify bounded materialisation.</summary>
        public int Count => 0;

        /// <summary>Returns the wrapped generic enumerator.</summary>
        /// <returns>The wrapped sequence enumerator.</returns>
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

        /// <summary>Returns the wrapped non-generic enumerator.</summary>
        /// <returns>The wrapped sequence enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
