// Module purpose: Adapts authorised canonical health telemetry into bounded MOD-12 evidence under explicit opt-in policy without runtime activation or side effects.
using DBNotifier.Domain;

namespace DBNotifier.Application.AIOps;

/// <summary>Identifies whether a purpose-specific MOD-12 data policy explicitly permits evidence use.</summary>
public enum ObserverDataOptInState
{
    /// <summary>Refuses all evidence use; this zero value preserves fail-closed defaults.</summary>
    Disabled,

    /// <summary>Permits only the sources, period and purpose declared by the policy.</summary>
    Enabled,
}

/// <summary>Separates ordinary analysis policy from isolated offline evaluation policy.</summary>
public enum ObserverDataUse
{
    /// <summary>Permits local non-mutating analysis of already authorised canonical telemetry.</summary>
    RuntimeAnalysis = 1,

    /// <summary>Permits deterministic offline evaluation, including explicitly declared synthetic evidence.</summary>
    OfflineEvaluation = 2,
}

/// <summary>Defines one exact database-instance and Agent source pair authorised by a data policy.</summary>
public sealed class ObserverDataScope
{
    /// <summary>Initialises one exact canonical telemetry source boundary.</summary>
    /// <param name="instanceId">Non-empty database-instance identifier.</param>
    /// <param name="agentId">Non-empty Agent identifier assigned to the source.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when either identifier is empty.</exception>
    public ObserverDataScope(Guid instanceId, Guid agentId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        InstanceId = instanceId;
        AgentId = agentId;
    }

    /// <summary>Gets the authorised database-instance identifier.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorised Agent identifier.</summary>
    public Guid AgentId { get; }
}

/// <summary>
/// Defines a bounded, purpose-specific and time-limited opt-in policy. The policy never authorises collection,
/// persistence, mode promotion, recommendation or execution; it governs only use of telemetry already authorised by
/// the owning application boundary.
/// </summary>
public sealed class ObserverDataPolicy
{
    /// <summary>Maximum exact instance-and-Agent source pairs accepted by one policy.</summary>
    public const int MaximumSourceCount = 1_000;

    private readonly HashSet<(Guid InstanceId, Guid AgentId)> allowedSources;

    /// <summary>Initialises one immutable MOD-12 data-use policy.</summary>
    /// <param name="policyId">Stable non-secret policy identifier.</param>
    /// <param name="policyVersion">Stable policy version.</param>
    /// <param name="optInState">Explicit enabled or disabled state.</param>
    /// <param name="dataUse">Purpose-specific runtime-analysis or offline-evaluation use.</param>
    /// <param name="authorisationScopeId">Non-empty server-side authorisation scope.</param>
    /// <param name="effectiveFrom">Inclusive UTC policy start.</param>
    /// <param name="expiresAt">Exclusive UTC policy expiry.</param>
    /// <param name="allowedSources">Zero to <see cref="MaximumSourceCount"/> exact source pairs.</param>
    /// <param name="allowSyntheticEvidence">Whether an offline-evaluation policy may accept declared synthetic evidence.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedSources"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for duplicate sources, invalid periods or non-UTC instants.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for empty identifiers, invalid enum values or excessive sources.</exception>
    public ObserverDataPolicy(
        string policyId,
        string policyVersion,
        ObserverDataOptInState optInState,
        ObserverDataUse dataUse,
        Guid authorisationScopeId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<ObserverDataScope> allowedSources,
        bool allowSyntheticEvidence = false)
    {
        if (!Enum.IsDefined(optInState))
        {
            throw new ArgumentOutOfRangeException(nameof(optInState));
        }

        if (!Enum.IsDefined(dataUse))
        {
            throw new ArgumentOutOfRangeException(nameof(dataUse));
        }

        ArgumentOutOfRangeException.ThrowIfEqual(authorisationScopeId, Guid.Empty);
        ValidateUtcInstant(effectiveFrom, nameof(effectiveFrom));
        ValidateUtcInstant(expiresAt, nameof(expiresAt));
        if (expiresAt <= effectiveFrom)
        {
            throw new ArgumentException("Observer data policy expiry must follow its effective instant.", nameof(expiresAt));
        }

        if (allowSyntheticEvidence && dataUse != ObserverDataUse.OfflineEvaluation)
        {
            throw new ArgumentException(
                "Synthetic evidence can be enabled only for isolated offline evaluation.",
                nameof(allowSyntheticEvidence));
        }

        ArgumentNullException.ThrowIfNull(allowedSources);
        ObserverDataScope?[] boundedSources = allowedSources.Take(MaximumSourceCount + 1).ToArray();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            boundedSources.Length,
            MaximumSourceCount,
            nameof(allowedSources));
        if (boundedSources.Any(source => source is null))
        {
            throw new ArgumentException("Observer data policies cannot contain null source scopes.", nameof(allowedSources));
        }

