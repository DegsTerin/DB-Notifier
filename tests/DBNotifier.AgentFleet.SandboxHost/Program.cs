// Module purpose: Executes one loopback-only, test-material-only Agent Fleet heartbeat in a temporary child process and exposes no operational composition.
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Infrastructure.AgentFleet;
using DBNotifier.Persistence.Agent.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

return await DBNotifier.AgentFleet.SandboxHost.SandboxHostProgram.RunAsync(args).ConfigureAwait(false);

namespace DBNotifier.AgentFleet.SandboxHost
{
    /// <summary>Provides a stable assembly marker so the integration test can locate this temporary harness.</summary>
    public static class SandboxHostMarker;

    /// <summary>Owns strict argument admission, private IPC loading, one fenced heartbeat and deterministic cleanup.</summary>
    internal static class SandboxHostProgram
    {
        private const int MaximumPasswordBytes = 128;
        private const int MaximumPkcs12Bytes = 32 * 1024;
        private const int MaximumThumbprintBytes = 160;

        /// <summary>Runs one explicitly marked sandbox heartbeat and returns a bounded process exit code.</summary>
        /// <param name="args">Strict non-secret harness arguments.</param>
        /// <returns>Zero for safe completion or a non-zero bounded failure code.</returns>
        public static async Task<int> RunAsync(string[] args)
        {
            string stage = "argument_validation";
            try
            {
                SandboxArguments options = SandboxArguments.Parse(args);
                stage = "identity_pipe";
                IdentityPackage package = await ReadIdentityPackageAsync(options.IdentityPipe).ConfigureAwait(false);
                try
                {
                    stage = "identity_loading";
                    using X509Certificate2 identity = X509CertificateLoader.LoadPkcs12(
                        package.Pkcs12,
                        package.Password,
                        X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
                    stage = "client_creation";
                    using HttpClient client = CreatePinnedClient(identity, package.ServerThumbprint, options.BaseAddress);
                    stage = "local_store_creation";
                    AgentFleetLocalStore store = await CreateStoreAsync(options.DatabasePath).ConfigureAwait(false);
                    FixedTimeProvider clock = new(options.UtcNow);
                    IAgentFleetClientTransport transport = new HttpAgentFleetClientTransport(
                        options.BaseAddress,
                        options.AgentVersion,
                        identityReference => string.Equals(
                            identityReference,
                            options.IdentityReference,
                            StringComparison.Ordinal)
                                ? client
                                : throw new InvalidOperationException("sandbox_host.identity_reference_invalid"));
                    if (options.PauseAfterResponseMarker is not null)
                    {
                        transport = new PauseAfterAcceptedHeartbeatTransport(
                            transport,
                            options.PauseAfterResponseMarker);
                    }

                    AgentFleetClientCoordinator oneShot = new(
                        new UnavailableEnrollmentIdentityStore(),
                        transport,
                        store,
                        clock,
                        TimeSpan.FromMinutes(5));
                    AgentFleetSandboxResilienceCoordinator resilience = new(
                        oneShot,
                        store,
                        clock,
                        new RejectingDelay(),
                        new FixedJitter(),
                        new AgentFleetSandboxRetryPolicy(
                            1,
                            TimeSpan.Zero,
                            TimeSpan.Zero,
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(5),
                            0));
                    stage = "heartbeat_execution";
                    AgentFleetSandboxResilienceResult result = await resilience.SendHeartbeatAsync(
                        options.OwnerId,
                        options.AgentVersion,
                        CancellationToken.None).ConfigureAwait(false);
                    string output = JsonSerializer.Serialize(new SandboxResult(
                        result.Result.Succeeded,
                        result.Result.State.ToString(),
                        result.Result.Code,
                        result.Attempts,
                        result.FenceToken));
                    await Console.Out.WriteLineAsync(output).ConfigureAwait(false);
                    return result.Result.Succeeded ? 0 : 3;
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
                        $"sandbox_host.failed:{stage}:{exception.GetType().Name}:{file}")
                    .ConfigureAwait(false);
                return 2;
            }
        }

        private static async Task<AgentFleetLocalStore> CreateStoreAsync(string databasePath)
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Shared,
                ForeignKeys = true,
                DefaultTimeout = 1,
            }.ToString();
            DbContextOptions<AgentDbContext> options = new DbContextOptionsBuilder<AgentDbContext>()
                .UseSqlite(connectionString)
                .Options;
            ContextFactory factory = new(options);
            await new AgentFleetSandboxDatabaseGuard(factory)
                .ValidateAsync(CancellationToken.None)
                .ConfigureAwait(false);
            return new AgentFleetLocalStore(factory);
        }

