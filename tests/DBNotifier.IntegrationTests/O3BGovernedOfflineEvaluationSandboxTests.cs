// Module purpose: Proves O3-B freezes governed deterministic policy before one-use holdout evaluation and fails closed.
using System.Collections.ObjectModel;
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises accepted, bounded and adversarial O3-B paths using only the approved O3-A synthetic corpus.</summary>
public sealed class O3BGovernedOfflineEvaluationSandboxTests
{
    private static readonly DateTimeOffset FrozenAt = O3ASyntheticCorpusAuthority.NowUtc.AddMinutes(1);
    private static readonly DateTimeOffset EvaluatedAt = FrozenAt.AddMinutes(1);

    /// <summary>Proves policy is frozen before holdout and produces complete non-authorising segmented measurements.</summary>
    [Fact]
    public void FrozenPolicyEvaluatesHoldoutWithCompleteSyntheticMetrics()
    {
        using Fixture fixture = new();

        O3BEvaluationOutcome outcome = fixture.Evaluate();

        Assert.Equal(O3BEvaluationDisposition.Accepted, outcome.Disposition);
        Assert.Equal("o3b.synthetic.approved", outcome.Code);
        O3BEvaluationPackage package = Assert.IsType<O3BEvaluationPackage>(outcome.Package);
        Assert.True(package.Payload.SyntheticApproved);
        Assert.False(package.Payload.ProductionRepresentative);
        Assert.False(package.Payload.IsAuthorising);
        Assert.Equal(3, package.Payload.SegmentMetrics.Count);
        Assert.All(
            package.Payload.SegmentMetrics,
            metric =>
            {
                Assert.Equal(1, metric.CaseCount);
                Assert.Equal(1d, metric.Coverage);
                Assert.Equal(0d, metric.AbstentionRate);
                Assert.Equal(1d, metric.Stability);
                Assert.Equal(1d, metric.Explainability);
                Assert.True(metric.CriteriaPassed);
                Assert.InRange(metric.MeanAbsolutePredictionError, 0d, 1d);
                Assert.InRange(metric.AccountedDurationUnits, 1, 1);
                Assert.InRange(metric.AccountedMemoryBytes, 4_096, 4_096);
                Assert.InRange(metric.WorkUnits, 4, 4);
            });
        Assert.Equal(1, fixture.Gate.HoldoutReadCount);
        Assert.Equal(
            O3BEvaluationDisposition.Accepted,
            O3BGovernanceVerifier.VerifyResult(package, fixture.Policy).Disposition);
        Assert.NotNull(outcome.Head);
    }

    /// <summary>Proves calibration can inspect only development and calibration before freeze.</summary>
    [Fact]
    public void CalibrationDoesNotReadHoldoutAndPolicyIsContentAddressed()
    {
        using Fixture fixture = new();

        Assert.Equal(0, fixture.Gate.HoldoutReadCount);
        Assert.True(fixture.Policy.Policy.Frozen);
        Assert.False(fixture.Policy.Policy.ProductionRepresentative);
        Assert.False(fixture.Policy.Policy.IsAuthorising);
        Assert.Equal(
            O1CanonicalCryptography.Digest(fixture.Policy.Policy),
            fixture.Policy.PolicyDigest);
        Assert.Equal(3, fixture.Policy.Policy.Rules.Count);
        Assert.Equal(3, fixture.Policy.Policy.Criteria.Count);
        Assert.Equal(
            O3BEvaluationDisposition.Accepted,
            O3BGovernanceVerifier.VerifyPolicy(
                fixture.Policy,
                fixture.Corpus,
                fixture.CorpusHead,
                EvaluatedAt).Disposition);
    }