        ObserverDataScope[] copiedSources = boundedSources.Select(source => source!).ToArray();
        if (copiedSources
                .Select(source => (source.InstanceId, source.AgentId))
                .Distinct()
                .Count() != copiedSources.Length)
        {
            throw new ArgumentException("Observer data policies cannot repeat source scopes.", nameof(allowedSources));
        }

        if (optInState == ObserverDataOptInState.Enabled && copiedSources.Length == 0)
        {
            throw new ArgumentException("An enabled Observer data policy requires at least one exact source.", nameof(allowedSources));
        }

        PolicyId = ObserverContractGuard.StableIdentifier(policyId, nameof(policyId));
        PolicyVersion = ObserverContractGuard.StableIdentifier(policyVersion, nameof(policyVersion));
        OptInState = optInState;
        DataUse = dataUse;
        AuthorisationScopeId = authorisationScopeId;
        EffectiveFrom = effectiveFrom;
        ExpiresAt = expiresAt;
        AllowedSources = Array.AsReadOnly(copiedSources
            .OrderBy(source => source.InstanceId)
            .ThenBy(source => source.AgentId)
            .ToArray());
        this.allowedSources = AllowedSources
            .Select(source => (source.InstanceId, source.AgentId))
            .ToHashSet();
        AllowSyntheticEvidence = allowSyntheticEvidence;
    }

    /// <summary>Gets the stable data-policy identifier.</summary>
    public string PolicyId { get; }

    /// <summary>Gets the stable data-policy version.</summary>
    public string PolicyVersion { get; }

    /// <summary>Gets the explicit fail-closed opt-in state.</summary>
    public ObserverDataOptInState OptInState { get; }

    /// <summary>Gets the sole declared data use.</summary>
    public ObserverDataUse DataUse { get; }

    /// <summary>Gets the authorisation scope derived into adapted evidence.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the inclusive UTC policy start.</summary>
    public DateTimeOffset EffectiveFrom { get; }

    /// <summary>Gets the exclusive UTC policy expiry.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the immutable exact source allowlist.</summary>
    public IReadOnlyList<ObserverDataScope> AllowedSources { get; }

    /// <summary>Gets whether an isolated offline evaluation may use declared synthetic evidence.</summary>
    public bool AllowSyntheticEvidence { get; }

    /// <summary>Gets the classification fixed by the policy rather than supplied by telemetry.</summary>
    public ObserverDataClassification Classification { get; } = ObserverDataClassification.OperationalTelemetry;

    /// <summary>Gets the sanitisation result fixed by the trusted adapter contract.</summary>
    public ObserverRedactionStatus RedactionStatus { get; } = ObserverRedactionStatus.Sanitised;

    /// <summary>Gets the retention class that prohibits adapter-owned persistence.</summary>
    public ObserverRetentionClass RetentionClass { get; } = ObserverRetentionClass.EphemeralAnalysisOnly;

    /// <summary>Gets the non-mutating analysis purpose fixed by this policy.</summary>
    public ObserverPermittedPurpose PermittedPurpose { get; } = ObserverPermittedPurpose.NonMutatingObserverAnalysis;

    /// <summary>Gets a value indicating that this policy never authorises collection.</summary>
    public bool AllowsCollection { get; }

    /// <summary>Gets a value indicating that this policy never authorises persistence.</summary>
    public bool AllowsPersistence { get; }

    /// <summary>Gets a value indicating that this policy never activates or promotes an Observer runtime mode.</summary>
    public bool ActivatesObserverMode { get; }

    /// <summary>Checks an exact source pair without exposing a mutable policy collection.</summary>
    /// <param name="instanceId">Canonical database-instance identifier.</param>
    /// <param name="agentId">Canonical Agent identifier.</param>
    /// <returns><see langword="true"/> only for an exact allowlisted pair.</returns>
    internal bool ContainsSource(Guid instanceId, Guid agentId) =>
        allowedSources.Contains((instanceId, agentId));

    /// <summary>Rejects default or offset-bearing instants at a policy trust boundary.</summary>
    /// <param name="value">Instant to validate.</param>
    /// <param name="parameterName">Public parameter name used by any exception.</param>
    /// <exception cref="ArgumentException">Thrown when the instant is default or not expressed as UTC.</exception>
    private static void ValidateUtcInstant(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observer data policy instants must be explicit UTC values.", parameterName);
        }
    }
}

