// Module purpose: Defines versioned, provider-neutral Agent Fleet contracts and persistence ports without enabling provider or command execution.
using System.Text.Json;
using DBNotifier.Application.Access;

namespace DBNotifier.Application.AgentFleet;

/// <summary>Defines the bounded first-generation protocol values used by the local Agent Fleet integration slice.</summary>
public static class AgentFleetProtocol
{
    /// <summary>Current durable HTTP protocol major supported by this slice.</summary>
    public const int CurrentProtocolVersion = 1;

    /// <summary>Current message schema version supported by enrollment and heartbeat requests.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Maximum complete assignment snapshot admitted for one Agent.</summary>
    public const int MaximumAssignments = 5000;

    /// <summary>Maximum aggregate UTF-8 bytes admitted across assignment endpoint and tag documents.</summary>
    public const int MaximumAssignmentPayloadBytes = 4 * 1024 * 1024;

    /// <summary>Maximum complete Agent rows admitted for one human catalogue response.</summary>
    public const int MaximumCatalogueEntries = 5000;

    /// <summary>Maximum RBAC scope rows admitted for one server-side authorisation decision.</summary>
    public const int MaximumAuthorisationScopes = 1024;

    /// <summary>Maximum certificate rows admitted for one resumable revocation-reconciliation transaction.</summary>
    public const int MaximumCertificatesPerAgent = 128;

    /// <summary>Maximum accepted future skew for Agent-produced timestamps.</summary>
    public static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Carries the non-secret enrollment request body. The one-time token is supplied separately through the
/// authorisation header and the private key never leaves the Agent.
/// </summary>
/// <param name="MessageId">Unique request identifier used for bounded audit correlation.</param>
/// <param name="SchemaVersion">Version of this request schema.</param>
/// <param name="InstallationId">Stable installation identifier pre-bound into the one-time token.</param>
/// <param name="DisplayName">Human-readable Agent name.</param>
/// <param name="Environment">Exact environment assigned by the enrollment token.</param>
/// <param name="Platform">Exact platform assigned by the enrollment token.</param>
/// <param name="AgentVersion">Agent product version presented during enrollment.</param>
/// <param name="RequestedScope">Exact scope assigned by the enrollment token.</param>
/// <param name="OccurredAt">UTC instant at which the Agent created the request.</param>
/// <param name="SentAt">UTC instant at which the Agent sent the request.</param>
/// <param name="CertificateSigningRequest">Base64-encoded PKCS#10 DER request containing only public-key material.</param>
public sealed record AgentEnrollmentRequest(
    Guid MessageId,
    int SchemaVersion,
    string InstallationId,
    string DisplayName,
    string Environment,
    string Platform,
    string AgentVersion,
    string RequestedScope,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    string CertificateSigningRequest);

/// <summary>Identifies the result of one enrollment attempt without revealing token validity details.</summary>
public enum AgentEnrollmentDisposition
{
    /// <summary>The Agent and its initial certificate were committed atomically.</summary>
    Enrolled,

    /// <summary>The public request shape or CSR encoding was invalid.</summary>
    Invalid,

    /// <summary>The token, scope or installation was not eligible; external callers receive one generic result.</summary>
    Denied,

    /// <summary>The configured certificate issuer could not issue a certificate.</summary>
    IssuerUnavailable,

