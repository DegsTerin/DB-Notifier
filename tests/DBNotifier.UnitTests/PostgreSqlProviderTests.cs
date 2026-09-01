// Module purpose: Verifies PostgreSQL provider boundaries and protects the documented project contract.
using System.Diagnostics;
using System.Globalization;
using System.Net;
using DBNotifier.Application.Security;
using DBNotifier.Domain;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;

namespace DBNotifier.UnitTests;

public sealed class PostgreSqlProviderTests
{
    /// <summary>Bounds only the synthetic Windows process fixture startup under concurrent runner load.</summary>
    private static readonly TimeSpan SyntheticProcessStartupTimeout = TimeSpan.FromSeconds(15);

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
    [InlineData("host=other.example port=5433")]
    [InlineData("postgresql://other.example/inventory")]
    [InlineData("postgres://other.example/inventory")]
    public void EndpointValidationRejectsConnectionStringsInDatabaseName(string database)
    {
        PostgreSqlDatabaseProvider provider = CreateProvider(PostgreSqlReadinessState.Accepting);

        ProviderValidationResult result = provider.ValidateEndpoint(Endpoint(
            new("host", "localhost"),
            new("port", "5432"),
            new("database", database)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "postgresql.database_invalid");
    }

    [Fact]
    public void EndpointValidationRejectsTlsWithoutServerIdentityValidation()
    {
        PostgreSqlDatabaseProvider provider = CreateProvider(PostgreSqlReadinessState.Accepting);

        ProviderValidationResult result = provider.ValidateEndpoint(Endpoint(
            new("host", "localhost"),
            new("port", "5432"),
            new("sslMode", "require")));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "postgresql.ssl_mode_invalid");
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
            "verify-full");

