// Module purpose: Proves factual WPF notification acceptance without invoking a real or visible Windows notification.
using DBNotifier.Application.Presentation;
using DBNotifier.Desktop.Wpf;
using Xunit;

namespace DBNotifier.Desktop.Wpf.Tests;

/// <summary>Verifies the modern-first direct fallback contract used by the reconciled WPF sink.</summary>
public sealed class ReconciledWindowsNotificationBoundaryTests
{
    /// <summary>Proves acceptance occurs only after the direct legacy boundary has returned success.</summary>
    [Fact]
    public void LegacyAcceptanceFollowsTheActualBoundaryCall()
    {
        bool boundaryReturned = false;

        ReconciledNotificationDeliveryResult result = ReconciledWindowsNotificationBoundary.Deliver(
            () => false,
            () =>
            {
                boundaryReturned = true;
                return boundaryReturned;
            });

        Assert.True(boundaryReturned);
        Assert.Equal(ReconciledNotificationDeliveryDisposition.Accepted, result.Disposition);
        Assert.Equal("delivery.local_platform_accepted", result.ErrorCode);
    }

    /// <summary>Proves a busy fallback remains retryable and is never classified as accepted queue admission.</summary>
    [Fact]
    public void BusyFallbackIsRetryable()
    {
        ReconciledNotificationDeliveryResult result = ReconciledWindowsNotificationBoundary.Deliver(
            () => false,
            () => false);

        Assert.Equal(ReconciledNotificationDeliveryDisposition.Retryable, result.Disposition);
        Assert.Equal("delivery.local_platform_busy", result.ErrorCode);
    }

    /// <summary>Proves a successful modern boundary prevents an unnecessary legacy fallback call.</summary>
    [Fact]
    public void ModernAcceptanceShortCircuitsTheFallback()
    {
        bool fallbackCalled = false;

        ReconciledNotificationDeliveryResult result = ReconciledWindowsNotificationBoundary.Deliver(
            () => true,
            () =>
            {
                fallbackCalled = true;
                return true;
            });

        Assert.Equal(ReconciledNotificationDeliveryDisposition.Accepted, result.Disposition);
        Assert.False(fallbackCalled);
    }
}
