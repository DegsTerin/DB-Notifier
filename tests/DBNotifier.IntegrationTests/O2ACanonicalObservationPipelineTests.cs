// Module purpose: Proves the complete O2-A synthetic Agent-to-MOD-12 chain and every authorised fail-closed boundary.
using DBNotifier.Application.AIOps;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Exercises the canonical read-only observation pipeline exclusively below the exact O2-A test marker, with synthetic
/// evidence, deterministic clocks and caller-owned temporary storage.
/// </summary>
public sealed class O2ACanonicalObservationPipelineTests
{
    private static readonly DateTimeOffset NowUtc = O1SyntheticTrustFixture.NowUtc;
    private static readonly Guid AgentId = Guid.Parse("a2a10000-0000-4000-8000-000000000001");
    private static readonly Guid InstanceId = Guid.Parse("a2a20000-0000-4000-8000-000000000001");

    /// <summary>Proves the real Agent and Server boundaries publish only a complete, non-authorising MOD-12 report.</summary>
    [Fact]
    public async Task AgentServerO1AndMod12PublishOnlyCompleteNonAuthorisingReports()
    {
        await WithPipelineAsync(
            async (pipeline, clock) =>
            {
                O2AAgentOutboxStore outbox = new();
                outbox.Add(O2ASyntheticObservation.Create(AgentId, InstanceId, 1, NowUtc));
                AgentOutboxDispatchResult dispatch = await new AgentOutboxDispatchRunner(
                        AgentId,
                        outbox,
                        new O2ALoopbackObservationTransport(new ObservationBatchIngestor(pipeline, clock)),
                        clock)
                    .RunOnceAsync(10);

                Assert.Equal(1, dispatch.PendingCount);
                Assert.Equal(1, dispatch.AcknowledgedCount);
                Assert.Equal(0, dispatch.RetryableCount);
                Assert.Equal(0, outbox.PendingCount);
                ObserverAnalysisReport report = Assert.Single(pipeline.PublishedReports);
                Assert.True(report.IsComplete);
                Assert.False(report.IsAuthorising);
                Assert.Equal(ObserverAnalysisDisposition.Completed, report.Disposition);
                Assert.Equal("aiops.observer.analysis.completed", report.Code);
                Assert.Equal("observer-analysis", report.Capabilities.Capability);
                Assert.Equal(ObserverActivationState.None, report.Capabilities.ActivationState);
                Assert.False(report.Capabilities.CollectsEvidence);
                Assert.False(report.Capabilities.PersistsEvidence);
                Assert.False(report.Capabilities.UsesLlm);
                Assert.False(report.Capabilities.CanRecommend);
                Assert.False(report.Capabilities.CanPlan);
                Assert.False(report.Capabilities.CanExecute);
                O2AProcessingOutcome outcome = Assert.Single(pipeline.Outcomes);
                Assert.Matches("^[A-Fa-f0-9]{64}$", outcome.EnvelopeDigest);
                Assert.Equal("o2a-context-1", outcome.ContextRevision);
                Assert.Equal("resource.released", pipeline.LastResourceReleaseCode);
            });
    }

