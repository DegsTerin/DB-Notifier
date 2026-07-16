// Module purpose: Executes bounded authenticated PostgreSQL probes inside the isolated provider; the core remains engine-neutral.
using System.Diagnostics;
using Npgsql;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>
/// Executes the fixed authenticated PostgreSQL health query with hostname-verifying TLS, certificate revocation
/// checking, bounded timeouts and no pooling or connection-string persistence.
/// </summary>
public sealed class NpgsqlAuthenticatedExecutor : IPostgreSqlAuthenticatedExecutor
{
    private const string HealthQuery = "SELECT 1";

    /// <summary>Executes one authenticated <c>SELECT 1</c> probe using the leased monitoring credential.</summary>
    /// <param name="endpoint">Validated PostgreSQL endpoint requiring <c>verify-full</c> TLS.</param>
    /// <param name="credential">Ephemeral monitoring credential lease; the secret is never logged or persisted.</param>
    /// <param name="timeout">Positive probe deadline, bounded again by the provider request policy.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to connection and query operations.</param>
    /// <returns>A canonical authenticated health result without provider exception details.</returns>
    public async ValueTask<PostgreSqlAuthenticatedResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        DBNotifier.Provider.Abstractions.IProviderCredential credential,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        if (string.IsNullOrWhiteSpace(credential.UserName) || credential.Secret.IsEmpty)
        {
            return new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.AuthenticationFailed, TimeSpan.Zero);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        string password = new(credential.Secret.Span);
        NpgsqlConnectionStringBuilder connectionString = new()
        {
            Host = endpoint.Host,
            Port = endpoint.Port,
            Database = string.IsNullOrWhiteSpace(endpoint.Database) ? "postgres" : endpoint.Database,
            Username = credential.UserName,
            Pooling = false,
            Timeout = TimeoutSeconds(timeout),
            CommandTimeout = TimeoutSeconds(timeout),
            SslMode = SslMode.VerifyFull,
            CheckCertificateRevocation = true,
        };

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString.ConnectionString);
            dataSourceBuilder.UsePasswordProvider(
                _ => password,
                (_, _) => ValueTask.FromResult(password));
            await using NpgsqlDataSource dataSource = dataSourceBuilder.Build();
            await using NpgsqlConnection connection = await dataSource
                .OpenConnectionAsync(deadline.Token)
                .ConfigureAwait(false);
            await using NpgsqlCommand command = new(HealthQuery, connection)
            {
                CommandTimeout = TimeoutSeconds(timeout),
            };
            object? result = await command.ExecuteScalarAsync(deadline.Token).ConfigureAwait(false);
            return result is 1
                ? new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.Healthy, stopwatch.Elapsed)
                : new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.Failed, stopwatch.Elapsed);
        }
        catch (PostgresException exception) when (exception.SqlState.StartsWith("28", StringComparison.Ordinal))
        {
            return new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.AuthenticationFailed, stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.TimedOut, stopwatch.Elapsed);
        }
        catch (NpgsqlException)
        {
            return new PostgreSqlAuthenticatedResult(PostgreSqlAuthenticatedState.Unavailable, stopwatch.Elapsed);
        }
        finally
        {
            password = string.Empty;
        }
    }

    private static int TimeoutSeconds(TimeSpan timeout)
    {
        double seconds = Math.Ceiling(timeout.TotalSeconds);
        return seconds >= 300 ? 300 : Math.Max(1, (int)seconds);
    }
}
