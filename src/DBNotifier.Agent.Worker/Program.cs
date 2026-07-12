using DBNotifier.Application.Monitoring;
using DBNotifier.Application.Security;
using DBNotifier.Infrastructure.Security;
using DBNotifier.Provider.Abstractions;
using DBNotifier.Providers.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IPostgreSqlReadinessExecutor, PostgreSqlReadinessExecutor>();
builder.Services.AddSingleton<IPostgreSqlAuthenticatedExecutor, NpgsqlAuthenticatedExecutor>();
builder.Services.AddSingleton<IDatabaseProvider, PostgreSqlDatabaseProvider>();
builder.Services.AddSingleton<IProviderRegistry>(services =>
    new ProviderRegistry(services.GetServices<IDatabaseProvider>()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICredentialVault, UnavailableCredentialVault>();
builder.Services.AddSingleton<ProbeInstanceHandler>();

using IHost host = builder.Build();
await host.RunAsync();
