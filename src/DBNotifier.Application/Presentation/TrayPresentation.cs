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

/// <summary>Identifies whether the Windows client may run an explicitly requested local notification-validation scenario.</summary>
public enum TrayNotificationValidationMode
{
    /// <summary>Uses only the normal immutable demonstration fixture and its factual freshness transitions.</summary>
    Disabled,

    /// <summary>Publishes the bounded deterministic transition matrix instead of normal fixture-change notifications.</summary>
    TransitionMatrix,
}

/// <summary>Resolves the explicit command-line opt-in for the local notification-transition validation matrix.</summary>
public static class TrayNotificationValidationPolicy
{
    private const string TransitionMatrixArgument = "--review-notification-transitions";

    /// <summary>Returns the transition matrix only when the exact validation-only switch is present.</summary>
    /// <param name="arguments">Command-line arguments supplied to the Windows client.</param>
    /// <returns>The explicitly selected validation mode; unknown or absent arguments fail safely to Disabled.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static TrayNotificationValidationMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(argument => string.Equals(
            argument,
            TransitionMatrixArgument,
            StringComparison.OrdinalIgnoreCase))
            ? TrayNotificationValidationMode.TransitionMatrix
            : TrayNotificationValidationMode.Disabled;
    }
}

/// <summary>Identifies an isolated desktop accessibility review that must not change normal demonstration data.</summary>
public enum DesktopAccessibilityReviewMode
{
    /// <summary>Uses the normal desktop control geometry.</summary>
    Disabled,

    /// <summary>Constrains the scenario selector popup so its existing items exercise the native vertical scrollbar.</summary>
    ComboBoxOverflow,
}

/// <summary>Resolves explicit command-line opt-in for bounded desktop accessibility review geometry.</summary>
public static class DesktopAccessibilityReviewPolicy
{
    private const string ComboBoxOverflowArgument = "--review-combobox-overflow";

    /// <summary>Returns the isolated ComboBox overflow mode only when its exact review switch is present.</summary>
    /// <param name="arguments">Command-line arguments supplied to the Windows client.</param>
    /// <returns>The explicitly selected review mode; unknown or absent arguments preserve normal geometry.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    public static DesktopAccessibilityReviewMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(argument => string.Equals(
            argument,
            ComboBoxOverflowArgument,
            StringComparison.OrdinalIgnoreCase))
            ? DesktopAccessibilityReviewMode.ComboBoxOverflow
            : DesktopAccessibilityReviewMode.Disabled;
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

    /// <summary>Determines whether hiding the secondary shell should request a fresh local availability confirmation.</summary>
    /// <param name="intent">The user or operating-system window intention that initiated the presentation action.</param>
    /// <returns>True only for an explicit close request; hidden startup, minimise, show and exit do not request a confirmation.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="intent"/> is not recognised.</exception>
    public static bool ShouldRequestAvailabilityConfirmation(TrayWindowIntent intent) => intent switch
    {
        TrayWindowIntent.CloseRequest => true,
        TrayWindowIntent.Minimize or TrayWindowIntent.Show or TrayWindowIntent.Exit => false,
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
    /// <summary>Classifies one freshness-aware instance state for event-specific notification presentation.</summary>
    /// <param name="state">Current provider-neutral instance state after canonical freshness evaluation.</param>
    /// <returns>
    /// Availability or recovery for current Healthy evidence, Warning for current Degraded or Maintenance evidence,
    /// Critical for current failure evidence, and Informational or Unknown for stale, invalid or unknown evidence.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="state"/> is null.</exception>
    public static TrayNotificationMeaning ResolveMeaning(TrayInstanceEffectiveState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Freshness != EvidenceFreshness.Current)
        {
            return TrayNotificationMeaning.InformationalOrUnknown;
        }

        return state.Status switch
        {
            HealthStatus.Healthy => TrayNotificationMeaning.AvailabilityOrRecovery,
            HealthStatus.Degraded or HealthStatus.Maintenance => TrayNotificationMeaning.Warning,
            HealthStatus.Unavailable or HealthStatus.AuthFailed or HealthStatus.Timeout => TrayNotificationMeaning.Critical,
            HealthStatus.Unknown => TrayNotificationMeaning.InformationalOrUnknown,
            _ => TrayNotificationMeaning.InformationalOrUnknown,
        };
    }

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

