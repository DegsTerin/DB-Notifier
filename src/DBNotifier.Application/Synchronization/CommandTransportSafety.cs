// Module purpose: Defines the execution-ineligible command-transport sandbox protocol, durable state boundaries and bounded coordinator without exposing any administrative executor.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DBNotifier.Application.Synchronization;

/// <summary>Defines exact schema, resource and retry limits for the local command-transport safety sandbox.</summary>
public static class CommandTransportProtocol
{
    /// <summary>Gets the only schema version accepted by the isolated sandbox protocol.</summary>
    public const int CurrentSchemaVersion = 2;

    /// <summary>Gets the maximum number of command or acknowledgement items accepted in one message.</summary>
    public const int MaximumBatchSize = 16;

    /// <summary>Gets the maximum number of provider-version declarations accepted in one poll.</summary>
    public const int MaximumProviderVersions = 32;

    /// <summary>Gets the maximum UTF-8 size of one complete HTTP request or response body.</summary>
    public const int MaximumHttpBodyBytes = 64 * 1024;

    /// <summary>Gets the maximum UTF-8 size of synthetic typed parameters.</summary>
    public const int MaximumTypedParametersBytes = 4 * 1024;

    /// <summary>Gets the fixed per-attempt timeout used only by the local sandbox coordinator.</summary>
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Gets the fixed lease duration used to fence concurrent local sandbox processes.</summary>
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(20);

    /// <summary>Gets the exact poll message type persisted by both sides.</summary>
    public const string PollMessageType = "command-poll-request.v2";

    /// <summary>Gets the exact acknowledgement message type persisted by both sides.</summary>
    public const string AcknowledgementMessageType = "command-acknowledgement-request.v2";

    /// <summary>Gets the required prefix for deliberately execution-ineligible test capabilities.</summary>
    public const string SyntheticCapabilityPrefix = "sandbox.command.";
}

/// <summary>Identifies the only two message kinds accepted by the sandbox transport stream.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CommandTransportMessageKind>))]
public enum CommandTransportMessageKind
{
    /// <summary>Requests a bounded set of execution-ineligible command fixtures.</summary>
    Poll,

    /// <summary>Reports only the durable receipt or safe terminal refusal of fixtures.</summary>
    Acknowledgement,
}

/// <summary>Declares that a transported fixture can never be offered to an executor.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CommandExecutionPolicy>))]
public enum CommandExecutionPolicy
{
    /// <summary>Prohibits execution regardless of the capability identifier or payload.</summary>
    Never,
}

/// <summary>Classifies durable transport receipt and safe terminal refusal without describing execution.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CommandTransportDisposition>))]
public enum CommandTransportDisposition
{
    /// <summary>The fixture was durably received and is eligible only for acknowledgement.</summary>
    Accepted,

    /// <summary>The exact acknowledgement was already committed.</summary>
    Duplicate,

    /// <summary>The message or fixture failed a closed validation rule.</summary>
    Rejected,

    /// <summary>The fixture expired without any execution attempt.</summary>
    Expired,

    /// <summary>The synthetic capability is deliberately unavailable and non-executable.</summary>
    Unsupported,
}

/// <summary>Represents one strictly bounded v2 poll request in the per-Agent monotonic stream.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportPollRequest(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    string AgentVersion,
    IReadOnlyDictionary<string, string> ProviderVersions,
    int MaximumCount);

/// <summary>Represents one execution-ineligible synthetic fixture delivered to the Agent inbox.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportEnvelope(
    Guid CommandId,
    string IdempotencyKey,
    Guid InstanceId,
    string ProviderId,
    string CapabilityId,
    string TypedParametersJson,
    DateTimeOffset RequestedAt,
    DateTimeOffset ExpiresAt,
    string ExpectedAgentVersion,
    string ExpectedProviderVersion,
    CommandExecutionPolicy ExecutionPolicy);

/// <summary>Represents one stable response correlated to a committed v2 poll request.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportPollResponse(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    Guid InReplyToMessageId,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    IReadOnlyList<CommandTransportEnvelope> Commands);

/// <summary>Reports only receipt or safe refusal of one synthetic fixture.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportAcknowledgement(
    Guid CommandId,
    string IdempotencyKey,
    DateTimeOffset AcknowledgedAt,
    CommandTransportDisposition Disposition,
    string? ReasonCode = null);

/// <summary>Represents one bounded v2 acknowledgement request in the same per-Agent stream as polling.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportAcknowledgementRequest(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    IReadOnlyList<CommandTransportAcknowledgement> Items);

