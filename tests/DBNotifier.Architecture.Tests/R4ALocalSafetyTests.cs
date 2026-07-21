// Module purpose: Guards R4-A routing, health separation and disabled external delivery without implementing R4-B concurrency.
namespace DBNotifier.Architecture.Tests;

/// <summary>Protects the local-only R4-A boundaries from accidental external activation or inferred routing.</summary>
public sealed class R4ALocalSafetyTests
{
    /// <summary>Confirms the normal API exposes separate health routes while retaining unavailable delivery adapters.</summary>
    [Fact]
    public void NormalServerCompositionKeepsExternalDeliveryUnavailable()
    {
        string program = Read("src", "DBNotifier.Server.Api", "Program.cs");
        string worker = Read("src", "DBNotifier.Server.Api", "ServerMaintenanceWorker.cs");

        Assert.Contains("MapServerHealthEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("serverOperationsOptions.ValidateForStartup();", program, StringComparison.Ordinal);
        Assert.True(
            program.IndexOf("serverOperationsOptions.ValidateForStartup();", StringComparison.Ordinal) <
            program.IndexOf("builder.WebHost.ConfigureKestrel", StringComparison.Ordinal));
        Assert.Contains("UnavailableServerMessagePublisher", program, StringComparison.Ordinal);
        Assert.Contains("server.delivery_durable_lease_unavailable", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSingleton<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddTransient<INotificationChannelAdapter", program, StringComparison.Ordinal);
        Assert.Contains("UnavailableAgentCertificateIssuer", program, StringComparison.Ordinal);
    }

    /// <summary>Confirms pending creation requires exact durable binding provenance and no all-channel fan-out.</summary>
    [Fact]
    public void ObservationRoutingRequiresExplicitEventBindingIdentity()
    {
        string ingestion = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "ServerObservationIngestionStore.cs");
        string model = Read(
            "src",
            "DBNotifier.Persistence.Server.PostgreSql",
            "ServerDbContext.cs");

        Assert.Contains("AlertRuleChannelBindingId", ingestion, StringComparison.Ordinal);
        Assert.Contains("DeliveryIdempotencyKey(eventId, binding.AlertRuleChannelBindingId)", ingestion, StringComparison.Ordinal);
        Assert.Contains("binding.Environment, instanceEnvironment", ingestion, StringComparison.Ordinal);
        Assert.DoesNotContain("channelIds.Select", ingestion, StringComparison.Ordinal);
        Assert.Contains("ck_delivery_pending_provenance", model, StringComparison.Ordinal);
        Assert.Contains("Quarantined", model, StringComparison.Ordinal);
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