/// <summary>Identifies which semantic source currently occupies the single Windows notification-area icon slot.</summary>
public enum TrayNotificationIconLeaseState
{
    /// <summary>The icon represents the factual provider-neutral fleet aggregate.</summary>
    Aggregate,

    /// <summary>The icon temporarily represents the meaning of a notification being displayed.</summary>
    NotificationMeaning,
}

/// <summary>Identifies bounded lifecycle signals for a temporary notification-meaning icon lease.</summary>
public enum TrayNotificationIconLeaseSignal
{
    /// <summary>Begins the best-effort notification source lease immediately before delivery.</summary>
    Begin,

    /// <summary>Ends the lease after Windows reports that the balloon was displayed.</summary>
    BalloonShown,

    /// <summary>Ends the lease when the bounded adapter fallback elapses.</summary>
    FallbackElapsed,

    /// <summary>Ends the lease after synchronous notification delivery fails.</summary>
    DeliveryFailed,

    /// <summary>Ends the lease while the notification-area adapter is being disposed.</summary>
    Disposed,
}

/// <summary>Constrains the temporary notification-meaning icon exception without coupling Application to Windows APIs.</summary>
public static class TrayNotificationIconLeasePolicy
{
    /// <summary>Gets the maximum best-effort interval before the platform adapter restores the factual aggregate icon.</summary>
    /// <remarks>The interval bounds a temporary visual exception; it is not a notification-delivery timeout.</remarks>
    public static TimeSpan FallbackDelay { get; } = TimeSpan.FromSeconds(2);

    /// <summary>Resolves the next lease state while failing invalid state or signal values safely to the aggregate icon.</summary>
    /// <param name="state">Current icon lease state owned by the platform adapter.</param>
    /// <param name="signal">Lifecycle signal observed by the adapter.</param>
    /// <returns>The temporary notification state only for a valid begin signal; otherwise the factual aggregate state.</returns>
    public static TrayNotificationIconLeaseState Resolve(
        TrayNotificationIconLeaseState state,
        TrayNotificationIconLeaseSignal signal)
    {
        bool currentStateIsValid = Enum.IsDefined(state);
        return currentStateIsValid && signal == TrayNotificationIconLeaseSignal.Begin
            ? TrayNotificationIconLeaseState.NotificationMeaning
            : TrayNotificationIconLeaseState.Aggregate;
    }
}

/// <summary>Represents one provider-neutral instance state after canonical freshness evaluation.</summary>
/// <param name="InstanceId">Stable identifier of the database instance represented by the state.</param>
/// <param name="Status">Provider-neutral health status reported by the captured evidence.</param>
/// <param name="Freshness">Canonical freshness classification evaluated for the capture instant.</param>
public sealed record TrayInstanceEffectiveState(
    Guid InstanceId,
    HealthStatus Status,
    EvidenceFreshness Freshness);

/// <summary>Describes one comparable instance whose effective status or freshness changed between captures.</summary>
/// <param name="InstanceId">Stable identifier shared by the previous and current effective states.</param>
/// <param name="Previous">Effective state captured before the change.</param>
/// <param name="Current">Effective state captured after the change.</param>
public sealed record TrayInstanceStateChange(
    Guid InstanceId,
    TrayInstanceEffectiveState Previous,
    TrayInstanceEffectiveState Current);

/// <summary>Describes one ordered, validation-only notification transition that never represents external evidence.</summary>
/// <param name="Sequence">One-based position used to identify the case during a bounded human review.</param>
/// <param name="Change">Provider-neutral transition supplied to the existing notification presentation path.</param>
public sealed record TrayNotificationTransitionValidationCase(
    int Sequence,
    TrayInstanceStateChange Change);

