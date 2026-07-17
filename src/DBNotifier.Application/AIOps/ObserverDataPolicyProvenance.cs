// Module purpose: Authenticates Observer data-policy grants and revocation snapshots locally without secret ownership, persistence, network access or runtime activation.
using System.Security.Cryptography;
using System.Text;

namespace DBNotifier.Application.AIOps;

/// <summary>
/// Binds one immutable Observer data policy to an issuing identity, signing key and bounded grant period. The grant
/// carries authorisation assertions only; its integrity is established separately by a signature and trust anchor.
/// </summary>
public sealed class ObserverDataPolicyGrant
{
    /// <summary>Initialises one purpose-specific policy grant.</summary>
    /// <param name="grantId">Stable non-secret grant identifier used for revocation.</param>
    /// <param name="issuerId">Stable identity of the policy authority.</param>
    /// <param name="signingKeyId">Stable identifier of the grant-signing public key.</param>
    /// <param name="issuedAt">UTC issuance instant.</param>
    /// <param name="expiresAt">Exclusive UTC grant expiry, evaluated independently from the policy period.</param>
    /// <param name="policy">Exact immutable policy authorised by the grant.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for an invalid period or non-UTC instant.</exception>
    public ObserverDataPolicyGrant(
        string grantId,
        string issuerId,
        string signingKeyId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        ObserverDataPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ObserverPolicyProvenanceGuard.ValidateUtcInstant(issuedAt, nameof(issuedAt));
        ObserverPolicyProvenanceGuard.ValidateUtcInstant(expiresAt, nameof(expiresAt));
        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException(
                "Observer policy grant expiry must follow issuance.",
                nameof(expiresAt));
        }

        GrantId = ObserverContractGuard.StableIdentifier(grantId, nameof(grantId));
        IssuerId = ObserverContractGuard.StableIdentifier(issuerId, nameof(issuerId));
        SigningKeyId = ObserverContractGuard.StableIdentifier(signingKeyId, nameof(signingKeyId));
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Policy = policy;
    }

    /// <summary>Gets the revocable grant identifier.</summary>
    public string GrantId { get; }

    /// <summary>Gets the asserted policy-authority identity.</summary>
    public string IssuerId { get; }

    /// <summary>Gets the asserted grant-signing key identifier.</summary>
    public string SigningKeyId { get; }

    /// <summary>Gets the UTC issuance instant.</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>Gets the exclusive UTC grant expiry.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets the exact policy whose authorisation is signed.</summary>
    public ObserverDataPolicy Policy { get; }
}

/// <summary>
/// Declares a bounded, independently signed revocation view for policy grants and grant-signing keys. It contains no
/// mutable store and is considered trustworthy only after verification by the adapter at a supplied UTC instant.
/// </summary>
public sealed class ObserverPolicyRevocationSnapshot
{
    /// <summary>Maximum revoked identifiers accepted in either revocation set.</summary>
    public const int MaximumRevocationCount = 10_000;

    private readonly HashSet<string> revokedGrantIds;
    private readonly HashSet<string> revokedSigningKeyIds;

