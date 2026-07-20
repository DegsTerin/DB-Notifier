// Module purpose: Classifies synchronous Windows notification boundary attempts without claiming visible Shell delivery.
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Selects the modern Windows notification API before the direct legacy fallback and returns acceptance only after
/// the selected boundary call has completed successfully. It owns no queue, provider state or administrative action.
/// </summary>
public static class ReconciledWindowsNotificationBoundary
{
    /// <summary>Attempts the modern boundary and then the direct legacy boundary under factual acceptance semantics.</summary>
    /// <param name="tryModern">Synchronous modern Windows API call that returns true only after platform acceptance.</param>
    /// <param name="tryLegacy">Synchronous legacy Windows call that returns true only after <c>ShowBalloonTip</c> returns.</param>
    /// <returns>An accepted result only after one supplied boundary succeeds; otherwise a retryable busy result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either required boundary delegate is absent.</exception>
    public static ReconciledNotificationDeliveryResult Deliver(
        Func<bool> tryModern,
        Func<bool> tryLegacy)
    {
        ArgumentNullException.ThrowIfNull(tryModern);
        ArgumentNullException.ThrowIfNull(tryLegacy);
        return tryModern() || tryLegacy()
            ? new(ReconciledNotificationDeliveryDisposition.Accepted, "delivery.local_platform_accepted")
            : new(ReconciledNotificationDeliveryDisposition.Retryable, "delivery.local_platform_busy");
    }
}