/// <summary>
/// Owns the deterministic validation-only matrix requested for recovery and every departure from current Healthy evidence.
/// The matrix does not schedule, deliver or persist notifications and is not part of the normal operational fixture.
/// </summary>
public static class TrayNotificationTransitionValidationMatrix
{
    private static readonly IReadOnlyList<TrayNotificationTransitionValidationCase> Matrix = Array.AsReadOnly(
    [
        CreateCase(1, "00000000-0000-0000-0000-000000000101", HealthStatus.Healthy, EvidenceFreshness.Stale, HealthStatus.Healthy, EvidenceFreshness.Current),
        CreateCase(2, "00000000-0000-0000-0000-000000000102", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Degraded, EvidenceFreshness.Current),
        CreateCase(3, "00000000-0000-0000-0000-000000000103", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Unavailable, EvidenceFreshness.Current),
        CreateCase(4, "00000000-0000-0000-0000-000000000104", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.AuthFailed, EvidenceFreshness.Current),
        CreateCase(5, "00000000-0000-0000-0000-000000000105", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Timeout, EvidenceFreshness.Current),
        CreateCase(6, "00000000-0000-0000-0000-000000000106", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Maintenance, EvidenceFreshness.Current),
        CreateCase(7, "00000000-0000-0000-0000-000000000107", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Unknown, EvidenceFreshness.Current),
        CreateCase(8, "00000000-0000-0000-0000-000000000108", HealthStatus.Healthy, EvidenceFreshness.Current, HealthStatus.Healthy, EvidenceFreshness.Stale),
    ]);

    /// <summary>Gets the immutable ordered matrix without starting timers, platform registration or delivery.</summary>
    public static IReadOnlyList<TrayNotificationTransitionValidationCase> Cases => Matrix;

    /// <summary>Creates one stable case whose prior and current states share the same validation-only identifier.</summary>
    /// <param name="sequence">One-based matrix position.</param>
    /// <param name="instanceId">Stable non-operational identifier reserved for this validation case.</param>
    /// <param name="previousStatus">Provider-neutral status before the validation transition.</param>
    /// <param name="previousFreshness">Canonical freshness before the validation transition.</param>
    /// <param name="currentStatus">Provider-neutral status after the validation transition.</param>
    /// <param name="currentFreshness">Canonical freshness after the validation transition.</param>
    /// <returns>A deterministic transition case suitable for pure tests or an explicitly authorised local review.</returns>
    private static TrayNotificationTransitionValidationCase CreateCase(
        int sequence,
        string instanceId,
        HealthStatus previousStatus,
        EvidenceFreshness previousFreshness,
        HealthStatus currentStatus,
        EvidenceFreshness currentFreshness)
    {
        Guid id = Guid.Parse(instanceId);
        return new(
            sequence,
            new(
                id,
                new(id, previousStatus, previousFreshness),
                new(id, currentStatus, currentFreshness)));
    }
}

/// <summary>Captures and compares provider-neutral instance states without delivering notifications or inferring aggregate changes.</summary>
public static class TrayInstanceStateChangePolicy
{
    /// <summary>Captures one deterministic effective state per inventory item from immutable evidence.</summary>
    /// <param name="snapshot">Inventory snapshot containing stable instance identifiers and provider-neutral evidence.</param>
    /// <param name="now">Current UTC instant used only for canonical freshness evaluation.</param>
    /// <param name="staleAfter">Strictly positive maximum age for current evidence.</param>
    /// <returns>Effective states ordered by instance identifier for deterministic comparison and presentation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public static IReadOnlyList<TrayInstanceEffectiveState> Capture(
        InventorySnapshot snapshot,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);

        return (snapshot.Items ?? [])
            .Where(item => item.Enabled)
            .Select(item => new TrayInstanceEffectiveState(
                item.InstanceId,
                item.Status,
                item.GetFreshness(now, staleAfter)))
            .OrderBy(state => state.InstanceId)
            .ToArray();
    }

    /// <summary>Returns every individual effective-state change shared by two captures.</summary>
    /// <param name="previous">Prior effective-state baseline, or null when no comparable capture exists.</param>
    /// <param name="current">Current effective-state capture.</param>
    /// <returns>
    /// Changes ordered by instance identifier. An absent baseline and instances without a state in both captures produce no change.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="current"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either non-null capture repeats an instance identifier.</exception>
    /// <remarks>
    /// The policy compares health status and freshness directly, so it preserves simultaneous changes and changes that leave the fleet aggregate unchanged.
    /// It does not deliver notifications or treat inventory membership alone as a status transition.
    /// </remarks>
    public static IReadOnlyList<TrayInstanceStateChange> DetectChanges(
        IEnumerable<TrayInstanceEffectiveState>? previous,
        IEnumerable<TrayInstanceEffectiveState> current)
    {
        ArgumentNullException.ThrowIfNull(current);
        Dictionary<Guid, TrayInstanceEffectiveState> currentByInstance = current.ToDictionary(state => state.InstanceId);

        if (previous is null)
        {
            return [];
        }

        Dictionary<Guid, TrayInstanceEffectiveState> previousByInstance = previous.ToDictionary(state => state.InstanceId);

        return currentByInstance.Values
            .Where(currentState =>
                previousByInstance.TryGetValue(currentState.InstanceId, out TrayInstanceEffectiveState? previousState) &&
                previousState != currentState)
            .OrderBy(currentState => currentState.InstanceId)
            .Select(currentState => new TrayInstanceStateChange(
                currentState.InstanceId,
                previousByInstance[currentState.InstanceId],
                currentState))
            .ToArray();
    }
}

