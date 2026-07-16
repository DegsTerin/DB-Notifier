// Module purpose: Publishes local Windows app notifications without reading provider state or controlling external services.
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the unpackaged Windows app-notification registration used by the notification-area client.
/// It supplies a stable display name and transparent availability mark, marshals activation to WPF and fails safely when the platform is unavailable.
/// </summary>
internal sealed class WindowsAppNotificationPublisher : IDisposable
{
    private const string DisplayName = "DB Notifier";
    private const uint WindowsAppSdkMajorMinor = 0x00020002;
    private const string WindowsAppSdkVersionTag = "";
    private const string AvailabilityTag = "window-hidden";
    private const string AvailabilityGroup = "local-availability";
    private static readonly PackageVersion MinimumWindowsAppSdkVersion = new(2, 2, 0, 0);
    private static readonly TimeSpan AvailabilityLifetime = TimeSpan.FromMinutes(5);
    private readonly AppNotificationManager manager;
    private readonly Dispatcher dispatcher;
    private readonly Action showDesktop;
    private bool registered;

    /// <summary>Initialises one publisher around the process-wide Windows app-notification manager.</summary>
    /// <param name="manager">Process-wide notification manager supplied by the Windows App SDK.</param>
    /// <param name="dispatcher">Owning WPF dispatcher used for notification activation.</param>
    /// <param name="showDesktop">Safe local callback that reveals the secondary desktop shell.</param>
    private WindowsAppNotificationPublisher(
        AppNotificationManager manager,
        Dispatcher dispatcher,
        Action showDesktop)
    {
        this.manager = manager;
        this.dispatcher = dispatcher;
        this.showDesktop = showDesktop;
    }

    /// <summary>Attempts to register a local unpackaged publisher with an explicit display name and transparent identity asset.</summary>
    /// <param name="dispatcher">Owning WPF dispatcher used for notification activation.</param>
    /// <param name="showDesktop">Safe local callback that reveals the secondary desktop shell.</param>
    /// <returns>A registered publisher when supported and configured; otherwise null so the legacy local fallback remains available.</returns>
    public static WindowsAppNotificationPublisher? TryCreate(Dispatcher dispatcher, Action showDesktop)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(showDesktop);

        try
        {
            if (!Bootstrap.TryInitialize(
                WindowsAppSdkMajorMinor,
                WindowsAppSdkVersionTag,
                MinimumWindowsAppSdkVersion,
                Bootstrap.InitializeOptions.None,
                out _))
            {
                WriteFallbackDiagnostic("runtime-unavailable");
                return null;
            }
        }
        catch (Exception exception)
        {
            // Missing or damaged runtime/bootstrap files must not prevent the notification-area client from starting.
            WriteFallbackDiagnostic("runtime-initialisation-failed", exception);
            return null;
        }

        WindowsAppNotificationPublisher? publisher = null;
        try
        {
            if (!AppNotificationManager.IsSupported())
            {
                ShutdownRuntime("platform-unsupported-shutdown-failed");
                WriteFallbackDiagnostic("platform-unsupported");
                return null;
            }

            string iconPath = Path.Combine(
                AppContext.BaseDirectory,
                "NotificationAssets",
                "DBNotifier.Availability.png");
            if (!File.Exists(iconPath))
            {
                ShutdownRuntime("identity-asset-missing-shutdown-failed");
                WriteFallbackDiagnostic("identity-asset-missing");
                return null;
            }

            AppNotificationManager manager = AppNotificationManager.Default;
            publisher = new(manager, dispatcher, showDesktop);
            manager.NotificationInvoked += publisher.NotificationInvoked;
            manager.Register(DisplayName, new Uri(iconPath));
            publisher.registered = true;
            return publisher;
        }
        catch (Exception exception)
        {
            publisher?.Dispose();
            if (publisher is null) ShutdownRuntime("registration-rollback-failed");
            WriteFallbackDiagnostic("registration-failed", exception);
            return null;
        }
    }

    /// <summary>Attempts to publish one local close-to-Tray availability confirmation.</summary>
    /// <param name="title">Localised title displayed by the Windows notification surface.</param>
    /// <param name="message">Localised factual message confirming that the application remains available.</param>
    /// <returns>True when the platform accepted the publication request; false when the fallback should be attempted.</returns>
    /// <remarks>Acceptance is not proof of visible delivery because Windows policy and focus settings can suppress presentation.</remarks>
    public bool TryPublishAvailability(string title, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (!registered) return false;

        try
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddArgument("action", "show")
                .AddText(title)
                .AddText(message)
                .BuildNotification();
            notification.Tag = AvailabilityTag;
            notification.Group = AvailabilityGroup;
            notification.Expiration = DateTimeOffset.UtcNow.Add(AvailabilityLifetime);
            notification.ExpiresOnReboot = true;
            notification.SuppressDisplay = false;
            manager.Show(notification);
            return true;
        }
        catch (Exception exception)
        {
            WriteFallbackDiagnostic("publication-failed", exception);
            return false;
        }
    }

    /// <summary>Releases process registration and activation resources without removing historical Notification Centre entries.</summary>
    public void Dispose()
    {
        try
        {
            manager.NotificationInvoked -= NotificationInvoked;
            if (registered) manager.Unregister();
        }
        catch (Exception exception)
        {
            // Process shutdown must remain safe when the Windows registration has already been removed externally.
            WriteFallbackDiagnostic("unregistration-failed", exception);
        }
        finally
        {
            registered = false;
            ShutdownRuntime("runtime-shutdown-failed");
        }
    }

    /// <summary>Marshals notification activation to the owning WPF dispatcher and reveals only the local secondary shell.</summary>
    /// <param name="sender">Process-wide notification manager that received the activation.</param>
    /// <param name="args">Activation data supplied by Windows; no external command or provider input is executed.</param>
    private void NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        if (!args.Arguments.TryGetValue("action", out string? action) ||
            !string.Equals(action, "show", StringComparison.Ordinal))
        {
            return;
        }

        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
        try
        {
            _ = dispatcher.BeginInvoke(showDesktop);
        }
        catch (InvalidOperationException)
        {
            // Shutdown may begin between the state check and enqueue; activation is safely ignored in that race.
        }
    }

    /// <summary>Records a sanitised local diagnostic without exposing paths, notification content or external state.</summary>
    /// <param name="stage">Stable non-secret stage that selected the legacy fallback or encountered shutdown drift.</param>
    /// <param name="exception">Optional exception whose type, but not message or stack, identifies the local failure category.</param>
    private static void WriteFallbackDiagnostic(string stage, Exception? exception = null)
    {
        Trace.TraceWarning(
            "DB Notifier Windows app notifications unavailable at {0}; using or retaining the safe local fallback. Failure type: {1}.",
            stage,
            exception?.GetType().Name ?? "none");
    }

    /// <summary>Releases the explicitly initialised Windows App Runtime without allowing local runtime drift to abort shutdown.</summary>
    /// <param name="failureStage">Stable non-secret stage recorded only when runtime shutdown fails.</param>
    private static void ShutdownRuntime(string failureStage)
    {
        try
        {
            Bootstrap.Shutdown();
        }
        catch (Exception exception)
        {
            WriteFallbackDiagnostic(failureStage, exception);
        }
    }
}
