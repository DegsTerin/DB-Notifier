// Module purpose: Calibrates and evaluates deterministic MOD-12 rules against the governed O3-A synthetic corpus in an opt-in sandbox.
using System.Collections.ObjectModel;
using System.Text.Json;
using DBNotifier.Application.AIOps;

namespace DBNotifier.IntegrationTests;

/// <summary>Classifies bounded O3-B calibration and holdout outcomes without granting runtime authority.</summary>
internal enum O3BEvaluationDisposition
{
    Accepted,
    Rejected,
    Quarantined,
}

/// <summary>Declares one frozen deterministic threshold selected only from development and calibration members.</summary>
internal sealed record O3BFrozenRule(
    string Segment,
    ObserverThresholdComparison Comparison,
    double Threshold);

/// <summary>Declares quantitative synthetic holdout criteria before any holdout member is read.</summary>
internal sealed record O3BMetricCriterion(
    string Segment,
    double MinimumCoverage,
    double MaximumAbstentionRate,
    int MaximumFalsePositives,
    int MaximumFalseNegatives,
    double MaximumMeanAbsolutePredictionError,
    double MinimumStability,
    double MinimumExplainability,
    long MaximumAccountedDurationUnits,
    long MaximumAccountedMemoryBytes,
    long MaximumWorkUnits);

/// <summary>Binds a frozen policy to one exact governed corpus revision and its non-holdout calibration material.</summary>
internal sealed record O3BFrozenPolicyPayload(
    int SchemaVersion,
    string PolicyId,
    long PolicyRevision,
    string CorpusDatasetId,
    long CorpusRevision,
    string CorpusManifestDigest,
    string CorpusMembershipDigest,
    string CorpusCriteriaDigest,
    string DevelopmentDigest,
    string CalibrationDigest,
    DateTimeOffset FrozenAtUtc,
    bool Frozen,
    bool ProductionRepresentative,
    bool IsAuthorising,
    IReadOnlyList<O3BFrozenRule> Rules,
    IReadOnlyList<O3BMetricCriterion> Criteria);

/// <summary>Records one exact role signature over an immutable O3-B policy or result.</summary>
internal sealed record O3BSignature(
    O1TrustRole Role,
    string KeyId,
    string Signature);

/// <summary>Combines a frozen policy with dual approval, attestation and public synthetic verification material.</summary>
internal sealed record O3BFrozenPolicyPackage(
    O3BFrozenPolicyPayload Policy,
    string PolicyDigest,
    IReadOnlyList<O3BSignature> Approvals,
    O3BSignature Attestation,
    IReadOnlyDictionary<string, string> PublicKeys);

/// <summary>Reports complete provider-neutral holdout measurements for one declared O3-A segment.</summary>
internal sealed record O3BSegmentMetrics(
    string Segment,
    int CaseCount,
    int EvaluatedCount,
    int AbstentionCount,
    int TruePositiveCount,
    int FalsePositiveCount,
    int FalseNegativeCount,
    int TrueNegativeCount,
    double Coverage,
    double AbstentionRate,
    double MeanAbsolutePredictionError,
    double Stability,
    double Explainability,
    long AccountedDurationUnits,
    long AccountedMemoryBytes,
    long WorkUnits,
    bool CriteriaPassed);

/// <summary>Contains one complete, content-addressed synthetic holdout result without case payloads.</summary>
internal sealed record O3BEvaluationPayload(
    int SchemaVersion,
    string EvaluationId,
    string CorpusManifestDigest,
    long CorpusRevision,
    string PolicyDigest,
    DateTimeOffset EvaluatedAtUtc,
    bool ProcessingCompleted,
    bool SyntheticApproved,
    bool ProductionRepresentative,
    bool IsAuthorising,
    string Code,
    IReadOnlyList<O3BSegmentMetrics> SegmentMetrics);

/// <summary>Authenticates one exact aggregate O3-B result against its frozen policy attestation key.</summary>
internal sealed record O3BEvaluationPackage(
    O3BEvaluationPayload Payload,
    string ResultDigest,
    O3BSignature Attestation);

/// <summary>Bounds synthetic holdout cases, deterministic work, accounted memory and the evaluation deadline.</summary>
internal sealed record O3BEvaluationBudget(
    int MaximumCases,
    long MaximumWorkUnits,
    long MaximumAccountedMemoryBytes,
    DateTimeOffset DeadlineUtc);

/// <summary>Tracks the last accepted corpus/policy pair to reject holdout reuse and corpus rollback.</summary>
internal sealed record O3BEvaluationHead(
    long CorpusRevision,
    string CorpusManifestDigest,
    string PolicyDigest,
    string ResultDigest);

/// <summary>Returns a bounded O3-B result code, aggregate package and optional accepted continuity head.</summary>
internal sealed record O3BEvaluationOutcome(
    O3BEvaluationDisposition Disposition,
    string Code,
    O3BEvaluationPackage? Package,
    O3BEvaluationHead? Head);

