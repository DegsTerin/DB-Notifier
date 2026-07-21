// Module purpose: Validates and coordinates fail-closed Agent Fleet enrollment, heartbeat and read-only access without owning HTTP or persistence.
using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.Access;

namespace DBNotifier.Application.AgentFleet;

/// <summary>
/// Defines the opaque one-time token format and its salted digest so provisioning and consumption use one contract.
/// </summary>
public static class AgentEnrollmentTokenProof
{
    /// <summary>Exact digest identifier persisted with each enrollment token.</summary>
    public const string HashAlgorithm = "SHA256";

    /// <summary>Required high-entropy token secret length.</summary>
    public const int SecretLength = 32;

    /// <summary>Required per-token random salt length.</summary>
    public const int SaltLength = 16;

    private static readonly byte[] Domain = Encoding.ASCII.GetBytes("DB-Notifier.AgentEnrollmentToken.v1\0");

    /// <summary>
    /// Extracts the non-secret token identifier without validating or persisting the secret portion.
    /// </summary>
    /// <param name="token">Token in <c>32-hex-guid.base64url-secret</c> form.</param>
    /// <param name="enrollmentTokenId">Parsed public identifier when valid.</param>
    /// <returns><see langword="true"/> only for the exact bounded format.</returns>
    public static bool TryGetTokenId(string? token, out Guid enrollmentTokenId) =>
        TryParse(token, out enrollmentTokenId, out byte[]? secret) && ClearAndReturn(secret);

