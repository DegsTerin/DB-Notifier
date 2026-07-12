using System.Globalization;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;

namespace DBNotifier.Providers.PostgreSql;

public sealed class PostgreSqlDatabaseProvider(IPostgreSqlReadinessExecutor readinessExecutor) : IDatabaseProvider
{
    private static readonly ProviderType Type = ProviderType.Parse("postgresql");
    private static readonly HashSet<string> AllowedEndpointKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "host",
        "port",
        "database",
        "pgIsReadyPath",
    };

    private static readonly IReadOnlyList<ProviderCapability> DeclaredCapabilities =
    [
        new("health.readiness.v1", CapabilityState.Supported, "windows|linux", "implemented"),
        new("health.authenticated.v1", CapabilityState.Unsupported, "any", "not-implemented"),
        new("control.start.v1", CapabilityState.Unsupported, "any", "not-implemented"),
        new("control.stop.v1", CapabilityState.Unsupported, "any", "not-implemented"),
        new("control.restart.v1", CapabilityState.Unsupported, "any", "not-implemented"),
    ];

    public ProviderType ProviderType => Type;

    public string Version => "0.1.0";

    public IReadOnlyList<ProviderCapability> Capabilities => DeclaredCapabilities;

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

        if (endpoint.TryGetValue("database", out string database) && database.Length > 128)
        {
            errors.Add(new("postgresql.database_invalid", "database", "Database name exceeds the supported length."));
        }

        if (endpoint.TryGetValue("pgIsReadyPath", out string executable) &&
            (string.IsNullOrWhiteSpace(executable) || executable.Length > 1024))
        {
            errors.Add(new("postgresql.executable_invalid", "pgIsReadyPath", "The pg_isready path is invalid."));
        }

        return errors.Count == 0 ? ProviderValidationResult.Valid : ProviderValidationResult.Invalid([.. errors]);
    }

    public async ValueTask<ProviderProbeResult> ProbeAsync(
        ProviderProbeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(request.Timeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.AttemptNumber, 1);

        ProviderValidationResult validation = ValidateEndpoint(request.Endpoint);
        if (!validation.IsValid)
        {
            throw new ArgumentException("Endpoint must be validated before probing.", nameof(request));
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
                Error("postgresql.probe_invalid", ErrorCategory.Configuration, Retryability.AfterConfigurationChange,
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
