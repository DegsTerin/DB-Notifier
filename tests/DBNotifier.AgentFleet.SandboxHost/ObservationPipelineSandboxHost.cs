// Module purpose: Runs one synthetic provider-neutral observation cycle and outbox dispatch using only fixture-owned SQLite, IPC and HTTPS loopback.
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Security;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using DBNotifier.Infrastructure.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Provider.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DBNotifier.AgentFleet.SandboxHost;

/// <summary>
/// Composes the real Agent monitoring, SQLite outbox and HTTPS transport boundaries around a deterministic
/// synthetic provider. It is reachable only through the exact test-harness switch and performs no external access.
/// </summary>
internal static class ObservationPipelineSandboxHost
{
    /// <summary>Runs one bounded synthetic monitoring/dispatch operation and emits only aggregate non-secret evidence.</summary>
    /// <param name="args">Strict non-secret harness arguments.</param>
    /// <returns>Zero after a safely completed attempt, including expected retryable or rejected transport outcomes.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        string stage = "argument_validation";
        try
        {
            ObservationArguments options = ObservationArguments.Parse(args);
            stage = "identity_pipe";
            SandboxHostProgram.IdentityPackage package = await SandboxHostProgram
                .ReadIdentityPackageAsync(options.IdentityPipe)
                .ConfigureAwait(false);
            try
            {
                stage = "identity_loading";
                using X509Certificate2 identity = X509CertificateLoader.LoadPkcs12(
                    package.Pkcs12,
                    package.Password,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                stage = "client_creation";
                using HttpClient client = SandboxHostProgram.CreatePinnedClient(
                    identity,
                    package.ServerThumbprint,
                    options.BaseAddress);
                stage = "local_store_validation";
                ContextFactory factory = await CreateFactoryAsync(options.DatabasePath).ConfigureAwait(false);
                await ValidateRegistrationAndAssignmentAsync(factory, options).ConfigureAwait(false);
                MutableTimeProvider clock = new(options.UtcNow);

                int persisted = 0;
                if (options.Statuses.Count > 0)
                {
                    stage = "synthetic_monitoring";
                    SyntheticObservationProvider provider = new(options.Statuses);
                    ProbeInstanceHandler handler = new(
                        new ProviderRegistry([provider]),
                        new UnavailableCredentialVault(),
                        clock);
                    AgentMonitoringAssignmentSource assignments = new(
                        factory,
                        NullLogger<AgentMonitoringAssignmentSource>.Instance,
                        maximumAssignments: 10);
                    using AgentObservationOutboxSink sink = new(factory, clock);
                    MonitoringCycleRunner runner = new(
                        options.AgentId,
                        assignments,
                        handler,
                        sink,
                        clock,
                        maximumConcurrency: 1,
                        cycleDeadline: TimeSpan.FromSeconds(10));

                    for (int index = 0; index < options.Statuses.Count; index++)
                    {
                        MonitoringCycleResult cycle = await runner.RunOnceAsync().ConfigureAwait(false);
                        if (cycle.DueCount != 1 || cycle.PersistedCount != 1 || cycle.Failures.Count != 0)
                        {
                            throw new InvalidOperationException("sandbox_observation.monitoring_result_invalid");
                        }

                        persisted = checked(persisted + cycle.PersistedCount);
                        if (index + 1 < options.Statuses.Count)
                        {
                            clock.Advance(TimeSpan.FromSeconds(31));
                        }
                    }
                }

                stage = "outbox_dispatch";
                IObservationBatchTransport transport = new HttpObservationBatchTransport(
                    client,
                    options.BaseAddress,
                    options.AgentVersion);
                transport = options.DispatchMode switch
                {
                    ObservationDispatchMode.Normal => transport,
                    ObservationDispatchMode.DropAcceptedResponse => new DropAcceptedResponseTransport(transport),
                    ObservationDispatchMode.ReverseBatch => new ReverseBatchTransport(transport),
                    _ => throw new InvalidOperationException("sandbox_observation.dispatch_mode_invalid"),
                };
                AgentOutboxDispatchRunner dispatcher = new(
                    options.AgentId,
                    new AgentOutboxStore(factory),
                    transport,
                    clock);
                AgentOutboxDispatchResult dispatch = await dispatcher
                    .RunOnceAsync(100)
                    .ConfigureAwait(false);
                string output = JsonSerializer.Serialize(new ObservationSandboxResult(
                    persisted,
                    dispatch.PendingCount,
                    dispatch.AcknowledgedCount,
                    dispatch.RetryableCount));
                await Console.Out.WriteLineAsync(output).ConfigureAwait(false);
                return 0;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(package.Pkcs12);
            }
        }
        catch (Exception exception) when (exception is not StackOverflowException and not OutOfMemoryException)
        {
            string file = exception is FileNotFoundException missingFile
                ? Path.GetFileName(missingFile.FileName) ?? string.Empty
                : string.Empty;
            await Console.Error.WriteLineAsync(
                    $"sandbox_observation.failed:{stage}:{exception.GetType().Name}:{file}")
                .ConfigureAwait(false);
            return 2;
        }
    }

