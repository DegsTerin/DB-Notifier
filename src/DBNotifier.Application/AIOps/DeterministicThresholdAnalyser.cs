// Module purpose: Evaluates versioned threshold rules over sanitised Observer evidence without recommendation, planning or mutation.
namespace DBNotifier.Application.AIOps;

/// <summary>Defines the supported deterministic numeric comparisons.</summary>
public enum ObserverThresholdComparison
{
    /// <summary>The sample must be strictly greater than the threshold.</summary>
    GreaterThan,

    /// <summary>The sample must be greater than or equal to the threshold.</summary>
    GreaterThanOrEqual,

    /// <summary>The sample must be strictly less than the threshold.</summary>
    LessThan,

    /// <summary>The sample must be less than or equal to the threshold.</summary>
    LessThanOrEqual,
}

/// <summary>Classifies the factual importance of a non-mutating Observer finding.</summary>
public enum ObserverFindingSeverity
{
    /// <summary>Informational evidence that does not imply a fault.</summary>
    Information,

    /// <summary>Evidence requiring attention without declaring a critical condition.</summary>
    Warning,

    /// <summary>Evidence matching a rule classified as critical by deterministic policy.</summary>
    Critical,
}

/// <summary>Distinguishes a detected signal, an evaluated absence and an analysis blocked by insufficient evidence.</summary>
public enum ObserverFindingDisposition
{
    /// <summary>The configured deterministic condition was detected.</summary>
    Detected,

    /// <summary>Sufficient evidence was evaluated and the configured condition was not detected.</summary>
    NotDetected,

    /// <summary>The engine refused to claim either outcome because the evidence was inadequate.</summary>
    InsufficientEvidence,
}

