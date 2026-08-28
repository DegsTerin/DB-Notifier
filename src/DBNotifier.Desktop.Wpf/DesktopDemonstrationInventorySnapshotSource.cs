// Module purpose: Adapts the authorised local desktop demonstration fixture to the provider-neutral read-only reconciliation contract.
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Produces localised snapshots from immutable in-memory demonstration evidence without connecting to an Agent,
/// provider, monitored database, service controller or external infrastructure.
/// </summary>
internal sealed class DesktopDemonstrationInventorySnapshotSource : IDesktopFleetSnapshotSource
{
    private readonly DesktopDemonstrationEvidence evidence;
    private readonly DesktopLocalisationService localisation;

    /// <summary>Initialises the isolated demonstration adapter used until an authorised integration source exists.</summary>
    /// <param name="evidence">Immutable local evidence owned by the current desktop process.</param>
    /// <param name="localisation">Generated localisation owner used only to project visible labels.</param>
    internal DesktopDemonstrationInventorySnapshotSource(
        DesktopDemonstrationEvidence evidence,
        DesktopLocalisationService localisation)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(localisation);
        this.evidence = evidence;
        this.localisation = localisation;
    }

    /// <summary>Returns one localised copy of the immutable local fixture through the read-only source boundary.</summary>
    /// <param name="cancellationToken">Cancellation requested by the owning desktop lifecycle.</param>
    /// <returns>An accepted provider-neutral snapshot explicitly identified as local demonstration evidence.</returns>
    /// <exception cref="OperationCanceledException">Thrown when cancellation was requested before projection.</exception>
    public ValueTask<DesktopFleetSnapshotReadResult> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new DesktopFleetSnapshotReadResult(
            DesktopFleetReadDisposition.Accepted,
            evidence.CreateInventorySnapshot(localisation),
            "source.local-demonstration"));
    }
}
