// Module purpose: Proves O5-R2 opt-in, kill-switch, crash recovery, quarantine, fencing and cleanup in the exact synthetic test-only sandbox.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises the O5-R2 safety control plane without an Observer runtime, provider, corpus, evaluator or publisher.</summary>
public sealed class O5R2ControlPlaneSandboxTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 15, 0, 0, TimeSpan.Zero);

    /// <summary>Proves the exact marker, dual control, scope binding, expiry and one-use approval contract.</summary>
    [Fact]
    public async Task ExactMarkerAndApprovalsFailClosedOutsideTheirBoundedScope()
    {
        await WithSandboxAsync(
            async fixture =>
            {
                Assert.Throws<InvalidOperationException>(() => fixture.CreateCoordinator("not-the-marker"));
                ObserverOptInApproval valid = fixture.Approval("revision-1");
                await using O5R2ControlPlaneSandbox coordinator = fixture.CreateCoordinator();

                using O5R2SandboxAdmission accepted = await coordinator.TryAdmitAsync(
                    valid,
                    Now.AddSeconds(5));
                using O5R2SandboxAdmission replay = await coordinator.TryAdmitAsync(
                    valid,
                    Now.AddSeconds(5));
                using O5R2SandboxAdmission wrongScope = await coordinator.TryAdmitAsync(
                    fixture.Approval("revision-2", cellId: "OBS-PILOT-PG16-LOCAL-999"),
                    Now.AddSeconds(5));
                using O5R2SandboxAdmission expired = await coordinator.TryAdmitAsync(
                    fixture.Approval(
                        "revision-3",
                        notBefore: Now.AddMinutes(-2),
                        expiresAt: Now.AddMinutes(-1)),
                    Now.AddSeconds(5));
                using O5R2SandboxAdmission crossedKey = await coordinator.TryAdmitAsync(
                    fixture.CrossSignedApproval("revision-4"),
                    Now.AddSeconds(5));

                Assert.True(accepted.Admitted);
                Assert.False(accepted.MayEvaluate);
                Assert.False(accepted.MayPublish);
                Assert.Equal(ObserverActivationState.None, coordinator.ActivationState);
                Assert.False(replay.Admitted);
                Assert.Equal("o5r2.control.approval_replayed", replay.Code);
                Assert.Equal("o5r2.control.scope_mismatch", wrongScope.Code);
                Assert.Equal("o5r2.control.approval_expired", expired.Code);
                Assert.Equal("o5r2.control.approval_unproved", crossedKey.Code);
            });
    }

    /// <summary>Proves the priority kill switch cancels current simulations and blocks every later admission.</summary>
    [Fact]
    public async Task KillSwitchCancelsCurrentContextAndBlocksEveryNewAdmission()
    {
        await WithSandboxAsync(
            async fixture =>
            {
                await using O5R2ControlPlaneSandbox coordinator = fixture.CreateCoordinator();
                using O5R2SandboxAdmission active = await coordinator.TryAdmitAsync(
                    fixture.Approval("active-revision"),
                    Now.AddSeconds(5));
                Assert.True(active.Admitted);

                Task<O5R2Checkpoint> killTask = coordinator.EngageKillSwitchAsync();
                Task<O5R2SandboxAdmission>[] laterAdmissions = Enumerable
                    .Range(0, 100)
                    .Select(index => coordinator.TryAdmitAsync(
                        fixture.Approval($"post-kill-{index}"),
                        Now.AddSeconds(5)))
                    .ToArray();
                O5R2Checkpoint killed = await killTask;
                O5R2SandboxAdmission[] deniedAdmissions = await Task.WhenAll(laterAdmissions);

                Assert.True(killed.KillSwitchEngaged);
                Assert.Null(killed.ActiveContextRevision);
                Assert.True(active.CancellationToken.IsCancellationRequested);
                Assert.False(await coordinator.CanPublishAsync(active.Fence, "active-revision"));
                foreach (O5R2SandboxAdmission denied in deniedAdmissions)
                {
                    using (denied)
                    {
                        Assert.False(denied.Admitted);
                        Assert.Equal("o5r2.control.kill_switch_engaged", denied.Code);
                    }
                }

                Assert.Equal(ObserverActivationState.None, coordinator.ActivationState);
            });
    }

    /// <summary>Proves restart and both crash boundaries recover only the old or new complete inactive state.</summary>
    [Fact]
    public async Task CrashRestartRecoveryAcceptsOnlyOldOrNewCompleteState()
    {
        await WithSandboxAsync(
            async fixture =>
            {
                O5R2Checkpoint initial = await fixture.Store.LoadAsync();
                O5R2Checkpoint first = Next(initial, "before-replace");
                await Assert.ThrowsAsync<O5R2InjectedCrashException>(
                    () => fixture.Store.CommitAsync(
                        initial.Generation,
                        first,
                        O5R2CommitFault.BeforeReplace));
                O5R2Checkpoint oldState = await fixture.Store.LoadAsync();
                Assert.Equal(initial, oldState);

                O5R2Checkpoint second = Next(oldState, "after-replace");
                await Assert.ThrowsAsync<O5R2InjectedCrashException>(
                    () => fixture.Store.CommitAsync(
                        oldState.Generation,
                        second,
                        O5R2CommitFault.AfterReplaceBeforeWitness));
                O5R2Checkpoint newState = await fixture.Store.LoadAsync();
                Assert.Equal(second.ActivationState, newState.ActivationState);
                Assert.Equal(second.Generation, newState.Generation);
                Assert.Equal(second.Fence, newState.Fence);
                Assert.Equal(second.KillSwitchEngaged, newState.KillSwitchEngaged);
                Assert.Equal(second.Quarantined, newState.Quarantined);
                Assert.Equal(second.Code, newState.Code);
                Assert.Equal(second.ActiveContextRevision, newState.ActiveContextRevision);
                Assert.Equal(second.ConsumedApprovalIds, newState.ConsumedApprovalIds);

                await using O5R2ControlPlaneSandbox restarted = fixture.CreateCoordinator();
                O5R2Checkpoint recovered = await restarted.RecoverAsync();
                Assert.Equal("None", recovered.ActivationState);
                Assert.Null(recovered.ActiveContextRevision);
                Assert.False(recovered.Quarantined);
                Assert.Equal(second.Generation + 1, recovered.Generation);
                Assert.False(await restarted.CanPublishAsync(second.Fence, "after-replace"));
            });
    }

    /// <summary>Proves authenticated corruption, rollback, gaps and split views enter durable quarantine.</summary>
    [Theory]
    [InlineData("corruption", "o5r2.store.corrupt")]
    [InlineData("rollback", "o5r2.store.rollback")]
    [InlineData("gap", "o5r2.store.gap")]
    [InlineData("split-view", "o5r2.store.split_view")]
    public async Task ContinuityFailuresQuarantineWithoutActivation(string scenario, string expectedCode)
    {
        await WithSandboxAsync(
            async fixture =>
            {
                O5R2Checkpoint initial = await fixture.Store.LoadAsync();
                O5R2Checkpoint first = await fixture.Store.CommitAsync(
                    initial.Generation,
                    Next(initial, "first"));

                if (scenario == "corruption")
                {
                    await File.WriteAllTextAsync(fixture.Store.StatePath, "not-an-envelope");
                }
                else if (scenario == "rollback")
                {
                    byte[] oldState = await File.ReadAllBytesAsync(fixture.Store.StatePath);
                    await fixture.Store.CommitAsync(first.Generation, Next(first, "second"));
                    await File.WriteAllBytesAsync(fixture.Store.StatePath, oldState);
                }
                else if (scenario == "gap")
                {
                    await fixture.Store.ReplaceStateForTestAsync(
                        first with
                        {
                            Generation = first.Generation + 2,
                            Fence = first.Fence + 2,
                            Code = "gap",
                        });
                }
                else
                {
                    await fixture.Store.ReplaceStateForTestAsync(first with { Code = "divergent" });
                }

                O5R2Checkpoint quarantined = await fixture.Store.LoadAsync();
                Assert.True(quarantined.Quarantined);
                Assert.Equal(expectedCode, quarantined.Code);
                Assert.Equal("None", quarantined.ActivationState);
                Assert.Null(quarantined.ActiveContextRevision);

                await using O5R2ControlPlaneSandbox coordinator = fixture.CreateCoordinator();
                using O5R2SandboxAdmission denied = await coordinator.TryAdmitAsync(
                    fixture.Approval("after-quarantine"),
                    Now.AddSeconds(5));
                Assert.False(denied.Admitted);
                Assert.Equal(expectedCode, denied.Code);
            });
    }

    /// <summary>Proves missing state, deadline, cancellation, stale fencing and rollback all preserve None.</summary>
    [Fact]
    public async Task MissingInvalidAndObsoleteContextsNeverEvaluateOrPublish()
    {
        await WithSandboxAsync(
            async fixture =>
            {
                await using O5R2ControlPlaneSandbox coordinator = fixture.CreateCoordinator();
                O5R2Checkpoint missing = await coordinator.RecoverAsync();
                Assert.Equal(O5R2Checkpoint.Initial(), missing);

                using O5R2SandboxAdmission expired = await coordinator.TryAdmitAsync(
                    fixture.Approval("expired-deadline"),
                    Now);
                Assert.Equal("o5r2.control.deadline_expired", expired.Code);
                using CancellationTokenSource cancelled = new();
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () =>
                    {
                        using O5R2SandboxAdmission _ = await coordinator.TryAdmitAsync(
                            fixture.Approval("cancelled"),
                            Now.AddSeconds(5),
                            cancelled.Token);
                    });

                using O5R2SandboxAdmission admitted = await coordinator.TryAdmitAsync(
                    fixture.Approval("current"),
                    Now.AddSeconds(5));
                O5R2Checkpoint rolledBack = await coordinator.RollbackToNoneAsync();
                Assert.Equal("None", rolledBack.ActivationState);
                Assert.True(rolledBack.KillSwitchEngaged);
                Assert.True(admitted.CancellationToken.IsCancellationRequested);
                Assert.False(await coordinator.CanPublishAsync(admitted.Fence - 1, "obsolete"));
                Assert.False(await coordinator.CanPublishAsync(admitted.Fence, "current"));
            });
    }

    /// <summary>Constructs the next complete checkpoint with a monotonic generation and fence.</summary>
    /// <param name="current">Current complete checkpoint.</param>
    /// <param name="contextRevision">Synthetic context revision left active to exercise recovery.</param>
    /// <returns>Next complete inactive checkpoint.</returns>
    private static O5R2Checkpoint Next(O5R2Checkpoint current, string contextRevision) =>
        current with
        {
            Generation = current.Generation + 1,
            Fence = current.Fence + 1,
            Code = "o5r2.control.synthetic_commit",
            ActiveContextRevision = contextRevision,
        };

    /// <summary>Runs one exact temporary sandbox and proves its root can be removed completely.</summary>
    /// <param name="action">Bounded test action.</param>
    private static async Task WithSandboxAsync(Func<O5R2Fixture, Task> action)
    {
        string root = Path.Combine(Path.GetTempPath(), $"dbnotifier-o5r2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            O5R2Fixture fixture = new(root);
            await action(fixture);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }

            Assert.False(Directory.Exists(root));
        }
    }

    /// <summary>Owns distinct synthetic keys, deterministic time and the exact temporary O5-R2 store.</summary>
    private sealed class O5R2Fixture
    {
        private readonly O5R2SyntheticActivationAuthority authority;
        private readonly FrozenTimeProvider timeProvider = new(Now);

        /// <summary>Initialises fixture-owned synthetic trust material under the exact temporary root.</summary>
        /// <param name="root">Exact temporary root.</param>
        internal O5R2Fixture(string root)
        {
            byte[] stateKey = Enumerable.Repeat((byte)0x5A, 32).ToArray();
            byte[] roleAKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
            byte[] roleBKey = Enumerable.Range(65, 32).Select(value => (byte)value).ToArray();
            Store = new O5R2ControlPlaneStore(root, stateKey);
            authority = new O5R2SyntheticActivationAuthority(
            [
                new(
                    ObserverApprovalRole.ControlApproverA,
                    "o5r2-control-a",
                    roleAKey),
                new(
                    ObserverApprovalRole.ControlApproverB,
                    "o5r2-control-b",
                    roleBKey),
            ]);
        }

        /// <summary>Gets the exact temporary authenticated store.</summary>
        internal O5R2ControlPlaneStore Store { get; }

        /// <summary>Creates a correctly marked coordinator unless a negative marker is explicitly supplied.</summary>
        /// <param name="marker">Exact opt-in marker.</param>
        /// <returns>Test-only coordinator.</returns>
        internal O5R2ControlPlaneSandbox CreateCoordinator(
            string marker = O5R2ControlPlaneSandbox.Marker) =>
            new(marker, Store, authority, timeProvider);

        /// <summary>Creates one exact dual-controlled approval.</summary>
        /// <param name="revision">Context revision.</param>
        /// <param name="cellId">Pilot-cell identifier.</param>
        /// <param name="notBefore">Optional validity start.</param>
        /// <param name="expiresAt">Optional expiry.</param>
        /// <returns>Authenticated bounded approval.</returns>
        internal ObserverOptInApproval Approval(
            string revision,
            string cellId = O5R2ControlPlaneSandbox.PilotCellId,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? expiresAt = null) =>
            authority.Sign(
                Guid.NewGuid(),
                cellId,
                O5R2ControlPlaneSandbox.PilotEnvironment,
                O5R2ControlPlaneSandbox.PilotPurpose,
                revision,
                notBefore ?? Now.AddSeconds(-1),
                expiresAt ?? Now.AddMinutes(1));

        /// <summary>Creates a structurally valid approval signed with one crossed role key.</summary>
        /// <param name="revision">Context revision.</param>
        /// <returns>Unauthenticated crossed-key approval.</returns>
        internal ObserverOptInApproval CrossSignedApproval(string revision)
        {
            ObserverOptInApproval valid = Approval(revision);
            ObserverApprovalSignature first = valid.Signatures[0];
            return new ObserverOptInApproval(
                valid.ApprovalId,
                valid.CellId,
                valid.Environment,
                valid.Purpose,
                valid.Revision,
                valid.NotBefore,
                valid.ExpiresAt,
                [
                    first,
                    new ObserverApprovalSignature(
                        ObserverApprovalRole.ControlApproverB,
                        "o5r2-control-b",
                        first.Signature.ToArray()),
                ]);
        }
    }

    /// <summary>Provides a fixed UTC instant for deterministic approval and deadline decisions.</summary>
    /// <param name="now">Fixed UTC instant.</param>
    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
