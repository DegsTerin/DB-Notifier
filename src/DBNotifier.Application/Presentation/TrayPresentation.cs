// Module purpose: Defines provider-neutral Tray Presentation behaviour without depending on concrete providers, delivery channels or user interfaces.
using DBNotifier.Domain;

namespace DBNotifier.Application.Presentation;

/// <summary>Identifies whether the Windows client starts in its normal notification-area mode or an explicit desktop-review mode.</summary>
public enum TrayStartupMode
{
    /// <summary>Starts only the notification-area controller and keeps the secondary desktop shell hidden.</summary>
    NotificationArea,

    /// <summary>Shows the secondary desktop shell for a bounded development or accessibility review.</summary>
    ShowDesktop,
}

/// <summary>Resolves the bounded command-line override used by development and accessibility audits.</summary>
public static class TrayStartupPolicy
{
    /// <summary>Returns notification-area-first startup unless the exact desktop-review switch is present.</summary>
    /// <param name="arguments">Command-line arguments supplied to the Windows client.</param>
    /// <returns>The safe startup mode; unknown arguments do not make the desktop visible.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static TrayStartupMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(argument => string.Equals(argument, "--show-desktop", StringComparison.OrdinalIgnoreCase))
            ? TrayStartupMode.ShowDesktop
            : TrayStartupMode.NotificationArea;
    }
}

/// <summary>Identifies a user or operating-system request affecting a tray-owned desktop window.</summary>
public enum TrayWindowIntent
{
    /// <summary>Requests that the visible window be minimised.</summary>
    Minimize,

    /// <summary>Requests that the visible window be closed without exiting the notification-area client.</summary>
    CloseRequest,

    /// <summary>Requests that the secondary desktop window be shown and activated.</summary>
    Show,

    /// <summary>Requests an explicit application exit.</summary>
    Exit,
}

/// <summary>Identifies the safe presentation action selected for a tray-owned desktop window.</summary>
public enum TrayWindowAction
{
    /// <summary>Hides the window while preserving the notification-area client.</summary>
    HideToTray,

    /// <summary>Shows and activates the secondary desktop window.</summary>
    ShowAndActivate,

    /// <summary>Terminates the notification-area client after an explicit exit request.</summary>
    ExitApplication,
}

/// <summary>Maps window intentions to safe notification-area-first presentation actions.</summary>
public static class TrayPresentationPolicy
{
    /// <summary>Resolves the presentation action for a supported tray-owned window intention.</summary>
    /// <param name="intent">The window intention to resolve.</param>
    /// <returns>The safe presentation action associated with <paramref name="intent"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="intent"/> is not recognised.</exception>
    public static TrayWindowAction Resolve(TrayWindowIntent intent) => intent switch
    {
        TrayWindowIntent.Minimize or TrayWindowIntent.CloseRequest => TrayWindowAction.HideToTray,
        TrayWindowIntent.Show => TrayWindowAction.ShowAndActivate,
        TrayWindowIntent.Exit => TrayWindowAction.ExitApplication,
        _ => throw new ArgumentOutOfRangeException(nameof(intent)),
    };
}

/// <summary>Provides provider-neutral status, freshness and support text for the compact tray surface.</summary>
/// <param name="StatusLabel">The canonical user-facing status label.</param>
/// <param name="FreshnessLabel">The factual user-facing freshness label.</param>
/// <param name="SupportLabel">The factual capability or support label.</param>
/// <param name="UsesExternalData">Whether the presentation is backed by an authorised external data source.</param>
public sealed record TrayStatusPresentation(
    string StatusLabel,
    string FreshnessLabel,
    string SupportLabel,
    bool UsesExternalData);

/// <summary>Identifies the aggregate state exposed by the notification-area presentation.</summary>
public enum TrayAggregateState
{
    /// <summary>All current, classified observations are healthy.</summary>
    Healthy,

    /// <summary>At least one current observation requires attention without proving a critical failure.</summary>
    Warning,

    /// <summary>At least one current observation proves a critical availability, authentication or timeout failure.</summary>
    Critical,

    /// <summary>No current conclusion can be made because data is absent, stale or explicitly unknown.</summary>
    Unknown,
}

/// <summary>Identifies the provider-neutral meaning conveyed by one Windows notification.</summary>
public enum TrayNotificationMeaning
{
    /// <summary>Confirms application availability or reports that an observed target has recovered.</summary>
    AvailabilityOrRecovery,

    /// <summary>Reports a degraded condition that requires attention without proving a critical failure.</summary>
    Warning,

    /// <summary>Reports a proved critical availability, authentication or timeout failure.</summary>
    Critical,

    /// <summary>Reports information or an unknown condition that must not imply health.</summary>
    InformationalOrUnknown,
}