    /// <summary>Initialises one immutable revocation snapshot.</summary>
    /// <param name="snapshotId">Stable snapshot identifier.</param>
    /// <param name="snapshotVersion">Independent revocation-data version.</param>
    /// <param name="issuerId">Stable identity of the revocation authority.</param>
    /// <param name="signingKeyId">Stable identifier of the revocation-signing key.</param>
    /// <param name="issuedAt">UTC snapshot issuance instant.</param>
    /// <param name="expiresAt">Exclusive UTC freshness bound.</param>
    /// <param name="revokedGrantIds">Bounded grant identifiers revoked by this view.</param>
    /// <param name="revokedSigningKeyIds">Bounded grant-signing key identifiers revoked by this view.</param>
    /// <exception cref="ArgumentNullException">Thrown when either revocation collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown for invalid times, null entries or repeated identifiers.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a collection exceeds its bound.</exception>
    public ObserverPolicyRevocationSnapshot(
        string snapshotId,
        string snapshotVersion,
        string issuerId,
        string signingKeyId,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        IReadOnlyCollection<string> revokedGrantIds,
        IReadOnlyCollection<string> revokedSigningKeyIds)
    {
        ObserverPolicyProvenanceGuard.ValidateUtcInstant(issuedAt, nameof(issuedAt));
        ObserverPolicyProvenanceGuard.ValidateUtcInstant(expiresAt, nameof(expiresAt));
        if (expiresAt <= issuedAt)
        {
            throw new ArgumentException("Revocation snapshot expiry must follow issuance.", nameof(expiresAt));
        }

        SnapshotId = ObserverContractGuard.StableIdentifier(snapshotId, nameof(snapshotId));
        SnapshotVersion = ObserverContractGuard.StableIdentifier(snapshotVersion, nameof(snapshotVersion));
        IssuerId = ObserverContractGuard.StableIdentifier(issuerId, nameof(issuerId));
        SigningKeyId = ObserverContractGuard.StableIdentifier(signingKeyId, nameof(signingKeyId));
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        RevokedGrantIds = CopyIdentifiers(revokedGrantIds, nameof(revokedGrantIds));
        RevokedSigningKeyIds = CopyIdentifiers(revokedSigningKeyIds, nameof(revokedSigningKeyIds));
        this.revokedGrantIds = RevokedGrantIds.ToHashSet(StringComparer.Ordinal);
        this.revokedSigningKeyIds = RevokedSigningKeyIds.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Gets the revocation snapshot identifier.</summary>
    public string SnapshotId { get; }

    /// <summary>Gets the independent revocation snapshot version.</summary>
    public string SnapshotVersion { get; }

    /// <summary>Gets the revocation-authority identity.</summary>
    public string IssuerId { get; }

    /// <summary>Gets the revocation-signing key identifier.</summary>
    public string SigningKeyId { get; }

    /// <summary>Gets the UTC issuance instant.</summary>
    public DateTimeOffset IssuedAt { get; }

    /// <summary>Gets the exclusive UTC freshness bound.</summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>Gets revoked grant identifiers in deterministic order.</summary>
    public IReadOnlyList<string> RevokedGrantIds { get; }

    /// <summary>Gets revoked grant-signing key identifiers in deterministic order.</summary>
    public IReadOnlyList<string> RevokedSigningKeyIds { get; }

    /// <summary>Checks whether a grant or its signing key is explicitly revoked.</summary>
    /// <param name="grantId">Exact grant identifier.</param>
    /// <param name="grantSigningKeyId">Exact grant-signing key identifier.</param>
    /// <returns><see langword="true"/> when either identifier is revoked.</returns>
    internal bool IsRevoked(string grantId, string grantSigningKeyId) =>
        revokedGrantIds.Contains(grantId) || revokedSigningKeyIds.Contains(grantSigningKeyId);

    /// <summary>Copies and validates one bounded identifier set.</summary>
    /// <param name="values">Caller-owned identifier collection.</param>
    /// <param name="parameterName">Public parameter name used by exceptions.</param>
    /// <returns>An immutable, ordinally sorted identifier view.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown for null or repeated entries.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the set exceeds its bound.</exception>
    private static System.Collections.ObjectModel.ReadOnlyCollection<string> CopyIdentifiers(
        IReadOnlyCollection<string> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        string?[] bounded = values.Take(MaximumRevocationCount + 1).ToArray();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bounded.Length, MaximumRevocationCount, parameterName);
        if (bounded.Any(value => value is null))
        {
            throw new ArgumentException("Revocation sets cannot contain null identifiers.", parameterName);
        }

        string[] copied = bounded
            .Select(value => ObserverContractGuard.StableIdentifier(value!, parameterName))
            .ToArray();
        if (copied.Distinct(StringComparer.Ordinal).Count() != copied.Length)
        {
            throw new ArgumentException("Revocation sets cannot repeat identifiers.", parameterName);
        }

        return Array.AsReadOnly(copied.Order(StringComparer.Ordinal).ToArray());
    }
}

/// <summary>Provides one explicitly trusted ECDSA P-256 public key for a single issuer and signing purpose.</summary>
public sealed class ObserverPolicyTrustAnchor
{
    private const int MaximumPublicKeyLength = 512;
    private readonly byte[] subjectPublicKeyInfo;

