// Module purpose: Executes one explicitly selected command-transport safety cycle in a temporary child process with durable Agent SQLite and test-only mTLS identity.
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.AgentFleet.SandboxHost;

/// <summary>Owns strict admission, response-loss injection and cleanup for one child-process command cycle.</summary>
internal static class CommandTransportSandboxHost
{
    /// <summary>Runs one bounded cycle and returns a sanitised process result.</summary>
    /// <param name="args">Strict non-secret sandbox arguments.</param>
    /// <returns>Zero on completion or a bounded non-zero failure code.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        string stage = "argument_validation";
        try
        {
            Arguments options = Arguments.Parse(args);
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
                using HttpClient httpClient = SandboxHostProgram.CreatePinnedClient(
                    identity,
                    package.ServerThumbprint,
                    options.BaseAddress);
                ICommandTransportSandboxClient client = new HttpCommandTransportSandboxClient(
                    httpClient,
                    options.BaseAddress,
                    options.AgentVersion);
                if (options.Fault != "none")
                {
                    client = new LoseSuccessfulResponsesClient(client, options.Fault, 3);
                }

                stage = "local_store";
                string connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = options.DatabasePath,
                    Mode = SqliteOpenMode.ReadWrite,
                    Cache = SqliteCacheMode.Shared,
                    ForeignKeys = true,
                    Pooling = false,
                    DefaultTimeout = 1,
                }.ToString();
                DbContextOptions<AgentDbContext> databaseOptions =
                    new DbContextOptionsBuilder<AgentDbContext>().UseSqlite(connectionString).Options;
                ContextFactory factory = new(databaseOptions);
                await new AgentFleetSandboxDatabaseGuard(factory)
                    .ValidateAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                AgentCommandTransportSandboxStore store = new(factory);
                CommandTransportSandboxCoordinator coordinator = new(
                    options.AgentId,
                    options.OwnerId,
                    options.AgentVersion,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [options.ProviderId] = options.ProviderVersion,
                    },
                    store,
                    client,
                    new FixedTimeProvider(options.UtcNow),
                    new CommandTransportDelay(),
                    CommandTransportRetryPolicy.SandboxDefault);
                stage = "cycle";
                CommandTransportCycleResult result = await coordinator
                    .RunOnceAsync(options.MaximumCount, CancellationToken.None)
                    .ConfigureAwait(false);
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(new ChildResult(
                    true,
                    result.DeliveredCount,
                    result.AcknowledgedCount,
                    result.AttemptCount,
                    result.FenceToken))).ConfigureAwait(false);
                return 0;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(package.Pkcs12);
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            await Console.Error.WriteLineAsync(
                $"command_transport_host.failed:{stage}:{exception.GetType().Name}").ConfigureAwait(false);
            return 3;
        }
    }

    private sealed record ChildResult(
        bool Succeeded,
        int Delivered,
        int Acknowledged,
        int Attempts,
        long Fence);

    private sealed record Arguments(
        string DatabasePath,
        Uri BaseAddress,
        string IdentityPipe,
        string OwnerId,
        Guid AgentId,
        string AgentVersion,
        string ProviderId,
        string ProviderVersion,
        int MaximumCount,
        DateTimeOffset UtcNow,
        string Fault)
    {
        /// <summary>Parses the exact command-transport switch and bounded sandbox-only argument set.</summary>
        public static Arguments Parse(string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.Length != 25 || args[0] != "--sandbox-command-transport")
            {
                throw new ArgumentException("command_transport_host.arguments_invalid", nameof(args));
            }

            Dictionary<string, string> values = new(StringComparer.Ordinal);
            for (int index = 1; index < args.Length; index += 2)
            {
                if (!args[index].StartsWith("--", StringComparison.Ordinal) ||
                    !values.TryAdd(args[index], args[index + 1]))
                {
                    throw new ArgumentException("command_transport_host.arguments_invalid", nameof(args));
                }
            }

            string root = Path.GetFullPath(Require(values, "--sandbox-root"));
            string database = Path.GetFullPath(Require(values, "--database"));
            Uri baseAddress = new(Require(values, "--base-address"), UriKind.Absolute);
            string pipe = Require(values, "--identity-pipe");
            string owner = Require(values, "--owner");
            string agentText = Require(values, "--agent-id");
            string agentVersion = Require(values, "--agent-version");
            string providerId = Require(values, "--provider-id");
            string providerVersion = Require(values, "--provider-version");
            string countText = Require(values, "--maximum-count");
            string utcText = Require(values, "--utc-now");
            string fault = Require(values, "--fault");
            string relativeDatabase = Path.GetRelativePath(root, database);
            if (values.Count != 0 || !Directory.Exists(root) || !File.Exists(database) ||
                !Path.GetFileName(root).StartsWith("dbnotifier-command-transport-sandbox-", StringComparison.Ordinal) ||
                Path.IsPathRooted(relativeDatabase) || relativeDatabase == ".." ||
                relativeDatabase.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                baseAddress.Scheme != Uri.UriSchemeHttps || !baseAddress.IsLoopback ||
                !Guid.TryParse(agentText, out Guid agentId) || agentId == Guid.Empty ||
                !int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out int maximumCount) ||
                maximumCount is < 1 or > CommandTransportProtocol.MaximumBatchSize ||
                !DateTimeOffset.TryParseExact(
                    utcText,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTimeOffset utcNow) || utcNow.Offset != TimeSpan.Zero ||
                fault is not ("none" or "lose-poll" or "lose-ack") ||
                !Stable(pipe, 8, 160) || !Stable(owner, 8, 160) ||
                !Stable(agentVersion, 1, 64) || !Stable(providerId, 1, 64) ||
                !Stable(providerVersion, 1, 64))
            {
                throw new ArgumentException("command_transport_host.boundary_invalid", nameof(args));
            }

            return new(
                database,
                baseAddress,
                pipe,
                owner,
                agentId,
                agentVersion,
                providerId,
                providerVersion,
                maximumCount,
                utcNow,
                fault);
        }

        private static string Require(Dictionary<string, string> values, string name)
        {
            if (!values.Remove(name, out string? value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("command_transport_host.arguments_invalid", nameof(values));
            }

            return value;
        }

        private static bool Stable(string value, int minimum, int maximum) =>
            value.Length >= minimum && value.Length <= maximum &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
    }

    private sealed class LoseSuccessfulResponsesClient(
        ICommandTransportSandboxClient inner,
        string fault,
        int losses) : ICommandTransportSandboxClient
    {
        private int remaining = losses;

        /// <inheritdoc />
        public async ValueTask<CommandTransportPollResponse> PollAsync(
            CommandTransportPollRequest request,
            CancellationToken cancellationToken)
        {
            CommandTransportPollResponse response = await inner.PollAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (fault == "lose-poll" && Interlocked.Decrement(ref remaining) >= 0)
            {
                throw new IOException("command.transport_response_lost");
            }

            return response;
        }

        /// <inheritdoc />
        public async ValueTask<CommandTransportAcknowledgementResponse> AcknowledgeAsync(
            CommandTransportAcknowledgementRequest request,
            CancellationToken cancellationToken)
        {
            CommandTransportAcknowledgementResponse response = await inner
                .AcknowledgeAsync(request, cancellationToken).ConfigureAwait(false);
            if (fault == "lose-ack" && Interlocked.Decrement(ref remaining) >= 0)
            {
                throw new IOException("command.transport_response_lost");
            }

            return response;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ContextFactory(DbContextOptions<AgentDbContext> options)
        : IDbContextFactory<AgentDbContext>
    {
        /// <inheritdoc />
        public AgentDbContext CreateDbContext() => new(options);
    }
}
