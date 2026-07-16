// Module purpose: Defines the sanitised, provider-neutral evidence contract consumed by the inactive MOD-12 Observer analysis foundation.
namespace DBNotifier.Application.AIOps;

/// <summary>Classifies the evidential quality of one sanitised operational metric.</summary>
public enum ObserverEvidenceQuality
{
    /// <summary>The canonical source verified the value under its authorised contract.</summary>
    Verified,

    /// <summary>The value remains usable but carries a declared limitation.</summary>
    Degraded,

    /// <summary>The source cannot establish enough quality for analysis.</summary>
    Unknown,
}

/// <summary>Identifies whether a numeric evidence value is present at the initial Observer boundary.</summary>
public enum ObserverMissingness
{
    /// <summary>The metric value is present and finite; missing values are represented by absent evidence.</summary>
    Present = 1,
}

/// <summary>Declares the deliberately narrow series cardinality supported by the initial evidence contract.</summary>
public enum ObserverMetricCardinality
{
    /// <summary>The item represents exactly one instance-and-metric series without arbitrary labels or dimensions.</summary>
    SingleSeries = 1,
}

/// <summary>Identifies the only information classification accepted by the initial Observer foundation.</summary>
public enum ObserverDataClassification
{
    /// <summary>Operational telemetry that contains no database content, personal data or secret material.</summary>
    OperationalTelemetry = 1,
}

/// <summary>Identifies the required redaction result at the initial Observer trust boundary.</summary>
public enum ObserverRedactionStatus
{
    /// <summary>The upstream canonical transformation reports that disallowed content was removed.</summary>
    Sanitised = 1,
}

/// <summary>Identifies the only retention class supported by the in-memory foundation.</summary>
public enum ObserverRetentionClass
{
    /// <summary>The evidence is held only by the caller and current analysis; the foundation performs no persistence.</summary>
    EphemeralAnalysisOnly = 1,
}

/// <summary>Identifies the only permitted purpose supported by the initial foundation.</summary>
public enum ObserverPermittedPurpose
{
    /// <summary>Non-mutating deterministic and statistical analysis in the inactive Observer foundation.</summary>
    NonMutatingObserverAnalysis = 1,
}

