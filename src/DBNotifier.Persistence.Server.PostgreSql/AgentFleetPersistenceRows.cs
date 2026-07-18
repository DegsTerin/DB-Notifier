// Module purpose: Defines central Agent Fleet identity and anti-replay rows without storing token values, private keys or full certificates.
namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>Stores one salted, one-time enrollment-token proof and its exact pre-authorised scope.</summary>
public sealed class AgentEnrollmentTokenRow
{
    /// <summary>Gets or sets the non-secret public token identifier.</summary>
    public Guid EnrollmentTokenId { get; set; }

    /// <summary>Gets or sets the per-token random salt.</summary>
    public required byte[] Salt { get; set; }

    /// <summary>Gets or sets the domain-separated hash of the high-entropy token secret.</summary>
    public required byte[] SecretHash { get; set; }

    /// <summary>Gets or sets the exact hash algorithm identifier.</summary>
    public required string HashAlgorithm { get; set; }

    /// <summary>Gets or sets the installation identifier authorised during token provisioning.</summary>
    public required string ExpectedInstallationId { get; set; }

    /// <summary>Gets or sets the environment authorised during token provisioning.</summary>
    public required string ExpectedEnvironment { get; set; }

    /// <summary>Gets or sets the platform authorised during token provisioning.</summary>
    public required string ExpectedPlatform { get; set; }

    /// <summary>Gets or sets the exact scope authorised during token provisioning.</summary>
    public required string Scope { get; set; }

    /// <summary>Gets or sets the one-time token lifecycle state.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets the trusted issuance instant.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>Gets or sets the exclusive expiry instant.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the consumption instant.</summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    /// <summary>Gets or sets the revocation instant.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets the Agent created by successful consumption.</summary>
    public Guid? ConsumedByAgentId { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token protecting one-time use.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>Stores certificate identity metadata while keeping certificate bodies and every private key outside ordinary persistence.</summary>
public sealed class AgentCertificateRow
{
    /// <summary>Gets or sets the certificate metadata identifier.</summary>
    public Guid AgentCertificateId { get; set; }

    /// <summary>Gets or sets the owning Agent identifier.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the canonical certificate thumbprint.</summary>
    public required string Thumbprint { get; set; }

    /// <summary>Gets or sets the SHA-256 digest of the certificate public key, or null for a legacy backfill.</summary>
    public string? PublicKeySha256 { get; set; }

    /// <summary>Gets or sets the SHA-256 digest of the validated CSR, or null for a legacy backfill.</summary>
    public string? CertificateSigningRequestSha256 { get; set; }

    /// <summary>Gets or sets the certificate lifecycle state.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets the trusted issuance instant.</summary>
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>Gets or sets the certificate validity start, or null when unavailable for legacy metadata.</summary>
    public DateTimeOffset? NotBefore { get; set; }

    /// <summary>Gets or sets the certificate validity end, or null when unavailable for legacy metadata.</summary>
    public DateTimeOffset? NotAfter { get; set; }

    /// <summary>Gets or sets the monotonic revocation instant.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets a bounded machine-readable revocation reason.</summary>
    public string? RevocationReasonCode { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>Preserves the highest accepted heartbeat sequence independently of heartbeat-detail retention.</summary>
public sealed class AgentHeartbeatCursorRow
{
    /// <summary>Gets or sets the owning Agent identifier and primary key.</summary>
    public Guid AgentId { get; set; }

    /// <summary>Gets or sets the highest accepted heartbeat sequence.</summary>
    public long HighestAcceptedSequence { get; set; }

    /// <summary>Gets or sets the most recently accepted heartbeat message identifier.</summary>
    public Guid LastMessageId { get; set; }

    /// <summary>Gets or sets the trusted cursor update instant.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}
