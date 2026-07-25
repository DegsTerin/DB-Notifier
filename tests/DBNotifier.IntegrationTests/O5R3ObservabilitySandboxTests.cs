// Module purpose: Proves the O5-R3 closed observability catalogue, numeric SLOs and sanitised synthetic incident exercises.
using System.Text.Json;
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Verifies O5-R3 observability remains bounded, sanitised, measurable and non-authorising.</summary>
public sealed class O5R3ObservabilitySandboxTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Proves every closed incident has numeric SLOs, functional ownership, escalation and a bounded runbook.</summary>
    [Fact]
    public void CatalogueIsClosedVersionedMeasurableAndOwned()
    {
        O5R3ObservabilityCatalog catalog = O5R3ObservabilityCatalog.CreateDefault();

        Assert.Equal("o5r3-observability-1.0.0", O5R3ObservabilityCatalog.Version);
        Assert.Equal(Enum.GetValues<O5R3ExercisePhase>().Length, catalog.Slos.Count);
        Assert.Equal(Enum.GetValues<O5R3IncidentKind>().Length, catalog.Alerts.Count);
        Assert.Equal(Enum.GetValues<O5R3IncidentKind>().Length, catalog.Runbooks.Count);
        Assert.Equal(
            [
                ("o5r3.sli.detection.elapsed_ms", 1_000L),
                ("o5r3.sli.containment.elapsed_ms", 3_000L),
                ("o5r3.sli.recovery.elapsed_ms", 8_000L),
                ("o5r3.sli.closure.elapsed_ms", 15_000L),
            ],
            catalog.Slos.Select(slo => (slo.Id, slo.MaximumElapsedMilliseconds)));
        Assert.All(catalog.Alerts, alert =>
        {
            Assert.StartsWith("role.", alert.OwnerRole, StringComparison.Ordinal);
            Assert.StartsWith("role.", alert.EscalationRole, StringComparison.Ordinal);
            Assert.Contains(catalog.Runbooks, runbook => runbook.Id == alert.RunbookId);
        });
        Assert.All(catalog.Runbooks, runbook => Assert.InRange(runbook.Steps.Count, 3, 8));
    }

    /// <summary>Runs all six fault classes within the frozen numeric SLOs and records only sanitised bounded evidence.</summary>
    [Fact]
    public void AllAuthorisedIncidentExercisesMeetFrozenSlos()
    {
        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        O5R3ObservabilityCatalog catalog = O5R3ObservabilityCatalog.CreateDefault();

        foreach ((O5R3AlertDefinition alert, int index) in catalog.Alerts.Select((alert, index) => (alert, index)))
        {
            O5R3ExerciseResult result = sandbox.Run(
                Exercise(alert, Now.AddSeconds(index)),
                Now.AddSeconds(index + 12));

            Assert.True(result.Accepted);
            Assert.Equal("o5r3.exercise.accepted", result.Code);
            Assert.Equal(ObserverActivationState.None, result.ActivationState);
            Assert.False(result.MayEvaluate);
            Assert.False(result.MayPublish);
            Assert.NotNull(result.Record);
            Assert.Equal(alert.AlertCode, result.Record.AlertCode);
            Assert.Equal(alert.OwnerRole, result.Record.OwnerRole);
            Assert.Equal(alert.EscalationRole, result.Record.EscalationRole);
            Assert.Equal(alert.RunbookId, result.Record.RunbookId);
        }

        O5R3ObservabilitySnapshot snapshot = sandbox.Snapshot(Now.AddMinutes(1));
        Assert.Equal(6, snapshot.ExerciseCount);
        Assert.Equal(O5R3ObservabilityCatalog.MaximumMetricSeries, snapshot.MetricSeriesCount);
        Assert.Equal("None", snapshot.ActivationState);
        Assert.Equal(O5R3ObservabilityCatalog.Version, snapshot.CatalogVersion);
    }

    /// <summary>Proves payload-shaped diagnostics and unknown incident values fail closed without reflection into evidence.</summary>
    [Fact]
    public void UnknownDiagnosticsAndPayloadCanariesFailClosed()
    {
        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        const string sensitiveField = "pass" + "word";
        const string canary = sensitiveField + "=forbidden SELECT secret FROM topology";
        O5R3ExerciseInput hostile = Exercise(
            O5R3IncidentKind.Corruption,
            canary,
            Now);
        O5R3ExerciseResult unknownCode = sandbox.Run(hostile, Now.AddSeconds(12));
        O5R3ExerciseResult unknownIncident = sandbox.Run(
            hostile with
            {
                Incident = (O5R3IncidentKind)999,
                DiagnosticCode = "o5r2.store.corrupt",
            },
            Now.AddSeconds(12));

        Assert.False(unknownCode.Accepted);
        Assert.Equal("o5r3.diagnostic.not_allowlisted", unknownCode.Code);
        Assert.False(unknownIncident.Accepted);
        Assert.Equal("o5r3.exercise.incident_unknown", unknownIncident.Code);
        string evidence = JsonSerializer.Serialize(sandbox.Snapshot(Now.AddSeconds(12)));
        Assert.DoesNotContain(canary, evidence, StringComparison.Ordinal);
        Assert.DoesNotContain(sensitiveField, evidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT", evidence, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Proves invalid timelines and missed SLOs produce fixed fail-closed outcomes and no retained record.</summary>
    [Fact]
    public void TimelineAndSloFailuresRetainNoEvidence()
    {
        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        O5R3AlertDefinition alert = O5R3ObservabilityCatalog.CreateDefault().Alerts[0];
        O5R3ExerciseInput valid = Exercise(alert, Now);

        O5R3ExerciseResult invalidTimeline = sandbox.Run(
            valid with { ContainedAt = valid.OccurredAt.AddMilliseconds(50) },
            Now.AddSeconds(12));
        O5R3ExerciseResult missedSlo = sandbox.Run(
            valid with { ClosedAt = valid.OccurredAt.AddMilliseconds(15_001) },
            Now.AddSeconds(16));

        Assert.Equal("o5r3.exercise.timeline_invalid", invalidTimeline.Code);
        Assert.Equal("o5r3.exercise.slo_failed", missedSlo.Code);
        Assert.Equal(0, sandbox.Snapshot(Now.AddSeconds(16)).ExerciseCount);
    }

    /// <summary>Proves retained evidence has a strict record cap and cannot grow caller-defined cardinality.</summary>
    [Fact]
    public void CapacityAndCardinalityRemainBounded()
    {
        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        O5R3AlertDefinition alert = O5R3ObservabilityCatalog.CreateDefault().Alerts[0];
        for (int index = 0; index < O5R3ObservabilityCatalog.MaximumExerciseRecords; index++)
        {
            DateTimeOffset occurredAt = Now.AddMilliseconds(index * 10);
            Assert.True(sandbox.Run(Exercise(alert, occurredAt), occurredAt.AddSeconds(12)).Accepted);
        }

        O5R3ExerciseResult overflow = sandbox.Run(
            Exercise(alert, Now.AddMinutes(1)),
            Now.AddMinutes(1).AddSeconds(12));
        O5R3ObservabilitySnapshot snapshot = sandbox.Snapshot(Now.AddMinutes(1).AddSeconds(12));

        Assert.False(overflow.Accepted);
        Assert.Equal("o5r3.observability.capacity_exhausted", overflow.Code);
        Assert.Equal(O5R3ObservabilityCatalog.MaximumExerciseRecords, snapshot.ExerciseCount);
        Assert.Equal(4, snapshot.MetricSeriesCount);
        Assert.True(snapshot.MetricSeriesCount <= O5R3ObservabilityCatalog.MaximumMetricSeries);
    }

    /// <summary>Proves records expire at the fixed retention boundary and old inputs cannot be re-admitted.</summary>
    [Fact]
    public void RetentionPrunesOldEvidence()
    {
        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        O5R3AlertDefinition alert = O5R3ObservabilityCatalog.CreateDefault().Alerts[0];
        O5R3ExerciseInput input = Exercise(alert, Now);
        Assert.True(sandbox.Run(input, input.ClosedAt).Accepted);

        DateTimeOffset afterRetention = input.ClosedAt + O5R3ObservabilityCatalog.Retention + TimeSpan.FromMilliseconds(1);
        Assert.Empty(sandbox.Snapshot(afterRetention).Records);
        O5R3ExerciseResult expired = sandbox.Run(input, afterRetention);
        Assert.Equal("o5r3.observability.expired", expired.Code);
    }

    /// <summary>Proves cancellation and the exact marker refuse work before any evidence is retained.</summary>
    [Fact]
    public void MarkerAndCancellationFailBeforeWork()
    {
        O5R3ObservabilityCatalog catalog = O5R3ObservabilityCatalog.CreateDefault();
        Assert.Throws<InvalidOperationException>(
            () => new O5R3ObservabilitySandbox("DBNOTIFIER_O5_R3", catalog));

        O5R3ObservabilitySandbox sandbox = CreateSandbox();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(
            () => sandbox.Run(
                Exercise(catalog.Alerts[0], Now),
                Now.AddSeconds(12),
                cancellation.Token));
        Assert.Empty(sandbox.Snapshot(Now.AddSeconds(12)).Records);
    }

    /// <summary>Proves an unmeasurable SLO, missing owner or missing runbook blocks catalogue construction.</summary>
    [Fact]
    public void IncompleteGovernanceBlocksCatalogue()
    {
        O5R3ObservabilityCatalog valid = O5R3ObservabilityCatalog.CreateDefault();
        O5R3SloDefinition[] invalidSlos = valid.Slos.ToArray();
        invalidSlos[0] = invalidSlos[0] with { MaximumElapsedMilliseconds = 0 };
        Assert.Equal(
            "o5r3.catalog.slo_unmeasurable",
            Assert.Throws<InvalidOperationException>(
                () => new O5R3ObservabilityCatalog(invalidSlos, valid.Alerts, valid.Runbooks)).Message);

        O5R3AlertDefinition[] unownedAlerts = valid.Alerts.ToArray();
        unownedAlerts[0] = unownedAlerts[0] with { OwnerRole = string.Empty };
        Assert.Equal(
            "o5r3.catalog.alert_unowned",
            Assert.Throws<InvalidOperationException>(
                () => new O5R3ObservabilityCatalog(valid.Slos, unownedAlerts, valid.Runbooks)).Message);

        Assert.Equal(
            "o5r3.catalog.incomplete",
            Assert.Throws<InvalidOperationException>(
                () => new O5R3ObservabilityCatalog(valid.Slos, valid.Alerts, valid.Runbooks.Skip(1).ToArray())).Message);
    }

    /// <summary>Creates the exact marked sandbox with its frozen default catalogue.</summary>
    /// <returns>A non-operational O5-R3 sandbox.</returns>
    private static O5R3ObservabilitySandbox CreateSandbox() =>
        new(
            O5R3ObservabilitySandbox.Marker,
            O5R3ObservabilityCatalog.CreateDefault());

    /// <summary>Creates one complete exercise from an exact catalogue alert.</summary>
    /// <param name="alert">Allow-listed alert definition.</param>
    /// <param name="occurredAt">Synthetic occurrence time.</param>
    /// <returns>A deterministic timeline within every frozen SLO.</returns>
    private static O5R3ExerciseInput Exercise(O5R3AlertDefinition alert, DateTimeOffset occurredAt) =>
        Exercise(alert.Incident, alert.SourceDiagnosticCode, occurredAt);

    /// <summary>Creates one complete exercise from explicit incident and diagnostic values.</summary>
    /// <param name="incident">Closed incident kind.</param>
    /// <param name="diagnosticCode">Candidate source diagnostic code.</param>
    /// <param name="occurredAt">Synthetic occurrence time.</param>
    /// <returns>A deterministic timeline within every frozen SLO.</returns>
    private static O5R3ExerciseInput Exercise(
        O5R3IncidentKind incident,
        string diagnosticCode,
        DateTimeOffset occurredAt) =>
        new(
            incident,
            diagnosticCode,
            occurredAt,
            occurredAt.AddMilliseconds(250),
            occurredAt.AddMilliseconds(1_000),
            occurredAt.AddMilliseconds(3_000),
            occurredAt.AddMilliseconds(6_000));
}