    /// <summary>
    /// Computes the salted token digest used for persistence. The original token is never returned or retained.
    /// </summary>
    /// <param name="token">Exact high-entropy token.</param>
    /// <param name="salt">Per-token random salt.</param>
    /// <returns>SHA-256 digest bytes.</returns>
    /// <exception cref="ArgumentException">Thrown when token or salt is outside the fixed format.</exception>
    public static byte[] ComputeSecretHash(string token, ReadOnlySpan<byte> salt)
    {
        if (salt.Length != SaltLength || !TryParse(token, out _, out byte[]? secret))
        {
            throw new ArgumentException("Enrollment token proof input is outside policy.");
        }

        try
        {
            return ComputeSecretHash(secret, salt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>Parses the fixed token and transfers ownership of the secret bytes to the caller.</summary>
    /// <param name="token">Untrusted token text.</param>
    /// <param name="enrollmentTokenId">Parsed public identifier.</param>
    /// <param name="secret">New secret buffer which the caller must clear.</param>
    /// <returns><see langword="true"/> when parsing succeeds.</returns>
    internal static bool TryParse(string? token, out Guid enrollmentTokenId, out byte[]? secret)
    {
        enrollmentTokenId = Guid.Empty;
        secret = null;
        if (token is null || token.Length != 76 || token[32] != '.' ||
            !Guid.TryParseExact(token.AsSpan(0, 32), "N", out enrollmentTokenId))
        {
            return false;
        }

        ReadOnlySpan<char> encoded = token.AsSpan(33);
        Span<char> padded = stackalloc char[44];
        for (int index = 0; index < encoded.Length; index++)
        {
            char value = encoded[index];
            if (!(char.IsAsciiLetterOrDigit(value) || value is '-' or '_'))
            {
                enrollmentTokenId = Guid.Empty;
                return false;
            }

            padded[index] = value switch
            {
                '-' => '+',
                '_' => '/',
                _ => value,
            };
        }

        padded[^1] = '=';
        byte[] candidate = new byte[SecretLength];
        if (!Convert.TryFromBase64Chars(padded, candidate, out int written) || written != SecretLength)
        {
            CryptographicOperations.ZeroMemory(candidate);
            enrollmentTokenId = Guid.Empty;
            return false;
        }

        secret = candidate;
        return true;
    }

    /// <summary>Computes the domain-separated hash from already parsed secret bytes.</summary>
    /// <param name="secret">Exact high-entropy secret bytes.</param>
    /// <param name="salt">Exact per-token salt.</param>
    /// <returns>New SHA-256 digest bytes.</returns>
    internal static byte[] ComputeSecretHash(ReadOnlySpan<byte> secret, ReadOnlySpan<byte> salt)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        hash.AppendData(Domain);
        hash.AppendData(salt);
        hash.AppendData(secret);
        return hash.GetHashAndReset();
    }

    /// <summary>Clears a parsed secret while preserving the boolean expression used by the public identifier parser.</summary>
    /// <param name="secret">Owned secret buffer.</param>
    /// <returns><see langword="true"/> after clearing a non-null buffer.</returns>
    private static bool ClearAndReturn(byte[]? secret)
    {
        if (secret is null)
        {
            return false;
        }

        CryptographicOperations.ZeroMemory(secret);
        return true;
    }
}

/// <summary>Coordinates Agent Fleet validation and delegates every durable decision to the server-side store.</summary>
/// <param name="store">Durable Agent Fleet persistence and authorisation boundary.</param>
/// <param name="certificateIssuer">Certificate issuer port; production remains unavailable until separately configured.</param>
/// <param name="timeProvider">Trusted server clock.</param>
public sealed partial class AgentFleetService(
    IAgentFleetStore store,
    IAgentCertificateIssuer certificateIssuer,
    TimeProvider timeProvider)
{
    private const int MaximumCertificateSigningRequestBytes = 8192;
    private const int MaximumCertificateBytes = 16384;
    private const long MaximumReportedQueueDepth = int.MaxValue;

    /// <summary>
    /// Validates one token-bound request, obtains a public client certificate and atomically consumes the token.
    /// </summary>
    /// <param name="enrollmentToken">One-time token supplied outside the JSON body.</param>
    /// <param name="request">Non-secret enrollment body.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Safe enrollment outcome with generic token denial semantics.</returns>
    public async ValueTask<AgentEnrollmentOutcome> EnrollAsync(
        string? enrollmentToken,
        AgentEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (!TryValidateEnrollmentRequest(request, now, out AgentCertificateIssueRequest? issueRequest))
        {
            await store.AuditEnrollmentDenialAsync(
                "enrollment.request_invalid",
                now,
                cancellationToken).ConfigureAwait(false);
            return EnrollmentFailure(AgentEnrollmentDisposition.Invalid, "enrollment.request_invalid");
        }

        AgentCertificateIssueRequest validatedIssueRequest = issueRequest!;

        if (!AgentEnrollmentTokenProof.TryParse(
                enrollmentToken,
                out Guid enrollmentTokenId,
                out byte[]? tokenSecret))
        {
            await store.AuditEnrollmentDenialAsync(
                "enrollment.token_invalid",
                now,
                cancellationToken).ConfigureAwait(false);
            return EnrollmentFailure(AgentEnrollmentDisposition.Denied, "enrollment.denied");
        }

        try
        {
            AgentEnrollmentTokenChallenge? challenge = await store
                .GetEnrollmentTokenAsync(enrollmentTokenId, cancellationToken)
                .ConfigureAwait(false);
            if (challenge is null || !ChallengeMatches(
                    challenge,
                    validatedIssueRequest,
                    now,
                    tokenSecret,
                    out byte[]? proof))
            {
                await store.AuditEnrollmentDenialAsync(
                    "enrollment.token_or_scope_denied",
                    now,
                    cancellationToken).ConfigureAwait(false);
                return EnrollmentFailure(AgentEnrollmentDisposition.Denied, "enrollment.denied");
            }

            byte[] validatedProof = proof!;
            using (new SensitiveBufferScope(validatedProof))
            {
                AgentCertificateIssueResult certificate = await certificateIssuer
                    .IssueAsync(validatedIssueRequest, now, cancellationToken)
                    .ConfigureAwait(false);
                if (certificate.Disposition == AgentCertificateIssueDisposition.Unavailable)
                {
                    await store.AuditEnrollmentDenialAsync(
                        "enrollment.issuer_unavailable",
                        now,
                        cancellationToken).ConfigureAwait(false);
                    return EnrollmentFailure(
                        AgentEnrollmentDisposition.IssuerUnavailable,
                        "enrollment.issuer_unavailable");
                }

                if (certificate.Disposition != AgentCertificateIssueDisposition.Issued ||
                    !ValidateIssuedCertificate(
                        certificate,
                        validatedIssueRequest,
                        now))
                {
                    await store.AuditEnrollmentDenialAsync(
                        "enrollment.csr_invalid",
                        now,
                        cancellationToken).ConfigureAwait(false);
                    return EnrollmentFailure(AgentEnrollmentDisposition.Invalid, "enrollment.csr_invalid");
                }

                AgentEnrollmentCommitResult committed = await store.CompleteEnrollmentAsync(
                    new AgentEnrollmentCommit(
                        challenge.EnrollmentTokenId,
                        challenge.ConcurrencyToken,
                        validatedProof,
                        validatedIssueRequest,
                        request.MessageId,
                        request.OccurredAt,
                        request.SentAt,
                        certificate),
                    now,
                    cancellationToken).ConfigureAwait(false);
                return committed.Disposition switch
                {
                    AgentEnrollmentCommitDisposition.Enrolled => new AgentEnrollmentOutcome(
                        AgentEnrollmentDisposition.Enrolled,
                        committed.AgentId,
                        certificate.CertificateDer,
                        certificate.NotAfter,
                        null),
                    AgentEnrollmentCommitDisposition.Conflict => EnrollmentFailure(
                        AgentEnrollmentDisposition.Conflict,
                        "enrollment.conflict"),
                    _ => EnrollmentFailure(AgentEnrollmentDisposition.Denied, "enrollment.denied"),
                };
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenSecret);
        }
    }

    /// <summary>Validates and records one heartbeat under a durable per-Agent anti-replay cursor.</summary>
    /// <param name="request">Untrusted heartbeat body.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Accepted, duplicate, gapped or fail-closed heartbeat outcome.</returns>
    public ValueTask<AgentHeartbeatOutcome> RecordHeartbeatAsync(
        AgentHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = timeProvider.GetUtcNow();
        AgentHeartbeatDisposition validation = ValidateHeartbeat(request, now);
        if (validation != AgentHeartbeatDisposition.Accepted)
        {
            return ValueTask.FromResult(new AgentHeartbeatOutcome(
                validation,
                null,
                0,
                null,
                validation == AgentHeartbeatDisposition.Incompatible
                    ? "protocol.version_unsupported"
                    : "heartbeat.request_invalid"));
        }

        return store.RecordHeartbeatAsync(
            new ValidatedAgentHeartbeat(request, ComputeHeartbeatDigest(request)),
            now,
            cancellationToken);
    }

    /// <summary>Returns one complete read-only assignment snapshot for the authenticated Agent.</summary>
    /// <param name="agentId">Authenticated Agent identifier.</param>
    /// <param name="agentVersion">Bounded Agent version header.</param>
    /// <param name="afterVersion">Optional exact version already held by the Agent.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Complete snapshot, not-modified result or fail-closed refusal.</returns>
    public ValueTask<AgentAssignmentOutcome> GetAssignmentsAsync(
        Guid agentId,
        string agentVersion,
        string? afterVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        if (!IsStableIdentifier(agentVersion, 1, 64) ||
            (afterVersion is not null && !IsUpperHex(afterVersion, 64)))
        {
            throw new ArgumentException("Agent assignment request is outside policy.");
        }

        return store.GetAssignmentsAsync(
            agentId,
            agentVersion,
            afterVersion,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    /// <summary>Returns safe Agent Fleet facts within one authenticated human's RBAC scope.</summary>
    /// <param name="actor">Authenticated human actor.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Scoped Agent catalogue.</returns>
    public ValueTask<AgentFleetCatalogue> GetCatalogueAsync(
        HumanActor actor,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        return store.GetAuthorisedAgentsAsync(
            actor.SubjectId,
            PlatformPermissions.AgentsRead,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    /// <summary>Revokes an Agent and all current client certificates under server-side RBAC.</summary>
    /// <param name="actor">Authenticated human actor.</param>
    /// <param name="agentId">Target Agent identifier.</param>
    /// <param name="request">Bounded machine-readable reason.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Monotonic revocation result.</returns>
    public ValueTask<AgentRevocationOutcome> RevokeAsync(
        HumanActor actor,
        Guid agentId,
        AgentRevocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentOutOfRangeException.ThrowIfEqual(agentId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request);
        if (!IsStableIdentifier(request.ReasonCode, 3, 100))
        {
            throw new ArgumentException("Agent revocation reason is outside policy.", nameof(request));
        }

        return store.RevokeAgentAsync(
            actor.SubjectId,
            agentId,
            PlatformPermissions.AgentsRevoke,
            request.ReasonCode,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    /// <summary>Validates request shape and decodes the bounded public CSR.</summary>
    /// <param name="request">Untrusted enrollment body.</param>
    /// <param name="now">Trusted decision instant.</param>
    /// <param name="validated">Validated issuer request.</param>
    /// <returns><see langword="true"/> only when every field is admissible.</returns>
    private static bool TryValidateEnrollmentRequest(
        AgentEnrollmentRequest request,
        DateTimeOffset now,
        out AgentCertificateIssueRequest? validated)
    {
        validated = null;
        if (request.MessageId == Guid.Empty || request.SchemaVersion != AgentFleetProtocol.CurrentSchemaVersion ||
            !IsStableIdentifier(request.InstallationId, 8, 160) ||
            !IsSafeDisplayName(request.DisplayName, 1, 200) ||
            !IsStableIdentifier(request.Environment, 1, 100) ||
            !IsStableIdentifier(request.Platform, 1, 100) ||
            !IsStableIdentifier(request.AgentVersion, 1, 64) ||
            !IsStableIdentifier(request.RequestedScope, 1, 160) ||
            !IsExplicitUtc(request.OccurredAt) || !IsExplicitUtc(request.SentAt) ||
            request.OccurredAt > request.SentAt ||
            request.SentAt - now > AgentFleetProtocol.MaximumFutureClockSkew ||
            string.IsNullOrWhiteSpace(request.CertificateSigningRequest) ||
            request.CertificateSigningRequest.Length > 12000)
        {
            return false;
        }

        byte[] csr;
        try
        {
            csr = Convert.FromBase64String(request.CertificateSigningRequest);
        }
        catch (FormatException)
        {
            return false;
        }

        if (csr.Length is < 128 or > MaximumCertificateSigningRequestBytes)
        {
            return false;
        }

        byte[] digest = SHA256.HashData(csr);
        validated = new AgentCertificateIssueRequest(
            csr,
            digest,
            request.InstallationId,
            request.DisplayName.Trim(),
            request.Environment,
            request.Platform,
            request.AgentVersion,
            request.RequestedScope);
        return true;
    }

    /// <summary>Checks the token proof, one-time lifecycle and every exact scope binding.</summary>
    /// <param name="challenge">Stored token challenge.</param>
    /// <param name="request">Validated request.</param>
    /// <param name="now">Trusted decision instant.</param>
    /// <param name="secret">Presented high-entropy secret.</param>
    /// <param name="proof">New computed digest, owned by the caller when successful.</param>
    /// <returns><see langword="true"/> when the challenge is current and exact.</returns>
    private static bool ChallengeMatches(
        AgentEnrollmentTokenChallenge challenge,
        AgentCertificateIssueRequest request,
        DateTimeOffset now,
        ReadOnlySpan<byte> secret,
        out byte[]? proof)
    {
        proof = null;
        if (!string.Equals(challenge.HashAlgorithm, AgentEnrollmentTokenProof.HashAlgorithm, StringComparison.Ordinal) ||
            challenge.Salt.Length != AgentEnrollmentTokenProof.SaltLength ||
            challenge.SecretHash.Length != SHA256.HashSizeInBytes ||
            challenge.ConsumedAt is not null || challenge.RevokedAt is not null ||
            !IsExplicitUtc(challenge.IssuedAt) || !IsExplicitUtc(challenge.ExpiresAt) ||
            challenge.IssuedAt > now || challenge.ExpiresAt <= now ||
            !string.Equals(challenge.ExpectedInstallationId, request.InstallationId, StringComparison.Ordinal) ||
            !string.Equals(challenge.ExpectedEnvironment, request.Environment, StringComparison.Ordinal) ||
            !string.Equals(challenge.ExpectedPlatform, request.Platform, StringComparison.Ordinal) ||
            !string.Equals(challenge.Scope, request.RequestedScope, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] candidate = AgentEnrollmentTokenProof.ComputeSecretHash(secret, challenge.Salt.Span);
        if (!CryptographicOperations.FixedTimeEquals(candidate, challenge.SecretHash.Span))
        {
            CryptographicOperations.ZeroMemory(candidate);
            return false;
        }

        proof = candidate;
        return true;
    }

    /// <summary>Validates public certificate contents independently of issuer-provided metadata.</summary>
    /// <param name="result">Issuer result.</param>
    /// <param name="request">Exact validated CSR and its independently computed digest.</param>
    /// <param name="now">Trusted issuance instant.</param>
    /// <returns><see langword="true"/> only for a P-256 client certificate matching every reported value.</returns>
    private static bool ValidateIssuedCertificate(
        AgentCertificateIssueResult result,
        AgentCertificateIssueRequest request,
        DateTimeOffset now)
    {
        if (result.CertificateDer.Length is < 128 or > MaximumCertificateBytes ||
            result.CertificateSigningRequestSha256.Length != SHA256.HashSizeInBytes ||
            !CryptographicOperations.FixedTimeEquals(
                result.CertificateSigningRequestSha256.Span,
                request.CertificateSigningRequestSha256.Span) ||
            !IsUpperHex(result.CertificateThumbprint, 40, 160) ||
            !IsUpperHex(result.PublicKeySha256, 64) ||
            result.NotBefore is null || result.NotAfter is null ||
            !IsExplicitUtc(result.NotBefore.Value) || !IsExplicitUtc(result.NotAfter.Value) ||
            result.NotBefore.Value - now > AgentFleetProtocol.MaximumFutureClockSkew ||
            result.NotAfter.Value <= now)
        {
            return false;
        }

        try
        {
            CertificateRequest signingRequest = CertificateRequest.LoadSigningRequest(
                request.CertificateSigningRequestDer.ToArray(),
                HashAlgorithmName.SHA256,
                CertificateRequestLoadOptions.Default);
            using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(result.CertificateDer.Span);
            if (certificate.HasPrivateKey ||
                !string.Equals(NormaliseThumbprint(certificate.Thumbprint), result.CertificateThumbprint, StringComparison.Ordinal) ||
                certificate.NotBefore.ToUniversalTime() != result.NotBefore.Value.UtcDateTime ||
                certificate.NotAfter.ToUniversalTime() != result.NotAfter.Value.UtcDateTime ||
                !HasClientCertificateConstraints(certificate))
            {
                return false;
            }

            using ECDsa? publicKey = certificate.GetECDsaPublicKey();
            if (publicKey is null || publicKey.KeySize != 256 ||
                publicKey.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7")
            {
                return false;
            }

            string publicKeyDigest = Convert.ToHexString(SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
            return string.Equals(publicKeyDigest, result.PublicKeySha256, StringComparison.Ordinal) &&
                CryptographicOperations.FixedTimeEquals(
                    signingRequest.PublicKey.ExportSubjectPublicKeyInfo(),
                    publicKey.ExportSubjectPublicKeyInfo());
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Checks non-CA, digital-signature and client-auth certificate constraints.</summary>
    /// <param name="certificate">Public certificate to inspect.</param>
    /// <returns><see langword="true"/> when every required extension is present.</returns>
    private static bool HasClientCertificateConstraints(X509Certificate2 certificate)
    {
        X509BasicConstraintsExtension? basic = certificate.Extensions.OfType<X509BasicConstraintsExtension>().SingleOrDefault();
        X509KeyUsageExtension? usage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
        X509EnhancedKeyUsageExtension? enhanced = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
        return basic is { CertificateAuthority: false } &&
            usage is not null && usage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) &&
            enhanced is not null && enhanced.EnhancedKeyUsages
                .Cast<Oid>()
                .Any(oid => oid.Value == "1.3.6.1.5.5.7.3.2");
    }

    /// <summary>Validates heartbeat shape, timestamps, queue claims and protocol overlap.</summary>
    /// <param name="request">Untrusted heartbeat.</param>
    /// <param name="now">Trusted server time.</param>
    /// <returns>Accepted validation marker, incompatible result or invalid result.</returns>
    private static AgentHeartbeatDisposition ValidateHeartbeat(AgentHeartbeatRequest request, DateTimeOffset now)
    {
        if (request.ProtocolMinimum < 1 || request.ProtocolMaximum < request.ProtocolMinimum ||
            AgentFleetProtocol.CurrentProtocolVersion < request.ProtocolMinimum ||
            AgentFleetProtocol.CurrentProtocolVersion > request.ProtocolMaximum)
        {
            return AgentHeartbeatDisposition.Incompatible;
        }

        bool queueShapeValid = request.QueueDepth switch
        {
            0 => request.OldestQueuedAt is null,
            > 0 and <= MaximumReportedQueueDepth => request.OldestQueuedAt is not null,
            _ => false,
        };
        if (request.MessageId == Guid.Empty || request.AgentId == Guid.Empty ||
            request.SchemaVersion != AgentFleetProtocol.CurrentSchemaVersion || request.Sequence < 1 ||
            !IsStableIdentifier(request.AgentVersion, 1, 64) || !queueShapeValid ||
            !IsExplicitUtc(request.OccurredAt) || !IsExplicitUtc(request.SentAt) ||
            !IsExplicitUtc(request.AgentTime) ||
            (request.OldestQueuedAt is not null && !IsExplicitUtc(request.OldestQueuedAt.Value)) ||
            request.OccurredAt > request.SentAt ||
            request.SentAt - now > AgentFleetProtocol.MaximumFutureClockSkew ||
            request.AgentTime - now > AgentFleetProtocol.MaximumFutureClockSkew ||
            request.OldestQueuedAt > request.SentAt + AgentFleetProtocol.MaximumFutureClockSkew)
        {
            return AgentHeartbeatDisposition.Invalid;
        }

        return AgentHeartbeatDisposition.Accepted;
    }

    /// <summary>Computes the canonical heartbeat digest used for exact duplicate comparison.</summary>
    /// <param name="request">Validated heartbeat.</param>
    /// <returns>Uppercase SHA-256 hexadecimal digest.</returns>
    private static string ComputeHeartbeatDigest(AgentHeartbeatRequest request)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("messageId", request.MessageId);
            writer.WriteNumber("schemaVersion", request.SchemaVersion);
            writer.WriteString("agentId", request.AgentId);
            writer.WriteNumber("sequence", request.Sequence);
            writer.WriteString("occurredAt", request.OccurredAt.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("sentAt", request.SentAt.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteString("agentVersion", request.AgentVersion);
            writer.WriteNumber("protocolMinimum", request.ProtocolMinimum);
            writer.WriteNumber("protocolMaximum", request.ProtocolMaximum);
            writer.WriteNumber("queueDepth", request.QueueDepth);
            if (request.OldestQueuedAt is null)
            {
                writer.WriteNull("oldestQueuedAt");
            }
            else
            {
                writer.WriteString(
                    "oldestQueuedAt",
                    request.OldestQueuedAt.Value.ToString("O", CultureInfo.InvariantCulture));
            }

            writer.WriteString("agentTime", request.AgentTime.ToString("O", CultureInfo.InvariantCulture));
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(buffer.WrittenSpan));
    }

    /// <summary>Validates an authenticated human actor at the Application boundary.</summary>
    /// <param name="actor">Actor to validate.</param>
    private static void ValidateActor(HumanActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(actor.SubjectId);
        if (actor.SubjectId.Length > 300)
        {
            throw new ArgumentException("Human subject identifier is outside policy.", nameof(actor));
        }
    }

    /// <summary>Checks bounded stable identifiers without accepting whitespace or control characters.</summary>
    /// <param name="value">Candidate identifier.</param>
    /// <param name="minimumLength">Minimum admitted length.</param>
    /// <param name="maximumLength">Maximum admitted length.</param>
    /// <returns><see langword="true"/> for an exact safe identifier.</returns>
    private static bool IsStableIdentifier(string? value, int minimumLength, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length >= minimumLength && value.Length <= maximumLength &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    /// <summary>Checks a bounded display value while excluding controls and untrimmed input.</summary>
    /// <param name="value">Candidate display value.</param>
    /// <param name="minimumLength">Minimum admitted length.</param>
    /// <param name="maximumLength">Maximum admitted length.</param>
    /// <returns><see langword="true"/> for a safe bounded display value.</returns>
    private static bool IsSafeDisplayName(string? value, int minimumLength, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value == value.Trim() &&
        value.Length >= minimumLength && value.Length <= maximumLength &&
        value.All(character => !char.IsControl(character));

    /// <summary>Checks an uppercase hexadecimal value with an exact length.</summary>
    /// <param name="value">Candidate text.</param>
    /// <param name="exactLength">Required length.</param>
    /// <returns><see langword="true"/> for exact uppercase hexadecimal text.</returns>
    private static bool IsUpperHex(string? value, int exactLength) => IsUpperHex(value, exactLength, exactLength);

    /// <summary>Checks an uppercase hexadecimal value within an inclusive length range.</summary>
    /// <param name="value">Candidate text.</param>
    /// <param name="minimumLength">Minimum length.</param>
    /// <param name="maximumLength">Maximum length.</param>
    /// <returns><see langword="true"/> for bounded uppercase hexadecimal text.</returns>
    private static bool IsUpperHex(string? value, int minimumLength, int maximumLength) =>
        value is not null && value.Length >= minimumLength && value.Length <= maximumLength &&
        value.All(character => char.IsAsciiDigit(character) || character is >= 'A' and <= 'F');

    /// <summary>Checks that a timestamp is non-default and explicitly UTC.</summary>
    /// <param name="value">Timestamp to inspect.</param>
    /// <returns><see langword="true"/> only for explicit UTC.</returns>
    private static bool IsExplicitUtc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;

    /// <summary>Normalises an X.509 thumbprint for exact persistence comparison.</summary>
    /// <param name="thumbprint">Framework-provided thumbprint.</param>
    /// <returns>Uppercase hexadecimal text without separators.</returns>
    private static string NormaliseThumbprint(string thumbprint) =>
        thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    /// <summary>Creates a certificate-free enrollment failure result.</summary>
    /// <param name="disposition">Typed failure disposition.</param>
    /// <param name="errorCode">Stable sanitised code.</param>
    /// <returns>Failure outcome containing no certificate bytes.</returns>
    private static AgentEnrollmentOutcome EnrollmentFailure(
        AgentEnrollmentDisposition disposition,
        string errorCode) =>
        new(disposition, null, ReadOnlyMemory<byte>.Empty, null, errorCode);

    /// <summary>Clears one sensitive byte buffer at the end of a lexical scope.</summary>
    /// <param name="buffer">Owned digest buffer.</param>
    private sealed class SensitiveBufferScope(byte[] buffer) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose() => CryptographicOperations.ZeroMemory(buffer);
    }
}
