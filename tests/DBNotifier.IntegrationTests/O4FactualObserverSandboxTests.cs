// Module purpose: Proves O4 traceability, fail-closed publication and exact test-only activation with synthetic O2/O3 results.
using DBNotifier.Application.AIOps;
using Xunit;

namespace DBNotifier.IntegrationTests;

/// <summary>Exercises the in-memory O4 projection and its adversarial publication boundary.</summary>
public sealed class O4FactualObserverSandboxTests
{
    /// <summary>Proves O4 is built only from accepted O2/O3 outputs and remains factual and non-authorising.</summary>
    [Fact]
    public async Task AcceptedO2AndO3ResultsProduceTraceableReadOnlyProjection()
    {
        await WithProjectionAsync(
            (projection, now) =>
            {
                O4ObserverProjectionVerification verification =
                    O4ObserverProjectionVerifier.Verify(projection, projection, now);

                Assert.True(verification.Accepted);
                Assert.Equal("o4.projection.accepted", verification.Code);
                Assert.True(projection.Payload.ProcessingCompleted);
                Assert.True(projection.Payload.Synthetic);
                Assert.False(projection.Payload.Operational);
                Assert.False(projection.Payload.ProductionRepresentative);
                Assert.False(projection.Payload.IsAuthorising);
                Assert.False(projection.Payload.Revoked);
                Assert.False(projection.Payload.Superseded);
                Assert.Equal(ObserverActivationState.None.ToString(), projection.Payload.ActivationState);
                Assert.Matches("^[A-Fa-f0-9]{64}$", projection.Payload.Trace.O2EnvelopeDigest);
                Assert.Matches("^[A-Fa-f0-9]{64}$", projection.Payload.Trace.O3ResultDigest);
                Assert.Matches("^[A-Fa-f0-9]{64}$", projection.Payload.Trace.PolicyDigest);
                Assert.Matches("^[A-Fa-f0-9]{64}$", projection.Payload.Trace.CorpusManifestDigest);
                Assert.Single(projection.Payload.Signals);
                Assert.Equal(O4ObserverFreshness.Stale, projection.Payload.Signals[0].Freshness);
                Assert.Equal("Unknown", projection.Payload.Forecasts.State);
                Assert.Contains(
                    "o4.limitation.forecast_unavailable",
                    projection.Payload.Forecasts.Limitations);
                Assert.DoesNotContain(
                    projection.GetType().GetProperties(),
                    property => property.Name.Contains("Recommend", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("Command", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase));
                return Task.CompletedTask;
            });
    }

    /// <summary>Proves incomplete, altered, expired, revoked, superseded and future projections all fail closed.</summary>
    [Fact]
    public async Task UnsafeProjectionVariantsFailClosedWithoutPartialPublication()
    {
        await WithProjectionAsync(
            (accepted, now) =>
            {
                O4ObserverProjectionPayload[] unsafePayloads =
                [
                    accepted.Payload with { ProcessingCompleted = false },
                    accepted.Payload with { ValidUntilUtc = now },
                    accepted.Payload with { Revoked = true },
                    accepted.Payload with { Superseded = true },
                    accepted.Payload with { GeneratedAtUtc = now.AddMinutes(2), ValidUntilUtc = now.AddMinutes(3) },
                    accepted.Payload with
                    {
                        Trace = accepted.Payload.Trace with
                        {
                            PolicyDigest = O1CanonicalCryptography.TextDigest("different-policy"),
                        },
                    },
                    accepted.Payload with
                    {
                        Trace = accepted.Payload.Trace with
                        {
                            CorpusManifestDigest = O1CanonicalCryptography.TextDigest("different-corpus"),
                        },
                    },
                    accepted.Payload with { Signals = Array.Empty<O4ObserverSignal>() },
                ];

                foreach (O4ObserverProjectionPayload payload in unsafePayloads)
                {
                    O4ObserverProjectionPackage candidate =
                        new(payload, O1CanonicalCryptography.Digest(payload));
                    O4ObserverProjectionVerification verification =
                        O4ObserverProjectionVerifier.Verify(candidate, accepted, now);
                    Assert.False(verification.Accepted);
                    Assert.Equal("o4.projection.failed_closed", verification.Code);
                }

                O4ObserverProjectionPackage alteredWithoutDigest = accepted with
                {
                    Payload = accepted.Payload with { SourceLabel = "altered-source" },
                };
                Assert.False(
                    O4ObserverProjectionVerifier
                        .Verify(alteredWithoutDigest, accepted, now)
                        .Accepted);
                return Task.CompletedTask;
            });
    }

    /// <summary>Proves the process marker parser refuses missing and approximate activation before a listener exists.</summary>
    [Fact]
    public async Task ProcessRejectsMissingAndApproximateActivation()
    {
        Assert.Equal(2, await O4SandboxProcess.RunAsync([]));
        Assert.Equal(
            2,
            await O4SandboxProcess.RunAsync(
            [
                "--activation",
                "o4-factual-observer-projection",
                "--dashboard-root",
                "missing",
                "--run-id",
                Guid.NewGuid().ToString("D"),
            ]));
    }

    /// <summary>Creates and removes one exact O4 root around a projection assertion.</summary>
    private static async Task WithProjectionAsync(
        Func<O4ObserverProjectionPackage, DateTimeOffset, Task> assertion)
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"DBNotifier-O4-Test-{Guid.NewGuid():N}");
        string candidate = Path.GetFullPath(root);
        string temporary = Path.GetFullPath(Path.GetTempPath());
        if (!candidate.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(candidate).StartsWith("DBNotifier-O4-Test-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("o4.test.cleanup_scope_invalid");
        }
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            O4ObserverProjectionPackage projection =
                await O4ObserverProjectionFactory.CreateAsync(root, now);
            await assertion(projection, now);
        }
        finally
        {
            if (Directory.Exists(candidate))
            {
                Directory.Delete(candidate, recursive: true);
            }
        }
        Assert.False(Directory.Exists(candidate));
    }
}
