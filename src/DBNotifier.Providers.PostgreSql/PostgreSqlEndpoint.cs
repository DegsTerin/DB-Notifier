using System.Globalization;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

public sealed record PostgreSqlEndpoint(
    string Host,
    int Port,
    string? Database,
    string PgIsReadyPath)
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

        return new PostgreSqlEndpoint(host, port, database, executable);
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
