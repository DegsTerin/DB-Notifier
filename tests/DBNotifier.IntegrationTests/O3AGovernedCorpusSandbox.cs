// Module purpose: Defines and verifies a governed, content-addressed synthetic corpus inside the opt-in O3-A sandbox only.
using System.Collections.ObjectModel;
using System.Text.Json;
using DBNotifier.Application.AIOps;

namespace DBNotifier.IntegrationTests;

/// <summary>Names the immutable partitions required by the O3-A synthetic corpus contract.</summary>
internal enum O3ACorpusPartition
{
    Development,
    Calibration,
    Holdout,
}

/// <summary>Classifies the result of fail-closed O3-A corpus admission.</summary>
internal enum O3ACorpusDisposition
{
    Accepted,
    Rejected,
    Quarantined,
}

/// <summary>Describes one bounded provider-neutral synthetic observation used only for corpus governance tests.</summary>
internal sealed record O3ACorpusContent(
    string CaseId,
    string ScenarioId,
    string EvidenceFingerprint,
    string SourceGroupId,
    string Segment,
    string Outcome,
    string LoadClass,
    string QualityClass,
    string Transformation,
    double NormalisedValue,
    bool IsMissing,
    bool IsAdversarial);

/// <summary>Binds one immutable corpus member to its partition and content digest.</summary>
internal sealed record O3ACorpusMember(
    string CaseId,
    O3ACorpusPartition Partition,
    string Segment,
    string ContentDigest);

/// <summary>Declares quantitative acceptance criteria before a segment is admitted.</summary>
internal sealed record O3ASegmentCriterion(
    string Segment,
    int ExpectedDevelopment,
    int ExpectedCalibration,
    int ExpectedHoldout,
    int MaximumMissing,
    IReadOnlyList<string> RequiredOutcomes,
    IReadOnlyList<string> RequiredLoadClasses,
    IReadOnlyList<string> RequiredQualityClasses);

/// <summary>Records explicit synthetic-only authority, purpose, retention and representativeness limitations.</summary>
internal sealed record O3ACorpusGovernance(
    string Authority,
    string Licence,
    string Purpose,
    string Classification,
    string Origin,
    string Retention,
    string WithdrawalPolicy,
    string ConsentBasis,
    string RedactionPolicy,
    bool ProductionRepresentative,
    IReadOnlyList<string> KnownBiases,
    IReadOnlyList<string> KnownGaps);

/// <summary>Represents the exact canonical manifest payload protected by independent role signatures.</summary>
internal sealed record O3ACorpusManifestPayload(
    int SchemaVersion,
    string DatasetId,
    long Revision,
    string PreviousManifestDigest,
    string TenantId,
    string Environment,
    string Purpose,
    DateTimeOffset NotBeforeUtc,
    DateTimeOffset ExpiresUtc,
    bool Withdrawn,
    string MembershipDigest,
    O3ACorpusGovernance Governance,
    IReadOnlyList<O3ACorpusMember> Members,
    IReadOnlyList<O3ASegmentCriterion> Criteria);

/// <summary>Records one exact role signature over a canonical O3-A manifest payload.</summary>
internal sealed record O3ACorpusSignature(
    O1TrustRole Role,
    string KeyId,
    string Signature);

/// <summary>Combines an authenticated manifest, exact content and public synthetic verification material.</summary>
internal sealed record O3ACorpusPackage(
    O3ACorpusManifestPayload Manifest,
    IReadOnlyList<O3ACorpusContent> Contents,
    IReadOnlyList<O3ACorpusSignature> Approvals,
    O3ACorpusSignature Attestation,
    IReadOnlyDictionary<string, string> PublicKeys);

/// <summary>Tracks only the last accepted synthetic corpus head and irreversible withdrawal state.</summary>
internal sealed record O3ACorpusHead(
    string DatasetId,
    long Revision,
    string ManifestDigest,
    string MembershipDigest,
    string CriteriaDigest,
    bool Withdrawn);