        private static HttpClient CreatePinnedClient(
            X509Certificate2 identity,
            string serverThumbprint,
            Uri baseAddress)
        {
            HttpClientHandler handler = new()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                    certificate is not null && string.Equals(
                        NormaliseThumbprint(certificate.Thumbprint),
                        serverThumbprint,
                        StringComparison.Ordinal),
            };
            handler.ClientCertificates.Add(identity);
            return new HttpClient(handler, disposeHandler: true) { BaseAddress = baseAddress };
        }

        private static async Task<IdentityPackage> ReadIdentityPackageAsync(string pipeName)
        {
            using NamedPipeClientStream pipe = new(
                ".",
                pipeName,
                PipeDirection.In,
                PipeOptions.Asynchronous);
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
            await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
            byte[] passwordBytes = await ReadFrameAsync(pipe, MaximumPasswordBytes, timeout.Token).ConfigureAwait(false);
            byte[] pkcs12 = await ReadFrameAsync(pipe, MaximumPkcs12Bytes, timeout.Token).ConfigureAwait(false);
            byte[] thumbprintBytes = await ReadFrameAsync(pipe, MaximumThumbprintBytes, timeout.Token)
                .ConfigureAwait(false);
            try
            {
                string password = Encoding.UTF8.GetString(passwordBytes);
                string thumbprint = NormaliseThumbprint(Encoding.ASCII.GetString(thumbprintBytes));
                if (password.Length is < 16 or > 128 || thumbprint.Length is < 40 or > 128 ||
                    thumbprint.Any(character => !char.IsAsciiHexDigit(character)))
                {
                    throw new InvalidDataException("sandbox_host.identity_package_invalid");
                }

                return new IdentityPackage(pkcs12, password, thumbprint);
            }
            catch
            {
                CryptographicOperations.ZeroMemory(pkcs12);
                throw;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
                CryptographicOperations.ZeroMemory(thumbprintBytes);
            }
        }

        private static async Task<byte[]> ReadFrameAsync(
            Stream stream,
            int maximumLength,
            CancellationToken cancellationToken)
        {
            byte[] lengthBytes = new byte[sizeof(int)];
            await stream.ReadExactlyAsync(lengthBytes, cancellationToken).ConfigureAwait(false);
            int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes));
            if (length is < 1 || length > maximumLength)
            {
                throw new InvalidDataException("sandbox_host.identity_frame_invalid");
            }

            byte[] payload = new byte[length];
            await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
            return payload;
        }

        private static string NormaliseThumbprint(string value) => value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        private sealed record IdentityPackage(byte[] Pkcs12, string Password, string ServerThumbprint);

        private sealed record SandboxResult(
            bool Succeeded,
            string State,
            string Code,
            int Attempts,
            long? FenceToken);

        private sealed record SandboxArguments(
            string DatabasePath,
            Uri BaseAddress,
            string IdentityReference,
            string IdentityPipe,
            string OwnerId,
            string AgentVersion,
            DateTimeOffset UtcNow,
            string? PauseAfterResponseMarker)
        {
            /// <summary>Parses only the exact sandbox switch and bounded non-secret option set.</summary>
            /// <param name="args">Process arguments.</param>
            /// <returns>Validated sandbox options.</returns>
            public static SandboxArguments Parse(string[] args)
            {
                ArgumentNullException.ThrowIfNull(args);
                if (args.Length < 15 || args[0] != "--sandbox-agent-fleet" || args.Length % 2 == 0)
                {
                    throw new ArgumentException("sandbox_host.arguments_invalid", nameof(args));
                }

                Dictionary<string, string> values = new(StringComparer.Ordinal);
                for (int index = 1; index < args.Length; index += 2)
                {
                    if (!args[index].StartsWith("--", StringComparison.Ordinal) ||
                        !values.TryAdd(args[index], args[index + 1]))
                    {
                        throw new ArgumentException("sandbox_host.arguments_invalid", nameof(args));
                    }
                }

                string root = Require(values, "--sandbox-root");
                string database = Require(values, "--database");
                string marker = values.Remove("--pause-after-response-marker", out string? suppliedMarker)
                    ? suppliedMarker
                    : string.Empty;
                Uri baseAddress = new(Require(values, "--base-address"), UriKind.Absolute);
                string identityReference = Require(values, "--identity-reference");
                string identityPipe = Require(values, "--identity-pipe");
                string ownerId = Require(values, "--owner");
                string agentVersion = Require(values, "--agent-version");
                string utcText = Require(values, "--utc-now");
                if (values.Count != 0 || !DateTimeOffset.TryParseExact(
                        utcText,
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal,
                        out DateTimeOffset utcNow) || utcNow.Offset != TimeSpan.Zero)
                {
                    throw new ArgumentException("sandbox_host.arguments_invalid", nameof(args));
                }

                string fullRoot = Path.GetFullPath(root);
                string fullDatabase = ValidateOwnedPath(fullRoot, database, mustExist: true);
                string? fullMarker = string.IsNullOrEmpty(marker)
                    ? null
                    : ValidateOwnedPath(fullRoot, marker, mustExist: false);
                if (!Directory.Exists(fullRoot) ||
                    !Path.GetFileName(fullRoot).StartsWith("dbnotifier-agent-fleet-sandbox-", StringComparison.Ordinal) ||
                    baseAddress.Scheme != Uri.UriSchemeHttps || !baseAddress.IsLoopback ||
                    !IsStable(identityReference, 16, 160) || !identityReference.StartsWith("sandbox-identity:", StringComparison.Ordinal) ||
                    !IsStable(identityPipe, 8, 160) || !IsStable(ownerId, 8, 160) || !IsStable(agentVersion, 1, 64))
                {
                    throw new ArgumentException("sandbox_host.boundary_invalid", nameof(args));
                }

                return new SandboxArguments(
                    fullDatabase,
                    baseAddress,
                    identityReference,
                    identityPipe,
                    ownerId,
                    agentVersion,
                    utcNow,
                    fullMarker);
            }

            private static string Require(Dictionary<string, string> values, string name)
            {
                if (!values.Remove(name, out string? value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("sandbox_host.arguments_invalid", nameof(values));
                }

                return value;
            }

            private static string ValidateOwnedPath(string root, string value, bool mustExist)
            {
                string fullPath = Path.GetFullPath(value);
                string relative = Path.GetRelativePath(root, fullPath);
                if (Path.IsPathRooted(relative) || relative == ".." ||
                    relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                    (mustExist && !File.Exists(fullPath)))
                {
                    throw new ArgumentException("sandbox_host.path_invalid", nameof(value));
                }

                return fullPath;
            }

            private static bool IsStable(string value, int minimum, int maximum) =>
                value.Length >= minimum && value.Length <= maximum &&
                value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
        }

        private sealed class PauseAfterAcceptedHeartbeatTransport(
            IAgentFleetClientTransport inner,
            string markerPath) : IAgentFleetClientTransport
        {
            /// <inheritdoc />
            public ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
                string token,
                AgentEnrollmentRequest request,
                CancellationToken cancellationToken) => inner.EnrolAsync(token, request, cancellationToken);

            /// <inheritdoc />
            public async ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
                string identityReference,
                AgentHeartbeatRequest request,
                CancellationToken cancellationToken)
            {
                AgentFleetTransportResult<AgentHeartbeatOutcome> result = await inner.SendHeartbeatAsync(
                    identityReference,
                    request,
                    cancellationToken).ConfigureAwait(false);
                if (result.Disposition == AgentFleetTransportDisposition.Succeeded)
                {
                    await File.WriteAllTextAsync(markerPath, "accepted", cancellationToken).ConfigureAwait(false);
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                }

                return result;
            }

            /// <inheritdoc />
            public ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
                string identityReference,
                Guid agentId,
                string agentVersion,
                string? currentVersion,
                CancellationToken cancellationToken) => inner.GetAssignmentsAsync(
                    identityReference,
                    agentId,
                    agentVersion,
                    currentVersion,
                    cancellationToken);
        }

        private sealed class UnavailableEnrollmentIdentityStore : IAgentEnrollmentIdentityStore
        {
            /// <inheritdoc />
            public ValueTask<AgentEnrollmentKeyMaterial> BeginAsync(
                string installationId,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            /// <inheritdoc />
            public ValueTask<AgentIdentityCompletion> CompleteAsync(
                Guid operationId,
                ReadOnlyMemory<byte> certificateDer,
                CancellationToken cancellationToken) => throw new NotSupportedException();

            /// <inheritdoc />
            public ValueTask AbortAsync(Guid operationId, CancellationToken cancellationToken) =>
                ValueTask.CompletedTask;

            /// <inheritdoc />
            public ValueTask RemoveAsync(string identityReference, CancellationToken cancellationToken) =>
                ValueTask.CompletedTask;
        }

        private sealed class RejectingDelay : IAgentFleetSandboxDelay
        {
            /// <inheritdoc />
            public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
                throw new InvalidOperationException("sandbox_host.retry_not_authorised");
        }

        private sealed class FixedJitter : IAgentFleetSandboxJitter
        {
            /// <inheritdoc />
            public double NextUnitInterval() => 0.5;
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

            /// <inheritdoc />
            public Task<AgentDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new AgentDbContext(options));
        }
    }
}
