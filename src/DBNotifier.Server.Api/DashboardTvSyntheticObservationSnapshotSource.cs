// Module purpose: Projects synthetic reconciled observation state into the Dashboard TV contract only when an isolated E2E host registers this adapter.
using System.Data;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Server.Api;

/// <summary>
/// Reads the current reconciled state from sandbox persistence and exposes only observations whose stored
/// evidence is explicitly synthetic. The normal Server composition never registers this adapter.
/// </summary>
/// <param name="contextFactory">Factory for the fixture-owned Server database.</param>
/// <param name="timeProvider">Trusted sandbox clock used to anchor an empty snapshot.</param>
public sealed class DashboardTvSyntheticObservationSnapshotSource(
    IDbContextFactory<ServerDbContext> contextFactory,
    TimeProvider timeProvider) : IDashboardTvSnapshotSource
{
    private const string SupportLabel = "Synthetic sandbox evidence";
    private const string LocationLabel = "Local observation pipeline";
    private readonly DateTimeOffset emptySnapshotGeneratedAt = timeProvider.GetUtcNow().ToUniversalTime();

    /// <inheritdoc />
    public async ValueTask<DashboardTvSnapshot> ReadAsync(CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        ProjectionRow[] projections = await (
                from instance in context.Instances.AsNoTracking()
                join state in context.InstanceObservationStates.AsNoTracking()
                    on instance.InstanceId equals state.InstanceId
                where instance.Enabled && instance.ArchivedAt == null &&
                    instance.AssignedAgentId != null && instance.AssignedAgentId == state.AgentId
                orderby instance.DisplayName, instance.InstanceId
                select new ProjectionRow(
                    instance.InstanceId,
                    instance.DisplayName,
                    instance.ProviderType,
                    instance.Environment,
                    instance.Enabled,
                    state.AgentId,
                    state.ObservationId,
                    state.LastProcessedSequence,
                    state.Status,
                    state.ObservedAt,
                    state.ReceivedAt))
            .Take(DashboardTvSnapshotContract.MaximumItemCount + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (projections.Length > DashboardTvSnapshotContract.MaximumItemCount)
        {
            throw new InvalidOperationException("dashboard_tv.synthetic_projection_limit_exceeded");
        }

        Guid[] observationIds = projections.Select(item => item.ObservationId).ToArray();
        HealthSampleRow[] samples = observationIds.Length == 0
            ? []
            : await context.HealthSamples
                .AsNoTracking()
                .Where(sample => observationIds.Contains(sample.ObservationId))
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        Dictionary<Guid, HealthSampleRow> samplesByObservation = samples
            .GroupBy(sample => sample.ObservationId)
            .ToDictionary(group => group.Key, group => group.Single());

        List<DashboardTvInventoryItem> items = new(projections.Length);
        foreach (ProjectionRow projection in projections)
        {
            if (!samplesByObservation.TryGetValue(projection.ObservationId, out HealthSampleRow? sample) ||
                sample.InstanceId != projection.InstanceId || sample.AgentId != projection.AgentId ||
                sample.Sequence != projection.LastProcessedSequence ||
                !string.Equals(sample.ProviderType, projection.ProviderType, StringComparison.Ordinal) ||
                !string.Equals(sample.Status, projection.Status, StringComparison.Ordinal) ||
                sample.ObservedAt != projection.ObservedAt || sample.ReceivedAt != projection.ReceivedAt ||
                !string.Equals(sample.EvidenceLevel, EvidenceLevel.Synthetic.ToString(), StringComparison.Ordinal) ||
                string.Equals(projection.Status, nameof(HealthStatus.Healthy), StringComparison.Ordinal) ||
                !TryMapStatus(projection.Status, out string canonicalStatus))
            {
                throw new InvalidOperationException("dashboard_tv.synthetic_projection_evidence_invalid");
            }

            items.Add(new DashboardTvInventoryItem(
                projection.InstanceId,
                projection.DisplayName,
                projection.ProviderType,
                SupportLabel,
                projection.Environment,
                LocationLabel,
                canonicalStatus,
                FormatUtc(projection.ObservedAt),
                FormatUtc(projection.ReceivedAt),
                checked((int)sample.DurationMilliseconds),
                projection.Enabled));
        }

        DateTimeOffset generatedAt = projections.Length == 0
            ? emptySnapshotGeneratedAt
            : projections.Max(item => item.ReceivedAt).ToUniversalTime();
        DashboardTvSnapshot snapshot = new(
            DashboardTvSnapshotContract.CurrentSchemaVersion,
            FormatUtc(generatedAt),
            items);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    /// <summary>Maps the canonical domain enum name to the stable Dashboard wire identifier.</summary>
    private static bool TryMapStatus(string value, out string mapped)
    {
        mapped = value switch
        {
            nameof(HealthStatus.Healthy) => "healthy",
            nameof(HealthStatus.Degraded) => "degraded",
            nameof(HealthStatus.Unavailable) => "unavailable",
            nameof(HealthStatus.AuthFailed) => "authFailed",
            nameof(HealthStatus.Timeout) => "timeout",
            nameof(HealthStatus.Maintenance) => "maintenance",
            nameof(HealthStatus.Unknown) => "unknown",
            _ => string.Empty,
        };
        return mapped.Length != 0;
    }

    /// <summary>Formats one authoritative instant as the existing UTC millisecond wire representation.</summary>
    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Holds only the bounded persistence fields required to prove and build one safe projected item.</summary>
    private sealed record ProjectionRow(
        Guid InstanceId,
        string DisplayName,
        string ProviderType,
        string Environment,
        bool Enabled,
        Guid AgentId,
        Guid ObservationId,
        long LastProcessedSequence,
        string Status,
        DateTimeOffset ObservedAt,
        DateTimeOffset ReceivedAt);
}