    /// <summary>A concurrent or uniqueness conflict prevented enrollment.</summary>
    Conflict,
}

/// <summary>
/// Returns the safe result of an enrollment attempt. Certificate bytes contain public certificate data only and
/// must never be logged or persisted outside the authorised Agent key store.
/// </summary>
/// <param name="Disposition">Typed enrollment result.</param>
/// <param name="AgentId">Enrolled Agent identifier when successful.</param>
/// <param name="CertificateDer">Issued public certificate in DER form when successful.</param>
/// <param name="CertificateNotAfter">Certificate expiry when successful.</param>
/// <param name="ErrorCode">Stable sanitised refusal code when unsuccessful.</param>
public sealed record AgentEnrollmentOutcome(
    AgentEnrollmentDisposition Disposition,
    Guid? AgentId,
    ReadOnlyMemory<byte> CertificateDer,
    DateTimeOffset? CertificateNotAfter,
    string? ErrorCode);

/// <summary>
/// Describes one stored enrollment token challenge without exposing the original high-entropy token value.
/// </summary>
/// <param name="EnrollmentTokenId">Public token identifier used for indexed lookup.</param>
/// <param name="Salt">Per-token random salt used by the fixed enrollment digest.</param>
/// <param name="SecretHash">Persisted digest of the token secret.</param>
/// <param name="HashAlgorithm">Exact digest algorithm identifier.</param>
/// <param name="ExpectedInstallationId">Installation identifier bound at provisioning.</param>
/// <param name="ExpectedEnvironment">Environment bound at provisioning.</param>
/// <param name="ExpectedPlatform">Platform bound at provisioning.</param>
/// <param name="Scope">Authorised enrollment scope.</param>
/// <param name="IssuedAt">Trusted issuance instant.</param>
/// <param name="ExpiresAt">Exclusive expiry instant.</param>
/// <param name="ConsumedAt">Consumption instant, if already used.</param>
/// <param name="RevokedAt">Revocation instant, if revoked.</param>
/// <param name="ConcurrencyToken">Optimistic concurrency token protecting one-time use.</param>
public sealed record AgentEnrollmentTokenChallenge(
    Guid EnrollmentTokenId,
    ReadOnlyMemory<byte> Salt,
    ReadOnlyMemory<byte> SecretHash,
    string HashAlgorithm,
    string ExpectedInstallationId,
    string ExpectedEnvironment,
    string ExpectedPlatform,
    string Scope,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsumedAt,
    DateTimeOffset? RevokedAt,
    Guid ConcurrencyToken);

/// <summary>
/// Carries a validated public-key enrollment request to the certificate issuer boundary.
/// </summary>
/// <param name="CertificateSigningRequestDer">Bounded PKCS#10 DER bytes.</param>
/// <param name="CertificateSigningRequestSha256">Canonical digest of the exact CSR.</param>
/// <param name="InstallationId">Validated installation identifier.</param>
/// <param name="DisplayName">Validated display name.</param>
/// <param name="Environment">Validated environment.</param>
/// <param name="Platform">Validated platform.</param>
/// <param name="AgentVersion">Validated Agent version.</param>
/// <param name="RequestedScope">Validated enrollment scope.</param>
public sealed record AgentCertificateIssueRequest(
    ReadOnlyMemory<byte> CertificateSigningRequestDer,
    ReadOnlyMemory<byte> CertificateSigningRequestSha256,
    string InstallationId,
    string DisplayName,
    string Environment,
    string Platform,
    string AgentVersion,
    string RequestedScope);

/// <summary>Identifies whether the certificate issuer produced a bounded certificate.</summary>
public enum AgentCertificateIssueDisposition
{
    /// <summary>A certificate matching the CSR was issued.</summary>
    Issued,

    /// <summary>The CSR was invalid, unsupported or failed proof-of-possession validation.</summary>
    InvalidRequest,