/// <summary>Returns the durable server classification for one acknowledgement item.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportAcknowledgementResult(
    Guid CommandId,
    CommandTransportDisposition Disposition,
    string? ReasonCode = null);

/// <summary>Represents one stable response correlated to a committed v2 acknowledgement request.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportAcknowledgementResponse(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    Guid InReplyToMessageId,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    IReadOnlyList<CommandTransportAcknowledgementResult> Items);

/// <summary>Provides one bounded, sanitised protocol failure response without historical payload content.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CommandTransportProblem(
    int SchemaVersion,
    string Code,
    long? ExpectedSequence,
    bool Retryable);

/// <summary>Classifies a fail-closed server refusal so the endpoint can select a safe HTTP status.</summary>
public enum CommandTransportFailureKind
{
    /// <summary>The request failed structural, temporal or resource validation.</summary>
    Invalid,

    /// <summary>The request conflicts with durable identity or payload history.</summary>
    Conflict,

    /// <summary>The request skipped the next expected stream sequence.</summary>
    Gap,

    /// <summary>The request sequence precedes durable history and is not a known replay.</summary>
    Reordered,

    /// <summary>The authenticated Agent is absent, inactive or revoked.</summary>
    AgentInactive,

    /// <summary>The bounded sandbox transport is already processing another request.</summary>
    Busy,
}

/// <summary>Represents an expected, sanitised command-transport refusal at a trust boundary.</summary>
public sealed class CommandTransportException : Exception
{
    /// <summary>Initialises one fail-closed transport exception without embedding request content.</summary>
    /// <param name="kind">Stable failure classification.</param>
    /// <param name="code">Bounded machine-readable reason code.</param>
    /// <param name="expectedSequence">Next durable sequence when safe and relevant.</param>
    public CommandTransportException(
        CommandTransportFailureKind kind,
        string code,
        long? expectedSequence = null)
        : base(code)
    {
        Kind = kind;
        Code = code;
        ExpectedSequence = expectedSequence;
    }

    /// <summary>Gets the stable failure classification.</summary>
    public CommandTransportFailureKind Kind { get; }

    /// <summary>Gets the bounded machine-readable reason code.</summary>
    public string Code { get; }

    /// <summary>Gets the next durable sequence when disclosure is safe and relevant.</summary>
    public long? ExpectedSequence { get; }
}

/// <summary>Describes the exact pending message persisted by the Agent before network I/O.</summary>
public sealed record CommandTransportPendingMessage(
    Guid MessageId,
    long Sequence,
    CommandTransportMessageKind Kind,
    string PayloadJson,
    string PayloadSha256,
    int AttemptCount);

/// <summary>Reports the atomic result of accepting one poll response into the Agent-local store.</summary>
public sealed record CommandTransportPollCommit(
    int DeliveredCount,
    CommandTransportPendingMessage? PendingAcknowledgement);

/// <summary>Reports one bounded coordinator cycle without implying command execution.</summary>
public sealed record CommandTransportCycleResult(
    int DeliveredCount,
    int AcknowledgedCount,
    int AttemptCount,
    long FenceToken);

/// <summary>Defines durable server processing for the isolated v2 command-transport surface.</summary>
public interface ICommandTransportServerStore
{
    /// <summary>Processes or exactly replays one poll request as a single durable transaction.</summary>
    ValueTask<CommandTransportPollResponse> PollAsync(
        CommandTransportPollRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>Processes or exactly replays one acknowledgement request as a single durable transaction.</summary>
    ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
        CommandTransportAcknowledgementRequest request,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);
}

/// <summary>Defines durable Agent-local preparation, inbox and fencing operations for the sandbox protocol.</summary>
public interface ICommandTransportAgentStore
{
    /// <summary>Attempts to acquire one bounded process lease and returns its monotonic fence.</summary>
    ValueTask<long?> TryAcquireLeaseAsync(
        Guid agentId,
        string ownerId,
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken);

