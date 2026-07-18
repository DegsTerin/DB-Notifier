// Module purpose: Coordinates the localised Windows tray lifecycle without controlling database or operating-system services.
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Maps window intents to notification-area presentation, localises visible labels and owns the bounded semantic-icon lease
/// used while Windows displays a notification. It never controls a database or operating-system service.
/// </summary>
internal sealed class TrayApplicationController : IDisposable
{
    private const int MaximumLegacyNotificationQueueLength = 16;
    private static readonly TimeSpan LegacyNotificationDisplayInterval = TimeSpan.FromSeconds(4);
    private readonly MainWindow window;
    private readonly System.Windows.Application application;
    private readonly DesktopLocalisationService localisation;
    private readonly DesktopDemonstrationEvidence evidence;
    private readonly IReadOnlyList<TrayNotificationTransitionValidationCase> transitionValidationCases;
    private Icon applicationIcon;
    private Icon? notificationMeaningIcon;
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly DispatcherTimer notificationIconRestoreTimer;
    private readonly DispatcherTimer legacyNotificationAdvanceTimer;
    private readonly DispatcherTimer fleetRefreshTimer;
    private readonly WindowsAppNotificationPublisher? appNotificationPublisher;
    private readonly TrayFlyoutWindow flyout;
    private readonly Queue<LegacyNotificationRequest> legacyNotificationQueue = new();
    private TrayFleetSummary fleetSummary;
    private IReadOnlyList<TrayInstanceEffectiveState> instanceStates;
    private bool disposing;
    private bool exiting;
    private bool legacyNotificationInFlight;
    private long legacyNotificationStartedTimestamp;
    private long notificationIconLeaseStartedTimestamp;
    private int nextTransitionValidationCaseIndex;
    private TrayNotificationIconLeaseState notificationIconLeaseState = TrayNotificationIconLeaseState.Aggregate;