/// <summary>
/// Mediates partition access so calibration cannot enumerate holdout members and holdout cannot open before policy
/// freeze.
/// </summary>
internal sealed class O3BPartitionGate
{
    private readonly O3ACorpusPackage corpus;
    private int holdoutReadCount;

    /// <summary>Initialises one gate over an already admitted immutable O3-A package.</summary>
    /// <param name="corpus">Exact authenticated corpus package.</param>
    internal O3BPartitionGate(O3ACorpusPackage corpus)
    {
        this.corpus = corpus;
    }

    /// <summary>Gets the number of holdout openings for one-use enforcement evidence.</summary>
    internal int HoldoutReadCount => holdoutReadCount;

    /// <summary>Returns only development and calibration members in canonical order.</summary>
    /// <returns>Immutable non-holdout content paired with its declared partition.</returns>
    internal IReadOnlyList<(O3ACorpusPartition Partition, O3ACorpusContent Content)> ReadCalibrationMaterial()
    {
        Dictionary<string, O3ACorpusContent> contents = corpus.Contents.ToDictionary(
            item => item.CaseId,
            StringComparer.Ordinal);
        return Array.AsReadOnly(
            corpus.Manifest.Members
                .Where(item => item.Partition is O3ACorpusPartition.Development or O3ACorpusPartition.Calibration)
                .OrderBy(item => item.Partition)
                .ThenBy(item => item.CaseId, StringComparer.Ordinal)
                .Select(item => (item.Partition, contents[item.CaseId]))
                .ToArray());
    }

    /// <summary>Opens immutable holdout members only after an authenticated policy is frozen.</summary>
    /// <param name="policy">Verified frozen policy bound to the exact corpus.</param>
    /// <param name="openedAtUtc">Trusted holdout opening instant strictly after freeze.</param>
    /// <returns>Immutable holdout content in canonical order.</returns>
    /// <exception cref="InvalidOperationException">Thrown for pre-freeze or repeated holdout access.</exception>
    internal IReadOnlyList<O3ACorpusContent> ReadHoldout(
        O3BFrozenPolicyPayload policy,
        DateTimeOffset openedAtUtc)
    {
        if (!policy.Frozen || openedAtUtc <= policy.FrozenAtUtc)
        {
            throw new InvalidOperationException("Holdout access requires an earlier frozen policy.");
        }
        if (Interlocked.Increment(ref holdoutReadCount) != 1)
        {
            throw new InvalidOperationException("Holdout access is one-use.");
        }

        Dictionary<string, O3ACorpusContent> contents = corpus.Contents.ToDictionary(
            item => item.CaseId,
            StringComparer.Ordinal);
        return Array.AsReadOnly(
            corpus.Manifest.Members
                .Where(item => item.Partition == O3ACorpusPartition.Holdout)
                .OrderBy(item => item.CaseId, StringComparer.Ordinal)
                .Select(item => contents[item.CaseId])
                .ToArray());
    }
}

/// <summary>Creates and authenticates bounded O3-B policies and aggregate results using ephemeral distinct role keys.</summary>
internal sealed class O3BSyntheticPolicyAuthority : IDisposable
{
    private static readonly string[] RequiredSegments = ["availability", "capacity", "latency"];
    private readonly O1SyntheticKeyRing keys = new("o3b");

    /// <summary>
    /// Calibrates bounded threshold directions from non-holdout material and freezes criteria before holdout access.
    /// </summary>
    /// <param name="corpus">Accepted O3-A package.</param>
    /// <param name="head">Verified exact O3-A head.</param>
    /// <param name="gate">Partition gate that exposes no holdout content during calibration.</param>
    /// <param name="frozenAtUtc">Trusted policy freeze instant.</param>
    /// <returns>Dual-approved, attested and content-addressed frozen policy.</returns>
    internal O3BFrozenPolicyPackage Freeze(
        O3ACorpusPackage corpus,
        O3ACorpusHead head,
        O3BPartitionGate gate,
        DateTimeOffset frozenAtUtc)
    {
        IReadOnlyList<(O3ACorpusPartition Partition, O3ACorpusContent Content)> material =
            gate.ReadCalibrationMaterial();
        O3BFrozenRule[] rules = RequiredSegments
            .Select(segment => CalibrateRule(segment, material.Select(item => item.Content).ToArray()))
            .OrderBy(item => item.Segment, StringComparer.Ordinal)
            .ToArray();
        O3BMetricCriterion[] criteria = RequiredSegments
            .Select(
                segment => new O3BMetricCriterion(
                    segment,
                    1d,
                    0d,
                    1,
                    0,
                    1d,
                    1d,
                    1d,
                    1,
                    4_096,
                    4))
            .ToArray();
        O3BFrozenPolicyPayload policy = new(
            1,
            "o3b-deterministic-threshold-policy",
            1,
            head.DatasetId,
            head.Revision,
            head.ManifestDigest,
            head.MembershipDigest,
            head.CriteriaDigest,
            PartitionDigest(material, O3ACorpusPartition.Development),
            PartitionDigest(material, O3ACorpusPartition.Calibration),
            frozenAtUtc,
            true,
            false,
            false,
            Array.AsReadOnly(rules),
            Array.AsReadOnly(criteria));
        return Sign(policy);
    }