/// <summary>
/// Represents one numeric, sanitised and scope-bound evidence item without provider-native payloads, labels or
/// executable content. Construction validates contract shape, not the authenticity of caller assertions; a future
/// authorised integration must derive these fields from canonical telemetry. Analysis still evaluates freshness,
/// timestamp consistency and evidential quality.
/// </summary>
public sealed class ObserverMetricSample
{
    /// <summary>Initialises one immutable metric sample at the MOD-12 trust boundary.</summary>
    /// <param name="evidenceId">Non-empty immutable identifier linking back to canonical evidence.</param>
    /// <param name="instanceId">Non-empty database-instance identifier.</param>
    /// <param name="authorisationScopeId">Non-empty scope that authorised this item for Observer analysis.</param>
    /// <param name="metricKey">Lower-case stable metric identifier containing only letters, digits, dots, hyphens or underscores.</param>
    /// <param name="unit">Lower-case stable unit identifier.</param>
    /// <param name="value">Finite numeric value expressed in <paramref name="unit"/>.</param>
    /// <param name="observedAt">Source observation instant; normalised to UTC.</param>
    /// <param name="receivedAt">Canonical receipt instant; normalised to UTC.</param>
    /// <param name="quality">Declared evidential quality.</param>
    /// <param name="missingness">Must be <see cref="ObserverMissingness.Present"/>; absence is represented by no sample.</param>
    /// <param name="cardinality">Must be <see cref="ObserverMetricCardinality.SingleSeries"/>.</param>
    /// <param name="sourceKind">Stable canonical source kind without provider-native or secret content.</param>
    /// <param name="schemaVersion">Version of the canonical source schema.</param>
    /// <param name="transformationVersion">Version of the sanitising transformation.</param>
    /// <param name="classification">Must be <see cref="ObserverDataClassification.OperationalTelemetry"/>.</param>
    /// <param name="redactionStatus">Must be <see cref="ObserverRedactionStatus.Sanitised"/>.</param>
    /// <param name="retentionClass">Must be <see cref="ObserverRetentionClass.EphemeralAnalysisOnly"/>.</param>
    /// <param name="permittedPurpose">Must be <see cref="ObserverPermittedPurpose.NonMutatingObserverAnalysis"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required identifier is null.</exception>
    /// <exception cref="ArgumentException">Thrown for empty identifiers, unsafe identifiers or non-finite values.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for empty GUIDs or unsupported enum values.</exception>
    public ObserverMetricSample(
        Guid evidenceId,
        Guid instanceId,
        Guid authorisationScopeId,
        string metricKey,
        string unit,
        double value,
        DateTimeOffset observedAt,
        DateTimeOffset receivedAt,
        ObserverEvidenceQuality quality,
        ObserverMissingness missingness,
        ObserverMetricCardinality cardinality,
        string sourceKind,
        string schemaVersion,
        string transformationVersion,
        ObserverDataClassification classification,
        ObserverRedactionStatus redactionStatus,
        ObserverRetentionClass retentionClass,
        ObserverPermittedPurpose permittedPurpose)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(evidenceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(authorisationScopeId, Guid.Empty);
        if (!double.IsFinite(value))
        {
            throw new ArgumentException("Observer evidence values must be finite.", nameof(value));
        }

        if (!Enum.IsDefined(quality))
        {
            throw new ArgumentOutOfRangeException(nameof(quality));
        }

        if (missingness != ObserverMissingness.Present)
        {
            throw new ArgumentOutOfRangeException(nameof(missingness));
        }

        if (cardinality != ObserverMetricCardinality.SingleSeries)
        {
            throw new ArgumentOutOfRangeException(nameof(cardinality));
        }

        if (classification != ObserverDataClassification.OperationalTelemetry)
        {
            throw new ArgumentOutOfRangeException(nameof(classification));
        }

        if (redactionStatus != ObserverRedactionStatus.Sanitised)
        {
            throw new ArgumentOutOfRangeException(nameof(redactionStatus));
        }

        if (retentionClass != ObserverRetentionClass.EphemeralAnalysisOnly)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionClass));
        }

        if (permittedPurpose != ObserverPermittedPurpose.NonMutatingObserverAnalysis)
        {
            throw new ArgumentOutOfRangeException(nameof(permittedPurpose));
        }

        EvidenceId = evidenceId;
        InstanceId = instanceId;
        AuthorisationScopeId = authorisationScopeId;
        MetricKey = ObserverContractGuard.StableIdentifier(metricKey, nameof(metricKey));
        Unit = ObserverContractGuard.StableIdentifier(unit, nameof(unit));
        Value = value;
        ObservedAt = observedAt.ToUniversalTime();
        ReceivedAt = receivedAt.ToUniversalTime();
        Quality = quality;
        Missingness = missingness;
        Cardinality = cardinality;
        SourceKind = ObserverContractGuard.StableIdentifier(sourceKind, nameof(sourceKind));
        SchemaVersion = ObserverContractGuard.StableIdentifier(schemaVersion, nameof(schemaVersion));
        TransformationVersion = ObserverContractGuard.StableIdentifier(
            transformationVersion,
            nameof(transformationVersion));
        Classification = classification;
        RedactionStatus = redactionStatus;
        RetentionClass = retentionClass;
        PermittedPurpose = permittedPurpose;
    }

    /// <summary>Gets the immutable canonical evidence identifier.</summary>
    public Guid EvidenceId { get; }

    /// <summary>Gets the database-instance scope of the sample.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the authorisation scope under which analysis is permitted.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the provider-neutral metric identifier.</summary>
    public string MetricKey { get; }

    /// <summary>Gets the canonical unit identifier.</summary>
    public string Unit { get; }

    /// <summary>Gets the finite numeric value.</summary>
    public double Value { get; }

    /// <summary>Gets the source observation instant in UTC.</summary>
    public DateTimeOffset ObservedAt { get; }

    /// <summary>Gets the canonical receipt instant in UTC.</summary>
    public DateTimeOffset ReceivedAt { get; }

    /// <summary>Gets the declared evidence quality.</summary>
    public ObserverEvidenceQuality Quality { get; }

    /// <summary>Gets the explicit present-value missingness state.</summary>
    public ObserverMissingness Missingness { get; }

    /// <summary>Gets the bounded single-series cardinality declaration.</summary>
    public ObserverMetricCardinality Cardinality { get; }

    /// <summary>Gets the canonical source kind.</summary>
    public string SourceKind { get; }

    /// <summary>Gets the canonical source schema version.</summary>
    public string SchemaVersion { get; }

    /// <summary>Gets the sanitising transformation version.</summary>
    public string TransformationVersion { get; }

    /// <summary>Gets the explicitly asserted information classification.</summary>
    public ObserverDataClassification Classification { get; }

    /// <summary>Gets the explicitly asserted redaction result.</summary>
    public ObserverRedactionStatus RedactionStatus { get; }

    /// <summary>Gets the retention class that prohibits foundation-owned persistence.</summary>
    public ObserverRetentionClass RetentionClass { get; }

    /// <summary>Gets the sole non-mutating purpose permitted for the evidence.</summary>
    public ObserverPermittedPurpose PermittedPurpose { get; }
}

