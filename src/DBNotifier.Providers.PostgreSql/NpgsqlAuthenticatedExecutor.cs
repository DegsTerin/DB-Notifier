// Module purpose: Implements Npgsql Authenticated Executor inside the isolated PostgreSQL provider; the core remains engine-neutral.
using System.Diagnostics;
using Npgsql;

namespace DBNotifier.Providers.PostgreSql;

public sealed class NpgsqlAuthenticatedExecutor : IPostgreSqlAuthenticatedExecutor
{
    private const string HealthQuery = "SELECT 1";

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
            SslMode = string.Equals(endpoint.SslMode, "verify-full", StringComparison.OrdinalIgnoreCase)
                ? SslMode.VerifyFull
                : SslMode.Require,
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