    /// <summary>Signs a deliberate policy variant for adversarial verification tests.</summary>
    /// <param name="policy">Exact policy payload.</param>
    /// <returns>Authenticated policy package.</returns>
    internal O3BFrozenPolicyPackage Sign(O3BFrozenPolicyPayload policy)
    {
        string digest = O1CanonicalCryptography.Digest(policy);
        O3BSignature Signature(O1TrustRole role) =>
            new(role, keys.KeyId(role), keys.Sign(role, policy));
        O3BSignature[] approvals =
        [
            Signature(O1TrustRole.PolicyApproverA),
            Signature(O1TrustRole.PolicyApproverB),
        ];
        O3BSignature attestation = Signature(O1TrustRole.PolicyAttestation);
        Dictionary<string, string> publicKeys = approvals
            .Append(attestation)
            .ToDictionary(item => item.KeyId, item => keys.PublicKey(item.Role), StringComparer.Ordinal);
        return new(
            policy,
            digest,
            Array.AsReadOnly(approvals),
            attestation,
            new ReadOnlyDictionary<string, string>(publicKeys));
    }

    /// <summary>Authenticates one exact aggregate evaluation payload with the policy attestation role.</summary>
    /// <param name="payload">Complete aggregate synthetic result.</param>
    /// <returns>Content-addressed result package.</returns>
    internal O3BEvaluationPackage Attest(O3BEvaluationPayload payload) =>
        new(
            payload,
            O1CanonicalCryptography.Digest(payload),
            new(
                O1TrustRole.PolicyAttestation,
                keys.KeyId(O1TrustRole.PolicyAttestation),
                keys.Sign(O1TrustRole.PolicyAttestation, payload)));

    /// <summary>Disposes every ephemeral signing key without persistence.</summary>
    public void Dispose() => keys.Dispose();

    /// <summary>Selects one transparent threshold direction and midpoint from development and calibration only.</summary>
    private static O3BFrozenRule CalibrateRule(string segment, IReadOnlyList<O3ACorpusContent> material)
    {
        O3ACorpusContent[] segmentCases = material
            .Where(item => item.Segment == segment)
            .OrderBy(item => item.NormalisedValue)
            .ToArray();
        O3ACorpusContent[] critical = segmentCases.Where(item => item.Outcome == "critical").ToArray();
        O3ACorpusContent[] nonCritical = segmentCases.Where(item => item.Outcome != "critical").ToArray();
        if (critical.Length == 0)
        {
            double extrapolated = Math.Round(Math.Min(1d, segmentCases.Max(item => item.NormalisedValue) + 0.3d), 6);
            return new(segment, ObserverThresholdComparison.GreaterThanOrEqual, extrapolated);
        }

        double criticalMean = critical.Average(item => item.NormalisedValue);
        double nonCriticalMean = nonCritical.Average(item => item.NormalisedValue);
        double threshold = Math.Round((criticalMean + nonCriticalMean) / 2d, 6);
        return criticalMean >= nonCriticalMean
            ? new(segment, ObserverThresholdComparison.GreaterThanOrEqual, threshold)
            : new(segment, ObserverThresholdComparison.LessThanOrEqual, threshold);
    }

    /// <summary>Computes a canonical digest for exactly one non-holdout partition.</summary>
    private static string PartitionDigest(
        IReadOnlyList<(O3ACorpusPartition Partition, O3ACorpusContent Content)> material,
        O3ACorpusPartition partition) =>
        O1CanonicalCryptography.Digest(
            material
                .Where(item => item.Partition == partition)
                .Select(item => item.Content)
                .OrderBy(item => item.CaseId, StringComparer.Ordinal)
                .ToArray());
}

/// <summary>Verifies frozen policy authority, exact corpus binding, criteria completeness and aggregate result integrity.</summary>
internal static class O3BGovernanceVerifier
{
    internal const string ActivationMarker = "o3b-governed-offline-evaluation-sandbox";
    private static readonly string[] RequiredSegments = ["availability", "capacity", "latency"];

