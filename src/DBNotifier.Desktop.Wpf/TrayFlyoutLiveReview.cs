// Module purpose: Defines the bounded, presentation-only W02 flyout sequence without persistence, delivery or operational integration.
using System.Collections.ObjectModel;
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Identifies whether the W02 flyout-only in-memory review sequence is explicitly enabled.</summary>
internal enum TrayFlyoutLiveReviewMode
{
    /// <summary>Preserves the normal immutable demonstration presentation.</summary>
    Disabled,

    /// <summary>Allows the bounded W02 presentation sequence while the notification-area flyout remains open.</summary>
    BoundedInMemorySequence,
}

/// <summary>Resolves the exact, fail-closed command-line marker for the W02 presentation-only review.</summary>
internal static class TrayFlyoutLiveReviewPolicy
{
    internal const string ReviewArgument = "--review-flyout-live-update";

    /// <summary>Enables W02 only when the exact marker occurs once.</summary>
    /// <param name="arguments">Command-line arguments supplied to the WPF process.</param>
    /// <returns>The bounded review mode for one exact marker; otherwise the disabled default.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="arguments"/> is null.</exception>
    internal static TrayFlyoutLiveReviewMode Resolve(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        int matchCount = arguments.Count(argument => string.Equals(
            argument,
            ReviewArgument,
            StringComparison.OrdinalIgnoreCase));
        return matchCount == 1
            ? TrayFlyoutLiveReviewMode.BoundedInMemorySequence
            : TrayFlyoutLiveReviewMode.Disabled;
    }

    /// <summary>Allows the secondary shell only when W02 is disabled.</summary>
    /// <param name="mode">Resolved W02 mode.</param>
    /// <returns>True only for the ordinary disabled W02 composition; invalid future values fail closed.</returns>
    internal static bool AllowsSecondaryShell(TrayFlyoutLiveReviewMode mode) =>
        mode == TrayFlyoutLiveReviewMode.Disabled;
}

/// <summary>Owns the exact four stable identifiers rendered by the bounded W02 demonstration.</summary>
internal static class TrayFlyoutLiveReviewFixture
{
    /// <summary>Gets the Finance row identifier.</summary>
    internal static Guid FinanceId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Gets the Orders row identifier.</summary>
    internal static Guid OrdersId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000002");

    /// <summary>Gets the Analytics row identifier.</summary>
    internal static Guid AnalyticsId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000003");

    /// <summary>Gets the Catalogue row identifier.</summary>
    internal static Guid CatalogueId { get; } = Guid.Parse("00000000-0000-0000-0000-000000000004");

    /// <summary>Gets the immutable exact set that every coherent W02 frame must contain.</summary>
    internal static ReadOnlyCollection<Guid> InstanceIds { get; } = Array.AsReadOnly(
        [FinanceId, OrdersId, AnalyticsId, CatalogueId]);
}

/// <summary>
/// Describes one flyout row in a synthetic presentation frame while retaining the stable demonstration identity.
/// </summary>
/// <param name="InstanceId">Stable identifier of the existing local demonstration row.</param>
/// <param name="Status">Provider-neutral health status rendered beside the row.</param>
/// <param name="Freshness">Canonical freshness that may fail the visible state safely to Unknown or Stale.</param>
/// <param name="Enabled">Whether the row participates in the current-health aggregate for this in-memory frame.</param>
internal sealed record TrayFlyoutInstancePresentationState(
    Guid InstanceId,
    HealthStatus Status,
    EvidenceFreshness Freshness,
    bool Enabled);

/// <summary>
/// Owns one internally coherent W02 frame whose aggregate and counts are derived from the same ordered row states.
/// </summary>
internal sealed class TrayFlyoutPresentationFrame
{
    /// <summary>Initialises a frame and derives its aggregate without accepting duplicate row identities.</summary>
    /// <param name="instanceStates">Bounded ordered row states belonging to the local demonstration fixture.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="instanceStates"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the frame repeats, omits or invents an instance identifier.</exception>
    internal TrayFlyoutPresentationFrame(IEnumerable<TrayFlyoutInstancePresentationState> instanceStates)
    {
        ArgumentNullException.ThrowIfNull(instanceStates);
        TrayFlyoutInstancePresentationState[] materialisedStates = instanceStates.ToArray();
        if (materialisedStates.Length == 0)
        {
            throw new ArgumentException("A W02 presentation frame must contain at least one row.", nameof(instanceStates));
        }

        if (materialisedStates.Select(state => state.InstanceId).Distinct().Count() != materialisedStates.Length)
        {
            throw new ArgumentException("A W02 presentation frame cannot repeat an instance identifier.", nameof(instanceStates));
        }

        HashSet<Guid> frameIds = materialisedStates.Select(state => state.InstanceId).ToHashSet();
        if (!frameIds.SetEquals(TrayFlyoutLiveReviewFixture.InstanceIds))
        {
            throw new ArgumentException(
                "A W02 presentation frame must contain the exact four demonstration rows.",
                nameof(instanceStates));
        }

        InstanceStates = Array.AsReadOnly(materialisedStates);
        Summary = TrayFleetPresentationPolicy.Summarise(materialisedStates.Select(state => new TrayFleetSignal(
            state.Status,
            state.Freshness != EvidenceFreshness.Current,
            state.Enabled)));
    }