    /// <summary>Returns the pending message or atomically prepares the next poll before network I/O.</summary>
    ValueTask<CommandTransportPendingMessage> GetOrPreparePollAsync(
        Guid agentId,
        long fenceToken,
        string agentVersion,
        IReadOnlyDictionary<string, string> providerVersions,
        int maximumCount,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Records one bounded network attempt without changing pending payload identity.</summary>
    ValueTask RecordAttemptAsync(
        Guid agentId,
        long fenceToken,
        Guid messageId,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken);

    /// <summary>Atomically persists a validated poll response and prepares its acknowledgement when required.</summary>
    ValueTask<CommandTransportPollCommit> AcceptPollResponseAsync(
        Guid agentId,
        long fenceToken,
        CommandTransportPollResponse response,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>Atomically applies a validated acknowledgement response and clears the pending request.</summary>
    ValueTask<int> AcceptAcknowledgementResponseAsync(
        Guid agentId,
        long fenceToken,
        CommandTransportAcknowledgementResponse response,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>Releases the current lease only when owner and fence still match.</summary>
    ValueTask ReleaseLeaseAsync(
        Guid agentId,
        string ownerId,
        long fenceToken,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Defines the HTTPS client boundary used only by the explicitly composed sandbox coordinator.</summary>
public interface ICommandTransportSandboxClient
{
    /// <summary>Sends one already persisted poll request.</summary>
    ValueTask<CommandTransportPollResponse> PollAsync(
        CommandTransportPollRequest request,
        CancellationToken cancellationToken);

    /// <summary>Sends one already persisted acknowledgement request.</summary>
    ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
        CommandTransportAcknowledgementRequest request,
        CancellationToken cancellationToken);
}

/// <summary>Abstracts retry delay so deterministic tests need no wall-clock wait.</summary>
public interface ICommandTransportDelay
{
    /// <summary>Waits for one bounded retry delay while honouring cancellation.</summary>
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

/// <summary>Uses the runtime timer only when an authorised sandbox coordinator is explicitly composed.</summary>
public sealed class CommandTransportDelay : ICommandTransportDelay
{
    /// <inheritdoc />
    public async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
}

/// <summary>Defines the bounded retry schedule for one local sandbox operation.</summary>
public sealed record CommandTransportRetryPolicy(
    int MaximumRetries,
    TimeSpan FirstBackoff,
    TimeSpan SecondBackoff)
{
    /// <summary>Gets the exact authorised sandbox retry policy.</summary>
    public static CommandTransportRetryPolicy SandboxDefault { get; } =
        new(2, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(250));

    /// <summary>Validates retry count and delays before any work starts.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a value exceeds the sandbox policy.</exception>
    public void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MaximumRetries, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(MaximumRetries, 2);
        if (FirstBackoff < TimeSpan.Zero || FirstBackoff > TimeSpan.FromMilliseconds(100) ||
            SecondBackoff < FirstBackoff || SecondBackoff > TimeSpan.FromMilliseconds(250))
        {
            throw new ArgumentOutOfRangeException(nameof(FirstBackoff), "Retry delays exceed the sandbox policy.");
        }
    }

    /// <summary>Returns the configured delay before the specified retry.</summary>
    /// <param name="retryNumber">One-based retry number.</param>
    /// <returns>The deterministic bounded delay.</returns>
    public TimeSpan GetDelay(int retryNumber) => retryNumber switch
    {
        1 => FirstBackoff,
        2 => SecondBackoff,
        _ => throw new ArgumentOutOfRangeException(nameof(retryNumber)),
    };
}

/// <summary>Serialises the closed v2 contract set and computes exact SHA-256 evidence for durable replay.</summary>
public static class CommandTransportCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Serialises one supported contract into bounded canonical transport JSON.</summary>
    /// <typeparam name="T">One of the closed command-transport contract types.</typeparam>
    /// <param name="value">Validated contract value.</param>
    /// <returns>UTF-8 JSON represented as a string.</returns>
    public static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, Options);
    }

    /// <summary>Deserialises one supported contract and rejects unknown or missing material.</summary>
    /// <typeparam name="T">Expected closed command-transport contract type.</typeparam>
    /// <param name="json">Bounded persisted JSON.</param>
    /// <returns>The required typed contract.</returns>
    /// <exception cref="InvalidDataException">Thrown when the persisted payload cannot be decoded.</exception>
    public static T Deserialize<T>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<T>(json, Options) ??
            throw new InvalidDataException("command.transport_payload_empty");
    }

    /// <summary>Computes an uppercase SHA-256 digest over exact UTF-8 JSON bytes.</summary>
    /// <param name="json">Exact bounded JSON.</param>
    /// <returns>A 64-character uppercase digest.</returns>
    public static string ComputeSha256(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(json)));
    }
}

