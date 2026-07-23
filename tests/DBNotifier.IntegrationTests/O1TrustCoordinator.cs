// Module purpose: Validates synthetic O1 trust bundles, dual approvals, corpus authority and monotonic continuity before one atomic sandbox commit.
namespace DBNotifier.IntegrationTests;

/// <summary>Holds independently provisioned public roots and role keys; candidates cannot add trust to this context.</summary>
internal sealed class O1PublicTrustContext
{
    private readonly Dictionary<string, string> rootsByDigest;
    private readonly Dictionary<string, O1PublicRoleKey> rolesByKeyId;

    /// <summary>Initialises an immutable public verification context.</summary>
    /// <param name="rootsByDigest">Independently selected root public keys keyed by their material digest.</param>
    /// <param name="rolesByKeyId">Independently selected role public keys keyed by identifier.</param>
    internal O1PublicTrustContext(
        IReadOnlyDictionary<string, string> rootsByDigest,
        IReadOnlyDictionary<string, O1PublicRoleKey> rolesByKeyId)
    {
        this.rootsByDigest = new Dictionary<string, string>(
            rootsByDigest,
            StringComparer.Ordinal);
        this.rolesByKeyId = new Dictionary<string, O1PublicRoleKey>(
            rolesByKeyId,
            StringComparer.Ordinal);
    }

    /// <summary>Finds one independently provisioned root by exact material digest.</summary>
    /// <param name="digest">Exact root material digest.</param>
    /// <param name="publicKey">Resolved public key.</param>
    /// <returns><see langword="true"/> when the root is independently present.</returns>
    internal bool TryGetRoot(string digest, out string? publicKey) =>
        rootsByDigest.TryGetValue(digest, out publicKey);

    /// <summary>Finds one independently provisioned role key by exact identifier.</summary>
    /// <param name="keyId">Exact key identifier.</param>
    /// <param name="roleKey">Resolved role and public material.</param>
    /// <returns><see langword="true"/> when the key is independently present.</returns>
    internal bool TryGetRole(string keyId, out O1PublicRoleKey? roleKey) =>
        rolesByKeyId.TryGetValue(keyId, out roleKey);
}

/// <summary>Associates one independently provisioned public key with exactly one synthetic role.</summary>
internal sealed record O1PublicRoleKey(
    O1TrustRole Role,
    string PublicKey);

/// <summary>Defines the canonical fields signed by one dual-control decision.</summary>
internal sealed record O1ApprovalPayload(
    O1ApprovalKind Kind,
    string DecisionId,
    string DomainId,
    string ProposedHeadDigest,
    long RecoveryEpoch,
    string SeriesId,
    string Nonce,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc);

/// <summary>Defines the exact root-signed content of one role-key delegation.</summary>
internal sealed record O1DelegationPayload(
    O1TrustRole Role,
    string KeyId,
    string PublicKey,
    long RecoveryEpoch,
    string SeriesId,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc);

/// <summary>Defines the exact corpus content approved and attested independently of its signatures.</summary>
internal sealed record O1CorpusManifestPayload(
    string DatasetId,
    long Revision,
    string PreviousManifestDigest,
    string TenantId,
    string Environment,
    string Purpose,
    long RecoveryEpoch,
    string SeriesId,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc,
    bool Withdrawn,
    IReadOnlyList<O1CorpusCase> Cases,
    IReadOnlyDictionary<string, int> ExpectedSegmentCounts);