    /// <summary>Initialises tray controls from the shared fleet summary and subscribes to window and language state changes.</summary>
    /// <param name="window">Secondary WPF shell controlled by notification-area intents.</param>
    /// <param name="application">Owning WPF application lifecycle.</param>
    /// <param name="localisation">Localisation owner used by Tray text and the flyout.</param>
    /// <param name="providerVisualIdentityPolicy">Theme-aware local provider-logo resolver used only by per-instance rows.</param>
    /// <param name="evidence">Immutable locale-independent evidence shared with the full desktop shell.</param>
    /// <param name="initialSummary">Aggregate evaluated at the evidence creation instant.</param>
    /// <param name="notificationValidationMode">Explicit validation-only mode; invalid values fail safely to normal fixture behaviour.</param>
    public TrayApplicationController(
        MainWindow window,
        System.Windows.Application application,
        DesktopLocalisationService localisation,
        ProviderVisualIdentityPolicy providerVisualIdentityPolicy,
        DesktopDemonstrationEvidence evidence,
        TrayFleetSummary initialSummary,
        TrayNotificationValidationMode notificationValidationMode)
    {
        this.window = window;
        this.application = application;
        this.localisation = localisation;
        this.evidence = evidence;
        transitionValidationCases = notificationValidationMode == TrayNotificationValidationMode.TransitionMatrix
            ? TrayNotificationTransitionValidationMatrix.Cases
            : [];
        fleetSummary = initialSummary;
        instanceStates = evidence.CaptureInstanceStates(evidence.GeneratedAt);
        flyout = new TrayFlyoutWindow(
            localisation,
            providerVisualIdentityPolicy,
            evidence,
            initialSummary,
            ShowView,
            () => Apply(TrayWindowIntent.Exit));
        (applicationIcon, notifyIcon) = CreateNotificationAreaResources(initialSummary.State);
        notificationIconRestoreTimer = new DispatcherTimer
        {
            Interval = TrayNotificationIconLeasePolicy.FallbackDelay,
        };
        notificationIconRestoreTimer.Tick += NotificationIconRestoreTimerTick;
        legacyNotificationAdvanceTimer = new DispatcherTimer
        {
            Interval = LegacyNotificationDisplayInterval,
        };
        legacyNotificationAdvanceTimer.Tick += LegacyNotificationAdvanceTimerTick;
        fleetRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30),
        };
        fleetRefreshTimer.Tick += FleetRefreshTimerTick;
        notifyIcon.MouseClick += NotifyIconMouseClick;
        notifyIcon.BalloonTipClicked += NotifyIconBalloonTipClicked;
        appNotificationPublisher = WindowsAppNotificationPublisher.TryCreate(
            application.Dispatcher,
            () => Apply(TrayWindowIntent.Show));
        window.StateChanged += WindowStateChanged;
        window.Closing += WindowClosing;
        localisation.LanguageChanged += LanguageChanged;
        RefreshText();
        fleetRefreshTimer.Start();
    }

    /// <summary>Releases notification and event resources without changing external process state.</summary>
    public void Dispose()
    {
        disposing = true;
        legacyNotificationQueue.Clear();
        window.StateChanged -= WindowStateChanged;
        window.Closing -= WindowClosing;
        localisation.LanguageChanged -= LanguageChanged;
        notifyIcon.MouseClick -= NotifyIconMouseClick;
        notifyIcon.BalloonTipClicked -= NotifyIconBalloonTipClicked;
        fleetRefreshTimer.Tick -= FleetRefreshTimerTick;
        fleetRefreshTimer.Stop();
        RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.Disposed);
        notificationIconRestoreTimer.Tick -= NotificationIconRestoreTimerTick;
        notificationIconRestoreTimer.Stop();
        legacyNotificationAdvanceTimer.Tick -= LegacyNotificationAdvanceTimerTick;
        legacyNotificationAdvanceTimer.Stop();
        legacyNotificationInFlight = false;
        appNotificationPublisher?.Dispose();
        notifyIcon.Visible = false;
        flyout.CloseForApplicationExit();
        notifyIcon.Dispose();
        notificationMeaningIcon?.Dispose();
        notificationMeaningIcon = null;
        applicationIcon.Dispose();
    }

    /// <summary>Creates the factual aggregate Tray icon and notification-area component as one exception-safe resource set.</summary>
    /// <param name="state">Provider-neutral aggregate used only to select the state-bearing Tray icon.</param>
    /// <returns>The owned aggregate icon frame and configured notification-area component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when Windows reports an invalid native icon metric.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a packaged semantic icon resource is unavailable.</exception>
    private static (Icon ApplicationIcon, Forms.NotifyIcon NotifyIcon) CreateNotificationAreaResources(
        TrayAggregateState state)
    {
        Icon? applicationIcon = null;
        Forms.NotifyIcon? notifyIcon = null;

        try
        {
            applicationIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                state,
                Forms.SystemInformation.SmallIconSize.Width);
            notifyIcon = new Forms.NotifyIcon();
            notifyIcon.Icon = applicationIcon;
            notifyIcon.Visible = true;
            return (applicationIcon, notifyIcon);
        }
        catch
        {
            if (notifyIcon is not null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }

            applicationIcon?.Dispose();
            throw;
        }
    }

    /// <summary>Applies the minimise-to-tray policy when the WPF window is minimised.</summary>
    private void WindowStateChanged(object? sender, EventArgs e)
    {
        if (window.WindowState == WindowState.Minimized) Apply(TrayWindowIntent.Minimize);
    }

    /// <summary>Converts a normal close request to a tray action until explicit application exit.</summary>
    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        e.Cancel = true;
        Apply(TrayWindowIntent.CloseRequest);
    }

    /// <summary>Refreshes tray labels after an interface-language change.</summary>
    private void LanguageChanged(object? sender, EventArgs e)
    {
        RefreshFleetPresentation(TimeProvider.System.GetUtcNow(), advanceTransitionValidation: false);
    }

    /// <summary>Reconciles the shared immutable evidence at the bounded desktop refresh interval.</summary>
    /// <param name="sender">Dispatcher timer that owns no external work.</param>
    /// <param name="e">Timer event metadata.</param>
    private void FleetRefreshTimerTick(object? sender, EventArgs e) =>
        RefreshFleetPresentation(TimeProvider.System.GetUtcNow(), advanceTransitionValidation: true);

    /// <summary>Updates every factual surface, then advances either normal fixture changes or one explicit validation case.</summary>
    /// <param name="evaluatedAt">UTC instant used only to age the immutable evidence.</param>
    /// <param name="advanceTransitionValidation">Whether this timer-owned refresh may advance the validation-only matrix.</param>
    private void RefreshFleetPresentation(DateTimeOffset evaluatedAt, bool advanceTransitionValidation)
    {
        TrayFleetSummary next = evidence.Summarise(evaluatedAt);
        IReadOnlyList<TrayInstanceEffectiveState> nextInstanceStates = evidence.CaptureInstanceStates(evaluatedAt);
        IReadOnlyList<TrayInstanceStateChange> changes = TrayInstanceStateChangePolicy.DetectChanges(
            instanceStates,
            nextInstanceStates);

        // Advance the in-memory baseline before delivery so platform failure or re-entrant UI work cannot replay a transition.
        instanceStates = nextInstanceStates;
        if (next.State != fleetSummary.State)
        {
            ReplaceAggregateIcon(next.State);
        }
        fleetSummary = next;
        RefreshText();
        flyout.RefreshPresentation(evaluatedAt, next);
        window.RefreshOperationalEvidence(evaluatedAt, next.State);
        if (transitionValidationCases.Count == 0)
        {
            PublishDemonstrationStatusChanges(changes);
        }
        else if (advanceTransitionValidation)
        {
            PublishNextTransitionValidationCase();
        }
    }

    /// <summary>Replaces the owned factual Tray icon without disturbing an active notification-meaning lease.</summary>
    /// <param name="state">New provider-neutral fleet state derived from the shared evidence.</param>
    private void ReplaceAggregateIcon(TrayAggregateState state)
    {
        Icon replacement = BrandStatusIconPolicy.LoadWindowsIcon(state, Forms.SystemInformation.SmallIconSize.Width);
        if (notificationIconLeaseState == TrayNotificationIconLeaseState.Aggregate)
        {
            notifyIcon.Icon = replacement;
        }
        Icon previous = applicationIcon;
        applicationIcon = replacement;
        previous.Dispose();
    }

    /// <summary>Updates all tray-visible strings from the generated localisation dictionary.</summary>
    private void RefreshText()
    {
        string aggregateLabel = localisation.Text($"Tray.Aggregate.{fleetSummary.State}");
        notifyIcon.Text = localisation.Text("Tray.TooltipSummary", aggregateLabel);
    }

    /// <summary>Toggles the accessible WPF fleet flyout after the Windows shell reports a complete primary or secondary click.</summary>
    private void NotifyIconMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button is Forms.MouseButtons.Left or Forms.MouseButtons.Right)
        {
            RefreshFleetPresentation(TimeProvider.System.GetUtcNow(), advanceTransitionValidation: false);
            ToggleFlyout();
        }
    }

    /// <summary>Reveals only the safe local secondary shell when the user activates a legacy fallback notification.</summary>
    /// <param name="sender">NotifyIcon that reported the notification activation.</param>
    /// <param name="e">Event metadata supplied by Windows Forms.</param>
    private void NotifyIconBalloonTipClicked(object? sender, EventArgs e)
    {
        Apply(TrayWindowIntent.Show);
    }

    /// <summary>Restores the factual aggregate icon after the monotonic bounded notification-meaning lease.</summary>
    /// <param name="sender">Dispatcher timer that bounds the temporary notification-meaning state.</param>
    /// <param name="e">Timer event metadata.</param>
    private void NotificationIconRestoreTimerTick(object? sender, EventArgs e)
    {
        TimeSpan remaining = TrayNotificationIconLeasePolicy.FallbackDelay -
            Stopwatch.GetElapsedTime(notificationIconLeaseStartedTimestamp);
        if (remaining > TimeSpan.Zero)
        {
            notificationIconRestoreTimer.Interval = remaining;
            return;
        }

        RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal.FallbackElapsed);
    }

    /// <summary>Advances the serialised fallback from its sole lifecycle source after one bounded display interval.</summary>
    /// <param name="sender">Monotonic dispatcher timer bounding one legacy fallback request.</param>
    /// <param name="e">Timer event metadata.</param>
    private void LegacyNotificationAdvanceTimerTick(object? sender, EventArgs e)
    {
        TimeSpan remaining = LegacyNotificationDisplayInterval - Stopwatch.GetElapsedTime(legacyNotificationStartedTimestamp);
        if (remaining > TimeSpan.Zero)
        {
            legacyNotificationAdvanceTimer.Interval = remaining;
            return;
        }

        CompleteLegacyNotification(TrayNotificationIconLeaseSignal.FallbackElapsed);
    }

    /// <summary>Ends the bounded best-effort notification-icon lease and continues any queued local fallback notifications.</summary>
    /// <param name="signal">Lifecycle signal that requires the factual aggregate icon to resume.</param>
    private void RestoreAggregateIconAfterNotificationCapture(TrayNotificationIconLeaseSignal signal)
    {
        if (notificationIconLeaseState != TrayNotificationIconLeaseState.NotificationMeaning)
        {
            notificationIconRestoreTimer.Stop();
            notificationMeaningIcon?.Dispose();
            notificationMeaningIcon = null;
            return;
        }

        notifyIcon.Icon = applicationIcon;
        notificationIconLeaseState = TrayNotificationIconLeasePolicy.Resolve(notificationIconLeaseState, signal);
        notificationIconRestoreTimer.Stop();
        notificationMeaningIcon?.Dispose();
        notificationMeaningIcon = null;
    }

    /// <summary>Completes one bounded fallback request, restores factual Tray state and starts the next queued item.</summary>
    /// <param name="signal">Lease completion signal used to restore the factual aggregate icon.</param>
    private void CompleteLegacyNotification(TrayNotificationIconLeaseSignal signal)
    {
        legacyNotificationAdvanceTimer.Stop();
        RestoreAggregateIconAfterNotificationCapture(signal);
        legacyNotificationInFlight = false;
        TryShowNextLegacyNotification();
    }

    /// <summary>Shows or dismisses the single transient notification-area surface.</summary>
    private void ToggleFlyout()
    {
        if (flyout.IsVisible)
        {
            flyout.Hide();
            return;
        }

        flyout.ShowNearNotificationArea();
    }

    /// <summary>Restores the desktop window and selects one safe read-only destination.</summary>
    private void ShowView(DesktopView view)
    {
        Apply(TrayWindowIntent.Show);
        window.ShowView(view);
    }

    /// <summary>Executes only the local window action resolved by the neutral tray policy.</summary>
    private void Apply(TrayWindowIntent intent)
    {
        switch (TrayPresentationPolicy.Resolve(intent))
        {
            case TrayWindowAction.HideToTray:
                window.Hide();
                if (TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation(intent))
                {
                    ShowCloseToTrayNotification();
                }
                break;
            case TrayWindowAction.ShowAndActivate:
                window.Show();
                window.WindowState = WindowState.Normal;
                window.Activate();
                break;
            case TrayWindowAction.ExitApplication:
                exiting = true;
                notifyIcon.Visible = false;
                window.Close();
                application.Shutdown();
                break;
        }
    }

    /// <summary>Requests one fresh close-to-Tray confirmation through a bounded best-effort green source.</summary>
    private void ShowCloseToTrayNotification()
    {
        string title = localisation.Text("Tray.BalloonTitle");
        string message = localisation.Text("Tray.BalloonMessage");

        // Each explicit close requests a fresh confirmation with an explicit transparent DB Notifier identity asset.
        // Platform acceptance is not proof that Windows displayed the card because Focus Assist and notification policy remain authoritative.
        if (appNotificationPublisher?.TryPublishAvailability(title, message) == true) return;

        QueueLegacyNotification(title, message, TrayNotificationMeaning.AvailabilityOrRecovery);
    }

    /// <summary>Publishes every individual change from the explicitly authorised local demonstration fixture.</summary>
    /// <param name="changes">Stable ordered changes detected after the silent in-memory baseline.</param>
    private void PublishDemonstrationStatusChanges(IReadOnlyList<TrayInstanceStateChange> changes)
    {
        foreach (TrayInstanceStateChange change in changes)
        {
            DesktopDemonstrationItem? item = evidence.FindItem(change.InstanceId);
            if (item is null)
            {
                Trace.TraceWarning("DB Notifier skipped a local demonstration notification whose fixture item was unavailable.");
                continue;
            }

            PublishDemonstrationStatusChange(
                change,
                localisation.Text("Tray.StatusChangeTitle"),
                localisation.Text(item.DisplayNameKey));
        }
    }

    /// <summary>Advances one explicitly selected validation case without replaying normal fixture changes.</summary>
    private void PublishNextTransitionValidationCase()
    {
        if (nextTransitionValidationCaseIndex >= transitionValidationCases.Count) return;

        // Advance before platform delivery so fallback failure, language changes or re-entrant UI work cannot replay the case.
        TrayNotificationTransitionValidationCase validationCase =
            transitionValidationCases[nextTransitionValidationCaseIndex++];
        string displayName = localisation.Text(
            "Tray.TransitionValidationCase",
            validationCase.Sequence,
            transitionValidationCases.Count);
        PublishDemonstrationStatusChange(
            validationCase.Change,
            localisation.Text("Tray.TransitionValidationTitle"),
            displayName);
    }

    /// <summary>Formats and publishes one local demonstration or validation-only transition through the shared bounded adapters.</summary>
    /// <param name="change">Provider-neutral effective-state transition to present.</param>
    /// <param name="title">Localised title that distinguishes the owning demonstration or validation scenario.</param>
    /// <param name="displayName">Localised non-external instance or validation-case label.</param>
    private void PublishDemonstrationStatusChange(
        TrayInstanceStateChange change,
        string title,
        string displayName)
    {
        string message = localisation.Text(
            "Tray.StatusChangeMessage",
            displayName,
            LocaliseEffectiveState(change.Previous),
            LocaliseEffectiveState(change.Current));
        TrayNotificationMeaning meaning = TrayNotificationPresentationPolicy.ResolveMeaning(change.Current);

        if (appNotificationPublisher?.TryPublishDemonstrationStatusChange(
                change.InstanceId,
                title,
                message,
                meaning) == true)
        {
            return;
        }

        QueueLegacyNotification(title, message, meaning);
    }

    /// <summary>Returns a factual localised state label while failing invalid freshness or status safely to Unknown.</summary>
    /// <param name="state">Provider-neutral effective state attached to one detected change.</param>
    /// <returns>Current health label, Stale label or Unknown label as supported by the generated catalogue.</returns>
    private string LocaliseEffectiveState(TrayInstanceEffectiveState state) => state.Freshness switch
    {
        EvidenceFreshness.Current when Enum.IsDefined(state.Status) => localisation.Text($"Status.{state.Status}"),
        EvidenceFreshness.Stale => localisation.Text("Status.Stale"),
        _ => localisation.Text("Status.Unknown"),
    };

    /// <summary>Queues one bounded, event-specific legacy fallback without allowing concurrent balloon replacement.</summary>
    /// <param name="title">Localised title displayed by the Windows notification surface.</param>
    /// <param name="message">Localised factual message containing no secret or external state.</param>
    /// <param name="meaning">Typed meaning used to select the temporary canonical semantic bell.</param>
    private void QueueLegacyNotification(string title, string message, TrayNotificationMeaning meaning)
    {
        if (disposing || exiting) return;
        int pendingNotifications = legacyNotificationQueue.Count + (legacyNotificationInFlight ? 1 : 0);
        if (pendingNotifications >= MaximumLegacyNotificationQueueLength)
        {
            Trace.TraceWarning("DB Notifier local fallback notification queue reached its bounded capacity.");
            return;
        }

        legacyNotificationQueue.Enqueue(new(title, message, meaning));
        TryShowNextLegacyNotification();
    }

    /// <summary>Displays the next queued fallback only after the prior semantic-icon lease has completed.</summary>
    private void TryShowNextLegacyNotification()
    {
        if (disposing || exiting ||
            legacyNotificationInFlight ||
            legacyNotificationQueue.Count == 0)
        {
            return;
        }

        LegacyNotificationRequest request = legacyNotificationQueue.Dequeue();
        legacyNotificationInFlight = true;
        try
        {
            notificationIconRestoreTimer.Stop();
            legacyNotificationAdvanceTimer.Stop();
            notificationMeaningIcon = BrandStatusIconPolicy.LoadWindowsIcon(
                TrayNotificationPresentationPolicy.ResolveIconState(request.Meaning),
                Forms.SystemInformation.SmallIconSize.Width);
            notifyIcon.Icon = notificationMeaningIcon;
            notificationIconLeaseState = TrayNotificationIconLeasePolicy.Resolve(
                notificationIconLeaseState,
                TrayNotificationIconLeaseSignal.Begin);
            notificationIconLeaseStartedTimestamp = Stopwatch.GetTimestamp();
            legacyNotificationStartedTimestamp = notificationIconLeaseStartedTimestamp;
            notificationIconRestoreTimer.Interval = TrayNotificationIconLeasePolicy.FallbackDelay;
            legacyNotificationAdvanceTimer.Interval = LegacyNotificationDisplayInterval;
            notificationIconRestoreTimer.Start();
            legacyNotificationAdvanceTimer.Start();
            notifyIcon.ShowBalloonTip(
                3000,
                request.Title,
                request.Message,
                Forms.ToolTipIcon.None);
        }
        catch
        {
            CompleteLegacyNotification(TrayNotificationIconLeaseSignal.DeliveryFailed);
        }
    }

    /// <summary>Contains one serialised local fallback request and its own semantic notification meaning.</summary>
    /// <param name="Title">Localised notification title.</param>
    /// <param name="Message">Localised factual notification message.</param>
    /// <param name="Meaning">Typed meaning used only for the temporary notification source icon.</param>
    private sealed record LegacyNotificationRequest(
        string Title,
        string Message,
        TrayNotificationMeaning Meaning);
}
