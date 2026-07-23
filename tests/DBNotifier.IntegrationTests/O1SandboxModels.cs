// Module purpose: Defines immutable, synthetic O1 trust, checkpoint, corpus and resource contracts for the opt-in local sandbox only.
using System.Collections.ObjectModel;

namespace DBNotifier.IntegrationTests;

/// <summary>Names the cryptographically separated synthetic roles exercised by the O1 sandbox.</summary>
internal enum O1TrustRole
{
    Root,
    BootstrapApproverA,
    BootstrapApproverB,
    RecoveryApproverA,
    RecoveryApproverB,
    PolicyApproverA,
    PolicyApproverB,
    DataOwnerApprover,
    DataGovernanceApprover,
    PolicyAttestation,
    CorpusAttestation,
    GrantSigner,
    RevocationDecision,
    RevocationSnapshot,
    TrustBundle,
}

/// <summary>Classifies the one-use governance decisions accepted by the sandbox coordinator.</summary>
internal enum O1ApprovalKind
{
    Bootstrap,
    Recovery,
    Policy,
    Corpus,
}

/// <summary>Classifies deterministic local checkpoint commit faults used to prove atomic recovery.</summary>
internal enum O1CommitFault
{
    None,
    BeforeReplace,
    AfterReplaceBeforeWitness,
}

/// <summary>Represents one finite resource envelope bound into a signed synthetic trust bundle.</summary>
internal sealed record O1ResourceEnvelope(
    long MaximumInputBytes,
    int MaximumStructureDepth,
    int MaximumItems,
    long MaximumAccountedMemoryBytes,
    long MaximumWorkUnits,
    int MaximumResults,
    long MaximumOutputBytes,
    int MaximumControlMetadataEntries,
    TimeSpan TotalDuration,
    TimeSpan FirstByteDuration,
    TimeSpan IdleDuration,
    int MaximumParallelism,
    bool QueueEnabled)
{
    /// <summary>Returns a conservative finite fixture envelope with serial execution and no queue.</summary>
    /// <returns>A bounded test-only resource envelope.</returns>
    internal static O1ResourceEnvelope Fixture() =>
        new(
            64 * 1024,
            16,
            256,
            512 * 1024,
            100_000,
            64,
            64 * 1024,
            64,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(2),
            1,
            false);
}

/// <summary>Describes one independently rooted role key delegated for an exact synthetic epoch and series.</summary>
internal sealed record O1RoleDelegation(
    O1TrustRole Role,
    string KeyId,
    string PublicKey,
    long RecoveryEpoch,
    string SeriesId,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc,
    string RootSignature);

/// <summary>Describes one signature over an exact one-use governance decision.</summary>
internal sealed record O1ApprovalSignature(
    O1TrustRole Role,
    string KeyId,
    string Signature);

/// <summary>Describes one exact dual-control decision whose nonce is consumed atomically with checkpoint advancement.</summary>
internal sealed record O1DualApproval(
    O1ApprovalKind Kind,
    string DecisionId,
    string DomainId,
    string ProposedHeadDigest,
    long RecoveryEpoch,
    string SeriesId,
    string Nonce,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc,
    IReadOnlyList<O1ApprovalSignature> Signatures);

/// <summary>Describes one bounded synthetic corpus member and its immutable partition membership.</summary>
internal sealed record O1CorpusCase(
    string CaseId,
    string Partition,
    string Segment,
    string ContentDigest);

/// <summary>Describes an independently approved, role-attested synthetic corpus manifest.</summary>
internal sealed record O1CorpusManifest(
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
    IReadOnlyDictionary<string, int> ExpectedSegmentCounts,
    O1DualApproval Approval,
    string AttestationKeyId,
    string AttestationSignature)
{
    /// <summary>Creates immutable case and segment views so caller-owned collections cannot drift after admission.</summary>
    /// <returns>An equivalent manifest whose collections no longer alias caller state.</returns>
    internal O1CorpusManifest Freeze() =>
        this with
        {
            Cases = Array.AsReadOnly(Cases.ToArray()),
            ExpectedSegmentCounts = new ReadOnlyDictionary<string, int>(
                new SortedDictionary<string, int>(
                    ExpectedSegmentCounts.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value,
                        StringComparer.Ordinal),
                    StringComparer.Ordinal)),
        };
}

/// <summary>Describes the canonical payload signed by the synthetic trust-bundle role.</summary>
internal sealed record O1TrustBundlePayload(
    int SchemaVersion,
    string DomainId,
    string TenantId,
    string Environment,
    string Purpose,
    IReadOnlyList<string> Scope,
    long RootSetVersion,
    string RootSetDigest,
    long RecoveryEpoch,
    string SeriesId,
    string EpochContextId,
    long Generation,
    string PreviousBundleDigest,
    string RevocationHeadDigest,
    string CorpusManifestDigest,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc,
    O1ResourceEnvelope Resources,
    IReadOnlyList<O1RoleDelegation> Delegations,
    string AuditReference);

/// <summary>Combines one canonical bundle payload with its independent approvals, manifest and role signature.</summary>
internal sealed record O1TrustBundle(
    O1TrustBundlePayload Payload,
    O1DualApproval ContinuityApproval,
    O1DualApproval PolicyApproval,
    O1CorpusManifest CorpusManifest,
    string BundleKeyId,
    string BundleSignature);

/// <summary>Records one bounded corpus head committed atomically with the trust checkpoint.</summary>
internal sealed class O1CorpusHeadState
{
    /// <summary>Gets or sets the stable synthetic dataset identifier.</summary>
    public string DatasetId { get; set; } = string.Empty;

