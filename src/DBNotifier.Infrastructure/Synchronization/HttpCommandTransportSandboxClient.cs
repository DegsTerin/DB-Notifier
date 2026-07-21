// Module purpose: Sends only bounded v2 command-transport sandbox messages over HTTPS loopback and validates responses before materialisation.
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.Http;

namespace DBNotifier.Infrastructure.Synchronization;

/// <summary>Implements the loopback-only HTTPS client for the isolated command-transport safety sandbox.</summary>
public sealed class HttpCommandTransportSandboxClient(
    HttpClient httpClient,
    Uri serverBaseAddress,
    string agentVersion) : ICommandTransportSandboxClient
{
    private static readonly HashSet<string> JsonMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
    };
    private static readonly HashSet<string> ProblemMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/problem+json",
        "application/json",
    };

    /// <inheritdoc />
    public ValueTask<CommandTransportPollResponse> PollAsync(
        CommandTransportPollRequest request,
        CancellationToken cancellationToken) => SendAsync<CommandTransportPollRequest, CommandTransportPollResponse>(
            request.AgentId,
            "commands:poll",
            request,
            cancellationToken);

    /// <inheritdoc />
    public ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
        CommandTransportAcknowledgementRequest request,
        CancellationToken cancellationToken) =>
        SendAsync<CommandTransportAcknowledgementRequest, CommandTransportAcknowledgementResponse>(
            request.AgentId,
            "commands:ack",
            request,
            cancellationToken);

    private async ValueTask<TResponse> SendAsync<TRequest, TResponse>(
        Guid agentId,
        string operation,
        TRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBoundary(agentId);
        string json = CommandTransportCodec.Serialize(request);
        byte[] payload = Encoding.UTF8.GetBytes(json);
        if (payload.Length > CommandTransportProtocol.MaximumHttpBodyBytes)
        {
            throw new InvalidDataException("command.transport_request_too_large");
        }

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            new Uri(serverBaseAddress, $"api/v2/sandbox/agents/{agentId:D}/{operation}"))
        {
            Content = new ByteArrayContent(payload),
        };
        message.Content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        string version = CommandTransportProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture);
        message.Headers.Add("DBN-Protocol-Version", version);
        message.Headers.Add("DBN-Message-Schema", version);
        message.Headers.Add("DBN-Agent-Version", agentVersion);
        using HttpResponseMessage response = await httpClient.SendAsync(
            message,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new CommandTransportException(
                    CommandTransportFailureKind.AgentInactive,
                    "agent.identity_inactive");
            }

            byte[] problemBytes = await BoundedHttpJsonReader.ReadBytesAsync(
                response.Content,
                CommandTransportProtocol.MaximumHttpBodyBytes,
                ProblemMediaTypes,
                cancellationToken).ConfigureAwait(false);
            string problemJson = Encoding.UTF8.GetString(problemBytes);
            CommandTransportProblem problem;
            try
            {
                problem = CommandTransportCodec.Deserialize<CommandTransportProblem>(problemJson);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException)
            {
                throw new HttpRequestException(
                    "command.transport_problem_invalid",
                    exception,
                    response.StatusCode);
            }

            if (problem.Retryable || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new HttpRequestException(problem.Code, null, response.StatusCode);
            }

            throw new CommandTransportException(
                response.StatusCode == HttpStatusCode.Forbidden
                    ? CommandTransportFailureKind.AgentInactive
                    : response.StatusCode == HttpStatusCode.Conflict
                        ? CommandTransportFailureKind.Conflict
                        : CommandTransportFailureKind.Invalid,
                problem.Code,
                problem.ExpectedSequence);
        }

        if (!HasVersion(response, "DBN-Protocol-Version") || !HasVersion(response, "DBN-Message-Schema"))
        {
            throw new InvalidDataException("command.transport_response_headers_invalid");
        }

        byte[] responseBytes = await BoundedHttpJsonReader.ReadBytesAsync(
            response.Content,
            CommandTransportProtocol.MaximumHttpBodyBytes,
            JsonMediaTypes,
            cancellationToken).ConfigureAwait(false);
        string responseJson = Encoding.UTF8.GetString(responseBytes);
        try
        {
            return CommandTransportCodec.Deserialize<TResponse>(responseJson);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("command.transport_response_invalid", exception);
        }
    }

    private void ValidateBoundary(Guid agentId)
    {
        if (agentId == Guid.Empty || !serverBaseAddress.IsAbsoluteUri ||
            !string.Equals(serverBaseAddress.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !serverBaseAddress.IsLoopback || !string.IsNullOrEmpty(serverBaseAddress.UserInfo) ||
            !string.IsNullOrEmpty(serverBaseAddress.Query) || !string.IsNullOrEmpty(serverBaseAddress.Fragment) ||
            string.IsNullOrWhiteSpace(agentVersion) || agentVersion.Length > 64)
        {
            throw new InvalidOperationException("command.transport_sandbox_boundary_invalid");
        }
    }

    private static bool HasVersion(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values) &&
        values.SequenceEqual(
            [CommandTransportProtocol.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)],
            StringComparer.Ordinal);
}
