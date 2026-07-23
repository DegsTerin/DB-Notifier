// Module purpose: Builds bounded, independently keyed synthetic O1 trust and corpus candidates without retaining or persisting private material.
using System.Collections.ObjectModel;

namespace DBNotifier.IntegrationTests;

/// <summary>Owns two ephemeral trust hierarchies used to prove ordinary continuity and separately authorised recovery.</summary>
internal sealed class O1SyntheticTrustFixture : IDisposable
{
    private static readonly DateTimeOffset ValidFrom = new(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidUntil = new(2026, 7, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] CanonicalScope = ["instance:synthetic-alpha", "purpose:capacity"];
    private readonly O1SyntheticKeyRing primary = new("primary");
    private readonly O1SyntheticKeyRing recovery = new("recovery");

    /// <summary>Initialises independently rooted public trust for both the ordinary and recovery fixture hierarchies.</summary>
    internal O1SyntheticTrustFixture()
    {
        Dictionary<string, string> roots = new(StringComparer.Ordinal);
        Dictionary<string, O1PublicRoleKey> roles = new(StringComparer.Ordinal);
        AddPublicHierarchy(primary, roots, roles);
        AddPublicHierarchy(recovery, roots, roles);
        Trust = new O1PublicTrustContext(roots, roles);
    }

    /// <summary>Gets the immutable public verification context; it contains no private material.</summary>
    internal O1PublicTrustContext Trust { get; }

    /// <summary>Gets the deterministic instant inside every fixture validity window.</summary>
    internal static DateTimeOffset NowUtc => ValidFrom.AddHours(1);

    /// <summary>Gets the exact sorted scope used by all valid candidates.</summary>
    internal static IReadOnlyList<string> Scope => CanonicalScope;

    /// <summary>Builds the one valid generation-one bootstrap candidate.</summary>
    /// <returns>A fully signed, independently approved synthetic bundle.</returns>
    internal O1TrustBundle Bootstrap() =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            recoveryEpoch: 1,
            seriesId: "series-primary",
            generation: 1,
            previousBundleDigest: string.Empty,
            corpusRevision: 1,
            previousManifestDigest: string.Empty,
            auditReference: "audit-bootstrap");

