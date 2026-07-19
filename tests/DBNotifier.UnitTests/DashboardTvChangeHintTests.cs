// Module purpose: Verifies the exact Dashboard TV hint contract, activation guards and ephemeral session revocation without opening a runtime.
using DBNotifier.Application.Presentation;
using DBNotifier.Server.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DBNotifier.UnitTests;

public sealed class DashboardTvChangeHintTests
{
    [Theory]
    [InlineData("Production", true, true)]
    [InlineData(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName, false, true)]
    [InlineData(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName, true, false)]
    public void RegistrationFailsClosedUnlessEverySandboxGuardMatches(
        string environmentName,
        bool snapshotEnabled,
        bool hintsEnabled)
    {
        ServiceCollection services = new();
        IConfiguration configuration = Configuration(snapshotEnabled, hintsEnabled);

        bool registered = services.AddDashboardTvChangeHintSandbox(
            new TestHostEnvironment(environmentName),
            configuration);

        Assert.False(registered);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(DashboardTvSignalRSandboxSessionStore));
    }

    [Fact]
    public void ContractRejectsSurplusVersionAndRevisionAmbiguity()
    {
        DashboardTvChangeHint valid = new(
            DashboardTvChangeHintContract.CurrentSchemaVersion,
            $"sha256-{new string('a', 64)}");

        Assert.True(DashboardTvChangeHintContract.IsValid(valid));
        Assert.False(DashboardTvChangeHintContract.IsValid(valid with { SchemaVersion = "dashboard-tv-change-hint.v2" }));
        Assert.False(DashboardTvChangeHintContract.IsValid(valid with { ProjectionRevision = $"sha256-{new string('A', 64)}" }));
        Assert.False(DashboardTvChangeHintContract.IsValid(valid with { ProjectionRevision = "sha256-short" }));
    }

    [Fact]
    public void SessionReplacementRevocationAndExpiryFailClosed()
    {
        MutableTimeProvider time = new(new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        DashboardTvSignalRSandboxSessionStore sessions = new(time);

        Assert.True(sessions.TryIssue("local-reviewer", out string first, out DateTimeOffset firstExpiry));
        Assert.True(sessions.TryValidate(first, out string firstSubject));
        Assert.Equal("local-reviewer", firstSubject);
        Assert.Equal(time.GetUtcNow().Add(DashboardTvSignalRSandboxSessionStore.SessionLifetime), firstExpiry);

        Assert.True(sessions.TryIssue("local-reviewer", out string replacement, out _));
        Assert.NotEqual(first, replacement);
        Assert.False(sessions.TryValidate(first, out _));
        Assert.True(sessions.TryValidate(replacement, out _));

        sessions.Revoke(replacement);
        sessions.Revoke(replacement);
        Assert.False(sessions.TryValidate(replacement, out _));

        Assert.True(sessions.TryIssue("expiring-reviewer", out string expiring, out _));
        time.Advance(DashboardTvSignalRSandboxSessionStore.SessionLifetime);
        Assert.False(sessions.TryValidate(expiring, out _));
    }

    /// <summary>Creates exact in-memory sandbox configuration without an external source.</summary>
    /// <param name="snapshotEnabled">Whether the authoritative snapshot guard is active.</param>
    /// <param name="hintsEnabled">Whether the independent hint guard is active.</param>
    /// <returns>Immutable in-memory configuration.</returns>
    private static IConfiguration Configuration(bool snapshotEnabled, bool hintsEnabled) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = snapshotEnabled.ToString(),
            [$"{DashboardTvChangeHintSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = hintsEnabled.ToString(),
        }).Build();

    /// <summary>Provides a deterministic, explicitly advanced UTC clock for expiry tests.</summary>
    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => current;

        /// <summary>Advances the test-only clock by one non-negative interval.</summary>
        /// <param name="interval">Amount added to the current instant.</param>
        public void Advance(TimeSpan interval) => current = current.Add(interval);
    }

    /// <summary>Provides only the host values needed by registration guard tests.</summary>
    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        /// <inheritdoc />
        public string EnvironmentName { get; set; } = environmentName;

        /// <inheritdoc />
        public string ApplicationName { get; set; } = "DBNotifier.UnitTests";

        /// <inheritdoc />
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        /// <inheritdoc />
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
