// Module purpose: Implements the closed, sanitised and bounded O5-R3 observability sandbox without operational integrations.
using DBNotifier.Application.AIOps;

namespace DBNotifier.IntegrationTests;

/// <summary>Identifies the six synthetic incident classes exercised by the O5-R3 sandbox.</summary>
internal enum O5R3IncidentKind
{
    Saturation = 1,
    Corruption = 2,
    Stale = 3,
    SplitView = 4,
    KillSwitch = 5,
    Rollback = 6,
}

/// <summary>Identifies the four measurable phases of one synthetic incident exercise.</summary>
internal enum O5R3ExercisePhase
{
    Detection = 1,
    Containment = 2,
    Recovery = 3,
    Closure = 4,
}

/// <summary>Defines one numeric sandbox SLO before any synthetic exercise is evaluated.</summary>
/// <param name="Id">Stable SLO identifier.</param>
/// <param name="Phase">Incident phase measured from the initial occurrence.</param>
/// <param name="MaximumElapsedMilliseconds">Inclusive maximum elapsed time.</param>
internal sealed record O5R3SloDefinition(
    string Id,
    O5R3ExercisePhase Phase,
    long MaximumElapsedMilliseconds);

/// <summary>Defines one bounded, non-executable incident runbook as stable action identifiers.</summary>
/// <param name="Id">Stable runbook identifier.</param>
/// <param name="Steps">Closed ordered action identifiers that grant no operational authority.</param>
internal sealed record O5R3RunbookDefinition(string Id, IReadOnlyList<string> Steps);

/// <summary>Maps one synthetic O5-R2 diagnostic to its alert, functional owner, escalation and runbook.</summary>
/// <param name="Incident">Closed incident kind.</param>
/// <param name="SourceDiagnosticCode">Allow-listed O5-R2 diagnostic code.</param>
/// <param name="AlertCode">Stable sanitised alert code.</param>
/// <param name="OwnerRole">Functional owner role; this is not a named production owner.</param>
/// <param name="EscalationRole">Functional escalation role.</param>
/// <param name="RunbookId">Bounded non-executable runbook identifier.</param>
internal sealed record O5R3AlertDefinition(
    O5R3IncidentKind Incident,
    string SourceDiagnosticCode,
    string AlertCode,
    string OwnerRole,
    string EscalationRole,
    string RunbookId);

/// <summary>
/// Owns the immutable O5-R3 catalogue of SLOs, diagnostics, alert ownership and non-executable runbooks.
/// </summary>
internal sealed class O5R3ObservabilityCatalog
{
    internal const string Version = "o5r3-observability-1.0.0";
    internal const int MaximumExerciseRecords = 24;
    internal const int MaximumMetricSeries = 24;
    internal static readonly TimeSpan Retention = TimeSpan.FromMinutes(15);

    private static readonly IReadOnlyList<O5R3SloDefinition> DefaultSlos = Array.AsReadOnly(
        new O5R3SloDefinition[]
    {
        new("o5r3.sli.detection.elapsed_ms", O5R3ExercisePhase.Detection, 1_000),
        new("o5r3.sli.containment.elapsed_ms", O5R3ExercisePhase.Containment, 3_000),
        new("o5r3.sli.recovery.elapsed_ms", O5R3ExercisePhase.Recovery, 8_000),
        new("o5r3.sli.closure.elapsed_ms", O5R3ExercisePhase.Closure, 15_000),
    });

