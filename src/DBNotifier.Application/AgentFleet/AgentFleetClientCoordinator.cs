// Module purpose: Coordinates test-only Agent enrollment, durable heartbeat delivery and read-only assignment reconciliation without provider execution.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DBNotifier.Application.AgentFleet;

/// <summary>
/// Coordinates the bounded Agent-side portion of Agent Fleet v1. It owns no network, key store or database and
/// cannot execute monitoring, providers, commands or notifications.
/// </summary>
/// <param name="identityStore">Test-only private-identity boundary.</param>
/// <param name="transport">Bounded Agent Fleet v1 transport.</param>
/// <param name="localStore">Durable non-secret Agent-local state.</param>
/// <param name="timeProvider">Trusted clock used for validation and evidence.</param>
/// <param name="assignmentFreshness">Bounded validity interval for the last-known-valid snapshot.</param>
public sealed class AgentFleetClientCoordinator(
    IAgentEnrollmentIdentityStore identityStore,
    IAgentFleetClientTransport transport,
    IAgentFleetLocalStore localStore,
    TimeProvider timeProvider,
    TimeSpan assignmentFreshness)
{
    private readonly TimeSpan boundedAssignmentFreshness = ValidateFreshness(assignmentFreshness);

    /// <summary>Enrolls one Agent with bootstrap material supplied only by an authorised E2E fixture.</summary>
    /// <param name="bootstrap">In-memory one-time bootstrap.</param>
    /// <param name="cancellationToken">Cancellation propagated through key, transport and store boundaries.</param>
    /// <returns>Sanitised enrollment result.</returns>
    public async ValueTask<AgentFleetClientResult> EnrolForTestAsync(
        AgentEnrollmentBootstrap bootstrap,
        CancellationToken cancellationToken)
    {
        ValidateBootstrap(bootstrap);
        AgentLocalRegistration? existing = await localStore
            .GetRegistrationAsync(cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            bool exact = string.Equals(existing.InstallationId, bootstrap.InstallationId, StringComparison.Ordinal) &&
                string.Equals(existing.Environment, bootstrap.Environment, StringComparison.Ordinal);
            return new AgentFleetClientResult(
                exact && existing.State == AgentLocalIdentityState.Active,
                exact ? existing.State : AgentLocalIdentityState.Conflict,
                exact ? "enrollment.already_enrolled" : "enrollment.local_identity_conflict");
        }

        AgentEnrollmentKeyMaterial keyMaterial = await identityStore
            .BeginAsync(bootstrap.InstallationId, cancellationToken)
            .ConfigureAwait(false);
        AgentIdentityCompletion? completion = null;
        try
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            AgentEnrollmentRequest request = new(
                Guid.NewGuid(),
                AgentFleetProtocol.CurrentSchemaVersion,
                bootstrap.InstallationId,
                bootstrap.DisplayName,
                bootstrap.Environment,
                bootstrap.Platform,
                bootstrap.AgentVersion,
                bootstrap.RequestedScope,
                now,
                now,
                keyMaterial.CertificateSigningRequest);
            AgentFleetTransportResult<AgentEnrollmentOutcome> response = await transport
                .EnrolAsync(bootstrap.Token, request, cancellationToken)
                .ConfigureAwait(false);
            if (response.Disposition != AgentFleetTransportDisposition.Succeeded ||
                response.Value is not { Disposition: AgentEnrollmentDisposition.Enrolled } outcome ||
                outcome.AgentId is not Guid agentId || agentId == Guid.Empty ||
                outcome.CertificateNotAfter is not DateTimeOffset certificateNotAfter ||
                !TryValidateIssuedCertificate(
                    keyMaterial.CertificateSigningRequest,
                    outcome.CertificateDer,
                    certificateNotAfter,
                    now,
                    out string certificateThumbprint))
            {
                return new AgentFleetClientResult(
                    false,
                    MapFailureState(response.Disposition),
                    response.ErrorCode ?? response.Value?.ErrorCode ?? "enrollment.response_invalid");
            }

            completion = await identityStore
                .CompleteAsync(keyMaterial.OperationId, outcome.CertificateDer, cancellationToken)
                .ConfigureAwait(false);
            if (!string.Equals(completion.CertificateThumbprint, certificateThumbprint, StringComparison.Ordinal))
            {
                await identityStore.RemoveAsync(completion.IdentityReference, cancellationToken).ConfigureAwait(false);
                completion = null;
                return new AgentFleetClientResult(
                    false,
                    AgentLocalIdentityState.Conflict,
                    "enrollment.identity_store_conflict");
            }

            AgentLocalRegistration registration = new(
                agentId,
                bootstrap.InstallationId,
                bootstrap.Environment,
                completion.IdentityReference,
                certificateThumbprint,
                certificateNotAfter,
                now,
                AgentLocalIdentityState.Active,
                null);
            try
            {
                await localStore.SaveEnrollmentAsync(registration, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await identityStore.RemoveAsync(completion.IdentityReference, CancellationToken.None).ConfigureAwait(false);
                completion = null;
                throw;
            }

            return new AgentFleetClientResult(true, AgentLocalIdentityState.Active, "enrollment.enrolled");
        }
        finally
        {
            if (completion is null)
            {
                await identityStore.AbortAsync(keyMaterial.OperationId, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Sends or exactly replays one durable heartbeat without inferring instance health.</summary>
    /// <param name="agentVersion">Bounded Agent version reported to the Server.</param>
    /// <param name="operationLease">Exact local owner and fence required for every durable mutation.</param>
    /// <param name="cancellationToken">Cancellation propagated through store and transport boundaries.</param>
    /// <returns>Sanitised heartbeat result.</returns>
    public async ValueTask<AgentFleetClientResult> SendHeartbeatOnceAsync(
        string agentVersion,
        AgentFleetOperationLease operationLease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operationLease);
        ValidateAgentVersion(agentVersion);
        AgentLocalRegistration? registration = await localStore
            .GetRegistrationAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentFleetClientResult? unavailable = await CheckActiveRegistrationAsync(
                registration,
                operationLease,
                cancellationToken)
            .ConfigureAwait(false);
        if (unavailable is not null)
        {
            return unavailable;
        }

        AgentHeartbeatQueueEvidence queue = await localStore
            .GetQueueEvidenceAsync(cancellationToken)
            .ConfigureAwait(false);
        DateTimeOffset now = timeProvider.GetUtcNow();
        PendingAgentHeartbeat pending = await localStore
            .GetOrCreatePendingHeartbeatAsync(
                registration!,
                agentVersion,
                queue,
                now,
                operationLease,
                cancellationToken)
            .ConfigureAwait(false);
        AgentFleetTransportResult<AgentHeartbeatOutcome> response = await transport
            .SendHeartbeatAsync(
                registration!.IdentityReference,
                pending.Request,
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Disposition == AgentFleetTransportDisposition.Succeeded &&
            response.Value is { } outcome &&
            outcome.Disposition is AgentHeartbeatDisposition.Accepted or
                AgentHeartbeatDisposition.Duplicate or
                AgentHeartbeatDisposition.AcceptedWithGap &&
            outcome.HighestAcceptedSequence == pending.Request.Sequence &&
            outcome.AcceptedAt is not null)
        {
            await localStore.AcknowledgeHeartbeatAsync(
                    pending.Request,
                    outcome,
                    timeProvider.GetUtcNow(),
                    operationLease,
                    cancellationToken)
                .ConfigureAwait(false);
            return new AgentFleetClientResult(true, AgentLocalIdentityState.Active, "heartbeat.accepted");
        }

        AgentLocalIdentityState state = response.Disposition switch
        {
            AgentFleetTransportDisposition.TransientFailure => AgentLocalIdentityState.Offline,
            AgentFleetTransportDisposition.Denied => AgentLocalIdentityState.RevokedOrDenied,
            AgentFleetTransportDisposition.Incompatible => AgentLocalIdentityState.Incompatible,
            _ when response.Value?.Disposition == AgentHeartbeatDisposition.AgentInactive =>
                AgentLocalIdentityState.RevokedOrDenied,
            _ when response.Value?.Disposition == AgentHeartbeatDisposition.Incompatible =>
                AgentLocalIdentityState.Incompatible,
            _ => AgentLocalIdentityState.Conflict,
        };
        string code = response.ErrorCode ?? response.Value?.ErrorCode ?? "heartbeat.response_invalid";
        await localStore.SetIdentityStateAsync(
            registration.AgentId,
            state,
            code,
            timeProvider.GetUtcNow(),
            operationLease,
            cancellationToken).ConfigureAwait(false);
        return new AgentFleetClientResult(
            false,
            state,
            code,
            response.Disposition == AgentFleetTransportDisposition.TransientFailure,
            response.RetryAfter);
    }

    /// <summary>Reads and atomically applies one complete assignment snapshot or preserves the current LKG.</summary>
    /// <param name="agentVersion">Bounded Agent version used for protocol negotiation.</param>
    /// <param name="operationLease">Exact local owner and fence required for every durable mutation.</param>
    /// <param name="cancellationToken">Cancellation propagated through store and transport boundaries.</param>
    /// <returns>Sanitised reconciliation result.</returns>
    public async ValueTask<AgentFleetClientResult> ReconcileAssignmentsOnceAsync(
        string agentVersion,
        AgentFleetOperationLease operationLease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operationLease);
        ValidateAgentVersion(agentVersion);
        AgentLocalRegistration? registration = await localStore
            .GetRegistrationAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentFleetClientResult? unavailable = await CheckActiveRegistrationAsync(
                registration,
                operationLease,
                cancellationToken)
            .ConfigureAwait(false);
        if (unavailable is not null)
        {
            return unavailable;
        }

        AgentAssignmentLocalState current = await localStore
            .GetAssignmentStateAsync(registration!.AgentId, cancellationToken)
            .ConfigureAwait(false);
        AgentFleetTransportResult<AgentAssignmentSnapshot> response = await transport
            .GetAssignmentsAsync(
                registration.IdentityReference,
                registration.AgentId,
                agentVersion,
                current.Version,
                cancellationToken)
            .ConfigureAwait(false);
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (response.Disposition == AgentFleetTransportDisposition.NotModified &&
            current.Version is not null &&
            MatchesVersionEntityTag(current.Version, response.EntityTag))
        {
            await localStore.RecordAssignmentsNotModifiedAsync(
                registration.AgentId,
                current.Version,
                response.EntityTag!,
                now,
                operationLease,
                cancellationToken).ConfigureAwait(false);
            return new AgentFleetClientResult(true, AgentLocalIdentityState.Active, "assignments.not_modified");
        }

        if (response.Disposition == AgentFleetTransportDisposition.Succeeded && response.Value is { } snapshot &&
            AgentAssignmentVersion.TryValidate(
                snapshot,
                registration.AgentId,
                registration.Environment,
                response.EntityTag,
                out _))
        {
            await localStore.ApplyAssignmentsAsync(
                snapshot,
                response.EntityTag!,
                now,
                operationLease,
                cancellationToken).ConfigureAwait(false);
            return new AgentFleetClientResult(true, AgentLocalIdentityState.Active, "assignments.applied");
        }

        AgentLocalIdentityState state = response.Disposition switch
        {
            AgentFleetTransportDisposition.TransientFailure => IsStale(current, now)
                ? AgentLocalIdentityState.Stale
                : AgentLocalIdentityState.Offline,
            AgentFleetTransportDisposition.Denied => AgentLocalIdentityState.RevokedOrDenied,
            AgentFleetTransportDisposition.Incompatible => AgentLocalIdentityState.Incompatible,
            _ => IsStale(current, now) ? AgentLocalIdentityState.Stale : AgentLocalIdentityState.Active,
        };
        string code = response.ErrorCode ??
            (response.Value is null ? null : "assignments.snapshot_invalid") ??
            "assignments.response_invalid";
        if (response.Value is not null && !AgentAssignmentVersion.TryValidate(
                response.Value,
                registration.AgentId,
                registration.Environment,
                response.EntityTag,
                out string? validationErrorCode))
        {
            code = validationErrorCode ?? code;
        }

        await localStore.RecordAssignmentFailureAsync(
            registration.AgentId,
            state,
            code,
            now,
            operationLease,
            cancellationToken).ConfigureAwait(false);
        return new AgentFleetClientResult(
            false,
            state,
            code,
            response.Disposition == AgentFleetTransportDisposition.TransientFailure,
            response.RetryAfter);
    }

    private async ValueTask<AgentFleetClientResult?> CheckActiveRegistrationAsync(
        AgentLocalRegistration? registration,
        AgentFleetOperationLease operationLease,
        CancellationToken cancellationToken)
    {
        if (registration is null)
        {
            return new AgentFleetClientResult(false, AgentLocalIdentityState.NotEnrolled, "agent.not_enrolled");
        }

        if (registration.State is AgentLocalIdentityState.RevokedOrDenied or
            AgentLocalIdentityState.Incompatible or AgentLocalIdentityState.Conflict or
            AgentLocalIdentityState.Expired)
        {
            return new AgentFleetClientResult(false, registration.State, "agent.identity_unavailable");
        }

        if (registration.CertificateNotAfter <= timeProvider.GetUtcNow())
        {
            await localStore.SetIdentityStateAsync(
                registration.AgentId,
                AgentLocalIdentityState.Expired,
                "agent.certificate_expired",
                timeProvider.GetUtcNow(),
                operationLease,
                cancellationToken).ConfigureAwait(false);
            return new AgentFleetClientResult(false, AgentLocalIdentityState.Expired, "agent.certificate_expired");
        }

        return null;
    }

    private bool IsStale(AgentAssignmentLocalState current, DateTimeOffset now) =>
        current.LastSucceededAt is null || now - current.LastSucceededAt > boundedAssignmentFreshness;

    private static TimeSpan ValidateFreshness(TimeSpan value)
    {
        if (value < TimeSpan.FromSeconds(1) || value > TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Assignment freshness is outside policy.");
        }

        return value;
    }

    private static bool MatchesVersionEntityTag(string version, string? entityTag) =>
        AgentAssignmentVersion.IsUpperHexDigest(version) &&
        entityTag is { Length: 66 } && entityTag[0] == '"' && entityTag[^1] == '"' &&
        string.Equals(entityTag[1..^1], version, StringComparison.Ordinal);

    private static AgentLocalIdentityState MapFailureState(AgentFleetTransportDisposition disposition) => disposition switch
    {
        AgentFleetTransportDisposition.TransientFailure => AgentLocalIdentityState.Offline,
        AgentFleetTransportDisposition.Denied => AgentLocalIdentityState.RevokedOrDenied,
        AgentFleetTransportDisposition.Incompatible => AgentLocalIdentityState.Incompatible,
        _ => AgentLocalIdentityState.Conflict,
    };

    private static void ValidateBootstrap(AgentEnrollmentBootstrap bootstrap)
    {
        ArgumentNullException.ThrowIfNull(bootstrap);
        if (string.IsNullOrWhiteSpace(bootstrap.Token) || bootstrap.Token.Length > 512 ||
            !IsStableIdentifier(bootstrap.InstallationId, 8, 160) ||
            string.IsNullOrWhiteSpace(bootstrap.DisplayName) || bootstrap.DisplayName != bootstrap.DisplayName.Trim() ||
            bootstrap.DisplayName.Length > 200 || bootstrap.DisplayName.Any(char.IsControl) ||
            !IsStableIdentifier(bootstrap.Environment, 1, 100) ||
            !IsStableIdentifier(bootstrap.Platform, 1, 100) ||
            !IsStableIdentifier(bootstrap.AgentVersion, 1, 64) ||
            !IsStableIdentifier(bootstrap.RequestedScope, 1, 160))
        {
            throw new ArgumentException("Test enrollment bootstrap is outside policy.", nameof(bootstrap));
        }
    }

    private static void ValidateAgentVersion(string agentVersion)
    {
        if (!IsStableIdentifier(agentVersion, 1, 64))
        {
            throw new ArgumentException("Agent version is outside policy.", nameof(agentVersion));
        }
    }

    private static bool IsStableIdentifier(string? value, int minimumLength, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= minimumLength && value.Length <= maximumLength &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or ':' or '-');

    private static bool TryValidateIssuedCertificate(
        string certificateSigningRequest,
        ReadOnlyMemory<byte> certificateDer,
        DateTimeOffset expectedNotAfter,
        DateTimeOffset now,
        out string thumbprint)
    {
        thumbprint = string.Empty;
        if (certificateDer.Length is < 128 or > 16_384 || expectedNotAfter <= now ||
            expectedNotAfter.Offset != TimeSpan.Zero)
        {
            return false;
        }

        try
        {
            byte[] csr = Convert.FromBase64String(certificateSigningRequest);
            CertificateRequest request = CertificateRequest.LoadSigningRequest(
                csr,
                HashAlgorithmName.SHA256,
                CertificateRequestLoadOptions.Default);
            using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(certificateDer.Span);
            if (certificate.HasPrivateKey || certificate.NotAfter.ToUniversalTime() != expectedNotAfter.UtcDateTime ||
                certificate.NotBefore.ToUniversalTime() - now.UtcDateTime > AgentFleetProtocol.MaximumFutureClockSkew ||
                !HasClientCertificateConstraints(certificate) || !IsNistP256(request.PublicKey))
            {
                return false;
            }

            using ECDsa? certificateKey = certificate.GetECDsaPublicKey();
            if (certificateKey is null || certificateKey.KeySize != 256 ||
                certificateKey.ExportParameters(false).Curve.Oid.Value != "1.2.840.10045.3.1.7" ||
                !CryptographicOperations.FixedTimeEquals(
                    request.PublicKey.ExportSubjectPublicKeyInfo(),
                    certificateKey.ExportSubjectPublicKeyInfo()))
            {
                return false;
            }

            thumbprint = certificate.Thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
            return thumbprint.Length >= 40 && thumbprint.All(character =>
                char.IsAsciiDigit(character) || character is >= 'A' and <= 'F');
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    private static bool HasClientCertificateConstraints(X509Certificate2 certificate)
    {
        X509BasicConstraintsExtension? basic = certificate.Extensions
            .OfType<X509BasicConstraintsExtension>().SingleOrDefault();
        X509KeyUsageExtension? usage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
        X509EnhancedKeyUsageExtension? enhanced = certificate.Extensions
            .OfType<X509EnhancedKeyUsageExtension>().SingleOrDefault();
        return basic is { CertificateAuthority: false } &&
            usage is not null && usage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature) &&
            enhanced is not null && enhanced.EnhancedKeyUsages.Cast<Oid>()
                .Any(oid => oid.Value == "1.3.6.1.5.5.7.3.2");
    }

    private static bool IsNistP256(PublicKey publicKey)
    {
        if (publicKey.Oid.Value != "1.2.840.10045.2.1")
        {
            return false;
        }

        byte[]? parameters = publicKey.EncodedParameters?.RawData;
        return parameters is not null && parameters.AsSpan().SequenceEqual(
            new byte[] { 0x06, 0x08, 0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07 });
    }
}
