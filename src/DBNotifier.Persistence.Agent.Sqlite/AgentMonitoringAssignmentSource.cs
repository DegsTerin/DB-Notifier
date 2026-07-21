// Module purpose: Implements Agent Monitoring Assignment Source for the Agent-local SQLite boundary without exposing monitored database secrets.
using System.Data;
using System.Globalization;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Monitoring;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>
/// Reads enabled Agent-local assignments, filters them by the latest observation cadence and rotates due work
/// fairly without exposing endpoint secrets or exceeding the configured fleet bound.
/// </summary>
/// <param name="contextFactory">Factory for read-only Agent-local persistence contexts.</param>
/// <param name="logger">Structured logger used only for stable invalid-assignment codes.</param>
/// <param name="maximumAssignments">Hard assignment ceiling between one and 5,000.</param>
public sealed partial class AgentMonitoringAssignmentSource(
    IDbContextFactory<AgentDbContext> contextFactory,
    ILogger<AgentMonitoringAssignmentSource> logger,
    int maximumAssignments = 5000) : IMonitoringAssignmentSource
{
    private int rotationIndex;

    /// <summary>Returns due, validated assignments in a rotating order that avoids repeatedly starving the same tail.</summary>
    /// <param name="now">UTC instant used to compare each monitoring interval with its latest observation.</param>
    /// <param name="cancellationToken">Cancellation propagated from the monitoring worker.</param>
    /// <returns>At most the configured maximum number of due provider-neutral assignments.</returns>
    /// <exception cref="InvalidOperationException">Thrown when persisted enabled assignments exceed the hard ceiling.</exception>
    public async ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumAssignments, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumAssignments, 5000);
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        AgentInstanceAssignmentRow[] configured = await context.InstanceAssignments
            .AsNoTracking()
            .Where(row => row.Enabled)
            .OrderBy(row => row.InstanceId)
            .Take(maximumAssignments + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (configured.Length > maximumAssignments)
        {
            throw new InvalidOperationException("assignment.maximum_exceeded");
        }

        if (configured.Length == 0)
        {
            return [];
        }

        Dictionary<Guid, DateTimeOffset> latestByInstance = await GetLatestObservationTimesAsync(
            context,
            cancellationToken).ConfigureAwait(false);

        List<MonitoringAssignment> due = [];
        foreach (AgentInstanceAssignmentRow row in configured)
        {
            if (latestByInstance.TryGetValue(row.InstanceId, out DateTimeOffset latest) &&
                latest.AddSeconds(row.IntervalSeconds) > now)
            {
                continue;
            }

            if (TryCreateAssignment(row, out MonitoringAssignment? assignment, out string errorCode))
            {
                due.Add(assignment);
            }
            else
            {
                LogInvalidAssignment(logger, row.InstanceId, errorCode);
            }
        }

        if (due.Count < 2)
        {
            return due;
        }

        // Rotate equal-priority work between cycles so a repeatedly exhausted deadline cannot always
        // penalise the same tail assignments. The source is a singleton in the Agent host.
        int startIndex = (int)((uint)(Interlocked.Increment(ref rotationIndex) - 1) % (uint)due.Count);
        return due.Skip(startIndex).Concat(due.Take(startIndex)).ToArray();
    }

    /// <summary>Reads only the latest observation time for each enabled assignment to avoid loading unbounded history.</summary>
    private static async ValueTask<Dictionary<Guid, DateTimeOffset>> GetLatestObservationTimesAsync(
        AgentDbContext context,
        CancellationToken cancellationToken)
    {
        System.Data.Common.DbConnection connection = context.Database.GetDbConnection();
        bool closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using System.Data.Common.DbCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT observation.instance_id, MAX(observation.observed_at)
                FROM health_observations AS observation
                INNER JOIN instance_assignments AS assignment
                    ON assignment.instance_id = observation.instance_id
                WHERE assignment.enabled = 1
                GROUP BY observation.instance_id
                """;
            await using System.Data.Common.DbDataReader reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            Dictionary<Guid, DateTimeOffset> latest = [];
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (Guid.TryParse(reader.GetString(0), out Guid instanceId) &&
                    DateTimeOffset.TryParse(
                        reader.GetString(1),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset observedAt))
                {
                    latest[instanceId] = observedAt;
                }
            }

            return latest;
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static bool TryCreateAssignment(
        AgentInstanceAssignmentRow row,
        out MonitoringAssignment assignment,
        out string errorCode)
    {
        assignment = null!;
        errorCode = "assignment.invalid";
        if (row.TimeoutSeconds is < 1 or > 300 || row.RetryCount is < 0 or > 9)
        {
            errorCode = "assignment.policy_invalid";
            return false;
        }

        if (!ProviderType.TryParse(row.ProviderType, out ProviderType providerType) ||
            !TryParseEndpoint(providerType, row.EndpointJson, out ProviderEndpoint? endpoint))
        {
            errorCode = "assignment.endpoint_invalid";
            return false;
        }

        CredentialReference? credential = null;
        if (!string.IsNullOrWhiteSpace(row.MonitoringCredentialReference) &&
            !TryParseCredentialReference(row.MonitoringCredentialReference, out credential))
        {
            errorCode = "assignment.credential_reference_invalid";
            return false;
        }

        assignment = new MonitoringAssignment(
            row.InstanceId,
            endpoint,
            credential,
            new ProbePolicy(
                TimeSpan.FromSeconds(row.TimeoutSeconds),
                checked(row.RetryCount + 1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(Math.Min(row.TimeoutSeconds, 30))));
        return true;
    }

    private static bool TryParseEndpoint(
        ProviderType providerType,
        string endpointJson,
        out ProviderEndpoint endpoint)
    {
        endpoint = null!;
        try
        {
            using JsonDocument document = JsonDocument.Parse(endpointJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            List<KeyValuePair<string, string>> properties = [];
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                string? value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
                    JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
                    _ => null,
                };
                if (value is null)
                {
                    return false;
                }

                properties.Add(new KeyValuePair<string, string>(property.Name, value));
            }

            endpoint = new ProviderEndpoint(providerType, properties);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryParseCredentialReference(string json, out CredentialReference? reference)
    {
        reference = null;
        return AgentAssignmentValidator.TryParseMonitoringCredentialReference(json, out reference);
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "Skipping invalid monitoring assignment {InstanceId}: {ErrorCode}")]
    private static partial void LogInvalidAssignment(ILogger logger, Guid instanceId, string errorCode);
}
