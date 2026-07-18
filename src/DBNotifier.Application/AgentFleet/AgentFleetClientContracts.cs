// Module purpose: Defines fail-closed Agent-side identity, heartbeat, assignment-reconciliation and persistence ports without enabling runtime execution.
namespace DBNotifier.Application.AgentFleet;

/// <summary>Identifies the local Agent Fleet identity and reconciliation state without inferring instance health.</summary>
public enum AgentLocalIdentityState
{
    /// <summary>No locally enrolled identity exists.</summary>
    NotEnrolled,

    /// <summary>The local identity is valid for bounded Agent Fleet communication.</summary>
    Active,

    /// <summary>A transient transport failure prevents current communication.</summary>
    Offline,

    /// <summary>The last-known-valid assignment snapshot exceeded its freshness policy.</summary>
    Stale,

    /// <summary>The remote protocol or schema is incompatible.</summary>
    Incompatible,

    /// <summary>The local certificate is outside its validity window.</summary>
    Expired,

    /// <summary>The authenticated identity was revoked or denied.</summary>
    RevokedOrDenied,

    /// <summary>Local identity, configuration or durable evidence conflicts.</summary>
    Conflict,
}

/// <summary>Carries one test-only enrollment bootstrap entirely in memory.</summary>
/// <param name="Token">One-time token supplied only by an E2E fixture.</param>
/// <param name="InstallationId">Stable test installation identifier.</param>
/// <param name="DisplayName">Bounded display name.</param>
/// <param name="Environment">Exact test environment.</param>
/// <param name="Platform">Exact test platform.</param>
/// <param name="AgentVersion">Bounded Agent version.</param>
/// <param name="RequestedScope">Exact token-bound scope.</param>
public sealed record AgentEnrollmentBootstrap(
    string Token,
    string InstallationId,
    string DisplayName,
    string Environment,
    string Platform,
    string AgentVersion,
    string RequestedScope);

/// <summary>Returns public enrollment material while the private key remains inside its identity-store boundary.</summary>
/// <param name="OperationId">Opaque handle for completing or aborting this enrollment attempt.</param>
/// <param name="CertificateSigningRequest">Base64 PKCS#10 DER containing public material only.</param>
public sealed record AgentEnrollmentKeyMaterial(Guid OperationId, string CertificateSigningRequest);

/// <summary>Describes a completed identity-store operation without exposing certificate or private-key bytes.</summary>
/// <param name="IdentityReference">Opaque reference used for authenticated Agent requests.</param>
/// <param name="CertificateThumbprint">Canonical public certificate thumbprint.</param>
public sealed record AgentIdentityCompletion(string IdentityReference, string CertificateThumbprint);

/// <summary>Persists only non-secret local identity metadata.</summary>
/// <param name="AgentId">Server-issued Agent identifier.</param>
/// <param name="InstallationId">Exact local installation identifier.</param>
/// <param name="Environment">Exact enrolled environment.</param>
/// <param name="IdentityReference">Opaque reference to private identity material outside SQLite.</param>
/// <param name="CertificateThumbprint">Canonical public certificate thumbprint.</param>
/// <param name="CertificateNotAfter">Certificate expiry instant.</param>
/// <param name="EnrolledAt">Trusted local enrollment completion instant.</param>
/// <param name="State">Current fail-closed identity state.</param>
/// <param name="ActiveConfigurationVersion">Last-known-valid assignment version, when present.</param>
public sealed record AgentLocalRegistration(
    Guid AgentId,
    string InstallationId,
    string Environment,
    string IdentityReference,
    string CertificateThumbprint,
    DateTimeOffset CertificateNotAfter,
    DateTimeOffset EnrolledAt,
    AgentLocalIdentityState State,
    string? ActiveConfigurationVersion);

/// <summary>Provides bounded queue facts for heartbeat reporting.</summary>
/// <param name="Depth">Number of unacknowledged local observation messages.</param>
/// <param name="OldestQueuedAt">Oldest queued instant when depth is non-zero.</param>
public sealed record AgentHeartbeatQueueEvidence(long Depth, DateTimeOffset? OldestQueuedAt);

/// <summary>Returns one durable pending heartbeat and whether it was newly created.</summary>
/// <param name="Request">Exact request that must be replayed until terminal acknowledgement.</param>
/// <param name="Created">Whether this call created rather than reloaded the pending envelope.</param>
public sealed record PendingAgentHeartbeat(AgentHeartbeatRequest Request, bool Created);

