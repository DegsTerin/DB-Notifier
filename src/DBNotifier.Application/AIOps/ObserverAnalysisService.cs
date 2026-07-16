// Module purpose: Orchestrates the inactive MOD-12 Observer foundation as a bounded, deterministic and non-mutating in-memory analysis.
namespace DBNotifier.Application.AIOps;

/// <summary>Reports the fixed capability restrictions of the initial Observer foundation.</summary>
public sealed class ObserverCapabilityProfile
{
    /// <summary>Initialises the fixed, non-mutating capability profile.</summary>
    internal ObserverCapabilityProfile()
    {
    }

    /// <summary>Gets the only implemented analysis mode.</summary>
    public string Mode { get; } = "OBSERVER";

    /// <summary>Gets a value indicating that this foundation never collects evidence by itself.</summary>
    public bool CollectsEvidence { get; }

    /// <summary>Gets a value indicating that this foundation performs no persistence.</summary>
    public bool PersistsEvidence { get; }

    /// <summary>Gets a value indicating that this foundation has no LLM integration.</summary>
    public bool UsesLlm { get; }

    /// <summary>Gets a value indicating that this foundation cannot issue recommendations.</summary>
    public bool CanRecommend { get; }

    /// <summary>Gets a value indicating that this foundation cannot create action plans.</summary>
    public bool CanPlan { get; }

    /// <summary>Gets a value indicating that this foundation has no command or executor access.</summary>
    public bool CanExecute { get; }
}

/// <summary>Defines a bounded collection of deterministic Observer rules and forecast policies.</summary>
public sealed class ObserverAnalysisPolicy
{
    /// <summary>Maximum number of policies accepted for each analyser type in one report.</summary>
    public const int MaximumPolicyCount = 100;

    /// <summary>Initialises one immutable analysis policy.</summary>
    /// <param name="thresholdRules">Zero to <see cref="MaximumPolicyCount"/> threshold rules with unique identifiers.</param>
    /// <param name="capacityForecasts">Zero to <see cref="MaximumPolicyCount"/> capacity policies with unique identifiers.</param>
    /// <exception cref="ArgumentNullException">Thrown when either policy collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when identifiers are duplicated within an analyser type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either policy collection exceeds its bound.</exception>
    public ObserverAnalysisPolicy(
        IReadOnlyCollection<ObserverThresholdRule> thresholdRules,
        IReadOnlyCollection<ObserverCapacityForecastPolicy> capacityForecasts)
    {
        ArgumentNullException.ThrowIfNull(thresholdRules);
        ArgumentNullException.ThrowIfNull(capacityForecasts);
        ObserverThresholdRule?[] boundedRules = thresholdRules.Take(MaximumPolicyCount + 1).ToArray();
        ObserverCapacityForecastPolicy?[] boundedForecasts = capacityForecasts.Take(MaximumPolicyCount + 1).ToArray();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(boundedRules.Length, MaximumPolicyCount, nameof(thresholdRules));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            boundedForecasts.Length,
            MaximumPolicyCount,
            nameof(capacityForecasts));

        if (boundedRules.Any(rule => rule is null))
        {
            throw new ArgumentException("Observer threshold policies cannot contain null items.", nameof(thresholdRules));
        }

        if (boundedForecasts.Any(policy => policy is null))
        {
            throw new ArgumentException("Observer capacity policies cannot contain null items.", nameof(capacityForecasts));
        }

        ObserverThresholdRule[] copiedRules = boundedRules
            .Select(rule => rule!)
            .OrderBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ThenBy(rule => rule.RuleVersion, StringComparer.Ordinal)
            .ToArray();
        ObserverCapacityForecastPolicy[] copiedForecasts = boundedForecasts
            .Select(policy => policy!)
            .OrderBy(policy => policy.PolicyId, StringComparer.Ordinal)
            .ThenBy(policy => policy.PolicyVersion, StringComparer.Ordinal)
            .ToArray();
        if (copiedRules.Select(rule => rule.RuleId).Distinct(StringComparer.Ordinal).Count() != copiedRules.Length)
        {
            throw new ArgumentException("Observer threshold rule identifiers must be unique.", nameof(thresholdRules));
        }

