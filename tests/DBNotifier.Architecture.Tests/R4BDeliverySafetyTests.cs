// Module purpose: Guards R4-B PostgreSQL ownership evidence from activating ordinary external delivery.
namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the exact local-only ownership lab and normal-composition containment.</summary>
public sealed class R4BDeliverySafetyTests
{
    /// <summary>Confirms production composition retains disabled settings and unavailable external boundaries.</summary>
    [Fact]
    public void NormalCompositionKeepsExternalDeliveryUnavailable()
    {
        string program = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string worker = Read("src", "DBNotifier.Server.Api", "ServerMaintenanceWorker.cs");
        string settings = Read("src", "DBNotifier.Server.Api", "appsettings.json");

        Assert.Contains("UnavailableServerMessagePublisher", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddTransient<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.Contains("server.delivery_durable_lease_unavailable", worker, StringComparison.Ordinal);
        Assert.Contains("\"ServerOutboxEnabled\": false", settings, StringComparison.Ordinal);
        Assert.Contains("\"NotificationDeliveryEnabled\": false", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("DBNOTIFIER_R4B_POSTGRESQL", program, StringComparison.Ordinal);
        Assert.DoesNotContain("DBNOTIFIER_R4B_POSTGRESQL", worker, StringComparison.Ordinal);
    }

    /// <summary>Confirms PostgreSQL ownership uses database time, skip-locked claims and explicit ambiguity.</summary>
    [Fact]
    public void OwnershipStoreContainsRequiredPostgreSqlConcurrencyPrimitives()
    {
        string store = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "PostgreSqlDeliveryOwnershipStore.cs");
        string contracts = Read(
            "src",
            "DBNotifier.Application",
            "Operations",
            "MaintenanceAndDelivery.cs");

        Assert.Contains("FOR UPDATE SKIP LOCKED", store, StringComparison.Ordinal);
        Assert.Contains("clock_timestamp()", store, StringComparison.Ordinal);
        Assert.Contains("lease_fence = target.lease_fence + 1", store, StringComparison.Ordinal);
        Assert.Contains("delivery.handoff_outcome_ambiguous", store, StringComparison.Ordinal);
        Assert.Contains("lease_owner_id = @owner_id", store, StringComparison.Ordinal);
        Assert.Contains("IdempotencyKey", contracts, StringComparison.Ordinal);
        Assert.Contains("DeliveryDisposition.Ambiguous", contracts, StringComparison.Ordinal);
    }

    /// <summary>Confirms the disposable runner refuses downloads and binds PostgreSQL only to loopback.</summary>
    [Fact]
    public void PostgreSqlLabIsExplicitPinnedAndLocalOnly()
    {
        string runner = Read("scripts", "run-r4b-postgresql-delivery-lab.ps1");

        Assert.Contains("#Requires -Version 7.0", runner, StringComparison.Ordinal);
        Assert.Contains("--pull', 'never", runner, StringComparison.Ordinal);
        Assert.Contains("127.0.0.1:0:5432", runner, StringComparison.Ordinal);
        Assert.Contains("com.db-notifier.r4b=true", runner, StringComparison.Ordinal);
        Assert.Contains("DBNOTIFIER_R4B_POSTGRESQL_LAB', 'local-test", runner, StringComparison.Ordinal);
        Assert.DoesNotContain("docker pull", runner, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(params string[] path) => File.ReadAllText(
        Path.Combine([RepositoryRoot(), .. path]));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               (!File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")) ||
                !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ??
            throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
