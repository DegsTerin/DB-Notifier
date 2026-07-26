// Module purpose: Executes bounded authenticated PostgreSQL probes inside the isolated provider; the core remains engine-neutral.
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Provider.Abstractions;
using Npgsql;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>
/// Executes the fixed authenticated PostgreSQL health query with hostname-verifying TLS, certificate revocation
/// checking, bounded timeouts and no pooling or connection-string persistence.
/// </summary>
/// <param name="networkAuthorizer">Local authority that resolves and admits the exact monitoring destination.</param>
public sealed class NpgsqlAuthenticatedExecutor(
    INetworkEgressAuthorizer networkAuthorizer) : IPostgreSqlAuthenticatedExecutor
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
        PostgreSqlNetworkResolution network = await PostgreSqlNetworkEgress
            .ResolveAsync(networkAuthorizer, endpoint, cancellationToken)
            .ConfigureAwait(false);
        if (!network.IsApproved)
        {
            return new PostgreSqlAuthenticatedResult(
                PostgreSqlAuthenticatedState.InvalidConfiguration,
                stopwatch.Elapsed,
                network.FailureCode);
        }

        endpoint = network.Endpoint;
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
            CheckCertificateRevocation = false,
        };

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            NpgsqlDataSourceBuilder dataSourceBuilder = new(connectionString.ConnectionString);
            dataSourceBuilder.UsePasswordProvider(
                _ => password,
                (_, _) => ValueTask.FromResult(password));
            dataSourceBuilder.UseSslClientAuthenticationOptionsCallback(
                options => ConfigureSslOptions(options, network.OriginalHost));
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

    /// <summary>
    /// Preserves the original hostname for SNI and identity verification while preventing certificate downloads
    /// or online revocation traffic.
    /// </summary>
    /// <param name="options">TLS options created by Npgsql for the IP-pinned connection.</param>
    /// <param name="originalHost">Original validated hostname retained solely for TLS server identity.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="originalHost"/> is absent.</exception>
    internal static void ConfigureSslOptions(
        SslClientAuthenticationOptions options,
        string originalHost)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalHost);
        options.TargetHost = originalHost;
        options.CertificateRevocationCheckMode = X509RevocationMode.Offline;
        options.CertificateChainPolicy = OfflineCertificateChainPolicy.CreateServerAuthentication();
    }

    /// <summary>Converts one positive provider deadline into Npgsql's bounded whole-second contract.</summary>
    /// <param name="timeout">Positive provider deadline.</param>
    /// <returns>A whole-second timeout between one and 300.</returns>
    private static int TimeoutSeconds(TimeSpan timeout)
    {
        double seconds = Math.Ceiling(timeout.TotalSeconds);
        return seconds >= 300 ? 300 : Math.Max(1, (int)seconds);
    }
}