    /// <summary>Proves holdout cannot open before freeze and cannot be read twice.</summary>
    [Fact]
    public void HoldoutAccessBeforeFreezeOrAfterUseFailsClosed()
    {
        using Fixture fixture = new();
        O3BFrozenPolicyPayload notFrozen = fixture.Policy.Policy with { Frozen = false };

        Assert.Throws<InvalidOperationException>(
            () => fixture.Gate.ReadHoldout(notFrozen, EvaluatedAt));
        Assert.Single(fixture.Gate.ReadHoldout(fixture.Policy.Policy, EvaluatedAt).Take(1));
        Assert.Throws<InvalidOperationException>(
            () => fixture.Gate.ReadHoldout(fixture.Policy.Policy, EvaluatedAt.AddSeconds(1)));
    }

    /// <summary>Proves policy mutation, wrong role, revision drift and corpus binding divergence fail closed.</summary>
    [Fact]
    public void AlteredPolicyAndDivergentRevisionFailClosed()
    {
        using Fixture fixture = new();
        O3BFrozenRule changedRule = fixture.Policy.Policy.Rules[0] with { Threshold = 0.99 };
        O3BFrozenPolicyPackage unsignedChange = fixture.Policy with
        {
            Policy = fixture.Policy.Policy with
            {
                Rules = Array.AsReadOnly(
                    new[] { changedRule }.Concat(fixture.Policy.Policy.Rules.Skip(1)).ToArray()),
            },
        };
        O3BFrozenPolicyPackage wrongRole = fixture.Policy with
        {
            Approvals = Array.AsReadOnly(
                new[]
                {
                    fixture.Policy.Approvals[0],
                    fixture.Policy.Approvals[1] with { Role = O1TrustRole.PolicyApproverA },
                }),
        };
        O3BFrozenPolicyPackage revisionDrift = fixture.PolicyAuthority.Sign(
            fixture.Policy.Policy with { CorpusRevision = fixture.CorpusHead.Revision + 1 });
        O3BFrozenPolicyPackage corpusDrift = fixture.PolicyAuthority.Sign(
            fixture.Policy.Policy with
            {
                CorpusMembershipDigest = O1CanonicalCryptography.TextDigest("different-membership"),
            });

        Assert.Equal(
            "o3b.policy.invalid",
            VerifyPolicy(fixture, unsignedChange).Code);
        Assert.Equal(
            "o3b.policy.authority_unproved",
            VerifyPolicy(fixture, wrongRole).Code);
        Assert.Equal(
            "o3b.policy.invalid",
            VerifyPolicy(fixture, revisionDrift).Code);
        Assert.Equal(
            "o3b.policy.invalid",
            VerifyPolicy(fixture, corpusDrift).Code);
    }

    /// <summary>Proves withdrawn, expired, leaked and incomplete corpus candidates never reach holdout.</summary>
    [Fact]
    public void WithdrawnExpiredLeakedAndIncompleteCorpusFailClosed()
    {
        using Fixture fixture = new();
        O3ACorpusPackage withdrawn = fixture.CorpusAuthority.Sign(
            fixture.Corpus.Manifest with { Withdrawn = true },
            fixture.Corpus.Contents);
        O3BEvaluationOutcome withdrawnResult = O3BOfflineEvaluator.Evaluate(
            withdrawn,
            fixture.CorpusHead,
            fixture.Policy,
            fixture.PolicyAuthority,
            new(withdrawn),
            fixture.Budget,
            EvaluatedAt);
        O3BEvaluationOutcome expired = O3BOfflineEvaluator.Evaluate(
            fixture.Corpus,
            fixture.CorpusHead,
            fixture.Policy,
            fixture.PolicyAuthority,
            new(fixture.Corpus),
            fixture.Budget with { DeadlineUtc = fixture.Corpus.Manifest.ExpiresUtc.AddMinutes(1) },
            fixture.Corpus.Manifest.ExpiresUtc);

        O3ACorpusContent leakedContent = fixture.Corpus.Contents[1] with
        {
            EvidenceFingerprint = fixture.Corpus.Contents[0].EvidenceFingerprint,
        };
        O3ACorpusPackage leaked = Repack(
            fixture.CorpusAuthority,
            fixture.Corpus,
            [fixture.Corpus.Contents[0], leakedContent, .. fixture.Corpus.Contents.Skip(2)]);
        O3BEvaluationOutcome leakedResult = O3BOfflineEvaluator.Evaluate(
            leaked,
            fixture.CorpusHead,
            fixture.Policy,
            fixture.PolicyAuthority,
            new(leaked),
            fixture.Budget,
            EvaluatedAt);

        O3ACorpusPackage incomplete = fixture.Corpus with
        {
            Contents = Array.AsReadOnly(fixture.Corpus.Contents.Take(8).ToArray()),
        };
        O3BEvaluationOutcome incompleteResult = O3BOfflineEvaluator.Evaluate(
            incomplete,
            fixture.CorpusHead,
            fixture.Policy,
            fixture.PolicyAuthority,
            new(incomplete),
            fixture.Budget,
            EvaluatedAt);

        Assert.Equal("o3b.corpus.withdrawn", withdrawnResult.Code);
        Assert.Equal("o3b.corpus.unavailable", expired.Code);
        Assert.Equal("o3b.corpus.unavailable", leakedResult.Code);
        Assert.Equal("o3b.corpus.unavailable", incompleteResult.Code);
        Assert.Equal(0, fixture.Gate.HoldoutReadCount);
    }

