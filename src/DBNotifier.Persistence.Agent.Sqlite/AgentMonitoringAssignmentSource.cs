// Module purpose: Implements Agent Monitoring Assignment Source for the Agent-local SQLite boundary without exposing monitored database secrets.
using System.Text.Json;
using DBNotifier.Application.Monitoring;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed partial class AgentMonitoringAssignmentSource(
    IDbContextFactory<AgentDbContext> contextFactory,
    ILogger<AgentMonitoringAssignmentSource> logger) : IMonitoringAssignmentSource
{
    public async ValueTask<IReadOnlyList<MonitoringAssignment>> GetDueAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using AgentDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        AgentInstanceAssignmentRow[] configured = await context.InstanceAssignments
            .AsNoTracking()
            .Where(row => row.Enabled)
            .OrderBy(row => row.InstanceId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (configured.Length == 0)
        {
            return [];
        }

        HashSet<Guid> configuredIds = configured.Select(row => row.InstanceId).ToHashSet();
        var observationTimes = await context.HealthObservations
            .AsNoTracking()
            .Where(row => configuredIds.Contains(row.InstanceId))
            .Select(row => new { row.InstanceId, row.ObservedAt })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        Dictionary<Guid, DateTimeOffset> latestByInstance = observationTimes
            .GroupBy(row => row.InstanceId)
            .ToDictionary(group => group.Key, group => group.Max(row => row.ObservedAt));

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

        return due;
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
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("referenceId", out JsonElement referenceIdElement) ||
                !Guid.TryParse(referenceIdElement.GetString(), out Guid referenceId) ||
                !root.TryGetProperty("vaultProvider", out JsonElement vaultProviderElement) ||
                !root.TryGetProperty("locator", out JsonElement locatorElement))
            {
                return false;
            }

            reference = new CredentialReference(
                referenceId,
                vaultProviderElement.GetString() ?? string.Empty,
                locatorElement.GetString() ?? string.Empty,
                CredentialPurpose.Monitoring);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Warning,
        Message = "Skipping invalid monitoring assignment {InstanceId}: {ErrorCode}")]
    private static partial void LogInvalidAssignment(ILogger logger, Guid instanceId, string errorCode);
}
