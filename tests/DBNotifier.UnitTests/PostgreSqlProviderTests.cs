using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;

namespace DBNotifier.UnitTests;

public sealed class PostgreSqlProviderTests
{
    [Fact]
    public void EndpointValidationIsTypedAndRejectsUnknownProperties()
    {
        PostgreSqlDatabaseProvider provider = new(new StubExecutor(PostgreSqlReadinessState.Accepting));
        ProviderEndpoint endpoint = Endpoint(
            new("host", "localhost"),
            new("port", "5432"),
            new("vendorMode", "invalid"));

        ProviderValidationResult result = provider.ValidateEndpoint(endpoint);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "endpoint.unknown_property");
    }

    [Theory]
    [InlineData(PostgreSqlReadinessState.Accepting, HealthStatus.Healthy, EvidenceLevel.ProviderReadiness)]
    [InlineData(PostgreSqlReadinessState.Rejecting, HealthStatus.Degraded, EvidenceLevel.ProviderReadiness)]
    [InlineData(PostgreSqlReadinessState.NoResponse, HealthStatus.Unavailable, EvidenceLevel.ProviderReadiness)]
    [InlineData(PostgreSqlReadinessState.InvalidConfiguration, HealthStatus.Unknown, EvidenceLevel.Unknown)]
    [InlineData(PostgreSqlReadinessState.TimedOut, HealthStatus.Timeout, EvidenceLevel.ProviderReadiness)]
    [InlineData(PostgreSqlReadinessState.TransportReachable, HealthStatus.Degraded, EvidenceLevel.TransportOnly)]
    public async Task ReadinessStatesMapToCanonicalHealth(
        PostgreSqlReadinessState readinessState,
        HealthStatus expectedStatus,
        EvidenceLevel expectedEvidence)
    {
        PostgreSqlDatabaseProvider provider = new(new StubExecutor(readinessState));

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(Endpoint(), null, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedEvidence, result.EvidenceLevel);
        if (readinessState == PostgreSqlReadinessState.TransportReachable)
        {
            Assert.Contains("transport-only-evidence", result.Limitations);
            Assert.NotEqual(HealthStatus.Healthy, result.Status);
        }
    }

    [Fact]
    public async Task FailedTcpFallbackRemainsTransportEvidence()
    {
        PostgreSqlDatabaseProvider provider = new(new StubExecutor(PostgreSqlReadinessState.NoResponse, "tcp"));

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(Endpoint(), null, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(HealthStatus.Unavailable, result.Status);
        Assert.Equal(EvidenceLevel.TransportOnly, result.EvidenceLevel);
        Assert.Equal("tcp", result.Method);
    }

    [Fact]
    public void NativeProbeUsesArgumentListWithoutShellConcatenation()
    {
        PostgreSqlEndpoint endpoint = new(
            "db.example.org",
            5433,
            "inventory database",
            "C:\\Program Files\\PostgreSQL\\bin\\pg_isready.exe");

        System.Diagnostics.ProcessStartInfo startInfo =
            PostgreSqlReadinessExecutor.CreateStartInfo(endpoint, TimeSpan.FromMilliseconds(1500));

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(endpoint.PgIsReadyPath, startInfo.FileName);
        Assert.Equal(["-h", "db.example.org", "-p", "5433", "-t", "2", "-d", "inventory database"],
            startInfo.ArgumentList);
    }

    [Fact]
    public void CapabilitiesDoNotClaimAdministrativeControl()
    {
        PostgreSqlDatabaseProvider provider = new(new StubExecutor(PostgreSqlReadinessState.Accepting));

        Assert.Contains(provider.Capabilities,
            capability => capability.CapabilityId == "health.readiness.v1" &&
                          capability.State == CapabilityState.Supported);
        Assert.All(
            provider.Capabilities.Where(capability => capability.CapabilityId.StartsWith("control.", StringComparison.Ordinal)),
            capability => Assert.Equal(CapabilityState.Unsupported, capability.State));
    }

    private static ProviderEndpoint Endpoint(params KeyValuePair<string, string>[] properties)
    {
        KeyValuePair<string, string>[] effectiveProperties = properties.Length == 0
            ? [new("host", "localhost"), new("port", "5432")]
            : properties;
        return new ProviderEndpoint(ProviderType.Parse("postgresql"), effectiveProperties);
    }

    private sealed class StubExecutor(PostgreSqlReadinessState state, string method = "fixture") : IPostgreSqlReadinessExecutor
    {
        public ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PostgreSqlReadinessResult(state, method, TimeSpan.FromMilliseconds(12)));
    }
}
