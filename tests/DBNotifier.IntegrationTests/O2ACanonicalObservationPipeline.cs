// Module purpose: Composes the O2-A canonical read-only observation pipeline exclusively inside the exact test-only sandbox.
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.AIOps;
using DBNotifier.Application.Synchronization;
using DBNotifier.Domain;

namespace DBNotifier.IntegrationTests;

/// <summary>
/// Carries one immutable, provider-neutral observation across the Server-to-MOD-12 boundary while retaining its exact
/// source sequence, O1 context and data-handling declarations. It contains no provider-native payload or secret.
/// </summary>
internal sealed record O2ACanonicalObservationEnvelope(
    string ContractVersion,
    string SourceCategory,
    Guid MessageId,
    long Sequence,
    Guid ObservationId,
    Guid InstanceId,
    Guid AgentId,
    Guid AuthorisationScopeId,
    string Environment,
    string ProviderType,
    string ProviderVersion,
    string Status,
    string Method,
    string EvidenceLevel,
    int AttemptCount,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ReceivedAtUtc,
    long DurationMilliseconds,
    IReadOnlyList<string> Limitations,
    string TransformationProfile,
    string MetricFamily,
    string Unit,
    string AccountingProfile,
    int ExpectedItemCount,
    int ActualItemCount,
    IReadOnlyList<string> MissingFields,
    string SourceDigest,
    string ContextRevision,
    ObserverDataClassification Classification,
    ObserverRedactionStatus RedactionStatus,
    ObserverRetentionClass RetentionClass,
    ObserverPermittedPurpose PermittedPurpose);

/// <summary>Records one bounded downstream decision without turning an incomplete or refused analysis into a publication.</summary>
internal sealed record O2AProcessingOutcome(
    long Sequence,
    string Code,
    bool Published,
    string EnvelopeDigest,
    string ContextRevision,
    ObserverAnalysisReport? Report);

/// <summary>Provides one immutable signed policy context and its separately configured public trust.</summary>
internal sealed record O2APolicyEvidence(
    ObserverDataPolicyVerificationContext Context,
    ObserverPolicyTrustConfiguration Trust);

/// <summary>
/// Owns purpose-separated synthetic policy and revocation keys for one pipeline fixture. Private material never leaves
/// this test-only authority and is disposed with the fixture.
/// </summary>
internal sealed class O2ASyntheticPolicyAuthority : IDisposable
{
    private const string IssuerId = "o2a.policy-authority";
    private const string GrantKeyId = "o2a.grant-key.v1";
    private const string RevocationKeyId = "o2a.revocation-key.v1";
    private const string SnapshotId = "o2a.revocations";
    private readonly ECDsa grantSigner = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ECDsa revocationSigner = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly ObserverDataPolicyGrant grant;
    private long snapshotSequence = 1;
    private bool revoked;

    /// <summary>Initialises one exact-source runtime-analysis policy for already authorised synthetic fixture evidence.</summary>
    /// <param name="agentId">Synthetic Agent identity.</param>
    /// <param name="instanceId">Synthetic instance identity.</param>
    /// <param name="scopeId">O2-A authorisation scope.</param>
    /// <param name="nowUtc">Deterministic UTC instant inside every validity period.</param>
    internal O2ASyntheticPolicyAuthority(
        Guid agentId,
        Guid instanceId,
        Guid scopeId,
        DateTimeOffset nowUtc)
    {
        ObserverDataPolicy policy = new(
            "o2a.runtime-policy",
            "1.0.0",
            ObserverDataOptInState.Enabled,
            ObserverDataUse.RuntimeAnalysis,
            scopeId,
            nowUtc.AddDays(-2),
            nowUtc.AddDays(1),
            [new ObserverDataScope(instanceId, agentId)]);
        grant = new ObserverDataPolicyGrant(
            "o2a.runtime-grant",
            IssuerId,
            GrantKeyId,
            nowUtc.AddDays(-2),
            nowUtc.AddDays(1),
            policy);
    }

