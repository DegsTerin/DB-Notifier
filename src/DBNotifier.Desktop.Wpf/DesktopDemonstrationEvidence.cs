// Module purpose: Owns one immutable, locale-independent desktop demonstration snapshot shared by the shell, Tray and flyout.
using DBNotifier.Application.Presentation;
using DBNotifier.Domain;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Stores provider-neutral operational evidence once per application lifetime while allowing visible labels to be localised.
/// Language, theme and navigation changes may rebuild presentation records, but cannot alter timestamps or fleet state.
/// </summary>
internal sealed class DesktopDemonstrationEvidence
{
    /// <summary>Gets the shared maximum age used by every desktop surface before evidence becomes stale.</summary>
    internal static TimeSpan StaleAfter { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Initialises one immutable evidence set at an exact generated instant.</summary>
    /// <param name="generatedAt">UTC instant that owns every relative demonstration timestamp.</param>
    /// <param name="items">Provider-neutral item evidence retained without localised visible text.</param>
    private DesktopDemonstrationEvidence(
        DateTimeOffset generatedAt,
        IReadOnlyList<DesktopDemonstrationItem> items)
    {
        GeneratedAt = generatedAt;
        Items = items;
    }

    /// <summary>Gets the immutable creation instant shown by every desktop freshness label.</summary>
    internal DateTimeOffset GeneratedAt { get; }

    /// <summary>Gets the ordered immutable instance evidence used by shell and notification-area surfaces.</summary>
    internal IReadOnlyList<DesktopDemonstrationItem> Items { get; }

    /// <summary>Creates the single STATE-05 desktop fixture without reading an Agent, API or monitored database.</summary>
    /// <param name="generatedAt">UTC instant from which all deterministic observation timestamps are derived.</param>
    /// <returns>One immutable provider-neutral evidence set for the application lifetime.</returns>
    internal static DesktopDemonstrationEvidence Create(DateTimeOffset generatedAt) => new(
        generatedAt,
        [
            new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Sample.Instance.Finance", "postgresql", "Sample.Support.Implemented", "Sample.Environment.Production", "Sample.Location.Datacentre", null, HealthStatus.Healthy, generatedAt.AddSeconds(-38), 24),
            new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Sample.Instance.Orders", "mysql", "Sample.Support.Planned", "Sample.Environment.Production", "Sample.Location.PrivateCloud", null, HealthStatus.Degraded, generatedAt.AddMinutes(-2), 86),
            new(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Sample.Instance.Analytics", "sql-server", "Sample.Support.Planned", "Sample.Environment.Validation", null, "Azure", HealthStatus.Timeout, generatedAt.AddMinutes(-3), null),
            new(Guid.Parse("00000000-0000-0000-0000-000000000004"), "Sample.Instance.Catalogue", "mongodb", "Sample.Support.Planned", "Sample.Environment.Development", "Sample.Location.LocalLinux", null, HealthStatus.Unknown, generatedAt.AddMinutes(-9), null, false),
        ]);

    /// <summary>Builds a localised inventory projection while preserving the immutable operational evidence.</summary>
    /// <param name="localisation">Current generated desktop localisation owner.</param>
    /// <returns>A presentation snapshot whose labels reflect the locale and whose timestamps remain unchanged.</returns>
    internal InventorySnapshot CreateInventorySnapshot(DesktopLocalisationService localisation)
    {
        ArgumentNullException.ThrowIfNull(localisation);
        return CreateInventorySnapshot(key => localisation.Text(key));
    }

    /// <summary>Summarises the shared evidence at one clock instant through the canonical Tray freshness policy.</summary>
    /// <param name="now">Current UTC instant used only to evaluate evidence age.</param>
    /// <returns>The same provider-neutral aggregate consumed by all product-mark surfaces.</returns>
    internal TrayFleetSummary Summarise(DateTimeOffset now) =>
        TrayFleetPresentationPolicy.Summarise(CreateInventorySnapshot(static key => key), now, StaleAfter);

    /// <summary>Captures the provider-neutral per-instance baseline used only by local demonstration change notifications.</summary>
    /// <param name="now">Current UTC instant used only to evaluate canonical freshness.</param>
    /// <returns>Effective instance states ordered by their stable identifiers.</returns>
    internal IReadOnlyList<TrayInstanceEffectiveState> CaptureInstanceStates(DateTimeOffset now) =>
        TrayInstanceStateChangePolicy.Capture(CreateInventorySnapshot(static key => key), now, StaleAfter);

    /// <summary>Finds the immutable demonstration item associated with one captured instance state.</summary>
    /// <param name="instanceId">Stable identifier emitted by the per-instance change policy.</param>
    /// <returns>The matching local fixture item, or null when the identifier is not part of this snapshot.</returns>
    internal DesktopDemonstrationItem? FindItem(Guid instanceId) =>
        Items.SingleOrDefault(item => item.InstanceId == instanceId);

    /// <summary>Creates one inventory projection through a supplied label resolver.</summary>
    /// <param name="translate">Resolver for canonical message keys; operational summarisation may retain the keys verbatim.</param>
    /// <returns>An immutable inventory snapshot with unchanged evidence timestamps.</returns>
    private InventorySnapshot CreateInventorySnapshot(Func<string, string> translate) => new(
        InventorySnapshot.CurrentSchemaVersion,
        GeneratedAt,
        Items.Select(item => item.ToPresentation(translate)).ToArray());
}

/// <summary>Contains one locale-independent item from the immutable desktop demonstration evidence set.</summary>
/// <param name="InstanceId">Stable demonstration instance identifier.</param>
/// <param name="DisplayNameKey">Canonical message key for the visible instance name.</param>
/// <param name="ProviderType">Stable provider identifier.</param>
/// <param name="SupportLabelKey">Canonical message key for support truth.</param>
/// <param name="EnvironmentKey">Canonical message key for the environment label.</param>
/// <param name="LocationKey">Canonical message key for the location, or null when a stable proper name is used.</param>
/// <param name="LocationLiteral">Stable proper-name location used when <paramref name="LocationKey"/> is null.</param>
/// <param name="Status">Provider-neutral health status.</param>
/// <param name="ReceivedAt">Immutable receipt instant.</param>
/// <param name="LatencyMilliseconds">Optional bounded demonstration latency.</param>
/// <param name="Enabled">Whether this fixture participates in current-health conclusions.</param>
internal sealed record DesktopDemonstrationItem(
    Guid InstanceId,
    string DisplayNameKey,
    string ProviderType,
    string SupportLabelKey,
    string EnvironmentKey,
    string? LocationKey,
    string? LocationLiteral,
    HealthStatus Status,
    DateTimeOffset ReceivedAt,
    double? LatencyMilliseconds,
    bool Enabled = true)
{
    /// <summary>Projects locale-independent evidence into one localised application presentation item.</summary>
    /// <param name="translate">Resolver for canonical visible-text message keys.</param>
    /// <returns>A presentation item retaining the exact status, timestamps and latency of the evidence.</returns>
    internal InstanceInventoryItem ToPresentation(Func<string, string> translate)
    {
        ArgumentNullException.ThrowIfNull(translate);
        string location = LocationKey is null ? LocationLiteral ?? string.Empty : translate(LocationKey);
        return new(
            InstanceId,
            translate(DisplayNameKey),
            ProviderType,
            translate(SupportLabelKey),
            translate(EnvironmentKey),
            location,
            Status,
            ReceivedAt.AddSeconds(-1),
            ReceivedAt,
            LatencyMilliseconds is null ? null : TimeSpan.FromMilliseconds(LatencyMilliseconds.Value),
            Enabled);
    }
}