/// <summary>
/// Carries one typed canonical health observation and its authoritative receipt instant into the adapter. Construction
/// does not bless the data; the adapter revalidates canonical invariants and policy scope before creating evidence.
/// </summary>
public sealed class ObserverCanonicalHealthTelemetry
{
    /// <summary>Initialises one candidate canonical telemetry envelope.</summary>
    /// <param name="observation">Typed provider-neutral health observation.</param>
    /// <param name="receivedAt">Authoritative server receipt instant to be validated as UTC.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="observation"/> is null.</exception>
    public ObserverCanonicalHealthTelemetry(HealthObservation observation, DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(observation);
        Observation = observation;
        ReceivedAt = receivedAt;
    }

    /// <summary>Gets the typed canonical observation.</summary>
    public HealthObservation Observation { get; }

    /// <summary>Gets the authoritative receipt instant presented to the adapter.</summary>
    public DateTimeOffset ReceivedAt { get; }
}

/// <summary>Classifies whether canonical telemetry crossed the trusted MOD-12 adapter boundary.</summary>
public enum ObserverTelemetryAdaptationDisposition
{
    /// <summary>The adapter created one bounded numeric evidence item.</summary>
    Accepted = 1,

    /// <summary>The adapter refused telemetry or policy that failed a trust-boundary rule.</summary>
    Rejected = 2,
}

/// <summary>Reports one accepted sample or one sanitised stable rejection without echoing source content.</summary>
public sealed class ObserverTelemetryAdaptationResult
{
    /// <summary>Initialises one internally validated adaptation result.</summary>
    /// <param name="disposition">Accepted or rejected outcome.</param>
    /// <param name="code">Stable sanitised outcome code.</param>
    /// <param name="sample">Adapted sample only for an accepted outcome.</param>
    internal ObserverTelemetryAdaptationResult(
        ObserverTelemetryAdaptationDisposition disposition,
        string code,
        ObserverMetricSample? sample)
    {
        Disposition = disposition;
        Code = code;
        Sample = sample;
    }

    /// <summary>Gets the accepted or rejected disposition.</summary>
    public ObserverTelemetryAdaptationDisposition Disposition { get; }

    /// <summary>Gets the stable sanitised outcome code.</summary>
    public string Code { get; }

    /// <summary>Gets the adapted evidence, or <see langword="null"/> when rejected.</summary>
    public ObserverMetricSample? Sample { get; }
}

/// <summary>
/// Converts already authorised canonical health observations into one provider-neutral duration series. It derives all
/// handling declarations from policy, copies no native diagnostic content and performs no collection, persistence,
/// registration, network access or mode promotion.
/// </summary>
public static class CanonicalObserverTelemetryAdapter
{
    /// <summary>Provider-neutral metric emitted by this first trusted transformation.</summary>
    public const string MetricKey = "database.probe.duration";

    /// <summary>Canonical duration unit emitted by this first trusted transformation.</summary>
    public const string Unit = "milliseconds";

    private const string SourceKind = "canonical.health-observation";
    private const string SchemaVersion = "health-observation.v1";
    private const string TransformationVersion = "observer-duration.v1";
    private const int MaximumAttemptCount = 10;
    private const int MaximumLimitationCount = 20;
    private const int MaximumLimitationLength = 200;
    private const int MaximumProviderVersionLength = 64;
    private const int MaximumMethodLength = 64;
    private const int MaximumErrorCodeLength = 100;
    private const int MaximumSafeErrorLength = 1_000;
    private static readonly TimeSpan MaximumDuration = TimeSpan.FromMinutes(5);

    /// <summary>Adapts one canonical health observation under an explicit, current and exact-source opt-in policy.</summary>
    /// <param name="telemetry">Typed canonical observation and authoritative receipt instant.</param>
    /// <param name="policy">Purpose-specific data policy from the authorised application boundary.</param>
    /// <param name="asOf">Trusted UTC decision instant used to prevent lookahead and enforce policy validity.</param>
    /// <returns>One accepted numeric sample or a sanitised fail-closed rejection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="telemetry"/> or <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="asOf"/> is default or not expressed as UTC.</exception>
    public static ObserverTelemetryAdaptationResult Adapt(
        ObserverCanonicalHealthTelemetry telemetry,
        ObserverDataPolicy policy,
        DateTimeOffset asOf) =>
        AdaptCore(telemetry, policy, asOf, ObserverDataUse.RuntimeAnalysis);

