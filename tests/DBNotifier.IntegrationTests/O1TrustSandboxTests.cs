// Module purpose: Proves O1 trust continuity, atomic recovery, bounded resource admission, corpus authority and exact test-only multiprocess fencing.
using System.Diagnostics;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises the complete synthetic O1 sandbox without normal composition, operational data or persisted private material.</summary>
public sealed class O1TrustSandboxTests
{
    /// <summary>Proves direct continuity and fail-closed rollback, generation-gap, split-view and exact-idempotent replay behaviour.</summary>
    [Fact]
    public async Task TrustContinuityRejectsRollbackGapDivergenceAndReplay()
    {
        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                O1TrustBundle bootstrapBundle = fixture.Bootstrap();
                O1TrustAdmissionResult bootstrap = await AdmitAsync(
                    coordinator,
                    fixture,
                    bootstrapBundle,
                    "bootstrap");
                Assert.True(bootstrap.Accepted);
                Assert.False(bootstrap.Quarantined);

                O1TrustAdmissionResult exactReplay = await AdmitAsync(
                    coordinator,
                    fixture,
                    bootstrapBundle,
                    "exact-replay");
                Assert.True(exactReplay.Accepted);
                Assert.True(exactReplay.Idempotent);
                Assert.Equal(bootstrap.State.ContextRevision, exactReplay.State.ContextRevision);

                O1TrustAdmissionResult successor = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Successor(bootstrap.State),
                    "successor");
                Assert.True(successor.Accepted);
                Assert.Equal(2, successor.State.Generation);