    /// <summary>Gets a freshly signed exact revocation view under purpose-separated trust.</summary>
    /// <param name="nowUtc">Deterministic UTC decision instant.</param>
    /// <returns>Current policy evidence and matching trusted revocation checkpoint.</returns>
    internal O2APolicyEvidence Current(DateTimeOffset nowUtc)
    {
        ObserverPolicyRevocationSnapshot snapshot = new(
            SnapshotId,
            $"1.0.{snapshotSequence}",
            snapshotSequence,
            IssuerId,
            RevocationKeyId,
            nowUtc.AddDays(-2),
            nowUtc.AddDays(1),
            revoked ? [grant.GrantId] : [],
            []);
        byte[] grantSignature = grantSigner.SignData(
            ObserverPolicyProvenancePayload.CreateGrant(grant),
            HashAlgorithmName.SHA256);
        byte[] revocationSignature = revocationSigner.SignData(
            ObserverPolicyProvenancePayload.CreateRevocation(snapshot),
            HashAlgorithmName.SHA256);
        return new O2APolicyEvidence(
            new ObserverDataPolicyVerificationContext(
                grant,
                grantSignature,
                snapshot,
                revocationSignature),
            new ObserverPolicyTrustConfiguration(
                new ObserverPolicyTrustAnchor(
                    IssuerId,
                    GrantKeyId,
                    grantSigner.ExportSubjectPublicKeyInfo()),
                new ObserverPolicyTrustAnchor(
                    IssuerId,
                    RevocationKeyId,
                    revocationSigner.ExportSubjectPublicKeyInfo()),
                SnapshotId,
                snapshotSequence));
    }

    /// <summary>Advances the signed revocation checkpoint and revokes the only synthetic grant irreversibly.</summary>
    internal void Revoke()
    {
        revoked = true;
        snapshotSequence = checked(snapshotSequence + 1);
    }

    /// <summary>Destroys both independent synthetic private keys.</summary>
    public void Dispose()
    {
        grantSigner.Dispose();
        revocationSigner.Dispose();
    }
}

/// <summary>
/// Implements the Server ingestion boundary and joins it to O1 trust/resource admission and the pure MOD-12 analysis
/// service. The store is in-memory except for the caller-owned temporary O1 checkpoint and has no normal composition.
/// </summary>
internal sealed class O2ACanonicalObservationPipeline : IObservationIngestionStore, IDisposable
{
    internal const string ActivationMarker = "o2a-canonical-observation-pipeline-sandbox";
    internal const string ContractVersion = "o2a.canonical-observation.v1";
    private const string SourceCategory = "synthetic-fixture";
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly O1SandboxStore trustStore;
    private readonly O1SyntheticTrustFixture trustFixture;
    private readonly O1TrustCoordinator trustCoordinator;
    private readonly O1ResourceCoordinator resourceCoordinator;
    private readonly O2ASyntheticPolicyAuthority policyAuthority;
    private readonly TimeProvider timeProvider;
    private readonly Dictionary<Guid, string> messageDigests = [];
    private readonly Dictionary<Guid, string> observationDigests = [];
    private readonly SortedDictionary<long, PendingObservation> pending = [];
    private readonly List<O2AProcessingOutcome> outcomes = [];
    private readonly List<ObserverAnalysisReport> publishedReports = [];
    private long highestContiguousSequence;
    private bool disposed;

    /// <summary>Initialises an already bootstrapped O2-A pipeline.</summary>
    private O2ACanonicalObservationPipeline(
        O1SandboxStore trustStore,
        O1SyntheticTrustFixture trustFixture,
        O1TrustCoordinator trustCoordinator,
        O1ResourceCoordinator resourceCoordinator,
        O2ASyntheticPolicyAuthority policyAuthority,
        TimeProvider timeProvider,
        Guid agentId,
        Guid instanceId,
        Guid authorisationScopeId)
    {
        this.trustStore = trustStore;
        this.trustFixture = trustFixture;
        this.trustCoordinator = trustCoordinator;
        this.resourceCoordinator = resourceCoordinator;
        this.policyAuthority = policyAuthority;
        this.timeProvider = timeProvider;
        AgentId = agentId;
        InstanceId = instanceId;
        AuthorisationScopeId = authorisationScopeId;
    }

    /// <summary>Gets the exact synthetic Agent accepted by this sandbox.</summary>
    internal Guid AgentId { get; }

    /// <summary>Gets the exact synthetic instance accepted by this sandbox.</summary>
    internal Guid InstanceId { get; }

    /// <summary>Gets the immutable MOD-12 authorisation scope used by every accepted sample.</summary>
    internal Guid AuthorisationScopeId { get; }

    /// <summary>Gets completed, non-authorising reports only; refused and interrupted attempts never enter this view.</summary>
    internal IReadOnlyList<ObserverAnalysisReport> PublishedReports => publishedReports.AsReadOnly();

    /// <summary>Gets every downstream decision in deterministic contiguous-sequence order.</summary>
    internal IReadOnlyList<O2AProcessingOutcome> Outcomes => outcomes.AsReadOnly();

    /// <summary>Gets the O1 resource release result for quiescence and fence assertions.</summary>
    internal string LastResourceReleaseCode => resourceCoordinator.LastReleaseCode;

    /// <summary>Gets or sets one bounded hook used to supersede context or observe cancellation before publication.</summary>
    internal Func<CancellationToken, Task>? BeforePublicationAsync { get; set; }