    /// <summary>Adapts one canonical item only for the isolated offline runner's declared data use.</summary>
    /// <param name="telemetry">Typed canonical observation and authoritative receipt instant.</param>
    /// <param name="policy">Explicit offline-evaluation policy.</param>
    /// <param name="asOf">Trusted UTC case instant.</param>
    /// <returns>One accepted numeric sample or a sanitised fail-closed rejection.</returns>
    internal static ObserverTelemetryAdaptationResult AdaptForOfflineEvaluation(
        ObserverCanonicalHealthTelemetry telemetry,
        ObserverDataPolicy policy,
        DateTimeOffset asOf) =>
        AdaptCore(telemetry, policy, asOf, ObserverDataUse.OfflineEvaluation);

    /// <summary>Applies the shared adapter boundary while enforcing the caller's fixed purpose.</summary>
    /// <param name="telemetry">Typed canonical observation and authoritative receipt instant.</param>
    /// <param name="policy">Purpose-specific data policy.</param>
    /// <param name="asOf">Trusted UTC decision instant.</param>
    /// <param name="requiredDataUse">Fixed runtime or offline use selected by the owning entry point.</param>
    /// <returns>One accepted numeric sample or a sanitised fail-closed rejection.</returns>
    private static ObserverTelemetryAdaptationResult AdaptCore(
        ObserverCanonicalHealthTelemetry telemetry,
        ObserverDataPolicy policy,
        DateTimeOffset asOf,
        ObserverDataUse requiredDataUse)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(policy);
        if (asOf == default || asOf.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observer adapter decision time must be an explicit UTC value.", nameof(asOf));
        }

        if (policy.OptInState != ObserverDataOptInState.Enabled)
        {
            return Rejected("aiops.observer.adapter.policy_disabled");
        }

        if (policy.DataUse != requiredDataUse)
        {
            return Rejected("aiops.observer.adapter.policy_purpose_mismatch");
        }

        if (asOf < policy.EffectiveFrom)
        {
            return Rejected("aiops.observer.adapter.policy_not_effective");
        }

        if (asOf >= policy.ExpiresAt)
        {
            return Rejected("aiops.observer.adapter.policy_expired");
        }

        HealthObservation observation = telemetry.Observation;
        if (!policy.ContainsSource(observation.InstanceId, observation.AgentId))
        {
            return Rejected("aiops.observer.adapter.source_denied");
        }

        if (!HasCanonicalContractShape(observation))
        {
            return Rejected("aiops.observer.adapter.contract_invalid");
        }

        if (observation.ObservedAt == default || observation.ObservedAt.Offset != TimeSpan.Zero ||
            telemetry.ReceivedAt == default || telemetry.ReceivedAt.Offset != TimeSpan.Zero ||
            telemetry.ReceivedAt < observation.ObservedAt || observation.ObservedAt > asOf || telemetry.ReceivedAt > asOf)
        {
            return Rejected("aiops.observer.adapter.timestamp_invalid");
        }

        if (observation.ObservedAt < policy.EffectiveFrom || telemetry.ReceivedAt < policy.EffectiveFrom ||
            observation.ObservedAt >= policy.ExpiresAt || telemetry.ReceivedAt >= policy.ExpiresAt)
        {
            return Rejected("aiops.observer.adapter.evidence_outside_policy_period");
        }

        if (observation.Quality.EvidenceLevel == EvidenceLevel.Unknown)
        {
            return Rejected("aiops.observer.adapter.evidence_unknown");
        }

        if (observation.Quality.EvidenceLevel == EvidenceLevel.Synthetic && !policy.AllowSyntheticEvidence)
        {
            return Rejected("aiops.observer.adapter.synthetic_denied");
        }