/// <summary>
/// Defines one bounded, single-instance analysis request. It rejects cross-instance, cross-authorisation and duplicate
/// evidence before any rule or statistical model can inspect the samples.
/// </summary>
public sealed class ObserverAnalysisRequest
{
    /// <summary>Maximum number of metric samples accepted in one in-memory analysis.</summary>
    public const int MaximumSampleCount = 10_000;

    /// <summary>Initialises one immutable analysis request.</summary>
    /// <param name="analysisId">Non-empty identifier used to correlate the deterministic report.</param>
    /// <param name="instanceId">Non-empty database-instance identifier shared by every sample.</param>
    /// <param name="authorisationScopeId">Non-empty authorisation scope shared by every sample.</param>
    /// <param name="asOf">Authoritative analysis instant; normalised to UTC.</param>
    /// <param name="samples">Zero to <see cref="MaximumSampleCount"/> sanitised metric samples.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="samples"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when evidence is duplicated or crosses the request boundaries.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for empty identifiers or an excessive sample count.</exception>
    public ObserverAnalysisRequest(
        Guid analysisId,
        Guid instanceId,
        Guid authorisationScopeId,
        DateTimeOffset asOf,
        IReadOnlyCollection<ObserverMetricSample> samples)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(analysisId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(authorisationScopeId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(samples);
        ObserverMetricSample[] copiedSamples = samples.Take(MaximumSampleCount + 1).ToArray();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(copiedSamples.Length, MaximumSampleCount, nameof(samples));
        HashSet<Guid> evidenceIds = [];
        foreach (ObserverMetricSample? sample in copiedSamples)
        {
            if (sample is null)
            {
                throw new ArgumentException("Observer evidence items cannot be null.", nameof(samples));
            }

            if (sample.InstanceId != instanceId)
            {
                throw new ArgumentException("Observer evidence cannot cross instance boundaries.", nameof(samples));
            }

            if (sample.AuthorisationScopeId != authorisationScopeId)
            {
                throw new ArgumentException("Observer evidence cannot cross authorisation boundaries.", nameof(samples));
            }

            if (!evidenceIds.Add(sample.EvidenceId))
            {
                throw new ArgumentException("Observer evidence identifiers must be unique.", nameof(samples));
            }
        }

        AnalysisId = analysisId;
        InstanceId = instanceId;
        AuthorisationScopeId = authorisationScopeId;
        AsOf = asOf.ToUniversalTime();
        Samples = Array.AsReadOnly(copiedSamples);
    }

    /// <summary>Gets the report correlation identifier.</summary>
    public Guid AnalysisId { get; }

    /// <summary>Gets the single database-instance boundary.</summary>
    public Guid InstanceId { get; }

    /// <summary>Gets the single authorisation boundary.</summary>
    public Guid AuthorisationScopeId { get; }

    /// <summary>Gets the authoritative analysis instant in UTC.</summary>
    public DateTimeOffset AsOf { get; }

    /// <summary>Gets an immutable view of the sanitised evidence.</summary>
    public IReadOnlyList<ObserverMetricSample> Samples { get; }
}

/// <summary>Provides shared validation for stable, non-secret Observer contract identifiers.</summary>
internal static class ObserverContractGuard
{
    private const int MaximumIdentifierLength = 128;

    /// <summary>Validates one lower-case ASCII identifier suitable for contracts, rules, versions and result codes.</summary>
    /// <param name="value">Identifier to validate.</param>
    /// <param name="parameterName">Public parameter name used by any resulting exception.</param>
    /// <returns>The original validated identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the identifier is empty, too long or contains unsafe characters.</exception>
    public static string StableIdentifier(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > MaximumIdentifierLength || !IsAsciiLetterOrDigit(value[0]) || !IsAsciiLetterOrDigit(value[^1]))
        {
            throw new ArgumentException("Observer identifiers must be bounded lower-case ASCII identifiers.", parameterName);
        }

        foreach (char character in value)
        {
            if (!IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_')
            {
                throw new ArgumentException("Observer identifiers must be bounded lower-case ASCII identifiers.", parameterName);
            }
        }

        return value;
    }

    /// <summary>Checks the deliberately narrow ASCII alphabet accepted by stable contract identifiers.</summary>
    /// <param name="value">Character to inspect.</param>
    /// <returns><see langword="true"/> only for a lower-case ASCII letter or digit.</returns>
    private static bool IsAsciiLetterOrDigit(char value) =>
        value is >= 'a' and <= 'z' or >= '0' and <= '9';
}