                O1TrustAdmissionResult rollback = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Rollback(successor.State),
                    "rollback");
                Assert.False(rollback.Accepted);
                Assert.Equal("trust.rollback", rollback.Code);
                Assert.True(rollback.Quarantined);
            });

        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                O1TrustAdmissionResult bootstrap = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Bootstrap(),
                    "bootstrap-gap");
                O1TrustAdmissionResult gap = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Gap(bootstrap.State),
                    "gap");
                Assert.False(gap.Accepted);
                Assert.True(gap.Quarantined);
                Assert.Equal("trust.generation_gap", gap.Code);
                Assert.True((await store.LoadAsync()).Quarantined);
            });

        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                O1TrustAdmissionResult bootstrap = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Bootstrap(),
                    "bootstrap-split");
                O1TrustAdmissionResult split = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Divergence(bootstrap.State),
                    "split");
                Assert.False(split.Accepted);
                Assert.True(split.Quarantined);
                Assert.Equal("trust.split_view", split.Code);
            });
    }

    /// <summary>Proves independent role keys, exact scope, dual control, signature integrity, deadlines and cancellation.</summary>
    [Fact]
    public async Task CryptographicRolesScopeAndDualControlFailClosed()
    {
        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                O1TrustBundle valid = fixture.Bootstrap();
                O1ApprovalSignature first = valid.PolicyApproval.Signatures[0];
                O1DualApproval crossedApproval = valid.PolicyApproval with
                {
                    Signatures = Array.AsReadOnly(
                    [
                        first,
                        first with { Role = O1TrustRole.PolicyApproverB },
                    ]),
                };
                O1TrustAdmissionResult crossed = await AdmitAsync(
                    coordinator,
                    fixture,
                    valid with { PolicyApproval = crossedApproval },
                    "crossed-role");
                Assert.False(crossed.Accepted);
                Assert.Equal("trust.authorisation_unproved", crossed.Code);

                O1TrustAdmissionResult wrongScope = await coordinator.AdmitAsync(
                    valid,
                    ["instance:other", "purpose:capacity"],
                    O1SyntheticTrustFixture.NowUtc.AddMinutes(1),
                    "wrong-scope");
                Assert.False(wrongScope.Accepted);
                Assert.Equal("trust.scope_mismatch", wrongScope.Code);

                O1TrustAdmissionResult badSignature = await AdmitAsync(
                    coordinator,
                    fixture,
                    valid with { BundleSignature = Convert.ToBase64String([1, 2, 3]) },
                    "bad-signature");
                Assert.False(badSignature.Accepted);
                Assert.Equal("trust.bundle_signature_invalid", badSignature.Code);

                O1TrustAdmissionResult deadline = await coordinator.AdmitAsync(
                    valid,
                    O1SyntheticTrustFixture.Scope,
                    O1SyntheticTrustFixture.NowUtc,
                    "deadline");
                Assert.False(deadline.Accepted);
                Assert.Equal("trust.deadline", deadline.Code);

                using CancellationTokenSource cancelled = new();
                cancelled.Cancel();
                O1TrustAdmissionResult cancellation = await coordinator.AdmitAsync(
                    valid,
                    O1SyntheticTrustFixture.Scope,
                    O1SyntheticTrustFixture.NowUtc.AddMinutes(1),
                    "cancelled",
                    cancellationToken: cancelled.Token);
                Assert.False(cancellation.Accepted);
                Assert.Equal("trust.cancelled", cancellation.Code);
            });
    }

    /// <summary>Proves old-or-new atomic state across injected crashes, witness repair, rollback quarantine and separately rooted recovery.</summary>
    [Fact]
    public async Task CrashRestartAndRecoveryPreserveAtomicContinuity()
    {
        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                await Assert.ThrowsAsync<O1InjectedCrashException>(
                    () => coordinator.AdmitAsync(
                        fixture.Bootstrap(),
                    O1SyntheticTrustFixture.Scope,
                    O1SyntheticTrustFixture.NowUtc.AddMinutes(1),
                        "crash-before-replace",
                        O1CommitFault.BeforeReplace));
                Assert.False((await new O1SandboxStore(Path.GetDirectoryName(store.StatePath)!).LoadAsync()).Initialised);

                O1TrustAdmissionResult bootstrap = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.Bootstrap(),
                    "bootstrap-after-crash");
                byte[] oldCompleteState = await File.ReadAllBytesAsync(store.StatePath);
                O1TrustBundle successor = fixture.Successor(bootstrap.State);
                await Assert.ThrowsAsync<O1InjectedCrashException>(
                    () => coordinator.AdmitAsync(
                        successor,
                        O1SyntheticTrustFixture.Scope,
                        O1SyntheticTrustFixture.NowUtc.AddMinutes(1),
                        "crash-after-replace",
                        O1CommitFault.AfterReplaceBeforeWitness));

                O1SandboxStore restartedStore = new(Path.GetDirectoryName(store.StatePath)!);
                O1CheckpointState recoveredNewState = await restartedStore.LoadAsync();
                Assert.Equal(2, recoveredNewState.Generation);
                Assert.Equal(2, recoveredNewState.ContextRevision);
                Assert.Equal(2, recoveredNewState.AuditIntents.Count);

                await File.WriteAllBytesAsync(store.StatePath, oldCompleteState);
                O1CheckpointState rollbackDetected = await restartedStore.LoadAsync();
                Assert.True(rollbackDetected.Quarantined);
                Assert.Equal("trust.rollback", rollbackDetected.QuarantineCode);

                O1TrustCoordinator recoveryCoordinator = Coordinator(restartedStore, fixture);
                O1TrustAdmissionResult recovery = await AdmitAsync(
                    recoveryCoordinator,
                    fixture,
                    fixture.Recovery(rollbackDetected),
                    "approved-recovery");
                Assert.True(recovery.Accepted);
                Assert.Equal("trust.recovery_accepted", recovery.Code);
                Assert.False(recovery.State.Quarantined);
                Assert.Equal(2, recovery.State.RecoveryEpochHighWater);
                Assert.Equal(1, recovery.State.Generation);
                Assert.NotEqual(bootstrap.State.SeriesId, recovery.State.SeriesId);

                File.Delete(restartedStore.StatePath);
                O1CheckpointState missingAfterInitialisation = await restartedStore.LoadAsync();
                Assert.True(missingAfterInitialisation.Quarantined);
                Assert.Equal("trust.continuity_unproved", missingAfterInitialisation.QuarantineCode);
            });
    }

    /// <summary>Proves every finite request dimension is checked before a lease or allocation authority is issued.</summary>
    [Fact]
    public void ResourceDimensionsRejectBeforeAllocation()
    {
        O1ResourceEnvelope envelope = O1ResourceEnvelope.Fixture();
        O1ResourceCoordinator coordinator = new(envelope, () => DateTimeOffset.UnixEpoch);
        (O1ResourceRequest Request, string Code)[] cases =
        [
            (new(-1, 1, 1, 1, 1, 1, 1), "resource.declaration_invalid"),
            (new(envelope.MaximumInputBytes + 1, 1, 1, 1, 1, 1, 1), "resource.input_bytes"),
            (new(1, envelope.MaximumStructureDepth + 1, 1, 1, 1, 1, 1), "resource.structure_depth"),
            (new(1, 1, envelope.MaximumItems + 1, 1, 1, 1, 1), "resource.items"),
            (new(1, 1, 1, envelope.MaximumAccountedMemoryBytes + 1, 1, 1, 1), "resource.memory"),
            (new(1, 1, 1, 1, envelope.MaximumWorkUnits + 1, 1, 1), "resource.work"),
            (new(1, 1, 1, 1, 1, envelope.MaximumResults + 1, 1), "resource.results"),
            (new(1, 1, 1, 1, 1, 1, envelope.MaximumOutputBytes + 1), "resource.output_bytes"),
        ];
        foreach ((O1ResourceRequest request, string expectedCode) in cases)
        {
            O1ResourceAdmission result = coordinator.TryAdmit(
                request,
                DateTimeOffset.UnixEpoch.AddMinutes(1),
                CancellationToken.None);
            Assert.False(result.Accepted);
            Assert.Null(result.Lease);
            Assert.Equal(expectedCode, result.Code);
        }
    }

    /// <summary>Proves serial capacity, disabled queue, deadline, cancellation, monotonic fencing and quiescent release.</summary>
    [Fact]
    public void SerialCapacityCancellationDeadlineAndFencingAreDeterministic()
    {
        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        O1ResourceCoordinator coordinator = new(O1ResourceEnvelope.Fixture(), () => now);
        O1ResourceRequest request = new(1, 1, 1, 1, 1, 1, 1);
        O1ResourceAdmission first = coordinator.TryAdmit(
            request,
            now.AddMinutes(1),
            CancellationToken.None);
        Assert.True(first.Accepted);
        Assert.NotNull(first.Lease);

        O1ResourceAdmission saturated = coordinator.TryAdmit(
            request,
            now.AddMinutes(1),
            CancellationToken.None);
        Assert.False(saturated.Accepted);
        Assert.Equal("resource.capacity_global", saturated.Code);

        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        Assert.Equal(
            "resource.cancelled",
            coordinator.TryAdmit(request, now.AddMinutes(1), cancelled.Token).Code);
        Assert.Equal(
            "resource.deadline",
            coordinator.TryAdmit(request, now, CancellationToken.None).Code);

        long staleFence = first.Lease!.Fence;
        first.Lease.MarkQuiescent();
        first.Lease.Dispose();
        Assert.Equal("resource.released", coordinator.LastReleaseCode);

        O1ResourceAdmission second = coordinator.TryAdmit(
            request,
            now.AddMinutes(1),
            CancellationToken.None);
        Assert.True(second.Accepted);
        Assert.True(second.Lease!.Fence > staleFence);
        coordinator.Release(staleFence, quiescent: true);
        Assert.Equal("resource.stale_fence", coordinator.LastReleaseCode);
        second.Lease.MarkQuiescent();
        second.Lease.Dispose();
        Assert.Equal("resource.released", coordinator.LastReleaseCode);
    }

    /// <summary>Proves reserved control admission remains independent while stale data-plane publication cannot release a newer fence.</summary>
    [Fact]
    public void ControlCapacityRemainsIndependentAndStalePublicationFails()
    {
        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        O1ResourceEnvelope envelope = O1ResourceEnvelope.Fixture();
        O1ResourceCoordinator coordinator = new(envelope, () => now);
        O1ResourceAdmission data = coordinator.TryAdmit(
            new O1ResourceRequest(1, 1, 1, 1, 1, 1, 1),
            now.AddMinutes(1),
            CancellationToken.None);
        Assert.True(data.Accepted);

        using O1ControlLease? control = coordinator.TryAdmitControl(
            1,
            now.AddMinutes(1),
            CancellationToken.None);
        Assert.NotNull(control);
        Assert.Null(coordinator.TryAdmitControl(1, now.AddMinutes(1), CancellationToken.None));
        Assert.Null(
            new O1ResourceCoordinator(envelope, () => now).TryAdmitControl(
                envelope.MaximumControlMetadataEntries + 1,
                now.AddMinutes(1),
                CancellationToken.None));
        data.Lease!.Dispose();
        Assert.Equal("resource.released_after_forced_fence", coordinator.LastReleaseCode);
    }

    /// <summary>Proves the corpus requires exact digest, independent attestation, dual approval and declared segment membership.</summary>
    [Fact]
    public void CorpusManifestRequiresApprovalSignatureAndExactMembership()
    {
        using O1SyntheticTrustFixture fixture = new();
        O1TrustBundle bundle = fixture.Bootstrap();
        Assert.Equal(
            "corpus.accepted",
            O1CorpusVerifier.Verify(
                bundle.CorpusManifest,
                bundle.Payload,
                fixture.Trust,
                currentHead: null,
                O1SyntheticTrustFixture.NowUtc));

        O1CorpusManifest changedCase = bundle.CorpusManifest with
        {
            Cases = Array.AsReadOnly(
                bundle.CorpusManifest.Cases
                    .Select(
                        item => item.CaseId == "case-holdout"
                            ? item with { ContentDigest = O1CanonicalCryptography.TextDigest("changed") }
                            : item)
                    .ToArray()),
        };
        Assert.Equal(
            "corpus.digest_mismatch",
            O1CorpusVerifier.Verify(
                changedCase,
                bundle.Payload,
                fixture.Trust,
                null,
                O1SyntheticTrustFixture.NowUtc));

        O1CorpusManifest badAttestation = bundle.CorpusManifest with
        {
            AttestationSignature = Convert.ToBase64String([9, 8, 7]),
        };
        Assert.Equal(
            "corpus.signature_invalid",
            O1CorpusVerifier.Verify(
                badAttestation,
                bundle.Payload,
                fixture.Trust,
                null,
                O1SyntheticTrustFixture.NowUtc));
    }

    /// <summary>Proves the ADR catalogue contains each of the 24 trust, 36 resource and eight corpus vectors exactly once.</summary>
    [Fact]
    public void VectorCatalogueMapsEveryAuthorisedVectorExactlyOnce()
    {
        Assert.Equal(68, O1VectorCatalogue.Entries.Count);
        Assert.Equal(24, O1VectorCatalogue.Entries.Count(entry => entry.Id.StartsWith("TR-", StringComparison.Ordinal)));
        Assert.Equal(36, O1VectorCatalogue.Entries.Count(entry => entry.Id.StartsWith("RE-", StringComparison.Ordinal)));
        Assert.Equal(8, O1VectorCatalogue.Entries.Count(entry => entry.Id.StartsWith("CO-", StringComparison.Ordinal)));
        Assert.Equal(68, O1VectorCatalogue.Entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(
            O1VectorCatalogue.Entries,
            entry =>
            {
                Assert.False(string.IsNullOrWhiteSpace(entry.Component));
                Assert.False(string.IsNullOrWhiteSpace(entry.Test));
                Assert.False(string.IsNullOrWhiteSpace(entry.Owner));
            });
    }

    /// <summary>Uses deterministic property boundaries and malformed cryptographic inputs to prove stable fail-closed behaviour without unbounded work.</summary>
    [Fact]
    public async Task BoundedPropertyAndFuzzInputsFailClosed()
    {
        O1ResourceEnvelope envelope = O1ResourceEnvelope.Fixture();
        DateTimeOffset now = DateTimeOffset.UnixEpoch;
        O1ResourceRequest maximum = new(
            envelope.MaximumInputBytes,
            envelope.MaximumStructureDepth,
            envelope.MaximumItems,
            envelope.MaximumAccountedMemoryBytes,
            envelope.MaximumWorkUnits,
            envelope.MaximumResults,
            envelope.MaximumOutputBytes);
        foreach (O1ResourceRequest admitted in new[]
        {
            maximum,
            maximum with
            {
                InputBytes = envelope.MaximumInputBytes - 1,
                AccountedMemoryBytes = envelope.MaximumAccountedMemoryBytes - 1,
                WorkUnits = envelope.MaximumWorkUnits - 1,
                OutputBytes = envelope.MaximumOutputBytes - 1,
            },
        })
        {
            O1ResourceCoordinator coordinator = new(envelope, () => now);
            O1ResourceAdmission result = coordinator.TryAdmit(
                admitted,
                now.AddMinutes(1),
                CancellationToken.None);
            Assert.True(result.Accepted);
            result.Lease!.MarkQuiescent();
            result.Lease.Dispose();
        }

        await WithSandboxAsync(
            async (store, fixture) =>
            {
                O1TrustCoordinator coordinator = Coordinator(store, fixture);
                O1ResourceEnvelope invalidEnvelope = envelope with { MaximumWorkUnits = 0 };
                O1TrustAdmissionResult invalidResources = await AdmitAsync(
                    coordinator,
                    fixture,
                    fixture.BootstrapWithResources(invalidEnvelope),
                    "invalid-resource-envelope");
                Assert.False(invalidResources.Accepted);
                Assert.Equal("resource.envelope_invalid", invalidResources.Code);

                O1TrustBundle valid = fixture.Bootstrap();
                Random deterministic = new(70123);
                for (int index = 0; index < 64; index++)
                {
                    byte[] malformed = new byte[index + 1];
                    deterministic.NextBytes(malformed);
                    O1TrustAdmissionResult fuzzed = await AdmitAsync(
                        coordinator,
                        fixture,
                        valid with { BundleSignature = Convert.ToBase64String(malformed) },
                        $"fuzz-{index}");
                    Assert.False(fuzzed.Accepted);
                    Assert.Equal("trust.bundle_signature_invalid", fuzzed.Code);
                }
                Assert.False((await store.LoadAsync()).Initialised);
            });
    }

    /// <summary>Proves two local processes cannot own the exact O1 coordinator fence concurrently.</summary>
    [Fact]
    public async Task MultiprocessHostAllowsOnlyOneExactFenceOwner()
    {
        string root = CreateSandboxRoot();
        string readyOne = Path.Combine(root, "ready-one");
        string readyTwo = Path.Combine(root, "ready-two");
        string release = Path.Combine(root, "release");
        Process? first = null;
        Process? second = null;
        try
        {
            string host = Path.Combine(
                RepositoryRoot(),
                "tests",
                "DBNotifier.State06.ConsolidatedSandboxHost",
                "bin",
                "Release",
                "net10.0",
                "DBNotifier.State06.ConsolidatedSandboxHost.dll");
            Assert.True(File.Exists(host), $"Expected the already-built O1 host at {host}.");
            first = StartFenceProcess(host, root, readyOne, release);
            await WaitForFileAsync(readyOne, TimeSpan.FromSeconds(5));

            second = StartFenceProcess(host, root, readyTwo, release);
            await second.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(4, second.ExitCode);
            Assert.Contains(
                "o1_sandbox.failed:fence_unavailable",
                await second.StandardError.ReadToEndAsync(),
                StringComparison.Ordinal);

            await File.WriteAllTextAsync(release, "release");
            await first.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, first.ExitCode);
        }
        finally
        {
            await StopOwnedProcessAsync(second);
            await StopOwnedProcessAsync(first);
            DeleteSandboxRoot(root);
        }
    }

    /// <summary>Creates one coordinator with a deterministic clock inside the fixture validity window.</summary>
    private static O1TrustCoordinator Coordinator(
        O1SandboxStore store,
        O1SyntheticTrustFixture fixture) =>
        new(store, fixture.Trust, () => O1SyntheticTrustFixture.NowUtc);

    /// <summary>Runs one ordinary admission with a deadline after the deterministic current instant.</summary>
    private static Task<O1TrustAdmissionResult> AdmitAsync(
        O1TrustCoordinator coordinator,
        O1SyntheticTrustFixture fixture,
        O1TrustBundle bundle,
        string correlationId) =>
        coordinator.AdmitAsync(
            bundle,
            O1SyntheticTrustFixture.Scope,
            O1SyntheticTrustFixture.NowUtc.AddMinutes(1),
            correlationId);

    /// <summary>Creates and removes one exact synthetic store around an asynchronous test body.</summary>
    private static async Task WithSandboxAsync(
        Func<O1SandboxStore, O1SyntheticTrustFixture, Task> test)
    {
        string root = CreateSandboxRoot();
        try
        {
            using O1SyntheticTrustFixture fixture = new();
            await test(new O1SandboxStore(root), fixture);
        }
        finally
        {
            DeleteSandboxRoot(root);
        }
    }

    /// <summary>Creates one exact O1-owned temporary root.</summary>
    private static string CreateSandboxRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), $"DBNotifier-O1-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>Deletes only the exact O1-owned root created by this test.</summary>
    private static void DeleteSandboxRoot(string root)
    {
        string canonical = Path.GetFullPath(root);
        string temporary = Path.GetFullPath(Path.GetTempPath());
        if (!canonical.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(canonical).StartsWith("DBNotifier-O1-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o1_sandbox.cleanup_scope_invalid");
        }
        if (Directory.Exists(canonical))
        {
            Directory.Delete(canonical, recursive: true);
        }
    }

    /// <summary>Starts one exact already-built O1 fence host without shell mediation.</summary>
    private static Process StartFenceProcess(
        string host,
        string root,
        string ready,
        string release)
    {
        ProcessStartInfo start = new()
        {
            FileName = "dotnet",
            WorkingDirectory = RepositoryRoot(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(host);
        foreach (string argument in new[]
        {
            "--activation",
            "o1-durable-trust-resource-sandbox",
            "--operation",
            "hold-fence",
            "--root",
            root,
            "--ready",
            ready,
            "--release",
            release,
            "--timeout-ms",
            "10000",
        })
        {
            start.ArgumentList.Add(argument);
        }
        return Process.Start(start) ?? throw new InvalidOperationException("o1_sandbox.process_start_failed");
    }

    /// <summary>Waits boundedly for one exact child-owned readiness file.</summary>
    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        using CancellationTokenSource budget = new(timeout);
        while (!File.Exists(path))
        {
            await Task.Delay(20, budget.Token);
        }
    }

    /// <summary>Stops only a still-running process instance started and retained by this test.</summary>
    private static async Task StopOwnedProcessAsync(Process? process)
    {
        if (process is null)
        {
            return;
        }
        using (process)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
    }

    /// <summary>Finds the repository root from the test output without consulting external state.</summary>
    private static string RepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "DBNotifier.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("o1_sandbox.repository_root_not_found");
    }
}