    /// <summary>Gets or sets one exact next-message deadline used by deterministic deadline tests.</summary>
    internal DateTimeOffset? NextDeadlineUtc { get; set; }

    /// <summary>Gets or sets one exact next-message resource demand used by boundary tests.</summary>
    internal O1ResourceRequest? NextResourceRequest { get; set; }

    /// <summary>
    /// Restores an already authenticated O2-B continuity baseline into a newly created in-memory O2-A session.
    /// </summary>
    /// <param name="highestSequence">Highest sequence durably completed by O2-B.</param>
    /// <param name="completedMessageDigests">Retained completed message identifiers and their canonical digests.</param>
    /// <param name="completedObservationDigests">Retained completed observation identifiers and their canonical digests.</param>
    /// <exception cref="InvalidOperationException">Thrown when the session is not fresh or the baseline is inconsistent.</exception>
    internal void RestoreDurableBaseline(
        long highestSequence,
        IReadOnlyDictionary<Guid, string> completedMessageDigests,
        IReadOnlyDictionary<Guid, string> completedObservationDigests)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(highestSequence);
        ArgumentNullException.ThrowIfNull(completedMessageDigests);
        ArgumentNullException.ThrowIfNull(completedObservationDigests);
        if (highestContiguousSequence != 0 ||
            messageDigests.Count != 0 ||
            observationDigests.Count != 0 ||
            pending.Count != 0 ||
            outcomes.Count != 0 ||
            publishedReports.Count != 0)
        {
            throw new InvalidOperationException("o2a.pipeline.restore_not_fresh");
        }

