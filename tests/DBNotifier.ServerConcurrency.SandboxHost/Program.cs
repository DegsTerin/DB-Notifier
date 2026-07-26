// Module purpose: Executes one explicitly selected central PostgreSQL identity-fence operation in an isolated test process.
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;
using DBNotifier.Application.Synchronization;
using DBNotifier.Persistence.Server.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

return await DBNotifier.ServerConcurrency.SandboxHost.SandboxHostProgram
    .RunAsync(args)
    .ConfigureAwait(false);

namespace DBNotifier.ServerConcurrency.SandboxHost
{
    /// <summary>Provides a stable marker for locating the built test-only child-process assembly.</summary>
    public static class SandboxHostMarker;

    /// <summary>
    /// Admits one strict local-test operation, receives its ephemeral connection over private IPC and returns only
    /// bounded sanitised evidence. It never starts a listener or composes a product runtime.
    /// </summary>
    internal static class SandboxHostProgram
    {
        private const string ActivationMarker = "--sandbox-r-egress-fence";
        private const string DisposableDatabaseName = "dbnotifier_r_egress_fence";
        private const string DisposableDatabaseUser = "dbnotifier_r_egress_fence";
        private const string SubjectId = "r-egress-fence-operator";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>Runs one marker-gated operation and maps failures to a bounded process exit code.</summary>
        /// <param name="args">Strict non-secret process arguments.</param>
        /// <returns>Zero for a completed or deliberately cancelled operation; otherwise two.</returns>
        internal static async Task<int> RunAsync(string[] args)
        {
            string stage = "argument-validation";
            try
            {
                SandboxArguments options = SandboxArguments.Parse(args);
                stage = "control-pipe";
                await using ControlChannel control = await ControlChannel
                    .ConnectAsync(options.ControlPipe)
                    .ConfigureAwait(false);
                string connectionString = await control.ReadSecretAsync().ConfigureAwait(false);
                connectionString = ValidateConnection(connectionString, options.ApplicationName);
                SerializationFailureTracker failureTracker = new();
                FenceCommandInterceptor commandInterceptor = new(control, options.Barrier, failureTracker);
                FenceTransactionInterceptor transactionInterceptor = new(failureTracker);
                DbContextOptions<ServerDbContext> contextOptions =
                    new DbContextOptionsBuilder<ServerDbContext>()
                        .UseNpgsql(connectionString)
                        .EnableSensitiveDataLogging(false)
                        .AddInterceptors(commandInterceptor, transactionInterceptor)
                        .Options;
                ServerContextFactory factory = new(contextOptions);

                await control.WriteSignalAsync(
                    new SandboxSignal("ready", Environment.ProcessId, null, null)).ConfigureAwait(false);
                await control.ReadExpectedAsync("start").ConfigureAwait(false);

                stage = "operation";
                SandboxOperationResult result = await ExecuteAsync(
                    options,
                    factory,
                    failureTracker,
                    control).ConfigureAwait(false);
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(result, JsonOptions)).ConfigureAwait(false);
                return 0;
            }
            catch (Exception exception) when (exception is not StackOverflowException and not OutOfMemoryException)
            {
                string sqlState = FindSqlState(exception) ?? "none";
                await Console.Error.WriteLineAsync(
                        $"r_egress_fence_host.failed:{stage}:{exception.GetType().Name}:{sqlState}")
                    .ConfigureAwait(false);
                return 2;
            }
        }