    /// <summary>Verifies a frozen policy against the exact accepted O3-A corpus head.</summary>
    /// <param name="package">Untrusted policy package.</param>
    /// <param name="corpus">Exact admitted corpus.</param>
    /// <param name="head">Accepted corpus head.</param>
    /// <param name="nowUtc">Trusted verification instant.</param>
    /// <returns>A bounded accepted, rejected or quarantined code.</returns>
    internal static (O3BEvaluationDisposition Disposition, string Code) VerifyPolicy(
        O3BFrozenPolicyPackage? package,
        O3ACorpusPackage corpus,
        O3ACorpusHead head,
        DateTimeOffset nowUtc)
    {
        try
        {
            if (package is null ||
                package.Policy is null ||
                package.Approvals is null ||
                package.Attestation is null ||
                package.PublicKeys is null)
            {
                return Reject("o3b.policy.absent");
            }

            O3BFrozenPolicyPayload policy = package.Policy;
            if (policy.SchemaVersion != 1 ||
                policy.PolicyRevision != 1 ||
                policy.PolicyId != "o3b-deterministic-threshold-policy" ||
                !policy.Frozen ||
                policy.ProductionRepresentative ||
                policy.IsAuthorising ||
                policy.FrozenAtUtc >= nowUtc ||
                policy.CorpusDatasetId != head.DatasetId ||
                policy.CorpusRevision != head.Revision ||
                policy.CorpusManifestDigest != head.ManifestDigest ||
                policy.CorpusMembershipDigest != head.MembershipDigest ||
                policy.CorpusCriteriaDigest != head.CriteriaDigest ||
                package.PolicyDigest != O1CanonicalCryptography.Digest(policy) ||
                !CompleteSegments(policy.Rules.Select(item => item.Segment)) ||
                !CompleteSegments(policy.Criteria.Select(item => item.Segment)) ||
                policy.Rules.Any(item => !double.IsFinite(item.Threshold) || item.Threshold is < 0 or > 1) ||
                policy.Criteria.Any(item => !ValidCriterion(item)))
            {
                return Reject("o3b.policy.invalid");
            }

            O3BPartitionGate gate = new(corpus);
            IReadOnlyList<(O3ACorpusPartition Partition, O3ACorpusContent Content)> material =
                gate.ReadCalibrationMaterial();
            if (policy.DevelopmentDigest != PartitionDigest(material, O3ACorpusPartition.Development) ||
                policy.CalibrationDigest != PartitionDigest(material, O3ACorpusPartition.Calibration))
            {
                return Quarantine("o3b.policy.corpus_divergence");
            }

            O1TrustRole[] roles = [O1TrustRole.PolicyApproverA, O1TrustRole.PolicyApproverB];
            if (!package.Approvals.Select(item => item.Role).Order().SequenceEqual(roles.Order()) ||
                package.Approvals.Select(item => item.KeyId).Distinct(StringComparer.Ordinal).Count() != 2 ||
                package.Attestation.Role != O1TrustRole.PolicyAttestation ||
                package.Approvals.Any(item => item.KeyId == package.Attestation.KeyId) ||
                package.PublicKeys.Count != 3)
            {
                return Quarantine("o3b.policy.authority_unproved");
            }

            foreach (O3BSignature signature in package.Approvals.Append(package.Attestation))
            {
                if (!package.PublicKeys.TryGetValue(signature.KeyId, out string? publicKey) ||
                    !signature.KeyId.EndsWith(signature.Role.ToString().ToLowerInvariant(), StringComparison.Ordinal) ||
                    !O1CanonicalCryptography.Verify(policy, signature.Signature, publicKey))
                {
                    return Quarantine("o3b.policy.authority_unproved");
                }
            }
            return Accept("o3b.policy.accepted");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArithmeticException or
            InvalidOperationException or
            JsonException)
        {
            return Reject("o3b.policy.invalid");
        }
    }

    /// <summary>Verifies complete, finite, criteria-consistent and authenticated aggregate result metrics.</summary>
    /// <param name="result">Untrusted evaluation package.</param>
    /// <param name="policy">Verified frozen policy package.</param>
    /// <returns>A bounded accepted, rejected or quarantined code.</returns>
    internal static (O3BEvaluationDisposition Disposition, string Code) VerifyResult(
        O3BEvaluationPackage? result,
        O3BFrozenPolicyPackage policy)
    {
        try
        {
            if (result is null || result.Payload is null || result.Attestation is null)
            {
                return Reject("o3b.result.absent");
            }
            O3BEvaluationPayload payload = result.Payload;
            if (payload.SchemaVersion != 1 ||
                !payload.ProcessingCompleted ||
                payload.ProductionRepresentative ||
                payload.IsAuthorising ||
                payload.PolicyDigest != policy.PolicyDigest ||
                payload.CorpusManifestDigest != policy.Policy.CorpusManifestDigest ||
                payload.CorpusRevision != policy.Policy.CorpusRevision ||
                result.ResultDigest != O1CanonicalCryptography.Digest(payload) ||
                !CompleteSegments(payload.SegmentMetrics.Select(item => item.Segment)) ||
                payload.SegmentMetrics.Any(item => !ValidMetrics(item)))
            {
                return Reject("o3b.result.invalid");
            }
            if (result.Attestation.Role != O1TrustRole.PolicyAttestation ||
                result.Attestation.KeyId != policy.Attestation.KeyId ||
                !policy.PublicKeys.TryGetValue(result.Attestation.KeyId, out string? publicKey) ||
                !O1CanonicalCryptography.Verify(payload, result.Attestation.Signature, publicKey))
            {
                return Quarantine("o3b.result.attestation_unproved");
            }
            bool criteriaPassed = payload.SegmentMetrics.All(item => item.CriteriaPassed);
            if (payload.SyntheticApproved != criteriaPassed ||
                payload.Code != (criteriaPassed ? "o3b.synthetic.approved" : "o3b.synthetic.not_qualified"))
            {
                return Reject("o3b.result.criteria_inconsistent");
            }
            return Accept("o3b.result.accepted");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArithmeticException or
            InvalidOperationException or
            JsonException)
        {
            return Reject("o3b.result.invalid");
        }
    }

