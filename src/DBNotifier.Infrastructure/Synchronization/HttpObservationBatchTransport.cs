// Module purpose: Implements Http Observation Batch Transport as an outer adapter behind application or provider contracts.
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.Http;

namespace DBNotifier.Infrastructure.Synchronization;

/// <summary>Transfers bounded canonical observation batches to one configured HTTPS Server boundary.</summary>
public sealed class HttpObservationBatchTransport : IObservationBatchTransport
{
    private const int MaximumResponseBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        MaxDepth = 16,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
    private static readonly HashSet<string> JsonMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
    };
    private readonly HttpClient httpClient;
    private readonly Uri serverBaseAddress;
    private readonly string agentVersion;

    /// <summary>Initialises one transport without opening a connection or resolving credentials.</summary>
    /// <param name="httpClient">Caller-owned authenticated HTTP client.</param>
    /// <param name="serverBaseAddress">Absolute HTTPS Server base address without user info, query or fragment.</param>
    /// <param name="agentVersion">Bounded Agent version header value.</param>
    /// <exception cref="ArgumentNullException">Thrown when the client or address is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the address or Agent version is invalid.</exception>
    public HttpObservationBatchTransport(
        HttpClient httpClient,
        Uri serverBaseAddress,
        string agentVersion)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(serverBaseAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentVersion);
        if (!serverBaseAddress.IsAbsoluteUri ||
            !string.Equals(serverBaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(serverBaseAddress.UserInfo) ||
            !string.IsNullOrEmpty(serverBaseAddress.Query) ||
            !string.IsNullOrEmpty(serverBaseAddress.Fragment))
        {
            throw new ArgumentException(
                "Observation synchronization requires an absolute HTTPS base address without user info, query, or fragment.",
                nameof(serverBaseAddress));
        }

        this.httpClient = httpClient;
        this.serverBaseAddress = serverBaseAddress;
        this.agentVersion = agentVersion;
    }

    /// <inheritdoc />
    public async ValueTask<ObservationBatchResult> SendAsync(
        Guid agentId,
        IReadOnlyList<AgentOutboxEnvelope> messages,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(messages);

        List<ObservationSyncMessage> valid = [];
        List<ObservationItemResult> localResults = [];
        foreach (AgentOutboxEnvelope envelope in messages)
        {
            if (!string.Equals(envelope.MessageType, "health.observation.v1", StringComparison.Ordinal) ||
                envelope.SchemaVersion != 1 ||
                !TryDeserialize(envelope, out ObservationSyncMessage? message))
            {
                localResults.Add(new ObservationItemResult(
                    envelope.MessageId,
                    ObservationIngestionDisposition.Rejected,
                    "sync.payload_unsupported"));
                continue;
            }

            valid.Add(message);
        }

        if (valid.Count == 0)
        {
            return new ObservationBatchResult(localResults, 0);
        }

        Uri endpoint = new(serverBaseAddress, $"api/v1/agents/{agentId:D}/observations:batch");
        using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new ObservationBatchRequest(agentId, valid), options: SerializerOptions),
        };
        request.Headers.Add("DBN-Protocol-Version", "1");
        request.Headers.Add("DBN-Agent-Version", agentVersion);
        request.Headers.Add("DBN-Message-Schema", "1");

        try
        {
            using HttpResponseMessage response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    ObservationBatchResult result = await BoundedHttpJsonReader.ReadAsync<ObservationBatchResult>(
                        response.Content,
                        MaximumResponseBytes,
                        SerializerOptions,
                        JsonMediaTypes,
                        cancellationToken).ConfigureAwait(false);
                    if (IsValidSuccessResponse(result, valid))
                    {
                        return new ObservationBatchResult(
                            [.. localResults, .. result.Items],
                            result.HighestContiguousSequence);
                    }
                }
                catch (Exception exception) when (
                    exception is JsonException or NotSupportedException or BoundedHttpJsonException)
                {
                    // A successful status with an invalid contract must never terminally acknowledge local data.
                }

                localResults.AddRange(valid.Select(message => new ObservationItemResult(
                    message.MessageId,
                    ObservationIngestionDisposition.Retryable,
                    "sync.response_invalid")));
                return new ObservationBatchResult(localResults, 0);
            }

            ObservationIngestionDisposition disposition = IsRetryable(response.StatusCode)
                ? ObservationIngestionDisposition.Retryable
                : ObservationIngestionDisposition.Rejected;
            string errorCode = disposition == ObservationIngestionDisposition.Retryable
                ? "sync.server_unavailable"
                : "sync.request_rejected";
            DateTimeOffset? retryAfter = response.Headers.RetryAfter?.Date;
            localResults.AddRange(valid.Select(message =>
                new ObservationItemResult(message.MessageId, disposition, errorCode, retryAfter)));
            return new ObservationBatchResult(localResults, 0);
        }
        catch (HttpRequestException)
        {
            localResults.AddRange(valid.Select(message => new ObservationItemResult(
                message.MessageId,
                ObservationIngestionDisposition.Retryable,
                "sync.transport_unavailable")));
            return new ObservationBatchResult(localResults, 0);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            localResults.AddRange(valid.Select(message => new ObservationItemResult(
                message.MessageId,
                ObservationIngestionDisposition.Retryable,
                "sync.transport_timeout")));
            return new ObservationBatchResult(localResults, 0);
        }
    }

    private static bool TryDeserialize(
        AgentOutboxEnvelope envelope,
        out ObservationSyncMessage message)
    {
        message = null!;
        try
        {
            ObservationPayload? payload = JsonSerializer.Deserialize<ObservationPayload>(
                envelope.PayloadJson,
                SerializerOptions);
            if (payload is null || payload.MessageId != envelope.MessageId || payload.SchemaVersion != envelope.SchemaVersion)
            {
                return false;
            }

            message = new ObservationSyncMessage(
                envelope.MessageId,
                envelope.SchemaVersion,
                envelope.Sequence,
                payload.ObservationId,
                payload.InstanceId,
                payload.AgentId,
                payload.ProviderType,
                payload.ProviderVersion,
                payload.Status,
                payload.Method,
                payload.EvidenceLevel,
                payload.AttemptCount,
                payload.ObservedAt,
                payload.DurationMilliseconds,
                payload.ErrorCode,
                payload.SafeErrorMessage,
                payload.Limitations ?? []);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    /// <summary>Accepts only one terminal or retryable result for every message sent in the HTTP batch.</summary>
    private static bool IsValidSuccessResponse(
        ObservationBatchResult result,
        List<ObservationSyncMessage> sent)
    {
        if (result.Items is null || result.Items.Count != sent.Count || result.HighestContiguousSequence < 0)
        {
            return false;
        }

        HashSet<Guid> expected = sent.Select(message => message.MessageId).ToHashSet();
        HashSet<Guid> returned = [];
        foreach (ObservationItemResult? item in result.Items)
        {
            if (item is null || !expected.Contains(item.MessageId) ||
                !Enum.IsDefined(item.Disposition) || !returned.Add(item.MessageId))
            {
                return false;
            }
        }

        return returned.SetEquals(expected);
    }

    private sealed record ObservationPayload(
        Guid MessageId,
        int SchemaVersion,
        Guid ObservationId,
        Guid InstanceId,
        Guid AgentId,
        string ProviderType,
        string ProviderVersion,
        string Status,
        string Method,
        string EvidenceLevel,
        int AttemptCount,
        DateTimeOffset ObservedAt,
        long DurationMilliseconds,
        string? ErrorCode,
        string? SafeErrorMessage,
        IReadOnlyList<string>? Limitations);
}
