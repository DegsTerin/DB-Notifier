// Module purpose: Validates the bounded contracts used only to make the two blocked STATE-06 human samples eligible for later review.
using System.Globalization;
using DBNotifier.Application.Presentation;
using Xunit;

namespace DBNotifier.IntegrationTests;

public sealed partial class AgentFleetApiEndToEndTests
{
    /// <summary>Confirms the immutable visual fixture distinguishes current Unknown from stale evidence.</summary>
    [Fact]
    public void FinalHumanVisualTruthFixtureIsValidDistinctAndExplicitlyUnsupported()
    {
        DateTimeOffset now = new(2026, 7, 20, 18, 0, 0, TimeSpan.Zero);
        DashboardTvSnapshot snapshot = ConsolidatedHarnessState.CreateHumanVisualTruthSnapshot(now);

        Assert.True(DashboardTvSnapshotValidator.TryValidate(snapshot, now, out string errorCode), errorCode);
        Assert.Equal(DashboardTvSnapshotContract.CurrentSchemaVersion, snapshot.SchemaVersion);
        Assert.Collection(
            snapshot.Items,
            current =>
            {
                Assert.Equal("unknown", current.Status);
                Assert.Equal(current.ReceivedAt, current.ObservedAt);
                Assert.Contains("Planned - not implemented", current.SupportLabel, StringComparison.Ordinal);
                Assert.Contains("sintético", current.LocationLabel, StringComparison.Ordinal);
            },
            stale =>
            {
                Assert.Equal("degraded", stale.Status);
                Assert.Equal(now.AddMinutes(-6), DateTimeOffset.Parse(stale.ReceivedAt, CultureInfo.InvariantCulture));
                Assert.Contains("Planned - not implemented", stale.SupportLabel, StringComparison.Ordinal);
                Assert.Contains("sintético", stale.LocationLabel, StringComparison.Ordinal);
            });
    }

    /// <summary>Confirms the companion page is self-contained, labelled test-only and free of secret-shaped material.</summary>
    [Fact]
    public void FinalHumanEvidencePageIsStaticSanitisedAndNonOperational()
    {
        ConsolidatedHarnessState state = new(Guid.Parse("06000000-0000-4000-8000-000000000006"), true);
        string page = ConsolidatedHarnessState.BuildHumanEvidencePage();

        Assert.True(state.HumanRemediationMode);
        Assert.Equal("quality-gate", state.HumanReviewSample);
        Assert.Contains("Evidência do harness — não é estado operacional", page, StringComparison.Ordinal);
        Assert.Contains("Browser → API disponível", page, StringComparison.Ordinal);
        Assert.Contains("Browser → API temporariamente limitada pelo sandbox", page, StringComparison.Ordinal);
        Assert.Contains("Browser → API: acesso de teste negado", page, StringComparison.Ordinal);
        Assert.Contains("Dashboard TV sandbox test-only", page, StringComparison.Ordinal);
        Assert.Contains("Planejado — não implementado", page, StringComparison.Ordinal);
        Assert.Contains("window.setTimeout(refresh", page, StringComparison.Ordinal);
        Assert.Contains("const refreshDelay = 2100", page, StringComparison.Ordinal);
        Assert.DoesNotContain("setInterval", page, StringComparison.Ordinal);
        Assert.Contains("Apresentar perda sintética do Agent", page, StringComparison.Ordinal);
        Assert.Contains("Apresentar unknown e stale", page, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private key", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pkcs12", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-DBN-", page, StringComparison.Ordinal);
        Assert.DoesNotContain(state.RunId, page, StringComparison.Ordinal);
        Assert.DoesNotContain("CommandAttempt", page, StringComparison.Ordinal);
    }

    /// <summary>Confirms only the automatic gate and the two blocked samples can select the test-only presentation.</summary>
    [Fact]
    public void FinalHumanReviewSelectorAcceptsOnlyTheBoundedModes()
    {
        ConsolidatedHarnessState sample001 = new(Guid.NewGuid(), true, "S06-HG-001");
        ConsolidatedHarnessState sample006 = new(Guid.NewGuid(), true, "S06-HG-006");

        Assert.Equal("S06-HG-001", sample001.HumanReviewSample);
        Assert.Equal("S06-HG-006", sample006.HumanReviewSample);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsolidatedHarnessState(Guid.NewGuid(), true, "S06-HG-002"));
        Assert.Throws<ArgumentException>(() => new ConsolidatedHarnessState(Guid.NewGuid(), false, "S06-HG-001"));
    }
}