        foreach ((Guid id, string digest) in completedMessageDigests)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(digest))
            {
                throw new InvalidOperationException("o2a.pipeline.restore_invalid");
            }
            messageDigests.Add(id, digest);
        }
        foreach ((Guid id, string digest) in completedObservationDigests)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(digest))
            {
                throw new InvalidOperationException("o2a.pipeline.restore_invalid");
            }
            observationDigests.Add(id, digest);
        }
        highestContiguousSequence = highestSequence;
    }

    /// <summary>Creates one bootstrapped pipeline below an exact caller-owned temporary sandbox root.</summary>
    /// <param name="rootPath">Canonical O2-A sandbox root.</param>
    /// <param name="agentId">Synthetic Agent identity.</param>
    /// <param name="instanceId">Synthetic instance identity.</param>
    /// <param name="timeProvider">Deterministic clock.</param>
    /// <param name="cancellationToken">Cancellation observed during bootstrap.</param>
    /// <returns>An initialised pipeline whose O1 context is active at revision one.</returns>
    internal static async Task<O2ACanonicalObservationPipeline> CreateAsync(
        string rootPath,
        Guid agentId,
        Guid instanceId,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(instanceId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(timeProvider);

        O1SyntheticTrustFixture fixture = new();
        try
        {
            O1SandboxStore store = new(Path.Combine(Path.GetFullPath(rootPath), "trust"));
            O1TrustCoordinator coordinator = new(store, fixture.Trust, () => timeProvider.GetUtcNow());
            O1TrustBundle bundle = fixture.Bootstrap();
            O1TrustAdmissionResult bootstrap = await coordinator.AdmitAsync(
                bundle,
                O1SyntheticTrustFixture.Scope,
                timeProvider.GetUtcNow().AddMinutes(1),
                "o2a-bootstrap",
                cancellationToken: cancellationToken);
            if (!bootstrap.Accepted || bootstrap.State.Quarantined)
            {
                throw new InvalidOperationException("o2a.pipeline.trust_bootstrap_failed");
            }

            Guid scopeId = Guid.Parse("a2a00000-0000-4000-8000-000000000001");
            return new O2ACanonicalObservationPipeline(
                store,
                fixture,
                coordinator,
                new O1ResourceCoordinator(bundle.Payload.Resources, () => timeProvider.GetUtcNow()),
                new O2ASyntheticPolicyAuthority(agentId, instanceId, scopeId, timeProvider.GetUtcNow()),
                timeProvider,
                agentId,
                instanceId,
                scopeId);
        }
        catch
        {
            fixture.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Applies idempotency and contiguous-sequence admission, then processes only newly contiguous canonical envelopes.
    /// </summary>
    /// <param name="message">Server-validated production observation message.</param>
    /// <param name="receivedAt">Authoritative Server receipt instant.</param>
    /// <param name="cancellationToken">Cancellation propagated through trust, resources and analysis.</param>
    /// <returns>Accepted, exact duplicate or stable fail-closed conflict.</returns>
    public async ValueTask<ObservationItemResult> IngestAsync(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(message.ErrorCode) ||
                !string.IsNullOrWhiteSpace(message.SafeErrorMessage))
            {
                return Rejected(message.MessageId, "o2a.canonical.error_contract_unsupported");
            }

            O1CheckpointState context = await trustStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!context.Initialised || context.Quarantined)
            {
                return Rejected(message.MessageId, "o2a.trust_context_inactive");
            }

            string digest = O1CanonicalCryptography.Digest(message);
            O2ACanonicalObservationEnvelope envelope = CreateEnvelope(
                message,
                receivedAt,
                context,
                digest,
                AuthorisationScopeId);
            if (messageDigests.TryGetValue(message.MessageId, out string? existingMessageDigest))
            {
                return string.Equals(existingMessageDigest, digest, StringComparison.Ordinal)
                    ? Duplicate(message.MessageId)
                    : Rejected(message.MessageId, "o2a.idempotency_conflict");
            }
            if (observationDigests.TryGetValue(message.ObservationId, out string? existingObservationDigest))
            {
                return string.Equals(existingObservationDigest, digest, StringComparison.Ordinal)
                    ? Duplicate(message.MessageId)
                    : Rejected(message.MessageId, "o2a.observation_conflict");
            }
            if (message.Sequence <= highestContiguousSequence || pending.ContainsKey(message.Sequence))
            {
                return Rejected(message.MessageId, "o2a.sequence_conflict");
            }

            PendingObservation candidate = new(envelope, digest);
            pending.Add(message.Sequence, candidate);
            messageDigests.Add(message.MessageId, digest);
            observationDigests.Add(message.ObservationId, digest);
            try
            {
                await ProcessContiguousAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (pending.Remove(message.Sequence))
                {
                    messageDigests.Remove(message.MessageId);
                    observationDigests.Remove(message.ObservationId);
                }
                throw;
            }

            return Accepted(message.MessageId);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Returns the highest sequence fully consumed by the joined synthetic pipeline.</summary>
    /// <param name="agentId">Exact Agent stream.</param>
    /// <param name="cancellationToken">Cancellation checked before reading state.</param>
    /// <returns>Zero before the first contiguous item or the current positive sequence.</returns>
    public ValueTask<long> GetHighestContiguousSequenceAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(agentId == AgentId ? highestContiguousSequence : 0L);
    }

    /// <summary>Advances O1 trust through its reserved control lane so in-flight publication becomes stale.</summary>
    /// <param name="cancellationToken">Cancellation propagated through the atomic trust update.</param>
    internal async Task AdvanceTrustAsync(CancellationToken cancellationToken = default)
    {
        using O1ControlLease control = resourceCoordinator.TryAdmitControl(
                1,
                timeProvider.GetUtcNow().AddMinutes(1),
                cancellationToken)
            ?? throw new InvalidOperationException("o2a.pipeline.control_capacity_unavailable");
        O1CheckpointState current = await trustStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        O1TrustAdmissionResult advanced = await trustCoordinator.AdmitAsync(
            trustFixture.Successor(current),
            O1SyntheticTrustFixture.Scope,
            timeProvider.GetUtcNow().AddMinutes(1),
            "o2a-context-advance",
            cancellationToken: cancellationToken);
        if (!advanced.Accepted || advanced.State.Quarantined)
        {
            throw new InvalidOperationException("o2a.pipeline.context_advance_failed");
        }
    }

    /// <summary>
    /// Advances a fresh synthetic O1 session to the exact durable context revision retained by O2-B.
    /// </summary>
    /// <param name="contextRevision">Canonical <c>o2a-context-N</c> revision.</param>
    /// <param name="cancellationToken">Cancellation propagated through each monotonic successor admission.</param>
    /// <exception cref="InvalidOperationException">Thrown for a malformed, regressive or unbounded revision.</exception>
    internal async Task AlignDurableContextAsync(
        string contextRevision,
        CancellationToken cancellationToken = default)
    {
        const string prefix = "o2a-context-";
        if (!contextRevision.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(
                contextRevision.AsSpan(prefix.Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out long target) ||
            target < 1 ||
            target > 1024)
        {
            throw new InvalidOperationException("o2a.pipeline.context_restore_invalid");
        }
        O1CheckpointState current = await trustStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (current.ContextRevision > target)
        {
            throw new InvalidOperationException("o2a.pipeline.context_restore_regressive");
        }
        while (current.ContextRevision < target)
        {
            await AdvanceTrustAsync(cancellationToken).ConfigureAwait(false);
            current = await trustStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Returns the exact current O1 context identifier without exposing checkpoint content.</summary>
    /// <param name="cancellationToken">Cancellation checked during the bounded local load.</param>
    /// <returns>The canonical monotonic context identifier.</returns>
    internal async Task<string> GetCurrentContextRevisionAsync(CancellationToken cancellationToken = default)
    {
        O1CheckpointState current = await trustStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        return ContextIdentifier(current.ContextRevision);
    }

    /// <summary>Revokes the exact synthetic MOD-12 data-use grant for all subsequent adaptations.</summary>
    internal void RevokePolicy() => policyAuthority.Revoke();

    /// <summary>Processes every now-contiguous item exactly once and leaves later gaps pending without analysis.</summary>
    private async Task ProcessContiguousAsync(CancellationToken cancellationToken)
    {
        long nextSequence = checked(highestContiguousSequence + 1);
        while (pending.TryGetValue(nextSequence, out PendingObservation? next))
        {
            await ProcessOneAsync(next.Envelope, cancellationToken).ConfigureAwait(false);
            pending.Remove(nextSequence);
            highestContiguousSequence = next.Envelope.Sequence;
            nextSequence = checked(highestContiguousSequence + 1);
        }
    }

    /// <summary>Runs one canonical item through resources, policy adaptation and complete-only MOD-12 publication.</summary>
    private async Task ProcessOneAsync(
        O2ACanonicalObservationEnvelope envelope,
        CancellationToken cancellationToken)
    {
        byte[] encoded = JsonSerializer.SerializeToUtf8Bytes(envelope);
        O1ResourceRequest request = NextResourceRequest ?? ResourceRequest(encoded.Length);
        NextResourceRequest = null;
        DateTimeOffset deadline = NextDeadlineUtc ??
            timeProvider.GetUtcNow().AddSeconds(10);
        NextDeadlineUtc = null;

        O1ResourceAdmission admission = resourceCoordinator.TryAdmit(
            request,
            deadline,
            cancellationToken);
        string envelopeDigest = O1CanonicalCryptography.Digest(envelope);
        if (!admission.Accepted)
        {
            outcomes.Add(
                new O2AProcessingOutcome(
                    envelope.Sequence,
                    admission.Code,
                    false,
                    envelopeDigest,
                    envelope.ContextRevision,
                    null));
            return;
        }

        using O1ResourceLease lease = admission.Lease!;
        try
        {
            HealthObservation observation = ToHealthObservation(envelope);
            O2APolicyEvidence policy = policyAuthority.Current(timeProvider.GetUtcNow());
            ObserverTelemetryAdaptationResult adapted = CanonicalObserverTelemetryAdapter.Adapt(
                new ObserverCanonicalHealthTelemetry(observation, envelope.ReceivedAtUtc),
                policy.Context,
                policy.Trust,
                timeProvider.GetUtcNow());
            if (adapted.Disposition != ObserverTelemetryAdaptationDisposition.Accepted)
            {
                outcomes.Add(
                    new O2AProcessingOutcome(
                        envelope.Sequence,
                        adapted.Code,
                        false,
                        envelopeDigest,
                        envelope.ContextRevision,
                        null));
                return;
            }

            ObserverMetricSample sample = adapted.Sample!;
            ObserverAnalysisRequest analysisRequest = new(
                envelope.ObservationId,
                envelope.InstanceId,
                AuthorisationScopeId,
                envelope.ReceivedAtUtc,
                [sample]);
            Func<CancellationToken, Task>? beforePublication = BeforePublicationAsync;
            BeforePublicationAsync = null;
            if (beforePublication is not null)
            {
                await beforePublication(cancellationToken).ConfigureAwait(false);
            }

            O1CheckpointState publicationContext = await trustStore
                .LoadAsync(cancellationToken)
                .ConfigureAwait(false);
            ObserverAnalysisReport report = ObserverAnalysisService.Analyse(
                analysisRequest,
                AnalysisPolicy(sample.MetricKey),
                new ObserverAnalysisExecutionContext(
                    deadline,
                    envelope.ContextRevision,
                    ContextIdentifier(publicationContext.ContextRevision),
                    timeProvider),
                cancellationToken);
            bool publish = report.IsComplete && !report.IsAuthorising;
            outcomes.Add(
                new O2AProcessingOutcome(
                    envelope.Sequence,
                    report.Code,
                    publish,
                    envelopeDigest,
                    envelope.ContextRevision,
                    report));
            if (publish)
            {
                publishedReports.Add(report);
            }
        }
        finally
        {
            lease.MarkQuiescent();
        }
    }

    /// <summary>Builds the immutable O2-A contract only after Server and O1 admission have accepted the source.</summary>
    private static O2ACanonicalObservationEnvelope CreateEnvelope(
        ObservationSyncMessage message,
        DateTimeOffset receivedAt,
        O1CheckpointState context,
        string sourceDigest,
        Guid authorisationScopeId) =>
        new(
            ContractVersion,
            SourceCategory,
            message.MessageId,
            message.Sequence,
            message.ObservationId,
            message.InstanceId,
            message.AgentId,
            authorisationScopeId,
            "synthetic-test",
            message.ProviderType,
            message.ProviderVersion,
            message.Status,
            message.Method,
            message.EvidenceLevel,
            message.AttemptCount,
            message.ObservedAt,
            receivedAt,
            message.DurationMilliseconds,
            Array.AsReadOnly(message.Limitations.ToArray()),
            "health-duration.v1",
            CanonicalObserverTelemetryAdapter.MetricKeyPrefix,
            "milliseconds",
            "o1-resource-envelope.v1",
            1,
            1,
            Array.Empty<string>(),
            sourceDigest,
            ContextIdentifier(context.ContextRevision),
            ObserverDataClassification.OperationalTelemetry,
            ObserverRedactionStatus.Sanitised,
            ObserverRetentionClass.EphemeralAnalysisOnly,
            ObserverPermittedPurpose.NonMutatingObserverAnalysis);

    /// <summary>Converts only fields already validated by the production Server ingestion boundary.</summary>
    private static HealthObservation ToHealthObservation(O2ACanonicalObservationEnvelope envelope)
    {
        if (!string.IsNullOrWhiteSpace(envelope.ProviderType) &&
            !ProviderType.TryParse(envelope.ProviderType, out _))
        {
            throw new InvalidOperationException("o2a.pipeline.provider_invalid");
        }
        return new HealthObservation(
            envelope.ObservationId,
            envelope.InstanceId,
            envelope.AgentId,
            ProviderType.Parse(envelope.ProviderType),
            envelope.ProviderVersion,
            Enum.Parse<HealthStatus>(envelope.Status, ignoreCase: false),
            envelope.Method,
            envelope.ObservedAtUtc,
            TimeSpan.FromMilliseconds(envelope.DurationMilliseconds),
            new ObservationQuality(
                Enum.Parse<EvidenceLevel>(envelope.EvidenceLevel, ignoreCase: false),
                envelope.AttemptCount,
                envelope.Limitations));
    }

    /// <summary>Builds one bounded threshold that also exposes stale evidence as insufficient rather than healthy.</summary>
    private static ObserverAnalysisPolicy AnalysisPolicy(string metricKey) =>
        new(
            [
                new ObserverThresholdRule(
                    "o2a.duration-threshold",
                    "1.0.0",
                    metricKey,
                    CanonicalObserverTelemetryAdapter.Unit,
                    ObserverThresholdComparison.GreaterThanOrEqual,
                    50,
                    ObserverFindingSeverity.Warning,
                    1,
                    TimeSpan.FromHours(2),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(10)),
            ],
            []);

    /// <summary>Derives a conservative deterministic demand from the final encoded envelope size.</summary>
    private static O1ResourceRequest ResourceRequest(int encodedBytes) =>
        new(
            encodedBytes,
            4,
            1,
            checked(encodedBytes * 3L + 4096),
            checked(encodedBytes + 100L),
            1,
            4096);

    /// <summary>Returns the stable revision identifier accepted by the Application contract guard.</summary>
    private static string ContextIdentifier(long revision) => $"o2a-context-{revision}";

    /// <summary>Creates one stable accepted disposition.</summary>
    private static ObservationItemResult Accepted(Guid messageId) =>
        new(messageId, ObservationIngestionDisposition.Accepted);

    /// <summary>Creates one stable exact-duplicate disposition.</summary>
    private static ObservationItemResult Duplicate(Guid messageId) =>
        new(messageId, ObservationIngestionDisposition.Duplicate);

    /// <summary>Creates one sanitised terminal refusal.</summary>
    private static ObservationItemResult Rejected(Guid messageId, string code) =>
        new(messageId, ObservationIngestionDisposition.Rejected, code);

    /// <summary>Disposes synthetic keys and local synchronisation primitives without deleting caller-owned roots.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        policyAuthority.Dispose();
        trustFixture.Dispose();
        gate.Dispose();
    }

    /// <summary>Retains one immutable envelope and digest while a lower sequence gap is outstanding.</summary>
    private sealed record PendingObservation(
        O2ACanonicalObservationEnvelope Envelope,
        string Digest);
}

/// <summary>Implements the production Agent outbox boundary over a bounded synthetic in-memory fixture.</summary>
internal sealed class O2AAgentOutboxStore : IAgentOutboxStore
{
    private readonly SortedDictionary<long, AgentOutboxEnvelope> pending = [];

    /// <summary>Gets the number of envelopes still awaiting a terminal Server classification.</summary>
    internal int PendingCount => pending.Count;

    /// <summary>Adds one unique synthetic envelope before Agent dispatch.</summary>
    /// <param name="message">Canonical production observation message.</param>
    internal void Add(ObservationSyncMessage message)
    {
        if (pending.ContainsKey(message.Sequence))
        {
            throw new InvalidOperationException("o2a.agent.sequence_duplicate");
        }
        pending.Add(
            message.Sequence,
            new AgentOutboxEnvelope(
                message.MessageId,
                message.Sequence,
                "health-observation",
                message.SchemaVersion,
                JsonSerializer.Serialize(message),
                message.ObservedAt,
                0));
    }

    /// <summary>Returns at most the caller's bounded count in Agent sequence order.</summary>
    public ValueTask<IReadOnlyList<AgentOutboxEnvelope>> GetPendingAsync(
        DateTimeOffset now,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<AgentOutboxEnvelope>>(
            pending.Values.Take(maximumCount).ToArray());
    }

    /// <summary>Removes only terminally accepted, duplicated or rejected envelopes from the synthetic outbox.</summary>
    public ValueTask ApplyResultsAsync(
        IReadOnlyList<ObservationItemResult> results,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (ObservationItemResult result in results)
        {
            if (result.Disposition is
                ObservationIngestionDisposition.Accepted or
                ObservationIngestionDisposition.Duplicate or
                ObservationIngestionDisposition.Rejected)
            {
                long? sequence = pending
                    .Where(entry => entry.Value.MessageId == result.MessageId)
                    .Select(entry => (long?)entry.Key)
                    .SingleOrDefault();
                if (sequence.HasValue)
                {
                    pending.Remove(sequence.Value);
                }
            }
        }
        return ValueTask.CompletedTask;
    }
}

/// <summary>Deserialises bounded Agent envelopes and delegates them directly to the production Server batch ingestor.</summary>
internal sealed class O2ALoopbackObservationTransport(
    ObservationBatchIngestor ingestor) : IObservationBatchTransport
{
    private const int MaximumPayloadBytes = 64 * 1024;

    /// <summary>Validates the synthetic Agent transport envelope without network access and invokes the Server boundary.</summary>
    public async ValueTask<ObservationBatchResult> SendAsync(
        Guid agentId,
        IReadOnlyList<AgentOutboxEnvelope> messages,
        CancellationToken cancellationToken)
    {
        List<ObservationSyncMessage> decoded = new(messages.Count);
        List<ObservationItemResult> rejected = [];
        foreach (AgentOutboxEnvelope envelope in messages)
        {
            if (!string.Equals(envelope.MessageType, "health-observation", StringComparison.Ordinal) ||
                envelope.SchemaVersion != 1 ||
                Encoding.UTF8.GetByteCount(envelope.PayloadJson) > MaximumPayloadBytes)
            {
                rejected.Add(new ObservationItemResult(
                    envelope.MessageId,
                    ObservationIngestionDisposition.Rejected,
                    "o2a.transport.envelope_invalid"));
                continue;
            }

            try
            {
                ObservationSyncMessage? message = JsonSerializer.Deserialize<ObservationSyncMessage>(
                    envelope.PayloadJson);
                if (message is null ||
                    message.MessageId != envelope.MessageId ||
                    message.Sequence != envelope.Sequence)
                {
                    rejected.Add(new ObservationItemResult(
                        envelope.MessageId,
                        ObservationIngestionDisposition.Rejected,
                        "o2a.transport.payload_mismatch"));
                }
                else
                {
                    decoded.Add(message);
                }
            }
            catch (JsonException)
            {
                rejected.Add(new ObservationItemResult(
                    envelope.MessageId,
                    ObservationIngestionDisposition.Rejected,
                    "o2a.transport.payload_invalid"));
            }
        }

        if (decoded.Count == 0)
        {
            return new ObservationBatchResult(rejected.AsReadOnly(), 0);
        }

        ObservationBatchResult accepted = await ingestor
            .HandleAsync(new ObservationBatchRequest(agentId, decoded), cancellationToken)
            .ConfigureAwait(false);
        return new ObservationBatchResult(
            rejected.Concat(accepted.Items).ToArray(),
            accepted.HighestContiguousSequence);
    }
}

/// <summary>Creates deterministic provider-neutral messages for O2-A tests and the exact process self-check.</summary>
internal static class O2ASyntheticObservation
{
    /// <summary>Builds one bounded, canonical synthetic observation message.</summary>
    /// <param name="agentId">Exact synthetic Agent.</param>
    /// <param name="instanceId">Exact synthetic instance.</param>
    /// <param name="sequence">Positive Agent stream sequence.</param>
    /// <param name="observedAtUtc">Explicit UTC observation instant.</param>
    /// <param name="status">Canonical health status.</param>
    /// <param name="durationMilliseconds">Bounded non-negative duration.</param>
    /// <param name="messageId">Optional stable message identifier.</param>
    /// <param name="observationId">Optional stable observation identifier.</param>
    /// <param name="schemaVersion">Protocol schema version, normally one.</param>
    /// <returns>One production observation synchronisation message containing only synthetic fixture values.</returns>
    internal static ObservationSyncMessage Create(
        Guid agentId,
        Guid instanceId,
        long sequence,
        DateTimeOffset observedAtUtc,
        HealthStatus status = HealthStatus.Degraded,
        long durationMilliseconds = 100,
        Guid? messageId = null,
        Guid? observationId = null,
        int schemaVersion = 1) =>
        new(
            messageId ?? Guid.NewGuid(),
            schemaVersion,
            sequence,
            observationId ?? Guid.NewGuid(),
            instanceId,
            agentId,
            "synthetic-db",
            "1.0.0",
            status.ToString(),
            "o2a-fixture",
            EvidenceLevel.ProviderAuthenticated.ToString(),
            1,
            observedAtUtc,
            durationMilliseconds,
            null,
            null,
            Array.Empty<string>());
}

/// <summary>Exposes one bounded reference run only through the exact O2-A marker in the existing test-only host.</summary>
public static class O2ASandboxProcess
{
    private const string ActivationMarker = "o2a-canonical-observation-pipeline-sandbox";

    /// <summary>Runs one Agent-to-MOD-12 self-check below an exact temporary root and emits aggregate evidence only.</summary>
    /// <param name="args">Exact activation, operation and root arguments.</param>
    /// <returns>Zero for a complete non-authorising publication, two for invalid activation or three for failure.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryReadOptions(args, out string? root))
        {
            Console.Error.WriteLine("o2a_sandbox.failed:activation_invalid");
            return 2;
        }

        Guid agentId = Guid.Parse("a2a10000-0000-4000-8000-000000000001");
        Guid instanceId = Guid.Parse("a2a20000-0000-4000-8000-000000000001");
        O2AFixedTimeProvider clock = new(O1SyntheticTrustFixture.NowUtc);
        try
        {
            using O2ACanonicalObservationPipeline pipeline =
                await O2ACanonicalObservationPipeline.CreateAsync(root!, agentId, instanceId, clock);
            ObservationBatchIngestor server = new(pipeline, clock);
            O2AAgentOutboxStore outbox = new();
            outbox.Add(O2ASyntheticObservation.Create(agentId, instanceId, 1, clock.GetUtcNow()));
            AgentOutboxDispatchResult dispatch = await new AgentOutboxDispatchRunner(
                    agentId,
                    outbox,
                    new O2ALoopbackObservationTransport(server),
                    clock)
                .RunOnceAsync(10);
            ObserverAnalysisReport? report = pipeline.PublishedReports.SingleOrDefault();
            if (dispatch.AcknowledgedCount != 1 ||
                outbox.PendingCount != 0 ||
                report is null ||
                !report.IsComplete ||
                report.IsAuthorising ||
                report.Capabilities.ActivationState != ObserverActivationState.None)
            {
                Console.Error.WriteLine("o2a_sandbox.failed:reference_incomplete");
                return 3;
            }

            Console.WriteLine(
                JsonSerializer.Serialize(
                    new
                    {
                        accepted = dispatch.AcknowledgedCount,
                        published = pipeline.PublishedReports.Count,
                        activationState = report.Capabilities.ActivationState.ToString(),
                        authorising = report.IsAuthorising,
                    }));
            return 0;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"o2a_sandbox.failed:{exception.GetType().Name}");
            return 3;
        }
    }

    /// <summary>Accepts only the exact marker, reference operation and O2-A-owned temporary root.</summary>
    private static bool TryReadOptions(string[] args, out string? root)
    {
        root = null;
        if (args.Length != 6 ||
            args[0] != "--activation" ||
            args[1] != ActivationMarker ||
            args[2] != "--operation" ||
            args[3] != "reference" ||
            args[4] != "--root")
        {
            return false;
        }

        try
        {
            string candidate = Path.GetFullPath(args[5]);
            string temporary = Path.GetFullPath(Path.GetTempPath());
            if (!candidate.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(candidate).StartsWith("DBNotifier-O2A-", StringComparison.Ordinal))
            {
                return false;
            }
            root = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            NotSupportedException or
            PathTooLongException)
        {
            return false;
        }
    }
}

/// <summary>Provides one immutable UTC instant to every O2-A trust, Server and MOD-12 decision.</summary>
internal sealed class O2AFixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    /// <summary>Returns the deterministic UTC fixture instant.</summary>
    public override DateTimeOffset GetUtcNow() => utcNow;
}