/// <summary>Validates independently approved corpus manifests and exact bounded membership.</summary>
internal static class O1CorpusVerifier
{
    /// <summary>Validates signatures, scope, revision, membership and segment counts before a corpus head can advance.</summary>
    /// <param name="manifest">Untrusted synthetic manifest.</param>
    /// <param name="bundle">Candidate bundle payload that names the manifest.</param>
    /// <param name="trust">Independent public trust context.</param>
    /// <param name="currentHead">Current durable corpus head, if any.</param>
    /// <param name="nowUtc">Deterministic current instant.</param>
    /// <returns>Stable failure code or <c>corpus.accepted</c>.</returns>
    internal static string Verify(
        O1CorpusManifest manifest,
        O1TrustBundlePayload bundle,
        O1PublicTrustContext trust,
        O1CorpusHeadState? currentHead,
        DateTimeOffset nowUtc)
    {
        O1CorpusManifestPayload payload = Payload(manifest);
        string digest = O1CanonicalCryptography.Digest(payload);
        if (!string.Equals(digest, bundle.CorpusManifestDigest, StringComparison.Ordinal))
        {
            return "corpus.digest_mismatch";
        }
        if (!trust.TryGetRole(manifest.AttestationKeyId, out O1PublicRoleKey? attestation) ||
            attestation is null ||
            attestation.Role != O1TrustRole.CorpusAttestation ||
            !O1CanonicalCryptography.Verify(
                payload,
                manifest.AttestationSignature,
                attestation.PublicKey))
        {
            return "corpus.signature_invalid";
        }
        if (!O1TrustCoordinator.VerifyApproval(
                manifest.Approval,
                O1ApprovalKind.Corpus,
                digest,
                bundle,
                trust,
                [O1TrustRole.DataOwnerApprover, O1TrustRole.DataGovernanceApprover],
                nowUtc))
        {
            return "corpus.approval_invalid";
        }
        if (manifest.DatasetId.Length is < 1 or > 96 ||
            manifest.Cases.Count is < 1 or > 256 ||
            manifest.ExpectedSegmentCounts.Count is < 1 or > 32 ||
            !string.Equals(manifest.TenantId, bundle.TenantId, StringComparison.Ordinal) ||
            !string.Equals(manifest.Environment, bundle.Environment, StringComparison.Ordinal) ||
            !string.Equals(manifest.Purpose, bundle.Purpose, StringComparison.Ordinal) ||
            manifest.RecoveryEpoch != bundle.RecoveryEpoch ||
            !string.Equals(manifest.SeriesId, bundle.SeriesId, StringComparison.Ordinal) ||
            nowUtc < manifest.NotBeforeUtc ||
            nowUtc >= manifest.ExpiresUtc)
        {
            return "corpus.scope_invalid";
        }
        if (manifest.Cases.Any(
                item => string.IsNullOrWhiteSpace(item.CaseId) ||
                    string.IsNullOrWhiteSpace(item.Partition) ||
                    string.IsNullOrWhiteSpace(item.Segment) ||
                    string.IsNullOrWhiteSpace(item.ContentDigest)) ||
            manifest.Cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() !=
                manifest.Cases.Count)
        {
            return "corpus.partition_invalid";
        }

        Dictionary<string, int> observed = manifest.Cases
            .GroupBy(item => item.Segment, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        if (observed.Count != manifest.ExpectedSegmentCounts.Count ||
            observed.Any(
                entry => !manifest.ExpectedSegmentCounts.TryGetValue(entry.Key, out int expected) ||
                    expected != entry.Value))
        {
            return "corpus.partition_invalid";
        }
        if (currentHead is null)
        {
            return manifest.Revision == 1 &&
                string.IsNullOrEmpty(manifest.PreviousManifestDigest)
                ? "corpus.accepted"
                : "corpus.generation_gap";
        }
        if (currentHead.Withdrawn && !manifest.Withdrawn)
        {
            return "corpus.withdrawal_irreversible";
        }
        if (manifest.Revision == currentHead.Revision &&
            string.Equals(digest, currentHead.ManifestDigest, StringComparison.Ordinal))
        {
            return "corpus.idempotent";
        }
        if (manifest.Revision == currentHead.Revision)
        {
            return "corpus.split_view";
        }
        if (manifest.Revision != checked(currentHead.Revision + 1) ||
            !string.Equals(
                manifest.PreviousManifestDigest,
                currentHead.ManifestDigest,
                StringComparison.Ordinal))
        {
            return manifest.Revision <= currentHead.Revision
                ? "corpus.rollback"
                : "corpus.generation_gap";
        }
        return "corpus.accepted";
    }

    /// <summary>Projects one manifest into its exact signature-free canonical payload.</summary>
    /// <param name="manifest">Manifest.</param>
    /// <returns>Canonical manifest payload.</returns>
    internal static O1CorpusManifestPayload Payload(O1CorpusManifest manifest) =>
        new(
            manifest.DatasetId,
            manifest.Revision,
            manifest.PreviousManifestDigest,
            manifest.TenantId,
            manifest.Environment,
            manifest.Purpose,
            manifest.RecoveryEpoch,
            manifest.SeriesId,
            manifest.NotBeforeUtc,
            manifest.ExpiresUtc,
            manifest.Withdrawn,
            manifest.Cases
                .OrderBy(item => item.CaseId, StringComparer.Ordinal)
                .ToArray(),
            new SortedDictionary<string, int>(
                manifest.ExpectedSegmentCounts.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.Ordinal),
                StringComparer.Ordinal));
}