    /// <summary>No authorised issuer is configured.</summary>
    Unavailable,
}

/// <summary>
/// Returns public certificate evidence from an issuer implementation.
/// </summary>
/// <param name="Disposition">Typed issuer result.</param>
/// <param name="CertificateDer">Public certificate DER bytes.</param>
/// <param name="CertificateThumbprint">Canonical certificate thumbprint.</param>
/// <param name="PublicKeySha256">SHA-256 digest of the issued public key.</param>
/// <param name="CertificateSigningRequestSha256">Digest of the CSR actually processed by the issuer.</param>
/// <param name="NotBefore">Certificate validity start.</param>
/// <param name="NotAfter">Certificate exclusive validity end.</param>
public sealed record AgentCertificateIssueResult(
    AgentCertificateIssueDisposition Disposition,
    ReadOnlyMemory<byte> CertificateDer,
    string? CertificateThumbprint,
    string? PublicKeySha256,
    ReadOnlyMemory<byte> CertificateSigningRequestSha256,
    DateTimeOffset? NotBefore,
    DateTimeOffset? NotAfter);

/// <summary>Issues an Agent client certificate without receiving or returning the Agent private key.</summary>
public interface IAgentCertificateIssuer
{
    /// <summary>
    /// Validates proof of possession and issues a bounded client certificate, or fails closed.
    /// </summary>
    /// <param name="request">Validated CSR and exact token-bound metadata.</param>
    /// <param name="issuedAt">Trusted UTC decision instant.</param>
    /// <param name="cancellationToken">Cancellation signal for issuer work.</param>
    /// <returns>Issued public certificate evidence or a typed refusal.</returns>
    ValueTask<AgentCertificateIssueResult> IssueAsync(
        AgentCertificateIssueRequest request,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken);
}

/// <summary>
/// Carries the validated enrollment evidence into the atomic persistence boundary.
/// </summary>
/// <param name="EnrollmentTokenId">Token identifier to consume.</param>
/// <param name="ExpectedTokenConcurrency">Token version observed before certificate issuance.</param>
/// <param name="PresentedSecretHash">Digest recomputed from the presented token and stored salt.</param>
/// <param name="Request">Validated certificate request.</param>
/// <param name="MessageId">Unique enrollment request identifier.</param>
/// <param name="OccurredAt">Agent request creation instant.</param>
/// <param name="SentAt">Agent send instant.</param>
/// <param name="Certificate">Issued public certificate evidence.</param>
public sealed record AgentEnrollmentCommit(
    Guid EnrollmentTokenId,
    Guid ExpectedTokenConcurrency,
    ReadOnlyMemory<byte> PresentedSecretHash,
    AgentCertificateIssueRequest Request,
    Guid MessageId,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    AgentCertificateIssueResult Certificate);

/// <summary>Identifies the persistence result of an enrollment commit.</summary>
public enum AgentEnrollmentCommitDisposition
{
    /// <summary>The token, Agent, certificate and audit entry were committed.</summary>
    Enrolled,

    /// <summary>The token was no longer eligible or its proof no longer matched.</summary>
    Denied,

    /// <summary>A uniqueness or optimistic concurrency conflict occurred.</summary>
    Conflict,
}

/// <summary>
/// Returns the durable enrollment commit result.
/// </summary>
/// <param name="Disposition">Typed persistence result.</param>
/// <param name="AgentId">New Agent identifier when enrolled.</param>
/// <param name="ErrorCode">Internal sanitised diagnostic code.</param>
public sealed record AgentEnrollmentCommitResult(
    AgentEnrollmentCommitDisposition Disposition,
    Guid? AgentId,
    string? ErrorCode);

/// <summary>
/// Carries a versioned Agent heartbeat. Server receipt time, rather than Agent time, owns freshness.
/// </summary>
/// <param name="MessageId">Unique heartbeat identifier.</param>
/// <param name="SchemaVersion">Heartbeat schema version.</param>
/// <param name="AgentId">Agent that produced the heartbeat.</param>
/// <param name="Sequence">Monotonic per-Agent heartbeat sequence.</param>
/// <param name="OccurredAt">UTC heartbeat creation instant.</param>
/// <param name="SentAt">UTC send instant.</param>
/// <param name="AgentVersion">Agent product version.</param>
/// <param name="ProtocolMinimum">Minimum supported protocol major.</param>
/// <param name="ProtocolMaximum">Maximum supported protocol major.</param>
/// <param name="QueueDepth">Bounded local queue depth.</param>
/// <param name="OldestQueuedAt">Oldest queued item instant, when the queue is non-empty.</param>
/// <param name="AgentTime">Agent clock instant used only to estimate skew.</param>
public sealed record AgentHeartbeatRequest(
    Guid MessageId,
    int SchemaVersion,
    Guid AgentId,
    long Sequence,
    DateTimeOffset OccurredAt,
    DateTimeOffset SentAt,
    string AgentVersion,
    int ProtocolMinimum,
    int ProtocolMaximum,
    long QueueDepth,
    DateTimeOffset? OldestQueuedAt,
    DateTimeOffset AgentTime);

/// <summary>Identifies a durable heartbeat outcome.</summary>
public enum AgentHeartbeatDisposition
{
    /// <summary>The next contiguous heartbeat was accepted.</summary>
    Accepted,