    /// <summary>Checks every required segment appears exactly once.</summary>
    private static bool CompleteSegments(IEnumerable<string> segments) =>
        segments.Order(StringComparer.Ordinal).SequenceEqual(RequiredSegments, StringComparer.Ordinal);

    /// <summary>Checks bounded, finite and non-authorising predeclared metric criteria.</summary>
    private static bool ValidCriterion(O3BMetricCriterion item) =>
        item.MinimumCoverage is >= 0 and <= 1 &&
        item.MaximumAbstentionRate is >= 0 and <= 1 &&
        item.MaximumFalsePositives >= 0 &&
        item.MaximumFalseNegatives >= 0 &&
        double.IsFinite(item.MaximumMeanAbsolutePredictionError) &&
        item.MaximumMeanAbsolutePredictionError is >= 0 and <= 1 &&
        item.MinimumStability is >= 0 and <= 1 &&
        item.MinimumExplainability is >= 0 and <= 1 &&
        item.MaximumAccountedDurationUnits > 0 &&
        item.MaximumAccountedMemoryBytes > 0 &&
        item.MaximumWorkUnits > 0;

    /// <summary>Checks all measured values are finite, bounded and internally complete.</summary>
    private static bool ValidMetrics(O3BSegmentMetrics item) =>
        item.CaseCount > 0 &&
        item.EvaluatedCount + item.AbstentionCount == item.CaseCount &&
        item.TruePositiveCount + item.FalsePositiveCount + item.FalseNegativeCount +
            item.TrueNegativeCount + item.AbstentionCount == item.CaseCount &&
        item.Coverage is >= 0 and <= 1 &&
        item.AbstentionRate is >= 0 and <= 1 &&
        double.IsFinite(item.MeanAbsolutePredictionError) &&
        item.MeanAbsolutePredictionError is >= 0 and <= 1 &&
        double.IsFinite(item.Stability) &&
        item.Stability is >= 0 and <= 1 &&
        double.IsFinite(item.Explainability) &&
        item.Explainability is >= 0 and <= 1 &&
        item.AccountedDurationUnits > 0 &&
        item.AccountedMemoryBytes > 0 &&
        item.WorkUnits > 0;

    /// <summary>Computes a canonical digest for one exact non-holdout partition.</summary>
    private static string PartitionDigest(
        IReadOnlyList<(O3ACorpusPartition Partition, O3ACorpusContent Content)> material,
        O3ACorpusPartition partition) =>
        O1CanonicalCryptography.Digest(
            material
                .Where(item => item.Partition == partition)
                .Select(item => item.Content)
                .OrderBy(item => item.CaseId, StringComparer.Ordinal)
                .ToArray());

    /// <summary>Creates one accepted code.</summary>
    private static (O3BEvaluationDisposition Disposition, string Code) Accept(string code) =>
        (O3BEvaluationDisposition.Accepted, code);

    /// <summary>Creates one bounded rejection.</summary>
    private static (O3BEvaluationDisposition Disposition, string Code) Reject(string code) =>
        (O3BEvaluationDisposition.Rejected, code);

    /// <summary>Creates one bounded quarantine.</summary>
    private static (O3BEvaluationDisposition Disposition, string Code) Quarantine(string code) =>
        (O3BEvaluationDisposition.Quarantined, code);
}