/// <summary>Returns bounded codes and aggregate counts without exposing corpus content.</summary>
internal sealed record O3ACorpusVerification(
    O3ACorpusDisposition Disposition,
    string Code,
    int CaseCount,
    int PartitionCount,
    bool ProductionRepresentative,
    O3ACorpusHead? Head);

/// <summary>Verifies authority, exact membership, partition integrity, quality criteria and continuity fail closed.</summary>
internal static class O3ACorpusVerifier
{
    internal const string ActivationMarker = "o3a-governed-corpus-sandbox";
    private const int MaximumCases = 128;
    private const int MaximumTextLength = 128;
    private static readonly string[] RequiredSegments = ["capacity", "availability", "latency"];
    private static readonly string[] AllowedOutcomes = ["healthy", "warning", "critical"];
    private static readonly string[] AllowedLoads = ["low", "nominal", "high"];
    private static readonly string[] AllowedQualities = ["complete", "noisy", "adversarial"];

    /// <summary>
    /// Verifies one candidate against public synthetic trust and an optional accepted predecessor.
    /// </summary>
    /// <param name="package">Untrusted synthetic corpus package.</param>
    /// <param name="nowUtc">Evaluation instant.</param>
    /// <param name="current">Optional accepted predecessor.</param>
    /// <returns>A bounded accepted, rejected or quarantined result.</returns>
    internal static O3ACorpusVerification Verify(
        O3ACorpusPackage? package,
        DateTimeOffset nowUtc,
        O3ACorpusHead? current = null)
    {
        try
        {
            if (package is null)
            {
                return Reject("o3a.corpus.absent");
            }
            if (package.Manifest is null ||
                package.Contents is null ||
                package.Approvals is null ||
                package.Attestation is null ||
                package.PublicKeys is null)
            {
                return Reject("o3a.corpus.malformed");
            }

            O3ACorpusManifestPayload manifest = package.Manifest;
            if (!ValidManifestShape(manifest, package))
            {
                return Reject("o3a.manifest.invalid");
            }

            string manifestDigest = O1CanonicalCryptography.Digest(manifest);
            if (!VerifyAuthority(package, manifest))
            {
                return Quarantine("o3a.authority.unproved");
            }

            O3ACorpusVerification? continuity = VerifyContinuity(manifest, manifestDigest, current);
            if (continuity is not null)
            {
                return continuity;
            }

            if (!VerifyExactMembership(manifest, package.Contents))
            {
                return Quarantine("o3a.membership.invalid");
            }

            if (!VerifyPartitionsAndLeakage(manifest, package.Contents))
            {
                return Reject("o3a.partition.invalid");
            }

            if (!VerifyQuality(manifest, package.Contents))
            {
                return Reject("o3a.quality.unproved");
            }

            if (nowUtc < manifest.NotBeforeUtc || nowUtc >= manifest.ExpiresUtc)
            {
                return Reject("o3a.scope.expired");
            }

            O3ACorpusHead head = new(
                manifest.DatasetId,
                manifest.Revision,
                manifestDigest,
                manifest.MembershipDigest,
                CriteriaDigest(manifest.Criteria),
                manifest.Withdrawn);
            return new O3ACorpusVerification(
                O3ACorpusDisposition.Accepted,
                manifest.Withdrawn ? "o3a.corpus.withdrawn" : "o3a.corpus.accepted",
                package.Contents.Count,
                Enum.GetValues<O3ACorpusPartition>().Length,
                false,
                head);
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            ArithmeticException or
            InvalidOperationException or
            JsonException)
        {
            return Reject("o3a.corpus.malformed");
        }
    }

