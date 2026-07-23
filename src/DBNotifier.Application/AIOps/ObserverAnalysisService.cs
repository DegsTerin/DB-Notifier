// Module purpose: Orchestrates the inactive MOD-12 Observer foundation as a bounded, deterministic and non-mutating in-memory analysis.
namespace DBNotifier.Application.AIOps;

/// <summary>Identifies whether the inactive MOD-12 foundation has been promoted into an operational mode.</summary>
public enum ObserverActivationState
{
    /// <summary>No MOD-12 mode is active; this is the only state exposed by the inactive foundation.</summary>
    None,
}

/// <summary>Classifies whether a bounded analysis completed or was refused before publication.</summary>
public enum ObserverAnalysisDisposition
{
    /// <summary>All bounded analysers completed and the publication boundary remained current.</summary>
    Completed = 1,

    /// <summary>Cancellation was observed before safe publication.</summary>
    Cancelled = 2,

    /// <summary>The absolute execution deadline elapsed before safe publication.</summary>
    Expired = 3,

    /// <summary>The publication context no longer matched the admitted evidence context.</summary>
    StaleContext = 4,
}

/// <summary>Reports the fixed capability restrictions of the initial Observer foundation.</summary>
public sealed class ObserverCapabilityProfile
{
    /// <summary>Initialises the fixed, non-mutating capability profile.</summary>
    internal ObserverCapabilityProfile()
    {
    }

    /// <summary>Gets the implemented, non-activating analysis capability.</summary>
    public string Capability { get; } = "observer-analysis";