/// <summary>Runs one-use deterministic holdout evaluation with bounded resources and fail-closed continuity.</summary>
internal static class O3BOfflineEvaluator
{
    /// <summary>
    /// Evaluates immutable holdout cases against the exact frozen policy and returns aggregate synthetic metrics.
    /// </summary>
    /// <param name="corpus">Exact O3-A package.</param>
    /// <param name="corpusHead">Accepted O3-A head.</param>
    /// <param name="policy">Frozen authenticated policy.</param>
    /// <param name="authority">Ephemeral result attestation authority.</param>
    /// <param name="gate">One-use partition gate.</param>
    /// <param name="budget">Hard case, work, memory and deadline limits.</param>
    /// <param name="evaluatedAtUtc">Trusted evaluation instant after freeze.</param>
    /// <param name="current">Optional accepted predecessor preventing reuse and rollback.</param>
    /// <param name="cancellationToken">Cancellation checked before and during every case.</param>
    /// <returns>Accepted aggregate result or a bounded fail-closed outcome.</returns>
    internal static O3BEvaluationOutcome Evaluate(
        O3ACorpusPackage corpus,
        O3ACorpusHead corpusHead,
        O3BFrozenPolicyPackage policy,
        O3BSyntheticPolicyAuthority authority,
        O3BPartitionGate gate,
        O3BEvaluationBudget budget,
        DateTimeOffset evaluatedAtUtc,
        O3BEvaluationHead? current = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Reject("o3b.evaluation.cancelled");
            }
            if (evaluatedAtUtc >= budget.DeadlineUtc)
            {
                return Reject("o3b.evaluation.deadline");
            }

            O3ACorpusVerification corpusVerification = O3ACorpusVerifier.Verify(corpus, evaluatedAtUtc);
            if (corpusVerification.Disposition != O3ACorpusDisposition.Accepted ||
                corpusVerification.Code != "o3a.corpus.accepted" ||
                corpusVerification.Head != corpusHead)
            {
                return Reject(
                    corpusVerification.Code == "o3a.corpus.withdrawn"
                        ? "o3b.corpus.withdrawn"
                        : "o3b.corpus.unavailable");
            }

            (O3BEvaluationDisposition policyDisposition, string policyCode) =
                O3BGovernanceVerifier.VerifyPolicy(policy, corpus, corpusHead, evaluatedAtUtc);
            if (policyDisposition != O3BEvaluationDisposition.Accepted)
            {
                return new(policyDisposition, policyCode, null, current);
            }
            if (current is not null)
            {
                if (corpusHead.Revision < current.CorpusRevision)
                {
                    return Quarantine("o3b.continuity.rollback", current);
                }
                if (corpusHead.Revision == current.CorpusRevision &&
                    corpusHead.ManifestDigest == current.CorpusManifestDigest &&
                    policy.PolicyDigest == current.PolicyDigest)
                {
                    return Quarantine("o3b.holdout.reuse", current);
                }
            }

            IReadOnlyList<O3ACorpusContent> holdout;
            try
            {
                holdout = gate.ReadHoldout(policy.Policy, evaluatedAtUtc);
            }
            catch (InvalidOperationException)
            {
                return current is null
                    ? Reject("o3b.holdout.access_invalid")
                    : Quarantine("o3b.holdout.access_invalid", current);
            }

            long requiredWork = holdout.Count * 4L;
            long requiredMemory = holdout.Count * 4_096L;
            if (holdout.Count == 0 ||
                holdout.Count > budget.MaximumCases ||
                requiredWork > budget.MaximumWorkUnits ||
                requiredMemory > budget.MaximumAccountedMemoryBytes)
            {
                return Reject("o3b.evaluation.resource_limit");
            }