/// <summary>Describes the locally active assignment reconciliation evidence.</summary>
/// <param name="Version">Last-known-valid content version.</param>
/// <param name="EntityTag">Strong ETag corresponding to the version.</param>
/// <param name="GeneratedAt">Trusted server generation instant.</param>
/// <param name="LastSucceededAt">Last local successful reconciliation instant.</param>
/// <param name="LastErrorCode">Most recent sanitised failure code.</param>
public sealed record AgentAssignmentLocalState(
    string? Version,
    string? EntityTag,
    DateTimeOffset? GeneratedAt,
    DateTimeOffset? LastSucceededAt,
    string? LastErrorCode);

/// <summary>Classifies transport outcomes before application state changes.</summary>
public enum AgentFleetTransportDisposition
{
    /// <summary>A complete typed response was returned.</summary>
    Succeeded,

    /// <summary>The caller already owns the exact assignment version.</summary>
    NotModified,

    /// <summary>A bounded transient failure may be retried later.</summary>
    TransientFailure,

    /// <summary>The identity is inactive, revoked or otherwise denied.</summary>
    Denied,

    /// <summary>The protocol or schema is incompatible.</summary>
    Incompatible,

    /// <summary>The response was malformed, oversized or inconsistent.</summary>
    InvalidResponse,
}

/// <summary>Wraps a bounded Agent Fleet transport response.</summary>
/// <typeparam name="T">Typed response payload.</typeparam>
/// <param name="Disposition">Transport classification.</param>
/// <param name="Value">Typed payload on success.</param>
/// <param name="EntityTag">Strong response ETag when supplied.</param>
/// <param name="ErrorCode">Sanitised stable failure code.</param>
public sealed record AgentFleetTransportResult<T>(
    AgentFleetTransportDisposition Disposition,
    T? Value,
    string? EntityTag,
    string? ErrorCode,
    TimeSpan? RetryAfter = null);

/// <summary>Identifies the only Agent Fleet operations admitted by the sandbox resilience lease.</summary>
public enum AgentFleetOperationKind
{
    /// <summary>One durable heartbeat delivery or exact replay.</summary>
    Heartbeat,

    /// <summary>One read-only assignment reconciliation.</summary>
    AssignmentReconciliation,
}

/// <summary>Fences one bounded Agent Fleet operation across local processes.</summary>
/// <param name="AgentId">Exact local Agent registration.</param>
/// <param name="OwnerId">Bounded sandbox-process owner identifier.</param>
/// <param name="Kind">Operation protected by the lease.</param>
/// <param name="FenceToken">Strictly increasing token that invalidates earlier owners.</param>
/// <param name="ExpiresAt">UTC instant after which another owner may acquire the lease.</param>
public sealed record AgentFleetOperationLease(
    Guid AgentId,
    string OwnerId,
    AgentFleetOperationKind Kind,
    long FenceToken,
    DateTimeOffset ExpiresAt);

/// <summary>Creates and retains private enrollment identity material outside ordinary Agent persistence.</summary>
public interface IAgentEnrollmentIdentityStore
{
    /// <summary>Creates one P-256 private key and its public CSR.</summary>
    /// <param name="installationId">Exact installation identifier bound into the CSR subject.</param>
    /// <param name="cancellationToken">Cancellation for local key generation.</param>
    /// <returns>Opaque operation handle and public CSR.</returns>
    ValueTask<AgentEnrollmentKeyMaterial> BeginAsync(
        string installationId,
        CancellationToken cancellationToken);

    /// <summary>Validates and binds the issued public certificate to the pending private key.</summary>
    /// <param name="operationId">Pending operation handle.</param>
    /// <param name="certificateDer">Issued public certificate DER.</param>
    /// <param name="cancellationToken">Cancellation for local completion.</param>
    /// <returns>An opaque identity reference and public thumbprint.</returns>
    ValueTask<AgentIdentityCompletion> CompleteAsync(
        Guid operationId,
        ReadOnlyMemory<byte> certificateDer,
        CancellationToken cancellationToken);

    /// <summary>Destroys pending private material after a failed or cancelled attempt.</summary>
    /// <param name="operationId">Pending operation handle.</param>
    /// <param name="cancellationToken">Cancellation for local cleanup.</param>
    ValueTask AbortAsync(Guid operationId, CancellationToken cancellationToken);

    /// <summary>Removes a completed identity when its metadata could not be committed safely.</summary>
    /// <param name="identityReference">Opaque completed identity reference.</param>
    /// <param name="cancellationToken">Cancellation for local cleanup.</param>
    ValueTask RemoveAsync(string identityReference, CancellationToken cancellationToken);
}