    /// <summary>Checks bounded canonical governance and collection shape before cryptographic work.</summary>
    private static bool ValidManifestShape(O3ACorpusManifestPayload manifest, O3ACorpusPackage package)
    {
        O3ACorpusGovernance governance = manifest.Governance;
        return governance is not null &&
            manifest.Members is not null &&
            manifest.Criteria is not null &&
            governance.KnownBiases is not null &&
            governance.KnownGaps is not null &&
            manifest.SchemaVersion == 1 &&
            manifest.Revision > 0 &&
            manifest.ExpiresUtc > manifest.NotBeforeUtc &&
            ValidText(manifest.DatasetId) &&
            ValidText(manifest.TenantId) &&
            manifest.Environment == "test-only" &&
            manifest.Purpose == "observer-evaluation-only" &&
            ValidDigestOrEmpty(manifest.PreviousManifestDigest) &&
            ValidDigest(manifest.MembershipDigest) &&
            manifest.Members.Count is > 0 and <= MaximumCases &&
            package.Contents.Count is > 0 and <= MaximumCases &&
            manifest.Criteria.Count == RequiredSegments.Length &&
            package.Approvals.Count == 2 &&
            package.PublicKeys.Count == 3 &&
            governance.Authority == "db-notifier-project-synthetic-fixture-owner" &&
            governance.Licence == "project-owned-synthetic-test-data" &&
            governance.Purpose == manifest.Purpose &&
            governance.Classification == "synthetic-non-personal-non-secret" &&
            governance.Origin == "deterministic-local-fixture" &&
            governance.Retention == "ephemeral-test-run-only" &&
            governance.WithdrawalPolicy == "irreversible-within-dataset-series" &&
            governance.ConsentBasis == "not-applicable-synthetic-data" &&
            governance.RedactionPolicy == "no-raw-operational-content-permitted" &&
            !governance.ProductionRepresentative &&
            governance.KnownBiases.Count > 0 &&
            governance.KnownGaps.Count > 0 &&
            governance.KnownBiases.All(ValidText) &&
            governance.KnownGaps.All(ValidText);
    }