        /// <summary>Executes the selected production store operation after all process participants are ready.</summary>
        /// <param name="options">Validated non-secret operation arguments.</param>
        /// <param name="factory">Factory for intercepted central PostgreSQL contexts.</param>
        /// <param name="failureTracker">Test-only deduplicated SQLSTATE evidence.</param>
        /// <param name="control">Private parent-process coordination channel.</param>
        /// <returns>Sanitised typed operation evidence.</returns>
        private static async Task<SandboxOperationResult> ExecuteAsync(
            SandboxArguments options,
            IDbContextFactory<ServerDbContext> factory,
            SerializationFailureTracker failureTracker,
            ControlChannel control)
        {
            long operationStarted = Stopwatch.GetTimestamp();
            if (options.Operation == SandboxOperation.Revoke)
            {
                AgentFleetStore store = new(factory, new UnusedAssignmentValidator());
                AgentRevocationOutcome outcome = await store.RevokeAgentAsync(
                    SubjectId,
                    options.AgentId,
                    PlatformPermissions.AgentsRevoke,
                    "r-egress-fence-lab",
                    options.UtcNow,
                    CancellationToken.None).ConfigureAwait(false);
                long completed = Stopwatch.GetTimestamp();
                return new SandboxOperationResult(
                    Environment.ProcessId,
                    "revoke",
                    false,
                    failureTracker.SerializationFailureCount,
                    operationStarted,
                    completed,
                    Stopwatch.Frequency,
                    [
                        new SandboxItemResult(
                            0,
                            outcome.Disposition.ToString(),
                            outcome.ErrorCode,
                            failureTracker.SerializationFailureCount,
                            operationStarted,
                            completed),
                    ]);
            }

            ServerObservationIngestionStore ingestion = new(factory);
            if (options.Operation == SandboxOperation.CancellableIngest)
            {
                using CancellationTokenSource cancellation = new();
                using CancellationTokenSource listenerCancellation = new();
                Task cancellationListener = WaitForCancellationAsync(
                    control,
                    cancellation,
                    listenerCancellation.Token);
                try
                {
                    await control.WriteSignalAsync(
                        new SandboxSignal("started", Environment.ProcessId, null, null)).ConfigureAwait(false);
                    ObservationItemResult item = await ingestion.IngestAsync(
                        Message(options, options.SequenceStart, 0),
                        options.UtcNow,
                        cancellation.Token).ConfigureAwait(false);
                    long completed = Stopwatch.GetTimestamp();
                    return SingleIngestionResult(
                        "ingest-cancellable",
                        options.SequenceStart,
                        item,
                        failureTracker.SerializationFailureCount,
                        operationStarted,
                        completed,
                        cancelled: false);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    long completed = Stopwatch.GetTimestamp();
                    await cancellationListener.ConfigureAwait(false);
                    return new SandboxOperationResult(
                        Environment.ProcessId,
                        "ingest-cancellable",
                        true,
                        failureTracker.SerializationFailureCount,
                        operationStarted,
                        completed,
                        Stopwatch.Frequency,
                        []);
                }
                finally
                {
                    listenerCancellation.Cancel();
                    await cancellationListener.ConfigureAwait(false);
                }
            }

            if (options.Operation == SandboxOperation.Load)
            {
                List<SandboxItemResult> items = new(options.Count);
                for (int offset = 0; offset < options.Count; offset++)
                {
                    long started = Stopwatch.GetTimestamp();
                    long sequence = checked(options.SequenceStart + offset);
                    int serializationFailuresBefore = failureTracker.SerializationFailureCount;
                    ObservationItemResult item = await ingestion.IngestAsync(
                        Message(options, sequence, offset),
                        options.UtcNow.AddMilliseconds(offset),
                        CancellationToken.None).ConfigureAwait(false);
                    long completed = Stopwatch.GetTimestamp();
                    int serializationFailureDelta = checked(
                        failureTracker.SerializationFailureCount - serializationFailuresBefore);
                    items.Add(new SandboxItemResult(
                        sequence,
                        item.Disposition.ToString(),
                        item.ErrorCode,
                        serializationFailureDelta,
                        started,
                        completed));
                }

                return new SandboxOperationResult(
                    Environment.ProcessId,
                    "load",
                    false,
                    failureTracker.SerializationFailureCount,
                    operationStarted,
                    Stopwatch.GetTimestamp(),
                    Stopwatch.Frequency,
                    items);
            }

            ObservationItemResult result = await ingestion.IngestAsync(
                Message(options, options.SequenceStart, 0),
                options.UtcNow,
                CancellationToken.None).ConfigureAwait(false);
            long operationCompleted = Stopwatch.GetTimestamp();
            return SingleIngestionResult(
                "ingest",
                options.SequenceStart,
                result,
                failureTracker.SerializationFailureCount,
                operationStarted,
                operationCompleted,
                cancelled: false);
        }

        /// <summary>Waits for the parent to request cancellation of one database-lock waiter.</summary>
        /// <param name="control">Private process coordination channel.</param>
        /// <param name="cancellation">Token source owned by the current operation.</param>
        /// <param name="listenerCancellation">Cancellation used only to stop the command listener after completion.</param>
        /// <returns>A task completing after the exact cancellation command is received.</returns>
        private static async Task WaitForCancellationAsync(
            ControlChannel control,
            CancellationTokenSource cancellation,
            CancellationToken listenerCancellation)
        {
            try
            {
                await control.ReadExpectedAsync("cancel", listenerCancellation).ConfigureAwait(false);
                cancellation.Cancel();
            }
            catch (OperationCanceledException) when (listenerCancellation.IsCancellationRequested)
            {
                // Completion or another operation failure stops only the private command listener.
            }
            catch
            {
                cancellation.Cancel();
                throw;
            }
        }

