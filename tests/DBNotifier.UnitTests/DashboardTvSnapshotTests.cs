// Module purpose: Verifies the bounded Dashboard TV snapshot contract and fail-closed sandbox activation guards.
using DBNotifier.Application.Presentation;
using DBNotifier.Server.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace DBNotifier.UnitTests;

public sealed class DashboardTvSnapshotTests
{
    [Fact]
    public void DeterministicSandboxFixturePassesTheCanonicalContract()
    {
        DateTimeOffset now = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
        DashboardTvSandboxSnapshotSource source = new(new FixedTimeProvider(now));

        bool valid = DashboardTvSnapshotValidator.TryValidate(source.Snapshot, now, out string errorCode);

        Assert.True(valid);
        Assert.Empty(errorCode);
        Assert.Equal(DashboardTvSnapshotContract.CurrentSchemaVersion, source.Snapshot.SchemaVersion);
        Assert.Equal(2, source.Snapshot.Items.Count);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName, false)]
    public void SandboxRegistrationFailsClosedUnlessBothGuardsMatch(string environmentName, bool enabled)
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = enabled.ToString(),
            })
            .Build();

        bool registered = services.AddDashboardTvSandbox(
            new TestHostEnvironment(environmentName),
            configuration);

        Assert.False(registered);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(DashboardTvSandboxSnapshotSource));
    }

    /// <summary>Verifies that the normal opted-in sandbox retains the immutable fixture as its only TV source.</summary>
    [Fact]
    public void SandboxRegistrationUsesTheImmutableFixtureByDefault()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DashboardTvSandboxEndpointRouteBuilderExtensions.ConfigurationSection}:Enabled"] = bool.TrueString,
            })
            .Build();

        bool registered = services.AddDashboardTvSandbox(
            new TestHostEnvironment(DashboardTvSandboxEndpointRouteBuilderExtensions.EnvironmentName),
            configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.True(registered);
        IDashboardTvSnapshotSource source = provider.GetRequiredService<IDashboardTvSnapshotSource>();
        Assert.IsType<DashboardTvSandboxSnapshotSource>(source);
        Assert.Same(source, provider.GetRequiredService<IDashboardTvSnapshotSource>());
    }

    [Fact]
    public void ValidationRejectsDuplicateIdentifiersAndFutureEvidence()
    {
        DateTimeOffset now = new(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);
        DashboardTvInventoryItem item = new(
            Guid.Parse("88d74662-18a2-4f08-bc7a-7d61e68a3137"),
            "Sandbox item",
            "provider-neutral",
            "Fixture",
            "Sandbox",
            "Local process",
            "healthy",
            "2026-07-18T12:01:00.000Z",
            "2026-07-18T12:01:01.000Z",
            1,
            true);
        DashboardTvSnapshot snapshot = new(
            DashboardTvSnapshotContract.CurrentSchemaVersion,
            "2026-07-18T12:02:00.000Z",
            [item, item]);

        bool valid = DashboardTvSnapshotValidator.TryValidate(snapshot, now, out string errorCode);

        Assert.False(valid);
        Assert.Equal("dashboard_tv.snapshot_invalid", errorCode);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "DBNotifier.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
