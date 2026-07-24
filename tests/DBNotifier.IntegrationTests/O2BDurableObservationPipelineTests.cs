// Module purpose: Proves O2-B durable continuity, bounded backpressure and sanitised observability under faults.
using System.Text.Json;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Exercises only exact temporary O2-B stores and deterministic synthetic observations; no normal composition,
/// operational source, provider, database, credential or external transport is involved.
/// </summary>
public sealed class O2BDurableObservationPipelineTests
{
    private static readonly Guid AgentId = Guid.Parse("b2b10000-0000-4000-8000-000000000001");
    private static readonly Guid InstanceId = Guid.Parse("b2b20000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset NowUtc = O1SyntheticTrustFixture.NowUtc;

    /// <summary>Proves complete publication survives restart and exact replay never duplicates it.</summary>
    [Fact]
    public async Task RestartAndReplayPreserveExactlyOneDurablePublication()
    {
        await WithRootAsync(
            async root =>
            {
                ObservationSyncMessage message = Message(1);
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    ObservationBatchResult accepted = await SendAsync(first, message);
                    Assert.Equal(ObservationIngestionDisposition.Accepted, Assert.Single(accepted.Items).Disposition);
                    Assert.Equal(1, accepted.HighestContiguousSequence);
                    Assert.Equal(1, first.TotalPublications);
                    Assert.False(Assert.Single(first.Publications).Authorising);
                    Assert.Equal("resource.released", first.LastResourceReleaseCode);
                }

                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                ObservationBatchResult replay = await SendAsync(reopened, message);
                Assert.Equal(ObservationIngestionDisposition.Duplicate, Assert.Single(replay.Items).Disposition);
                Assert.Equal(1, replay.HighestContiguousSequence);
                Assert.Equal(1, reopened.TotalPublications);
                Assert.Single(reopened.Publications);
            });
    }