/// <summary>Coordinates validation and atomic publication of one synthetic O1 trust context.</summary>
internal sealed class O1TrustCoordinator
{
    private readonly O1SandboxStore store;
    private readonly O1PublicTrustContext trust;
    private readonly Func<DateTimeOffset> utcNow;

    /// <summary>Initialises one host-owned sandbox coordinator.</summary>
    /// <param name="store">Host-owned local checkpoint store.</param>
    /// <param name="trust">Independently provisioned public trust.</param>
    /// <param name="utcNow">Deterministic UTC clock.</param>
    internal O1TrustCoordinator(
        O1SandboxStore store,
        O1PublicTrustContext trust,
        Func<DateTimeOffset> utcNow)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.trust = trust ?? throw new ArgumentNullException(nameof(trust));
        this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
    }

    /// <summary>Validates one candidate completely and publishes one new immutable context only through an atomic commit.</summary>
    /// <param name="bundle">Untrusted candidate bundle.</param>
    /// <param name="requestedScope">Exact canonical requested scope.</param>
    /// <param name="absoluteDeadlineUtc">Deadline recorded before validation.</param>
    /// <param name="correlationId">Bounded synthetic audit correlation identifier.</param>
    /// <param name="fault">Optional deterministic commit crash.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>Typed non-authorising failure or accepted complete checkpoint.</returns>
    internal async Task<O1TrustAdmissionResult> AdmitAsync(
        O1TrustBundle bundle,
        IReadOnlyList<string> requestedScope,
        DateTimeOffset absoluteDeadlineUtc,
        string correlationId,
        O1CommitFault fault = O1CommitFault.None,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(requestedScope);
        if (cancellationToken.IsCancellationRequested)
        {
            return Refused("trust.cancelled", new O1CheckpointState());
        }
        if (utcNow() >= absoluteDeadlineUtc)
        {
            return Refused("trust.deadline", new O1CheckpointState());
        }

        O1CheckpointState current = await store.LoadAsync(cancellationToken);
        string bundleDigest = O1CanonicalCryptography.Digest(bundle.Payload);
        if (current.Initialised &&
            !current.Quarantined &&
            bundle.Payload.RecoveryEpoch == current.RecoveryEpochHighWater &&
            bundle.Payload.Generation == current.Generation &&
            string.Equals(bundleDigest, current.BundleDigest, StringComparison.Ordinal))
        {
            return new O1TrustAdmissionResult(
                true,
                true,
                current.Quarantined,
                "trust.idempotent",
                current);
        }

        string validation = ValidateBundle(
            bundle,
            requestedScope,
            current,
            bundleDigest,
            utcNow());
        if (validation is not "trust.accepted" and not "trust.recovery_accepted")
        {
            if (validation is
                "trust.rollback" or
                "trust.generation_gap" or
                "trust.split_view" or
                "corpus.rollback" or
                "corpus.generation_gap" or
                "corpus.split_view")
            {
                O1CheckpointState quarantined = current.Clone();
                quarantined.Quarantined = true;
                quarantined.QuarantineCode = validation;
                quarantined.ContextRevision = checked(current.ContextRevision + 1);
                quarantined.Fence = checked(current.Fence + 1);
                quarantined.AuditIntents.Add(
                    new O1AuditIntent
                    {
                        Code = validation,
                        ContextRevision = quarantined.ContextRevision,
                        CorrelationId = correlationId,
                    });
                O1CheckpointState committed = await store.CommitAsync(
                    current.ContextRevision,
                    quarantined,
                    fault,
                    cancellationToken);
                return new O1TrustAdmissionResult(
                    false,
                    false,
                    true,
                    validation,
                    committed);
            }
            return Refused(validation, current);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Refused("trust.cancelled", current);
        }
        if (utcNow() >= absoluteDeadlineUtc)
        {
            return Refused("trust.deadline", current);
        }

        O1CheckpointState candidate = BuildCandidate(
            current,
            bundle,
            bundleDigest,
            correlationId);
        O1CheckpointState accepted = await store.CommitAsync(
            current.ContextRevision,
            candidate,
            fault,
            cancellationToken);
        return new O1TrustAdmissionResult(
            true,
            false,
            false,
            validation,
            accepted);
    }

    /// <summary>Validates cryptographic roles, approvals, scope, continuity, corpus and finite resources.</summary>
    /// <param name="bundle">Candidate.</param>
    /// <param name="requestedScope">Exact requested scope.</param>
    /// <param name="current">Current durable state.</param>
    /// <param name="bundleDigest">Candidate payload digest.</param>
    /// <param name="nowUtc">Deterministic current instant.</param>
    /// <returns>Stable acceptance or refusal code.</returns>
    private string ValidateBundle(
        O1TrustBundle bundle,
        IReadOnlyList<string> requestedScope,
        O1CheckpointState current,
        string bundleDigest,
        DateTimeOffset nowUtc)
    {
        O1TrustBundlePayload payload = bundle.Payload;
        if (payload.SchemaVersion != 1 ||
            payload.DomainId.Length is < 1 or > 96 ||
            payload.Scope.Count is < 1 or > 32 ||
            payload.Scope.Distinct(StringComparer.Ordinal).Count() != payload.Scope.Count ||
            !payload.Scope.SequenceEqual(
                payload.Scope.OrderBy(item => item, StringComparer.Ordinal),
                StringComparer.Ordinal) ||
            !payload.Scope.SequenceEqual(
                requestedScope.OrderBy(item => item, StringComparer.Ordinal),
                StringComparer.Ordinal) ||
            nowUtc < payload.NotBeforeUtc ||
            nowUtc >= payload.ExpiresUtc)
        {
            return "trust.scope_mismatch";
        }
        if (!ValidateFiniteResources(payload.Resources))
        {
            return "resource.envelope_invalid";
        }
        if (!trust.TryGetRoot(payload.RootSetDigest, out string? rootPublicKey) ||
            rootPublicKey is null ||
            O1CanonicalCryptography.PublicKeyDigest(rootPublicKey) != payload.RootSetDigest ||
            !ValidateDelegations(payload, rootPublicKey))
        {
            return "trust.root_unproved";
        }
        O1RoleDelegation? bundleDelegation = payload.Delegations.SingleOrDefault(
            item => item.Role == O1TrustRole.TrustBundle &&
                string.Equals(item.KeyId, bundle.BundleKeyId, StringComparison.Ordinal));
        if (bundleDelegation is null ||
            !O1CanonicalCryptography.Verify(
                payload,
                bundle.BundleSignature,
                bundleDelegation.PublicKey))
        {
            return "trust.bundle_signature_invalid";
        }

        string policyHead = PolicyHead(payload);
        if (!VerifyApproval(
                bundle.PolicyApproval,
                O1ApprovalKind.Policy,
                policyHead,
                payload,
                trust,
                [O1TrustRole.PolicyApproverA, O1TrustRole.PolicyApproverB],
                nowUtc))
        {
            return "trust.authorisation_unproved";
        }

        current.CorpusHeads.TryGetValue(
            bundle.CorpusManifest.DatasetId,
            out O1CorpusHeadState? currentCorpus);
        string corpus = O1CorpusVerifier.Verify(
            bundle.CorpusManifest,
            payload,
            trust,
            payload.RecoveryEpoch == current.RecoveryEpochHighWater ? currentCorpus : null,
            nowUtc);
        if (corpus is not "corpus.accepted" and not "corpus.idempotent")
        {
            return corpus;
        }

        string continuity = ValidateContinuity(
            bundle,
            current,
            bundleDigest,
            nowUtc);
        if (continuity is not "trust.accepted" and not "trust.recovery_accepted")
        {
            return continuity;
        }

        IEnumerable<string> nonces =
        [
            bundle.PolicyApproval.Nonce,
            bundle.CorpusManifest.Approval.Nonce,
        ];
        if (!current.Initialised || continuity == "trust.recovery_accepted")
        {
            nonces = nonces.Append(bundle.ContinuityApproval.Nonce);
        }
        string[] consumed = nonces.Distinct(StringComparer.Ordinal).ToArray();
        if (consumed.Any(
                nonce => current.ConsumedApprovalNonces.Contains(nonce, StringComparer.Ordinal)))
        {
            return "trust.approval_replayed";
        }
        return continuity;
    }

    /// <summary>Validates bootstrap, direct successor, rollback, gap, divergence and recovery rules.</summary>
    /// <param name="bundle">Candidate.</param>
    /// <param name="current">Current state.</param>
    /// <param name="bundleDigest">Candidate digest.</param>
    /// <param name="nowUtc">Current instant.</param>
    /// <returns>Stable continuity code.</returns>
    private string ValidateContinuity(
        O1TrustBundle bundle,
        O1CheckpointState current,
        string bundleDigest,
        DateTimeOffset nowUtc)
    {
        O1TrustBundlePayload payload = bundle.Payload;
        if (!current.Initialised)
        {
            return payload.RecoveryEpoch == 1 &&
                payload.Generation == 1 &&
                string.IsNullOrEmpty(payload.PreviousBundleDigest) &&
                VerifyApproval(
                    bundle.ContinuityApproval,
                    O1ApprovalKind.Bootstrap,
                    bundleDigest,
                    payload,
                    trust,
                    [O1TrustRole.BootstrapApproverA, O1TrustRole.BootstrapApproverB],
                    nowUtc)
                ? "trust.accepted"
                : "trust.bootstrap_invalid";
        }

        if (current.Quarantined)
        {
            bool recovery = payload.RecoveryEpoch == checked(current.RecoveryEpochHighWater + 1) &&
                payload.Generation == 1 &&
                !string.Equals(payload.SeriesId, current.SeriesId, StringComparison.Ordinal) &&
                string.Equals(
                    payload.PreviousBundleDigest,
                    current.BundleDigest,
                    StringComparison.Ordinal) &&
                VerifyApproval(
                    bundle.ContinuityApproval,
                    O1ApprovalKind.Recovery,
                    bundleDigest,
                    payload,
                    trust,
                    [O1TrustRole.RecoveryApproverA, O1TrustRole.RecoveryApproverB],
                    nowUtc);
            return recovery
                ? "trust.recovery_accepted"
                : "trust.recovery_invalid";
        }

        if (payload.RecoveryEpoch < current.RecoveryEpochHighWater ||
            (payload.RecoveryEpoch == current.RecoveryEpochHighWater &&
             payload.Generation < current.Generation))
        {
            return "trust.rollback";
        }
        if (payload.RecoveryEpoch != current.RecoveryEpochHighWater ||
            !string.Equals(payload.SeriesId, current.SeriesId, StringComparison.Ordinal))
        {
            return "trust.epoch_superseded";
        }
        if (payload.Generation == current.Generation &&
            !string.Equals(bundleDigest, current.BundleDigest, StringComparison.Ordinal))
        {
            return "trust.split_view";
        }
        if (payload.Generation != checked(current.Generation + 1) ||
            !string.Equals(
                payload.PreviousBundleDigest,
                current.BundleDigest,
                StringComparison.Ordinal))
        {
            return "trust.generation_gap";
        }
        return "trust.accepted";
    }

    /// <summary>Validates exact role delegations, root signatures, distinct identities and distinct key material.</summary>
    /// <param name="payload">Bundle payload.</param>
    /// <param name="rootPublicKey">Independent root key.</param>
    /// <returns><see langword="true"/> only for a complete non-crossed delegation set.</returns>
    private bool ValidateDelegations(
        O1TrustBundlePayload payload,
        string rootPublicKey)
    {
        O1TrustRole[] expectedRoles = Enum.GetValues<O1TrustRole>()
            .Where(role => role != O1TrustRole.Root)
            .ToArray();
        if (payload.Delegations.Count != expectedRoles.Length ||
            payload.Delegations.Select(item => item.Role).Distinct().Count() != expectedRoles.Length ||
            payload.Delegations.Select(item => item.KeyId).Distinct(StringComparer.Ordinal).Count() !=
                expectedRoles.Length)
        {
            return false;
        }

        string rootMaterialDigest = O1CanonicalCryptography.PublicKeyDigest(rootPublicKey);
        HashSet<string> material = new(StringComparer.Ordinal)
        {
            rootMaterialDigest,
        };
        foreach (O1RoleDelegation delegation in payload.Delegations)
        {
            if (!expectedRoles.Contains(delegation.Role) ||
                delegation.RecoveryEpoch != payload.RecoveryEpoch ||
                !string.Equals(delegation.SeriesId, payload.SeriesId, StringComparison.Ordinal) ||
                payload.NotBeforeUtc < delegation.NotBeforeUtc ||
                payload.ExpiresUtc > delegation.ExpiresUtc ||
                !trust.TryGetRole(delegation.KeyId, out O1PublicRoleKey? independent) ||
                independent is null ||
                independent.Role != delegation.Role ||
                !string.Equals(independent.PublicKey, delegation.PublicKey, StringComparison.Ordinal) ||
                !material.Add(O1CanonicalCryptography.PublicKeyDigest(delegation.PublicKey)) ||
                !O1CanonicalCryptography.Verify(
                    DelegationPayload(delegation),
                    delegation.RootSignature,
                    rootPublicKey))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Validates one exact dual approval against distinct expected roles and independently supplied keys.</summary>
    /// <param name="approval">Untrusted approval.</param>
    /// <param name="expectedKind">Required decision kind.</param>
    /// <param name="expectedHead">Exact precommitted head.</param>
    /// <param name="bundle">Candidate bundle payload.</param>
    /// <param name="trust">Independent public trust.</param>
    /// <param name="expectedRoles">Exact two approver roles.</param>
    /// <param name="nowUtc">Current instant.</param>
    /// <returns><see langword="true"/> only for exact valid dual control.</returns>
    internal static bool VerifyApproval(
        O1DualApproval approval,
        O1ApprovalKind expectedKind,
        string expectedHead,
        O1TrustBundlePayload bundle,
        O1PublicTrustContext trust,
        IReadOnlyList<O1TrustRole> expectedRoles,
        DateTimeOffset nowUtc)
    {
        if (approval.Kind != expectedKind ||
            approval.Signatures.Count != 2 ||
            approval.Signatures.Select(item => item.Role).Distinct().Count() != 2 ||
            approval.Signatures.Select(item => item.KeyId).Distinct(StringComparer.Ordinal).Count() != 2 ||
            !approval.Signatures.Select(item => item.Role).Order().SequenceEqual(expectedRoles.Order()) ||
            !string.Equals(approval.DomainId, bundle.DomainId, StringComparison.Ordinal) ||
            !string.Equals(approval.ProposedHeadDigest, expectedHead, StringComparison.Ordinal) ||
            approval.RecoveryEpoch != bundle.RecoveryEpoch ||
            !string.Equals(approval.SeriesId, bundle.SeriesId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(approval.Nonce) ||
            approval.Nonce.Length > 96 ||
            nowUtc < approval.NotBeforeUtc ||
            nowUtc >= approval.ExpiresUtc)
        {
            return false;
        }

        O1ApprovalPayload payload = ApprovalPayload(approval);
        foreach (O1ApprovalSignature signature in approval.Signatures)
        {
            if (!trust.TryGetRole(signature.KeyId, out O1PublicRoleKey? roleKey) ||
                roleKey is null ||
                roleKey.Role != signature.Role ||
                !O1CanonicalCryptography.Verify(
                    payload,
                    signature.Signature,
                    roleKey.PublicKey))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Creates the exact policy decision digest bound by dual control.</summary>
    /// <param name="payload">Bundle payload.</param>
    /// <returns>Policy-head digest.</returns>
    internal static string PolicyHead(O1TrustBundlePayload payload) =>
        O1CanonicalCryptography.Digest(
            new
            {
                payload.DomainId,
                payload.TenantId,
                payload.Environment,
                payload.Purpose,
                payload.Scope,
                payload.RecoveryEpoch,
                payload.SeriesId,
                payload.Resources,
                payload.NotBeforeUtc,
                payload.ExpiresUtc,
            });

    /// <summary>Projects one approval into its exact signature-free canonical payload.</summary>
    /// <param name="approval">Approval.</param>
    /// <returns>Canonical approval payload.</returns>
    internal static O1ApprovalPayload ApprovalPayload(O1DualApproval approval) =>
        new(
            approval.Kind,
            approval.DecisionId,
            approval.DomainId,
            approval.ProposedHeadDigest,
            approval.RecoveryEpoch,
            approval.SeriesId,
            approval.Nonce,
            approval.NotBeforeUtc,
            approval.ExpiresUtc);

    /// <summary>Projects one delegation into its exact signature-free canonical payload.</summary>
    /// <param name="delegation">Delegation.</param>
    /// <returns>Canonical delegation payload.</returns>
    internal static O1DelegationPayload DelegationPayload(O1RoleDelegation delegation) =>
        new(
            delegation.Role,
            delegation.KeyId,
            delegation.PublicKey,
            delegation.RecoveryEpoch,
            delegation.SeriesId,
            delegation.NotBeforeUtc,
            delegation.ExpiresUtc);

    /// <summary>Builds the complete next state only after every candidate component has passed validation.</summary>
    /// <param name="current">Current state.</param>
    /// <param name="bundle">Accepted candidate.</param>
    /// <param name="bundleDigest">Candidate digest.</param>
    /// <param name="correlationId">Bounded audit correlation identifier.</param>
    /// <returns>Complete detached candidate state.</returns>
    private static O1CheckpointState BuildCandidate(
        O1CheckpointState current,
        O1TrustBundle bundle,
        string bundleDigest,
        string correlationId)
    {
        O1CheckpointState candidate = current.Clone();
        candidate.Initialised = true;
        candidate.DomainId = bundle.Payload.DomainId;
        candidate.RootSetVersion = bundle.Payload.RootSetVersion;
        candidate.RootSetDigest = bundle.Payload.RootSetDigest;
        candidate.RecoveryEpochHighWater = bundle.Payload.RecoveryEpoch;
        candidate.SeriesId = bundle.Payload.SeriesId;
        candidate.Generation = bundle.Payload.Generation;
        candidate.BundleDigest = bundleDigest;
        candidate.RevocationHeadDigest = bundle.Payload.RevocationHeadDigest;
        candidate.ContextRevision = checked(current.ContextRevision + 1);
        candidate.Fence = checked(current.Fence + 1);
        candidate.Quarantined = false;
        candidate.QuarantineCode = string.Empty;
        candidate.CorpusHeads.Clear();
        candidate.CorpusHeads.Add(
            bundle.CorpusManifest.DatasetId,
            new O1CorpusHeadState
            {
                DatasetId = bundle.CorpusManifest.DatasetId,
                Revision = bundle.CorpusManifest.Revision,
                ManifestDigest = bundle.Payload.CorpusManifestDigest,
                Withdrawn = bundle.CorpusManifest.Withdrawn,
            });

        IEnumerable<string> nonces =
        [
            bundle.PolicyApproval.Nonce,
            bundle.CorpusManifest.Approval.Nonce,
        ];
        if (!current.Initialised || current.Quarantined)
        {
            nonces = nonces.Append(bundle.ContinuityApproval.Nonce);
        }
        foreach (string nonce in nonces.Distinct(StringComparer.Ordinal))
        {
            candidate.ConsumedApprovalNonces.Add(nonce);
        }
        candidate.AuditIntents.Add(
            new O1AuditIntent
            {
                Code = current.Quarantined
                    ? "trust.recovery_committed"
                    : "trust.bundle_committed",
                ContextRevision = candidate.ContextRevision,
                CorrelationId = correlationId,
            });
        return candidate;
    }

    /// <summary>Checks that every resource ceiling is finite and that O1 remains serial with no queue.</summary>
    /// <param name="resources">Signed resource envelope.</param>
    /// <returns><see langword="true"/> only for the authorised initial O1 boundary.</returns>
    private static bool ValidateFiniteResources(O1ResourceEnvelope resources) =>
        resources.MaximumInputBytes > 0 &&
        resources.MaximumStructureDepth > 0 &&
        resources.MaximumItems > 0 &&
        resources.MaximumAccountedMemoryBytes > 0 &&
        resources.MaximumWorkUnits > 0 &&
        resources.MaximumResults > 0 &&
        resources.MaximumOutputBytes > 0 &&
        resources.MaximumControlMetadataEntries > 0 &&
        resources.TotalDuration > TimeSpan.Zero &&
        resources.FirstByteDuration > TimeSpan.Zero &&
        resources.IdleDuration > TimeSpan.Zero &&
        resources.MaximumParallelism == 1 &&
        !resources.QueueEnabled;

    /// <summary>Creates one detached typed refusal without mutating durable state.</summary>
    /// <param name="code">Stable failure code.</param>
    /// <param name="state">Current state.</param>
    /// <returns>Non-authorising refusal.</returns>
    private static O1TrustAdmissionResult Refused(
        string code,
        O1CheckpointState state) =>
        new(false, false, state.Quarantined, code, state.Clone());
}
