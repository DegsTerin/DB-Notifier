// Module purpose: Verifies Postgre Sql Provider Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;

namespace DBNotifier.UnitTests;

public sealed class PostgreSqlProviderTests
{
    [Fact]
    public void EndpointValidationIsTypedAndRejectsUnknownProperties()
    {
        PostgreSqlDatabaseProvider provider = CreateProvider(PostgreSqlReadinessState.Accepting);
        ProviderEndpoint endpoint = Endpoint(
            new("host", "localhost"),
            new("port", "5432"),
            new("vendorMode", "invalid"));

        ProviderValidationResult result = provider.ValidateEndpoint(endpoint);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "endpoint.unknown_property");
        Assert.Equal("verify-full", PostgreSqlEndpoint.FromProviderEndpoint(Endpoint()).SslMode);
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
        PostgreSqlDatabaseProvider provider = CreateProvider(readinessState);

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
        PostgreSqlDatabaseProvider provider = CreateProvider(PostgreSqlReadinessState.NoResponse, "tcp");

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
            "C:\\Program Files\\PostgreSQL\\bin\\pg_isready.exe",
            null,
            "require");

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
        PostgreSqlDatabaseProvider provider = CreateProvider(PostgreSqlReadinessState.Accepting);

        Assert.Contains(provider.Capabilities,
            capability => capability.CapabilityId == "health.readiness.v1" &&
                          capability.State == CapabilityState.Supported);
        Assert.Contains(provider.Capabilities,
            capability => capability.CapabilityId == "health.authenticated.v1" &&
                          capability.State == CapabilityState.Supported);
        Assert.All(
            provider.Capabilities.Where(capability => capability.CapabilityId.StartsWith("control.", StringComparison.Ordinal)),
            capability => Assert.Equal(CapabilityState.Unsupported, capability.State));
    }

    [Fact]
    public void DiscoveryFindsNewestStandardWindowsInstallationWithoutUsingShell()
    {
        const string root = "C:\\Program Files\\PostgreSQL";
        const string version96 = "C:\\Program Files\\PostgreSQL\\9.6";
        const string version17 = "C:\\Program Files\\PostgreSQL\\17";
        const string version18 = "C:\\Program Files\\PostgreSQL\\18";
        const string expected = "C:\\Program Files\\PostgreSQL\\18\\bin\\pg_isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                [root] = [version96, version17, version18],
            },
            [expected]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            "pg_isready.exe", null, [], [root]));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.Found, result.State);
        Assert.Equal(expected, result.ExecutablePath, ignoreCase: true);
    }

    [Fact]
    public void DiscoveryRejectsUnrootedPathTraversalInsteadOfFallingBack()
    {
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), []));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            "..\\pg_isready.exe", null, [], []));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.Invalid, result.State);
    }

    [Fact]
    public void DiscoveryUsesConfiguredPostgresExecutableSiblingBeforeInstallationRoots()
    {
        const string postgres = "C:\\Custom PostgreSQL\\bin\\postgres.exe";
        const string expected = "C:\\Custom PostgreSQL\\bin\\pg_isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), [expected]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            "pg_isready.exe", postgres, [], []));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.Found, result.State);
        Assert.Equal(expected, result.ExecutablePath, ignoreCase: true);
    }

    [Theory]
    [InlineData(PostgreSqlTransportState.NoResponse, PostgreSqlReadinessState.NoResponse)]
    [InlineData(PostgreSqlTransportState.TimedOut, PostgreSqlReadinessState.TimedOut)]
    [InlineData(PostgreSqlTransportState.Reachable, PostgreSqlReadinessState.TransportReachable)]
    public async Task MissingUtilityFallsBackToTypedDnsRefusedOrReachableTransportFixture(
        PostgreSqlTransportState transportState,
        PostgreSqlReadinessState expected)
    {
        PostgreSqlReadinessExecutor executor = new(
            new StubDiscovery(PostgreSqlExecutableDiscoveryState.NotFound),
            new StubTransportProbe(transportState));

        PostgreSqlReadinessResult result = await executor.ExecuteAsync(
            PostgreSqlEndpoint.FromProviderEndpoint(Endpoint()), TimeSpan.FromSeconds(2), CancellationToken.None);

        Assert.Equal(expected, result.State);
        Assert.Equal("tcp", result.Method);
    }

    [Fact]
    public async Task InvalidDiscoveryFailsClosedWithoutTransportProbe()
    {
        StubTransportProbe transport = new(PostgreSqlTransportState.Reachable);
        PostgreSqlReadinessExecutor executor = new(
            new StubDiscovery(PostgreSqlExecutableDiscoveryState.Invalid), transport);

        PostgreSqlReadinessResult result = await executor.ExecuteAsync(
            PostgreSqlEndpoint.FromProviderEndpoint(Endpoint()), TimeSpan.FromSeconds(2), CancellationToken.None);

        Assert.Equal(PostgreSqlReadinessState.InvalidConfiguration, result.State);
        Assert.Equal(0, transport.CallCount);
    }

    [Theory]
    [InlineData(PostgreSqlAuthenticatedState.Healthy, HealthStatus.Healthy)]
    [InlineData(PostgreSqlAuthenticatedState.AuthenticationFailed, HealthStatus.AuthFailed)]
    [InlineData(PostgreSqlAuthenticatedState.Unavailable, HealthStatus.Unavailable)]
    [InlineData(PostgreSqlAuthenticatedState.TimedOut, HealthStatus.Timeout)]
    [InlineData(PostgreSqlAuthenticatedState.Failed, HealthStatus.Unknown)]
    public async Task AuthenticatedProbeMapsWithoutExposingCredential(
        PostgreSqlAuthenticatedState authenticatedState,
        HealthStatus expectedStatus)
    {
        PostgreSqlDatabaseProvider provider = new(
            new StubExecutor(PostgreSqlReadinessState.Accepting),
            new StubAuthenticatedExecutor(authenticatedState));
        using ProviderCredentialLease credential = new("monitor", "not-serialized".AsSpan());

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(Endpoint(), credential, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(EvidenceLevel.ProviderAuthenticated, result.EvidenceLevel);
        Assert.DoesNotContain("not-serialized", result.Error?.SafeMessage ?? string.Empty, StringComparison.Ordinal);
    }

    private static ProviderEndpoint Endpoint(params KeyValuePair<string, string>[] properties)
    {
        KeyValuePair<string, string>[] effectiveProperties = properties.Length == 0
            ? [new("host", "localhost"), new("port", "5432")]
            : properties;
        return new ProviderEndpoint(ProviderType.Parse("postgresql"), effectiveProperties);
    }

    private static PostgreSqlDatabaseProvider CreateProvider(
        PostgreSqlReadinessState state,
        string method = "fixture") =>
        new(new StubExecutor(state, method), new StubAuthenticatedExecutor(PostgreSqlAuthenticatedState.Healthy));

    private sealed class StubExecutor(PostgreSqlReadinessState state, string method = "fixture") : IPostgreSqlReadinessExecutor
    {
        public ValueTask<PostgreSqlReadinessResult> ExecuteAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PostgreSqlReadinessResult(state, method, TimeSpan.FromMilliseconds(12)));
    }

    private sealed class StubAuthenticatedExecutor(PostgreSqlAuthenticatedState state) : IPostgreSqlAuthenticatedExecutor
    {
        public ValueTask<PostgreSqlAuthenticatedResult> ExecuteAsync(
            PostgreSqlEndpoint endpoint,
            IProviderCredential credential,
            TimeSpan timeout,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PostgreSqlAuthenticatedResult(state, TimeSpan.FromMilliseconds(15)));
    }

    private sealed class StubDiscovery(PostgreSqlExecutableDiscoveryState state) : IPostgreSqlExecutableDiscovery
    {
        public PostgreSqlExecutableDiscoveryResult Resolve(PostgreSqlExecutableDiscoveryRequest request) =>
            new(state, state == PostgreSqlExecutableDiscoveryState.Found ? request.ConfiguredPgIsReadyPath : null, "fixture");
    }

    private sealed class StubTransportProbe(PostgreSqlTransportState state) : IPostgreSqlTransportProbe
    {
        public int CallCount { get; private set; }

        public ValueTask<PostgreSqlTransportState> ProbeAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(state);
        }
    }

    private sealed class DiscoveryFileSystem(
        IReadOnlyDictionary<string, IReadOnlyList<string>> directories,
        IReadOnlyCollection<string> files) : IPostgreSqlDiscoveryFileSystem
    {
        public bool IsOrdinaryFile(string path) => files.Contains(path, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> GetDirectories(string path) =>
            directories.TryGetValue(path, out IReadOnlyList<string>? result) ? result : [];
    }
}