    /// <summary>Proves the executable envelope declares every required provider-neutral contract group.</summary>
    [Fact]
    public void CanonicalEnvelopeDeclaresIdentityScopeVersionTimeResultProvenanceAndCompleteness()
    {
        string[] properties = typeof(O2ACanonicalObservationEnvelope)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ContractVersion), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.MessageId), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.AgentId), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.InstanceId), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.AuthorisationScopeId), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.Environment), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ObservedAtUtc), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ReceivedAtUtc), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.TransformationProfile), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.MetricFamily), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.Unit), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.AccountingProfile), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.SourceDigest), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ContextRevision), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.Classification), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.RedactionStatus), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.RetentionClass), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.PermittedPurpose), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ExpectedItemCount), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.ActualItemCount), properties);
        Assert.Contains(nameof(O2ACanonicalObservationEnvelope.MissingFields), properties);
        Assert.DoesNotContain(properties, property => property.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("Credential", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("Query", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("Sql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, property => property.Contains("Command", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Proves gap retention, reorder recovery, exact duplicates, replay and identity conflicts.</summary>
    [Fact]
    public async Task DuplicateReplayReorderAndGapRemainIdempotentAndContiguous()
    {
        await WithPipelineAsync(
            async (pipeline, clock) =>
            {
                ObservationBatchIngestor server = new(pipeline, clock);
                ObservationSyncMessage second = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    2,
                    NowUtc,
                    messageId: Guid.Parse("a2a30000-0000-4000-8000-000000000002"),
                    observationId: Guid.Parse("a2a40000-0000-4000-8000-000000000002"));

                ObservationBatchResult gap = await SendAsync(server, second);
                Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(gap.Items).Disposition);
                Assert.Equal(0, gap.HighestContiguousSequence);
                Assert.Empty(pipeline.PublishedReports);

                ObservationBatchResult duplicateWhilePending = await SendAsync(server, second);
                Assert.Equal(
                    ObservationIngestionDisposition.Duplicate,
                    Assert.Single(duplicateWhilePending.Items).Disposition);
                Assert.Equal(0, duplicateWhilePending.HighestContiguousSequence);

                ObservationSyncMessage first = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    1,
                    NowUtc,
                    messageId: Guid.Parse("a2a30000-0000-4000-8000-000000000001"),
                    observationId: Guid.Parse("a2a40000-0000-4000-8000-000000000001"));
                ObservationBatchResult reordered = await SendAsync(server, first);
                Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(reordered.Items).Disposition);
                Assert.Equal(2, reordered.HighestContiguousSequence);
                Assert.Equal([1L, 2L], pipeline.Outcomes.Select(outcome => outcome.Sequence));
                Assert.Equal(2, pipeline.PublishedReports.Count);

                ObservationBatchResult replay = await SendAsync(server, first);
                Assert.Equal(ObservationIngestionDisposition.Duplicate, Assert.Single(replay.Items).Disposition);
                Assert.Equal(2, replay.HighestContiguousSequence);
                Assert.Equal(2, pipeline.PublishedReports.Count);

                ObservationSyncMessage messageConflict = first with { DurationMilliseconds = 101 };
                ObservationItemResult conflictedMessage = Assert.Single((await SendAsync(server, messageConflict)).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, conflictedMessage.Disposition);
                Assert.Equal("o2a.idempotency_conflict", conflictedMessage.ErrorCode);

                ObservationSyncMessage observationConflict = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    3,
                    NowUtc,
                    observationId: first.ObservationId);
                ObservationItemResult conflictedObservation = Assert.Single(
                    (await SendAsync(server, observationConflict)).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, conflictedObservation.Disposition);
                Assert.Equal("o2a.observation_conflict", conflictedObservation.ErrorCode);
                Assert.Equal(2, pipeline.PublishedReports.Count);
            });
    }

    /// <summary>Proves contract, future-skew, unsupported-error, freshness and signed-revocation boundaries.</summary>
    [Fact]
    public async Task ContractFutureFreshnessAndRevocationFailClosed()
    {
        await WithPipelineAsync(
            async (pipeline, clock) =>
            {
                ObservationBatchIngestor server = new(pipeline, clock);
                ObservationSyncMessage invalidContract = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    1,
                    NowUtc,
                    schemaVersion: 2);
                ObservationItemResult invalid = Assert.Single((await SendAsync(server, invalidContract)).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, invalid.Disposition);
                Assert.Equal("observation.envelope_invalid", invalid.ErrorCode);

                ObservationSyncMessage future = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    1,
                    NowUtc.AddMinutes(6));
                ObservationItemResult futureResult = Assert.Single((await SendAsync(server, future)).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, futureResult.Disposition);
                Assert.Equal("observation.observed_at_future", futureResult.ErrorCode);

                ObservationSyncMessage unsupportedError = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    1,
                    NowUtc) with
                {
                    Status = HealthStatus.Timeout.ToString(),
                    ErrorCode = "synthetic.timeout",
                    SafeErrorMessage = "Synthetic bounded timeout.",
                };
                ObservationItemResult unsupported = Assert.Single((await SendAsync(server, unsupportedError)).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, unsupported.Disposition);
                Assert.Equal("o2a.canonical.error_contract_unsupported", unsupported.ErrorCode);

                ObservationSyncMessage stale = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    1,
                    NowUtc.AddHours(-1));
                ObservationItemResult staleResult = Assert.Single((await SendAsync(server, stale)).Items);
                Assert.Equal(ObservationIngestionDisposition.Accepted, staleResult.Disposition);
                ObserverAnalysisReport staleReport = Assert.Single(pipeline.PublishedReports);
                ObserverThresholdResult threshold = Assert.Single(staleReport.ThresholdResults);
                Assert.Equal(ObserverFindingDisposition.InsufficientEvidence, threshold.Disposition);
                Assert.False(staleReport.IsAuthorising);

                pipeline.RevokePolicy();
                ObservationSyncMessage revoked = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    2,
                    NowUtc);
                ObservationItemResult revokedResult = Assert.Single((await SendAsync(server, revoked)).Items);
                Assert.Equal(ObservationIngestionDisposition.Accepted, revokedResult.Disposition);
                O2AProcessingOutcome revokedOutcome = pipeline.Outcomes.Single(outcome => outcome.Sequence == 2);
                Assert.Equal("aiops.observer.adapter.provenance_revoked", revokedOutcome.Code);
                Assert.False(revokedOutcome.Published);
                Assert.Null(revokedOutcome.Report);
                Assert.Single(pipeline.PublishedReports);
            });
    }

    /// <summary>Proves context supersession, deadline, resource, cancellation and safe retry behaviour.</summary>
    [Fact]
    public async Task SupersessionDeadlineCancellationAndResourceLimitsNeverPublishPartialResults()
    {
        await WithPipelineAsync(
            async (pipeline, clock) =>
            {
                ObservationBatchIngestor server = new(pipeline, clock);
                pipeline.BeforePublicationAsync = pipeline.AdvanceTrustAsync;
                ObservationItemResult superseded = Assert.Single(
                    (await SendAsync(
                        server,
                        O2ASyntheticObservation.Create(AgentId, InstanceId, 1, NowUtc))).Items);
                Assert.Equal(ObservationIngestionDisposition.Accepted, superseded.Disposition);
                O2AProcessingOutcome staleContext = Assert.Single(pipeline.Outcomes);
                Assert.Equal("aiops.observer.analysis.context_stale", staleContext.Code);
                Assert.False(staleContext.Published);
                Assert.False(staleContext.Report!.IsComplete);
                Assert.Empty(pipeline.PublishedReports);

                pipeline.NextDeadlineUtc = NowUtc;
                await SendAsync(
                    server,
                    O2ASyntheticObservation.Create(AgentId, InstanceId, 2, NowUtc));
                Assert.Equal("resource.deadline", pipeline.Outcomes.Single(outcome => outcome.Sequence == 2).Code);
                Assert.Empty(pipeline.PublishedReports);

                pipeline.NextResourceRequest = new O1ResourceRequest(
                    (64 * 1024) + 1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1);
                await SendAsync(
                    server,
                    O2ASyntheticObservation.Create(AgentId, InstanceId, 3, NowUtc));
                Assert.Equal(
                    "resource.input_bytes",
                    pipeline.Outcomes.Single(outcome => outcome.Sequence == 3).Code);
                Assert.Empty(pipeline.PublishedReports);

                using CancellationTokenSource cancellation = new();
                pipeline.BeforePublicationAsync = _ =>
                {
                    cancellation.Cancel();
                    return Task.CompletedTask;
                };
                ObservationSyncMessage interrupted = O2ASyntheticObservation.Create(
                    AgentId,
                    InstanceId,
                    4,
                    NowUtc,
                    messageId: Guid.Parse("a2a30000-0000-4000-8000-000000000004"),
                    observationId: Guid.Parse("a2a40000-0000-4000-8000-000000000004"));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await SendAsync(server, interrupted, cancellation.Token));
                Assert.Equal(3, await pipeline.GetHighestContiguousSequenceAsync(AgentId, default));
                Assert.Empty(pipeline.PublishedReports);
                Assert.Equal("resource.released", pipeline.LastResourceReleaseCode);

                ObservationBatchResult retried = await SendAsync(server, interrupted);
                Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(retried.Items).Disposition);
                Assert.Equal(4, retried.HighestContiguousSequence);
                ObserverAnalysisReport recovered = Assert.Single(pipeline.PublishedReports);
                Assert.True(recovered.IsComplete);
                Assert.False(recovered.IsAuthorising);
            });
    }

    /// <summary>Proves the public sandbox entry point accepts only its exact marker and bounded temporary root.</summary>
    [Fact]
    public async Task ExactMarkerRunsReferenceOperationAndRejectsAnyOtherActivation()
    {
        Assert.Equal(
            2,
            await O2ASandboxProcess.RunAsync(
                ["--activation", "observer", "--operation", "reference", "--root", Path.GetTempPath()]));

        string root = CreateRoot();
        try
        {
            Assert.Equal(
                0,
                await O2ASandboxProcess.RunAsync(
                    [
                        "--activation",
                        O2ACanonicalObservationPipeline.ActivationMarker,
                        "--operation",
                        "reference",
                        "--root",
                        root,
                    ]));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>Sends one message through the production Server boundary using the exact synthetic Agent route.</summary>
    private static ValueTask<ObservationBatchResult> SendAsync(
        ObservationBatchIngestor server,
        ObservationSyncMessage message,
        CancellationToken cancellationToken = default) =>
        server.HandleAsync(new ObservationBatchRequest(AgentId, [message]), cancellationToken);

    /// <summary>Creates and cleans one exact synthetic O2-A root around a bounded test body.</summary>
    private static async Task WithPipelineAsync(
        Func<O2ACanonicalObservationPipeline, O2AFixedTimeProvider, Task> action)
    {
        string root = CreateRoot();
        try
        {
            O2AFixedTimeProvider clock = new(NowUtc);
            using O2ACanonicalObservationPipeline pipeline =
                await O2ACanonicalObservationPipeline.CreateAsync(root, AgentId, InstanceId, clock);
            await action(pipeline, clock);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    /// <summary>Returns an exact, unique path below the operating-system temporary root.</summary>
    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), $"DBNotifier-O2A-{Guid.NewGuid():N}");

    /// <summary>Removes only the exact caller-owned O2-A temporary root and proves its absence.</summary>
    private static void DeleteRoot(string root)
    {
        string canonical = Path.GetFullPath(root);
        string temporary = Path.GetFullPath(Path.GetTempPath());
        if (!canonical.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(canonical).StartsWith("DBNotifier-O2A-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o2a.test.cleanup_scope_invalid");
        }
        if (Directory.Exists(canonical))
        {
            Directory.Delete(canonical, recursive: true);
        }
        Assert.False(Directory.Exists(canonical));
    }
}