    /// <summary>Proves deadline, cancellation, case, work and memory saturation refuse partial publication.</summary>
    [Fact]
    public void DeadlineCancellationAndResourceLimitsFailClosed()
    {
        using Fixture fixture = new();
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        O3BEvaluationOutcome deadline = fixture.Evaluate(
            gate: new(fixture.Corpus),
            budget: fixture.Budget with { DeadlineUtc = EvaluatedAt });
        O3BEvaluationOutcome cancellation = fixture.Evaluate(
            gate: new(fixture.Corpus),
            cancellationToken: cancelled.Token);
        O3BEvaluationOutcome cases = fixture.Evaluate(
            gate: new(fixture.Corpus),
            budget: fixture.Budget with { MaximumCases = 2 });
        O3BEvaluationOutcome work = fixture.Evaluate(
            gate: new(fixture.Corpus),
            budget: fixture.Budget with { MaximumWorkUnits = 11 });
        O3BEvaluationOutcome memory = fixture.Evaluate(
            gate: new(fixture.Corpus),
            budget: fixture.Budget with { MaximumAccountedMemoryBytes = 12_287 });

        Assert.Equal("o3b.evaluation.deadline", deadline.Code);
        Assert.Equal("o3b.evaluation.cancelled", cancellation.Code);
        Assert.All(
            new[] { cases, work, memory },
            result =>
            {
                Assert.Equal(O3BEvaluationDisposition.Rejected, result.Disposition);
                Assert.Equal("o3b.evaluation.resource_limit", result.Code);
                Assert.Null(result.Package);
            });
    }

    /// <summary>Proves accepted pairs are one-use and earlier corpus revisions are quarantined.</summary>
    [Fact]
    public void HoldoutReuseAndRollbackAreQuarantined()
    {
        using Fixture fixture = new();
        O3BEvaluationOutcome first = fixture.Evaluate();
        O3BEvaluationHead head = Assert.IsType<O3BEvaluationHead>(first.Head);

        O3BEvaluationOutcome reuse = fixture.Evaluate(
            gate: new(fixture.Corpus),
            current: head);
        O3BEvaluationHead future = head with { CorpusRevision = head.CorpusRevision + 1 };
        O3BEvaluationOutcome rollback = fixture.Evaluate(
            gate: new(fixture.Corpus),
            current: future);

        Assert.Equal(O3BEvaluationDisposition.Quarantined, reuse.Disposition);
        Assert.Equal("o3b.holdout.reuse", reuse.Code);
        Assert.Equal(O3BEvaluationDisposition.Quarantined, rollback.Disposition);
        Assert.Equal("o3b.continuity.rollback", rollback.Code);
    }

