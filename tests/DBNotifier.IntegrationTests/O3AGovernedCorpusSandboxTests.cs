// Module purpose: Proves the O3-A governed synthetic corpus admits only authenticated, exact and quality-qualified packages.
using System.Collections.ObjectModel;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises accepted and adversarial O3-A corpus governance paths without operational data.</summary>
public sealed class O3AGovernedCorpusSandboxTests
{
    /// <summary>Proves the valid fixture has exact authority, membership, partitions and synthetic-only truth.</summary>
    [Fact]
    public void ValidCorpusIsAcceptedWithoutProductionRepresentativeness()
    {
        using O3ASyntheticCorpusAuthority authority = new();

        O3ACorpusVerification result = O3ACorpusVerifier.Verify(
            authority.Build(),
            O3ASyntheticCorpusAuthority.NowUtc);

        Assert.Equal(O3ACorpusDisposition.Accepted, result.Disposition);
        Assert.Equal("o3a.corpus.accepted", result.Code);
        Assert.Equal(9, result.CaseCount);
        Assert.Equal(3, result.PartitionCount);
        Assert.False(result.ProductionRepresentative);
        Assert.NotNull(result.Head);
    }

    /// <summary>Proves content corruption and manifest substitution cannot pass content-addressed membership.</summary>
    [Fact]
    public void CorruptionAndSelfConsistentSubstitutionFailClosed()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();
        O3ACorpusContent changed = original.Contents[0] with { NormalisedValue = 0.99 };
        O3ACorpusPackage unsignedContentChange = original with
        {
            Contents = Replace(original.Contents, 0, changed),
        };
        O3ACorpusVerification corruption = O3ACorpusVerifier.Verify(
            unsignedContentChange,
            O3ASyntheticCorpusAuthority.NowUtc);

        O3ACorpusPackage selfConsistent = Repack(authority, original, [changed, .. original.Contents.Skip(1)]);
        O3ACorpusPackage forged = selfConsistent with
        {
            Manifest = selfConsistent.Manifest with { DatasetId = "dataset-substituted" },
        };
        O3ACorpusVerification substitution = O3ACorpusVerifier.Verify(
            forged,
            O3ASyntheticCorpusAuthority.NowUtc);

