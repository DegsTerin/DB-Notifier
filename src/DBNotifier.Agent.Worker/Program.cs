using DBNotifier.Agent.Worker;
using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Security;
using DBNotifier.Infrastructure.Security;
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
string databasePath = AgentWorkerOptions.ResolveDatabasePath(workerOptions.DatabasePath);
string connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadWriteCreate,
    Cache = SqliteCacheMode.Shared,
}.ToString();

builder.Services.AddSingleton(workerOptions);
builder.Services.AddDbContextFactory<AgentDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(services => new AgentStoreInitializer(
    services.GetRequiredService<IDbContextFactory<AgentDbContext>>(),
    databasePath));
builder.Services.AddSingleton<IMonitoringAssignmentSource, AgentMonitoringAssignmentSource>();
builder.Services.AddSingleton<IHealthObservationSink, AgentObservationOutboxSink>();
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
builder.Services.AddSingleton<ProbeInstanceHandler>();
builder.Services.AddHostedService<AgentMonitoringWorker>();

using IHost host = builder.Build();
await host.RunAsync();