/// <summary>Maps notification meaning to the shared semantic icon family without delivering a notification.</summary>
public static class TrayNotificationPresentationPolicy
{
    /// <summary>Returns the safe icon state for the meaning conveyed by one notification.</summary>
    /// <param name="meaning">Provider-neutral notification meaning selected by the owning presentation adapter.</param>
    /// <returns>Green for availability or recovery, yellow for warning, red for critical, and neutral grey otherwise.</returns>
    /// <remarks>Future delivery adapters must also identify the affected instance in text; colour alone is never sufficient evidence.</remarks>
    public static TrayAggregateState ResolveIconState(TrayNotificationMeaning meaning) => meaning switch
    {
        TrayNotificationMeaning.AvailabilityOrRecovery => TrayAggregateState.Healthy,
        TrayNotificationMeaning.Warning => TrayAggregateState.Warning,
        TrayNotificationMeaning.Critical => TrayAggregateState.Critical,
        TrayNotificationMeaning.InformationalOrUnknown => TrayAggregateState.Unknown,
        _ => TrayAggregateState.Unknown,
    };
}

/// <summary>Supplies the minimum factual evidence needed to classify one instance for the Tray.</summary>
/// <param name="Status">The provider-neutral health status.</param>
/// <param name="IsStale">Whether freshness policy prevents the status from being treated as current.</param>
public sealed record TrayFleetSignal(HealthStatus Status, bool IsStale);

/// <summary>Contains provider-neutral fleet counts and their aggregate notification-area state.</summary>
/// <param name="State">The aggregate state selected by severity precedence.</param>
/// <param name="TotalCount">The number of classified instance signals.</param>
/// <param name="HealthyCount">The number of current healthy signals.</param>
/// <param name="WarningCount">The number of current warning signals.</param>
/// <param name="CriticalCount">The number of current critical signals.</param>
/// <param name="UnknownCount">The number of stale or explicitly unknown signals.</param>
public sealed record TrayFleetSummary(
    TrayAggregateState State,
    int TotalCount,
    int HealthyCount,
    int WarningCount,
    int CriticalCount,
    int UnknownCount);

/// <summary>Classifies fleet evidence for compact notification-area presentation without provider-specific branches.</summary>
public static class TrayFleetPresentationPolicy
{
    /// <summary>Creates a deterministic fleet summary while preventing stale evidence from being reported as healthy.</summary>
    /// <param name="signals">The bounded set of provider-neutral instance signals to classify.</param>
    /// <returns>Counts and aggregate state using critical, warning, unknown and healthy precedence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="signals"/> is null.</exception>
    public static TrayFleetSummary Summarise(IEnumerable<TrayFleetSignal> signals)
    {
        ArgumentNullException.ThrowIfNull(signals);

        int healthy = 0;
        int warning = 0;
        int critical = 0;
        int unknown = 0;

        foreach (TrayFleetSignal signal in signals)
        {
            switch (Classify(signal))
            {
                case TrayAggregateState.Healthy:
                    healthy++;
                    break;
                case TrayAggregateState.Warning:
                    warning++;
                    break;
                case TrayAggregateState.Critical:
                    critical++;
                    break;
                case TrayAggregateState.Unknown:
                    unknown++;
                    break;
            }
        }

        int total = healthy + warning + critical + unknown;
        TrayAggregateState state = critical > 0
            ? TrayAggregateState.Critical
            : warning > 0
                ? TrayAggregateState.Warning
                : unknown > 0 || total == 0
                    ? TrayAggregateState.Unknown
                    : TrayAggregateState.Healthy;

        return new(state, total, healthy, warning, critical, unknown);
    }

    /// <summary>Determines whether a later lifecycle phase should deliver a change notification.</summary>
    /// <param name="previous">The prior reconciled fleet summary, or null for the initial snapshot.</param>
    /// <param name="current">The current reconciled fleet summary.</param>
    /// <param name="notificationsEnabled">Whether the user has explicitly enabled change notifications.</param>
    /// <returns>True only for an enabled, non-initial and materially changed summary.</returns>
    /// <remarks>This policy does not deliver a Windows notification; delivery remains an integration responsibility.</remarks>
    public static bool ShouldNotify(
        TrayFleetSummary? previous,
        TrayFleetSummary current,
        bool notificationsEnabled) =>
        notificationsEnabled && previous is not null && previous != current;

    /// <summary>Maps one freshness-aware domain status to its compact aggregate class.</summary>
    /// <param name="signal">The signal to classify.</param>
    /// <returns>The provider-neutral aggregate class.</returns>
    private static TrayAggregateState Classify(TrayFleetSignal signal)
    {
        if (signal.IsStale)
        {
            return TrayAggregateState.Unknown;
        }

        return signal.Status switch
        {
            HealthStatus.Healthy => TrayAggregateState.Healthy,
            HealthStatus.Degraded or HealthStatus.Maintenance => TrayAggregateState.Warning,
            HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout => TrayAggregateState.Critical,
            HealthStatus.Unknown => TrayAggregateState.Unknown,
            _ => TrayAggregateState.Unknown,
        };
    }
}