    /// <summary>Proves incomplete, altered and non-finite aggregate metrics cannot be published as accepted.</summary>
    [Fact]
    public void IncompleteTamperedAndNonFiniteMetricsFailClosed()
    {
        using Fixture fixture = new();
        O3BEvaluationPackage original = Assert.IsType<O3BEvaluationPackage>(fixture.Evaluate().Package);
        O3BEvaluationPackage incomplete = original with
        {
            Payload = original.Payload with
            {
                SegmentMetrics = Array.AsReadOnly(original.Payload.SegmentMetrics.Take(2).ToArray()),
            },
        };
        O3BSegmentMetrics changed = original.Payload.SegmentMetrics[0] with { FalsePositiveCount = 99 };
        O3BEvaluationPackage tampered = original with
        {
            Payload = original.Payload with
            {
                SegmentMetrics = Array.AsReadOnly(
                    new[] { changed }.Concat(original.Payload.SegmentMetrics.Skip(1)).ToArray()),
            },
        };
        O3BSegmentMetrics nonFiniteMetric = original.Payload.SegmentMetrics[0] with
        {
            MeanAbsolutePredictionError = double.NaN,
        };
        O3BEvaluationPackage nonFinite = original with
        {
            Payload = original.Payload with
            {
                SegmentMetrics = Array.AsReadOnly(
                    new[] { nonFiniteMetric }.Concat(original.Payload.SegmentMetrics.Skip(1)).ToArray()),
            },
        };

        Assert.Equal("o3b.result.invalid", VerifyResult(fixture, incomplete).Code);
        Assert.Equal("o3b.result.invalid", VerifyResult(fixture, tampered).Code);
        Assert.Equal("o3b.result.invalid", VerifyResult(fixture, nonFinite).Code);
    }

    /// <summary>Runs deterministic bounded policy mutations as a property-style fail-closed matrix.</summary>
    [Fact]
    public void DeterministicPolicyMutationMatrixNeverAdmits()
    {
        using Fixture fixture = new();
        for (int index = 0; index < 96; index++)
        {
            O3BFrozenPolicyPayload changed = (index % 4) switch
            {
                0 => fixture.Policy.Policy with { Frozen = false },
                1 => fixture.Policy.Policy with { PolicyRevision = 2 + index },
                2 => fixture.Policy.Policy with { ProductionRepresentative = true },
                _ => fixture.Policy.Policy with { IsAuthorising = true },
            };
            O3BFrozenPolicyPackage package = fixture.PolicyAuthority.Sign(changed);

            (O3BEvaluationDisposition disposition, string code) = VerifyPolicy(fixture, package);

            Assert.NotEqual(O3BEvaluationDisposition.Accepted, disposition);
            Assert.InRange(code.Length, 1, 64);
        }
    }