    /// <summary>Initialises one immutable public trust anchor.</summary>
    /// <param name="issuerId">Exact trusted issuer identity.</param>
    /// <param name="signingKeyId">Exact trusted signing key identifier.</param>
    /// <param name="subjectPublicKeyInfo">DER-encoded ECDSA P-256 SubjectPublicKeyInfo bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subjectPublicKeyInfo"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when public-key bytes are empty or excessive.</exception>
    public ObserverPolicyTrustAnchor(string issuerId, string signingKeyId, byte[] subjectPublicKeyInfo)
    {
        ArgumentNullException.ThrowIfNull(subjectPublicKeyInfo);
        ArgumentOutOfRangeException.ThrowIfLessThan(subjectPublicKeyInfo.Length, 1, nameof(subjectPublicKeyInfo));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            subjectPublicKeyInfo.Length,
            MaximumPublicKeyLength,
            nameof(subjectPublicKeyInfo));
        IssuerId = ObserverContractGuard.StableIdentifier(issuerId, nameof(issuerId));
        SigningKeyId = ObserverContractGuard.StableIdentifier(signingKeyId, nameof(signingKeyId));
        this.subjectPublicKeyInfo = [.. subjectPublicKeyInfo];
    }

    /// <summary>Gets the exact trusted issuer identity.</summary>
    public string IssuerId { get; }

    /// <summary>Gets the exact trusted signing key identifier.</summary>
    public string SigningKeyId { get; }

    /// <summary>Copies the public-key bytes for local signature verification.</summary>
    /// <returns>A fresh copy of the DER-encoded public key.</returns>
    internal byte[] CopySubjectPublicKeyInfo() => [.. subjectPublicKeyInfo];
}

/// <summary>
/// Separates application-configured public trust from caller-supplied policy evidence. Grant and revocation anchors
/// have distinct signing purposes and this configuration contains no private material or mutable trust discovery.
/// </summary>
public sealed class ObserverPolicyTrustConfiguration
{
    /// <summary>Initialises one immutable purpose-separated trust configuration.</summary>
    /// <param name="grantTrustAnchor">Public key configured by the application only for policy grants.</param>
    /// <param name="revocationTrustAnchor">Public key configured by the application only for revocation views.</param>
    /// <exception cref="ArgumentNullException">Thrown when either trust anchor is null.</exception>
    public ObserverPolicyTrustConfiguration(
        ObserverPolicyTrustAnchor grantTrustAnchor,
        ObserverPolicyTrustAnchor revocationTrustAnchor)
    {
        ArgumentNullException.ThrowIfNull(grantTrustAnchor);
        ArgumentNullException.ThrowIfNull(revocationTrustAnchor);
        GrantTrustAnchor = grantTrustAnchor;
        RevocationTrustAnchor = revocationTrustAnchor;
    }

    /// <summary>Gets the application-configured grant verification anchor.</summary>
    public ObserverPolicyTrustAnchor GrantTrustAnchor { get; }

    /// <summary>Gets the application-configured revocation verification anchor.</summary>
    public ObserverPolicyTrustAnchor RevocationTrustAnchor { get; }
}

/// <summary>
/// Carries a signed policy grant and independently signed revocation snapshot as untrusted evidence. Construction
/// copies signatures but does not assert trust; configured public anchors remain in a separate application boundary.
/// </summary>
public sealed class ObserverDataPolicyVerificationContext
{
    private const int MaximumSignatureLength = 256;
    private readonly byte[] grantSignature;
    private readonly byte[] revocationSignature;

    /// <summary>Initialises one immutable, unverified policy context.</summary>
    /// <param name="grant">Policy authorisation assertion.</param>
    /// <param name="grantSignature">ECDSA P-256 SHA-256 signature of the canonical grant payload.</param>
    /// <param name="revocationSnapshot">Current revocation assertion.</param>
    /// <param name="revocationSignature">ECDSA P-256 SHA-256 signature of the canonical revocation payload.</param>
    /// <exception cref="ArgumentNullException">Thrown when an object or signature is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a signature is empty or excessive.</exception>
    public ObserverDataPolicyVerificationContext(
        ObserverDataPolicyGrant grant,
        byte[] grantSignature,
        ObserverPolicyRevocationSnapshot revocationSnapshot,
        byte[] revocationSignature)
    {
        ArgumentNullException.ThrowIfNull(grant);
        ArgumentNullException.ThrowIfNull(grantSignature);
        ArgumentNullException.ThrowIfNull(revocationSnapshot);
        ArgumentNullException.ThrowIfNull(revocationSignature);
        ValidateSignature(grantSignature, nameof(grantSignature));
        ValidateSignature(revocationSignature, nameof(revocationSignature));
        Grant = grant;
        this.grantSignature = [.. grantSignature];
        RevocationSnapshot = revocationSnapshot;
        this.revocationSignature = [.. revocationSignature];
    }

    /// <summary>Gets the policy grant to verify.</summary>
    public ObserverDataPolicyGrant Grant { get; }

    /// <summary>Gets the signed revocation view to verify.</summary>
    public ObserverPolicyRevocationSnapshot RevocationSnapshot { get; }

    /// <summary>Copies the caller-provided grant signature.</summary>
    /// <returns>A fresh signature copy.</returns>
    internal byte[] CopyGrantSignature() => [.. grantSignature];