    /// <summary>Gets the immutable ordered row states used by the flyout.</summary>
    internal IReadOnlyList<TrayFlyoutInstancePresentationState> InstanceStates { get; }

    /// <summary>Gets counts and aggregate derived exclusively from <see cref="InstanceStates"/>.</summary>
    internal TrayFleetSummary Summary { get; }

    /// <summary>Finds one state by stable demonstration identifier without inferring a replacement.</summary>
    /// <param name="instanceId">Stable row identifier to find.</param>
    /// <returns>The exact frame state, or null when this malformed frame does not contain the row.</returns>
    internal TrayFlyoutInstancePresentationState? Find(Guid instanceId) =>
        InstanceStates.SingleOrDefault(state => state.InstanceId == instanceId);
}

/// <summary>
/// Supplies three deterministic W02 frames and then stops; it owns no timer, callback, file, network or platform resource.
/// </summary>
internal sealed class TrayFlyoutLiveReviewSequence
{
    private static readonly ReadOnlyCollection<TrayFlyoutPresentationFrame> Frames = Array.AsReadOnly(
    [
        CreateFrame(
            (TrayFlyoutLiveReviewFixture.FinanceId, HealthStatus.Healthy, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.OrdersId, HealthStatus.Degraded, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.AnalyticsId, HealthStatus.Timeout, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.CatalogueId, HealthStatus.Unknown, EvidenceFreshness.Stale, false)),
        CreateFrame(
            (TrayFlyoutLiveReviewFixture.FinanceId, HealthStatus.Healthy, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.OrdersId, HealthStatus.Degraded, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.AnalyticsId, HealthStatus.Unknown, EvidenceFreshness.Current, false),
            (TrayFlyoutLiveReviewFixture.CatalogueId, HealthStatus.Unknown, EvidenceFreshness.Stale, false)),
        CreateFrame(
            (TrayFlyoutLiveReviewFixture.FinanceId, HealthStatus.Healthy, EvidenceFreshness.Stale, true),
            (TrayFlyoutLiveReviewFixture.OrdersId, HealthStatus.Degraded, EvidenceFreshness.Stale, true),
            (TrayFlyoutLiveReviewFixture.AnalyticsId, HealthStatus.Unknown, EvidenceFreshness.Current, true),
            (TrayFlyoutLiveReviewFixture.CatalogueId, HealthStatus.Unknown, EvidenceFreshness.Stale, false)),
    ]);

    private int nextFrameIndex;

    /// <summary>Gets the bounded delay between visible W02 presentation frames.</summary>
    internal static TimeSpan StepInterval { get; } = TimeSpan.FromSeconds(4);

    /// <summary>Restarts the deterministic sequence and returns its first coherent frame.</summary>
    /// <returns>The initial Critical frame used whenever the flyout is newly opened.</returns>
    internal TrayFlyoutPresentationFrame Reset()
    {
        nextFrameIndex = 1;
        return Frames[0];
    }

    /// <summary>Returns the next frame or null after the finite sequence is exhausted.</summary>
    /// <returns>The next coherent frame, or null when no more presentation changes are authorised.</returns>
    internal TrayFlyoutPresentationFrame? Advance()
    {
        if (nextFrameIndex >= Frames.Count)
        {
            return null;
        }

        return Frames[nextFrameIndex++];
    }

    /// <summary>Creates one frame from the four stable demonstration rows without reading any external state.</summary>
    /// <param name="states">Ordered tuples containing identity, status, freshness and enabled state.</param>
    /// <returns>A frame whose aggregate and counts are derived from these exact states.</returns>
    private static TrayFlyoutPresentationFrame CreateFrame(
        params (Guid InstanceId, HealthStatus Status, EvidenceFreshness Freshness, bool Enabled)[] states) =>
        new(states.Select(state => new TrayFlyoutInstancePresentationState(
            state.InstanceId,
            state.Status,
            state.Freshness,
            state.Enabled)));
}

/// <summary>Models the W02 open, bounded-tick and close lifecycle without owning a Dispatcher or platform resource.</summary>
internal sealed class TrayFlyoutLiveReviewSession
{
    private readonly TrayFlyoutLiveReviewSequence sequence = new();

    /// <summary>Gets the current frame while the synthetic flyout session is active.</summary>
    internal TrayFlyoutPresentationFrame? CurrentFrame { get; private set; }

    /// <summary>Restarts W02 for one newly opened flyout.</summary>
    /// <returns>The deterministic initial frame.</returns>
    internal TrayFlyoutPresentationFrame Open()
    {
        CurrentFrame = sequence.Reset();
        return CurrentFrame;
    }

    /// <summary>Advances one frame only while the session is active.</summary>
    /// <returns>The next frame, or null when closed or exhausted.</returns>
    internal TrayFlyoutPresentationFrame? Advance()
    {
        if (CurrentFrame is null)
        {
            return null;
        }

        TrayFlyoutPresentationFrame? next = sequence.Advance();
        if (next is not null)
        {
            CurrentFrame = next;
        }

        return next;
    }

    /// <summary>Ends the synthetic session without retaining a mutable presentation frame.</summary>
    internal void Close() => CurrentFrame = null;
}
