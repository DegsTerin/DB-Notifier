// Module purpose: Evaluates provider-neutral alert rule event and instance scope without performing delivery.
using System.Text.Json;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Applies the canonical fail-closed rule match used during delivery creation and defensive pending selection.
/// Malformed or incomplete rule JSON never broadens scope.
/// </summary>
internal static class AlertRoutingPolicy
{
    /// <summary>Determines whether one active rule admits the exact event type and instance.</summary>
    /// <param name="rule">Persisted rule with non-secret event configuration and optional instance scope.</param>
    /// <param name="eventType">Canonical event type under evaluation.</param>
    /// <param name="instanceId">Exact database instance that produced the event.</param>
    /// <returns><see langword="true"/> only when both event and instance scope are proven.</returns>
    internal static bool Matches(AlertRuleRow rule, string eventType, Guid instanceId)
    {
        try
        {
            using JsonDocument configuration = JsonDocument.Parse(rule.ConfigurationJson);
            if (!configuration.RootElement.TryGetProperty("eventTypes", out JsonElement eventTypes) ||
                eventTypes.ValueKind != JsonValueKind.Array ||
                !eventTypes.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String &&
                    string.Equals(item.GetString(), eventType, StringComparison.Ordinal)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(rule.InstanceScopeJson))
            {
                return true;
            }

            using JsonDocument scope = JsonDocument.Parse(rule.InstanceScopeJson);
            return scope.RootElement.TryGetProperty("instanceIds", out JsonElement instanceIds) &&
                instanceIds.ValueKind == JsonValueKind.Array &&
                instanceIds.EnumerateArray().Any(item =>
                    item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out Guid value) &&
                    value == instanceId);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