    /// <summary>Builds the direct next candidate for an accepted non-quarantined checkpoint.</summary>
    /// <param name="current">Accepted predecessor.</param>
    /// <returns>A fully signed direct successor.</returns>
    internal O1TrustBundle Successor(O1CheckpointState current) =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            current.RecoveryEpochHighWater,
            current.SeriesId,
            checked(current.Generation + 1),
            current.BundleDigest,
            checked(CurrentCorpus(current).Revision + 1),
            CurrentCorpus(current).ManifestDigest,
            $"audit-successor-{current.Generation + 1}");

    /// <summary>Builds a validly signed generation gap that must quarantine continuity.</summary>
    /// <param name="current">Accepted predecessor.</param>
    /// <returns>A cryptographically valid but discontinuous candidate.</returns>
    internal O1TrustBundle Gap(O1CheckpointState current) =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            current.RecoveryEpochHighWater,
            current.SeriesId,
            checked(current.Generation + 2),
            current.BundleDigest,
            checked(CurrentCorpus(current).Revision + 1),
            CurrentCorpus(current).ManifestDigest,
            "audit-gap");

    /// <summary>Builds a same-generation divergent view whose corpus head remains idempotent.</summary>
    /// <param name="current">Accepted predecessor.</param>
    /// <returns>A validly signed split-view candidate.</returns>
    internal O1TrustBundle Divergence(O1CheckpointState current) =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            current.RecoveryEpochHighWater,
            current.SeriesId,
            current.Generation,
            current.BundleDigest,
            CurrentCorpus(current).Revision,
            PreviousManifestDigest(CurrentCorpus(current)),
            "audit-divergent-view");

    /// <summary>Builds a lower-generation candidate to prove rollback refusal.</summary>
    /// <param name="current">Accepted predecessor whose generation is greater than one.</param>
    /// <returns>A validly signed lower-generation candidate.</returns>
    internal O1TrustBundle Rollback(O1CheckpointState current) =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            current.RecoveryEpochHighWater,
            current.SeriesId,
            Math.Max(1, current.Generation - 1),
            string.Empty,
            CurrentCorpus(current).Revision,
            PreviousManifestDigest(CurrentCorpus(current)),
            "audit-rollback");

    /// <summary>Builds the only eligible recovery candidate from a quarantined checkpoint.</summary>
    /// <param name="current">Quarantined predecessor.</param>
    /// <returns>A separately rooted, dual-approved epoch successor.</returns>
    internal O1TrustBundle Recovery(O1CheckpointState current) =>
        Build(
            recovery,
            O1ApprovalKind.Recovery,
            checked(current.RecoveryEpochHighWater + 1),
            "series-recovery",
            generation: 1,
            previousBundleDigest: current.BundleDigest,
            corpusRevision: 1,
            previousManifestDigest: string.Empty,
            auditReference: "audit-recovery");

    /// <summary>Builds a fresh signed candidate with the supplied finite envelope for resource-policy adversarial tests.</summary>
    /// <param name="resources">Resource envelope to bind into all signatures.</param>
    /// <returns>A bootstrap-shaped candidate.</returns>
    internal O1TrustBundle BootstrapWithResources(O1ResourceEnvelope resources) =>
        Build(
            primary,
            O1ApprovalKind.Bootstrap,
            recoveryEpoch: 1,
            seriesId: "series-primary",
            generation: 1,
            previousBundleDigest: string.Empty,
            corpusRevision: 1,
            previousManifestDigest: string.Empty,
            auditReference: "audit-resource-envelope",
            resources);

    /// <summary>Disposes both independent ephemeral private-key hierarchies.</summary>
    public void Dispose()
    {
        primary.Dispose();
        recovery.Dispose();
    }

    /// <summary>Builds one fully signed candidate after every hash-bound field is known.</summary>
    private static O1TrustBundle Build(
        O1SyntheticKeyRing ring,
        O1ApprovalKind continuityKind,
        long recoveryEpoch,
        string seriesId,
        long generation,
        string previousBundleDigest,
        long corpusRevision,
        string previousManifestDigest,
        string auditReference,
        O1ResourceEnvelope? resources = null)
    {
        O1CorpusManifestPayload corpusPayload = CorpusPayload(
            recoveryEpoch,
            seriesId,
            corpusRevision,
            previousManifestDigest);
        string corpusDigest = O1CanonicalCryptography.Digest(corpusPayload);
        O1DualApproval corpusApproval = Approval(
            ring,
            O1ApprovalKind.Corpus,
            corpusDigest,
            recoveryEpoch,
            seriesId,
            [O1TrustRole.DataOwnerApprover, O1TrustRole.DataGovernanceApprover],
            $"corpus-{recoveryEpoch}-{corpusRevision}");
        O1CorpusManifest corpus = new(
            corpusPayload.DatasetId,
            corpusPayload.Revision,
            corpusPayload.PreviousManifestDigest,
            corpusPayload.TenantId,
            corpusPayload.Environment,
            corpusPayload.Purpose,
            corpusPayload.RecoveryEpoch,
            corpusPayload.SeriesId,
            corpusPayload.NotBeforeUtc,
            corpusPayload.ExpiresUtc,
            corpusPayload.Withdrawn,
            Array.AsReadOnly(corpusPayload.Cases.ToArray()),
            new ReadOnlyDictionary<string, int>(
                corpusPayload.ExpectedSegmentCounts.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.Ordinal)),
            corpusApproval,
            ring.KeyId(O1TrustRole.CorpusAttestation),
            ring.Sign(O1TrustRole.CorpusAttestation, corpusPayload));

        string rootPublicKey = ring.PublicKey(O1TrustRole.Root);
        O1TrustBundlePayload payload = new(
            SchemaVersion: 1,
            DomainId: "o1-synthetic-domain",
            TenantId: "tenant-synthetic",
            Environment: "test-only",
            Purpose: "capacity",
            Scope: Array.AsReadOnly(CanonicalScope.ToArray()),
            RootSetVersion: recoveryEpoch,
            RootSetDigest: O1CanonicalCryptography.PublicKeyDigest(rootPublicKey),
            RecoveryEpoch: recoveryEpoch,
            SeriesId: seriesId,
            EpochContextId: $"epoch-{recoveryEpoch}-{seriesId}",
            Generation: generation,
            PreviousBundleDigest: previousBundleDigest,
            RevocationHeadDigest: O1CanonicalCryptography.TextDigest(
                $"revocation-{recoveryEpoch}-{generation}"),
            CorpusManifestDigest: corpusDigest,
            NotBeforeUtc: ValidFrom,
            ExpiresUtc: ValidUntil,
            Resources: resources ?? O1ResourceEnvelope.Fixture(),
            Delegations: Delegations(ring, recoveryEpoch, seriesId),
            AuditReference: auditReference);

        string bundleDigest = O1CanonicalCryptography.Digest(payload);
        O1DualApproval continuity = Approval(
            ring,
            continuityKind,
            bundleDigest,
            recoveryEpoch,
            seriesId,
            continuityKind == O1ApprovalKind.Recovery
                ? [O1TrustRole.RecoveryApproverA, O1TrustRole.RecoveryApproverB]
                : [O1TrustRole.BootstrapApproverA, O1TrustRole.BootstrapApproverB],
            $"continuity-{recoveryEpoch}-{generation}-{auditReference}");
        O1DualApproval policy = Approval(
            ring,
            O1ApprovalKind.Policy,
            O1TrustCoordinator.PolicyHead(payload),
            recoveryEpoch,
            seriesId,
            [O1TrustRole.PolicyApproverA, O1TrustRole.PolicyApproverB],
            $"policy-{recoveryEpoch}-{generation}-{auditReference}");
        return new O1TrustBundle(
            payload,
            continuity,
            policy,
            corpus.Freeze(),
            ring.KeyId(O1TrustRole.TrustBundle),
            ring.Sign(O1TrustRole.TrustBundle, payload));
    }

    /// <summary>Builds the exact two-signature decision payload for one governance domain.</summary>
    private static O1DualApproval Approval(
        O1SyntheticKeyRing ring,
        O1ApprovalKind kind,
        string proposedHead,
        long recoveryEpoch,
        string seriesId,
        IReadOnlyList<O1TrustRole> roles,
        string suffix)
    {
        O1DualApproval unsigned = new(
            kind,
            $"decision-{suffix}",
            "o1-synthetic-domain",
            proposedHead,
            recoveryEpoch,
            seriesId,
            $"nonce-{suffix}",
            ValidFrom,
            ValidUntil,
            []);
        O1ApprovalPayload payload = O1TrustCoordinator.ApprovalPayload(unsigned);
        O1ApprovalSignature[] signatures = roles
            .Select(
                role => new O1ApprovalSignature(
                    role,
                    ring.KeyId(role),
                    ring.Sign(role, payload)))
            .ToArray();
        return unsigned with { Signatures = Array.AsReadOnly(signatures) };
    }

    /// <summary>Builds every non-root role delegation under the exact independent root.</summary>
    private static ReadOnlyCollection<O1RoleDelegation> Delegations(
        O1SyntheticKeyRing ring,
        long recoveryEpoch,
        string seriesId)
    {
        List<O1RoleDelegation> delegations = [];
        foreach (O1TrustRole role in Enum.GetValues<O1TrustRole>().Where(role => role != O1TrustRole.Root))
        {
            O1RoleDelegation unsigned = new(
                role,
                ring.KeyId(role),
                ring.PublicKey(role),
                recoveryEpoch,
                seriesId,
                ValidFrom,
                ValidUntil,
                string.Empty);
            delegations.Add(
                unsigned with
                {
                    RootSignature = ring.Sign(
                        O1TrustRole.Root,
                        O1TrustCoordinator.DelegationPayload(unsigned)),
                });
        }
        return Array.AsReadOnly(delegations.ToArray());
    }

    /// <summary>Builds a small exact-membership corpus with two partitions and three declared segments.</summary>
    private static O1CorpusManifestPayload CorpusPayload(
        long recoveryEpoch,
        string seriesId,
        long revision,
        string previousManifestDigest)
    {
        O1CorpusCase[] cases =
        [
            new("case-train-a", "train", "segment-a", O1CanonicalCryptography.TextDigest("train-a")),
            new("case-train-b", "train", "segment-b", O1CanonicalCryptography.TextDigest("train-b")),
            new("case-holdout", "holdout", "segment-c", O1CanonicalCryptography.TextDigest("holdout")),
        ];
        SortedDictionary<string, int> counts = new(StringComparer.Ordinal)
        {
            ["segment-a"] = 1,
            ["segment-b"] = 1,
            ["segment-c"] = 1,
        };
        return new O1CorpusManifestPayload(
            "dataset-synthetic-capacity",
            revision,
            previousManifestDigest,
            "tenant-synthetic",
            "test-only",
            "capacity",
            recoveryEpoch,
            seriesId,
            ValidFrom,
            ValidUntil,
            Withdrawn: false,
            Array.AsReadOnly(cases.OrderBy(item => item.CaseId, StringComparer.Ordinal).ToArray()),
            new ReadOnlyDictionary<string, int>(counts));
    }

    /// <summary>Adds one hierarchy's public roots and role bindings to the independent verification context.</summary>
    private static void AddPublicHierarchy(
        O1SyntheticKeyRing ring,
        Dictionary<string, string> roots,
        Dictionary<string, O1PublicRoleKey> roles)
    {
        string rootPublicKey = ring.PublicKey(O1TrustRole.Root);
        roots.Add(O1CanonicalCryptography.PublicKeyDigest(rootPublicKey), rootPublicKey);
        foreach (O1TrustRole role in Enum.GetValues<O1TrustRole>().Where(role => role != O1TrustRole.Root))
        {
            roles.Add(
                ring.KeyId(role),
                new O1PublicRoleKey(role, ring.PublicKey(role)));
        }
    }

    /// <summary>Returns the fixture's single bounded corpus head.</summary>
    private static O1CorpusHeadState CurrentCorpus(O1CheckpointState current) =>
        current.CorpusHeads.Values.Single();

    /// <summary>Reconstructs the deterministic predecessor digest expected by an idempotent corpus payload.</summary>
    private static string PreviousManifestDigest(O1CorpusHeadState head) =>
        head.Revision == 1
            ? string.Empty
            : O1CanonicalCryptography.Digest(
                CorpusPayload(
                    recoveryEpoch: 1,
                    seriesId: "series-primary",
                    revision: head.Revision - 1,
                    previousManifestDigest: head.Revision == 2 ? string.Empty : "not-used-by-current-fixtures"));
}
