// Module purpose: Guards the W02 flyout review as a disabled-default, in-memory and notification-incapable presentation path.
namespace DBNotifier.Architecture.Tests;

/// <summary>Verifies the W02 composition and source boundaries without starting a WPF runtime.</summary>
public sealed class TrayFlyoutLiveReviewIsolationTests
{
    /// <summary>Ensures normal composition cannot activate W02 without one exact marker.</summary>
    [Fact]
    public void W02RequiresExactOptInAndOverridesConflictingRuntimeModes()
    {
        string application = Read("src", "DBNotifier.Desktop.Wpf", "App.xaml.cs");
        string review = Read("src", "DBNotifier.Desktop.Wpf", "TrayFlyoutLiveReview.cs");

        Assert.Contains("--review-flyout-live-update", review, StringComparison.Ordinal);
        Assert.Contains("StringComparison.OrdinalIgnoreCase", review, StringComparison.Ordinal);
        Assert.Contains("matchCount == 1", review, StringComparison.Ordinal);
        Assert.Contains("TrayFlyoutLiveReviewPolicy.Resolve(e.Args)", application, StringComparison.Ordinal);
        Assert.Contains("? TrayNotificationValidationMode.Disabled", application, StringComparison.Ordinal);
        Assert.Contains("? null", application, StringComparison.Ordinal);
        Assert.Contains("!flyoutLiveReviewEnabled &&", application, StringComparison.Ordinal);
    }

    /// <summary>Ensures W02 owns no persistence, network, command or notification implementation dependency.</summary>
    [Fact]
    public void W02SequenceIsPureAndBounded()
    {
        string review = Read("src", "DBNotifier.Desktop.Wpf", "TrayFlyoutLiveReview.cs");

        Assert.Contains("Array.AsReadOnly", review, StringComparison.Ordinal);
        Assert.Contains("return Frames[nextFrameIndex++]", review, StringComparison.Ordinal);
        Assert.DoesNotContain("System.IO", review, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Net", review, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", review, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowsAppNotificationPublisher", review, StringComparison.Ordinal);
        Assert.DoesNotContain("ReconciledNotification", review, StringComparison.Ordinal);
        Assert.DoesNotContain("AdministrativeCommand", review, StringComparison.Ordinal);
    }

    /// <summary>Ensures the active W02 controller cannot create or enter either Windows notification path.</summary>
    [Fact]
    public void W02CompositionSuppressesEveryNotificationPath()
    {
        string controller = Read("src", "DBNotifier.Desktop.Wpf", "TrayApplicationController.cs");
        string flyout = Read("src", "DBNotifier.Desktop.Wpf", "TrayFlyoutWindow.xaml.cs");

        Assert.Contains("appNotificationPublisher = suppressAllNotifications", controller, StringComparison.Ordinal);
        Assert.Contains("suppressDemonstrationNotifications =", controller, StringComparison.Ordinal);
        Assert.Contains("suppressAllNotifications || reconciledNotificationActivation is not null", controller, StringComparison.Ordinal);
        Assert.Contains("if (!suppressAllNotifications && reconciledNotificationActivation is not null)", controller, StringComparison.Ordinal);
        Assert.Contains("if (suppressAllNotifications)", controller, StringComparison.Ordinal);
        Assert.Contains("if (suppressAllNotifications || disposing || exiting) return false;", controller, StringComparison.Ordinal);
        Assert.Contains("flyoutLiveReviewTimer.Stop()", controller, StringComparison.Ordinal);
        Assert.Contains("TrayFlyoutLiveReviewPolicy.AllowsSecondaryShell(flyoutLiveReviewMode)", controller, StringComparison.Ordinal);
        Assert.Contains("OpenOverviewButton.IsEnabled = secondaryNavigationEnabled", flyout, StringComparison.Ordinal);
        Assert.Contains("OpenConfigurationButton.IsEnabled = secondaryNavigationEnabled", flyout, StringComparison.Ordinal);
        Assert.Contains("OpenHistoryAlertsButton.IsEnabled = secondaryNavigationEnabled", flyout, StringComparison.Ordinal);
        Assert.Contains(
            "RestoreFlyoutLiveReviewBaseline(TimeProvider.System.GetUtcNow())",
            controller,
            StringComparison.Ordinal);

        int restoreStart = controller.IndexOf(
            "private void RestoreFlyoutLiveReviewBaseline",
            StringComparison.Ordinal);
        int nextMethodStart = controller.IndexOf(
            "private void ApplyFlyoutLiveReviewFrame",
            restoreStart,
            StringComparison.Ordinal);
        Assert.True(restoreStart >= 0 && nextMethodStart > restoreStart);
        string restoreMethod = controller[restoreStart..nextMethodStart];
        Assert.Contains("flyout.RefreshPresentation", restoreMethod, StringComparison.Ordinal);
        Assert.DoesNotContain("window.", restoreMethod, StringComparison.Ordinal);
    }

    /// <summary>Ensures one frame drives the aggregate mark, tooltip, visible count and semantic row presentation together.</summary>
    [Fact]
    public void W02FrameUpdatesOneCoherentFlyoutTransaction()
    {
        string controller = Read("src", "DBNotifier.Desktop.Wpf", "TrayApplicationController.cs");
        string flyout = Read("src", "DBNotifier.Desktop.Wpf", "TrayFlyoutWindow.xaml.cs");
        string flyoutMarkup = Read("src", "DBNotifier.Desktop.Wpf", "TrayFlyoutWindow.xaml");

        Assert.Contains("ApplyFlyoutLiveReviewFrame", controller, StringComparison.Ordinal);
        Assert.Contains("flyoutLiveReviewSession.Open()", controller, StringComparison.Ordinal);
        Assert.Contains("flyoutLiveReviewSession.Advance()", controller, StringComparison.Ordinal);
        Assert.Contains("flyoutLiveReviewSession?.Close()", controller, StringComparison.Ordinal);
        Assert.Contains("ReplaceAggregateIcon(frame.Summary.State)", controller, StringComparison.Ordinal);
        Assert.Contains("fleetSummary = frame.Summary", controller, StringComparison.Ordinal);
        Assert.Contains("RefreshText()", controller, StringComparison.Ordinal);
        Assert.Contains("flyout.RefreshReviewPresentation(frame)", controller, StringComparison.Ordinal);
        Assert.Contains("Inventory.DisabledCount", flyout, StringComparison.Ordinal);
        Assert.Contains("ApplyPresentation(frame.Summary, frame.InstanceStates)", flyout, StringComparison.Ordinal);
        Assert.Contains("SemanticIcon[] glyphs", flyout, StringComparison.Ordinal);
        Assert.Contains("Kind=\"Disabled\"", flyoutMarkup, StringComparison.Ordinal);
    }

    /// <summary>Reads one repository file without depending on the caller's working directory.</summary>
    /// <param name="path">Path segments below the repository root.</param>
    /// <returns>The complete UTF-8-compatible source text.</returns>
    private static string Read(params string[] path) => File.ReadAllText(
        Path.Combine([RepositoryRoot(), .. path]));

    /// <summary>Finds the repository root containing the solution and permanent instructions.</summary>
    /// <returns>The absolute repository root.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the test output is not below the repository.</exception>
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