    /// <summary>Gets the explicit absence of MOD-12 mode activation.</summary>
    public ObserverActivationState ActivationState { get; } = ObserverActivationState.None;

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

/// <summary>
/// Defines the absolute publication boundary for one in-memory analysis without creating a runtime, trust host or
/// persistence surface.
/// </summary>
public sealed class ObserverAnalysisExecutionContext
{
    /// <summary>Initialises one immutable, deadline-bound publication context.</summary>
    /// <param name="deadline">Exclusive absolute UTC deadline.</param>
    /// <param name="evidenceContextRevision">Revision bound to the admitted evidence.</param>
    /// <param name="publicationContextRevision">Revision current at the publication boundary.</param>
    /// <param name="timeProvider">Clock used only for deadline validation.</param>
    /// <exception cref="ArgumentException">Thrown for a default, non-UTC or unsafe context value.</exception>
    public ObserverAnalysisExecutionContext(
        DateTimeOffset deadline,
        string evidenceContextRevision,
        string publicationContextRevision,
        TimeProvider? timeProvider = null)
    {
        if (deadline == default || deadline.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observer analysis deadlines must be explicit UTC values.", nameof(deadline));
        }

        Deadline = deadline;
        EvidenceContextRevision = ObserverContractGuard.StableIdentifier(
            evidenceContextRevision,
            nameof(evidenceContextRevision));
        PublicationContextRevision = ObserverContractGuard.StableIdentifier(
            publicationContextRevision,
            nameof(publicationContextRevision));
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the exclusive absolute UTC deadline.</summary>
    public DateTimeOffset Deadline { get; }

    /// <summary>Gets the revision bound to admitted evidence.</summary>
    public string EvidenceContextRevision { get; }

    /// <summary>Gets the revision asserted immediately before publication.</summary>
    public string PublicationContextRevision { get; }

    /// <summary>Gets the clock used only to validate the absolute deadline.</summary>
    internal TimeProvider TimeProvider { get; }
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
    /// <param name="disposition">Completed or fail-closed terminal disposition.</param>
    /// <param name="code">Stable sanitised publication code.</param>
    /// <param name="thresholdResults">Complete threshold outcomes, or an empty collection when not completed.</param>
    /// <param name="capacityForecasts">Complete capacity outcomes, or an empty collection when not completed.</param>
    internal ObserverAnalysisReport(
        ObserverAnalysisRequest request,
        ObserverAnalysisDisposition disposition,
        string code,
        IReadOnlyList<ObserverThresholdResult> thresholdResults,
        IReadOnlyList<ObserverCapacityForecastResult> capacityForecasts)
    {
        AnalysisId = request.AnalysisId;
        InstanceId = request.InstanceId;
        AuthorisationScopeId = request.AuthorisationScopeId;
        AnalysedAt = request.AsOf;
        Disposition = disposition;
        Code = code;
        IsComplete = disposition == ObserverAnalysisDisposition.Completed;
        ThresholdResults = IsComplete ? thresholdResults : [];
        CapacityForecasts = IsComplete ? capacityForecasts : [];
        Capabilities = new ObserverCapabilityProfile();
    }

    /// <summary>Gets the versioned report schema.</summary>
    public string SchemaVersion { get; } = "aiops.observer.analysis.v2";

    /// <summary>Gets the deterministic report correlation identifier.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets the analysed database-instance identifier.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorisation scope under which the evidence was analysed.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the authoritative analysis instant in UTC.</summary>
    public DateTimeOffset AnalysedAt { get; }

    /// <summary>Gets whether analysis completed or publication failed closed.</summary>
    public ObserverAnalysisDisposition Disposition { get; }

    /// <summary>Gets the stable sanitised publication code.</summary>
    public string Code { get; }

    /// <summary>Gets whether every analyser completed before a current publication boundary.</summary>
    public bool IsComplete { get; }

    /// <summary>Gets a value that is always false because this inactive foundation grants no authority.</summary>
    public bool IsAuthorising { get; }

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
    /// <param name="executionContext">Absolute deadline and publication-revision boundary.</param>
    /// <param name="cancellationToken">Cancellation checked before and after every bounded analyser invocation.</param>
    /// <returns>A complete deterministic report or an empty, typed, non-authorising refusal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static ObserverAnalysisReport Analyse(
        ObserverAnalysisRequest request,
        ObserverAnalysisPolicy policy,
        ObserverAnalysisExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(executionContext);
        ObserverAnalysisReport? refused = RefusalBeforePublication(
            request,
            executionContext,
            validateRevision: false,
            cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        List<ObserverThresholdResult> thresholdResults = new(policy.ThresholdRules.Count);
        foreach (ObserverThresholdRule rule in policy.ThresholdRules)
        {
            thresholdResults.Add(DeterministicThresholdAnalyser.Analyse(request, rule));
            refused = RefusalBeforePublication(
                request,
                executionContext,
                validateRevision: false,
                cancellationToken);
            if (refused is not null)
            {
                return refused;
            }
        }

        List<ObserverCapacityForecastResult> forecastResults = new(policy.CapacityForecasts.Count);
        foreach (ObserverCapacityForecastPolicy forecast in policy.CapacityForecasts)
        {
            forecastResults.Add(CapacityForecastAnalyser.Analyse(request, forecast));
            refused = RefusalBeforePublication(
                request,
                executionContext,
                validateRevision: false,
                cancellationToken);
            if (refused is not null)
            {
                return refused;
            }
        }

        refused = RefusalBeforePublication(
            request,
            executionContext,
            validateRevision: true,
            cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        return new ObserverAnalysisReport(
            request,
            ObserverAnalysisDisposition.Completed,
            "aiops.observer.analysis.completed",
            thresholdResults.AsReadOnly(),
            forecastResults.AsReadOnly());
    }

    /// <summary>Returns a typed empty refusal whenever cancellation, expiry or stale publication forbids a report.</summary>
    /// <param name="request">Validated request whose correlation boundary is retained.</param>
    /// <param name="executionContext">Absolute deadline and revision boundary.</param>
    /// <param name="validateRevision">Whether this is the final publication boundary.</param>
    /// <param name="cancellationToken">Caller cancellation observed without throwing partial results.</param>
    /// <returns>An empty refusal, or <see langword="null"/> while processing remains safe.</returns>
    private static ObserverAnalysisReport? RefusalBeforePublication(
        ObserverAnalysisRequest request,
        ObserverAnalysisExecutionContext executionContext,
        bool validateRevision,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Refused(
                request,
                ObserverAnalysisDisposition.Cancelled,
                "aiops.observer.analysis.cancelled");
        }

        if (executionContext.TimeProvider.GetUtcNow() >= executionContext.Deadline)
        {
            return Refused(
                request,
                ObserverAnalysisDisposition.Expired,
                "aiops.observer.analysis.deadline_expired");
        }

        return validateRevision &&
            !string.Equals(
                executionContext.EvidenceContextRevision,
                executionContext.PublicationContextRevision,
                StringComparison.Ordinal)
            ? Refused(
                request,
                ObserverAnalysisDisposition.StaleContext,
                "aiops.observer.analysis.context_stale")
            : null;
    }

    /// <summary>Creates an empty non-authorising terminal report.</summary>
    /// <param name="request">Request identity retained without analyser outcomes.</param>
    /// <param name="disposition">Typed terminal disposition.</param>
    /// <param name="code">Stable sanitised code.</param>
    /// <returns>An empty terminal report.</returns>
    private static ObserverAnalysisReport Refused(
        ObserverAnalysisRequest request,
        ObserverAnalysisDisposition disposition,
        string code) =>
        new(request, disposition, code, [], []);
}
