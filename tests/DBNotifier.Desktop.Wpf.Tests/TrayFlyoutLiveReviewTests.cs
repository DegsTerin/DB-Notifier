// Module purpose: Proves the bounded W02 presentation sequence without creating a WPF window or Windows notification.
using DBNotifier.Application.Presentation;
using DBNotifier.Desktop.Wpf;
using DBNotifier.Domain;
using Xunit;

namespace DBNotifier.Desktop.Wpf.Tests;

/// <summary>Verifies exact activation and coherent in-memory frame progression for the W02 flyout review.</summary>
public sealed class TrayFlyoutLiveReviewTests
{
    /// <summary>Proves W02 remains disabled unless its exact marker occurs once.</summary>
    [Fact]
    public void ActivationRequiresOneExactMarker()
    {
        Assert.Equal(
            TrayFlyoutLiveReviewMode.Disabled,
            TrayFlyoutLiveReviewPolicy.Resolve([]));
        Assert.Equal(
            TrayFlyoutLiveReviewMode.Disabled,
            TrayFlyoutLiveReviewPolicy.Resolve(["--review-flyout-live-updates"]));
        Assert.Equal(
            TrayFlyoutLiveReviewMode.Disabled,
            TrayFlyoutLiveReviewPolicy.Resolve(
                [TrayFlyoutLiveReviewPolicy.ReviewArgument, TrayFlyoutLiveReviewPolicy.ReviewArgument]));
        Assert.Equal(
            TrayFlyoutLiveReviewMode.BoundedInMemorySequence,
            TrayFlyoutLiveReviewPolicy.Resolve(["--REVIEW-FLYOUT-LIVE-UPDATE"]));
    }

    /// <summary>Proves null arguments are rejected rather than enabling an implicit review path.</summary>
    [Fact]
    public void ActivationRejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => TrayFlyoutLiveReviewPolicy.Resolve(null!));
    }

    /// <summary>Proves W02 cannot expose the preference-bearing secondary shell.</summary>
    [Fact]
    public void SecondaryShellIsAvailableOnlyWhenW02IsDisabled()
    {
        Assert.True(TrayFlyoutLiveReviewPolicy.AllowsSecondaryShell(TrayFlyoutLiveReviewMode.Disabled));
        Assert.False(TrayFlyoutLiveReviewPolicy.AllowsSecondaryShell(
            TrayFlyoutLiveReviewMode.BoundedInMemorySequence));
        Assert.False(TrayFlyoutLiveReviewPolicy.AllowsSecondaryShell((TrayFlyoutLiveReviewMode)999));
    }

    /// <summary>Proves the finite sequence changes aggregate, disabled count and row states coherently.</summary>
    [Fact]
    public void SequenceProducesThreeCoherentFramesAndThenStops()
    {
        TrayFlyoutLiveReviewSequence sequence = new();

        TrayFlyoutPresentationFrame first = sequence.Reset();
        TrayFlyoutPresentationFrame second = Assert.IsType<TrayFlyoutPresentationFrame>(sequence.Advance());
        TrayFlyoutPresentationFrame third = Assert.IsType<TrayFlyoutPresentationFrame>(sequence.Advance());

        Assert.Null(sequence.Advance());
        Assert.Equal(
            [TrayAggregateState.Critical, TrayAggregateState.Warning, TrayAggregateState.Unknown],
            new[] { first.Summary.State, second.Summary.State, third.Summary.State });
        Assert.Equal(
            [1, 2, 1],
            new[] { first.Summary.DisabledCount, second.Summary.DisabledCount, third.Summary.DisabledCount });

        foreach (TrayFlyoutPresentationFrame frame in new[] { first, second, third })
        {
            Assert.Equal(4, frame.InstanceStates.Count);
            TrayFleetSummary independentlyDerived = TrayFleetPresentationPolicy.Summarise(
                frame.InstanceStates.Select(state => new TrayFleetSignal(
                    state.Status,
                    state.Freshness != EvidenceFreshness.Current,
                    state.Enabled)));
            Assert.Equal(independentlyDerived, frame.Summary);
        }

        Guid analyticsId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        Assert.True(first.Find(analyticsId)!.Enabled);
        Assert.False(second.Find(analyticsId)!.Enabled);
        Assert.True(third.Find(analyticsId)!.Enabled);
        Assert.Equal(HealthStatus.Unknown, third.Find(analyticsId)!.Status);
    }

    /// <summary>Proves reopening the flyout restarts the same deterministic initial frame.</summary>
    [Fact]
    public void ResetRestartsTheBoundedSequence()
    {
        TrayFlyoutLiveReviewSequence sequence = new();
        TrayFlyoutPresentationFrame initial = sequence.Reset();
        _ = sequence.Advance();
        _ = sequence.Advance();

        TrayFlyoutPresentationFrame restarted = sequence.Reset();

        Assert.Equal(initial.Summary, restarted.Summary);
        Assert.Equal(initial.InstanceStates, restarted.InstanceStates);
        Assert.NotNull(sequence.Advance());
    }

    /// <summary>Proves open, bounded ticks, exhaustion, close and reopen are deterministic without a Dispatcher.</summary>
    [Fact]
    public void SessionModelsTheCompleteBoundedLifecycle()
    {
        TrayFlyoutLiveReviewSession session = new();

        Assert.Null(session.CurrentFrame);
        Assert.Null(session.Advance());
        Assert.Equal(TrayAggregateState.Critical, session.Open().Summary.State);
        Assert.Equal(TrayAggregateState.Warning, session.Advance()!.Summary.State);
        Assert.Equal(TrayAggregateState.Unknown, session.Advance()!.Summary.State);
        Assert.Null(session.Advance());
        Assert.Equal(TrayAggregateState.Unknown, session.CurrentFrame!.Summary.State);

        session.Close();

        Assert.Null(session.CurrentFrame);
        Assert.Null(session.Advance());
        Assert.Equal(TrayAggregateState.Critical, session.Open().Summary.State);
    }

    /// <summary>Proves malformed frames cannot repeat, omit or invent a stable row identity.</summary>
    [Fact]
    public void FrameRequiresTheExactFourFixtureIdentities()
    {
        TrayFlyoutInstancePresentationState[] validStates = TrayFlyoutLiveReviewFixture.InstanceIds
            .Select(instanceId => new TrayFlyoutInstancePresentationState(
                instanceId,
                HealthStatus.Healthy,
                EvidenceFreshness.Current,
                true))
            .ToArray();

        Assert.Throws<ArgumentException>(() => new TrayFlyoutPresentationFrame([]));
        Assert.Throws<ArgumentException>(() => new TrayFlyoutPresentationFrame([validStates[0], validStates[0]]));
        Assert.Throws<ArgumentException>(() => new TrayFlyoutPresentationFrame(validStates[..3]));
        Assert.Throws<ArgumentException>(() => new TrayFlyoutPresentationFrame(
            [.. validStates, new(Guid.NewGuid(), HealthStatus.Healthy, EvidenceFreshness.Current, true)]));
        Assert.Equal(4, new TrayFlyoutPresentationFrame(validStates).InstanceStates.Count);
    }
}
