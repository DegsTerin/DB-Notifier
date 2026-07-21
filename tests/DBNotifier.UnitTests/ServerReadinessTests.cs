// Module purpose: Verifies separate bounded and sanitised Server liveness and readiness contracts.
using System.Diagnostics;
using DBNotifier.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DBNotifier.UnitTests;

public sealed class ServerReadinessTests
{
    private const string ValidConfiguration = "Host=loopback.invalid;Database=fixture";

    [Fact]
    public async Task ReadinessRejectsMissingConfigurationWithoutCallingDatabase()
    {
        StubReadinessDatabase database = new(new ServerReadinessDatabaseResult(true, true));
        ServerReadinessProbe probe = new(Configuration(null), database);

        ServerReadinessResult result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("server.readiness.configuration_invalid", result.Code);
        Assert.Equal(0, database.CallCount);
    }

    [Theory]
    [InlineData("Host=;Database=fixture")]
    [InlineData("Host=loopback.invalid;Database=")]
    public async Task ReadinessRejectsEmptyRequiredPostgreSqlValues(string connectionString)
    {
        StubReadinessDatabase database = new(new ServerReadinessDatabaseResult(true, true));
        ServerReadinessProbe probe = new(Configuration(connectionString), database);

        ServerReadinessResult result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("server.readiness.configuration_invalid", result.Code);
        Assert.Equal(0, database.CallCount);
    }

    [Theory]
    [InlineData(false, false, "server.readiness.database_unavailable")]
    [InlineData(true, false, "server.readiness.schema_incompatible")]
    [InlineData(true, true, null)]
    public async Task ReadinessDistinguishesDatabaseAndSchemaFacts(
        bool connected,
        bool schemaCompatible,
        string? expectedCode)
    {
        ServerReadinessProbe probe = new(
            Configuration(ValidConfiguration),
            new StubReadinessDatabase(new ServerReadinessDatabaseResult(connected, schemaCompatible)));

        ServerReadinessResult result = await probe.CheckAsync(CancellationToken.None);

        Assert.Equal(expectedCode is null, result.IsReady);
        Assert.Equal(expectedCode, result.Code);
    }

    [Fact]
    public async Task ReadinessBoundsBlockedDatabaseCheckAndSanitisesResult()
    {
        ServerReadinessProbe probe = new(Configuration(ValidConfiguration), new BlockingReadinessDatabase());

        ServerReadinessResult result = await probe.CheckAsync(CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal("server.readiness.deadline_exceeded", result.Code);
    }

    [Fact]
    public async Task LivenessDoesNotConsultReadinessDependencyAndReadyReturns503()
    {
        StubReadinessDatabase database = new(new ServerReadinessDatabaseResult(false, false));
        ServiceCollection services = new();
        services.AddLogging();
        services.AddRouting();
        services.AddSingleton(new DiagnosticListener("DBNotifier.UnitTests.ServerReadiness"));
        services.AddSingleton<IConfiguration>(Configuration(ValidConfiguration));
        services.AddSingleton<IServerReadinessDatabase>(database);
        services.AddSingleton<ServerReadinessProbe>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        ApplicationBuilder application = new(provider);
        application.UseRouting();
        application.UseEndpoints(endpoints => endpoints.MapServerHealthEndpoints());
        RequestDelegate pipeline = application.Build();

        DefaultHttpContext live = Context(provider, "/health/live");
        await pipeline(live);
        Assert.Equal(StatusCodes.Status200OK, live.Response.StatusCode);
        Assert.Equal(0, database.CallCount);

        DefaultHttpContext ready = Context(provider, "/health/ready");
        await pipeline(ready);
        ready.Response.Body.Position = 0;
        string body = await new StreamReader(ready.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ready.Response.StatusCode);
        Assert.Equal("no-store", ready.Response.Headers.CacheControl);
        Assert.Contains("server.readiness.database_unavailable", body, StringComparison.Ordinal);
        Assert.DoesNotContain("loopback.invalid", body, StringComparison.Ordinal);
    }

    private static IConfiguration Configuration(string? connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(connectionString is null
                ? null
                : new Dictionary<string, string?> { ["ConnectionStrings:ServerDatabase"] = connectionString })
            .Build();

    private static DefaultHttpContext Context(IServiceProvider services, string path)
    {
        DefaultHttpContext context = new() { RequestServices = services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class StubReadinessDatabase(ServerReadinessDatabaseResult result) : IServerReadinessDatabase
    {
        public int CallCount { get; private set; }

        public ValueTask<ServerReadinessDatabaseResult> CheckAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class BlockingReadinessDatabase : IServerReadinessDatabase
    {
        private readonly TaskCompletionSource<ServerReadinessDatabaseResult> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<ServerReadinessDatabaseResult> CheckAsync(CancellationToken cancellationToken) =>
            new(completion.Task);
    }
}