        ObserverEvidenceQuality quality = observation.Quality.EvidenceLevel switch
        {
            EvidenceLevel.ProviderAuthenticated or EvidenceLevel.ProviderReadiness => ObserverEvidenceQuality.Verified,
            EvidenceLevel.TransportOnly or EvidenceLevel.Synthetic => ObserverEvidenceQuality.Degraded,
            _ => ObserverEvidenceQuality.Unknown,
        };
        ObserverMetricSample sample = new(
            observation.ObservationId,
            observation.InstanceId,
            policy.AuthorisationScopeId,
            MetricKey,
            Unit,
            observation.Duration.TotalMilliseconds,
            observation.ObservedAt,
            telemetry.ReceivedAt,
            quality,
            ObserverMissingness.Present,
            ObserverMetricCardinality.SingleSeries,
            SourceKind,
            SchemaVersion,
            TransformationVersion,
            policy.Classification,
            policy.RedactionStatus,
            policy.RetentionClass,
            policy.PermittedPurpose);
        return new ObserverTelemetryAdaptationResult(
            ObserverTelemetryAdaptationDisposition.Accepted,
            "aiops.observer.adapter.accepted",
            sample);
    }

    /// <summary>Checks the implemented canonical subset without copying diagnostic or provider-native content.</summary>
    /// <param name="observation">Typed health observation to validate.</param>
    /// <returns><see langword="true"/> only when every bounded canonical invariant is satisfied.</returns>
    private static bool HasCanonicalContractShape(HealthObservation observation)
    {
        ObservationQuality? quality = observation.Quality;
        NormalizedError? error = observation.Error;
        return observation.ObservationId != Guid.Empty &&
            observation.InstanceId != Guid.Empty &&
            observation.AgentId != Guid.Empty &&
            ProviderType.TryParse(observation.ProviderType.Value, out ProviderType canonicalProviderType) &&
            canonicalProviderType == observation.ProviderType &&
            Enum.IsDefined(observation.Status) &&
            !string.IsNullOrWhiteSpace(observation.ProviderVersion) &&
            observation.ProviderVersion.Length <= MaximumProviderVersionLength &&
            !string.IsNullOrWhiteSpace(observation.Method) &&
            observation.Method.Length <= MaximumMethodLength &&
            observation.Duration >= TimeSpan.Zero &&
            observation.Duration <= MaximumDuration &&
            quality is not null &&
            Enum.IsDefined(quality.EvidenceLevel) &&
            quality.AttemptCount is >= 1 and <= MaximumAttemptCount &&
            quality.Limitations is not null &&
            quality.Limitations.Count <= MaximumLimitationCount &&
            quality.Limitations.All(item =>
                !string.IsNullOrWhiteSpace(item) && item.Length <= MaximumLimitationLength) &&
            IsStatusEvidenceConsistent(observation.Status, quality.EvidenceLevel) &&
            IsErrorShapeValid(observation.Status, error);
    }

    /// <summary>Prevents healthy state from being created with transport-only, synthetic or unknown evidence.</summary>
    /// <param name="status">Canonical health status.</param>
    /// <param name="evidenceLevel">Canonical evidential strength.</param>
    /// <returns><see langword="true"/> when the status/evidence pair is safe.</returns>
    private static bool IsStatusEvidenceConsistent(HealthStatus status, EvidenceLevel evidenceLevel) =>
        status != HealthStatus.Healthy ||
        evidenceLevel is EvidenceLevel.ProviderAuthenticated or EvidenceLevel.ProviderReadiness;

    /// <summary>Checks bounded, sanitised error metadata while ensuring healthy evidence carries no error.</summary>
    /// <param name="status">Canonical health status.</param>
    /// <param name="error">Optional normalised error.</param>
    /// <returns><see langword="true"/> when the error shape is safe to ignore rather than copy.</returns>
    private static bool IsErrorShapeValid(HealthStatus status, NormalizedError? error)
    {
        if (error is null)
        {
            return true;
        }

        return status != HealthStatus.Healthy &&
            !string.IsNullOrWhiteSpace(error.Code) &&
            error.Code.Length <= MaximumErrorCodeLength &&
            Enum.IsDefined(error.Category) &&
            Enum.IsDefined(error.Retryability) &&
            !string.IsNullOrWhiteSpace(error.SafeMessage) &&
            error.SafeMessage.Length <= MaximumSafeErrorLength;
    }

    /// <summary>Creates a stable rejection that never reflects input identifiers or diagnostic text.</summary>
    /// <param name="code">Stable adapter rejection code.</param>
    /// <returns>A rejected adaptation result without evidence.</returns>
    private static ObserverTelemetryAdaptationResult Rejected(string code) =>
        new(ObserverTelemetryAdaptationDisposition.Rejected, code, null);
}
