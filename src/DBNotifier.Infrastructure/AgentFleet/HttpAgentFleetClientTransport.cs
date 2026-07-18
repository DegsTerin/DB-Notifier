// Module purpose: Implements bounded HTTP transport for Agent Fleet enrollment, heartbeat and read-only assignment reconciliation.
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.AgentFleet;

namespace DBNotifier.Infrastructure.AgentFleet;

/// <summary>
/// Sends only the three Agent Fleet v1 operations authorised for the Agent-side sandbox slice. Client-certificate
/// selection remains behind the supplied factory, so this adapter never owns or persists private key material.
/// </summary>
/// <param name="baseAddress">Absolute HTTPS API base address.</param>
/// <param name="agentVersion">Bounded Agent version used for negotiation.</param>
/// <param name="clientFactory">Returns the anonymous client for a null reference or an mTLS client for an identity reference.</param>
public sealed class HttpAgentFleetClientTransport(
    Uri baseAddress,
    string agentVersion,
    Func<string?, HttpClient> clientFactory) : IAgentFleetClientTransport
{
    private const int MaximumSmallResponseBytes = 128 * 1024;
    private const int MaximumAssignmentResponseBytes = 16 * 1024 * 1024;
    private const string ProtocolVersionHeader = "DBN-Protocol-Version";
    private const string ProtocolMinimumHeader = "DBN-Protocol-Minimum";
    private const string ProtocolMaximumHeader = "DBN-Protocol-Maximum";
    private const string AgentVersionHeader = "DBN-Agent-Version";
    private const string MessageSchemaHeader = "DBN-Message-Schema";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32,
    };

    private readonly Uri apiBaseAddress = ValidateBaseAddress(baseAddress);
    private readonly string boundedAgentVersion = ValidateAgentVersion(agentVersion);
    private readonly Func<string?, HttpClient> boundedClientFactory =
        clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

    /// <inheritdoc />
    public async ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
        string token,
        AgentEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(request);
        if (token.Length > 512)
        {
            throw new ArgumentException("Enrollment token is outside policy.", nameof(token));
        }

        using HttpRequestMessage message = CreateRequest(HttpMethod.Post, "api/v1/agents/enroll", request.SchemaVersion);
        message.Headers.Authorization = new AuthenticationHeaderValue("DBN-Enrollment", token);
        message.Content = JsonContent.Create(request, options: JsonOptions);
        return await SendAsync<AgentEnrollmentOutcome>(
            boundedClientFactory(null),
            message,
            MaximumSmallResponseBytes,
            allowNotModified: false,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
        string identityReference,
        AgentHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        ValidateIdentityReference(identityReference);
        ArgumentNullException.ThrowIfNull(request);
        using HttpRequestMessage message = CreateRequest(
            HttpMethod.Post,
            $"api/v1/agents/{request.AgentId:D}/heartbeats",
            request.SchemaVersion);
        message.Content = JsonContent.Create(request, options: JsonOptions);
        return await SendAsync<AgentHeartbeatOutcome>(
            boundedClientFactory(identityReference),
            message,
            MaximumSmallResponseBytes,
            allowNotModified: false,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
        string identityReference,
        Guid agentId,
        string agentVersion,
        string? currentVersion,
        CancellationToken cancellationToken)
    {
        ValidateIdentityReference(identityReference);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        if (!string.Equals(ValidateAgentVersion(agentVersion), boundedAgentVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException("Agent version differs from the configured transport version.", nameof(agentVersion));
        }

        string relative = $"api/v1/agents/{agentId:D}/assignments";
        if (currentVersion is not null)
        {
            if (!AgentAssignmentVersion.IsUpperHexDigest(currentVersion))
            {
                throw new ArgumentException("Assignment version is outside policy.", nameof(currentVersion));
            }

            relative += $"?afterVersion={Uri.EscapeDataString(currentVersion)}";
        }

        using HttpRequestMessage message = CreateRequest(
            HttpMethod.Get,
            relative,
            AgentFleetProtocol.CurrentSchemaVersion);
        if (currentVersion is not null)
        {
            message.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{currentVersion}\""));
        }

        return await SendAsync<AgentAssignmentSnapshot>(
            boundedClientFactory(identityReference),
            message,
            MaximumAssignmentResponseBytes,
            allowNotModified: true,
            cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relative, int schemaVersion)
    {
        HttpRequestMessage request = new(method, new Uri(apiBaseAddress, relative));
        request.Headers.TryAddWithoutValidation(
            ProtocolVersionHeader,
            AgentFleetProtocol.CurrentProtocolVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation(AgentVersionHeader, boundedAgentVersion);
        request.Headers.TryAddWithoutValidation(
            MessageSchemaHeader,
            schemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return request;
    }

    private static async ValueTask<AgentFleetTransportResult<T>> SendAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        int maximumResponseBytes,
        bool allowNotModified,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        try
        {
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            string? entityTag = response.Headers.ETag?.ToString();
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotModified)
            {
                ProblemEvidence? problem = await ReadProblemEvidenceAsync(response.Content, cancellationToken)
                    .ConfigureAwait(false);
                string errorCode = problem?.Code ?? "protocol.request_failed";
                AgentFleetTransportDisposition disposition = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => AgentFleetTransportDisposition.Denied,
                    HttpStatusCode.UpgradeRequired => AgentFleetTransportDisposition.Incompatible,
                    HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or
                        HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or
                        HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout =>
                        AgentFleetTransportDisposition.TransientFailure,
                    _ => AgentFleetTransportDisposition.InvalidResponse,
                };
                if (disposition == AgentFleetTransportDisposition.TransientFailure && problem?.Retryable == false)
                {
                    disposition = AgentFleetTransportDisposition.InvalidResponse;
                }

                return Failure<T>(
                    disposition,
                    errorCode,
                    entityTag,
                    disposition == AgentFleetTransportDisposition.TransientFailure
                        ? ReadRetryAfterCeiling(response)
                        : null);
            }

            if (!TryValidateVersionHeaders(response))
            {
                return Failure<T>(AgentFleetTransportDisposition.InvalidResponse, "protocol.response_invalid", entityTag);
            }

            if (allowNotModified && response.StatusCode == HttpStatusCode.NotModified)
            {
                return new AgentFleetTransportResult<T>(
                    AgentFleetTransportDisposition.NotModified,
                    default,
                    entityTag,
                    null);
            }

            if (response.IsSuccessStatusCode)
            {
                T? value = await ReadBoundedJsonAsync<T>(response.Content, maximumResponseBytes, cancellationToken)
                    .ConfigureAwait(false);
                return value is null
                    ? Failure<T>(AgentFleetTransportDisposition.InvalidResponse, "protocol.response_invalid", entityTag)
                    : new AgentFleetTransportResult<T>(
                        AgentFleetTransportDisposition.Succeeded,
                        value,
                        entityTag,
                        null);
            }

            return Failure<T>(AgentFleetTransportDisposition.InvalidResponse, "protocol.response_invalid", entityTag);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure<T>(AgentFleetTransportDisposition.TransientFailure, "transport.timeout", null);
        }
        catch (HttpRequestException)
        {
            return Failure<T>(AgentFleetTransportDisposition.TransientFailure, "transport.unavailable", null);
        }
        catch (JsonException)
        {
            return Failure<T>(AgentFleetTransportDisposition.InvalidResponse, "protocol.response_invalid", null);
        }
        catch (InvalidDataException)
        {
            return Failure<T>(AgentFleetTransportDisposition.InvalidResponse, "protocol.response_too_large", null);
        }
    }

    private static async ValueTask<T?> ReadBoundedJsonAsync<T>(
        HttpContent content,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is long length && (length < 0 || length > maximumBytes))
        {
            throw new InvalidDataException("Agent Fleet response exceeds its admitted byte ceiling.");
        }

        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream buffer = new(Math.Min(maximumBytes, 64 * 1024));
        byte[] chunk = new byte[16 * 1024];
        int total = 0;
        while (true)
        {
            int read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total = checked(total + read);
            if (total > maximumBytes)
            {
                throw new InvalidDataException("Agent Fleet response exceeds its admitted byte ceiling.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return JsonSerializer.Deserialize<T>(buffer.GetBuffer().AsSpan(0, total), JsonOptions);
    }

    /// <summary>Reads bounded machine code and retry authority from a Problem Details response.</summary>
    /// <param name="content">Untrusted response body.</param>
    /// <param name="cancellationToken">Cancellation for the bounded content read.</param>
    /// <returns>Validated evidence fields, or <see langword="null"/> when the body is absent or malformed.</returns>
    private static async ValueTask<ProblemEvidence?> ReadProblemEvidenceAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument problem = await ReadBoundedJsonAsync<JsonDocument>(
                content,
                MaximumSmallResponseBytes,
                cancellationToken).ConfigureAwait(false) ?? throw new JsonException();
            string? code = problem.RootElement.TryGetProperty("code", out JsonElement codeElement) &&
                codeElement.ValueKind == JsonValueKind.String &&
                codeElement.GetString() is { Length: > 0 and <= 100 } value
                    ? value
                    : null;
            bool? retryable = problem.RootElement.TryGetProperty("retryable", out JsonElement retryableElement) &&
                retryableElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? retryableElement.GetBoolean()
                    : null;
            return code is null && retryable is null ? null : new ProblemEvidence(code, retryable);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Contains the only Problem Details fields allowed to influence retry classification.</summary>
    /// <param name="Code">Optional bounded machine-readable code.</param>
    /// <param name="Retryable">Optional explicit retry authority.</param>
    private sealed record ProblemEvidence(string? Code, bool? Retryable);

    private static bool TryValidateVersionHeaders(HttpResponseMessage response)
    {
        string expected = AgentFleetProtocol.CurrentProtocolVersion.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        return HasSingleHeader(response, ProtocolVersionHeader, expected) &&
            HasSingleHeader(response, ProtocolMinimumHeader, expected) &&
            HasSingleHeader(response, ProtocolMaximumHeader, expected);
    }

    private static bool HasSingleHeader(HttpResponseMessage response, string name, string expected) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values) &&
        values is not null && values.ToArray() is [var value] &&
        string.Equals(value, expected, StringComparison.Ordinal);

    private static AgentFleetTransportResult<T> Failure<T>(
        AgentFleetTransportDisposition disposition,
        string errorCode,
        string? entityTag,
        TimeSpan? retryAfter = null) => new(disposition, default, entityTag, errorCode, retryAfter);

    /// <summary>Reads only a bounded delta Retry-After value for deterministic sandbox scheduling.</summary>
    /// <param name="response">Transient HTTP response.</param>
    /// <returns>A non-negative ceiling no greater than five minutes, or <see langword="null"/>.</returns>
    private static TimeSpan? ReadRetryAfterCeiling(HttpResponseMessage response)
    {
        TimeSpan? delta = response.Headers.RetryAfter?.Delta;
        return delta is { } value && value >= TimeSpan.Zero && value <= TimeSpan.FromMinutes(5)
            ? value
            : null;
    }

    private static Uri ValidateBaseAddress(Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!value.IsAbsoluteUri || !string.Equals(value.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(value.UserInfo) || !string.IsNullOrEmpty(value.Query) ||
            !string.IsNullOrEmpty(value.Fragment))
        {
            throw new ArgumentException("Agent Fleet transport requires an absolute HTTPS base address.", nameof(value));
        }

        return new Uri(value.AbsoluteUri.TrimEnd('/') + '/', UriKind.Absolute);
    }

    private static string ValidateAgentVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64 ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or ':' or '-')))
        {
            throw new ArgumentException("Agent version is outside policy.", nameof(value));
        }

        return value;
    }

    private static void ValidateIdentityReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 500 || value.Any(char.IsControl))
        {
            throw new ArgumentException("Agent identity reference is outside policy.", nameof(value));
        }
    }
}