    /// <summary>An exact replay returned the original durable acceptance.</summary>
    Duplicate,

    /// <summary>A later sequence was accepted while recording a visible gap.</summary>
    AcceptedWithGap,

    /// <summary>The message ID or sequence conflicts with durable history.</summary>
    Conflict,

    /// <summary>The Agent ceased to be active before the transaction committed.</summary>
    AgentInactive,

    /// <summary>The request failed bounded validation.</summary>
    Invalid,

    /// <summary>The Agent and server protocol ranges do not overlap.</summary>
    Incompatible,
}

/// <summary>
/// Returns durable heartbeat acceptance without treating heartbeat health as database-instance health.
/// </summary>
/// <param name="Disposition">Typed heartbeat result.</param>
/// <param name="AcceptedAt">Trusted original receipt instant when accepted or replayed.</param>
/// <param name="HighestAcceptedSequence">Durable cursor after processing.</param>
/// <param name="ClockSkewMilliseconds">Bounded estimate of server time minus Agent time.</param>
/// <param name="ErrorCode">Stable sanitised refusal code.</param>
public sealed record AgentHeartbeatOutcome(
    AgentHeartbeatDisposition Disposition,
    DateTimeOffset? AcceptedAt,
    long HighestAcceptedSequence,
    long? ClockSkewMilliseconds,
    string? ErrorCode);

/// <summary>
/// Carries a heartbeat whose shape, time and protocol range have passed the application boundary.
/// </summary>
/// <param name="Request">Validated original request.</param>
/// <param name="PayloadSha256">Canonical digest used for exact replay comparison.</param>
public sealed record ValidatedAgentHeartbeat(
    AgentHeartbeatRequest Request,
    string PayloadSha256);

/// <summary>
/// Describes one safe read-only monitoring assignment. Administrative credential references and commands are
/// deliberately absent.
/// </summary>
/// <param name="InstanceId">Assigned instance identifier.</param>
/// <param name="DisplayName">Instance display name.</param>
/// <param name="ProviderType">Stable provider identifier; this does not claim homologation.</param>
/// <param name="Environment">Exact assignment environment.</param>
/// <param name="Endpoint">Typed non-secret endpoint configuration.</param>
/// <param name="MonitoringCredentialReference">Opaque monitoring-only credential reference.</param>
/// <param name="Tags">Typed non-secret tags.</param>
/// <param name="IntervalSeconds">Configured observation interval.</param>
/// <param name="TimeoutSeconds">Configured provider timeout.</param>
/// <param name="RetryCount">Configured bounded retry count.</param>
/// <param name="UpdatedAt">Server-side configuration update instant.</param>
public sealed record AgentReadOnlyAssignment(
    Guid InstanceId,
    string DisplayName,
    string ProviderType,
    string Environment,
    JsonElement Endpoint,
    string? MonitoringCredentialReference,
    JsonElement Tags,
    int IntervalSeconds,
    int TimeoutSeconds,
    int RetryCount,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Returns one complete, ordered and bounded assignment snapshot.
/// </summary>
/// <param name="SchemaVersion">Assignment snapshot schema.</param>
/// <param name="AgentId">Owning Agent.</param>
/// <param name="Version">Deterministic digest of the complete assignment set.</param>
/// <param name="GeneratedAt">Trusted server generation instant.</param>
/// <param name="Assignments">Complete ordered assignments.</param>
public sealed record AgentAssignmentSnapshot(
    int SchemaVersion,
    Guid AgentId,
    string Version,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<AgentReadOnlyAssignment> Assignments);

/// <summary>Identifies assignment retrieval outcomes.</summary>
public enum AgentAssignmentDisposition
{
    /// <summary>A complete assignment snapshot is available.</summary>
    Available,

    /// <summary>The caller already has the exact current version.</summary>
    NotModified,

    /// <summary>The Agent is not active or its current version does not match the authenticated request.</summary>
    AgentInactive,

    /// <summary>The complete snapshot exceeded the admitted ceiling and was not truncated.</summary>
    LimitExceeded,

    /// <summary>Stored configuration was invalid and no partial snapshot was returned.</summary>
    InvalidStoredConfiguration,

    /// <summary>A concurrent consistency boundary prevented an authoritative complete snapshot.</summary>
    TemporarilyUnavailable,
}

/// <summary>
/// Returns a read-only assignment result.
/// </summary>
/// <param name="Disposition">Typed retrieval result.</param>
/// <param name="Snapshot">Complete snapshot when available.</param>
/// <param name="CurrentVersion">Current version for available or not-modified results.</param>
/// <param name="ErrorCode">Stable sanitised refusal code.</param>
public sealed record AgentAssignmentOutcome(
    AgentAssignmentDisposition Disposition,
    AgentAssignmentSnapshot? Snapshot,
    string? CurrentVersion,
    string? ErrorCode);

/// <summary>
/// Presents safe Agent Fleet facts to an authorised human without exposing installation identifiers, certificate
/// material or enrollment evidence.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="DisplayName">Human-readable Agent name.</param>
/// <param name="Environment">Agent environment.</param>
/// <param name="Platform">Declared platform.</param>
/// <param name="AgentVersion">Last accepted Agent version.</param>
/// <param name="State">Durable lifecycle state.</param>
/// <param name="EnrolledAt">Enrollment instant.</param>
/// <param name="RevokedAt">Revocation instant, if revoked.</param>
/// <param name="LastSeenAt">Last trusted server receipt instant, if any.</param>
public sealed record AgentFleetView(
    Guid AgentId,
    string DisplayName,
    string Environment,
    string Platform,
    string AgentVersion,
    string State,
    DateTimeOffset EnrolledAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastSeenAt);

/// <summary>
/// Returns a scoped human Agent catalogue.
/// </summary>
/// <param name="Authorised">Whether the subject held an applicable permission.</param>
/// <param name="Agents">Safe scoped Agent rows.</param>
/// <param name="Complete">Whether the bounded complete catalogue could be returned.</param>
/// <param name="ErrorCode">Stable sanitised refusal code for an incomplete result.</param>
public sealed record AgentFleetCatalogue(
    bool Authorised,
    IReadOnlyList<AgentFleetView> Agents,
    bool Complete = true,
    string? ErrorCode = null);

/// <summary>
/// Carries a bounded machine-readable reason for human-initiated Agent revocation.
/// </summary>
/// <param name="ReasonCode">Stable reason code without free-form secret-bearing text.</param>
public sealed record AgentRevocationRequest(string ReasonCode);

/// <summary>Identifies the result of an Agent revocation attempt.</summary>
public enum AgentRevocationDisposition
{
    /// <summary>The Agent identity and every active certificate are durably revoked.</summary>
    Revoked,

    /// <summary>The Agent was already revoked and certificate reconciliation is complete.</summary>
    AlreadyRevoked,

    /// <summary>The Agent is durably revoked while certificate reconciliation remains safely resumable.</summary>
    CertificatesReconciling,

    /// <summary>The Agent did not exist in the caller's authorised scope.</summary>
    Denied,

    /// <summary>An optimistic concurrency conflict prevented the transition.</summary>
    Conflict,
}

/// <summary>
/// Returns a sanitised Agent revocation result.
/// </summary>
/// <param name="Disposition">Typed revocation result.</param>
/// <param name="AgentId">Requested Agent identifier.</param>
/// <param name="RevokedAt">Original revocation instant when available.</param>
/// <param name="ErrorCode">Stable sanitised refusal code.</param>
public sealed record AgentRevocationOutcome(
    AgentRevocationDisposition Disposition,
    Guid AgentId,
    DateTimeOffset? RevokedAt,
    string? ErrorCode);

/// <summary>Owns durable Agent Fleet identity, heartbeat and read-only assignment operations.</summary>
public interface IAgentFleetStore
{
    /// <summary>Loads one enrollment challenge by its non-secret public identifier.</summary>
    /// <param name="enrollmentTokenId">Public token identifier.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>The stored challenge or <see langword="null"/> without revealing it externally.</returns>
    ValueTask<AgentEnrollmentTokenChallenge?> GetEnrollmentTokenAsync(
        Guid enrollmentTokenId,
        CancellationToken cancellationToken);

    /// <summary>Commits one-time token consumption, Agent identity, certificate and audit evidence atomically.</summary>
    /// <param name="commit">Validated enrollment evidence.</param>
    /// <param name="now">Trusted commit instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Durable commit result.</returns>
    ValueTask<AgentEnrollmentCommitResult> CompleteEnrollmentAsync(
        AgentEnrollmentCommit commit,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Records a bounded generic enrollment denial audit without persisting presented material.</summary>
    /// <param name="internalCode">Sanitised internal denial reason.</param>
    /// <param name="now">Trusted audit instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>A task representing the required audit write.</returns>
    ValueTask AuditEnrollmentDenialAsync(
        string internalCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Records a heartbeat and advances its durable anti-replay cursor atomically.</summary>
    /// <param name="heartbeat">Validated heartbeat.</param>
    /// <param name="receivedAt">Trusted server receipt instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Durable heartbeat result.</returns>
    ValueTask<AgentHeartbeatOutcome> RecordHeartbeatAsync(
        ValidatedAgentHeartbeat heartbeat,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken);

    /// <summary>Returns one complete read-only assignment snapshot for the exact active Agent.</summary>
    /// <param name="agentId">Authenticated Agent identifier.</param>
    /// <param name="agentVersion">Authenticated request version.</param>
    /// <param name="afterVersion">Optional exact snapshot version already held by the caller.</param>
    /// <param name="generatedAt">Trusted response instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Complete snapshot, not-modified result or fail-closed refusal.</returns>
    ValueTask<AgentAssignmentOutcome> GetAssignmentsAsync(
        Guid agentId,
        string agentVersion,
        string? afterVersion,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken);

    /// <summary>Returns safe Agent Fleet facts within the human subject's RBAC scope.</summary>
    /// <param name="subjectId">Authenticated human subject.</param>
    /// <param name="permissionCode">Required read permission.</param>
    /// <param name="now">Trusted authorisation instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Scoped catalogue and authorisation state.</returns>
    ValueTask<AgentFleetCatalogue> GetAuthorisedAgentsAsync(
        string subjectId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Revokes an Agent under server-side RBAC, then reconciles certificate metadata in resumable batches.</summary>
    /// <param name="subjectId">Authenticated human subject.</param>
    /// <param name="agentId">Target Agent.</param>
    /// <param name="permissionCode">Required revocation permission.</param>
    /// <param name="reasonCode">Validated reason code.</param>
    /// <param name="now">Trusted revocation instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Durable revocation result.</returns>
    ValueTask<AgentRevocationOutcome> RevokeAgentAsync(
        string subjectId,
        Guid agentId,
        string permissionCode,
        string reasonCode,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Coordinates bounded Agent Fleet protocol validation independently of HTTP and persistence technology.</summary>
public sealed partial class AgentFleetService;