    /// <summary>Creates a validated factory over the already migrated fixture-owned Agent SQLite file.</summary>
    private static async Task<ContextFactory> CreateFactoryAsync(string databasePath)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            DefaultTimeout = 1,
        }.ToString();
        DbContextOptions<AgentDbContext> contextOptions = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(connectionString)
            .Options;
        ContextFactory factory = new(contextOptions);
        await new AgentFleetSandboxDatabaseGuard(factory)
            .ValidateAsync(CancellationToken.None)
            .ConfigureAwait(false);
        return factory;
    }

    /// <summary>Proves that process arguments match the durable active registration and sole enabled assignment.</summary>
    private static async Task ValidateRegistrationAndAssignmentAsync(
        ContextFactory factory,
        ObservationArguments options)
    {
        await using AgentDbContext context = await factory.CreateDbContextAsync().ConfigureAwait(false);
        AgentRegistrationRow registration = await context.Registrations
            .AsNoTracking()
            .SingleAsync()
            .ConfigureAwait(false);
        AgentInstanceAssignmentRow assignment = await context.InstanceAssignments
            .AsNoTracking()
            .SingleAsync(row => row.Enabled)
            .ConfigureAwait(false);
        if (registration.AgentId != options.AgentId ||
            !string.Equals(registration.IdentityCertificateReference, options.IdentityReference, StringComparison.Ordinal) ||
            !string.Equals(registration.IdentityState, AgentLocalIdentityState.Active.ToString(), StringComparison.Ordinal) ||
            assignment.InstanceId != options.InstanceId ||
            !string.Equals(assignment.ProviderType, SyntheticObservationProvider.ProviderIdentifier, StringComparison.Ordinal) ||
            assignment.MonitoringCredentialReference is not null)
        {
            throw new InvalidOperationException("sandbox_observation.local_binding_invalid");
        }
    }

    /// <summary>Contains only aggregate, non-secret child-process evidence written to standard output.</summary>
    private sealed record ObservationSandboxResult(
        int PersistedCount,
        int PendingCount,
        int AcknowledgedCount,
        int RetryableCount);

    /// <summary>Contains the fully validated, non-secret boundaries for one synthetic observation operation.</summary>
    private sealed record ObservationArguments(
        string DatabasePath,
        Uri BaseAddress,
        string IdentityReference,
        string IdentityPipe,
        string AgentVersion,
        DateTimeOffset UtcNow,
        Guid AgentId,
        Guid InstanceId,
        IReadOnlyList<HealthStatus> Statuses,
        ObservationDispatchMode DispatchMode)
    {
        /// <summary>Parses only the exact observation sandbox switch and bounded non-secret options.</summary>
        public static ObservationArguments Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.Length < 25 || args[0] != "--sandbox-observation-pipeline" || args.Length % 2 == 0)
            {
                throw new ArgumentException("sandbox_observation.arguments_invalid", nameof(args));
            }

            Dictionary<string, string> values = new(StringComparer.Ordinal);
            for (int index = 1; index < args.Length; index += 2)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal) ||
                    !values.TryAdd(args[index], args[index + 1]))
                {
                    throw new ArgumentException("sandbox_observation.arguments_invalid", nameof(args));
                }
            }

            string fullRoot = Path.GetFullPath(Require(values, "--sandbox-root"));
            string fullDatabase = ValidateOwnedPath(fullRoot, Require(values, "--database"));
            Uri baseAddress = new(Require(values, "--base-address"), UriKind.Absolute);
            string identityReference = Require(values, "--identity-reference");
            string identityPipe = Require(values, "--identity-pipe");
            string agentVersion = Require(values, "--agent-version");
            string utcText = Require(values, "--utc-now");
            string agentText = Require(values, "--agent-id");
            string instanceText = Require(values, "--instance-id");
            string providerType = Require(values, "--provider-type");
            IReadOnlyList<HealthStatus> statuses = ParseStatuses(Require(values, "--statuses"));
            ObservationDispatchMode dispatchMode = Require(values, "--dispatch-mode") switch
            {
                "normal" => ObservationDispatchMode.Normal,
                "drop-accepted-response" => ObservationDispatchMode.DropAcceptedResponse,
                "reverse-batch" => ObservationDispatchMode.ReverseBatch,
                _ => throw new ArgumentException("sandbox_observation.dispatch_mode_invalid", nameof(args)),
            };

            if (values.Count != 0 ||
                !DateTimeOffset.TryParseExact(
                    utcText,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTimeOffset utcNow) || utcNow.Offset != TimeSpan.Zero ||
                !Guid.TryParseExact(agentText, "D", out Guid agentId) || agentId == Guid.Empty ||
                !Guid.TryParseExact(instanceText, "D", out Guid instanceId) || instanceId == Guid.Empty ||
                !Directory.Exists(fullRoot) ||
                !Path.GetFileName(fullRoot).StartsWith("dbnotifier-agent-fleet-sandbox-", StringComparison.Ordinal) ||
                baseAddress.Scheme != Uri.UriSchemeHttps ||
                !IPAddress.TryParse(baseAddress.Host, out IPAddress? loopbackAddress) ||
                !IPAddress.IsLoopback(loopbackAddress) ||
                providerType != SyntheticObservationProvider.ProviderIdentifier ||
                !IsStable(identityReference, 16, 160) ||
                !identityReference.StartsWith("sandbox-identity:", StringComparison.Ordinal) ||
                !IsStable(identityPipe, 8, 160) ||
                !IsStable(agentVersion, 1, 64))
            {
                throw new ArgumentException("sandbox_observation.boundary_invalid", nameof(args));
            }

            return new ObservationArguments(
                fullDatabase,
                baseAddress,
                identityReference,
                identityPipe,
                agentVersion,
                utcNow,
                agentId,
                instanceId,
                statuses,
                dispatchMode);
        }

        /// <summary>Parses a bounded list of non-healthy synthetic states, or the exact empty marker.</summary>
        private static List<HealthStatus> ParseStatuses(string value)
        {
            if (value == "none")
            {
                return [];
            }

            string[] tokens = value.Split(',', StringSplitOptions.None);
            if (tokens.Length is < 1 or > 10)
            {
                throw new ArgumentException("sandbox_observation.statuses_invalid", nameof(value));
            }

            List<HealthStatus> statuses = new(tokens.Length);
            foreach (string token in tokens)
            {
                HealthStatus status = token switch
                {
                    nameof(HealthStatus.Degraded) => HealthStatus.Degraded,
                    nameof(HealthStatus.Unavailable) => HealthStatus.Unavailable,
                    nameof(HealthStatus.Timeout) => HealthStatus.Timeout,
                    nameof(HealthStatus.Unknown) => HealthStatus.Unknown,
                    _ => throw new ArgumentException("sandbox_observation.statuses_invalid", nameof(value)),
                };
                statuses.Add(status);
            }

            return statuses;
        }

        /// <summary>Consumes one required non-blank option or rejects the complete argument set.</summary>
        private static string Require(Dictionary<string, string> values, string name)
        {
            if (!values.Remove(name, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("sandbox_observation.arguments_invalid", nameof(values));
            }

            return value;
        }

        /// <summary>Returns one existing file path only when it remains beneath the fixture-owned root.</summary>
        private static string ValidateOwnedPath(string root, string value)
        {
            string fullPath = Path.GetFullPath(value);
            string relative = Path.GetRelativePath(root, fullPath);
            if (Path.IsPathRooted(relative) || relative == ".." ||
                relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                !File.Exists(fullPath))
            {
                throw new ArgumentException("sandbox_observation.path_invalid", nameof(value));
            }

            return fullPath;
        }

        /// <summary>Accepts one bounded transport-safe identifier without whitespace or shell punctuation.</summary>
        private static bool IsStable(string value, int minimum, int maximum) =>
            value.Length >= minimum && value.Length <= maximum &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
    }

    /// <summary>Enumerates the three deterministic transport behaviours exposed by the exact sandbox switch.</summary>
    private enum ObservationDispatchMode
    {
        Normal,
        DropAcceptedResponse,
        ReverseBatch,
    }

    /// <summary>Returns deterministic synthetic evidence and never opens a socket, file, process or credential store.</summary>
    private sealed class SyntheticObservationProvider(IReadOnlyList<HealthStatus> statuses) : IDatabaseProvider
    {
        public const string ProviderIdentifier = "fixture-provider";
        private int nextStatus;

        /// <inheritdoc />
        public ProviderType ProviderType { get; } = ProviderType.Parse(ProviderIdentifier);

        /// <inheritdoc />
        public string Version => "sandbox-synthetic-v1";

        /// <inheritdoc />
        public IReadOnlyList<ProviderCapability> Capabilities => [];

        /// <inheritdoc />
        public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint) =>
            endpoint.ProviderType == ProviderType && endpoint.Properties.Count == 2 &&
            endpoint.TryGetValue("host", out string host) && host == "sandbox.invalid" &&
            endpoint.TryGetValue("port", out string port) && port == "5432"
                ? ProviderValidationResult.Valid
                : ProviderValidationResult.Invalid(new ProviderValidationError(
                    "sandbox.endpoint_invalid",
                    "endpoint",
                    "The synthetic sandbox endpoint is not the exact fixture."));

        /// <inheritdoc />
        public ValueTask<ProviderProbeResult> ProbeAsync(
            ProviderProbeRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.MonitoringCredential is not null || nextStatus >= statuses.Count)
            {
                throw new InvalidOperationException("sandbox_observation.provider_invocation_invalid");
            }

            HealthStatus status = statuses[nextStatus++];
            return ValueTask.FromResult(new ProviderProbeResult(
                status,
                EvidenceLevel.Synthetic,
                "sandbox-synthetic",
                TimeSpan.FromMilliseconds(12 + nextStatus),
                null,
                ["synthetic-no-external-data"]));
        }
    }

    /// <summary>Fails every credential lookup so the synthetic provider path cannot acquire secret material.</summary>
    private sealed class UnavailableCredentialVault : ICredentialVault
    {
        /// <inheritdoc />
        public ValueTask<IProviderCredential> ResolveAsync(
            CredentialReference reference,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sandbox_observation.credential_access_prohibited");
    }

    /// <summary>Discards one successful server response and returns retryable local outcomes to model response loss.</summary>
    private sealed class DropAcceptedResponseTransport(IObservationBatchTransport inner) : IObservationBatchTransport
    {
        /// <inheritdoc />
        public async ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken)
        {
            ObservationBatchResult received = await inner
                .SendAsync(agentId, messages, cancellationToken)
                .ConfigureAwait(false);
            bool accepted = received.Items.Count == messages.Count && received.Items.All(item =>
                item.Disposition is ObservationIngestionDisposition.Accepted or ObservationIngestionDisposition.Duplicate);
            if (!accepted)
            {
                return received;
            }

            return new ObservationBatchResult(
                messages.Select(message => new ObservationItemResult(
                    message.MessageId,
                    ObservationIngestionDisposition.Retryable,
                    "sync.test_response_lost")).ToArray(),
                received.HighestContiguousSequence);
        }
    }

    /// <summary>Reverses one local batch before the real HTTPS transport to prove server-side gap reconciliation.</summary>
    private sealed class ReverseBatchTransport(IObservationBatchTransport inner) : IObservationBatchTransport
    {
        /// <inheritdoc />
        public ValueTask<ObservationBatchResult> SendAsync(
            Guid agentId,
            IReadOnlyList<AgentOutboxEnvelope> messages,
            CancellationToken cancellationToken) =>
            inner.SendAsync(agentId, messages.Reverse().ToArray(), cancellationToken);
    }

    /// <summary>Provides a deterministic per-process clock advanced only by the bounded synthetic cycle.</summary>
    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;

        /// <summary>Advances the deterministic child-process clock by one positive bounded duration.</summary>
        public void Advance(TimeSpan duration)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(duration, TimeSpan.FromMinutes(5));
            now = now.Add(duration);
        }
    }

    /// <summary>Creates short-lived Agent contexts over the single fixture-owned SQLite database.</summary>
    private sealed class ContextFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        /// <inheritdoc />
        public AgentDbContext CreateDbContext() => new(options);

        /// <inheritdoc />
        public Task<AgentDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentDbContext(options));
    }
}
