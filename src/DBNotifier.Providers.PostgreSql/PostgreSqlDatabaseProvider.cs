// Module purpose: Implements the database-provider adapter inside the isolated PostgreSQL provider; the core remains engine-neutral.
using System.Globalization;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

/// <summary>
/// Adapts PostgreSQL endpoint validation, readiness and authenticated monitoring to the provider-neutral SDK;
/// administrative control capabilities remain explicitly unsupported.
/// </summary>
/// <param name="readinessExecutor">Approved-path readiness boundary with an explicit transport-only fallback.</param>
/// <param name="authenticatedExecutor">TLS-authenticated fixed-query health boundary.</param>
public sealed class PostgreSqlDatabaseProvider(
    IPostgreSqlReadinessExecutor readinessExecutor,
    IPostgreSqlAuthenticatedExecutor authenticatedExecutor) : IDatabaseProvider
{
    private static readonly TimeSpan MaximumProbeTimeout = TimeSpan.FromMinutes(5);
    private static readonly ProviderType Type = ProviderType.Parse("postgresql");
    private static readonly HashSet<string> AllowedEndpointKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "port",
        "database",
        "pgIsReadyPath",
        "postgresExecutablePath",
        "sslMode",
    };

    private static readonly IReadOnlyList<ProviderCapability> DeclaredCapabilities =
    [
        new("health.readiness.v1", CapabilityState.Supported, "windows|linux", "implemented"),
        new("health.authenticated.v1", CapabilityState.Supported, "windows|linux", "implemented"),
        new("control.start.v1", CapabilityState.Unsupported, "any", "not-implemented"),
        new("control.stop.v1", CapabilityState.Unsupported, "any", "not-implemented"),
        new("control.restart.v1", CapabilityState.Unsupported, "any", "not-implemented"),
    ];

    /// <inheritdoc />
    public ProviderType ProviderType => Type;

    /// <inheritdoc />
    public string Version => "0.1.0";

    /// <inheritdoc />
    public IReadOnlyList<ProviderCapability> Capabilities => DeclaredCapabilities;

    /// <inheritdoc />
    public ProviderValidationResult ValidateEndpoint(ProviderEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        List<ProviderValidationError> errors = [];
        if (endpoint.ProviderType != Type)
        {
            errors.Add(new("endpoint.provider_mismatch", "providerType", "The endpoint belongs to a different provider."));
        }

        foreach (string key in endpoint.Properties.Keys)
        {
            if (!AllowedEndpointKeys.Contains(key))
            {
                errors.Add(new("endpoint.unknown_property", key, "The endpoint contains an unsupported property."));
            }
        }

        if (!endpoint.TryGetValue("host", out string host) || !IsValidHost(host))
        {
            errors.Add(new("postgresql.host_invalid", "host", "A valid PostgreSQL host or local socket path is required."));
        }

        if (!endpoint.TryGetValue("port", out string portText) ||
            !int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out int port) ||
            port is < 1 or > 65535)
        {
            errors.Add(new("postgresql.port_invalid", "port", "PostgreSQL port must be between 1 and 65535."));
        }

        if (endpoint.TryGetValue("database", out string database) && !IsValidDatabaseName(database))
        {
            errors.Add(new(
                "postgresql.database_invalid",
                "database",
                "Database must be a bounded literal name rather than a connection string."));
        }

        if (endpoint.TryGetValue("pgIsReadyPath", out string executable) &&
            (string.IsNullOrWhiteSpace(executable) || executable.Length > 1024))
        {
            errors.Add(new("postgresql.executable_invalid", "pgIsReadyPath", "The pg_isready path is invalid."));
        }

        if (endpoint.TryGetValue("postgresExecutablePath", out string postgresExecutable) &&
            (string.IsNullOrWhiteSpace(postgresExecutable) || postgresExecutable.Length > 1024 ||
             postgresExecutable.Contains('\0') || !Path.IsPathFullyQualified(postgresExecutable)))
        {
            errors.Add(new("postgresql.postgres_executable_invalid", "postgresExecutablePath",
                "PostgreSQL executable path must be fully qualified when configured."));
        }

        if (endpoint.TryGetValue("sslMode", out string sslMode) &&
            !string.Equals(sslMode, "verify-full", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new(
                "postgresql.ssl_mode_invalid",
                "sslMode",
                "SSL mode must be verify-full so that the server certificate and host name are validated."));
        }

        return errors.Count == 0 ? ProviderValidationResult.Valid : ProviderValidationResult.Invalid([.. errors]);
    }

    /// <inheritdoc />
    public async ValueTask<ProviderProbeResult> ProbeAsync(
        ProviderProbeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(request.Timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.Timeout, MaximumProbeTimeout);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.AttemptNumber, 1);

        ProviderValidationResult validation = ValidateEndpoint(request.Endpoint);
        if (!validation.IsValid)
        {
            throw new ArgumentException("Endpoint must be validated before probing.", nameof(request));
        }

        if (request.MonitoringCredential is not null)
        {
            return await ProbeAuthenticatedAsync(request, cancellationToken).ConfigureAwait(false);
        }

        PostgreSqlReadinessResult result = await readinessExecutor.ExecuteAsync(
            PostgreSqlEndpoint.FromProviderEndpoint(request.Endpoint),
            request.Timeout,
            cancellationToken).ConfigureAwait(false);

        return result.State switch
        {
            PostgreSqlReadinessState.Accepting => CreateResult(
                HealthStatus.Healthy, EvidenceLevel.ProviderReadiness, result, null, []),
            PostgreSqlReadinessState.Rejecting => CreateResult(
                HealthStatus.Degraded,
                EvidenceLevel.ProviderReadiness,
                result,
                Error("postgresql.rejecting_connections", ErrorCategory.Provider, Retryability.Backoff,
                    "PostgreSQL is reachable but is rejecting connections."),
                ["server-not-accepting-connections"]),
            PostgreSqlReadinessState.NoResponse => CreateResult(
                HealthStatus.Unavailable,
                EvidenceFor(result),
                result,
                Error("postgresql.no_response", ErrorCategory.Network, Retryability.Backoff,
                    "PostgreSQL did not respond to the readiness probe."),
                []),
            PostgreSqlReadinessState.InvalidConfiguration => CreateResult(
                HealthStatus.Unknown,
                EvidenceLevel.Unknown,
                result,
                Error(
                    result.ErrorCode ?? "postgresql.probe_invalid",
                    ErrorCategory.Configuration,
                    Retryability.AfterConfigurationChange,
                    "The PostgreSQL readiness probe could not be attempted with the configured endpoint."),
                ["probe-not-attempted"]),
            PostgreSqlReadinessState.TimedOut => CreateResult(
                HealthStatus.Timeout,
                EvidenceFor(result),
                result,
                Error("postgresql.timeout", ErrorCategory.Timeout, Retryability.Backoff,
                    "The PostgreSQL readiness probe exceeded its deadline."),
                []),
            PostgreSqlReadinessState.TransportReachable => CreateResult(
                HealthStatus.Degraded,
                EvidenceLevel.TransportOnly,
                result,
                null,
                ["transport-only-evidence", "postgresql-readiness-unavailable"]),
            _ => throw new InvalidOperationException("Unknown PostgreSQL readiness state."),
        };
    }

    private async ValueTask<ProviderProbeResult> ProbeAuthenticatedAsync(
        ProviderProbeRequest request,
        CancellationToken cancellationToken)
    {
        PostgreSqlAuthenticatedResult result = await authenticatedExecutor.ExecuteAsync(
            PostgreSqlEndpoint.FromProviderEndpoint(request.Endpoint),
            request.MonitoringCredential!,
            request.Timeout,
            cancellationToken).ConfigureAwait(false);

        return result.State switch
        {
            PostgreSqlAuthenticatedState.Healthy => new ProviderProbeResult(
                HealthStatus.Healthy,
                EvidenceLevel.ProviderAuthenticated,
                "npgsql-select-1",
                result.Duration,
                null,
                []),
            PostgreSqlAuthenticatedState.AuthenticationFailed => new ProviderProbeResult(
                HealthStatus.AuthFailed,
                EvidenceLevel.ProviderAuthenticated,
                "npgsql-select-1",
                result.Duration,
                Error("postgresql.authentication_failed", ErrorCategory.Authentication, Retryability.AfterConfigurationChange,
                    "PostgreSQL rejected the monitoring credential."),
                []),
            PostgreSqlAuthenticatedState.Unavailable => new ProviderProbeResult(
                HealthStatus.Unavailable,
                EvidenceLevel.ProviderAuthenticated,
                "npgsql-select-1",
                result.Duration,
                Error("postgresql.authenticated_unavailable", ErrorCategory.Network, Retryability.Backoff,
                    "PostgreSQL was unavailable during the authenticated probe."),
                []),
            PostgreSqlAuthenticatedState.TimedOut => new ProviderProbeResult(
                HealthStatus.Timeout,
                EvidenceLevel.ProviderAuthenticated,
                "npgsql-select-1",
                result.Duration,
                Error("postgresql.authenticated_timeout", ErrorCategory.Timeout, Retryability.Backoff,
                    "The authenticated PostgreSQL probe exceeded its deadline."),
                []),
            PostgreSqlAuthenticatedState.InvalidConfiguration => new ProviderProbeResult(
                HealthStatus.Unknown,
                EvidenceLevel.Unknown,
                "network-egress",
                result.Duration,
                Error(
                    result.ErrorCode ?? "postgresql.probe_invalid",
                    ErrorCategory.Configuration,
                    Retryability.AfterConfigurationChange,
                    "The PostgreSQL authenticated probe could not be attempted with the configured endpoint."),
                ["probe-not-attempted"]),
            PostgreSqlAuthenticatedState.Failed => new ProviderProbeResult(
                HealthStatus.Unknown,
                EvidenceLevel.ProviderAuthenticated,
                "npgsql-select-1",
                result.Duration,
                Error("postgresql.authenticated_probe_failed", ErrorCategory.Provider, Retryability.Unknown,
                    "The authenticated PostgreSQL probe failed without a conclusive health result."),
                []),
            _ => throw new InvalidOperationException("Unknown PostgreSQL authenticated state."),
        };
    }

    private static bool IsValidHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 253)
        {
            return false;
        }

        if (host.StartsWith('/'))
        {
            return !host.Contains('\0');
        }

        return Uri.CheckHostName(host) is not UriHostNameType.Unknown;
    }

    private static bool IsValidDatabaseName(string database) =>
        !string.IsNullOrWhiteSpace(database) &&
        database.Length <= 128 &&
        !database.Any(char.IsControl) &&
        !database.Contains('=') &&
        !database.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !database.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static ProviderProbeResult CreateResult(
        HealthStatus status,
        EvidenceLevel evidenceLevel,
        PostgreSqlReadinessResult result,
        NormalizedError? error,
        IReadOnlyList<string> limitations) =>
        new(status, evidenceLevel, result.Method, result.Duration, error, limitations);

    private static EvidenceLevel EvidenceFor(PostgreSqlReadinessResult result) =>
        string.Equals(result.Method, "tcp", StringComparison.Ordinal)
            ? EvidenceLevel.TransportOnly
            : EvidenceLevel.ProviderReadiness;

    private static NormalizedError Error(
        string code,
        ErrorCategory category,
        Retryability retryability,
        string message) => new(code, category, retryability, message);
}
