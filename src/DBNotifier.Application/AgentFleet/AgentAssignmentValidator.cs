// Module purpose: Validates provider-neutral Agent assignments at both distribution and local persistence trust boundaries.
using System.Text.Json;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Application.AgentFleet;

/// <summary>Validates complete non-secret assignment content against the currently registered provider contracts.</summary>
public interface IAgentAssignmentValidator
{
    /// <summary>
    /// Validates one assignment without resolving a credential or contacting a provider endpoint.
    /// </summary>
    /// <param name="assignment">Untrusted assignment candidate.</param>
    /// <param name="expectedEnvironment">Exact Agent environment authorised for the snapshot.</param>
    /// <param name="errorCode">Stable sanitised failure code, or <see langword="null"/> on success.</param>
    /// <returns><see langword="true"/> only when provider, endpoint, tags and monitoring reference are safe.</returns>
    bool TryValidate(
        AgentReadOnlyAssignment assignment,
        string expectedEnvironment,
        out string? errorCode);
}

/// <summary>
/// Enforces the canonical non-secret assignment envelope and delegates endpoint-field semantics only to the
/// exact provider registered in the open Provider SDK registry.
/// </summary>
/// <param name="providerRegistry">Provider registry available at the current trust boundary.</param>
public sealed class AgentAssignmentValidator(IProviderRegistry providerRegistry) : IAgentAssignmentValidator
{
    private const int MaximumJsonDepth = 16;
    private const int MaximumJsonNodes = 2048;
    private const int MaximumCredentialReferenceLength = 500;
    private const int MaximumVaultProviderLength = 64;
    private const int MaximumLocatorLength = 300;
    private static readonly HashSet<string> CredentialKeys = new(StringComparer.Ordinal)
    {
        "referenceId",
        "vaultProvider",
        "locator",
        "purpose",
    };
    private static readonly string[] SecretLikeKeyFragments =
    [
        "password",
        "passwd",
        "secret",
        "token",
        "connectionstring",
        "privatekey",
        "apikey",
        "accesskey",
    ];

