// Module purpose: Implements Http Command Delivery Transport as an outer adapter behind application or provider contracts.
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.Http;

namespace DBNotifier.Infrastructure.Synchronization;

/// <summary>
/// Preserves the contained legacy command-delivery adapter while bounding every response; normal composition does
/// not register this transport and R2-A continues to prohibit command polling.
/// </summary>
/// <param name="httpClient">Caller-owned authenticated HTTP client.</param>
/// <param name="serverBaseAddress">Configured HTTPS Server boundary.</param>
/// <param name="agentVersion">Bounded Agent protocol version.</param>
public sealed class HttpCommandDeliveryTransport(HttpClient httpClient, Uri serverBaseAddress, string agentVersion)
    : ICommandDeliveryTransport
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

    /// <inheritdoc />
    public async ValueTask<CommandPollResponse> PollAsync(
        CommandPollRequest request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = Create(request.AgentId, "commands:poll", request);
        using HttpResponseMessage response = await httpClient.SendAsync(
            message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await BoundedHttpJsonReader.ReadAsync<CommandPollResponse>(
            response.Content,
            MaximumResponseBytes,
            SerializerOptions,
            JsonMediaTypes,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<CommandAcknowledgementResponse> AcknowledgeAsync(
        CommandAcknowledgementRequest request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = Create(request.AgentId, "commands:ack", request);
        using HttpResponseMessage response = await httpClient.SendAsync(
            message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await BoundedHttpJsonReader.ReadAsync<CommandAcknowledgementResponse>(
            response.Content,
            MaximumResponseBytes,
            SerializerOptions,
            JsonMediaTypes,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates one contained v1 request without changing the transport's disabled composition status.</summary>
    private HttpRequestMessage Create<T>(Guid agentId, string operation, T payload)
    {
        if (!serverBaseAddress.IsAbsoluteUri || serverBaseAddress.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(serverBaseAddress.UserInfo) || !string.IsNullOrEmpty(serverBaseAddress.Query) ||
            !string.IsNullOrEmpty(serverBaseAddress.Fragment))
        {
            throw new InvalidOperationException("Command delivery requires an absolute HTTPS server address.");
        }

        HttpRequestMessage message = new(HttpMethod.Post,
            new Uri(serverBaseAddress, $"api/v1/agents/{agentId:D}/{operation}"))
        {
            Content = JsonContent.Create(payload, options: SerializerOptions),
        };
        message.Headers.Add("DBN-Protocol-Version", "1");
        message.Headers.Add("DBN-Agent-Version", agentVersion);
        message.Headers.Add("DBN-Message-Schema", "1");
        return message;
    }
}