        if (copiedForecasts.Select(policy => policy.PolicyId).Distinct(StringComparer.Ordinal).Count() != copiedForecasts.Length)
        {
            throw new ArgumentException(
                "Observer capacity policy identifiers must be unique.",
                nameof(capacityForecasts));
        }

        ThresholdRules = Array.AsReadOnly(copiedRules);
        CapacityForecasts = Array.AsReadOnly(copiedForecasts);
    }

    /// <summary>Gets threshold rules in deterministic identifier order.</summary>
    public IReadOnlyList<ObserverThresholdRule> ThresholdRules { get; }

    /// <summary>Gets capacity policies in deterministic identifier order.</summary>
    public IReadOnlyList<ObserverCapacityForecastPolicy> CapacityForecasts { get; }
}

/// <summary>
/// Summarises one deterministic in-memory Observer analysis. The report exposes no recommendation, plan, command,
/// credential, provider, persistence or executable payload surface.
/// </summary>
public sealed class ObserverAnalysisReport
{
    /// <summary>Initialises an immutable report from one validated request and its deterministic outcomes.</summary>
    /// <param name="request">Scope-bound request whose identity and analysis instant are retained.</param>
    /// <param name="thresholdResults">Deterministic threshold outcomes in policy order.</param>
    /// <param name="capacityForecasts">Capacity outcomes in policy order.</param>
    internal ObserverAnalysisReport(
        ObserverAnalysisRequest request,
        IReadOnlyList<ObserverThresholdResult> thresholdResults,
        IReadOnlyList<ObserverCapacityForecastResult> capacityForecasts)
    {
        AnalysisId = request.AnalysisId;
        InstanceId = request.InstanceId;
        AuthorisationScopeId = request.AuthorisationScopeId;
        AnalysedAt = request.AsOf;
        ThresholdResults = thresholdResults;
        CapacityForecasts = capacityForecasts;
        Capabilities = new ObserverCapabilityProfile();
    }

    /// <summary>Gets the versioned report schema.</summary>
    public string SchemaVersion { get; } = "aiops.observer.analysis.v1";

    /// <summary>Gets the deterministic report correlation identifier.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets the analysed database-instance identifier.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorisation scope under which the evidence was analysed.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the authoritative analysis instant in UTC.</summary>
    public DateTimeOffset AnalysedAt { get; }

    /// <summary>Gets the immutable deterministic threshold outcomes.</summary>
    public IReadOnlyList<ObserverThresholdResult> ThresholdResults { get; }

    /// <summary>Gets the immutable explainable capacity forecasts.</summary>
    public IReadOnlyList<ObserverCapacityForecastResult> CapacityForecasts { get; }

    /// <summary>Gets the non-mutating capability declaration attached to every report.</summary>
    public ObserverCapabilityProfile Capabilities { get; }
}

/// <summary>Runs bounded Observer analysers over caller-supplied sanitised evidence without side effects.</summary>
public static class ObserverAnalysisService
{
    /// <summary>Produces one deterministic report using only the supplied in-memory request and policy.</summary>
    /// <param name="request">Validated single-instance evidence request.</param>
    /// <param name="policy">Bounded deterministic rules and statistical policies.</param>
    /// <param name="cancellationToken">Cancellation checked between bounded analyser invocations.</param>
    /// <returns>A versioned Observer-only report with explicit capability restrictions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or <paramref name="policy"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested between analyser invocations.</exception>
    public static ObserverAnalysisReport Analyse(
        ObserverAnalysisRequest request,
        ObserverAnalysisPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policy);
        cancellationToken.ThrowIfCancellationRequested();

        List<ObserverThresholdResult> thresholdResults = new(policy.ThresholdRules.Count);
        foreach (ObserverThresholdRule rule in policy.ThresholdRules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            thresholdResults.Add(DeterministicThresholdAnalyser.Analyse(request, rule));
        }

        List<ObserverCapacityForecastResult> forecastResults = new(policy.CapacityForecasts.Count);
        foreach (ObserverCapacityForecastPolicy forecast in policy.CapacityForecasts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            forecastResults.Add(CapacityForecastAnalyser.Analyse(request, forecast));
        }

        return new ObserverAnalysisReport(
            request,
            thresholdResults.AsReadOnly(),
            forecastResults.AsReadOnly());
    }
}
