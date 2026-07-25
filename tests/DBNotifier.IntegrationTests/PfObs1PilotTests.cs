// Module purpose: Verifies PF-OBS-1 corpus isolation, calibration, holdout and fail-closed environment activation without a live database.
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Proves the deterministic local pilot contracts independently of the physical PostgreSQL campaign.</summary>
public sealed class PfObs1PilotTests
{
    /// <summary>Proves exact disjoint partitioning and a policy frozen before holdout evaluation.</summary>
    [Fact]
    public void LaboratoryCorpusCalibratesAndEvaluatesDisjointHoldout()
    {
        IReadOnlyList<(double DurationMilliseconds, bool ExpectedDegraded)> measurements =
            Enumerable.Range(0, PfObs1CorpusEvaluation.SampleCount)
                .Select(index => index % 2 == 0
                    ? (5d + (index % 3), false)
                    : (65d + (index % 3), true))
                .ToArray();

        IReadOnlyList<PfObs1CorpusSample> corpus = PfObs1CorpusEvaluation.Partition(measurements);
        (double threshold, string policyDigest) = PfObs1CorpusEvaluation.Freeze(corpus);
        PfObs1HoldoutResult result = PfObs1CorpusEvaluation.Evaluate(
            corpus,
            threshold,
            policyDigest);

        Assert.Equal(36, corpus.Select(item => item.Id).Distinct().Count());
        Assert.All(
            Enum.GetValues<PfObs1Partition>(),
            partition => Assert.Equal(12, corpus.Count(item => item.Partition == partition)));
        Assert.InRange(threshold, 7.000001d, 64.999999d);
        Assert.Equal(12, result.HoldoutCount);
        Assert.Equal(1d, result.Accuracy);
        Assert.Equal(0, result.FalsePositive);
        Assert.Equal(0, result.FalseNegative);
    }

    /// <summary>Proves incomplete membership and invalid frozen policy fail closed.</summary>
    [Fact]
    public void LaboratoryCorpusRejectsIncompleteOrInvalidEvaluation()
    {
        IReadOnlyList<(double DurationMilliseconds, bool ExpectedDegraded)> incomplete =
            Enumerable.Range(0, PfObs1CorpusEvaluation.SampleCount - 1)
                .Select(index => ((double)index, index % 2 == 1))
                .ToArray();

        Assert.Throws<InvalidOperationException>(() => PfObs1CorpusEvaluation.Partition(incomplete));

        IReadOnlyList<PfObs1CorpusSample> complete = PfObs1CorpusEvaluation.Partition(
            Enumerable.Range(0, PfObs1CorpusEvaluation.SampleCount)
                .Select(index => index % 2 == 0 ? (5d, false) : (65d, true))
                .ToArray());
        Assert.Throws<InvalidOperationException>(
            () => PfObs1CorpusEvaluation.Evaluate(complete, double.NaN, new string('A', 64)));
    }

    /// <summary>Proves ordinary test execution does not require a PostgreSQL laboratory marker.</summary>
    [Fact]
    public async Task PilotEntrypointIsDormantWithoutExplicitMarker()
    {
        string? previous = Environment.GetEnvironmentVariable(PfObs1PilotCampaign.MarkerVariable);
        try
        {
            Environment.SetEnvironmentVariable(PfObs1PilotCampaign.MarkerVariable, null);
            await new PfObs1PilotEntryPointTests()
                .RunFunctionalPostgreSqlObserverPilotWhenExplicitlyAuthorised();
        }
        finally
        {
            Environment.SetEnvironmentVariable(PfObs1PilotCampaign.MarkerVariable, previous);
        }
    }
}