        /// <summary>Creates one bounded result for a single observation-ingestion call.</summary>
        /// <param name="operation">Stable operation name exposed to the parent process.</param>
        /// <param name="sequence">Positive stream sequence supplied to the store.</param>
        /// <param name="item">Typed production-store classification.</param>
        /// <param name="serializationFailures">Observed PostgreSQL serialization failures.</param>
        /// <param name="started">Monotonic operation-start timestamp.</param>
        /// <param name="completed">Monotonic operation-completion timestamp.</param>
        /// <param name="cancelled">Whether the operation was deliberately cancelled.</param>
        /// <returns>Sanitised child-process evidence.</returns>
        private static SandboxOperationResult SingleIngestionResult(
            string operation,
            long sequence,
            ObservationItemResult item,
            int serializationFailures,
            long started,
            long completed,
            bool cancelled) =>
            new(
                Environment.ProcessId,
                operation,
                cancelled,
                serializationFailures,
                started,
                completed,
                Stopwatch.Frequency,
                [
                    new SandboxItemResult(
                        sequence,
                        item.Disposition.ToString(),
                        item.ErrorCode,
                        serializationFailures,
                        started,
                        completed),
                ]);

        /// <summary>Creates a unique valid observation without retaining secret or endpoint material.</summary>
        /// <param name="options">Validated operation identity and message seed.</param>
        /// <param name="sequence">Positive Agent stream sequence.</param>
        /// <param name="offset">Zero-based load item offset used only to derive unique identifiers.</param>
        /// <returns>A provider-authenticated synthetic observation.</returns>
        private static ObservationSyncMessage Message(
            SandboxArguments options,
            long sequence,
            int offset) =>
            new(
                DeriveGuid(options.MessageId, offset),
                1,
                sequence,
                DeriveGuid(options.ObservationId, offset),
                options.InstanceId,
                options.AgentId,
                "postgresql",
                "fixture",
                "Healthy",
                "fixture",
                "ProviderAuthenticated",
                1,
                options.UtcNow.AddMilliseconds(offset),
                10,
                null,
                null,
                []);

        /// <summary>Derives a deterministic unique identifier for one bounded load item.</summary>
        /// <param name="seed">Non-empty per-process identifier seed.</param>
        /// <param name="offset">Zero-based offset no greater than twenty-four.</param>
        /// <returns>The unchanged seed for zero, otherwise a deterministic variant.</returns>
        private static Guid DeriveGuid(Guid seed, int offset)
        {
            if (offset == 0)
            {
                return seed;
            }

            byte[] bytes = seed.ToByteArray();
            uint suffix = BitConverter.ToUInt32(bytes, 12);
            BitConverter.GetBytes(unchecked(suffix + (uint)offset)).CopyTo(bytes, 12);
            return new Guid(bytes);
        }

