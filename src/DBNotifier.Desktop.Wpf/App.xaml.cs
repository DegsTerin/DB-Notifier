// Module purpose: Initialises the WPF desktop application without controlling database services implicitly.
using System.Windows;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the notification-area-first WPF lifecycle and read-only fleet reconciliation without controlling database
/// services, external infrastructure or administrative capabilities.
/// </summary>
public partial class App : System.Windows.Application, IDisposable
{
    private TrayApplicationController? trayController;
    private DesktopLocalisationService? localisation;
    private DesktopThemeService? theme;
    private DesktopMotionService? motion;
    private ProviderVisualIdentityPolicy? providerVisualIdentityPolicy;

    /// <summary>Loads safe preferences and starts in the notification area with only explicitly requested local review modes enabled.</summary>
    /// <param name="e">Startup arguments; exact desktop, notification-transition, accessibility and W02 review switches remain opt-in.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DesktopUiPreferenceStore preferences = new();
        localisation = new DesktopLocalisationService(this, preferences);
        theme = new DesktopThemeService(this, preferences);
        motion = new DesktopMotionService();
        localisation.Initialise();
        theme.Initialise();
        providerVisualIdentityPolicy = new ProviderVisualIdentityPolicy(theme);
        DateTimeOffset generatedAt = TimeProvider.System.GetUtcNow();
        DesktopDemonstrationEvidence evidence = DesktopDemonstrationEvidence.Create(generatedAt);
        IDesktopFleetSnapshotSource inventorySource = new DesktopDemonstrationInventorySnapshotSource(evidence, localisation);
        DesktopFleetReconciliationCoordinator reconciliation = new(
            inventorySource,
            DesktopDemonstrationEvidence.StaleAfter,
            TimeProvider.System);
        DesktopFleetReconciliationFrame initialFrame = reconciliation
            .ReconcileAsync(DesktopFleetRefreshTrigger.Initial)
            .AsTask()
            .GetAwaiter()
            .GetResult();
        InventorySnapshot initialSnapshot = initialFrame.Snapshot ??
            throw new InvalidOperationException("The local desktop inventory source did not establish an initial snapshot.");
        TrayFlyoutLiveReviewMode flyoutLiveReviewMode = TrayFlyoutLiveReviewPolicy.Resolve(e.Args);
        bool flyoutLiveReviewEnabled =
            flyoutLiveReviewMode == TrayFlyoutLiveReviewMode.BoundedInMemorySequence;
        DesktopAccessibilityReviewMode accessibilityReviewMode = flyoutLiveReviewEnabled
            ? DesktopAccessibilityReviewMode.Disabled
            : DesktopAccessibilityReviewPolicy.Resolve(e.Args);
        MainWindow window = new(
            localisation,
            theme,
            motion,
            providerVisualIdentityPolicy,
            evidence,
            initialSnapshot,
            initialFrame.Summary.State,
            accessibilityReviewMode);
        MainWindow = window;
        TrayNotificationValidationMode notificationValidationMode = flyoutLiveReviewEnabled
            ? TrayNotificationValidationMode.Disabled
            : TrayNotificationValidationPolicy.Resolve(e.Args);
        ReconciledNotificationSandboxActivation? reconciledNotificationActivation = flyoutLiveReviewEnabled
            ? null
            : ReconciledNotificationSandboxActivationPolicy.Resolve(e.Args);
        trayController = new TrayApplicationController(
            window,
            this,
            localisation,
            providerVisualIdentityPolicy,
            evidence,
            reconciliation,
            initialFrame,
            notificationValidationMode,
            reconciledNotificationActivation,
            flyoutLiveReviewMode);
        if (!flyoutLiveReviewEnabled &&
            TrayStartupPolicy.Resolve(e.Args) == TrayStartupMode.ShowDesktop)
        {
            window.Show();
        }
    }

    /// <summary>Releases tray and theme-observation resources when the WPF application exits.</summary>
    /// <param name="e">Framework exit event metadata.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    /// <summary>Releases owned presentation resources without controlling external services.</summary>
    public void Dispose()
    {
        trayController?.Dispose();
        trayController = null;
        providerVisualIdentityPolicy?.Dispose();
        providerVisualIdentityPolicy = null;
        theme?.Dispose();
        theme = null;
        motion?.Dispose();
        motion = null;
        GC.SuppressFinalize(this);
    }
}