    private static readonly IReadOnlyList<O5R3RunbookDefinition> DefaultRunbooks = Array.AsReadOnly(
        new O5R3RunbookDefinition[]
    {
        Runbook(
            "o5r3.runbook.saturation",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-admission-blocked",
            "escalate-to-functional-role",
            "close-after-control-proof"),
        Runbook(
            "o5r3.runbook.corruption",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-continuity-quarantined",
            "escalate-to-functional-role",
            "close-after-recovery-proof"),
        Runbook(
            "o5r3.runbook.stale",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-context-refused",
            "escalate-to-functional-role",
            "close-after-freshness-proof"),
        Runbook(
            "o5r3.runbook.split_view",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-continuity-quarantined",
            "escalate-to-functional-role",
            "close-after-recovery-proof"),
        Runbook(
            "o5r3.runbook.kill_switch",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-admission-blocked",
            "escalate-to-functional-role",
            "close-after-control-proof"),
        Runbook(
            "o5r3.runbook.rollback",
            "preserve-sanitised-evidence",
            "verify-activation-none",
            "confirm-continuity-quarantined",
            "escalate-to-functional-role",
            "close-after-recovery-proof"),
    });

    private static readonly IReadOnlyList<O5R3AlertDefinition> DefaultAlerts = Array.AsReadOnly(
        new O5R3AlertDefinition[]
    {
        Alert(
            O5R3IncidentKind.Saturation,
            "o5r2.control.approval_capacity_exhausted",
            "o5r3.alert.control_saturation",
            "role.observer-control-owner",
            "role.incident-commander",
            "o5r3.runbook.saturation"),
        Alert(
            O5R3IncidentKind.Corruption,
            "o5r2.store.corrupt",
            "o5r3.alert.state_corruption",
            "role.observer-security-owner",
            "role.incident-commander",
            "o5r3.runbook.corruption"),
        Alert(
            O5R3IncidentKind.Stale,
            "o5r2.control.deadline_expired",
            "o5r3.alert.stale_context",
            "role.observer-control-owner",
            "role.incident-commander",
            "o5r3.runbook.stale"),
        Alert(
            O5R3IncidentKind.SplitView,
            "o5r2.store.split_view",
            "o5r3.alert.split_view",
            "role.observer-security-owner",
            "role.incident-commander",
            "o5r3.runbook.split_view"),
        Alert(
            O5R3IncidentKind.KillSwitch,
            "o5r2.control.kill_switch_engaged",
            "o5r3.alert.kill_switch_engaged",
            "role.observer-control-owner",
            "role.incident-commander",
            "o5r3.runbook.kill_switch"),
        Alert(
            O5R3IncidentKind.Rollback,
            "o5r2.store.rollback",
            "o5r3.alert.rollback_detected",
            "role.observer-security-owner",
            "role.incident-commander",
            "o5r3.runbook.rollback"),
    });

    /// <summary>Initialises and validates a complete closed catalogue.</summary>
    /// <param name="slos">Exact numeric SLO definitions.</param>
    /// <param name="alerts">Exact incident-to-alert mappings.</param>
    /// <param name="runbooks">Exact non-executable runbooks.</param>
    /// <exception cref="InvalidOperationException">Thrown when the catalogue is incomplete, ambiguous or unbounded.</exception>
    internal O5R3ObservabilityCatalog(
        IReadOnlyList<O5R3SloDefinition> slos,
        IReadOnlyList<O5R3AlertDefinition> alerts,
        IReadOnlyList<O5R3RunbookDefinition> runbooks)
    {
        ArgumentNullException.ThrowIfNull(slos);
        ArgumentNullException.ThrowIfNull(alerts);
        ArgumentNullException.ThrowIfNull(runbooks);

        Slos = Array.AsReadOnly(slos.ToArray());
        Alerts = Array.AsReadOnly(alerts.ToArray());
        Runbooks = Array.AsReadOnly(runbooks.ToArray());
        Validate();
    }

    /// <summary>Gets the complete numeric SLO catalogue.</summary>
    internal IReadOnlyList<O5R3SloDefinition> Slos { get; }

    /// <summary>Gets the complete incident alert catalogue.</summary>
    internal IReadOnlyList<O5R3AlertDefinition> Alerts { get; }

    /// <summary>Gets the complete non-executable runbook catalogue.</summary>
    internal IReadOnlyList<O5R3RunbookDefinition> Runbooks { get; }