/// <summary>Transports only the version-one Agent Fleet operations authorised for this increment.</summary>
public interface IAgentFleetClientTransport
{
    /// <summary>Sends one test-only enrollment request.</summary>
    /// <param name="token">In-memory one-time test token.</param>
    /// <param name="request">Public enrollment request.</param>
    /// <param name="cancellationToken">Cancellation for the bounded request.</param>
    /// <returns>Typed enrollment transport result.</returns>
    ValueTask<AgentFleetTransportResult<AgentEnrollmentOutcome>> EnrolAsync(
        string token,
        AgentEnrollmentRequest request,
        CancellationToken cancellationToken);

    /// <summary>Sends one exact heartbeat with the referenced mTLS identity.</summary>
    /// <param name="identityReference">Opaque reference used to select the mTLS identity.</param>
    /// <param name="request">Exact durable heartbeat envelope.</param>
    /// <param name="cancellationToken">Cancellation for the bounded request.</param>
    /// <returns>Typed heartbeat transport result.</returns>
    ValueTask<AgentFleetTransportResult<AgentHeartbeatOutcome>> SendHeartbeatAsync(
        string identityReference,
        AgentHeartbeatRequest request,
        CancellationToken cancellationToken);

    /// <summary>Reads one complete assignment snapshot or an exact not-modified result.</summary>
    /// <param name="identityReference">Opaque reference used to select the mTLS identity.</param>
    /// <param name="agentId">Exact enrolled Agent identifier.</param>
    /// <param name="agentVersion">Bounded Agent version for negotiation.</param>
    /// <param name="currentVersion">Current last-known-valid version, when present.</param>
    /// <param name="cancellationToken">Cancellation for the bounded request.</param>
    /// <returns>Typed assignment transport result.</returns>
    ValueTask<AgentFleetTransportResult<AgentAssignmentSnapshot>> GetAssignmentsAsync(
        string identityReference,
        Guid agentId,
        string agentVersion,
        string? currentVersion,
        CancellationToken cancellationToken);
}

/// <summary>Owns Agent-local identity, heartbeat and assignment reconciliation evidence in SQLite.</summary>
public interface IAgentFleetLocalStore
{
    /// <summary>Returns the sole local registration, or <see langword="null"/>.</summary>
    /// <param name="cancellationToken">Cancellation for the local read.</param>
    /// <returns>The sole non-secret registration, or <see langword="null"/>.</returns>
    ValueTask<AgentLocalRegistration?> GetRegistrationAsync(CancellationToken cancellationToken);

    /// <summary>Commits one completed enrollment without replacing an existing identity.</summary>
    /// <param name="registration">Validated non-secret identity metadata.</param>
    /// <param name="cancellationToken">Cancellation for the atomic commit.</param>
    ValueTask SaveEnrollmentAsync(AgentLocalRegistration registration, CancellationToken cancellationToken);

    /// <summary>Attempts to acquire one expiring, monotonically fenced local operation lease.</summary>
    /// <param name="agentId">Exact local Agent identifier.</param>
    /// <param name="ownerId">Bounded sandbox-process owner identifier.</param>
    /// <param name="kind">Operation that the lease protects.</param>
    /// <param name="now">Trusted UTC acquisition instant.</param>
    /// <param name="duration">Bounded lease duration.</param>
    /// <param name="cancellationToken">Cancellation for the serialisable acquisition.</param>
    /// <returns>The acquired lease, or <see langword="null"/> while another unexpired owner holds it.</returns>
    ValueTask<AgentFleetOperationLease?> TryAcquireOperationLeaseAsync(
        Guid agentId,
        string ownerId,
        AgentFleetOperationKind kind,
        DateTimeOffset now,
        TimeSpan duration,
        CancellationToken cancellationToken);