/// <summary>Supplies the minimum factual evidence needed to classify one instance for the Tray.</summary>
/// <param name="Status">The provider-neutral health status.</param>
/// <param name="IsStale">Whether freshness policy prevents the status from being treated as current.</param>
/// <param name="Enabled">Whether the instance participates in current-health conclusions.</param>
public sealed record TrayFleetSignal(HealthStatus Status, bool IsStale, bool Enabled = true);

/// <summary>Contains provider-neutral fleet counts and their aggregate notification-area state.</summary>
/// <param name="State">The aggregate state selected by severity precedence.</param>
/// <param name="TotalCount">The number of classified instance signals.</param>
/// <param name="HealthyCount">The number of current healthy signals.</param>
/// <param name="WarningCount">The number of current warning signals.</param>
/// <param name="CriticalCount">The number of current critical signals.</param>
/// <param name="UnknownCount">The number of stale or explicitly unknown signals.</param>
/// <param name="DisabledCount">The number of signals explicitly excluded from current-health conclusions.</param>
public sealed record TrayFleetSummary(
    TrayAggregateState State,
    int TotalCount,
    int HealthyCount,
    int WarningCount,
    int CriticalCount,
    int UnknownCount,
    int DisabledCount = 0);

/// <summary>Classifies fleet evidence for compact notification-area presentation without provider-specific branches.</summary>
public static class TrayFleetPresentationPolicy
{
    /// <summary>Creates a fleet summary directly from immutable inventory evidence and the canonical freshness policy.</summary>
    /// <param name="snapshot">Inventory snapshot whose timestamps and provider-neutral statuses are evaluated.</param>
    /// <param name="now">Current UTC instant used only to evaluate evidence freshness.</param>
    /// <param name="staleAfter">Strictly positive maximum age for current evidence.</param>
    /// <returns>Counts and aggregate state derived from the same evidence used by detailed inventory presentation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="staleAfter"/> is not positive.</exception>
    public static TrayFleetSummary Summarise(InventorySnapshot snapshot, DateTimeOffset now, TimeSpan staleAfter)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);
        return Summarise((snapshot.Items ?? []).Select(item => new TrayFleetSignal(
            item.Status,
            item.GetFreshness(now, staleAfter) != EvidenceFreshness.Current,
            item.Enabled)));
    }

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
        int disabled = 0;

        foreach (TrayFleetSignal signal in signals)
        {
            if (!signal.Enabled)
            {
                disabled++;
                continue;
            }

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

        int enabledTotal = healthy + warning + critical + unknown;
        int total = enabledTotal + disabled;
        TrayAggregateState state = critical > 0
            ? TrayAggregateState.Critical
            : warning > 0
                ? TrayAggregateState.Warning
                : unknown > 0 || enabledTotal == 0
                    ? TrayAggregateState.Unknown
                    : TrayAggregateState.Healthy;

        return new(state, total, healthy, warning, critical, unknown, disabled);
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