            List<O3BSegmentMetrics> metrics = [];
            foreach (IGrouping<string, O3ACorpusContent> segmentGroup in
                     holdout.GroupBy(item => item.Segment, StringComparer.Ordinal).OrderBy(item => item.Key))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (evaluatedAtUtc >= budget.DeadlineUtc)
                {
                    return Reject("o3b.evaluation.deadline");
                }
                O3BFrozenRule rule = policy.Policy.Rules.Single(item => item.Segment == segmentGroup.Key);
                O3BMetricCriterion criterion = policy.Policy.Criteria.Single(
                    item => item.Segment == segmentGroup.Key);
                metrics.Add(EvaluateSegment(segmentGroup.Key, segmentGroup.ToArray(), rule, criterion, evaluatedAtUtc));
            }

            if (metrics.Count != policy.Policy.Criteria.Count)
            {
                return Reject("o3b.evaluation.segment_missing");
            }
            bool approved = metrics.All(item => item.CriteriaPassed);
            O3BEvaluationPayload payload = new(
                1,
                O1CanonicalCryptography.TextDigest(
                    $"{corpusHead.ManifestDigest}:{policy.PolicyDigest}:o3b-holdout-v1"),
                corpusHead.ManifestDigest,
                corpusHead.Revision,
                policy.PolicyDigest,
                evaluatedAtUtc,
                true,
                approved,
                false,
                false,
                approved ? "o3b.synthetic.approved" : "o3b.synthetic.not_qualified",
                Array.AsReadOnly(metrics.OrderBy(item => item.Segment, StringComparer.Ordinal).ToArray()));
            O3BEvaluationPackage result = authority.Attest(payload);
            (O3BEvaluationDisposition resultDisposition, string resultCode) =
                O3BGovernanceVerifier.VerifyResult(result, policy);
            if (resultDisposition != O3BEvaluationDisposition.Accepted)
            {
                return new(resultDisposition, resultCode, null, current);
            }

            O3BEvaluationHead head = new(
                corpusHead.Revision,
                corpusHead.ManifestDigest,
                policy.PolicyDigest,
                result.ResultDigest);
            return new(O3BEvaluationDisposition.Accepted, payload.Code, result, head);
        }
        catch (OperationCanceledException)
        {
            return Reject("o3b.evaluation.cancelled");
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArithmeticException or
            InvalidOperationException or
            JsonException)
        {
            return Reject("o3b.evaluation.failed_closed");
        }
    }

    /// <summary>Runs each case twice through the existing deterministic analyser and computes complete segment metrics.</summary>
    private static O3BSegmentMetrics EvaluateSegment(
        string segment,
        O3ACorpusContent[] cases,
        O3BFrozenRule frozenRule,
        O3BMetricCriterion criterion,
        DateTimeOffset evaluatedAtUtc)
    {
        int evaluated = 0;
        int abstained = 0;
        int truePositive = 0;
        int falsePositive = 0;
        int falseNegative = 0;
        int trueNegative = 0;
        int stable = 0;
        int explainable = 0;
        double absoluteError = 0;

        foreach (O3ACorpusContent evaluationCase in cases)
        {
            ObserverThresholdResult first = Analyse(evaluationCase, frozenRule, evaluatedAtUtc);
            ObserverThresholdResult second = Analyse(evaluationCase, frozenRule, evaluatedAtUtc);
            bool expectedPositive = evaluationCase.Outcome == "critical";
            if (first.Disposition == ObserverFindingDisposition.InsufficientEvidence)
            {
                abstained++;
                absoluteError++;
            }
            else
            {
                evaluated++;
                bool actualPositive = first.Disposition == ObserverFindingDisposition.Detected;
                absoluteError += actualPositive == expectedPositive ? 0 : 1;
                if (expectedPositive && actualPositive)
                {
                    truePositive++;
                }
                else if (!expectedPositive && actualPositive)
                {
                    falsePositive++;
                }
                else if (expectedPositive)
                {
                    falseNegative++;
                }
                else
                {
                    trueNegative++;
                }
            }

            if (first.Disposition == second.Disposition &&
                first.Code == second.Code &&
                first.ObservedValue == second.ObservedValue)
            {
                stable++;
            }
            if (!string.IsNullOrWhiteSpace(first.Code) &&
                first.EvidenceIds.Count == 1 &&
                first.MetricKey == $"observer.synthetic.{segment}")
            {
                explainable++;
            }
        }

        double coverage = (double)evaluated / cases.Length;
        double abstentionRate = (double)abstained / cases.Length;
        double meanAbsoluteError = absoluteError / cases.Length;
        double stability = (double)stable / cases.Length;
        double explainability = (double)explainable / cases.Length;
        long duration = cases.Length;
        long memory = cases.Length * 4_096L;
        long work = cases.Length * 4L;
        bool passed =
            coverage >= criterion.MinimumCoverage &&
            abstentionRate <= criterion.MaximumAbstentionRate &&
            falsePositive <= criterion.MaximumFalsePositives &&
            falseNegative <= criterion.MaximumFalseNegatives &&
            meanAbsoluteError <= criterion.MaximumMeanAbsolutePredictionError &&
            stability >= criterion.MinimumStability &&
            explainability >= criterion.MinimumExplainability &&
            duration <= criterion.MaximumAccountedDurationUnits &&
            memory <= criterion.MaximumAccountedMemoryBytes &&
            work <= criterion.MaximumWorkUnits;
        return new(
            segment,
            cases.Length,
            evaluated,
            abstained,
            truePositive,
            falsePositive,
            falseNegative,
            trueNegative,
            coverage,
            abstentionRate,
            meanAbsoluteError,
            stability,
            explainability,
            duration,
            memory,
            work,
            passed);
    }

    /// <summary>Maps one synthetic content item to canonical evidence and invokes the existing MOD-12 threshold analyser.</summary>
    private static ObserverThresholdResult Analyse(
        O3ACorpusContent evaluationCase,
        O3BFrozenRule frozenRule,
        DateTimeOffset evaluatedAtUtc)
    {
        Guid instanceId = StableGuid($"instance:{evaluationCase.CaseId}");
        Guid scopeId = StableGuid("o3b-synthetic-scope");
        ObserverMetricSample sample = new(
            StableGuid($"evidence:{evaluationCase.EvidenceFingerprint}"),
            instanceId,
            scopeId,
            $"observer.synthetic.{evaluationCase.Segment}",
            "ratio",
            evaluationCase.NormalisedValue,
            evaluatedAtUtc.AddSeconds(-1),
            evaluatedAtUtc,
            evaluationCase.QualityClass == "complete"
                ? ObserverEvidenceQuality.Verified
                : ObserverEvidenceQuality.Degraded,
            ObserverMissingness.Present,
            ObserverMetricCardinality.SingleSeries,
            "o3b-synthetic-corpus",
            "1.0.0",
            evaluationCase.Transformation,
            ObserverDataClassification.OperationalTelemetry,
            ObserverRedactionStatus.Sanitised,
            ObserverRetentionClass.EphemeralAnalysisOnly,
            ObserverPermittedPurpose.NonMutatingObserverAnalysis);
        ObserverAnalysisRequest request = new(
            StableGuid($"analysis:{evaluationCase.CaseId}"),
            instanceId,
            scopeId,
            evaluatedAtUtc,
            new[] { sample });
        ObserverThresholdRule rule = new(
            $"o3b-{evaluationCase.Segment}-threshold",
            "1.0.0",
            $"observer.synthetic.{evaluationCase.Segment}",
            "ratio",
            frozenRule.Comparison,
            frozenRule.Threshold,
            ObserverFindingSeverity.Critical,
            1,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
        return DeterministicThresholdAnalyser.Analyse(request, rule);
    }

    /// <summary>Derives a stable version-four identifier from synthetic non-secret content.</summary>
    private static Guid StableGuid(string value)
    {
        byte[] bytes = Convert.FromHexString(O1CanonicalCryptography.TextDigest(value)[..32]);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    /// <summary>Creates one bounded rejection without a partial report.</summary>
    private static O3BEvaluationOutcome Reject(string code) =>
        new(O3BEvaluationDisposition.Rejected, code, null, null);

    /// <summary>Creates one bounded continuity quarantine preserving the prior accepted head.</summary>
    private static O3BEvaluationOutcome Quarantine(string code, O3BEvaluationHead current) =>
        new(O3BEvaluationDisposition.Quarantined, code, null, current);
}

/// <summary>Exposes aggregate-only O3-B proof through the existing exact test host marker.</summary>
public static class O3BSandboxProcess
{
    /// <summary>Runs one synthetic policy freeze and holdout evaluation after exact opt-in activation.</summary>
    /// <param name="args">Exact marker and version-four run identifier.</param>
    /// <returns>Zero after synthetic approval, two for invalid activation, or three for fail-closed refusal.</returns>
    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length != 4 ||
            args[0] != "--activation" ||
            args[1] != O3BGovernanceVerifier.ActivationMarker ||
            args[2] != "--run-id" ||
            !Guid.TryParseExact(args[3], "D", out Guid runId) ||
            args[3][14] != '4' ||
            !"89abAB".Contains(args[3][19]))
        {
            Console.Error.WriteLine("o3b_sandbox.failed:activation_invalid");
            return Task.FromResult(2);
        }

        using O3ASyntheticCorpusAuthority corpusAuthority = new();
        using O3BSyntheticPolicyAuthority policyAuthority = new();
        O3ACorpusPackage corpus = corpusAuthority.Build();
        O3ACorpusVerification corpusVerification = O3ACorpusVerifier.Verify(
            corpus,
            O3ASyntheticCorpusAuthority.NowUtc);
        if (corpusVerification.Head is not O3ACorpusHead corpusHead)
        {
            Console.Error.WriteLine("o3b_sandbox.failed:corpus_unavailable");
            return Task.FromResult(3);
        }

        O3BPartitionGate gate = new(corpus);
        DateTimeOffset frozenAt = O3ASyntheticCorpusAuthority.NowUtc.AddMinutes(1);
        O3BFrozenPolicyPackage policy = policyAuthority.Freeze(corpus, corpusHead, gate, frozenAt);
        DateTimeOffset evaluatedAt = frozenAt.AddMinutes(1);
        O3BEvaluationOutcome outcome = O3BOfflineEvaluator.Evaluate(
            corpus,
            corpusHead,
            policy,
            policyAuthority,
            gate,
            new(9, 36, 36_864, evaluatedAt.AddMinutes(1)),
            evaluatedAt);
        if (outcome.Disposition != O3BEvaluationDisposition.Accepted || outcome.Package is null)
        {
            Console.Error.WriteLine($"o3b_sandbox.failed:{outcome.Code}");
            return Task.FromResult(3);
        }

        Console.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    code = outcome.Code,
                    run = O1CanonicalCryptography.TextDigest(runId.ToString("D"))[..16],
                    segments = outcome.Package.Payload.SegmentMetrics.Count,
                    syntheticApproved = outcome.Package.Payload.SyntheticApproved,
                    productionRepresentative = false,
                    authorising = false,
                    activationState = ObserverActivationState.None.ToString(),
                }));
        return Task.FromResult(0);
    }
}