    /// <summary>Creates the immutable default O5-R3 catalogue frozen before exercise execution.</summary>
    /// <returns>A validated closed catalogue.</returns>
    internal static O5R3ObservabilityCatalog CreateDefault() =>
        new(DefaultSlos, DefaultAlerts, DefaultRunbooks);

    /// <summary>Returns the alert definition for an exact allow-listed incident and diagnostic pair.</summary>
    /// <param name="incident">Closed incident kind.</param>
    /// <param name="diagnosticCode">Candidate diagnostic code.</param>
    /// <returns>The exact alert definition, or <see langword="null"/> when the pair is not allow-listed.</returns>
    internal O5R3AlertDefinition? FindAlert(O5R3IncidentKind incident, string diagnosticCode) =>
        Alerts.SingleOrDefault(
            alert =>
                alert.Incident == incident &&
                string.Equals(alert.SourceDiagnosticCode, diagnosticCode, StringComparison.Ordinal));

    /// <summary>Returns the numeric SLO for one incident phase.</summary>
    /// <param name="phase">Measured incident phase.</param>
    /// <returns>The unique numeric SLO.</returns>
    internal O5R3SloDefinition SloFor(O5R3ExercisePhase phase) =>
        Slos.Single(slo => slo.Phase == phase);

    /// <summary>Creates one immutable runbook from stable non-executable action identifiers.</summary>
    /// <param name="id">Stable runbook identifier.</param>
    /// <param name="steps">Closed ordered step identifiers.</param>
    /// <returns>The immutable runbook.</returns>
    private static O5R3RunbookDefinition Runbook(string id, params string[] steps) =>
        new(id, Array.AsReadOnly(steps));

    /// <summary>Creates one immutable alert mapping.</summary>
    /// <param name="incident">Closed incident kind.</param>
    /// <param name="sourceDiagnosticCode">Allow-listed O5-R2 diagnostic code.</param>
    /// <param name="alertCode">Stable O5-R3 alert code.</param>
    /// <param name="ownerRole">Functional owner role.</param>
    /// <param name="escalationRole">Functional escalation role.</param>
    /// <param name="runbookId">Exact runbook identifier.</param>
    /// <returns>The immutable alert mapping.</returns>
    private static O5R3AlertDefinition Alert(
        O5R3IncidentKind incident,
        string sourceDiagnosticCode,
        string alertCode,
        string ownerRole,
        string escalationRole,
        string runbookId) =>
        new(incident, sourceDiagnosticCode, alertCode, ownerRole, escalationRole, runbookId);