    /// <summary>Gets or sets the positive monotonic corpus revision.</summary>
    public long Revision { get; set; }

    /// <summary>Gets or sets the accepted manifest digest.</summary>
    public string ManifestDigest { get; set; } = string.Empty;

    /// <summary>Gets or sets whether withdrawal has become irreversible in the current epoch.</summary>
    public bool Withdrawn { get; set; }
}

/// <summary>Records one sanitised, bounded audit intent committed in the same local state transaction.</summary>
internal sealed class O1AuditIntent
{
    /// <summary>Gets or sets the stable event code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the committed context revision.</summary>
    public long ContextRevision { get; set; }

    /// <summary>Gets or sets the exact synthetic correlation identifier.</summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>Represents the complete atomic O1 sandbox state; no field is published independently.</summary>
internal sealed class O1CheckpointState
{
    /// <summary>Gets or sets the local schema version.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets whether the trust domain has completed its one permitted bootstrap.</summary>
    public bool Initialised { get; set; }

    /// <summary>Gets or sets the stable domain identifier.</summary>
    public string DomainId { get; set; } = string.Empty;

    /// <summary>Gets or sets the positive accepted root-set version.</summary>
    public long RootSetVersion { get; set; }

    /// <summary>Gets or sets the accepted root-set digest.</summary>
    public string RootSetDigest { get; set; } = string.Empty;

    /// <summary>Gets or sets the positive recovery epoch high-water.</summary>
    public long RecoveryEpochHighWater { get; set; }

    /// <summary>Gets or sets the immutable series selected inside the current epoch.</summary>
    public string SeriesId { get; set; } = string.Empty;

    /// <summary>Gets or sets the current subordinate bundle generation.</summary>
    public long Generation { get; set; }

    /// <summary>Gets or sets the accepted bundle digest.</summary>
    public string BundleDigest { get; set; } = string.Empty;

    /// <summary>Gets or sets the accepted cumulative revocation head.</summary>
    public string RevocationHeadDigest { get; set; } = string.Empty;

    /// <summary>Gets or sets the immutable published context revision.</summary>
    public long ContextRevision { get; set; }

    /// <summary>Gets or sets the monotonic coordinator fence.</summary>
    public long Fence { get; set; }

    /// <summary>Gets the bounded authorised corpus heads keyed by dataset identifier.</summary>
    public SortedDictionary<string, O1CorpusHeadState> CorpusHeads { get; init; } =
        new(StringComparer.Ordinal);

    /// <summary>Gets the bounded set of consumed one-use approval nonces.</summary>
    public List<string> ConsumedApprovalNonces { get; init; } = [];

    /// <summary>Gets the bounded sanitised audit intents committed with the checkpoint.</summary>
    public List<O1AuditIntent> AuditIntents { get; init; } = [];

    /// <summary>Gets or sets whether continuity is quarantined.</summary>
    public bool Quarantined { get; set; }

    /// <summary>Gets or sets the stable quarantine reason without untrusted detail.</summary>
    public string QuarantineCode { get; set; } = string.Empty;

    /// <summary>Creates a deep copy suitable for isolated candidate construction.</summary>
    /// <returns>A detached copy of the complete checkpoint state.</returns>
    internal O1CheckpointState Clone()
    {
        O1CheckpointState clone = new()
        {
            SchemaVersion = SchemaVersion,
            Initialised = Initialised,
            DomainId = DomainId,
            RootSetVersion = RootSetVersion,
            RootSetDigest = RootSetDigest,
            RecoveryEpochHighWater = RecoveryEpochHighWater,
            SeriesId = SeriesId,
            Generation = Generation,
            BundleDigest = BundleDigest,
            RevocationHeadDigest = RevocationHeadDigest,
            ContextRevision = ContextRevision,
            Fence = Fence,
            Quarantined = Quarantined,
            QuarantineCode = QuarantineCode,
        };
        foreach ((string key, O1CorpusHeadState value) in CorpusHeads)
        {
            clone.CorpusHeads.Add(
                key,
                new O1CorpusHeadState
                {
                    DatasetId = value.DatasetId,
                    Revision = value.Revision,
                    ManifestDigest = value.ManifestDigest,
                    Withdrawn = value.Withdrawn,
                });
        }
        clone.ConsumedApprovalNonces.AddRange(ConsumedApprovalNonces);
        clone.AuditIntents.AddRange(AuditIntents.Select(
            intent => new O1AuditIntent
            {
                Code = intent.Code,
                ContextRevision = intent.ContextRevision,
                CorrelationId = intent.CorrelationId,
            }));
        return clone;
    }
}

/// <summary>Represents the immutable outcome of one trust candidate admission attempt.</summary>
internal sealed record O1TrustAdmissionResult(
    bool Accepted,
    bool Idempotent,
    bool Quarantined,
    string Code,
    O1CheckpointState State);

/// <summary>Represents one finite evaluation request presented to the O1 resource coordinator.</summary>
internal sealed record O1ResourceRequest(
    long InputBytes,
    int StructureDepth,
    int Items,
    long AccountedMemoryBytes,
    long WorkUnits,
    int Results,
    long OutputBytes);

/// <summary>Maps one deterministic ADR-0007 vector to its enforcing component, executable test and accountable owner.</summary>
internal sealed record O1VectorTrace(
    string Id,
    string Component,
    string Test,
    string Owner);