    private readonly IProviderRegistry registry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));

    /// <inheritdoc />
    public bool TryValidate(
        AgentReadOnlyAssignment assignment,
        string expectedEnvironment,
        out string? errorCode)
    {
        errorCode = "assignments.non_secret_validation_failed";
        if (assignment is null || string.IsNullOrWhiteSpace(expectedEnvironment) ||
            !string.Equals(assignment.Environment, expectedEnvironment, StringComparison.Ordinal) ||
            !ProviderType.TryParse(assignment.ProviderType, out ProviderType providerType) ||
            !string.Equals(providerType.Value, assignment.ProviderType, StringComparison.Ordinal) ||
            assignment.Endpoint.ValueKind != JsonValueKind.Object ||
            assignment.Tags.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
        {
            return false;
        }

        if (!TryInspectJson(assignment.Endpoint, requirePrimitiveObjectValues: true, out errorCode) ||
            !TryInspectJson(assignment.Tags, requirePrimitiveObjectValues: false, out errorCode))
        {
            return false;
        }

        if (!TryCreateEndpoint(providerType, assignment.Endpoint, out ProviderEndpoint? endpoint) ||
            !registry.TryResolve(providerType, out IDatabaseProvider? provider) ||
            !provider.ValidateEndpoint(endpoint).IsValid)
        {
            errorCode = "assignments.provider_configuration_invalid";
            return false;
        }

        if (assignment.MonitoringCredentialReference is not null &&
            !TryParseMonitoringCredentialReference(assignment.MonitoringCredentialReference, out _))
        {
            errorCode = "assignments.credential_reference_invalid";
            return false;
        }

        errorCode = null;
        return true;
    }

    /// <summary>Parses only the exact monitoring-purpose credential-reference wire schema.</summary>
    /// <param name="json">Bounded opaque-reference JSON; it must not contain resolved secret material.</param>
    /// <param name="reference">Validated domain reference, or <see langword="null"/> on refusal.</param>
    /// <returns><see langword="true"/> only for the four exact fields and <c>Monitoring</c> purpose.</returns>
    public static bool TryParseMonitoringCredentialReference(
        string json,
        out CredentialReference? reference)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumCredentialReferenceLength)
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaximumJsonDepth,
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            JsonProperty[] properties = root.EnumerateObject().ToArray();
            if (properties.Length != CredentialKeys.Count ||
                properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length ||
                properties.Any(property => !CredentialKeys.Contains(property.Name)) ||
                !root.TryGetProperty("referenceId", out JsonElement referenceIdElement) ||
                referenceIdElement.ValueKind != JsonValueKind.String ||
                !Guid.TryParseExact(referenceIdElement.GetString(), "D", out Guid referenceId) ||
                referenceId == Guid.Empty ||
                !root.TryGetProperty("vaultProvider", out JsonElement vaultProviderElement) ||
                vaultProviderElement.ValueKind != JsonValueKind.String ||
                !IsStableIdentifier(vaultProviderElement.GetString(), MaximumVaultProviderLength) ||
                !root.TryGetProperty("locator", out JsonElement locatorElement) ||
                locatorElement.ValueKind != JsonValueKind.String ||
                !IsSafeLocator(locatorElement.GetString()) ||
                !root.TryGetProperty("purpose", out JsonElement purposeElement) ||
                purposeElement.ValueKind != JsonValueKind.String ||
                !string.Equals(purposeElement.GetString(), nameof(CredentialPurpose.Monitoring), StringComparison.Ordinal))
            {
                return false;
            }

            reference = new CredentialReference(
                referenceId,
                vaultProviderElement.GetString()!,
                locatorElement.GetString()!,
                CredentialPurpose.Monitoring);
            return true;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool TryCreateEndpoint(
        ProviderType providerType,
        JsonElement endpointElement,
        out ProviderEndpoint endpoint)
    {
        endpoint = null!;
        try
        {
            List<KeyValuePair<string, string>> properties = [];
            foreach (JsonProperty property in endpointElement.EnumerateObject())
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
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryInspectJson(
        JsonElement root,
        bool requirePrimitiveObjectValues,
        out string? errorCode)
    {
        int nodes = 0;
        Stack<(JsonElement Element, int Depth)> pending = new();
        pending.Push((root, 1));
        while (pending.TryPop(out (JsonElement Element, int Depth) current))
        {
            nodes++;
            if (nodes > MaximumJsonNodes || current.Depth > MaximumJsonDepth)
            {
                errorCode = "assignments.json_limit_exceeded";
                return false;
            }

            if (current.Element.ValueKind == JsonValueKind.Object)
            {
                HashSet<string> names = new(StringComparer.Ordinal);
                foreach (JsonProperty property in current.Element.EnumerateObject())
                {
                    if (!names.Add(property.Name) || IsSecretLikeKey(property.Name))
                    {
                        errorCode = "assignments.secret_like_field_rejected";
                        return false;
                    }

                    if (requirePrimitiveObjectValues && current.Depth == 1 &&
                        property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null)
                    {
                        errorCode = "assignments.endpoint_shape_invalid";
                        return false;
                    }

                    pending.Push((property.Value, current.Depth + 1));
                }
            }
            else if (current.Element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in current.Element.EnumerateArray())
                {
                    pending.Push((item, current.Depth + 1));
                }
            }
        }

        errorCode = null;
        return true;
    }

    private static bool IsSecretLikeKey(string key)
    {
        string canonical = new(key.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        return SecretLikeKeyFragments.Any(fragment => canonical.Contains(fragment, StringComparison.Ordinal));
    }

    private static bool IsStableIdentifier(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && value == value.Trim() &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    private static bool IsSafeLocator(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= MaximumLocatorLength && value == value.Trim() &&
        !value.Any(char.IsControl) && !value.Contains('=') && !value.Contains(';') &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-' or '/');
}