        System.Diagnostics.ProcessStartInfo startInfo =
            PostgreSqlReadinessExecutor.CreateStartInfo(endpoint, TimeSpan.FromMilliseconds(1500));

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(endpoint.PgIsReadyPath, startInfo.FileName);
        Assert.Equal(["-h", "db.example.org", "-p", "5433", "-t", "2"],
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
    public void DiscoveryRejectsConfiguredExecutableOutsideApprovedRoots()
    {
        const string configured = "C:\\Untrusted\\pg_isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), [configured]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            configured,
            null,
            ["C:\\Program Files\\PostgreSQL\\bin"],
            ["C:\\Program Files\\PostgreSQL"]));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.NotFound, result.State);
    }

    [Fact]
    public void DiscoveryRejectsWrongExecutableNameInsideApprovedRoot()
    {
        const string configured = "C:\\Program Files\\PostgreSQL\\bin\\not-pg-isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), [configured]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            configured,
            null,
            ["C:\\Program Files\\PostgreSQL\\bin"],
            []));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.NotFound, result.State);
    }

    [Fact]
    public void DiscoveryIgnoresPathDirectoryOutsideTrustedInstallationRoots()
    {
        const string candidate = "C:\\User Tools\\pg_isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), [candidate]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            "pg_isready.exe",
            null,
            ["C:\\User Tools"],
            ["C:\\Program Files\\PostgreSQL"]));

        Assert.Equal(PostgreSqlExecutableDiscoveryState.NotFound, result.State);
    }

    [Fact]
    public void RuntimeDiscoveryRootsIgnoreMutableProgramFilesEnvironmentVariables()
    {
        const string untrustedRoot = "C:\\DBNotifier-Untrusted-ProgramFiles";
        string? originalProgramFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        string? originalProgramFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        try
        {
            Environment.SetEnvironmentVariable("ProgramFiles", untrustedRoot);
            Environment.SetEnvironmentVariable("ProgramFiles(x86)", untrustedRoot);

            PostgreSqlExecutableDiscoveryRequest request = PostgreSqlExecutableDiscovery.CreateRuntimeRequest(
                PostgreSqlEndpoint.FromProviderEndpoint(Endpoint()));

            Assert.DoesNotContain(
                request.InstallationRoots,
                root => root.StartsWith(untrustedRoot, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ProgramFiles", originalProgramFiles);
            Environment.SetEnvironmentVariable("ProgramFiles(x86)", originalProgramFilesX86);
        }
    }

    [Fact]
    public void DiscoveryUsesConfiguredPostgresExecutableSiblingBeforeInstallationRoots()
    {
        const string postgres = "C:\\Custom PostgreSQL\\bin\\postgres.exe";
        const string expected = "C:\\Custom PostgreSQL\\bin\\pg_isready.exe";
        PostgreSqlExecutableDiscovery discovery = new(new DiscoveryFileSystem(
            new Dictionary<string, IReadOnlyList<string>>(), [expected]));

        PostgreSqlExecutableDiscoveryResult result = discovery.Resolve(new(
            "pg_isready.exe", postgres, ["C:\\Custom PostgreSQL\\bin"], ["C:\\Custom PostgreSQL"]));

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
            new StubTransportProbe(transportState),
            new StubNetworkEgressAuthorizer());

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
            new StubDiscovery(PostgreSqlExecutableDiscoveryState.Invalid),
            transport,
            new StubNetworkEgressAuthorizer());

        PostgreSqlReadinessResult result = await executor.ExecuteAsync(
            PostgreSqlEndpoint.FromProviderEndpoint(Endpoint()), TimeSpan.FromSeconds(2), CancellationToken.None);

        Assert.Equal(PostgreSqlReadinessState.InvalidConfiguration, result.State);
        Assert.Equal(0, transport.CallCount);
    }

    /// <summary>
    /// Verifies that timeout and caller cancellation terminate a synthetic readiness process tree without creating
    /// a visible console window.
    /// </summary>
    /// <param name="callerCancels">Whether cancellation, rather than the bounded timeout, initiates termination.</param>
    /// <returns>A task that completes after both synthetic processes have exited.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimeoutAndCancellationTerminateSyntheticReadinessProcessTree(bool callerCancels)
    {
        string systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string powershell = Path.Combine(systemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        string ping = Path.Combine(systemDirectory, "ping.exe");
        Assert.True(File.Exists(powershell));
        Assert.True(File.Exists(ping));
        ProcessStartInfo startInfo = new()
        {
            FileName = powershell,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(
            $"$start=[System.Diagnostics.ProcessStartInfo]::new(); $start.FileName='{ping}'; " +
            "$start.Arguments='127.0.0.1 -t'; $start.UseShellExecute=$false; $start.CreateNoWindow=$true; " +
            "$start.WindowStyle=[System.Diagnostics.ProcessWindowStyle]::Hidden; " +
            "$start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true; " +
            "$child=[System.Diagnostics.Process]::Start($start); " +
            "[Console]::Out.WriteLine($child.Id); [Console]::Out.Flush(); $child.WaitForExit()");
        using Process root = Process.Start(startInfo)!;
        Process? child = null;
        try
        {
            string? childLine = await root.StandardOutput.ReadLineAsync().WaitAsync(SyntheticProcessStartupTimeout);
            Assert.True(int.TryParse(childLine, NumberStyles.None, CultureInfo.InvariantCulture, out int childId));
            child = Process.GetProcessById(childId);
            root.Refresh();
            child.Refresh();
            Assert.Equal(IntPtr.Zero, root.MainWindowHandle);
            Assert.Equal(IntPtr.Zero, child.MainWindowHandle);

            using CancellationTokenSource caller = new();
            if (callerCancels)
            {
                caller.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await PostgreSqlReadinessExecutor.WaitForExitOrTerminateAsync(
                        root,
                        TimeSpan.FromSeconds(5),
                        caller.Token));
            }
            else
            {
                bool timedOut = await PostgreSqlReadinessExecutor.WaitForExitOrTerminateAsync(
                    root,
                    TimeSpan.FromMilliseconds(50),
                    caller.Token);
                Assert.True(timedOut);
            }

            using CancellationTokenSource exitDeadline = new(TimeSpan.FromSeconds(5));
            await child.WaitForExitAsync(exitDeadline.Token);
            Assert.True(root.HasExited);
            Assert.True(child.HasExited);
        }
        finally
        {
            if (!root.HasExited)
            {
                root.Kill(entireProcessTree: true);
                await root.WaitForExitAsync();
            }

            if (child is not null)
            {
                if (!child.HasExited)
                {
                    child.Kill();
                    await child.WaitForExitAsync();
                }

                child.Dispose();
            }
        }
    }

    [Theory]
    [InlineData(PostgreSqlAuthenticatedState.Healthy, HealthStatus.Healthy, EvidenceLevel.ProviderAuthenticated)]
    [InlineData(PostgreSqlAuthenticatedState.AuthenticationFailed, HealthStatus.AuthFailed, EvidenceLevel.ProviderAuthenticated)]
    [InlineData(PostgreSqlAuthenticatedState.Unavailable, HealthStatus.Unavailable, EvidenceLevel.ProviderAuthenticated)]
    [InlineData(PostgreSqlAuthenticatedState.TimedOut, HealthStatus.Timeout, EvidenceLevel.ProviderAuthenticated)]
    [InlineData(PostgreSqlAuthenticatedState.InvalidConfiguration, HealthStatus.Unknown, EvidenceLevel.Unknown)]
    [InlineData(PostgreSqlAuthenticatedState.Failed, HealthStatus.Unknown, EvidenceLevel.ProviderAuthenticated)]
    public async Task AuthenticatedProbeMapsWithoutExposingCredential(
        PostgreSqlAuthenticatedState authenticatedState,
        HealthStatus expectedStatus,
        EvidenceLevel expectedEvidence)
    {
        PostgreSqlDatabaseProvider provider = new(
            new StubExecutor(PostgreSqlReadinessState.Accepting),
            new StubAuthenticatedExecutor(authenticatedState));
        using ProviderCredentialLease credential = new("monitor", "not-serialized".AsSpan());

        ProviderProbeResult result = await provider.ProbeAsync(
            new ProviderProbeRequest(Endpoint(), credential, TimeSpan.FromSeconds(2), 1),
            CancellationToken.None);

        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedEvidence, result.EvidenceLevel);
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

        /// <inheritdoc />
        public ValueTask<PostgreSqlTransportResult> ProbeAsync(
            PostgreSqlEndpoint endpoint,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(new PostgreSqlTransportResult(state));
        }
    }

    /// <summary>Approves one deterministic loopback address without performing DNS or opening a socket.</summary>
    private sealed class StubNetworkEgressAuthorizer : INetworkEgressAuthorizer
    {
        /// <inheritdoc />
        public ValueTask<NetworkEgressResolution> ResolveAndAuthoriseAsync(
            NetworkEgressRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(NetworkEgressResolution.Approved([IPAddress.Loopback]));
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
