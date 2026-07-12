using System.Globalization;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

public sealed record PostgreSqlEndpoint(
    string Host,
    int Port,
    string? Database,
    string PgIsReadyPath,
    string SslMode)
{
    public const string DefaultExecutable = "pg_isready";

    public static PostgreSqlEndpoint FromProviderEndpoint(ProviderEndpoint endpoint)
    {
        string host = endpoint.Properties["host"];
        int port = int.Parse(endpoint.Properties["port"], NumberStyles.None, CultureInfo.InvariantCulture);
        endpoint.TryGetValue("database", out string? database);
        string executable = endpoint.TryGetValue("pgIsReadyPath", out string? configuredPath)
            ? configuredPath
            : DefaultExecutable;
        string sslMode = endpoint.TryGetValue("sslMode", out string? configuredSslMode)
            ? configuredSslMode
            : "verify-full";

        return new PostgreSqlEndpoint(host, port, database, executable, sslMode);
    }
}

public enum PostgreSqlReadinessState
{
    Accepting,
    Rejecting,
    NoResponse,
    InvalidConfiguration,
    TimedOut,
    TransportReachable,
}

public sealed record PostgreSqlReadinessResult(
    PostgreSqlReadinessState State,
    string Method,
    TimeSpan Duration);

public interface IPostgreSqlReadinessExecutor
{
    ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

public enum PostgreSqlAuthenticatedState
{
    Healthy,
    AuthenticationFailed,
    Unavailable,
    TimedOut,
    Failed,
}

public sealed record PostgreSqlAuthenticatedResult(
    PostgreSqlAuthenticatedState State,
    TimeSpan Duration);

public interface IPostgreSqlAuthenticatedExecutor
{
    ValueTask<PostgreSqlAuthenticatedResult> ExecuteAsync(
        PostgreSqlEndpoint endpoint,
        IProviderCredential credential,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
