// Module purpose: Implements the opt-in, kill-switch, fencing and rollback behaviours of the exact O5-R2 synthetic test-only sandbox.
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using DBNotifier.Application.AIOps;

namespace DBNotifier.IntegrationTests;

/// <summary>Authenticates exact dual-control approvals with distinct synthetic keys and no operational trust material.</summary>
internal sealed class O5R2SyntheticActivationAuthority
{
    private readonly IReadOnlyDictionary<ObserverApprovalRole, SyntheticRoleKey> keys;

    /// <summary>Initialises the authority with one distinct key per independent approval role.</summary>
    /// <param name="keys">Exact synthetic role keys.</param>
    internal O5R2SyntheticActivationAuthority(IReadOnlyCollection<SyntheticRoleKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Count != 2 ||
            keys.Select(item => item.Role).Distinct().Count() != 2 ||
            keys.Select(item => Convert.ToHexString(item.KeyBytes)).Distinct(StringComparer.Ordinal).Count() != 2)
        {
            throw new ArgumentException("O5-R2 requires two independent synthetic approval keys.", nameof(keys));
        }

        this.keys = keys.ToDictionary(item => item.Role);
    }

    /// <summary>Authenticates both exact signatures in fixed time without consuming the approval.</summary>
    /// <param name="approval">Scope-bound approval.</param>
    /// <returns><see langword="true"/> only when both independent role signatures are exact.</returns>
    internal bool Authenticate(ObserverOptInApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        byte[] payload = CanonicalPayload(approval);
        foreach (ObserverApprovalSignature signature in approval.Signatures)
        {
            if (!keys.TryGetValue(signature.Role, out SyntheticRoleKey? key) ||
                !string.Equals(key.KeyId, signature.KeyId, StringComparison.Ordinal))
            {
                return false;
            }

            byte[] expected = HMACSHA256.HashData(key.KeyBytes, payload);
            if (!CryptographicOperations.FixedTimeEquals(signature.Signature.ToArray(), expected))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Creates both role signatures over an exact approval payload.</summary>
    /// <param name="approvalId">One-use nonce.</param>
    /// <param name="cellId">Exact pilot cell.</param>
    /// <param name="environment">Exact environment.</param>
    /// <param name="purpose">Exact purpose.</param>
    /// <param name="revision">Exact context revision.</param>
    /// <param name="notBefore">Inclusive UTC validity start.</param>
    /// <param name="expiresAt">Exclusive UTC expiry.</param>
    /// <returns>Authenticated dual-controlled approval.</returns>
    internal ObserverOptInApproval Sign(
        Guid approvalId,
        string cellId,
        string environment,
        string purpose,
        string revision,
        DateTimeOffset notBefore,
        DateTimeOffset expiresAt)
    {
        ObserverOptInApproval unsigned = new(
            approvalId,
            cellId,
            environment,
            purpose,
            revision,
            notBefore,
            expiresAt,
            [
                new ObserverApprovalSignature(
                    ObserverApprovalRole.ControlApproverA,
                    keys[ObserverApprovalRole.ControlApproverA].KeyId,
                    new byte[32]),
                new ObserverApprovalSignature(
                    ObserverApprovalRole.ControlApproverB,
                    keys[ObserverApprovalRole.ControlApproverB].KeyId,
                    new byte[32]),
            ]);
        byte[] payload = CanonicalPayload(unsigned);
        return new ObserverOptInApproval(
            approvalId,
            cellId,
            environment,
            purpose,
            revision,
            notBefore,
            expiresAt,
            keys.Values
                .OrderBy(item => item.Role)
                .Select(item => new ObserverApprovalSignature(
                    item.Role,
                    item.KeyId,
                    HMACSHA256.HashData(item.KeyBytes, payload)))
                .ToArray());
    }

    /// <summary>Serialises only the bounded approval fields covered by both signatures.</summary>
    /// <param name="approval">Approval envelope.</param>
    /// <returns>Canonical UTF-8 payload.</returns>
    private static byte[] CanonicalPayload(ObserverOptInApproval approval) =>
        Encoding.UTF8.GetBytes(
            string.Join(
                '\n',
                approval.ApprovalId.ToString("D"),
                approval.CellId,
                approval.Environment,
                approval.Purpose,
                approval.Revision,
                approval.NotBefore.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                approval.ExpiresAt.UtcTicks.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    /// <summary>Represents one non-secret identifier and its synthetic role-specific key bytes.</summary>
    /// <param name="Role">Independent approval role.</param>
    /// <param name="KeyId">Stable non-secret key identifier.</param>
    /// <param name="KeyBytes">Synthetic key bytes confined to the test fixture.</param>
    internal sealed record SyntheticRoleKey(
        ObserverApprovalRole Role,
        string KeyId,
        byte[] KeyBytes);
}

/// <summary>Represents a non-activating simulated admission whose cancellation and fence can be observed.</summary>
internal sealed class O5R2SandboxAdmission : IDisposable
{
    private readonly O5R2ControlPlaneSandbox owner;
    private readonly bool mayEvaluate;
    private readonly bool mayPublish;
    private bool disposed;

    /// <summary>Initialises one exact simulated admission lease.</summary>
    /// <param name="owner">Owning sandbox coordinator.</param>
    /// <param name="admitted">Whether the control admission was accepted.</param>
    /// <param name="fence">Monotonic fence, or zero for refusals.</param>
    /// <param name="contextRevision">Exact admitted context revision.</param>
    /// <param name="code">Stable sanitised outcome.</param>
    /// <param name="cancellationToken">Token cancelled by kill switch or rollback.</param>
    internal O5R2SandboxAdmission(
        O5R2ControlPlaneSandbox owner,
        bool admitted,
        long fence,
        string? contextRevision,
        string code,
        CancellationToken cancellationToken)
    {
        this.owner = owner;
        Admitted = admitted;
        Fence = fence;
        ContextRevision = contextRevision;
        Code = code;
        CancellationToken = cancellationToken;
        mayEvaluate = false;
        mayPublish = false;
    }

    /// <summary>Gets whether the synthetic control admission was accepted.</summary>
    internal bool Admitted { get; }

    /// <summary>Gets the monotonic admission fence.</summary>
    internal long Fence { get; }

    /// <summary>Gets the exact admitted context revision.</summary>
    internal string? ContextRevision { get; }

    /// <summary>Gets the stable sanitised result code.</summary>
    internal string Code { get; }

    /// <summary>Gets the token cancelled by a higher-priority safety transition.</summary>
    internal CancellationToken CancellationToken { get; }

    /// <summary>Gets the immutable absence of evaluation authority.</summary>
    internal bool MayEvaluate => mayEvaluate;

    /// <summary>Gets the immutable absence of publication authority.</summary>
    internal bool MayPublish => mayPublish;

    /// <summary>Releases only the matching simulated admission.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        owner.Release(Fence);
    }
}

/// <summary>
/// Coordinates the exact O5-R2 test-only control sandbox while preserving activation None and exposing no evaluator,
/// corpus, pipeline or publisher dependency.
/// </summary>
internal sealed class O5R2ControlPlaneSandbox : IAsyncDisposable
{
    internal const string Marker = "DBNOTIFIER_O5_R2_TEST_ONLY";
    internal const string PilotCellId = "OBS-PILOT-PG16-LOCAL-001";
    internal const string PilotEnvironment = "local-laboratory";
    internal const string PilotPurpose = "read-only-observation";
    private readonly SemaphoreSlim control = new(1, 1);
    private readonly O5R2ControlPlaneStore store;
    private readonly O5R2SyntheticActivationAuthority authority;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<long, CancellationTokenSource> activeAdmissions = [];
    private readonly ObserverActivationState activationState = ObserverActivationState.None;
    private int killRequested;
    private bool disposed;

    /// <summary>Initialises one explicitly marked synthetic coordinator.</summary>
    /// <param name="marker">Exact test-only opt-in marker.</param>
    /// <param name="store">Temporary authenticated store.</param>
    /// <param name="authority">Synthetic dual-control authority.</param>
    /// <param name="timeProvider">Deterministic test clock.</param>
    /// <exception cref="InvalidOperationException">Thrown unless the exact test-only marker is supplied.</exception>
    internal O5R2ControlPlaneSandbox(
        string marker,
        O5R2ControlPlaneStore store,
        O5R2SyntheticActivationAuthority authority,
        TimeProvider timeProvider)
    {
        if (!string.Equals(marker, Marker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o5r2.sandbox.marker_required");
        }

        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets the activation state, which cannot change in the O5-R2 sandbox.</summary>
    internal ObserverActivationState ActivationState => activationState;

    /// <summary>Recovers a complete checkpoint and invalidates any context left active by an interrupted process.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recovered complete inactive checkpoint.</returns>
    internal async Task<O5R2Checkpoint> RecoverAsync(CancellationToken cancellationToken = default)
    {
        await control.WaitAsync(cancellationToken);
        try
        {
            O5R2Checkpoint current = await store.LoadAsync(cancellationToken);
            if (current.KillSwitchEngaged)
            {
                Volatile.Write(ref killRequested, 1);
            }

            if (current.Quarantined || current.ActiveContextRevision is null)
            {
                return current;
            }

            return await store.CommitAsync(
                current.Generation,
                current with
                {
                    Generation = current.Generation + 1,
                    Fence = current.Fence + 1,
                    ActiveContextRevision = null,
                    Code = "o5r2.control.restart_recovered_none",
                },
                cancellationToken: cancellationToken);
        }
        finally
        {
            control.Release();
        }
    }

    /// <summary>Authenticates, consumes and fences one simulated admission without activating evaluation or publication.</summary>
    /// <param name="approval">Exact one-use dual-controlled approval.</param>
    /// <param name="deadline">Exclusive absolute UTC deadline.</param>
    /// <param name="cancellationToken">Cancellation observed before commit.</param>
    /// <returns>A simulated control lease or fail-closed refusal.</returns>
    internal async Task<O5R2SandboxAdmission> TryAdmitAsync(
        ObserverOptInApproval approval,
        DateTimeOffset deadline,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approval);
        if (deadline == default || deadline.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("O5-R2 deadlines must be explicit UTC values.", nameof(deadline));
        }

        if (Volatile.Read(ref killRequested) != 0)
        {
            return Refused("o5r2.control.kill_switch_engaged");
        }

        await control.WaitAsync(cancellationToken);
        try
        {
            if (Volatile.Read(ref killRequested) != 0)
            {
                return Refused("o5r2.control.kill_switch_engaged");
            }

            DateTimeOffset now = timeProvider.GetUtcNow();
            if (now >= deadline)
            {
                return Refused("o5r2.control.deadline_expired");
            }

            O5R2Checkpoint current = await store.LoadAsync(cancellationToken);
            if (current.Quarantined)
            {
                return Refused(current.Code);
            }

            if (current.KillSwitchEngaged)
            {
                return Refused("o5r2.control.kill_switch_engaged");
            }

            if (!string.Equals(approval.CellId, PilotCellId, StringComparison.Ordinal) ||
                !string.Equals(approval.Environment, PilotEnvironment, StringComparison.Ordinal) ||
                !string.Equals(approval.Purpose, PilotPurpose, StringComparison.Ordinal))
            {
                return Refused("o5r2.control.scope_mismatch");
            }

            if (now < approval.NotBefore || now >= approval.ExpiresAt)
            {
                return Refused("o5r2.control.approval_expired");
            }

            if (!authority.Authenticate(approval))
            {
                return Refused("o5r2.control.approval_unproved");
            }

            if (current.ConsumedApprovalIds.Contains(approval.ApprovalId))
            {
                return Refused("o5r2.control.approval_replayed");
            }

            if (current.ConsumedApprovalIds.Count >= 64)
            {
                return Refused("o5r2.control.approval_capacity_exhausted");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref killRequested) != 0)
            {
                return Refused("o5r2.control.kill_switch_engaged");
            }

            if (timeProvider.GetUtcNow() >= deadline)
            {
                return Refused("o5r2.control.deadline_expired");
            }

            long fence = current.Fence + 1;
            O5R2Checkpoint committed = await store.CommitAsync(
                current.Generation,
                current with
                {
                    Generation = current.Generation + 1,
                    Fence = fence,
                    ActiveContextRevision = approval.Revision,
                    ConsumedApprovalIds = current.ConsumedApprovalIds.Append(approval.ApprovalId).ToArray(),
                    Code = "o5r2.control.simulated_admission",
                },
                cancellationToken: cancellationToken);
            CancellationTokenSource killCancellation = new();
            if (!activeAdmissions.TryAdd(fence, killCancellation))
            {
                killCancellation.Dispose();
                throw new InvalidOperationException("o5r2.control.fence_conflict");
            }
            return new O5R2SandboxAdmission(
                this,
                admitted: true,
                committed.Fence,
                committed.ActiveContextRevision,
                committed.Code,
                killCancellation.Token);
        }
        finally
        {
            control.Release();
        }
    }

    /// <summary>Commits the priority kill switch, fences every old context and cancels all current simulations.</summary>
    /// <param name="cancellationToken">Cancellation observed before the atomic safety commit.</param>
    /// <returns>The complete killed checkpoint.</returns>
    internal async Task<O5R2Checkpoint> EngageKillSwitchAsync(CancellationToken cancellationToken = default)
    {
        Interlocked.Exchange(ref killRequested, 1);
        await control.WaitAsync(cancellationToken);
        try
        {
            O5R2Checkpoint current = await store.LoadAsync(cancellationToken);
            if (current.Quarantined || current.KillSwitchEngaged)
            {
                return current;
            }

            O5R2Checkpoint committed = await store.CommitAsync(
                current.Generation,
                current with
                {
                    Generation = current.Generation + 1,
                    Fence = current.Fence + 1,
                    KillSwitchEngaged = true,
                    ActiveContextRevision = null,
                    Code = "o5r2.control.kill_switch_engaged",
                },
                cancellationToken: cancellationToken);
            foreach (CancellationTokenSource active in activeAdmissions.Values)
            {
                active.Cancel();
            }

            return committed;
        }
        finally
        {
            control.Release();
        }
    }

    /// <summary>Performs the only rollback outcome: a fenced, killed and inactive complete checkpoint.</summary>
    /// <param name="cancellationToken">Cancellation observed before commit.</param>
    /// <returns>The complete inactive rollback checkpoint.</returns>
    internal async Task<O5R2Checkpoint> RollbackToNoneAsync(CancellationToken cancellationToken = default)
    {
        O5R2Checkpoint state = await EngageKillSwitchAsync(cancellationToken);
        return state with { ActivationState = "None" };
    }

    /// <summary>Proves that no current or obsolete context can publish while activation remains None.</summary>
    /// <param name="fence">Candidate publication fence.</param>
    /// <param name="contextRevision">Candidate publication context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    internal async Task<bool> CanPublishAsync(
        long fence,
        string contextRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextRevision);
        O5R2Checkpoint current = await store.LoadAsync(cancellationToken);
        _ = fence;
        _ = current;
        return false;
    }

    /// <summary>Releases only the exact in-memory admission cancellation source.</summary>
    /// <param name="fence">Admission fence.</param>
    internal void Release(long fence)
    {
        if (activeAdmissions.TryRemove(fence, out CancellationTokenSource? source))
        {
            source.Dispose();
        }
    }

    /// <summary>Cancels and disposes all fixture-owned state without touching unrelated processes or data.</summary>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        await control.WaitAsync();
        try
        {
            foreach (CancellationTokenSource source in activeAdmissions.Values)
            {
                source.Cancel();
                source.Dispose();
            }

            activeAdmissions.Clear();
        }
        finally
        {
            control.Release();
            control.Dispose();
        }
    }

    /// <summary>Creates a refusal that carries no fence, evaluation or publication authority.</summary>
    /// <param name="code">Stable sanitised refusal code.</param>
    /// <returns>Fail-closed simulated admission.</returns>
    private O5R2SandboxAdmission Refused(string code) =>
        new(this, admitted: false, 0, null, code, CancellationToken.None);
}