    /// <summary>Copies the caller-provided revocation signature.</summary>
    /// <returns>A fresh signature copy.</returns>
    internal byte[] CopyRevocationSignature() => [.. revocationSignature];

    /// <summary>Checks one bounded signature without interpreting its cryptographic validity.</summary>
    /// <param name="signature">Caller-owned signature bytes.</param>
    /// <param name="parameterName">Public parameter name used by exceptions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when signature bytes are empty or excessive.</exception>
    private static void ValidateSignature(byte[] signature, string parameterName)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(signature.Length, 1, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(signature.Length, MaximumSignatureLength, parameterName);
    }
}

/// <summary>
/// Produces the versioned, length-prefixed canonical bytes signed outside MOD-12. It performs no signing, secret
/// handling, storage or external communication and returns a fresh payload for each call.
/// </summary>
public static class ObserverPolicyProvenancePayload
{
    private const string GrantSchemaVersion = "observer-policy-grant.v1";
    private const string RevocationSchemaVersion = "observer-policy-revocation.v1";

    /// <summary>Creates the canonical SHA-256 signing payload for one policy grant.</summary>
    /// <param name="grant">Grant whose complete identity, authorisation and policy content will be bound.</param>
    /// <returns>Fresh deterministic canonical bytes suitable for ECDSA P-256 SHA-256 signing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grant"/> is null.</exception>
    public static byte[] CreateGrant(ObserverDataPolicyGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        Write(writer, GrantSchemaVersion);
        Write(writer, grant.GrantId);
        Write(writer, grant.IssuerId);
        Write(writer, grant.SigningKeyId);
        Write(writer, grant.IssuedAt);
        Write(writer, grant.ExpiresAt);
        ObserverDataPolicy policy = grant.Policy;
        Write(writer, policy.PolicySchemaVersion);
        Write(writer, policy.PolicyId);
        Write(writer, policy.PolicyVersion);
        writer.Write((int)policy.OptInState);
        writer.Write((int)policy.DataUse);
        writer.Write(policy.AuthorisationScopeId.ToByteArray());
        Write(writer, policy.EffectiveFrom);
        Write(writer, policy.ExpiresAt);
        writer.Write(policy.AllowSyntheticEvidence);
        writer.Write(policy.AllowedSources.Count);
        foreach (ObserverDataScope source in policy.AllowedSources)
        {
            writer.Write(source.InstanceId.ToByteArray());
            writer.Write(source.AgentId.ToByteArray());
        }

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Creates the canonical SHA-256 signing payload for one revocation snapshot.</summary>
    /// <param name="snapshot">Snapshot whose identity, freshness and complete revocation sets will be bound.</param>
    /// <returns>Fresh deterministic canonical bytes suitable for ECDSA P-256 SHA-256 signing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public static byte[] CreateRevocation(ObserverPolicyRevocationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        Write(writer, RevocationSchemaVersion);
        Write(writer, snapshot.SnapshotId);
        Write(writer, snapshot.SnapshotVersion);
        Write(writer, snapshot.IssuerId);
        Write(writer, snapshot.SigningKeyId);
        Write(writer, snapshot.IssuedAt);
        Write(writer, snapshot.ExpiresAt);
        Write(writer, snapshot.RevokedGrantIds);
        Write(writer, snapshot.RevokedSigningKeyIds);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Writes one UTF-8 string with an explicit byte length.</summary>
    /// <param name="writer">In-memory canonical payload writer.</param>
    /// <param name="value">Validated contract value.</param>
    private static void Write(BinaryWriter writer, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    /// <summary>Writes one UTC instant as stable UTC ticks.</summary>
    /// <param name="writer">In-memory canonical payload writer.</param>
    /// <param name="value">Validated UTC instant.</param>
    private static void Write(BinaryWriter writer, DateTimeOffset value) => writer.Write(value.UtcTicks);

    /// <summary>Writes an already sorted identifier set with an explicit count.</summary>
    /// <param name="writer">In-memory canonical payload writer.</param>
    /// <param name="values">Immutable deterministic identifier view.</param>
    private static void Write(BinaryWriter writer, IReadOnlyList<string> values)
    {
        writer.Write(values.Count);
        foreach (string value in values)
        {
            Write(writer, value);
        }
    }
}

/// <summary>Verifies identity, authorisation integrity, freshness and revocation without accepting fallback trust.</summary>
internal static class ObserverDataPolicyProvenanceVerifier
{
    /// <summary>Verifies one complete context for an exact data use and UTC decision instant.</summary>
    /// <param name="context">Unverified policy, signatures, revocation view and public anchors.</param>
    /// <param name="requiredDataUse">Purpose selected by the owning adapter entry point.</param>
    /// <param name="asOf">Trusted UTC decision instant.</param>
    /// <returns>The verified policy or one sanitised fail-closed code.</returns>
    public static (ObserverDataPolicy? Policy, string? RejectionCode) Verify(
        ObserverDataPolicyVerificationContext context,
        ObserverPolicyTrustConfiguration trustConfiguration,
        ObserverDataUse requiredDataUse,
        DateTimeOffset asOf)
    {
        ObserverDataPolicyGrant grant = context.Grant;
        ObserverPolicyRevocationSnapshot revocation = context.RevocationSnapshot;
        if (!MatchesIdentity(grant.IssuerId, grant.SigningKeyId, trustConfiguration.GrantTrustAnchor) ||
            !MatchesIdentity(
                revocation.IssuerId,
                revocation.SigningKeyId,
                trustConfiguration.RevocationTrustAnchor) ||
            !string.Equals(grant.IssuerId, revocation.IssuerId, StringComparison.Ordinal))
        {
            return (null, "aiops.observer.adapter.provenance_identity_untrusted");
        }

        if (!VerifySignature(
                trustConfiguration.GrantTrustAnchor,
                ObserverPolicyProvenancePayload.CreateGrant(grant),
                context.CopyGrantSignature()) ||
            !VerifySignature(
                trustConfiguration.RevocationTrustAnchor,
                ObserverPolicyProvenancePayload.CreateRevocation(revocation),
                context.CopyRevocationSignature()))
        {
            return (null, "aiops.observer.adapter.provenance_integrity_invalid");
        }

        if (asOf < grant.IssuedAt || asOf >= grant.ExpiresAt)
        {
            return (null, "aiops.observer.adapter.provenance_grant_inactive");
        }

        if (asOf < revocation.IssuedAt || asOf >= revocation.ExpiresAt)
        {
            return (null, "aiops.observer.adapter.provenance_revocation_stale");
        }

        if (revocation.IsRevoked(grant.GrantId, grant.SigningKeyId))
        {
            return (null, "aiops.observer.adapter.provenance_revoked");
        }

        if (grant.Policy.DataUse != requiredDataUse)
        {
            return (null, "aiops.observer.adapter.policy_purpose_mismatch");
        }

        return (grant.Policy, null);
    }

    /// <summary>Checks that an assertion names exactly the configured issuer and key.</summary>
    /// <param name="issuerId">Asserted issuer.</param>
    /// <param name="signingKeyId">Asserted signing key.</param>
    /// <param name="anchor">Purpose-specific trust anchor.</param>
    /// <returns><see langword="true"/> only for exact ordinal identity matches.</returns>
    private static bool MatchesIdentity(string issuerId, string signingKeyId, ObserverPolicyTrustAnchor anchor) =>
        string.Equals(issuerId, anchor.IssuerId, StringComparison.Ordinal) &&
        string.Equals(signingKeyId, anchor.SigningKeyId, StringComparison.Ordinal);

    /// <summary>Verifies one ECDSA P-256 SHA-256 signature and rejects malformed or differently sized keys.</summary>
    /// <param name="anchor">Trusted public-key material.</param>
    /// <param name="payload">Canonical signed bytes.</param>
    /// <param name="signature">Caller-provided signature bytes.</param>
    /// <returns><see langword="true"/> only for a valid signature under an exact P-256 key.</returns>
    private static bool VerifySignature(ObserverPolicyTrustAnchor anchor, byte[] payload, byte[] signature)
    {
        try
        {
            using ECDsa verifier = ECDsa.Create();
            byte[] publicKey = anchor.CopySubjectPublicKeyInfo();
            verifier.ImportSubjectPublicKeyInfo(publicKey, out int bytesRead);
            return bytesRead == publicKey.Length &&
                verifier.KeySize == 256 &&
                verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}

/// <summary>Provides shared UTC validation for provenance contracts.</summary>
internal static class ObserverPolicyProvenanceGuard
{
    /// <summary>Rejects default or offset-bearing provenance instants.</summary>
    /// <param name="value">Instant to validate.</param>
    /// <param name="parameterName">Public parameter name used by exceptions.</param>
    /// <exception cref="ArgumentException">Thrown when the instant is default or not expressed as UTC.</exception>
    public static void ValidateUtcInstant(DateTimeOffset value, string parameterName)
    {
        if (value == default || value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observer provenance instants must be explicit UTC values.", parameterName);
        }
    }
}
