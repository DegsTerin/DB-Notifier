// Module purpose: Defines endpoint and probe contracts inside the isolated PostgreSQL provider; the core remains engine-neutral.
using System.Globalization;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>Contains validated, non-secret PostgreSQL endpoint and approved-path readiness-utility hints.</summary>
/// <param name="Host">DNS name, IP literal or supported local socket path.</param>
/// <param name="Port">PostgreSQL TCP port between 1 and 65,535.</param>
/// <param name="Database">Optional literal database name used only by authenticated probes.</param>
/// <param name="PgIsReadyPath">Exact utility name or fully qualified candidate path.</param>
/// <param name="PostgreSqlExecutablePath">Optional fully qualified server executable used to derive a sibling utility candidate.</param>
/// <param name="SslMode">Required hostname-verifying TLS mode.</param>
public sealed record PostgreSqlEndpoint(
    string Host,
    int Port,
    string? Database,
    string PgIsReadyPath,
    string? PostgreSqlExecutablePath,
    string SslMode)
{
    /// <summary>Gets the platform-neutral default readiness-utility name.</summary>
    public const string DefaultExecutable = "pg_isready";

    /// <summary>Maps one previously validated provider-neutral endpoint into the PostgreSQL provider contract.</summary>
    /// <param name="endpoint">Validated endpoint whose properties contain no secret material.</param>
    /// <returns>A typed PostgreSQL endpoint with fail-closed TLS defaults.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when a required validated property is absent.</exception>
    /// <exception cref="FormatException">Thrown when the validated port is not a decimal integer.</exception>
    public static PostgreSqlEndpoint FromProviderEndpoint(ProviderEndpoint endpoint)
    {
        string host = endpoint.Properties["host"];
        int port = int.Parse(endpoint.Properties["port"], NumberStyles.None, CultureInfo.InvariantCulture);
        endpoint.TryGetValue("database", out string? database);
        string executable = endpoint.TryGetValue("pgIsReadyPath", out string? configuredPath)
            ? configuredPath
            : DefaultExecutable;
        endpoint.TryGetValue("postgresExecutablePath", out string? postgresExecutablePath);
        string sslMode = endpoint.TryGetValue("sslMode", out string? configuredSslMode)
            ? configuredSslMode
            : "verify-full";

        return new PostgreSqlEndpoint(host, port, database, executable, postgresExecutablePath, sslMode);
    }
}

/// <summary>Describes provider readiness evidence returned by <c>pg_isready</c> or the bounded transport fallback.</summary>
public enum PostgreSqlReadinessState
{
    /// <summary>The provider utility reported that PostgreSQL accepts connections.</summary>
    Accepting,

    /// <summary>The provider utility reported that PostgreSQL rejects connections temporarily.</summary>
    Rejecting,

    /// <summary>No response was obtained within the provider or transport contract.</summary>
    NoResponse,

    /// <summary>The readiness probe could not run within its configuration boundary.</summary>
    InvalidConfiguration,

    /// <summary>The readiness or transport probe exceeded its deadline.</summary>
    TimedOut,

    /// <summary>TCP reachability was proved without PostgreSQL readiness or authentication.</summary>
    TransportReachable,
}

/// <summary>Contains one bounded PostgreSQL readiness outcome.</summary>
/// <param name="State">Canonical provider readiness state.</param>
/// <param name="Method">Stable evidence method such as <c>pg_isready</c> or <c>tcp</c>.</param>
/// <param name="Duration">Measured probe duration.</param>
/// <param name="ErrorCode">Optional sanitised configuration failure code when no probe was attempted.</param>
public sealed record PostgreSqlReadinessResult(
    PostgreSqlReadinessState State,
    string Method,
    TimeSpan Duration,
    string? ErrorCode = null);

/// <summary>Executes PostgreSQL readiness checks behind the isolated provider boundary.</summary>
public interface IPostgreSqlReadinessExecutor
{
    /// <summary>Executes one bounded readiness check without accepting secret arguments.</summary>
    /// <param name="endpoint">Validated non-secret PostgreSQL endpoint.</param>
    /// <param name="timeout">Positive probe deadline.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to process or transport work.</param>
    /// <returns>A canonical readiness result with an explicit evidence method.</returns>
    ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

/// <summary>Describes TCP reachability evidence that must not be promoted to PostgreSQL health.</summary>
public enum PostgreSqlTransportState
{
    /// <summary>A TCP connection was established without provider authentication.</summary>
    Reachable,

    /// <summary>The endpoint did not accept a TCP connection.</summary>
    NoResponse,

    /// <summary>The TCP attempt exceeded its deadline.</summary>
    TimedOut,

    /// <summary>The endpoint cannot be represented by this TCP probe.</summary>
    Invalid,
}

/// <summary>Probes bounded TCP reachability without interpreting it as provider readiness.</summary>
public interface IPostgreSqlTransportProbe
{
    /// <summary>Attempts one direct transport connection.</summary>
    /// <param name="endpoint">Validated PostgreSQL endpoint.</param>
    /// <param name="timeout">Positive transport deadline.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to socket work.</param>
    /// <returns>Reachability-only evidence with an optional sanitised configuration failure.</returns>
    ValueTask<PostgreSqlTransportResult> ProbeAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

/// <summary>Contains one transport-only result without disclosing its destination.</summary>
/// <param name="State">Canonical TCP reachability state.</param>
/// <param name="ErrorCode">Optional sanitised configuration failure code when no socket was opened.</param>
public sealed record PostgreSqlTransportResult(
    PostgreSqlTransportState State,
    string? ErrorCode = null);

/// <summary>Describes a fixed-query PostgreSQL result obtained with a monitoring credential.</summary>
public enum PostgreSqlAuthenticatedState
{
    /// <summary>The authenticated fixed query completed successfully.</summary>
    Healthy,

    /// <summary>PostgreSQL rejected the monitoring credential.</summary>
    AuthenticationFailed,

    /// <summary>The authenticated endpoint was unavailable.</summary>
    Unavailable,

    /// <summary>The authenticated probe exceeded its deadline.</summary>
    TimedOut,

    /// <summary>The configured destination was not authorised, so no connection was attempted.</summary>
    InvalidConfiguration,

    /// <summary>The probe completed without conclusive health evidence.</summary>
    Failed,
}

/// <summary>Contains one canonical authenticated PostgreSQL probe outcome.</summary>
/// <param name="State">Canonical authenticated state.</param>
/// <param name="Duration">Measured connection and fixed-query duration.</param>
/// <param name="ErrorCode">Optional sanitised configuration failure code when no probe was attempted.</param>
public sealed record PostgreSqlAuthenticatedResult(
    PostgreSqlAuthenticatedState State,
    TimeSpan Duration,
    string? ErrorCode = null);

/// <summary>Executes fixed authenticated PostgreSQL probes without exposing provider details to the neutral core.</summary>
public interface IPostgreSqlAuthenticatedExecutor
{
    /// <summary>Executes one hostname-verifying TLS probe with an ephemeral monitoring credential.</summary>
    /// <param name="endpoint">Validated PostgreSQL endpoint.</param>
    /// <param name="credential">Ephemeral least-privilege monitoring credential.</param>
    /// <param name="timeout">Positive probe deadline.</param>
    /// <param name="cancellationToken">Caller cancellation propagated to connection and query work.</param>
    /// <returns>A canonical authenticated result without provider exception details.</returns>
    ValueTask<PostgreSqlAuthenticatedResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        IProviderCredential credential,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