    /// <summary>Proves a durable gap survives restart and later closes in exact sequence without duplicate publication.</summary>
    [Fact]
    public async Task GapAndReorderSurviveRestartAndPublishInOrder()
    {
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    ObservationBatchResult gap = await SendAsync(first, Message(2));
                    Assert.Equal(0, gap.HighestContiguousSequence);
                    Assert.Equal(0, first.TotalPublications);
                    Assert.Equal(1, first.Observe().PendingCount);
                }

                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                ObservationBatchResult closed = await SendAsync(reopened, Message(1));
                Assert.Equal(2, closed.HighestContiguousSequence);
                Assert.Equal(2, reopened.TotalPublications);
                Assert.Equal([1L, 2L], reopened.Publications.Select(item => item.Sequence));
                Assert.Equal(0, reopened.Observe().PendingCount);
            });
    }

    /// <summary>Proves a superseded O1 context remains monotonic after restart and stale work is never published.</summary>
    [Fact]
    public async Task ContextSupersessionRemainsDurableAcrossRestart()
    {
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    first.BeforePublicationAsync = first.AdvanceTrustAsync;
                    await SendAsync(first, Message(1));
                    Assert.Equal(0, first.TotalPublications);
                    Assert.Equal("o2a-context-2", first.DurableContextRevision);
                }
                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                Assert.Equal("o2a-context-2", reopened.DurableContextRevision);
                await SendAsync(reopened, Message(2));
                Assert.Equal(1, reopened.TotalPublications);
                Assert.Equal("o2a-context-2", reopened.DurableContextRevision);
            });
    }

    /// <summary>Proves old-or-new atomic recovery at every authorised admission and completion crash boundary.</summary>
    [Theory]
    [InlineData((int)O2BCommitFault.BeforeReplace, false, 0)]
    [InlineData((int)O2BCommitFault.AfterReplaceBeforeWitness, true, 1)]
    public async Task AtomicCompletionRecoversOnlyOldOrNewCompleteState(
        int faultValue,
        bool completedBeforeRestart,
        long expectedPublications)
    {
        O2BCommitFault fault = (O2BCommitFault)faultValue;
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    first.NextCompletionFault = fault;
                    await Assert.ThrowsAsync<O2BInjectedCrashException>(
                        async () => await SendAsync(first, Message(1)));
                }

                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                Assert.Equal(1, await reopened.GetHighestContiguousSequenceAsync(AgentId, default));
                Assert.Equal(1, reopened.TotalPublications);
                Assert.Single(reopened.Publications);
                Assert.Equal(
                    completedBeforeRestart ? expectedPublications : 1,
                    reopened.TotalPublications);
                ObservationItemResult replay = Assert.Single((await SendAsync(reopened, Message(1))).Items);
                Assert.Equal(ObservationIngestionDisposition.Duplicate, replay.Disposition);
                Assert.Equal(1, reopened.TotalPublications);
            });
    }

    /// <summary>Proves a crash before admission replacement leaves the old complete state with no phantom pending item.</summary>
    [Fact]
    public async Task AdmissionCrashBeforeReplaceLeavesOldCompleteState()
    {
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    first.NextAdmissionFault = O2BCommitFault.BeforeReplace;
                    await Assert.ThrowsAsync<O2BInjectedCrashException>(
                        async () => await SendAsync(first, Message(1)));
                }
                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                Assert.Equal(0, await reopened.GetHighestContiguousSequenceAsync(AgentId, default));
                Assert.Equal(0, reopened.TotalPublications);
                Assert.Equal(0, reopened.Observe().PendingCount);
            });
    }

    /// <summary>Proves a process interruption after durable admission resumes pending work exactly once after restart.</summary>
    [Fact]
    public async Task DurablePendingResumesExactlyOnceAfterSyntheticProcessInterruption()
    {
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    first.BeforeProcessingAsync = _ =>
                        throw new O2BInjectedCrashException(O2BCommitFault.BeforeReplace);
                    await Assert.ThrowsAsync<O2BInjectedCrashException>(
                        async () => await SendAsync(first, Message(1)));
                }
                await using O2BDurableObservationPipeline reopened = await CreateAsync(root);
                Assert.Equal(1, await reopened.GetHighestContiguousSequenceAsync(AgentId, default));
                Assert.Equal(1, reopened.TotalPublications);
                Assert.Single(reopened.Publications);
            });
    }

    /// <summary>Proves missing, corrupt and rolled-back state enter stable quarantine and reject every ingress.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("rollback")]
    public async Task MissingCorruptAndRolledBackStateFailClosed(string scenario)
    {
        await WithRootAsync(
            async root =>
            {
                O2BDurablePipelineStore store = new(root);
                await using (O2BDurableObservationPipeline pipeline = await CreateAsync(root))
                {
                    await SendAsync(pipeline, Message(1));
                }
                byte[] oldState = await File.ReadAllBytesAsync(store.StatePath);
                byte[] oldWitness = await File.ReadAllBytesAsync(store.WitnessPath);

                await using (O2BDurableObservationPipeline advanced = await CreateAsync(root))
                {
                    await SendAsync(advanced, Message(2));
                }
                if (scenario == "missing")
                {
                    File.Delete(store.WitnessPath);
                }
                else if (scenario == "corrupt")
                {
                    await File.WriteAllTextAsync(store.StatePath, "{corrupt");
                }
                else
                {
                    await File.WriteAllBytesAsync(store.StatePath, oldState);
                    Assert.NotEqual(oldWitness, await File.ReadAllBytesAsync(store.WitnessPath));
                }

                await using O2BDurableObservationPipeline quarantined = await CreateAsync(root);
                Assert.True(quarantined.Observe().Quarantined);
                Assert.StartsWith("o2b.store.", quarantined.QuarantineCode, StringComparison.Ordinal);
                ObservationItemResult refusal = Assert.Single((await SendAsync(quarantined, Message(3))).Items);
                Assert.Equal(ObservationIngestionDisposition.Rejected, refusal.Disposition);
                Assert.Equal(quarantined.QuarantineCode, refusal.ErrorCode);
            });
    }

    /// <summary>Proves a newer session fence makes every older writer fail closed without corrupting the active stream.</summary>
    [Fact]
    public async Task StaleWriterFenceCannotCommit()
    {
        await WithRootAsync(
            async root =>
            {
                await using O2BDurableObservationPipeline stale = await CreateAsync(root);
                await using O2BDurableObservationPipeline current = await CreateAsync(root);
                O2BStoreRefusalException refusal = await Assert.ThrowsAsync<O2BStoreRefusalException>(
                    async () => await SendAsync(stale, Message(1)));
                Assert.Equal("o2b.store.stale_fence", refusal.Code);

                ObservationBatchResult accepted = await SendAsync(current, Message(1));
                Assert.Equal(1, accepted.HighestContiguousSequence);
                Assert.Equal(1, current.TotalPublications);
            });
    }

    /// <summary>Proves the no-queue concurrency boundary and durable pending capacity refuse excess work safely.</summary>
    [Fact]
    public async Task BusyAndSaturatedAdmissionRemainBoundedAndObservable()
    {
        await WithRootAsync(
            async root =>
            {
                await using O2BDurableObservationPipeline pipeline = await CreateAsync(root);
                TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
                pipeline.BeforeProcessingAsync = async cancellationToken =>
                {
                    entered.SetResult();
                    await release.Task.WaitAsync(cancellationToken);
                };
                Task<ObservationBatchResult> first = SendAsync(pipeline, Message(1));
                await entered.Task;
                ObservationItemResult busy = Assert.Single((await SendAsync(pipeline, Message(2))).Items);
                Assert.Equal(ObservationIngestionDisposition.Retryable, busy.Disposition);
                Assert.Equal("o2b.backpressure.busy", busy.ErrorCode);
                release.SetResult();
                await first;

                for (long sequence = 3; sequence <= 10; sequence++)
                {
                    ObservationItemResult accepted = Assert.Single((await SendAsync(pipeline, Message(sequence))).Items);
                    Assert.Equal(ObservationIngestionDisposition.Accepted, accepted.Disposition);
                }
                ObservationItemResult saturated = Assert.Single((await SendAsync(pipeline, Message(11))).Items);
                Assert.Equal(ObservationIngestionDisposition.Retryable, saturated.Disposition);
                Assert.Equal("o2b.backpressure.saturated", saturated.ErrorCode);
                O2BObservabilitySnapshot observed = pipeline.Observe();
                Assert.Equal(O2BDurablePipelineStore.MaximumPending, observed.PendingCount);
                Assert.Contains(observed.Counters, item => item.Code == "o2b.backpressure.busy");
                Assert.Contains(observed.Counters, item => item.Code == "o2b.backpressure.saturated");
            });
    }

    /// <summary>Proves cancellation leaves durable pending work and deadline refusal advances without partial publication.</summary>
    [Fact]
    public async Task CancellationResumesAndDeadlineFailsClosedWithoutPartialPublication()
    {
        await WithRootAsync(
            async root =>
            {
                await using (O2BDurableObservationPipeline first = await CreateAsync(root))
                {
                    TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    first.BeforeProcessingAsync = async cancellationToken =>
                    {
                        entered.SetResult();
                        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    };
                    using CancellationTokenSource cancellation = new();
                    Task<ObservationBatchResult> send = SendAsync(first, Message(1), cancellation.Token);
                    await entered.Task;
                    cancellation.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await send);
                }
                await using (O2BDurableObservationPipeline resumed = await CreateAsync(root))
                {
                    Assert.Equal(1, resumed.TotalPublications);
                }
                await using O2BDurableObservationPipeline deadline = await CreateAsync(root);
                deadline.NextDeadlineUtc = NowUtc;
                await SendAsync(deadline, Message(2));
                Assert.Equal(2, await deadline.GetHighestContiguousSequenceAsync(AgentId, default));
                Assert.Equal(1, deadline.TotalPublications);
                Assert.Contains(
                    deadline.Observe().Counters,
                    item => item.Code == "o2b.pipeline.completed_no_publication");
            });
    }

    /// <summary>Proves retained detail remains bounded while aggregate publication truth remains exact.</summary>
    [Fact]
    public async Task RetentionIsBoundedAndObservabilityContainsOnlyCodesAndCounters()
    {
        await WithRootAsync(
            async root =>
            {
                await using O2BDurableObservationPipeline pipeline = await CreateAsync(root);
                for (long sequence = 1; sequence <= 72; sequence++)
                {
                    await SendAsync(pipeline, Message(sequence));
                }
                O2BObservabilitySnapshot observed = pipeline.Observe();
                Assert.Equal(72, observed.TotalPublications);
                Assert.Equal(O2BDurablePipelineStore.MaximumOutcomes, observed.RetainedOutcomeCount);
                Assert.Equal(O2BDurablePipelineStore.MaximumPublications, observed.RetainedPublicationCount);
                O2BStoreSnapshot durable = Assert.IsType<O2BStoreSnapshot>(
                    await new O2BDurablePipelineStore(root).LoadAsync());
                Assert.Equal(O2BDurablePipelineStore.MaximumDigests, durable.Payload.MessageDigests.Count);
                Assert.Equal(O2BDurablePipelineStore.MaximumDigests, durable.Payload.ObservationDigests.Count);
                string json = JsonSerializer.Serialize(observed);
                Assert.DoesNotContain("synthetic-db", json, StringComparison.Ordinal);
                Assert.DoesNotContain(AgentId.ToString(), json, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(InstanceId.ToString(), json, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Provider", json, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Message", json, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Payload", json, StringComparison.OrdinalIgnoreCase);
            });
    }

    /// <summary>Proves the exact process bridge accepts its marker and produces a durable aggregate reference result.</summary>
    [Fact]
    public async Task ExactProcessBridgeCompletesReferenceRun()
    {
        await WithRootAsync(
            async root =>
            {
                int exitCode = await O2BSandboxProcess.RunAsync(
                    [
                        "--activation",
                        O2BDurableObservationPipeline.ActivationMarker,
                        "--operation",
                        "reference",
                        "--root",
                        root,
                    ]);
                Assert.Equal(0, exitCode);
            });
    }

    /// <summary>Creates one deterministic pipeline below the caller's exact temporary root.</summary>
    private static Task<O2BDurableObservationPipeline> CreateAsync(string root) =>
        O2BDurableObservationPipeline.CreateAsync(
            root,
            AgentId,
            InstanceId,
            new O2AFixedTimeProvider(NowUtc));

    /// <summary>Sends one message through the production Server batch boundary.</summary>
    private static async Task<ObservationBatchResult> SendAsync(
        O2BDurableObservationPipeline pipeline,
        ObservationSyncMessage message,
        CancellationToken cancellationToken = default)
    {
        ObservationBatchIngestor server = new(pipeline, new O2AFixedTimeProvider(NowUtc));
        return await server.HandleAsync(
            new ObservationBatchRequest(AgentId, [message]),
            cancellationToken);
    }

    /// <summary>Creates one stable synthetic observation at the requested sequence.</summary>
    private static ObservationSyncMessage Message(long sequence) =>
        O2ASyntheticObservation.Create(
            AgentId,
            InstanceId,
            sequence,
            NowUtc,
            HealthStatus.Degraded,
            messageId: DeterministicGuid(sequence, 3),
            observationId: DeterministicGuid(sequence, 4));

    /// <summary>Creates a stable UUID-shaped identifier without relying on random test data.</summary>
    private static Guid DeterministicGuid(long sequence, byte discriminator)
    {
        byte[] bytes = new byte[16];
        bytes[0] = 0xb2;
        bytes[1] = discriminator;
        BitConverter.GetBytes(sequence).CopyTo(bytes, 8);
        return new Guid(bytes);
    }

    /// <summary>Owns and removes one direct O2-B operating-system temporary child around an asynchronous test.</summary>
    private static async Task WithRootAsync(Func<string, Task> action)
    {
        string root = Path.Combine(Path.GetTempPath(), $"DBNotifier-O2B-{Guid.NewGuid():N}");
        try
        {
            await action(root);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
