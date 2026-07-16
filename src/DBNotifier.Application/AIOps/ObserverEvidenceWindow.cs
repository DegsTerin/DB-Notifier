// Module purpose: Selects bounded, temporally valid Observer evidence without hiding stale, future, conflicting or low-quality inputs.
namespace DBNotifier.Application.AIOps;

/// <summary>Represents one validated internal evidence window or one explicit insufficiency outcome.</summary>
/// <param name="Samples">Chronologically ordered samples when selection succeeded.</param>
/// <param name="FailureCode">Stable insufficiency code, or <see langword="null"/> when selection succeeded.</param>
/// <param name="EvidenceIds">Evidence identifiers relevant to the outcome.</param>
/// <param name="LimitationCodes">Stable factual limitations retained by the result.</param>
internal sealed record ObserverEvidenceSelection(
    IReadOnlyList<ObserverMetricSample> Samples,
    string? FailureCode,
    IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyList<string> LimitationCodes)
{
    /// <summary>Gets a value indicating whether the evidence window is safe to analyse.</summary>
    public bool IsUsable => FailureCode is null;
}

/// <summary>Applies shared no-lookahead, freshness, quality and conflict checks to Observer evidence.</summary>
internal static class ObserverEvidenceWindow
{
    internal const string MissingEvidence = "aiops.observer.evidence.missing";
    internal const string FutureEvidence = "aiops.observer.evidence.future";
    internal const string TimestampConflict = "aiops.observer.evidence.timestamp_conflict";
    internal const string DuplicateTimestamp = "aiops.observer.evidence.duplicate_timestamp";
    internal const string SourceContractConflict = "aiops.observer.evidence.source_contract_conflict";
    internal const string UnknownQuality = "aiops.observer.evidence.quality_unknown";
    internal const string StaleEvidence = "aiops.observer.evidence.stale";
    internal const string DegradedEvidence = "aiops.observer.evidence.degraded";

    /// <summary>Selects one metric window while refusing evidence that could create lookahead or hide uncertainty.</summary>
    /// <param name="request">Single-instance, scope-bound analysis request.</param>
    /// <param name="metricKey">Provider-neutral metric identifier.</param>
    /// <param name="unit">Canonical unit identifier.</param>
    /// <param name="lookback">Maximum history included in the analysis.</param>
    /// <param name="maximumSampleAge">Maximum acceptable age of the newest sample.</param>
    /// <returns>A usable ordered window or an explicit insufficiency result.</returns>
    public static ObserverEvidenceSelection Select(
        ObserverAnalysisRequest request,
        string metricKey,
        string unit,
        TimeSpan lookback,
        TimeSpan maximumSampleAge)
    {
        ObserverMetricSample[] matching = request.Samples
            .Where(sample =>
                string.Equals(sample.MetricKey, metricKey, StringComparison.Ordinal) &&
                string.Equals(sample.Unit, unit, StringComparison.Ordinal))
            .OrderBy(sample => sample.ObservedAt)
            .ThenBy(sample => sample.EvidenceId)
            .ToArray();

        if (matching.Length == 0)
        {
            return Failure(MissingEvidence, [], [MissingEvidence]);
        }

        ObserverMetricSample[] timestampConflicts = matching
            .Where(sample => sample.ReceivedAt < sample.ObservedAt)
            .ToArray();
        if (timestampConflicts.Length > 0)
        {
            return Failure(
                TimestampConflict,
                timestampConflicts.Select(sample => sample.EvidenceId),
                [TimestampConflict]);
        }

        ObserverMetricSample[] futureSamples = matching
            .Where(sample => sample.ObservedAt > request.AsOf || sample.ReceivedAt > request.AsOf)
            .ToArray();
        if (futureSamples.Length > 0)
        {
            return Failure(
                FutureEvidence,
                futureSamples.Select(sample => sample.EvidenceId),
                [FutureEvidence]);
        }

        DateTimeOffset windowStart = ObserverTimeBounds.SubtractOrMinimum(request.AsOf, lookback);
        ObserverMetricSample[] inWindow = matching
            .Where(sample => sample.ObservedAt >= windowStart)
            .ToArray();
        if (inWindow.Length == 0)
        {
            return Failure(
                StaleEvidence,
                [matching[^1].EvidenceId],
                [StaleEvidence]);
        }

        int sourceContractCount = inWindow
            .Select(sample => (sample.SourceKind, sample.SchemaVersion, sample.TransformationVersion))
            .Distinct()
            .Take(2)
            .Count();
        if (sourceContractCount > 1)
        {
            return Failure(
                SourceContractConflict,
                inWindow.Select(sample => sample.EvidenceId),
                [SourceContractConflict]);
        }

        IGrouping<DateTimeOffset, ObserverMetricSample>? duplicateTimestamp = inWindow
            .GroupBy(sample => sample.ObservedAt)
            .FirstOrDefault(group => group.Skip(1).Any());
        if (duplicateTimestamp is not null)
        {
            return Failure(
                DuplicateTimestamp,
                duplicateTimestamp.Select(sample => sample.EvidenceId),
                [DuplicateTimestamp]);
        }

        ObserverMetricSample[] unknownQuality = inWindow
            .Where(sample => sample.Quality == ObserverEvidenceQuality.Unknown)
            .ToArray();
        if (unknownQuality.Length > 0)
        {
            return Failure(
                UnknownQuality,
                unknownQuality.Select(sample => sample.EvidenceId),
                [UnknownQuality]);
        }

        ObserverMetricSample latest = inWindow[^1];
        if (request.AsOf - latest.ObservedAt > maximumSampleAge)
        {
            return Failure(
                StaleEvidence,
                [latest.EvidenceId],
                [StaleEvidence]);
        }

        string[] limitations = inWindow.Any(sample => sample.Quality == ObserverEvidenceQuality.Degraded)
            ? [DegradedEvidence]
            : [];

        return new ObserverEvidenceSelection(
            Array.AsReadOnly(inWindow),
            null,
            Array.AsReadOnly(inWindow.Select(sample => sample.EvidenceId).ToArray()),
            Array.AsReadOnly(limitations));
    }

    /// <summary>Creates a deterministic, immutable insufficiency selection.</summary>
    /// <param name="code">Stable failure code.</param>
    /// <param name="evidenceIds">Evidence links relevant to the refusal.</param>
    /// <param name="limitationCodes">Stable limitations explaining the refusal.</param>
    /// <returns>An unusable evidence selection in stable order.</returns>
    private static ObserverEvidenceSelection Failure(
        string code,
        IEnumerable<Guid> evidenceIds,
        IEnumerable<string> limitationCodes) =>
        new(
            Array.Empty<ObserverMetricSample>(),
            code,
            Array.AsReadOnly(evidenceIds.Order().ToArray()),
            Array.AsReadOnly(limitationCodes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()));
}