    /// <summary>Proves the exact process marker emits only aggregate synthetic and inactive evidence.</summary>
    [Fact]
    public async Task ProcessBridgeIsExactSanitisedAndInactive()
    {
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        using StringWriter output = new();
        using StringWriter error = new();
        try
        {
            Console.SetOut(output);
            Console.SetError(error);
            int invalid = await O3BSandboxProcess.RunAsync(
                ["--activation", "wrong", "--run-id", Guid.NewGuid().ToString("D")]);
            int accepted = await O3BSandboxProcess.RunAsync(
                [
                    "--activation",
                    O3BGovernanceVerifier.ActivationMarker,
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
        Assert.Contains("\"segments\":3", evidence, StringComparison.Ordinal);
        Assert.Contains("\"syntheticApproved\":true", evidence, StringComparison.Ordinal);
        Assert.Contains("\"productionRepresentative\":false", evidence, StringComparison.Ordinal);
        Assert.Contains("\"authorising\":false", evidence, StringComparison.Ordinal);
        Assert.Contains("\"activationState\":\"None\"", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("case-", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("scenario-", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("source-group-", evidence, StringComparison.Ordinal);
    }

    /// <summary>Verifies one candidate policy against the fixture's exact accepted corpus head.</summary>
    private static (O3BEvaluationDisposition Disposition, string Code) VerifyPolicy(
        Fixture fixture,
        O3BFrozenPolicyPackage policy) =>
        O3BGovernanceVerifier.VerifyPolicy(
            policy,
            fixture.Corpus,
            fixture.CorpusHead,
            EvaluatedAt);

    /// <summary>Verifies one result package against the fixture's exact frozen policy.</summary>
    private static (O3BEvaluationDisposition Disposition, string Code) VerifyResult(
        Fixture fixture,
        O3BEvaluationPackage result) =>
        O3BGovernanceVerifier.VerifyResult(result, fixture.Policy);

    /// <summary>Rebuilds exact content-addressed membership and signatures for one adversarial corpus.</summary>
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

    /// <summary>Owns one isolated synthetic corpus, frozen policy, partition gate and bounded evaluation budget.</summary>
    private sealed class Fixture : IDisposable
    {
        /// <summary>Builds and verifies one exact O3-A package before freezing its O3-B policy.</summary>
        internal Fixture()
        {
            Corpus = CorpusAuthority.Build();
            O3ACorpusVerification verification = O3ACorpusVerifier.Verify(
                Corpus,
                O3ASyntheticCorpusAuthority.NowUtc);
            CorpusHead = Assert.IsType<O3ACorpusHead>(verification.Head);
            Gate = new(Corpus);
            Policy = PolicyAuthority.Freeze(Corpus, CorpusHead, Gate, FrozenAt);
            Budget = new(9, 36, 36_864, EvaluatedAt.AddMinutes(1));
        }

        /// <summary>Gets the ephemeral O3-A authority.</summary>
        internal O3ASyntheticCorpusAuthority CorpusAuthority { get; } = new();

        /// <summary>Gets the ephemeral O3-B policy and result authority.</summary>
        internal O3BSyntheticPolicyAuthority PolicyAuthority { get; } = new();

        /// <summary>Gets the exact authenticated O3-A package.</summary>
        internal O3ACorpusPackage Corpus { get; }

        /// <summary>Gets the accepted exact O3-A head.</summary>
        internal O3ACorpusHead CorpusHead { get; }

        /// <summary>Gets the default one-use partition gate.</summary>
        internal O3BPartitionGate Gate { get; }

        /// <summary>Gets the frozen authenticated policy.</summary>
        internal O3BFrozenPolicyPackage Policy { get; }

        /// <summary>Gets the bounded default evaluation budget.</summary>
        internal O3BEvaluationBudget Budget { get; }

        /// <summary>Runs one evaluation with optional isolated gate, budget, continuity and cancellation variants.</summary>
        /// <param name="gate">Optional gate; defaults to the fixture's one-use gate.</param>
        /// <param name="budget">Optional budget; defaults to the bounded accepted budget.</param>
        /// <param name="current">Optional accepted predecessor.</param>
        /// <param name="cancellationToken">Optional cancellation.</param>
        /// <returns>Bounded evaluation outcome.</returns>
        internal O3BEvaluationOutcome Evaluate(
            O3BPartitionGate? gate = null,
            O3BEvaluationBudget? budget = null,
            O3BEvaluationHead? current = null,
            CancellationToken cancellationToken = default) =>
            O3BOfflineEvaluator.Evaluate(
                Corpus,
                CorpusHead,
                Policy,
                PolicyAuthority,
                gate ?? Gate,
                budget ?? Budget,
                EvaluatedAt,
                current,
                cancellationToken);

        /// <summary>Disposes all ephemeral signing keys without persistence.</summary>
        public void Dispose()
        {
            PolicyAuthority.Dispose();
            CorpusAuthority.Dispose();
        }
    }
}