    /// <summary>Fails closed unless every incident, SLO, owner, escalation and runbook is complete and bounded.</summary>
    private void Validate()
    {
        O5R3IncidentKind[] incidents = Enum.GetValues<O5R3IncidentKind>();
        O5R3ExercisePhase[] phases = Enum.GetValues<O5R3ExercisePhase>();
        if (Slos.Count != phases.Length ||
            Slos.Select(slo => slo.Phase).Distinct().Count() != phases.Length ||
            Alerts.Count != incidents.Length ||
            Alerts.Select(alert => alert.Incident).Distinct().Count() != incidents.Length ||
            Runbooks.Count != incidents.Length ||
            Runbooks.Select(runbook => runbook.Id).Distinct(StringComparer.Ordinal).Count() != incidents.Length)
        {
            throw new InvalidOperationException("o5r3.catalog.incomplete");
        }

        if (Slos.Any(
                slo =>
                    slo.MaximumElapsedMilliseconds <= 0 ||
                    !IsStableIdentifier(slo.Id)))
        {
            throw new InvalidOperationException("o5r3.catalog.slo_unmeasurable");
        }

        if (Alerts.Any(
                alert =>
                    !IsStableIdentifier(alert.SourceDiagnosticCode) ||
                    !IsStableIdentifier(alert.AlertCode) ||
                    !IsStableIdentifier(alert.OwnerRole) ||
                    !IsStableIdentifier(alert.EscalationRole) ||
                    !IsStableIdentifier(alert.RunbookId)) ||
            Alerts.Select(alert => alert.SourceDiagnosticCode).Distinct(StringComparer.Ordinal).Count() != incidents.Length ||
            Alerts.Select(alert => alert.AlertCode).Distinct(StringComparer.Ordinal).Count() != incidents.Length)
        {
            throw new InvalidOperationException("o5r3.catalog.alert_unowned");
        }

        HashSet<string> runbookIds = Runbooks
            .Select(runbook => runbook.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (Alerts.Any(alert => !runbookIds.Contains(alert.RunbookId)) ||
            Runbooks.Any(
                runbook =>
                    !IsStableIdentifier(runbook.Id) ||
                    runbook.Steps.Count is < 3 or > 8 ||
                    runbook.Steps.Any(step => !IsStableIdentifier(step))))
        {
            throw new InvalidOperationException("o5r3.catalog.runbook_missing");
        }
    }

    /// <summary>Accepts only bounded lower-case ASCII identifiers with no payload-bearing characters.</summary>
    /// <param name="value">Candidate identifier.</param>
    /// <returns><see langword="true"/> when the identifier is safe and bounded.</returns>
    private static bool IsStableIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 96 &&
        value.All(
            character =>
                char.IsAsciiLetterLower(character) ||
                char.IsAsciiDigit(character) ||
                character is '.' or '_' or '-');
}

/// <summary>Supplies the exact synthetic timestamps and diagnostic code for one bounded exercise.</summary>
/// <param name="Incident">Closed incident kind.</param>
/// <param name="DiagnosticCode">Exact allow-listed O5-R2 diagnostic code.</param>
/// <param name="OccurredAt">Synthetic incident occurrence time.</param>
/// <param name="DetectedAt">Synthetic detection time.</param>
/// <param name="ContainedAt">Synthetic containment time.</param>
/// <param name="RecoveredAt">Synthetic recovery time.</param>
/// <param name="ClosedAt">Synthetic closure time.</param>
internal sealed record O5R3ExerciseInput(
    O5R3IncidentKind Incident,
    string DiagnosticCode,
    DateTimeOffset OccurredAt,
    DateTimeOffset DetectedAt,
    DateTimeOffset ContainedAt,
    DateTimeOffset RecoveredAt,
    DateTimeOffset ClosedAt);

/// <summary>Records one accepted exercise using only allow-listed codes and numeric measurements.</summary>
/// <param name="Incident">Closed incident kind.</param>
/// <param name="AlertCode">Sanitised alert code.</param>
/// <param name="SourceDiagnosticCode">Allow-listed O5-R2 diagnostic code.</param>
/// <param name="OwnerRole">Functional owner role.</param>
/// <param name="EscalationRole">Functional escalation role.</param>
/// <param name="RunbookId">Non-executable runbook identifier.</param>
/// <param name="DetectedInMilliseconds">Elapsed time from occurrence to detection.</param>
/// <param name="ContainedInMilliseconds">Elapsed time from occurrence to containment.</param>
/// <param name="RecoveredInMilliseconds">Elapsed time from occurrence to recovery.</param>
/// <param name="ClosedInMilliseconds">Elapsed time from occurrence to closure.</param>
/// <param name="ClosedAt">Synthetic closure instant used by bounded retention.</param>
internal sealed record O5R3ExerciseRecord(
    O5R3IncidentKind Incident,
    string AlertCode,
    string SourceDiagnosticCode,
    string OwnerRole,
    string EscalationRole,
    string RunbookId,
    long DetectedInMilliseconds,
    long ContainedInMilliseconds,
    long RecoveredInMilliseconds,
    long ClosedInMilliseconds,
    DateTimeOffset ClosedAt);

/// <summary>Returns one non-authorising exercise decision with an optional accepted sanitised record.</summary>
/// <param name="Accepted">Whether the complete exercise passed every guard and numeric SLO.</param>
/// <param name="Code">Stable sanitised outcome code.</param>
/// <param name="Record">Accepted bounded evidence, or <see langword="null"/> for refusal.</param>
/// <param name="ActivationState">Immutable inactive Observer state.</param>
/// <param name="MayEvaluate">Whether the result grants evaluation authority.</param>
/// <param name="MayPublish">Whether the result grants publication authority.</param>
internal sealed record O5R3ExerciseResult(
    bool Accepted,
    string Code,
    O5R3ExerciseRecord? Record,
    ObserverActivationState ActivationState,
    bool MayEvaluate,
    bool MayPublish);

/// <summary>Provides one bounded sanitised snapshot of retained synthetic exercises.</summary>
/// <param name="CatalogVersion">Immutable catalogue version.</param>
/// <param name="ActivationState">Inactive Observer state.</param>
/// <param name="ExerciseCount">Retained exercise count.</param>
/// <param name="MetricSeriesCount">Bounded incident/phase metric-series count.</param>
/// <param name="Records">Retained allow-listed records.</param>
internal sealed record O5R3ObservabilitySnapshot(
    string CatalogVersion,
    string ActivationState,
    int ExerciseCount,
    int MetricSeriesCount,
    IReadOnlyList<O5R3ExerciseRecord> Records);

/// <summary>
/// Executes O5-R3 synthetic incident exercises, applies predeclared SLOs and retains only bounded sanitised evidence.
/// </summary>
internal sealed class O5R3ObservabilitySandbox
{
    internal const string Marker = "DBNOTIFIER_O5_R3_TEST_ONLY";

