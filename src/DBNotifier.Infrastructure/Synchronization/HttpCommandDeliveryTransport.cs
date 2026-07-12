// Module purpose: Implements Http Command Delivery Transport as an outer adapter behind application or provider contracts.
using System.Net.Http.Json;
using System.Text.Json;
using DBNotifier.Application.Synchronization;

namespace DBNotifier.Infrastructure.Synchronization;

public sealed class HttpCommandDeliveryTransport(HttpClient httpClient, Uri serverBaseAddress, string agentVersion)
    : ICommandDeliveryTransport
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<CommandPollResponse> PollAsync(
        CommandPollRequest request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = Create(request.AgentId, "commands:poll", request);
        using HttpResponseMessage response = await httpClient.SendAsync(
            message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandPollResponse>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidDataException("The command poll response is empty.");
    }

    public async ValueTask<CommandAcknowledgementResponse> AcknowledgeAsync(
        CommandAcknowledgementRequest request,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = Create(request.AgentId, "commands:ack", request);
        using HttpResponseMessage response = await httpClient.SendAsync(
            message, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommandAcknowledgementResponse>(SerializerOptions, cancellationToken)
            .ConfigureAwait(false) ?? throw new InvalidDataException("The command acknowledgement response is empty.");
    }

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
