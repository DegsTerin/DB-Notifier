// Module purpose: Implements Program for the opt-in Agent runtime while preserving fail-closed defaults.
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Agent.Worker;
using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Operations;
using DBNotifier.Application.Security;
using DBNotifier.Application.Synchronization;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Infrastructure.Synchronization;
using DBNotifier.Persistence.Agent.Sqlite;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
AgentWorkerOptions workerOptions = new();
builder.Configuration.GetSection(AgentWorkerOptions.SectionName).Bind(workerOptions);
AgentSynchronizationOptions synchronizationOptions = new();
builder.Configuration.GetSection(AgentSynchronizationOptions.SectionName).Bind(synchronizationOptions);
synchronizationOptions.ValidateCommandPollingForStartup();
AgentFleetClientOptions agentFleetClientOptions = new();
builder.Configuration.GetSection(AgentFleetClientOptions.SectionName).Bind(agentFleetClientOptions);
agentFleetClientOptions.ValidateForStartup();
AgentRetentionOptions retentionOptions = new();
builder.Configuration.GetSection(AgentRetentionOptions.SectionName).Bind(retentionOptions);
string databasePath = AgentWorkerOptions.ResolveDatabasePath(workerOptions.DatabasePath);
string connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWriteCreate,
    Cache = SqliteCacheMode.Shared,
}.ToString();

builder.Services.AddSingleton(workerOptions);
builder.Services.AddSingleton(synchronizationOptions);
builder.Services.AddSingleton(agentFleetClientOptions);
builder.Services.AddSingleton(retentionOptions);
builder.Services.AddDbContextFactory<AgentDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(services => new AgentStoreInitializer(
    services.GetRequiredService<IDbContextFactory<AgentDbContext>>(),
    databasePath));
builder.Services.AddSingleton<IMonitoringAssignmentSource, AgentMonitoringAssignmentSource>();
builder.Services.AddSingleton<IHealthObservationSink, AgentObservationOutboxSink>();
builder.Services.AddSingleton<IAgentOutboxStore, AgentOutboxStore>();
builder.Services.AddSingleton<IAgentRetentionStore, AgentRetentionStore>();
builder.Services.AddSingleton<IPostgreSqlDiscoveryFileSystem, PostgreSqlDiscoveryFileSystem>();
builder.Services.AddSingleton<IPostgreSqlExecutableDiscovery, PostgreSqlExecutableDiscovery>();
builder.Services.AddSingleton<IPostgreSqlTransportProbe, TcpPostgreSqlTransportProbe>();
builder.Services.AddSingleton<IPostgreSqlReadinessExecutor, PostgreSqlReadinessExecutor>();
builder.Services.AddSingleton<IPostgreSqlAuthenticatedExecutor, NpgsqlAuthenticatedExecutor>();
builder.Services.AddSingleton<IDatabaseProvider, PostgreSqlDatabaseProvider>();
builder.Services.AddSingleton<IProviderRegistry>(services =>
    new ProviderRegistry(services.GetServices<IDatabaseProvider>()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICredentialVaultAdapter, WindowsCredentialManagerVaultAdapter>();
builder.Services.AddSingleton<ICredentialVaultAdapter, LinuxSecretServiceVaultAdapter>();
builder.Services.AddSingleton<ICredentialVault>(services =>
    new CompositeCredentialVault(services.GetServices<ICredentialVaultAdapter>()));
builder.Services.AddSingleton(_ =>
    new HttpClient(CreateSynchronizationHandler(synchronizationOptions))
    {
        Timeout = TimeSpan.FromSeconds(30),
    });
builder.Services.AddSingleton<IObservationBatchTransport>(services =>
    new HttpObservationBatchTransport(
        services.GetRequiredService<HttpClient>(),
        synchronizationOptions.Enabled
            ? synchronizationOptions.ValidateAndGetServerBaseAddress()
            : new Uri("https://disabled.invalid/", UriKind.Absolute),
        synchronizationOptions.AgentVersion));
builder.Services.AddSingleton<ProbeInstanceHandler>();
builder.Services.AddHostedService<AgentMonitoringWorker>();
builder.Services.AddHostedService<AgentOutboxDispatchWorker>();
builder.Services.AddHostedService<AgentRetentionWorker>();

using IHost host = builder.Build();
await host.RunAsync();

static HttpClientHandler CreateSynchronizationHandler(AgentSynchronizationOptions options)
{
    HttpClientHandler handler = new() { CheckCertificateRevocationList = true };
    if (!options.Enabled)
    {
        return handler;
    }

    _ = options.ValidateAndGetServerBaseAddress();
    using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection certificates = store.Certificates.Find(
        X509FindType.FindByThumbprint,
        options.ClientCertificateThumbprint!,
        validOnly: true);
    X509Certificate2? certificate = certificates
        .OfType<X509Certificate2>()
        .SingleOrDefault(candidate => candidate.HasPrivateKey);
    if (certificate is null)
    {
        throw new InvalidOperationException("The configured Agent client certificate is unavailable or invalid.");
    }

    handler.ClientCertificates.Add(certificate);
    return handler;
}
