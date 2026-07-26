// Module purpose: Proves the PF-OBS-1-D4 diagnostic identity, bounded cancellation and fail-closed evidence boundary.
using System.Diagnostics;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Defines the isolated xUnit collection used by the strict cancellation-timing diagnostic.
/// </summary>
/// <remarks>
/// The 100-millisecond diagnostic must not compete with unrelated integration-test
/// collections because unrelated test load would confound its isolated cancellation measurement.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class O5R5CancellationTimingGroup
{
    /// <summary>Identifies the non-parallel cancellation-timing collection.</summary>
    public const string Name = "O5/R5 cancellation timing";
}

/// <summary>
/// Validates D4 diagnostics without executing an HM-01–HM-03 physical campaign.
/// </summary>
/// <remarks>
/// The class runs after parallel collections so the strict timing assertion measures the
/// candidate rather than scheduler contention created by unrelated integration tests.
/// </remarks>
[Collection(O5R5CancellationTimingGroup.Name)]
public sealed class O5R5CancellationWorkingSetDiagnosticsTests
{
    /// <summary>Proves the predeclared canonical statement retains its exact authenticated identity.</summary>
    [Fact]
    public void DiagnosticProtocolIdentityAndLimitsAreFrozen()
    {
        Assert.Equal(
            O5R5D4DiagnosticProtocol.Digest,
            O5R5D4DiagnosticProtocol.CalculateDigest());
        Assert.Equal(35, O5R5D4DiagnosticProtocol.RetainedRepetitions);
        Assert.Equal(1, O5R5D4DiagnosticProtocol.PreparationRepetitions);
        Assert.Equal(786_432, O5R5D4DiagnosticProtocol.WorkingSetLimitBytes);
        Assert.Equal(100, O5R5D4DiagnosticProtocol.InvocationDeadlineMilliseconds);
    }

    /// <summary>Proves the comparison candidate observes inner cancellation and honours outer cancellation.</summary>
    [Fact]
    public void WaitHandleCandidateIsTimelyAndCancellationAware()
    {
        using CancellationTokenSource inner = new();
        inner.CancelAfter(TimeSpan.FromMilliseconds(10));
        Stopwatch elapsed = Stopwatch.StartNew();

        bool observed = O5R5D4CancellationDiagnosticRunner.ObserveThroughWaitHandle(
            inner,
            CancellationToken.None);

        elapsed.Stop();
        Assert.True(observed);
        Assert.InRange(
            elapsed.Elapsed.TotalMilliseconds,
            0,
            O5R5D4DiagnosticProtocol.InvocationDeadlineMilliseconds);

        using CancellationTokenSource outer = new();
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            outer.Token);
        outer.Cancel();
        Assert.Throws<OperationCanceledException>(
            () => O5R5D4CancellationDiagnosticRunner.ObserveThroughWaitHandle(
                linked,
                outer.Token));
    }

    /// <summary>Proves evidence destinations cannot escape the exact D4 temporary boundary.</summary>
    [Fact]
    public void EvidenceDestinationFailsClosedOutsideExactTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-PF-OBS-1-D4-{Guid.NewGuid():N}");
        string accepted = Path.Combine(root, "async-delay.json");

        Assert.Equal(Path.GetFullPath(accepted), O5R5D4DiagnosticEvidenceWriter.ValidateDestination(accepted));
        InvalidOperationException outside = Assert.Throws<InvalidOperationException>(
            () => O5R5D4DiagnosticEvidenceWriter.ValidateDestination(
                Path.Combine(Path.GetTempPath(), "async-delay.json")));
        Assert.Equal("o5r5d4.output.path_refused", outside.Message);
        InvalidOperationException wrongName = Assert.Throws<InvalidOperationException>(
            () => O5R5D4DiagnosticEvidenceWriter.ValidateDestination(
                Path.Combine(root, "selected.json")));
        Assert.Equal("o5r5d4.output.path_refused", wrongName.Message);
    }

    /// <summary>Proves the built-in D4 metric source returns complete non-negative counters.</summary>
    [Fact]
    public void WindowsMetricSourceReturnsCompleteSanitisedCounters()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using O5R5D4ProcessMetricSource source = new();
        O5R5D4ProcessMetrics metrics = source.Capture();

        Assert.True(metrics.WorkingSetBytes > 0);
        Assert.True(metrics.PrivateMemoryBytes > 0);
        Assert.True(metrics.GcHeapBytes >= 0);
        Assert.True(metrics.GcCommittedBytes >= 0);
        Assert.True(metrics.ThreadCount >= 0);
        Assert.True(metrics.HandleCount > 0);
        Assert.True(metrics.TotalAllocatedBytes >= 0);
    }
}