    /// <summary>Requires distinct owner, governance and attestation keys with exact role-bound signatures.</summary>
    private static bool VerifyAuthority(
        O3ACorpusPackage package,
        O3ACorpusManifestPayload manifest)
    {
        O1TrustRole[] approvalRoles =
        [
            O1TrustRole.DataOwnerApprover,
            O1TrustRole.DataGovernanceApprover,
        ];
        if (package.Approvals.Select(item => item.Role).Order().SequenceEqual(approvalRoles.Order()) is false ||
            package.Approvals.Select(item => item.KeyId).Distinct(StringComparer.Ordinal).Count() != 2 ||
            package.Attestation.Role != O1TrustRole.CorpusAttestation ||
            package.Approvals.Any(item => item.KeyId == package.Attestation.KeyId))
        {
            return false;
        }

        foreach (O3ACorpusSignature signature in package.Approvals.Append(package.Attestation))
        {
            if (!ValidText(signature.KeyId) ||
                string.IsNullOrWhiteSpace(signature.Signature) ||
                !package.PublicKeys.TryGetValue(signature.KeyId, out string? publicKey) ||
                !signature.KeyId.EndsWith(signature.Role.ToString().ToLowerInvariant(), StringComparison.Ordinal) ||
                !O1CanonicalCryptography.Verify(manifest, signature.Signature, publicKey))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Protects the monotonic manifest chain and irreversible withdrawal state.</summary>
    private static O3ACorpusVerification? VerifyContinuity(
        O3ACorpusManifestPayload manifest,
        string manifestDigest,
        O3ACorpusHead? current)
    {
        if (current is null)
        {
            return manifest.Revision == 1 && manifest.PreviousManifestDigest.Length == 0
                ? null
                : Quarantine("o3a.continuity.bootstrap_invalid");
        }
        if (manifest.DatasetId != current.DatasetId)
        {
            return Quarantine("o3a.continuity.dataset_substitution");
        }
        if (manifest.Revision == current.Revision && manifestDigest == current.ManifestDigest)
        {
            return null;
        }
        if (manifest.Revision <= current.Revision)
        {
            return Quarantine("o3a.continuity.rollback");
        }
        if (manifest.Revision != current.Revision + 1)
        {
            return Quarantine("o3a.continuity.gap");
        }
        if (manifest.PreviousManifestDigest != current.ManifestDigest)
        {
            return Quarantine("o3a.continuity.divergence");
        }
        if (manifest.MembershipDigest != current.MembershipDigest)
        {
            return Quarantine("o3a.continuity.membership_changed");
        }
        if (CriteriaDigest(manifest.Criteria) != current.CriteriaDigest)
        {
            return Quarantine("o3a.continuity.criteria_changed");
        }
        if (current.Withdrawn && !manifest.Withdrawn)
        {
            return Quarantine("o3a.withdrawal.irreversible");
        }
        return null;
    }

    /// <summary>Compares every descriptor with its exact content digest and the aggregate membership digest.</summary>
    private static bool VerifyExactMembership(
        O3ACorpusManifestPayload manifest,
        IReadOnlyList<O3ACorpusContent> contents)
    {
        if (manifest.Members.Count != contents.Count)
        {
            return false;
        }
        Dictionary<string, O3ACorpusContent> byId;
        try
        {
            byId = contents.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }

        foreach (O3ACorpusMember member in manifest.Members)
        {
            if (!byId.TryGetValue(member.CaseId, out O3ACorpusContent? content) ||
                member.Segment != content.Segment ||
                member.ContentDigest != O1CanonicalCryptography.Digest(content))
            {
                return false;
            }
        }
        return manifest.MembershipDigest == MembershipDigest(manifest.Members);
    }

    /// <summary>Proves all three partitions are populated, immutable and free from duplicate or leaked identities.</summary>
    private static bool VerifyPartitionsAndLeakage(
        O3ACorpusManifestPayload manifest,
        IReadOnlyList<O3ACorpusContent> contents)
    {
        return Enum.GetValues<O3ACorpusPartition>()
                .All(partition => manifest.Members.Any(item => item.Partition == partition)) &&
            manifest.Members.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count() ==
                manifest.Members.Count &&
            manifest.Members.Select(item => item.ContentDigest).Distinct(StringComparer.Ordinal).Count() ==
                manifest.Members.Count &&
            contents.Select(item => item.EvidenceFingerprint).Distinct(StringComparer.Ordinal).Count() ==
                contents.Count &&
            contents.Select(item => item.SourceGroupId).Distinct(StringComparer.Ordinal).Count() ==
                contents.Count;
    }

    /// <summary>Enforces declared segment counts, missingness limits, allowed values and conflict/poisoning checks.</summary>
    private static bool VerifyQuality(
        O3ACorpusManifestPayload manifest,
        IReadOnlyList<O3ACorpusContent> contents)
    {
        if (!manifest.Criteria.Select(item => item.Segment)
                .Order(StringComparer.Ordinal)
                .SequenceEqual(RequiredSegments.Order(StringComparer.Ordinal), StringComparer.Ordinal) ||
            contents.Any(
                item =>
                    !ValidText(item.CaseId) ||
                    !ValidText(item.ScenarioId) ||
                    !ValidText(item.EvidenceFingerprint) ||
                    !ValidText(item.SourceGroupId) ||
                    !RequiredSegments.Contains(item.Segment, StringComparer.Ordinal) ||
                    !AllowedOutcomes.Contains(item.Outcome, StringComparer.Ordinal) ||
                    !AllowedLoads.Contains(item.LoadClass, StringComparer.Ordinal) ||
                    !AllowedQualities.Contains(item.QualityClass, StringComparer.Ordinal) ||
                    item.Transformation != "bounded-normalisation-v1" ||
                    !double.IsFinite(item.NormalisedValue) ||
                    item.NormalisedValue is < 0 or > 1 ||
                    item.IsAdversarial != (item.QualityClass == "adversarial")))
        {
            return false;
        }

        bool conflict = contents
            .GroupBy(item => item.ScenarioId, StringComparer.Ordinal)
            .Any(group => group.Select(item => item.Outcome).Distinct(StringComparer.Ordinal).Count() > 1);
        if (conflict)
        {
            return false;
        }

        foreach (O3ASegmentCriterion criterion in manifest.Criteria)
        {
            if (criterion.ExpectedDevelopment <= 0 ||
                criterion.ExpectedCalibration <= 0 ||
                criterion.ExpectedHoldout <= 0 ||
                criterion.MaximumMissing < 0 ||
                criterion.RequiredOutcomes is null ||
                criterion.RequiredLoadClasses is null ||
                criterion.RequiredQualityClasses is null ||
                !SetEquals(criterion.RequiredOutcomes, AllowedOutcomes) ||
                !SetEquals(criterion.RequiredLoadClasses, AllowedLoads) ||
                !SetEquals(criterion.RequiredQualityClasses, AllowedQualities))
            {
                return false;
            }
            IReadOnlyList<O3ACorpusContent> segmentContents = contents
                .Where(item => item.Segment == criterion.Segment)
                .ToArray();
            if (segmentContents.Count(item => item.IsMissing) > criterion.MaximumMissing ||
                !SetEquals(
                    segmentContents.Select(item => item.Outcome).ToArray(),
                    criterion.RequiredOutcomes.ToArray()) ||
                !SetEquals(
                    segmentContents.Select(item => item.LoadClass).ToArray(),
                    criterion.RequiredLoadClasses.ToArray()) ||
                !SetEquals(
                    segmentContents.Select(item => item.QualityClass).ToArray(),
                    criterion.RequiredQualityClasses.ToArray()) ||
                Count(manifest, criterion.Segment, O3ACorpusPartition.Development) !=
                    criterion.ExpectedDevelopment ||
                Count(manifest, criterion.Segment, O3ACorpusPartition.Calibration) !=
                    criterion.ExpectedCalibration ||
                Count(manifest, criterion.Segment, O3ACorpusPartition.Holdout) !=
                    criterion.ExpectedHoldout)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Counts exact segment membership in one partition.</summary>
    private static int Count(
        O3ACorpusManifestPayload manifest,
        string segment,
        O3ACorpusPartition partition) =>
        manifest.Members.Count(item => item.Segment == segment && item.Partition == partition);

    /// <summary>Compares bounded declared value sets independently of input order.</summary>
    private static bool SetEquals(IReadOnlyList<string> actual, string[] expected) =>
        actual.Count == expected.Length &&
        actual.Distinct(StringComparer.Ordinal).Count() == expected.Length &&
        actual.All(item => expected.Contains(item, StringComparer.Ordinal));

    /// <summary>Computes exact sorted membership without including mutable content bodies.</summary>
    internal static string MembershipDigest(IReadOnlyList<O3ACorpusMember> members) =>
        O1CanonicalCryptography.Digest(
            members
                .OrderBy(item => item.CaseId, StringComparer.Ordinal)
                .ThenBy(item => item.Partition)
                .ToArray());

    /// <summary>Computes one stable digest over predeclared segment criteria in canonical order.</summary>
    internal static string CriteriaDigest(IReadOnlyList<O3ASegmentCriterion> criteria) =>
        O1CanonicalCryptography.Digest(
            criteria.OrderBy(item => item.Segment, StringComparer.Ordinal).ToArray());

    /// <summary>Checks a bounded non-empty canonical text value.</summary>
    private static bool ValidText(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= MaximumTextLength &&
        value == value.Trim() &&
        !value.Any(char.IsControl);

    /// <summary>Checks an upper-case SHA-256 hexadecimal digest.</summary>
    private static bool ValidDigest(string value) =>
        value.Length == 64 && value.All(character => char.IsDigit(character) || character is >= 'A' and <= 'F');

    /// <summary>Checks either no predecessor or an exact digest.</summary>
    private static bool ValidDigestOrEmpty(string value) => value.Length == 0 || ValidDigest(value);

    /// <summary>Creates one bounded rejection.</summary>
    private static O3ACorpusVerification Reject(string code) =>
        new(O3ACorpusDisposition.Rejected, code, 0, 0, false, null);

    /// <summary>Creates one bounded quarantine result.</summary>
    private static O3ACorpusVerification Quarantine(string code) =>
        new(O3ACorpusDisposition.Quarantined, code, 0, 0, false, null);
}

/// <summary>Builds deterministic synthetic packages using three distinct ephemeral O1 role keys.</summary>
internal sealed class O3ASyntheticCorpusAuthority : IDisposable
{
    private static readonly DateTimeOffset ValidFrom = new(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidUntil = new(2026, 7, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] SegmentNames = ["capacity", "availability", "latency"];
    private static readonly string[] OutcomeNames = ["healthy", "warning", "critical"];
    private static readonly string[] LoadNames = ["low", "nominal", "high"];
    private static readonly string[] QualityNames = ["complete", "noisy", "adversarial"];
    private static readonly string[] BiasDeclarations =
        ["synthetic distributions do not model production prevalence"];
    private static readonly string[] GapDeclarations =
        ["no production topology, provider or workload evidence"];
    private readonly O1SyntheticKeyRing keys = new("o3a");

    /// <summary>Gets an instant inside the synthetic authority window.</summary>
    internal static DateTimeOffset NowUtc => ValidFrom.AddHours(1);

    /// <summary>Builds revision one with three segments represented in each immutable partition.</summary>
    /// <returns>A fully authenticated synthetic-only package.</returns>
    internal O3ACorpusPackage Build() => BuildRevision(1, string.Empty, false);

    /// <summary>Builds a direct authenticated successor of an accepted head.</summary>
    /// <param name="head">Accepted predecessor.</param>
    /// <param name="withdrawn">Whether the successor irreversibly withdraws the dataset.</param>
    /// <returns>A fully authenticated successor.</returns>
    internal O3ACorpusPackage Successor(O3ACorpusHead head, bool withdrawn = false) =>
        BuildRevision(head.Revision + 1, head.ManifestDigest, withdrawn);

    /// <summary>Re-signs a deliberately changed manifest or content set for adversarial tests.</summary>
    /// <param name="manifest">Manifest payload to authenticate.</param>
    /// <param name="contents">Exact content to package.</param>
    /// <returns>A package with valid signatures over the supplied manifest.</returns>
    internal O3ACorpusPackage Sign(
        O3ACorpusManifestPayload manifest,
        IReadOnlyList<O3ACorpusContent> contents)
    {
        O3ACorpusSignature Owner(O1TrustRole role) =>
            new(role, keys.KeyId(role), keys.Sign(role, manifest));
        O3ACorpusSignature[] approvals =
        [
            Owner(O1TrustRole.DataOwnerApprover),
            Owner(O1TrustRole.DataGovernanceApprover),
        ];
        O3ACorpusSignature attestation = Owner(O1TrustRole.CorpusAttestation);
        Dictionary<string, string> publicKeys = approvals
            .Append(attestation)
            .ToDictionary(item => item.KeyId, item => keys.PublicKey(item.Role), StringComparer.Ordinal);
        return new O3ACorpusPackage(
            manifest,
            Array.AsReadOnly(contents.ToArray()),
            Array.AsReadOnly(approvals),
            attestation,
            new ReadOnlyDictionary<string, string>(publicKeys));
    }

    /// <summary>Disposes all ephemeral signing keys without persistence.</summary>
    public void Dispose() => keys.Dispose();

    /// <summary>Creates one complete immutable revision and authenticates its exact manifest.</summary>
    private O3ACorpusPackage BuildRevision(long revision, string previousDigest, bool withdrawn)
    {
        O3ACorpusContent[] contents = BuildContents();
        O3ACorpusMember[] members = contents
            .Select(
                (content, index) => new O3ACorpusMember(
                    content.CaseId,
                    (O3ACorpusPartition)(index / 3),
                    content.Segment,
                    O1CanonicalCryptography.Digest(content)))
            .OrderBy(item => item.CaseId, StringComparer.Ordinal)
            .ToArray();
        O3ASegmentCriterion[] criteria = SegmentNames
            .Select(
                segment => new O3ASegmentCriterion(
                    segment,
                    1,
                    1,
                    1,
                    0,
                    Array.AsReadOnly(OutcomeNames),
                    Array.AsReadOnly(LoadNames),
                    Array.AsReadOnly(QualityNames)))
            .ToArray();
        O3ACorpusGovernance governance = new(
            "db-notifier-project-synthetic-fixture-owner",
            "project-owned-synthetic-test-data",
            "observer-evaluation-only",
            "synthetic-non-personal-non-secret",
            "deterministic-local-fixture",
            "ephemeral-test-run-only",
            "irreversible-within-dataset-series",
            "not-applicable-synthetic-data",
            "no-raw-operational-content-permitted",
            false,
            Array.AsReadOnly(BiasDeclarations),
            Array.AsReadOnly(GapDeclarations));
        O3ACorpusManifestPayload manifest = new(
            1,
            "dataset-o3a-synthetic-observer",
            revision,
            previousDigest,
            "tenant-synthetic",
            "test-only",
            "observer-evaluation-only",
            ValidFrom,
            ValidUntil,
            withdrawn,
            O3ACorpusVerifier.MembershipDigest(members),
            governance,
            Array.AsReadOnly(members),
            Array.AsReadOnly(criteria));
        return Sign(manifest, contents);
    }

    /// <summary>Creates nine deterministic provider-neutral cases with disjoint leakage groups.</summary>
    private static O3ACorpusContent[] BuildContents()
    {
        List<O3ACorpusContent> contents = [];
        for (int partition = 0; partition < 3; partition++)
        {
            for (int segment = 0; segment < SegmentNames.Length; segment++)
            {
                int ordinal = partition * 3 + segment;
                contents.Add(
                    new O3ACorpusContent(
                        $"case-{partition}-{segment}",
                        $"scenario-{ordinal}",
                        O1CanonicalCryptography.TextDigest($"evidence-{ordinal}"),
                        O1CanonicalCryptography.TextDigest($"source-group-{ordinal}"),
                        SegmentNames[segment],
                        OutcomeNames[(partition + segment) % OutcomeNames.Length],
                        LoadNames[(partition + segment) % LoadNames.Length],
                        QualityNames[(partition + segment) % QualityNames.Length],
                        "bounded-normalisation-v1",
                        (ordinal + 1) / 10d,
                        false,
                        QualityNames[(partition + segment) % QualityNames.Length] == "adversarial"));
            }
        }
        return contents.ToArray();
    }
}

/// <summary>Exposes one exact, aggregate-only O3-A process proof through the existing test host.</summary>
public static class O3ASandboxProcess
{
    /// <summary>
    /// Validates the exact marker and emits only bounded aggregate synthetic-corpus evidence.
    /// </summary>
    /// <param name="args">Exact activation marker and version-four run identifier.</param>
    /// <returns>Zero for accepted synthetic evidence, two for invalid activation, or three for refusal.</returns>
    public static Task<int> RunAsync(string[] args)
    {
        if (args.Length != 4 ||
            args[0] != "--activation" ||
            args[1] != O3ACorpusVerifier.ActivationMarker ||
            args[2] != "--run-id" ||
            !Guid.TryParseExact(args[3], "D", out Guid runId) ||
            args[3][14] != '4' ||
            !"89abAB".Contains(args[3][19]))
        {
            Console.Error.WriteLine("o3a_sandbox.failed:activation_invalid");
            return Task.FromResult(2);
        }

        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusVerification verification = O3ACorpusVerifier.Verify(
            authority.Build(),
            O3ASyntheticCorpusAuthority.NowUtc);
        if (verification.Disposition != O3ACorpusDisposition.Accepted)
        {
            Console.Error.WriteLine($"o3a_sandbox.failed:{verification.Code}");
            return Task.FromResult(3);
        }

        Console.WriteLine(
            JsonSerializer.Serialize(
                new
                {
                    code = verification.Code,
                    run = O1CanonicalCryptography.TextDigest(runId.ToString("D"))[..16],
                    cases = verification.CaseCount,
                    partitions = verification.PartitionCount,
                    productionRepresentative = false,
                    activationState = ObserverActivationState.None.ToString(),
                }));
        return Task.FromResult(0);
    }
}