        /// <summary>
        /// Validates the minimum disposable-database identity and rebuilds the secret connection from a fixed safe
        /// option set, so unrecognised source options cannot affect the local laboratory session.
        /// </summary>
        /// <param name="connectionString">Ephemeral secret received over private IPC.</param>
        /// <param name="applicationName">Expected non-secret PostgreSQL session label.</param>
        /// <returns>A rebuilt bounded connection string containing only the options admitted by this host.</returns>
        /// <exception cref="InvalidDataException">Thrown for any unsafe or ambiguous connection configuration.</exception>
        private static string ValidateConnection(string connectionString, string applicationName)
        {
            NpgsqlConnectionStringBuilder source;
            try
            {
                source = new NpgsqlConnectionStringBuilder(connectionString);
            }
            catch (ArgumentException)
            {
                throw new InvalidDataException("r_egress_fence_host.connection_invalid");
            }

            string? password = source.Password;
            if (!IPAddress.TryParse(source.Host, out IPAddress? address) ||
                !IPAddress.IsLoopback(address) ||
                source.Port is < 1 or > 65535 ||
                !string.Equals(source.Database, DisposableDatabaseName, StringComparison.Ordinal) ||
                !string.Equals(source.Username, DisposableDatabaseUser, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(password) || password.Length is < 16 or > 256 ||
                source.Pooling ||
                source.SslMode != SslMode.Disable ||
                source.IncludeErrorDetail ||
                source.LogParameters ||
                source.PersistSecurityInfo ||
                !string.Equals(source.ApplicationName, applicationName, StringComparison.Ordinal))
            {
                throw new InvalidDataException("r_egress_fence_host.connection_invalid");
            }

            // Rebuilding rather than reusing the input strips provider options that could alter session state,
            // diagnostics, routing, pooling or wait bounds.
            return new NpgsqlConnectionStringBuilder
            {
                Host = address.ToString(),
                Port = source.Port,
                Database = DisposableDatabaseName,
                Username = DisposableDatabaseUser,
                Password = password,
                ApplicationName = applicationName,
                SslMode = SslMode.Disable,
                Pooling = false,
                Enlist = false,
                Multiplexing = false,
                NoResetOnClose = false,
                PersistSecurityInfo = false,
                IncludeErrorDetail = false,
                LogParameters = false,
                Timeout = 5,
                CommandTimeout = 15,
                CancellationTimeout = 2000,
            }.ConnectionString;
        }

        /// <summary>Finds the first safe PostgreSQL SQLSTATE in an exception chain.</summary>
        /// <param name="exception">Failure whose nested causes may include a PostgreSQL error.</param>
        /// <returns>A five-character SQLSTATE, or null.</returns>
        private static string? FindSqlState(Exception exception) =>
            FindPostgresException(exception)?.SqlState;

        /// <summary>Finds the first PostgreSQL exception object so duplicate observer callbacks can share its identity.</summary>
        /// <param name="exception">Failure whose nested causes may include a PostgreSQL error.</param>
        /// <returns>The first PostgreSQL exception, or null.</returns>
        private static PostgresException? FindPostgresException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgres &&
                    postgres.SqlState is { Length: 5 })
                {
                    return postgres;
                }
            }

            return null;
        }

        /// <summary>
        /// Deduplicates serialization evidence by the exact PostgreSQL exception instance shared by command and
        /// transaction diagnostics, while retaining no database message or statement text.
        /// </summary>
        private sealed class SerializationFailureTracker
        {
            private static readonly object ObservedMarker = new();
            private readonly ConditionalWeakTable<PostgresException, object> observedFailures = new();
            private int serializationFailureCount;

            /// <summary>Gets the number of distinct SQLSTATE 40001 failures observed by this process.</summary>
            internal int SerializationFailureCount => Volatile.Read(ref serializationFailureCount);

            /// <summary>Records one distinct SQLSTATE 40001 exception without retaining a diagnostic projection.</summary>
            /// <param name="exception">Provider failure supplied by an EF diagnostic callback.</param>
            internal void Record(Exception exception)
            {
                PostgresException? postgres = FindPostgresException(exception);
                if (postgres is not null &&
                    string.Equals(
                        postgres.SqlState,
                        PostgresErrorCodes.SerializationFailure,
                        StringComparison.Ordinal) &&
                    observedFailures.TryAdd(postgres, ObservedMarker))
                {
                    Interlocked.Increment(ref serializationFailureCount);
                }
            }
        }

        /// <summary>Creates fresh central contexts with the exact intercepted PostgreSQL options.</summary>
        /// <param name="options">Immutable provider options for this process.</param>
        private sealed class ServerContextFactory(DbContextOptions<ServerDbContext> options) :
            IDbContextFactory<ServerDbContext>
        {
            /// <inheritdoc />
            public ServerDbContext CreateDbContext() => new(options);

            /// <inheritdoc />
            public Task<ServerDbContext> CreateDbContextAsync(
                CancellationToken cancellationToken = default) =>
                Task.FromResult(new ServerDbContext(options));
        }

        /// <summary>Rejects the unrelated assignment capability; revocation never invokes it.</summary>
        private sealed class UnusedAssignmentValidator : IAgentAssignmentValidator
        {
            /// <inheritdoc />
            public bool TryValidate(
                AgentReadOnlyAssignment assignment,
                string expectedEnvironment,
                out string? errorCode)
            {
                errorCode = "r_egress_fence_host.assignment_not_available";
                return false;
            }
        }

        /// <summary>
        /// Observes PostgreSQL commands, counts SQLSTATE 40001 and pauses only after the selected locking statement
        /// has executed, so the parent can prove a real cross-process blocking relationship.
        /// </summary>
        private sealed class FenceCommandInterceptor(
            ControlChannel control,
            SandboxBarrier barrier,
            SerializationFailureTracker failureTracker) : DbCommandInterceptor
        {
            private int barrierEntered;

            /// <inheritdoc />
            public override async ValueTask<InterceptionResult> DataReaderClosingAsync(
                DbCommand command,
                DataReaderClosingEventData eventData,
                InterceptionResult result)
            {
                // ReaderExecutedAsync is too early for a streamed PostgreSQL reader: EF has not consumed the row
                // that proves a SELECT lock or an UPDATE RETURNING effect. Closing occurs after materialisation.
                await PauseIfSelectedAsync(command, CancellationToken.None).ConfigureAwait(false);
                return result;
            }

            /// <inheritdoc />
            public override async ValueTask<int> NonQueryExecutedAsync(
                DbCommand command,
                CommandExecutedEventData eventData,
                int result,
                CancellationToken cancellationToken = default)
            {
                await PauseIfSelectedAsync(command, cancellationToken).ConfigureAwait(false);
                return result;
            }

            /// <inheritdoc />
            public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
            {
                failureTracker.Record(eventData.Exception);
                base.CommandFailed(command, eventData);
            }

            /// <inheritdoc />
            public override Task CommandFailedAsync(
                DbCommand command,
                CommandErrorEventData eventData,
                CancellationToken cancellationToken = default)
            {
                failureTracker.Record(eventData.Exception);
                return base.CommandFailedAsync(command, eventData, cancellationToken);
            }

            /// <summary>Pauses exactly once after the configured lock-owning statement has executed.</summary>
            /// <param name="command">Executed database command.</param>
            /// <param name="cancellationToken">Command cancellation propagated by EF.</param>
            /// <returns>A task completing after the parent releases the barrier.</returns>
            private async Task PauseIfSelectedAsync(
                DbCommand command,
                CancellationToken cancellationToken)
            {
                bool matches = barrier switch
                {
                    SandboxBarrier.IdentityLock =>
                        command.CommandText.Contains("FOR NO KEY UPDATE", StringComparison.OrdinalIgnoreCase),
                    SandboxBarrier.AgentUpdate =>
                        command.CommandText.Contains("UPDATE agents", StringComparison.OrdinalIgnoreCase) ||
                        command.CommandText.Contains("UPDATE \"agents\"", StringComparison.OrdinalIgnoreCase),
                    _ => false,
                };
                if (!matches || Interlocked.CompareExchange(ref barrierEntered, 1, 0) != 0)
                {
                    return;
                }

                if (command.Connection is not NpgsqlConnection connection || connection.ProcessID < 1)
                {
                    throw new InvalidOperationException("r_egress_fence_host.backend_identity_unavailable");
                }

                await control.WriteSignalAsync(
                    new SandboxSignal(
                        "barrier",
                        Environment.ProcessId,
                        connection.ProcessID,
                        barrier == SandboxBarrier.IdentityLock ? "identity-lock" : "agent-update"),
                    cancellationToken).ConfigureAwait(false);
                await control.ReadExpectedAsync("release", cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Observes transaction failures so a serialization error raised by PostgreSQL during commit is not lost
        /// after the production store performs its bounded reclassification.
        /// </summary>
        private sealed class FenceTransactionInterceptor(
            SerializationFailureTracker failureTracker) : DbTransactionInterceptor
        {
            /// <inheritdoc />
            public override void TransactionFailed(
                DbTransaction transaction,
                TransactionErrorEventData eventData)
            {
                failureTracker.Record(eventData.Exception);
                base.TransactionFailed(transaction, eventData);
            }

            /// <inheritdoc />
            public override Task TransactionFailedAsync(
                DbTransaction transaction,
                TransactionErrorEventData eventData,
                CancellationToken cancellationToken = default)
            {
                failureTracker.Record(eventData.Exception);
                return base.TransactionFailedAsync(transaction, eventData, cancellationToken);
            }
        }

        /// <summary>Owns framed current-user-only parent/child coordination without logging transferred payloads.</summary>
        private sealed class ControlChannel(NamedPipeClientStream pipe) : IAsyncDisposable
        {
            private const int MaximumSecretBytes = 4096;
            private const int MaximumControlBytes = 1024;
            private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
            private static readonly TimeSpan FrameTimeout = TimeSpan.FromSeconds(30);
            private static readonly UTF8Encoding StrictUtf8 = new(false, true);

            /// <summary>Connects to the exact private pipe within a bounded wait.</summary>
            /// <param name="pipeName">Validated opaque pipe identifier.</param>
            /// <returns>A connected control channel.</returns>
            internal static async Task<ControlChannel> ConnectAsync(string pipeName)
            {
                NamedPipeClientStream pipe = new(
                    ".",
                    pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                try
                {
                    using CancellationTokenSource timeout = new(ConnectTimeout);
                    try
                    {
                        await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                    {
                        throw new TimeoutException("r_egress_fence_host.control_timeout");
                    }

                    return new ControlChannel(pipe);
                }
                catch
                {
                    await pipe.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }

            /// <summary>Reads the single bounded connection secret transferred before operation admission.</summary>
            /// <returns>The ephemeral connection string held only in process memory.</returns>
            internal async Task<string> ReadSecretAsync()
            {
                using CancellationTokenSource timeout = CreateFrameTimeout(CancellationToken.None);
                byte[] payload;
                try
                {
                    payload = await ReadFrameAsync(MaximumSecretBytes, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    throw new TimeoutException("r_egress_fence_host.control_timeout");
                }

                try
                {
                    return StrictUtf8.GetString(payload);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                }
            }

            /// <summary>Writes one bounded sanitised coordination signal.</summary>
            /// <param name="signal">Non-secret signal.</param>
            /// <param name="cancellationToken">Optional bounded cancellation.</param>
            /// <returns>A task completing after the frame is flushed.</returns>
            internal async Task WriteSignalAsync(
                SandboxSignal signal,
                CancellationToken cancellationToken = default)
            {
                using CancellationTokenSource timeout = CreateFrameTimeout(cancellationToken);
                try
                {
                    await WriteFrameAsync(
                        JsonSerializer.SerializeToUtf8Bytes(signal, JsonOptions),
                        MaximumControlBytes,
                        timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("r_egress_fence_host.control_timeout");
                }
            }

            /// <summary>Reads and validates one exact parent coordination command.</summary>
            /// <param name="expected">Expected stable command name.</param>
            /// <param name="cancellationToken">Optional bounded cancellation.</param>
            /// <returns>A task completing after validation.</returns>
            internal async Task ReadExpectedAsync(
                string expected,
                CancellationToken cancellationToken = default)
            {
                using CancellationTokenSource timeout = CreateFrameTimeout(cancellationToken);
                byte[] payload;
                try
                {
                    payload = await ReadFrameAsync(MaximumControlBytes, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException("r_egress_fence_host.control_timeout");
                }

                try
                {
                    using JsonDocument document = JsonDocument.Parse(
                        payload,
                        new JsonDocumentOptions
                        {
                            AllowTrailingCommas = false,
                            CommentHandling = JsonCommentHandling.Disallow,
                            MaxDepth = 2,
                        });
                    JsonElement root = document.RootElement;
                    JsonProperty[] properties = root.ValueKind == JsonValueKind.Object
                        ? [.. root.EnumerateObject()]
                        : [];
                    if (properties.Length != 1 ||
                        !properties[0].NameEquals("command") ||
                        properties[0].Value.ValueKind != JsonValueKind.String ||
                        !string.Equals(
                            properties[0].Value.GetString(),
                            expected,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidDataException("r_egress_fence_host.control_invalid");
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                }
            }

            /// <summary>Creates a linked per-frame deadline without weakening a caller's earlier cancellation.</summary>
            /// <param name="cancellationToken">Optional caller cancellation.</param>
            /// <returns>A caller-owned source cancelled by either boundary.</returns>
            private static CancellationTokenSource CreateFrameTimeout(CancellationToken cancellationToken)
            {
                CancellationTokenSource timeout = CancellationTokenSource
                    .CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(FrameTimeout);
                return timeout;
            }

            /// <summary>Reads one length-prefixed bounded frame.</summary>
            /// <param name="maximumLength">Maximum accepted payload bytes.</param>
            /// <param name="cancellationToken">Cancellation applied to pipe reads.</param>
            /// <returns>The caller-owned payload.</returns>
            private async Task<byte[]> ReadFrameAsync(
                int maximumLength,
                CancellationToken cancellationToken)
            {
                byte[] length = new byte[sizeof(int)];
                await pipe.ReadExactlyAsync(length, cancellationToken).ConfigureAwait(false);
                int count = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(length));
                if (count is < 1 || count > maximumLength)
                {
                    throw new InvalidDataException("r_egress_fence_host.frame_invalid");
                }

                byte[] payload = new byte[count];
                try
                {
                    await pipe.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
                    return payload;
                }
                catch
                {
                    CryptographicOperations.ZeroMemory(payload);
                    throw;
                }
            }

            /// <summary>Writes one length-prefixed bounded frame and clears its temporary buffers.</summary>
            /// <param name="payload">Caller-owned non-secret payload.</param>
            /// <param name="maximumLength">Maximum permitted payload bytes.</param>
            /// <param name="cancellationToken">Cancellation applied to pipe writes.</param>
            /// <returns>A task completing after flush.</returns>
            private async Task WriteFrameAsync(
                byte[] payload,
                int maximumLength,
                CancellationToken cancellationToken)
            {
                try
                {
                    if (payload.Length is < 1 || payload.Length > maximumLength)
                    {
                        throw new InvalidDataException("r_egress_fence_host.frame_invalid");
                    }

                    byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
                    await pipe.WriteAsync(length, cancellationToken).ConfigureAwait(false);
                    await pipe.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                    await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(payload);
                }
            }

            /// <summary>Closes only the current child-process pipe endpoint.</summary>
            /// <returns>A completed asynchronous disposal.</returns>
            public ValueTask DisposeAsync() => pipe.DisposeAsync();
        }

        /// <summary>Identifies the production-store operation selected for one child process.</summary>
        private enum SandboxOperation
        {
            Ingest,
            CancellableIngest,
            Revoke,
            Load,
        }

        /// <summary>Identifies the test-only post-command boundary selected for deterministic coordination.</summary>
        private enum SandboxBarrier
        {
            None,
            IdentityLock,
            AgentUpdate,
        }

        /// <summary>Captures validated non-secret arguments for one process operation.</summary>
        private sealed record SandboxArguments(
            SandboxOperation Operation,
            SandboxBarrier Barrier,
            string ControlPipe,
            string ApplicationName,
            Guid AgentId,
            Guid InstanceId,
            Guid MessageId,
            Guid ObservationId,
            long SequenceStart,
            int Count,
            DateTimeOffset UtcNow)
        {
            /// <summary>Parses only the exact activation marker and complete bounded option set.</summary>
            /// <param name="args">Raw process arguments.</param>
            /// <returns>Validated sandbox arguments.</returns>
            internal static SandboxArguments Parse(string[] args)
            {
                ArgumentNullException.ThrowIfNull(args);
                if (args.Length != 23 || args[0] != ActivationMarker)
                {
                    throw new ArgumentException("r_egress_fence_host.arguments_invalid", nameof(args));
                }

                Dictionary<string, string> values = new(StringComparer.Ordinal);
                for (int index = 1; index < args.Length; index += 2)
                {
                    if (!args[index].StartsWith("--", StringComparison.Ordinal) ||
                        !values.TryAdd(args[index], args[index + 1]))
                    {
                        throw new ArgumentException("r_egress_fence_host.arguments_invalid", nameof(args));
                    }
                }

                string operationText = Require(values, "--operation");
                string barrierText = Require(values, "--barrier");
                string pipe = Require(values, "--control-pipe");
                string applicationName = Require(values, "--application-name");
                SandboxOperation operation = default;
                SandboxBarrier barrier = default;
                Guid agentId = Guid.Empty;
                Guid instanceId = Guid.Empty;
                Guid messageId = Guid.Empty;
                Guid observationId = Guid.Empty;
                long sequenceStart = 0;
                int count = 0;
                DateTimeOffset utcNow = default;
                bool parsed =
                    TryParseOperation(operationText, out operation) &&
                    TryParseBarrier(barrierText, out barrier) &&
                    Guid.TryParseExact(Require(values, "--agent-id"), "D", out agentId) &&
                    Guid.TryParseExact(Require(values, "--instance-id"), "D", out instanceId) &&
                    Guid.TryParseExact(Require(values, "--message-id"), "D", out messageId) &&
                    Guid.TryParseExact(Require(values, "--observation-id"), "D", out observationId) &&
                    long.TryParse(
                        Require(values, "--sequence-start"),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out sequenceStart) &&
                    int.TryParse(
                        Require(values, "--count"),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out count) &&
                    DateTimeOffset.TryParseExact(
                        Require(values, "--utc-now"),
                        "O",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal,
                        out utcNow);
                if (!parsed || values.Count != 0 ||
                    agentId == Guid.Empty || instanceId == Guid.Empty ||
                    messageId == Guid.Empty || observationId == Guid.Empty ||
                    sequenceStart < 1 || count is < 1 or > 25 ||
                    utcNow.Offset != TimeSpan.Zero ||
                    !IsStable(pipe, 8, 160) ||
                    !IsStable(applicationName, 8, 63) ||
                    (operation != SandboxOperation.Load && count != 1) ||
                    sequenceStart > long.MaxValue - (count - 1L) ||
                    utcNow > DateTimeOffset.MaxValue.AddMilliseconds(-(count - 1)) ||
                    !IsAllowedCombination(operation, barrier))
                {
                    throw new ArgumentException("r_egress_fence_host.arguments_invalid", nameof(args));
                }

                return new SandboxArguments(
                    operation,
                    barrier,
                    pipe,
                    applicationName,
                    agentId,
                    instanceId,
                    messageId,
                    observationId,
                    sequenceStart,
                    count,
                    utcNow);
            }

            /// <summary>Maps one exact external operation name without admitting numeric or undefined enum values.</summary>
            /// <param name="value">Untrusted command-line value.</param>
            /// <param name="operation">Recognised operation when this method returns true.</param>
            /// <returns>True only for one canonical lower-case operation name.</returns>
            private static bool TryParseOperation(string value, out SandboxOperation operation)
            {
                operation = value switch
                {
                    "ingest" => SandboxOperation.Ingest,
                    "ingest-cancellable" => SandboxOperation.CancellableIngest,
                    "revoke" => SandboxOperation.Revoke,
                    "load" => SandboxOperation.Load,
                    _ => default,
                };
                return value is "ingest" or "ingest-cancellable" or "revoke" or "load";
            }

            /// <summary>Maps one exact external barrier name without admitting numeric or undefined enum values.</summary>
            /// <param name="value">Untrusted command-line value.</param>
            /// <param name="barrier">Recognised barrier when this method returns true.</param>
            /// <returns>True only for one canonical lower-case barrier name.</returns>
            private static bool TryParseBarrier(string value, out SandboxBarrier barrier)
            {
                barrier = value switch
                {
                    "none" => SandboxBarrier.None,
                    "identity-lock" => SandboxBarrier.IdentityLock,
                    "agent-update" => SandboxBarrier.AgentUpdate,
                    _ => default,
                };
                return value is "none" or "identity-lock" or "agent-update";
            }

            /// <summary>Restricts barriers to the production operation that can execute their owning statement.</summary>
            /// <param name="operation">Validated operation.</param>
            /// <param name="barrier">Validated barrier.</param>
            /// <returns>True only when the child can reach the requested boundary.</returns>
            private static bool IsAllowedCombination(
                SandboxOperation operation,
                SandboxBarrier barrier) =>
                (operation, barrier) switch
                {
                    (SandboxOperation.Ingest, SandboxBarrier.None or SandboxBarrier.IdentityLock) => true,
                    (SandboxOperation.CancellableIngest, SandboxBarrier.None) => true,
                    (SandboxOperation.Revoke, SandboxBarrier.None or SandboxBarrier.AgentUpdate) => true,
                    (SandboxOperation.Load, SandboxBarrier.None or SandboxBarrier.IdentityLock) => true,
                    _ => false,
                };

            /// <summary>Removes and returns one required option.</summary>
            /// <param name="values">Remaining parsed option map.</param>
            /// <param name="name">Exact option name.</param>
            /// <returns>Non-empty option value.</returns>
            private static string Require(Dictionary<string, string> values, string name)
            {
                if (!values.Remove(name, out string? value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("r_egress_fence_host.arguments_invalid", nameof(values));
                }

                return value;
            }

            /// <summary>Checks a bounded identifier alphabet safe for command lines and PostgreSQL labels.</summary>
            /// <param name="value">Untrusted candidate.</param>
            /// <param name="minimum">Minimum inclusive length.</param>
            /// <param name="maximum">Maximum inclusive length.</param>
            /// <returns>True only for the stable bounded alphabet.</returns>
            private static bool IsStable(string value, int minimum, int maximum) =>
                value.Length >= minimum && value.Length <= maximum &&
                value.All(character =>
                    char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');
        }

        /// <summary>Represents one bounded child coordination signal.</summary>
        /// <param name="Signal">Stable signal name.</param>
        /// <param name="ProcessId">Exact operating-system child process identifier.</param>
        /// <param name="BackendPid">PostgreSQL backend process identifier when a command owns a lock.</param>
        /// <param name="Boundary">Selected barrier name when applicable.</param>
        private sealed record SandboxSignal(
            string Signal,
            int ProcessId,
            int? BackendPid,
            string? Boundary);

        /// <summary>Reports one sanitised child-process operation.</summary>
        /// <param name="ProcessId">Exact operating-system child process identifier.</param>
        /// <param name="Operation">Stable operation name.</param>
        /// <param name="Cancelled">Whether deliberate cancellation ended the operation.</param>
        /// <param name="SerializationFailureCount">Observed SQLSTATE 40001 count.</param>
        /// <param name="StartedTimestamp">Monotonic process-wide start timestamp.</param>
        /// <param name="CompletedTimestamp">Monotonic process-wide completion timestamp.</param>
        /// <param name="StopwatchFrequency">Monotonic ticks per second.</param>
        /// <param name="Items">Bounded per-item outcomes.</param>
        private sealed record SandboxOperationResult(
            int ProcessId,
            string Operation,
            bool Cancelled,
            int SerializationFailureCount,
            long StartedTimestamp,
            long CompletedTimestamp,
            long StopwatchFrequency,
            IReadOnlyList<SandboxItemResult> Items);

        /// <summary>Reports one bounded observation or revocation outcome.</summary>
        /// <param name="Sequence">Observation sequence, or zero for revocation.</param>
        /// <param name="Disposition">Typed production result name.</param>
        /// <param name="ErrorCode">Stable sanitised error code.</param>
        /// <param name="SerializationFailureCount">SQLSTATE 40001 failures attributable to this exact item.</param>
        /// <param name="StartedTimestamp">Monotonic item-start timestamp.</param>
        /// <param name="CompletedTimestamp">Monotonic item-completion timestamp.</param>
        private sealed record SandboxItemResult(
            long Sequence,
            string Disposition,
            string? ErrorCode,
            int SerializationFailureCount,
            long StartedTimestamp,
            long CompletedTimestamp);
    }
}