    private readonly O5R3ObservabilityCatalog catalog;
    private readonly List<O5R3ExerciseRecord> records = [];

    /// <summary>Initialises the exact opt-in O5-R3 sandbox.</summary>
    /// <param name="marker">Exact test-only marker.</param>
    /// <param name="catalog">Validated immutable catalogue.</param>
    /// <exception cref="InvalidOperationException">Thrown before state allocation when the marker is absent or incorrect.</exception>
    internal O5R3ObservabilitySandbox(string marker, O5R3ObservabilityCatalog catalog)
    {
        if (!string.Equals(marker, Marker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r3.sandbox.marker_required");
        }

        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <summary>Evaluates and records one complete synthetic incident exercise without granting any product authority.</summary>
    /// <param name="input">Closed incident kind, allow-listed code and deterministic UTC timeline.</param>
    /// <param name="observedAt">Current synthetic UTC instant used for retention.</param>
    /// <param name="cancellationToken">Cancellation checked before validation and commit.</param>
    /// <returns>A sanitised accepted or fail-closed decision.</returns>
    internal O5R3ExerciseResult Run(
        O5R3ExerciseInput input,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsUtc(observedAt) ||
            !IsUtc(input.OccurredAt) ||
            !IsUtc(input.DetectedAt) ||
            !IsUtc(input.ContainedAt) ||
            !IsUtc(input.RecoveredAt) ||
            !IsUtc(input.ClosedAt))
        {
            return Refused("o5r3.exercise.invalid_time");
        }

        Prune(observedAt);
        if (input.ClosedAt < observedAt - O5R3ObservabilityCatalog.Retention)
        {
            return Refused("o5r3.observability.expired");
        }

        if (!Enum.IsDefined(input.Incident))
        {
            return Refused("o5r3.exercise.incident_unknown");
        }

        O5R3AlertDefinition? alert = catalog.FindAlert(input.Incident, input.DiagnosticCode);
        if (alert is null)
        {
            return Refused("o5r3.diagnostic.not_allowlisted");
        }

        long[] elapsed =
        [
            Milliseconds(input.DetectedAt - input.OccurredAt),
            Milliseconds(input.ContainedAt - input.OccurredAt),
            Milliseconds(input.RecoveredAt - input.OccurredAt),
            Milliseconds(input.ClosedAt - input.OccurredAt),
        ];
        if (elapsed.Any(value => value < 0) ||
            !elapsed.SequenceEqual(elapsed.Order()))
        {
            return Refused("o5r3.exercise.timeline_invalid");
        }

        O5R3ExercisePhase[] phases = Enum.GetValues<O5R3ExercisePhase>();
        if (phases.Where((phase, index) =>
                elapsed[index] > catalog.SloFor(phase).MaximumElapsedMilliseconds).Any())
        {
            return Refused("o5r3.exercise.slo_failed");
        }

        if (records.Count >= O5R3ObservabilityCatalog.MaximumExerciseRecords)
        {
            return Refused("o5r3.observability.capacity_exhausted");
        }

        cancellationToken.ThrowIfCancellationRequested();
        O5R3ExerciseRecord record = new(
            input.Incident,
            alert.AlertCode,
            alert.SourceDiagnosticCode,
            alert.OwnerRole,
            alert.EscalationRole,
            alert.RunbookId,
            elapsed[0],
            elapsed[1],
            elapsed[2],
            elapsed[3],
            input.ClosedAt);
        records.Add(record);

        if (MetricSeriesCount() > O5R3ObservabilityCatalog.MaximumMetricSeries)
        {
            records.RemoveAt(records.Count - 1);
            return Refused("o5r3.observability.cardinality_exhausted");
        }

        return new O5R3ExerciseResult(
            true,
            "o5r3.exercise.accepted",
            record,
            ObserverActivationState.None,
            false,
            false);
    }

    /// <summary>Returns retained evidence after applying the exact bounded retention window.</summary>
    /// <param name="observedAt">Current synthetic UTC instant.</param>
    /// <returns>An immutable sanitised snapshot.</returns>
    internal O5R3ObservabilitySnapshot Snapshot(DateTimeOffset observedAt)
    {
        if (!IsUtc(observedAt))
        {
            throw new ArgumentException("O5-R3 snapshot time must be an explicit UTC value.", nameof(observedAt));
        }

        Prune(observedAt);
        return new O5R3ObservabilitySnapshot(
            O5R3ObservabilityCatalog.Version,
            ObserverActivationState.None.ToString(),
            records.Count,
            MetricSeriesCount(),
            Array.AsReadOnly(records.ToArray()));
    }

    /// <summary>Creates one fixed sanitised refusal that never reflects untrusted input.</summary>
    /// <param name="code">Allow-listed internal outcome code.</param>
    /// <returns>A non-authorising refusal.</returns>
    private static O5R3ExerciseResult Refused(string code) =>
        new(false, code, null, ObserverActivationState.None, false, false);

    /// <summary>Removes records whose closure lies outside the inclusive retention boundary.</summary>
    /// <param name="observedAt">Current synthetic UTC instant.</param>
    private void Prune(DateTimeOffset observedAt) =>
        records.RemoveAll(record => record.ClosedAt < observedAt - O5R3ObservabilityCatalog.Retention);

    /// <summary>Counts fixed incident/phase series without accepting caller-defined labels.</summary>
    /// <returns>The bounded number of retained metric series.</returns>
    private int MetricSeriesCount() =>
        records.Select(record => record.Incident).Distinct().Count() *
        Enum.GetValues<O5R3ExercisePhase>().Length;

    /// <summary>Converts a non-negative bounded duration to integer milliseconds without overflow.</summary>
    /// <param name="duration">Synthetic elapsed duration.</param>
    /// <returns>Integer milliseconds, or <c>-1</c> for an invalid duration.</returns>
    private static long Milliseconds(TimeSpan duration) =>
        duration < TimeSpan.Zero || duration.TotalMilliseconds > long.MaxValue
            ? -1
            : checked((long)duration.TotalMilliseconds);

    /// <summary>Verifies that a synthetic instant is explicit UTC rather than local or unspecified.</summary>
    /// <param name="value">Candidate timestamp.</param>
    /// <returns><see langword="true"/> for a non-default UTC instant.</returns>
    private static bool IsUtc(DateTimeOffset value) =>
        value != default && value.Offset == TimeSpan.Zero;
}