    /// <summary>Releases only the exact lease still owned by the caller.</summary>
    /// <param name="lease">Exact owner and fence token previously acquired.</param>
    /// <param name="cancellationToken">Cancellation for the bounded release.</param>
    ValueTask ReleaseOperationLeaseAsync(
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Returns bounded outbox facts without loading payloads.</summary>
    /// <param name="cancellationToken">Cancellation for the aggregate read.</param>
    /// <returns>Bounded queue depth and age evidence.</returns>
    ValueTask<AgentHeartbeatQueueEvidence> GetQueueEvidenceAsync(CancellationToken cancellationToken);

    /// <summary>Creates or reloads the exact pending heartbeat envelope.</summary>
    /// <param name="registration">Current local Agent registration.</param>
    /// <param name="agentVersion">Bounded Agent version.</param>
    /// <param name="queueEvidence">Current bounded queue evidence.</param>
    /// <param name="now">Trusted UTC creation instant.</param>
    /// <param name="cancellationToken">Cancellation for the atomic operation.</param>
    /// <returns>The exact new or previously pending heartbeat.</returns>
    ValueTask<PendingAgentHeartbeat> GetOrCreatePendingHeartbeatAsync(
        AgentLocalRegistration registration,
        string agentVersion,
        AgentHeartbeatQueueEvidence queueEvidence,
        DateTimeOffset now,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Commits a compatible heartbeat receipt and clears only its exact pending envelope.</summary>
    /// <param name="request">Exact pending request.</param>
    /// <param name="outcome">Compatible durable Server receipt.</param>
    /// <param name="cancellationToken">Cancellation for the atomic commit.</param>
    ValueTask AcknowledgeHeartbeatAsync(
        AgentHeartbeatRequest request,
        AgentHeartbeatOutcome outcome,
        DateTimeOffset acknowledgedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Changes the identity state while preserving durable evidence.</summary>
    /// <param name="agentId">Exact local Agent identifier.</param>
    /// <param name="state">Fail-closed state to persist.</param>
    /// <param name="errorCode">Sanitised stable reason.</param>
    /// <param name="now">Trusted UTC attempt instant.</param>
    /// <param name="cancellationToken">Cancellation for the state update.</param>
    ValueTask SetIdentityStateAsync(
        Guid agentId,
        AgentLocalIdentityState state,
        string errorCode,
        DateTimeOffset now,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Returns last-known-valid assignment reconciliation evidence.</summary>
    /// <param name="agentId">Exact local Agent identifier.</param>
    /// <param name="cancellationToken">Cancellation for the local read.</param>
    /// <returns>Version, ETag, freshness and failure evidence.</returns>
    ValueTask<AgentAssignmentLocalState> GetAssignmentStateAsync(
        Guid agentId,
        CancellationToken cancellationToken);

    /// <summary>Atomically replaces all local assignments and advances the active version.</summary>
    /// <param name="snapshot">Complete validated assignment snapshot.</param>
    /// <param name="entityTag">Strong ETag matching the snapshot version.</param>
    /// <param name="appliedAt">Trusted UTC application instant.</param>
    /// <param name="cancellationToken">Cancellation for the atomic replacement.</param>
    ValueTask ApplyAssignmentsAsync(
        AgentAssignmentSnapshot snapshot,
        string entityTag,
        DateTimeOffset appliedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Records a successful not-modified reconciliation without changing the snapshot.</summary>
    /// <param name="agentId">Exact local Agent identifier.</param>
    /// <param name="version">Current last-known-valid version.</param>
    /// <param name="entityTag">Strong ETag matching the version.</param>
    /// <param name="checkedAt">Trusted UTC check instant.</param>
    /// <param name="cancellationToken">Cancellation for the evidence update.</param>
    ValueTask RecordAssignmentsNotModifiedAsync(
        Guid agentId,
        string version,
        string entityTag,
        DateTimeOffset checkedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);

    /// <summary>Records a sanitised reconciliation failure while preserving the active snapshot.</summary>
    /// <param name="agentId">Exact local Agent identifier.</param>
    /// <param name="state">Fail-closed state after the failure.</param>
    /// <param name="errorCode">Sanitised stable reason.</param>
    /// <param name="failedAt">Trusted UTC failure instant.</param>
    /// <param name="cancellationToken">Cancellation for the evidence update.</param>
    ValueTask RecordAssignmentFailureAsync(
        Guid agentId,
        AgentLocalIdentityState state,
        string errorCode,
        DateTimeOffset failedAt,
        AgentFleetOperationLease lease,
        CancellationToken cancellationToken);
}

/// <summary>Summarises one Agent-side operation without exposing payload or identity material.</summary>
/// <param name="Succeeded">Whether the operation reached its safe success condition.</param>
/// <param name="State">Resulting local identity/reconciliation state.</param>
/// <param name="Code">Stable sanitised result code.</param>
public sealed record AgentFleetClientResult(
    bool Succeeded,
    AgentLocalIdentityState State,
    string Code,
    bool Retryable = false,
    TimeSpan? RetryAfter = null);