/// <summary>Defines one bounded and versioned deterministic threshold rule.</summary>
public sealed class ObserverThresholdRule
{
    /// <summary>Initialises one deterministic rule without any execution authority.</summary>
    /// <param name="ruleId">Stable lower-case rule identifier.</param>
    /// <param name="ruleVersion">Stable version identifier.</param>
    /// <param name="metricKey">Provider-neutral metric identifier.</param>
    /// <param name="unit">Canonical unit required by the rule.</param>
    /// <param name="comparison">Numeric comparison applied to each selected sample.</param>
    /// <param name="threshold">Finite threshold value.</param>
    /// <param name="severity">Deterministic severity assigned when the rule is detected.</param>
    /// <param name="requiredConsecutiveSamples">Number of consecutive matching samples, from one to sixty.</param>
    /// <param name="evaluationWindow">Positive evidence lookback no greater than thirty days.</param>
    /// <param name="maximumSampleAge">Positive freshness limit no greater than thirty days.</param>
    /// <param name="maximumSampleGap">Positive maximum interval between consecutive evidence items.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required identifier is null.</exception>
    /// <exception cref="ArgumentException">Thrown for unsafe identifiers or a non-finite threshold.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an enum or bound is outside policy.</exception>
    public ObserverThresholdRule(
        string ruleId,
        string ruleVersion,
        string metricKey,
        string unit,
        ObserverThresholdComparison comparison,
        double threshold,
        ObserverFindingSeverity severity,
        int requiredConsecutiveSamples,
        TimeSpan evaluationWindow,
        TimeSpan maximumSampleAge,
        TimeSpan maximumSampleGap)
    {
        if (!Enum.IsDefined(comparison))
        {
            throw new ArgumentOutOfRangeException(nameof(comparison));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        if (!double.IsFinite(threshold))
        {
            throw new ArgumentException("Observer thresholds must be finite.", nameof(threshold));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(requiredConsecutiveSamples, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(requiredConsecutiveSamples, 60);
        ObserverTimeBounds.PositiveAtMost(evaluationWindow, TimeSpan.FromDays(30), nameof(evaluationWindow));
        ObserverTimeBounds.PositiveAtMost(maximumSampleAge, evaluationWindow, nameof(maximumSampleAge));
        ObserverTimeBounds.PositiveAtMost(maximumSampleGap, evaluationWindow, nameof(maximumSampleGap));

        RuleId = ObserverContractGuard.StableIdentifier(ruleId, nameof(ruleId));
        RuleVersion = ObserverContractGuard.StableIdentifier(ruleVersion, nameof(ruleVersion));
        MetricKey = ObserverContractGuard.StableIdentifier(metricKey, nameof(metricKey));
        Unit = ObserverContractGuard.StableIdentifier(unit, nameof(unit));
        Comparison = comparison;
        Threshold = threshold;
        Severity = severity;
        RequiredConsecutiveSamples = requiredConsecutiveSamples;
        EvaluationWindow = evaluationWindow;
        MaximumSampleAge = maximumSampleAge;
        MaximumSampleGap = maximumSampleGap;
    }

    /// <summary>Gets the stable rule identifier.</summary>
    public string RuleId { get; }

    /// <summary>Gets the rule version used to reproduce the result.</summary>
    public string RuleVersion { get; }

    /// <summary>Gets the provider-neutral metric identifier.</summary>
    public string MetricKey { get; }

    /// <summary>Gets the required canonical unit.</summary>
    public string Unit { get; }

    /// <summary>Gets the deterministic comparison.</summary>
    public ObserverThresholdComparison Comparison { get; }

    /// <summary>Gets the finite threshold value.</summary>
    public double Threshold { get; }

    /// <summary>Gets the severity assigned by policy when detected.</summary>
    public ObserverFindingSeverity Severity { get; }

    /// <summary>Gets the debounce count required for detection.</summary>
    public int RequiredConsecutiveSamples { get; }

    /// <summary>Gets the bounded evidence lookback independently of newest-sample freshness.</summary>
    public TimeSpan EvaluationWindow { get; }

    /// <summary>Gets the newest-sample freshness limit.</summary>
    public TimeSpan MaximumSampleAge { get; }

    /// <summary>Gets the maximum interval allowed between selected samples.</summary>
    public TimeSpan MaximumSampleGap { get; }
}

/// <summary>Returns the reproducible outcome of one deterministic threshold rule.</summary>
public sealed class ObserverThresholdResult
{
    /// <summary>Initialises an immutable threshold result from a validated rule and evidence outcome.</summary>
    /// <param name="rule">Rule whose identity, version and policy values are retained.</param>
    /// <param name="request">Scope-bound request whose identity and analysis instant are retained.</param>
    /// <param name="disposition">Factual detected, not-detected or insufficient disposition.</param>
    /// <param name="code">Stable non-secret outcome code.</param>
    /// <param name="observedAt">Newest selected evidence instant, when usable.</param>
    /// <param name="observedValue">Newest selected value, when usable.</param>
    /// <param name="evidenceIds">Immutable evidence links.</param>
    /// <param name="limitationCodes">Immutable factual limitations.</param>
    internal ObserverThresholdResult(
        ObserverThresholdRule rule,
        ObserverAnalysisRequest request,
        ObserverFindingDisposition disposition,
        string code,
        DateTimeOffset? observedAt,
        double? observedValue,
        IReadOnlyList<Guid> evidenceIds,
        IReadOnlyList<string> limitationCodes)
    {
        RuleId = rule.RuleId;
        RuleVersion = rule.RuleVersion;
        AnalysisId = request.AnalysisId;
        InstanceId = request.InstanceId;
        AuthorisationScopeId = request.AuthorisationScopeId;
        MetricKey = rule.MetricKey;
        Unit = rule.Unit;
        EvaluatedAt = request.AsOf;
        Disposition = disposition;
        Code = code;
        PolicySeverity = rule.Severity;
        ObservedAt = observedAt;
        ObservedValue = observedValue;
        ValidUntil = observedAt is { } observed
            ? ObserverTimeBounds.AddOrMaximum(observed, rule.MaximumSampleAge)
            : null;
        Threshold = rule.Threshold;
        EvidenceIds = evidenceIds;
        LimitationCodes = limitationCodes;
    }

    /// <summary>Gets the stable rule identifier.</summary>
    public string RuleId { get; }

    /// <summary>Gets the exact rule version evaluated.</summary>
    public string RuleVersion { get; }

    /// <summary>Gets the analysis correlation identifier retained from the scope-bound request.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets the database-instance identifier.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorisation scope under which the evidence was analysed.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the provider-neutral metric identifier.</summary>
    public string MetricKey { get; }

    /// <summary>Gets the canonical unit.</summary>
    public string Unit { get; }

    /// <summary>Gets the analysis instant in UTC.</summary>
    public DateTimeOffset EvaluatedAt { get; }

    /// <summary>Gets the factual analysis disposition.</summary>
    public ObserverFindingDisposition Disposition { get; }

    /// <summary>Gets the stable, non-secret outcome code.</summary>
    public string Code { get; }

    /// <summary>Gets the policy severity that applies only when <see cref="Disposition"/> is detected.</summary>
    public ObserverFindingSeverity PolicySeverity { get; }

    /// <summary>Gets the newest selected observation instant, when available.</summary>
    public DateTimeOffset? ObservedAt { get; }

    /// <summary>Gets the newest selected value, when available.</summary>
    public double? ObservedValue { get; }

    /// <summary>Gets the instant after which this result is stale unless the rule is analysed again.</summary>
    public DateTimeOffset? ValidUntil { get; }

    /// <summary>Gets the configured rule threshold.</summary>
    public double Threshold { get; }

    /// <summary>Gets immutable links to evidence used or rejected.</summary>
    public IReadOnlyList<Guid> EvidenceIds { get; }

    /// <summary>Gets stable limitations that qualify the result.</summary>
    public IReadOnlyList<string> LimitationCodes { get; }
}

/// <summary>Evaluates deterministic threshold rules over sanitised evidence without side effects.</summary>
public static class DeterministicThresholdAnalyser
{
    /// <summary>Evaluates one rule at the request's authoritative instant.</summary>
    /// <param name="request">Validated, single-instance Observer analysis request.</param>
    /// <param name="rule">Versioned deterministic rule.</param>
    /// <returns>A detected, not-detected or insufficient-evidence result with evidence links.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or <paramref name="rule"/> is null.</exception>
    public static ObserverThresholdResult Analyse(ObserverAnalysisRequest request, ObserverThresholdRule rule)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rule);

        ObserverEvidenceSelection selection = ObserverEvidenceWindow.Select(
            request,
            rule.MetricKey,
            rule.Unit,
            rule.EvaluationWindow,
            rule.MaximumSampleAge);
        if (!selection.IsUsable)
        {
            return Insufficient(
                request,
                rule,
                selection.FailureCode!,
                selection.EvidenceIds,
                selection.LimitationCodes);
        }

        if (selection.Samples.Count < rule.RequiredConsecutiveSamples)
        {
            return Insufficient(
                request,
                rule,
                "aiops.observer.threshold.insufficient_samples",
                selection.EvidenceIds,
                [.. selection.LimitationCodes, "aiops.observer.threshold.insufficient_samples"],
                selection.Samples[^1]);
        }

        ObserverMetricSample[] selected = selection.Samples
            .TakeLast(rule.RequiredConsecutiveSamples)
            .ToArray();
        IReadOnlyList<string> selectedLimitations = SelectedLimitations(selected);
        for (int index = 1; index < selected.Length; index++)
        {
            if (selected[index].ObservedAt - selected[index - 1].ObservedAt > rule.MaximumSampleGap)
            {
                return Insufficient(
                    request,
                    rule,
                    "aiops.observer.threshold.sample_gap",
                    selected.Select(sample => sample.EvidenceId),
                    [.. selectedLimitations, "aiops.observer.threshold.sample_gap"],
                    selected[^1]);
            }
        }

        bool detected = selected.All(sample => Matches(sample.Value, rule.Comparison, rule.Threshold));
        ObserverMetricSample latest = selected[^1];
        return new ObserverThresholdResult(
            rule,
            request,
            detected ? ObserverFindingDisposition.Detected : ObserverFindingDisposition.NotDetected,
            detected ? "aiops.observer.threshold.detected" : "aiops.observer.threshold.not_detected",
            latest.ObservedAt,
            latest.Value,
            Array.AsReadOnly(selected.Select(sample => sample.EvidenceId).ToArray()),
            selectedLimitations);
    }

    /// <summary>Creates an immutable insufficient-evidence result with stable ordering.</summary>
    /// <param name="request">Validated analysis boundary.</param>
    /// <param name="rule">Rule that could not be evaluated conclusively.</param>
    /// <param name="code">Stable insufficiency code.</param>
    /// <param name="evidenceIds">Evidence links relevant to the refusal.</param>
    /// <param name="limitationCodes">Stable limitations explaining the refusal.</param>
    /// <param name="latest">Newest usable evidence item, when one exists despite the refusal.</param>
    /// <returns>An insufficient-evidence threshold result.</returns>
    private static ObserverThresholdResult Insufficient(
        ObserverAnalysisRequest request,
        ObserverThresholdRule rule,
        string code,
        IEnumerable<Guid> evidenceIds,
        IEnumerable<string> limitationCodes,
        ObserverMetricSample? latest = null) =>
        new(
            rule,
            request,
            ObserverFindingDisposition.InsufficientEvidence,
            code,
            latest?.ObservedAt,
            latest?.Value,
            Array.AsReadOnly(evidenceIds.Distinct().Order().ToArray()),
            Array.AsReadOnly(limitationCodes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));

    /// <summary>Derives limitations only from evidence that is linked to the threshold result.</summary>
    /// <param name="selected">Exact debounce evidence returned to the consumer.</param>
    /// <returns>An immutable list of limitations attributable to the selected evidence.</returns>
    private static IReadOnlyList<string> SelectedLimitations(IReadOnlyList<ObserverMetricSample> selected) =>
        selected.Any(sample => sample.Quality == ObserverEvidenceQuality.Degraded)
            ? Array.AsReadOnly(new[] { ObserverEvidenceWindow.DegradedEvidence })
            : Array.Empty<string>();

    /// <summary>Applies one validated deterministic comparison to a finite value.</summary>
    /// <param name="value">Finite evidence value.</param>
    /// <param name="comparison">Validated typed comparison.</param>
    /// <param name="threshold">Finite rule threshold.</param>
    /// <returns><see langword="true"/> when the configured relation holds.</returns>
    private static bool Matches(double value, ObserverThresholdComparison comparison, double threshold) =>
        comparison switch
        {
            ObserverThresholdComparison.GreaterThan => value > threshold,
            ObserverThresholdComparison.GreaterThanOrEqual => value >= threshold,
            ObserverThresholdComparison.LessThan => value < threshold,
            ObserverThresholdComparison.LessThanOrEqual => value <= threshold,
            _ => false,
        };
}

/// <summary>Validates bounded durations shared by Observer policies.</summary>
internal static class ObserverTimeBounds
{
    /// <summary>Requires a positive duration no greater than the supplied policy maximum.</summary>
    /// <param name="value">Duration to validate.</param>
    /// <param name="maximum">Inclusive upper bound.</param>
    /// <param name="parameterName">Public parameter name used by any resulting exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value falls outside the bounds.</exception>
    public static void PositiveAtMost(TimeSpan value, TimeSpan maximum, string parameterName)
    {
        if (value <= TimeSpan.Zero || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    /// <summary>Subtracts a bounded duration, saturating at the minimum representable instant.</summary>
    /// <param name="value">UTC-compatible source instant.</param>
    /// <param name="duration">Positive duration to subtract.</param>
    /// <returns>The exact result or <see cref="DateTimeOffset.MinValue"/> when subtraction would underflow.</returns>
    public static DateTimeOffset SubtractOrMinimum(DateTimeOffset value, TimeSpan duration)
    {
        try
        {
            return value - duration;
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>Adds a bounded duration, saturating at the maximum representable instant.</summary>
    /// <param name="value">UTC-compatible source instant.</param>
    /// <param name="duration">Positive duration to add.</param>
    /// <returns>The exact result or <see cref="DateTimeOffset.MaxValue"/> when addition would overflow.</returns>
    public static DateTimeOffset AddOrMaximum(DateTimeOffset value, TimeSpan duration)
    {
        try
        {
            return value + duration;
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.MaxValue;
        }
    }
}