/// <summary>Coordinates one fenced, serial, retry-bounded poll/ack cycle without executing inbox content.</summary>
public sealed class CommandTransportSandboxCoordinator(
    Guid agentId,
    string ownerId,
    string agentVersion,
    IReadOnlyDictionary<string, string> providerVersions,
    ICommandTransportAgentStore store,
    ICommandTransportSandboxClient client,
    TimeProvider timeProvider,
    ICommandTransportDelay delay,
    CommandTransportRetryPolicy retryPolicy)
{
    /// <summary>Runs one bounded cycle and preserves every pending message across cancellation or transport failure.</summary>
    /// <param name="maximumCount">Requested batch count, no greater than the sandbox maximum.</param>
    /// <param name="cancellationToken">Caller cancellation signal.</param>
    /// <returns>Counts of transport-only receipt and acknowledgement plus the acquired fence.</returns>
    /// <exception cref="InvalidOperationException">Thrown when another process owns the local lease.</exception>
    public async ValueTask<CommandTransportCycleResult> RunOnceAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        Validate(maximumCount);
        retryPolicy.Validate();
        string stableOwner = ownerId;
        long? fence = null;
        try
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            fence = await store.TryAcquireLeaseAsync(
                agentId,
                stableOwner,
                now,
                CommandTransportProtocol.LeaseDuration,
                cancellationToken).ConfigureAwait(false);
            if (fence is null)
            {
                throw new InvalidOperationException("command.transport_lease_unavailable");
            }

            CommandTransportPendingMessage pending = await store.GetOrPreparePollAsync(
                agentId,
                fence.Value,
                agentVersion,
                providerVersions,
                maximumCount,
                now,
                cancellationToken).ConfigureAwait(false);
            int attempts = 0;
            int delivered = 0;
            int acknowledged = 0;
            if (pending.Kind == CommandTransportMessageKind.Poll)
            {
                CommandTransportPollResponse pollResponse = await SendWithRetryAsync<
                    CommandTransportPollRequest,
                    CommandTransportPollResponse>(
                    pending,
                    fence.Value,
                    (request, token) => client.PollAsync(request, token),
                    count => attempts += count,
                    cancellationToken).ConfigureAwait(false);
                CommandTransportPollCommit commit = await store.AcceptPollResponseAsync(
                    agentId,
                    fence.Value,
                    pollResponse,
                    timeProvider.GetUtcNow(),
                    cancellationToken).ConfigureAwait(false);
                delivered = commit.DeliveredCount;
                pending = commit.PendingAcknowledgement ?? pending;
                if (commit.PendingAcknowledgement is null)
                {
                    return new(delivered, 0, attempts, fence.Value);
                }
            }

            CommandTransportAcknowledgementResponse acknowledgementResponse = await SendWithRetryAsync<
                CommandTransportAcknowledgementRequest,
                CommandTransportAcknowledgementResponse>(
                pending,
                fence.Value,
                (request, token) => client.AcknowledgeAsync(request, token),
                count => attempts += count,
                cancellationToken).ConfigureAwait(false);
            acknowledged = await store.AcceptAcknowledgementResponseAsync(
                agentId,
                fence.Value,
                acknowledgementResponse,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);
            return new(delivered, acknowledged, attempts, fence.Value);
        }
        finally
        {
            if (fence is not null)
            {
                await store.ReleaseLeaseAsync(
                    agentId,
                    stableOwner,
                    fence.Value,
                    timeProvider.GetUtcNow(),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask<TResponse> SendWithRetryAsync<TRequest, TResponse>(
        CommandTransportPendingMessage pending,
        long fenceToken,
        Func<TRequest, CancellationToken, ValueTask<TResponse>> send,
        Action<int> recordAttempts,
        CancellationToken cancellationToken)
    {
        TRequest request = CommandTransportCodec.Deserialize<TRequest>(pending.PayloadJson);
        int attempts = 0;
        for (int attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempts++;
            await store.RecordAttemptAsync(
                agentId,
                fenceToken,
                pending.MessageId,
                timeProvider.GetUtcNow(),
                cancellationToken).ConfigureAwait(false);
            try
            {
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(CommandTransportProtocol.AttemptTimeout);
                TResponse response = await send(request, timeout.Token).ConfigureAwait(false);
                recordAttempts(attempts);
                return response;
            }
            catch (Exception exception) when (IsRetryable(exception, cancellationToken) && attempt < retryPolicy.MaximumRetries)
            {
                await delay.DelayAsync(retryPolicy.GetDelay(attempt + 1), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                recordAttempts(attempts);
                throw;
            }
        }
    }

    private static bool IsRetryable(Exception exception, CancellationToken callerToken) =>
        exception is IOException or HttpRequestException or TimeoutException ||
        exception is OperationCanceledException && !callerToken.IsCancellationRequested;

    private void Validate(int maximumCount)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumCount, CommandTransportProtocol.MaximumBatchSize);
        if (ownerId.Length > 160 || agentVersion.Length > 64 ||
            providerVersions.Count > CommandTransportProtocol.MaximumProviderVersions)
        {
            throw new ArgumentException("Command transport identity or provider evidence exceeds sandbox limits.");
        }
    }
}