        Assert.Equal(O3ACorpusDisposition.Quarantined, corruption.Disposition);
        Assert.Equal("o3a.membership.invalid", corruption.Code);
        Assert.Equal(O3ACorpusDisposition.Quarantined, substitution.Disposition);
        Assert.Equal("o3a.authority.unproved", substitution.Code);
    }

    /// <summary>Proves duplicate identifiers and cross-partition evidence or source leakage are rejected.</summary>
    [Fact]
    public void DuplicateAndPartitionLeakageAreRejected()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();

        O3ACorpusContent leaked = original.Contents[1] with
        {
            EvidenceFingerprint = original.Contents[0].EvidenceFingerprint,
            SourceGroupId = original.Contents[0].SourceGroupId,
        };
        O3ACorpusPackage leakedPackage = Repack(
            authority,
            original,
            [original.Contents[0], leaked, .. original.Contents.Skip(2)]);
        O3ACorpusVerification leakage = O3ACorpusVerifier.Verify(
            leakedPackage,
            O3ASyntheticCorpusAuthority.NowUtc);

        O3ACorpusMember duplicate = original.Manifest.Members[1] with
        {
            CaseId = original.Manifest.Members[0].CaseId,
        };
        O3ACorpusMember[] members = Replace(original.Manifest.Members, 1, duplicate);
        O3ACorpusManifestPayload duplicateManifest = original.Manifest with
        {
            Members = Array.AsReadOnly(members),
            MembershipDigest = O3ACorpusVerifier.MembershipDigest(members),
        };
        O3ACorpusPackage duplicatePackage = authority.Sign(duplicateManifest, original.Contents);
        O3ACorpusVerification duplication = O3ACorpusVerifier.Verify(
            duplicatePackage,
            O3ASyntheticCorpusAuthority.NowUtc);

        Assert.Equal(O3ACorpusDisposition.Rejected, leakage.Disposition);
        Assert.Equal("o3a.partition.invalid", leakage.Code);
        Assert.Equal(O3ACorpusDisposition.Quarantined, duplication.Disposition);
        Assert.Equal("o3a.membership.invalid", duplication.Code);
    }

    /// <summary>Proves missingness, label conflicts, poisoning and undeclared segments cannot be admitted.</summary>
    [Fact]
    public void MissingConflictPoisoningAndIncompleteCriteriaFailClosed()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();

        O3ACorpusVerification missing = VerifyChanged(
            authority,
            original,
            0,
            original.Contents[0] with { IsMissing = true });
        O3ACorpusVerification conflict = VerifyChanged(
            authority,
            original,
            1,
            original.Contents[1] with
            {
                ScenarioId = original.Contents[0].ScenarioId,
                Outcome = original.Contents[0].Outcome == "healthy" ? "critical" : "healthy",
            });
        O3ACorpusVerification poisoning = VerifyChanged(
            authority,
            original,
            2,
            original.Contents[2] with { NormalisedValue = 4.2 });
        O3ACorpusManifestPayload incompleteManifest = original.Manifest with
        {
            Criteria = Array.AsReadOnly(original.Manifest.Criteria.Take(2).ToArray()),
        };
        O3ACorpusVerification incomplete = O3ACorpusVerifier.Verify(
            authority.Sign(incompleteManifest, original.Contents),
            O3ASyntheticCorpusAuthority.NowUtc);

        Assert.All(
            new[] { missing, conflict, poisoning, incomplete },
            result => Assert.Equal(O3ACorpusDisposition.Rejected, result.Disposition));
        Assert.Equal("o3a.quality.unproved", missing.Code);
        Assert.Equal("o3a.quality.unproved", conflict.Code);
        Assert.Equal("o3a.quality.unproved", poisoning.Code);
        Assert.Equal("o3a.manifest.invalid", incomplete.Code);
    }

    /// <summary>Proves owner, governance and attestation roles are distinct and exact.</summary>
    [Fact]
    public void WrongRoleOrSignatureIsQuarantined()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();
        O3ACorpusPackage wrongRole = original with
        {
            Approvals = Array.AsReadOnly(
                new[]
                {
                    original.Approvals[0],
                    original.Approvals[1] with { Role = O1TrustRole.DataOwnerApprover },
                }),
        };
        O3ACorpusPackage badSignature = original with
        {
            Attestation = original.Attestation with
            {
                Signature = Convert.ToBase64String(new byte[64]),
            },
        };

        Assert.Equal(
            "o3a.authority.unproved",
            O3ACorpusVerifier.Verify(wrongRole, O3ASyntheticCorpusAuthority.NowUtc).Code);
        Assert.Equal(
            "o3a.authority.unproved",
            O3ACorpusVerifier.Verify(badSignature, O3ASyntheticCorpusAuthority.NowUtc).Code);
    }

    /// <summary>Proves scope expiry and any production-representativeness claim fail closed.</summary>
    [Fact]
    public void ExpiryAndProductionClaimAreRejected()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();
        O3ACorpusManifestPayload claim = original.Manifest with
        {
            Governance = original.Manifest.Governance with { ProductionRepresentative = true },
        };

        O3ACorpusVerification expired = O3ACorpusVerifier.Verify(
            original,
            original.Manifest.ExpiresUtc);
        O3ACorpusVerification productionClaim = O3ACorpusVerifier.Verify(
            authority.Sign(claim, original.Contents),
            O3ASyntheticCorpusAuthority.NowUtc);

        Assert.Equal("o3a.scope.expired", expired.Code);
        Assert.Equal("o3a.manifest.invalid", productionClaim.Code);
        Assert.False(expired.ProductionRepresentative);
        Assert.False(productionClaim.ProductionRepresentative);
    }

    /// <summary>Proves direct succession and withdrawal while rejecting rollback, gap, divergence, partition drift and reuse.</summary>
    [Fact]
    public void ContinuityAndWithdrawalAreMonotonic()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage first = authority.Build();
        O3ACorpusHead head = Assert.IsType<O3ACorpusHead>(
            O3ACorpusVerifier.Verify(first, O3ASyntheticCorpusAuthority.NowUtc).Head);
        O3ACorpusPackage second = authority.Successor(head);
        O3ACorpusVerification acceptedSecond = O3ACorpusVerifier.Verify(
            second,
            O3ASyntheticCorpusAuthority.NowUtc,
            head);
        O3ACorpusHead secondHead = Assert.IsType<O3ACorpusHead>(acceptedSecond.Head);
        O3ACorpusPackage withdrawn = authority.Successor(secondHead, withdrawn: true);
        O3ACorpusVerification acceptedWithdrawal = O3ACorpusVerifier.Verify(
            withdrawn,
            O3ASyntheticCorpusAuthority.NowUtc,
            secondHead);
        O3ACorpusHead withdrawnHead = Assert.IsType<O3ACorpusHead>(acceptedWithdrawal.Head);

        O3ACorpusManifestPayload rollback = second.Manifest with
        {
            Revision = 1,
            PreviousManifestDigest = string.Empty,
        };
        O3ACorpusManifestPayload gap = second.Manifest with { Revision = secondHead.Revision + 2 };
        O3ACorpusManifestPayload divergence = second.Manifest with
        {
            Revision = secondHead.Revision + 1,
            PreviousManifestDigest = O1CanonicalCryptography.TextDigest("wrong-head"),
        };
        O3ACorpusMember[] changedPartitionMembers = second.Manifest.Members.ToArray();
        changedPartitionMembers[0] = changedPartitionMembers[0] with
        {
            Partition = O3ACorpusPartition.Holdout,
        };
        O3ACorpusManifestPayload changedPartition = second.Manifest with
        {
            Revision = secondHead.Revision + 1,
            PreviousManifestDigest = secondHead.ManifestDigest,
            Members = Array.AsReadOnly(changedPartitionMembers),
            MembershipDigest = O3ACorpusVerifier.MembershipDigest(changedPartitionMembers),
        };
        O3ACorpusPackage reuse = authority.Successor(withdrawnHead, withdrawn: false);

        Assert.Equal(O3ACorpusDisposition.Accepted, acceptedSecond.Disposition);
        Assert.Equal("o3a.corpus.withdrawn", acceptedWithdrawal.Code);
        Assert.Equal(
            "o3a.continuity.rollback",
            O3ACorpusVerifier.Verify(
                authority.Sign(rollback, second.Contents),
                O3ASyntheticCorpusAuthority.NowUtc,
                secondHead).Code);
        Assert.Equal(
            "o3a.continuity.gap",
            O3ACorpusVerifier.Verify(
                authority.Sign(gap, second.Contents),
                O3ASyntheticCorpusAuthority.NowUtc,
                secondHead).Code);
        Assert.Equal(
            "o3a.continuity.divergence",
            O3ACorpusVerifier.Verify(
                authority.Sign(divergence, second.Contents),
                O3ASyntheticCorpusAuthority.NowUtc,
                secondHead).Code);
        Assert.Equal(
            "o3a.continuity.membership_changed",
            O3ACorpusVerifier.Verify(
                authority.Sign(changedPartition, second.Contents),
                O3ASyntheticCorpusAuthority.NowUtc,
                secondHead).Code);
        Assert.Equal(
            "o3a.withdrawal.irreversible",
            O3ACorpusVerifier.Verify(
                reuse,
                O3ASyntheticCorpusAuthority.NowUtc,
                withdrawnHead).Code);
    }

    /// <summary>Runs deterministic bounded mutations to prove malformed transformations never throw or admit.</summary>
    [Fact]
    public void DeterministicMutationMatrixFailsClosed()
    {
        using O3ASyntheticCorpusAuthority authority = new();
        O3ACorpusPackage original = authority.Build();
        for (int index = 0; index < 96; index++)
        {
            int target = index % original.Contents.Count;
            O3ACorpusContent changed = (index % 3) switch
            {
                0 => original.Contents[target] with { NormalisedValue = 2 + index },
                1 => original.Contents[target] with { Transformation = $"poison-{index}" },
                _ => original.Contents[target] with { Outcome = $"unknown-{index}" },
            };

            O3ACorpusVerification result = VerifyChanged(authority, original, target, changed);

            Assert.NotEqual(O3ACorpusDisposition.Accepted, result.Disposition);
            Assert.InRange(result.Code.Length, 1, 64);
            Assert.Equal(0, result.CaseCount);
        }
    }

    /// <summary>Proves the process bridge requires the exact marker and emits aggregate non-operational evidence only.</summary>
    [Fact]
    public async Task ProcessBridgeIsExactAndSanitised()
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using StringWriter output = new();
        using StringWriter error = new();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int invalid = await O3ASandboxProcess.RunAsync(
                ["--activation", "wrong", "--run-id", Guid.NewGuid().ToString("D")]);
            int accepted = await O3ASandboxProcess.RunAsync(
                [
                    "--activation",
                    O3ACorpusVerifier.ActivationMarker,
                    "--run-id",
                    Guid.NewGuid().ToString("D"),
                ]);

            Assert.Equal(2, invalid);
            Assert.Equal(0, accepted);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        string evidence = output.ToString();
        Assert.Contains("\"cases\":9", evidence, StringComparison.Ordinal);
        Assert.Contains("\"partitions\":3", evidence, StringComparison.Ordinal);
        Assert.Contains("\"productionRepresentative\":false", evidence, StringComparison.Ordinal);
        Assert.Contains("\"activationState\":\"None\"", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("scenario-", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("source-group-", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("provider", evidence, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Rebuilds content-addressed membership before verifying one deliberate content mutation.</summary>
    private static O3ACorpusVerification VerifyChanged(
        O3ASyntheticCorpusAuthority authority,
        O3ACorpusPackage original,
        int index,
        O3ACorpusContent changed)
    {
        O3ACorpusContent[] contents = Replace(original.Contents, index, changed);
        return O3ACorpusVerifier.Verify(
            Repack(authority, original, contents),
            O3ASyntheticCorpusAuthority.NowUtc);
    }

    /// <summary>Rebuilds exact member digests and role signatures for a self-consistent adversarial package.</summary>
    private static O3ACorpusPackage Repack(
        O3ASyntheticCorpusAuthority authority,
        O3ACorpusPackage original,
        IReadOnlyList<O3ACorpusContent> contents)
    {
        Dictionary<string, O3ACorpusContent> byId = contents.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        O3ACorpusMember[] members = original.Manifest.Members
            .Select(
                member => member with
                {
                    Segment = byId[member.CaseId].Segment,
                    ContentDigest = O1CanonicalCryptography.Digest(byId[member.CaseId]),
                })
            .ToArray();
        O3ACorpusManifestPayload manifest = original.Manifest with
        {
            Members = Array.AsReadOnly(members),
            MembershipDigest = O3ACorpusVerifier.MembershipDigest(members),
        };
        return authority.Sign(manifest, contents);
    }

    /// <summary>Creates a copied collection with one exact index replaced.</summary>
    private static T[] Replace<T>(IReadOnlyList<T> values, int index, T replacement)
    {
        T[] copy = values.ToArray();
        copy[index] = replacement;
        return copy;
    }
}
