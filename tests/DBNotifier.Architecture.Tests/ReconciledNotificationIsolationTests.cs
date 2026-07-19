// Module purpose: Guards the disabled-default, loopback-only and read-only boundaries of reconciled local notifications.
namespace DBNotifier.Architecture.Tests;

public sealed class ReconciledNotificationIsolationTests
{
    [Fact]
    public void NormalApiDoesNotRegisterOrMapTheNotificationSandbox()
    {
        string program = Read("src", "DBNotifier.Server.Api", "Program.cs");

        Assert.DoesNotContain("AddReconciledLocalNotificationSandbox", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapReconciledLocalNotificationSandbox", program, StringComparison.Ordinal);
    }

    [Fact]
    public void WpfRequiresExactOptInAndSuppressesFixtureNotificationsWhenActive()
    {
        string runtime = Read("src", "DBNotifier.Desktop.Wpf", "ReconciledNotificationSandboxRuntime.cs");
        string controller = Read("src", "DBNotifier.Desktop.Wpf", "TrayApplicationController.cs");

        Assert.Contains("--reconciled-notification-sandbox", runtime, StringComparison.Ordinal);
        Assert.Contains("--notifications-opt-in", runtime, StringComparison.Ordinal);
        Assert.Contains("IPAddress.IsLoopback", runtime, StringComparison.Ordinal);
        Assert.Contains("IsUnderTemporaryRoot", runtime, StringComparison.Ordinal);
        Assert.Contains("suppressDemonstrationNotifications", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationPipelineContainsNoSignalRCommandOrExternalChannelDependency()
    {
        string combined = string.Join('\n',
            Read("src", "DBNotifier.Application", "Presentation", "ReconciledLocalNotification.cs"),
            Read("src", "DBNotifier.Infrastructure", "Presentation", "ReconciledLocalNotificationAdapters.cs"),
            Read("src", "DBNotifier.Server.Api", "ReconciledLocalNotificationSandbox.cs"),
            Read("src", "DBNotifier.Desktop.Wpf", "ReconciledNotificationSandboxRuntime.cs"));

        Assert.DoesNotContain("SignalR", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StartAsync(", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("StopAsync(", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("AdministrativeCommand", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Smtp", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Webhook", combined, StringComparison.OrdinalIgnoreCase);
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
        return directory?.FullName ?? throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
