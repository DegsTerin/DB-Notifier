// Module purpose: Verifies provider-neutral Agent Fleet validation and delegation without exercising runtime, network or external persistence.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DBNotifier.Application.Access;
using DBNotifier.Application.AgentFleet;

namespace DBNotifier.UnitTests;

/// <summary>
/// Verifies the local Agent Fleet application boundary with in-memory doubles and ephemeral cryptographic material.
/// </summary>
public sealed class AgentFleetServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 17, 18, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies exact token parsing, deterministic domain-separated hashing and malformed-input refusal.</summary>
    [Fact]
    public void TokenProofUsesExactFormatAndSaltedHashWithoutRetainingRawSecretBytes()
    {
        TokenMaterial material = CreateTokenMaterial(CreateEnrollmentRequest());
        byte[]? repeatedHash = null;
        byte[]? otherHash = null;
        byte[] otherSalt = RandomNumberGenerator.GetBytes(AgentEnrollmentTokenProof.SaltLength);
        try
        {
            Assert.True(AgentEnrollmentTokenProof.TryGetTokenId(material.Token, out Guid parsedTokenId));
            Assert.Equal(material.Challenge.EnrollmentTokenId, parsedTokenId);

            repeatedHash = AgentEnrollmentTokenProof.ComputeSecretHash(material.Token, material.Salt);
            otherHash = AgentEnrollmentTokenProof.ComputeSecretHash(material.Token, otherSalt);
            Assert.True(CryptographicOperations.FixedTimeEquals(material.SecretHash, repeatedHash));
            Assert.False(CryptographicOperations.FixedTimeEquals(material.SecretHash, otherHash));
            Assert.Equal(SHA256.HashSizeInBytes, repeatedHash.Length);

            string malformed = string.Concat(material.Token.AsSpan(0, material.Token.Length - 1), "*");
            Assert.False(AgentEnrollmentTokenProof.TryGetTokenId(malformed, out _));
            Assert.Throws<ArgumentException>(() =>
                AgentEnrollmentTokenProof.ComputeSecretHash(
                    material.Token,
                    new byte[AgentEnrollmentTokenProof.SaltLength - 1]));
        }
        finally
        {
            material.Clear();
            CryptographicOperations.ZeroMemory(otherSalt);
            if (repeatedHash is not null)
            {
                CryptographicOperations.ZeroMemory(repeatedHash);
            }

            if (otherHash is not null)
            {
                CryptographicOperations.ZeroMemory(otherHash);
            }
        }
    }

    /// <summary>Verifies valid token binding, P-256 certificate validation and proof-only persistence delegation.</summary>
    [Fact]
    public async Task EnrollmentAcceptsP256CertificateAndPassesOnlyClearedProofToStore()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            Guid enrolledAgentId = Guid.NewGuid();
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            store.EnrollmentResults.Enqueue(new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Enrolled,
                enrolledAgentId,
                null));
            EphemeralCertificateIssuer issuer = new(ECCurve.NamedCurves.nistP256);
            AgentFleetService service = Service(store, issuer);

            AgentEnrollmentOutcome outcome = await service.EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Enrolled, outcome.Disposition);
            Assert.Equal(enrolledAgentId, outcome.AgentId);
            Assert.False(outcome.CertificateDer.IsEmpty);
            Assert.NotNull(outcome.CertificateNotAfter);
            Assert.Null(outcome.ErrorCode);
            Assert.Equal(1, issuer.Calls);
            Assert.Equal(1, store.CompleteEnrollmentCalls);
            Assert.True(store.EnrollmentProofMatchedChallenge);
            Assert.NotNull(store.LastEnrollmentCommit);
            Assert.All(store.LastEnrollmentCommit!.PresentedSecretHash.ToArray(), value => Assert.Equal(0, value));
            Assert.Null(typeof(AgentEnrollmentCommit).GetProperty("EnrollmentToken"));
            Assert.Null(typeof(AgentEnrollmentCommit).GetProperty("TokenSecret"));

            AgentCertificateIssueRequest issuedRequest = Assert.IsType<AgentCertificateIssueRequest>(issuer.LastRequest);
            byte[] csr = Convert.FromBase64String(request.CertificateSigningRequest);
            byte[] expectedDigest = SHA256.HashData(csr);
            try
            {
                Assert.True(CryptographicOperations.FixedTimeEquals(
                    expectedDigest,
                    issuedRequest.CertificateSigningRequestSha256.Span));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expectedDigest);
                CryptographicOperations.ZeroMemory(csr);
            }
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies exact token scope binding before issuer work and generic external denial semantics.</summary>
    [Fact]
    public async Task EnrollmentDeniesScopeMismatchBeforeCertificateIssuance()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            RecordingAgentFleetStore store = new()
            {
                EnrollmentChallenge = material.Challenge with { ExpectedEnvironment = "production" },
            };
            EphemeralCertificateIssuer issuer = new(ECCurve.NamedCurves.nistP256);

            AgentEnrollmentOutcome outcome = await Service(store, issuer).EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Denied, outcome.Disposition);
            Assert.Equal("enrollment.denied", outcome.ErrorCode);
            Assert.Null(outcome.AgentId);
            Assert.True(outcome.CertificateDer.IsEmpty);
            Assert.Equal(0, issuer.Calls);
            Assert.Equal(0, store.CompleteEnrollmentCalls);
            Assert.Contains("enrollment.token_or_scope_denied", store.EnrollmentAuditCodes);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies that an unavailable issuer fails closed and records a sanitised denial.</summary>
    [Fact]
    public async Task EnrollmentAuditsUnavailableIssuerWithoutConsumingToken()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            EphemeralCertificateIssuer issuer = new(
                ECCurve.NamedCurves.nistP256,
                CertificateFixtureMode.Unavailable);

            AgentEnrollmentOutcome outcome = await Service(store, issuer).EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.IssuerUnavailable, outcome.Disposition);
            Assert.Equal("enrollment.issuer_unavailable", outcome.ErrorCode);
            Assert.Equal(1, issuer.Calls);
            Assert.Equal(0, store.CompleteEnrollmentCalls);
            Assert.Contains("enrollment.issuer_unavailable", store.EnrollmentAuditCodes);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies that a stale pre-issuance read cannot bypass the atomic one-time commit decision.</summary>
    [Fact]
    public async Task EnrollmentReplayPreservesAtomicCommitDenial()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            Guid enrolledAgentId = Guid.NewGuid();
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            store.EnrollmentResults.Enqueue(new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Enrolled,
                enrolledAgentId,
                null));
            store.EnrollmentResults.Enqueue(new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Denied,
                null,
                "enrollment.token_consumed"));
            EphemeralCertificateIssuer issuer = new(ECCurve.NamedCurves.nistP256);
            AgentFleetService service = Service(store, issuer);

            AgentEnrollmentOutcome first = await service.EnrollAsync(material.Token, request);
            AgentEnrollmentOutcome replay = await service.EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Enrolled, first.Disposition);
            Assert.Equal(AgentEnrollmentDisposition.Denied, replay.Disposition);
            Assert.Equal("enrollment.denied", replay.ErrorCode);
            Assert.Null(replay.AgentId);
            Assert.True(replay.CertificateDer.IsEmpty);
            Assert.Equal(2, store.CompleteEnrollmentCalls);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies that a certificate on a non-authorised elliptic curve fails closed before persistence.</summary>
    [Fact]
    public async Task EnrollmentRejectsCertificateOutsideNistP256()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            EphemeralCertificateIssuer issuer = new(ECCurve.NamedCurves.nistP384);

            AgentEnrollmentOutcome outcome = await Service(store, issuer).EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Invalid, outcome.Disposition);
            Assert.Equal("enrollment.csr_invalid", outcome.ErrorCode);
            Assert.Equal(0, store.CompleteEnrollmentCalls);
            Assert.Contains("enrollment.csr_invalid", store.EnrollmentAuditCodes);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies that issuer output must remain bound to the exact CSR admitted by the service.</summary>
    [Fact]
    public async Task EnrollmentRejectsCertificateWithMismatchedCsrDigest()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            EphemeralCertificateIssuer issuer = new(
                ECCurve.NamedCurves.nistP256,
                CertificateFixtureMode.MismatchedCsrDigest);

            AgentEnrollmentOutcome outcome = await Service(store, issuer).EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Invalid, outcome.Disposition);
            Assert.Equal(0, store.CompleteEnrollmentCalls);
            Assert.Contains("enrollment.csr_invalid", store.EnrollmentAuditCodes);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies the Server rejects an issuer certificate whose key differs from the CSR despite coherent metadata.</summary>
    [Fact]
    public async Task EnrollmentRejectsCertificateWhoseSpkiDoesNotMatchTheCsr()
    {
        AgentEnrollmentRequest request = CreateEnrollmentRequest();
        TokenMaterial material = CreateTokenMaterial(request);
        try
        {
            RecordingAgentFleetStore store = new() { EnrollmentChallenge = material.Challenge };
            EphemeralCertificateIssuer issuer = new(
                ECCurve.NamedCurves.nistP256,
                CertificateFixtureMode.MismatchedPublicKey);

            AgentEnrollmentOutcome outcome = await Service(store, issuer).EnrollAsync(material.Token, request);

            Assert.Equal(AgentEnrollmentDisposition.Invalid, outcome.Disposition);
            Assert.Equal("enrollment.csr_invalid", outcome.ErrorCode);
            Assert.Equal(0, store.CompleteEnrollmentCalls);
            Assert.Contains("enrollment.csr_invalid", store.EnrollmentAuditCodes);
        }
        finally
        {
            material.Clear();
        }
    }

    /// <summary>Verifies canonical heartbeat digest stability and preservation of the store's replay result.</summary>
    [Fact]
    public async Task HeartbeatDelegatesCanonicalDigestAndReturnsDurableReplayOutcome()
    {
        AgentHeartbeatRequest request = CreateHeartbeatRequest();
        RecordingAgentFleetStore store = new();
        store.HeartbeatResults.Enqueue(new AgentHeartbeatOutcome(
            AgentHeartbeatDisposition.Accepted,
            Now,
            request.Sequence,
            0,
            null));
        store.HeartbeatResults.Enqueue(new AgentHeartbeatOutcome(
            AgentHeartbeatDisposition.Duplicate,
            Now,
            request.Sequence,
            0,
            null));
        AgentFleetService service = Service(store, new EphemeralCertificateIssuer(ECCurve.NamedCurves.nistP256));

        AgentHeartbeatOutcome accepted = await service.RecordHeartbeatAsync(request);
        AgentHeartbeatOutcome duplicate = await service.RecordHeartbeatAsync(request);

        Assert.Equal(AgentHeartbeatDisposition.Accepted, accepted.Disposition);
        Assert.Equal(AgentHeartbeatDisposition.Duplicate, duplicate.Disposition);
        Assert.Equal(2, store.RecordHeartbeatCalls.Count);
        Assert.Equal(store.RecordHeartbeatCalls[0].PayloadSha256, store.RecordHeartbeatCalls[1].PayloadSha256);
        Assert.Equal(SHA256.HashSizeInBytes * 2, store.RecordHeartbeatCalls[0].PayloadSha256.Length);
        Assert.All(
            store.RecordHeartbeatCalls[0].PayloadSha256,
            character => Assert.True(char.IsAsciiDigit(character) || character is >= 'A' and <= 'F'));
        Assert.All(store.HeartbeatReceivedAt, receivedAt => Assert.Equal(Now, receivedAt));
    }

    /// <summary>Verifies protocol mismatch and malformed queue claims are refused before durable work.</summary>
    [Fact]
    public async Task HeartbeatRejectsIncompatibleAndInvalidRequestsBeforeStore()
    {
        AgentHeartbeatRequest valid = CreateHeartbeatRequest();
        AgentHeartbeatRequest incompatible = valid with { ProtocolMinimum = 2, ProtocolMaximum = 2 };
        AgentHeartbeatRequest invalid = valid with { QueueDepth = 1, OldestQueuedAt = null };
        RecordingAgentFleetStore store = new();
        AgentFleetService service = Service(store, new EphemeralCertificateIssuer(ECCurve.NamedCurves.nistP256));

        AgentHeartbeatOutcome incompatibleOutcome = await service.RecordHeartbeatAsync(incompatible);
        AgentHeartbeatOutcome invalidOutcome = await service.RecordHeartbeatAsync(invalid);

        Assert.Equal(AgentHeartbeatDisposition.Incompatible, incompatibleOutcome.Disposition);
        Assert.Equal("protocol.version_unsupported", incompatibleOutcome.ErrorCode);
        Assert.Equal(AgentHeartbeatDisposition.Invalid, invalidOutcome.Disposition);
        Assert.Equal("heartbeat.request_invalid", invalidOutcome.ErrorCode);
        Assert.Empty(store.RecordHeartbeatCalls);
    }

    /// <summary>Verifies bounded assignment request validation and exact delegation of the caller-held version.</summary>
    [Fact]
    public async Task AssignmentsValidateHeadersAndDelegateCompleteSnapshotRequest()
    {
        Guid agentId = Guid.NewGuid();
        string currentVersion = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA256.HashSizeInBytes));
        AgentAssignmentSnapshot snapshot = new(
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            currentVersion,
            Now,
            []);
        RecordingAgentFleetStore store = new()
        {
            AssignmentResult = new AgentAssignmentOutcome(
                AgentAssignmentDisposition.Available,
                snapshot,
                currentVersion,
                null),
        };
        AgentFleetService service = Service(store, new EphemeralCertificateIssuer(ECCurve.NamedCurves.nistP256));

        AgentAssignmentOutcome outcome = await service.GetAssignmentsAsync(
            agentId,
            "1.0.0-test",
            currentVersion);

        Assert.Equal(AgentAssignmentDisposition.Available, outcome.Disposition);
        AssignmentCall call = Assert.Single(store.GetAssignmentsCalls);
        Assert.Equal(agentId, call.AgentId);
        Assert.Equal("1.0.0-test", call.AgentVersion);
        Assert.Equal(currentVersion, call.AfterVersion);
        Assert.Equal(Now, call.GeneratedAt);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAssignmentsAsync(agentId, "version with spaces", currentVersion).AsTask());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAssignmentsAsync(agentId, "1.0.0-test", currentVersion.ToLowerInvariant()).AsTask());
        Assert.Single(store.GetAssignmentsCalls);
    }

    /// <summary>Verifies fixed RBAC permission selection for safe catalogue reads and monotonic revocation calls.</summary>
    [Fact]
    public async Task CatalogueAndRevocationUseDedicatedPermissionsAndTrustedTime()
    {
        Guid agentId = Guid.NewGuid();
        AgentFleetView view = new(
            agentId,
            "Fixture Agent",
            "test",
            "windows-x64",
            "1.0.0-test",
            "Active",
            Now.AddMinutes(-5),
            null,
            Now.AddSeconds(-5));
        RecordingAgentFleetStore store = new()
        {
            CatalogueResult = new AgentFleetCatalogue(true, [view]),
            RevocationResult = new AgentRevocationOutcome(
                AgentRevocationDisposition.Revoked,
                agentId,
                Now,
                null),
        };
        AgentFleetService service = Service(store, new EphemeralCertificateIssuer(ECCurve.NamedCurves.nistP256));
        HumanActor actor = new("oidc:fixture-operator");

        AgentFleetCatalogue catalogue = await service.GetCatalogueAsync(actor);
        AgentRevocationOutcome revocation = await service.RevokeAsync(
            actor,
            agentId,
            new AgentRevocationRequest("operator-request"));

        Assert.True(catalogue.Authorised);
        Assert.Single(catalogue.Agents);
        Assert.Equal(AgentRevocationDisposition.Revoked, revocation.Disposition);
        Assert.Equal(new CatalogueCall(actor.SubjectId, PlatformPermissions.AgentsRead, Now), store.LastCatalogueCall);
        Assert.Equal(
            new RevocationCall(
                actor.SubjectId,
                agentId,
                PlatformPermissions.AgentsRevoke,
                "operator-request",
                Now),
            store.LastRevocationCall);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RevokeAsync(actor, agentId, new AgentRevocationRequest("free form reason")).AsTask());
        Assert.Equal(1, store.RevokeCalls);
    }

    /// <summary>Creates the service with a deterministic trusted clock.</summary>
    /// <param name="store">In-memory persistence double.</param>
    /// <param name="issuer">Ephemeral certificate issuer.</param>
    /// <returns>Configured Agent Fleet service.</returns>
    private static AgentFleetService Service(IAgentFleetStore store, IAgentCertificateIssuer issuer) =>
        new(store, issuer, new FixedTimeProvider(Now));

    /// <summary>Creates a valid enrollment request with a fresh in-memory P-256 CSR.</summary>
    /// <returns>Versioned request containing public CSR material only.</returns>
    private static AgentEnrollmentRequest CreateEnrollmentRequest()
    {
        using ECDsa requestKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        CertificateRequest certificateRequest = new(
            "CN=DB-Notifier Unit Test Enrollment",
            requestKey,
            HashAlgorithmName.SHA256);
        byte[] csr = certificateRequest.CreateSigningRequest();
        try
        {
            return new AgentEnrollmentRequest(
                Guid.NewGuid(),
                AgentFleetProtocol.CurrentSchemaVersion,
                $"installation:{Guid.NewGuid():N}",
                "Fixture Agent",
                "test",
                "windows-x64",
                "1.0.0-test",
                "fleet:test",
                Now.AddSeconds(-2),
                Now.AddSeconds(-1),
                Convert.ToBase64String(csr));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(csr);
        }
    }

    /// <summary>Creates an exact token and its persisted salted challenge from random in-memory bytes.</summary>
    /// <param name="request">Request whose scope is bound into the challenge.</param>
    /// <returns>Ephemeral token material which the caller must clear.</returns>
    private static TokenMaterial CreateTokenMaterial(AgentEnrollmentRequest request)
    {
        Guid tokenId = Guid.NewGuid();
        byte[] secret = RandomNumberGenerator.GetBytes(AgentEnrollmentTokenProof.SecretLength);
        byte[] salt = RandomNumberGenerator.GetBytes(AgentEnrollmentTokenProof.SaltLength);
        try
        {
            string encodedSecret = Convert.ToBase64String(secret)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            string token = $"{tokenId:N}.{encodedSecret}";
            byte[] secretHash = AgentEnrollmentTokenProof.ComputeSecretHash(token, salt);
            AgentEnrollmentTokenChallenge challenge = new(
                tokenId,
                salt,
                secretHash,
                AgentEnrollmentTokenProof.HashAlgorithm,
                request.InstallationId,
                request.Environment,
                request.Platform,
                request.RequestedScope,
                Now.AddMinutes(-1),
                Now.AddMinutes(5),
                null,
                null,
                Guid.NewGuid());
            return new TokenMaterial(token, challenge, salt, secretHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secret);
        }
    }

    /// <summary>Creates a valid heartbeat whose freshness is owned by the fixed server receipt time.</summary>
    /// <returns>Bounded protocol-version-one heartbeat.</returns>
    private static AgentHeartbeatRequest CreateHeartbeatRequest() =>
        new(
            Guid.NewGuid(),
            AgentFleetProtocol.CurrentSchemaVersion,
            Guid.NewGuid(),
            1,
            Now.AddSeconds(-2),
            Now.AddSeconds(-1),
            "1.0.0-test",
            AgentFleetProtocol.CurrentProtocolVersion,
            AgentFleetProtocol.CurrentProtocolVersion,
            0,
            null,
            Now.AddSeconds(-1));

    /// <summary>Owns generated token arrays so tests can erase them deterministically.</summary>
    /// <param name="Token">Ephemeral token text held only for the duration of one test.</param>
    /// <param name="Challenge">Persisted challenge projection containing no raw token.</param>
    /// <param name="Salt">Generated salt buffer.</param>
    /// <param name="SecretHash">Generated digest buffer.</param>
    private sealed record TokenMaterial(
        string Token,
        AgentEnrollmentTokenChallenge Challenge,
        byte[] Salt,
        byte[] SecretHash)
    {
        /// <summary>Erases the mutable generated proof buffers after the test.</summary>
        public void Clear()
        {
            CryptographicOperations.ZeroMemory(Salt);
            CryptographicOperations.ZeroMemory(SecretHash);
        }
    }

    /// <summary>Controls a narrow issuer deviation used to verify fail-closed certificate validation.</summary>
    private enum CertificateFixtureMode
    {
        /// <summary>Returns certificate metadata matching the exact CSR.</summary>
        Valid,

        /// <summary>Returns a digest which does not match the admitted CSR.</summary>
        MismatchedCsrDigest,

        /// <summary>Returns a valid client certificate for a different P-256 public key.</summary>
        MismatchedPublicKey,

        /// <summary>Reports that no certificate authority is available.</summary>
        Unavailable,
    }

    /// <summary>Issues public self-signed fixture certificates using keys generated and disposed in memory.</summary>
    private sealed class EphemeralCertificateIssuer(
        ECCurve curve,
        CertificateFixtureMode mode = CertificateFixtureMode.Valid) : IAgentCertificateIssuer
    {
        /// <summary>Gets the number of issuer calls.</summary>
        public int Calls { get; private set; }

        /// <summary>Gets the last public CSR request received by the issuer.</summary>
        public AgentCertificateIssueRequest? LastRequest { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentCertificateIssueResult> IssueAsync(
            AgentCertificateIssueRequest request,
            DateTimeOffset issuedAt,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastRequest = request;

            if (mode == CertificateFixtureMode.Unavailable)
            {
                return ValueTask.FromResult(new AgentCertificateIssueResult(
                    AgentCertificateIssueDisposition.Unavailable,
                    ReadOnlyMemory<byte>.Empty,
                    null,
                    null,
                    ReadOnlyMemory<byte>.Empty,
                    null,
                    null));
            }

            CertificateRequest parsedRequest = CertificateRequest.LoadSigningRequest(
                request.CertificateSigningRequestDer.ToArray(),
                HashAlgorithmName.SHA256,
                CertificateRequestLoadOptions.Default);
            bool useCsrPublicKey = curve.Oid.Value == ECCurve.NamedCurves.nistP256.Oid.Value &&
                mode != CertificateFixtureMode.MismatchedPublicKey;
            using ECDsa? alternativeKey = useCsrPublicKey ? null : ECDsa.Create(curve);
            CertificateRequest? alternativeRequest = alternativeKey is null
                ? null
                : new CertificateRequest(
                    "CN=DB-Notifier Unit Test Alternative",
                    alternativeKey,
                    HashAlgorithmName.SHA256);
            CertificateRequest certificateRequest = new(
                new X500DistinguishedName("CN=DB-Notifier Unit Test Agent"),
                useCsrPublicKey ? parsedRequest.PublicKey : alternativeRequest!.PublicKey,
                HashAlgorithmName.SHA256);
            certificateRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            certificateRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
            OidCollection enhancedUsages = new();
            enhancedUsages.Add(new Oid("1.3.6.1.5.5.7.3.2"));
            certificateRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(enhancedUsages, false));

            using ECDsa rootKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest rootRequest = new(
                "CN=DB-Notifier Unit Test Root",
                rootKey,
                HashAlgorithmName.SHA256);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                true));
            using X509Certificate2 rootCertificate = rootRequest.CreateSelfSigned(
                issuedAt.AddMinutes(-2),
                issuedAt.AddHours(2));
            using X509Certificate2 issuedCertificate = certificateRequest.Create(
                rootCertificate,
                issuedAt.AddMinutes(-1),
                issuedAt.AddHours(1),
                RandomNumberGenerator.GetBytes(16));
            byte[] certificateDer = issuedCertificate.Export(X509ContentType.Cert);
            using X509Certificate2 publicCertificate = X509CertificateLoader.LoadCertificate(certificateDer);
            using ECDsa publicKey = publicCertificate.GetECDsaPublicKey()
                ?? throw new InvalidOperationException("The ephemeral certificate did not expose an ECDSA public key.");
            string publicKeySha256 = Convert.ToHexString(
                SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo()));
            ReadOnlyMemory<byte> csrDigest = mode == CertificateFixtureMode.MismatchedCsrDigest
                ? new byte[SHA256.HashSizeInBytes]
                : request.CertificateSigningRequestSha256;

            AgentCertificateIssueResult result = new(
                AgentCertificateIssueDisposition.Issued,
                certificateDer,
                publicCertificate.Thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant(),
                publicKeySha256,
                csrDigest,
                new DateTimeOffset(publicCertificate.NotBefore.ToUniversalTime()),
                new DateTimeOffset(publicCertificate.NotAfter.ToUniversalTime()));
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>Supplies a fixed UTC instant to keep validation and delegation assertions deterministic.</summary>
    /// <param name="now">UTC instant returned to the service.</param>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>Captures one assignment store invocation.</summary>
    /// <param name="AgentId">Authenticated Agent identifier.</param>
    /// <param name="AgentVersion">Validated Agent version.</param>
    /// <param name="AfterVersion">Optional current snapshot version.</param>
    /// <param name="GeneratedAt">Trusted response instant.</param>
    private readonly record struct AssignmentCall(
        Guid AgentId,
        string AgentVersion,
        string? AfterVersion,
        DateTimeOffset GeneratedAt);

    /// <summary>Captures one human catalogue store invocation.</summary>
    /// <param name="SubjectId">Authenticated human subject.</param>
    /// <param name="PermissionCode">Required fixed permission.</param>
    /// <param name="Now">Trusted authorisation instant.</param>
    private readonly record struct CatalogueCall(string SubjectId, string PermissionCode, DateTimeOffset Now);

    /// <summary>Captures one human revocation store invocation.</summary>
    /// <param name="SubjectId">Authenticated human subject.</param>
    /// <param name="AgentId">Target Agent.</param>
    /// <param name="PermissionCode">Required fixed permission.</param>
    /// <param name="ReasonCode">Validated machine-readable reason.</param>
    /// <param name="Now">Trusted revocation instant.</param>
    private readonly record struct RevocationCall(
        string SubjectId,
        Guid AgentId,
        string PermissionCode,
        string ReasonCode,
        DateTimeOffset Now);

    /// <summary>Records Application-port calls without persistence, runtime or external effects.</summary>
    private sealed class RecordingAgentFleetStore : IAgentFleetStore
    {
        /// <summary>Gets or sets the token challenge returned to the service.</summary>
        public AgentEnrollmentTokenChallenge? EnrollmentChallenge { get; init; }

        /// <summary>Gets queued atomic enrollment results.</summary>
        public Queue<AgentEnrollmentCommitResult> EnrollmentResults { get; } = new();

        /// <summary>Gets queued durable heartbeat results.</summary>
        public Queue<AgentHeartbeatOutcome> HeartbeatResults { get; } = new();

        /// <summary>Gets or sets the assignment result returned by the store.</summary>
        public AgentAssignmentOutcome AssignmentResult { get; init; } = new(
            AgentAssignmentDisposition.AgentInactive,
            null,
            null,
            "agent.inactive");

        /// <summary>Gets or sets the catalogue result returned by the store.</summary>
        public AgentFleetCatalogue CatalogueResult { get; init; } = new(false, []);

        /// <summary>Gets or sets the revocation result returned by the store.</summary>
        public AgentRevocationOutcome? RevocationResult { get; init; }

        /// <summary>Gets the number of atomic enrollment commits.</summary>
        public int CompleteEnrollmentCalls { get; private set; }

        /// <summary>Gets whether the presented enrollment proof matched the stored digest during the call.</summary>
        public bool EnrollmentProofMatchedChallenge { get; private set; }

        /// <summary>Gets the last enrollment commit after the service has cleared its sensitive proof buffer.</summary>
        public AgentEnrollmentCommit? LastEnrollmentCommit { get; private set; }

        /// <summary>Gets sanitised enrollment audit codes.</summary>
        public List<string> EnrollmentAuditCodes { get; } = [];

        /// <summary>Gets validated heartbeat calls.</summary>
        public List<ValidatedAgentHeartbeat> RecordHeartbeatCalls { get; } = [];

        /// <summary>Gets trusted heartbeat receipt instants.</summary>
        public List<DateTimeOffset> HeartbeatReceivedAt { get; } = [];

        /// <summary>Gets assignment store calls.</summary>
        public List<AssignmentCall> GetAssignmentsCalls { get; } = [];

        /// <summary>Gets the last catalogue store call.</summary>
        public CatalogueCall? LastCatalogueCall { get; private set; }

        /// <summary>Gets the last revocation store call.</summary>
        public RevocationCall? LastRevocationCall { get; private set; }

        /// <summary>Gets the number of revocation store calls.</summary>
        public int RevokeCalls { get; private set; }

        /// <inheritdoc />
        public ValueTask<AgentEnrollmentTokenChallenge?> GetEnrollmentTokenAsync(
            Guid enrollmentTokenId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult(EnrollmentChallenge?.EnrollmentTokenId == enrollmentTokenId
                ? EnrollmentChallenge
                : null);

        /// <inheritdoc />
        public ValueTask<AgentEnrollmentCommitResult> CompleteEnrollmentAsync(
            AgentEnrollmentCommit commit,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            CompleteEnrollmentCalls++;
            LastEnrollmentCommit = commit;
            EnrollmentProofMatchedChallenge = EnrollmentChallenge is not null &&
                CryptographicOperations.FixedTimeEquals(
                    commit.PresentedSecretHash.Span,
                    EnrollmentChallenge.SecretHash.Span);
            AgentEnrollmentCommitResult result = EnrollmentResults.TryDequeue(out AgentEnrollmentCommitResult? queued)
                ? queued
                : new AgentEnrollmentCommitResult(
                    AgentEnrollmentCommitDisposition.Denied,
                    null,
                    "enrollment.denied");
            return ValueTask.FromResult(result);
        }

        /// <inheritdoc />
        public ValueTask AuditEnrollmentDenialAsync(
            string internalCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            EnrollmentAuditCodes.Add(internalCode);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public ValueTask<AgentHeartbeatOutcome> RecordHeartbeatAsync(
            ValidatedAgentHeartbeat heartbeat,
            DateTimeOffset receivedAt,
            CancellationToken cancellationToken)
        {
            RecordHeartbeatCalls.Add(heartbeat);
            HeartbeatReceivedAt.Add(receivedAt);
            AgentHeartbeatOutcome result = HeartbeatResults.TryDequeue(out AgentHeartbeatOutcome? queued)
                ? queued
                : new AgentHeartbeatOutcome(
                    AgentHeartbeatDisposition.Accepted,
                    receivedAt,
                    heartbeat.Request.Sequence,
                    0,
                    null);
            return ValueTask.FromResult(result);
        }

        /// <inheritdoc />
        public ValueTask<AgentAssignmentOutcome> GetAssignmentsAsync(
            Guid agentId,
            string agentVersion,
            string? afterVersion,
            DateTimeOffset generatedAt,
            CancellationToken cancellationToken)
        {
            GetAssignmentsCalls.Add(new AssignmentCall(agentId, agentVersion, afterVersion, generatedAt));
            return ValueTask.FromResult(AssignmentResult);
        }

        /// <inheritdoc />
        public ValueTask<AgentFleetCatalogue> GetAuthorisedAgentsAsync(
            string subjectId,
            string permissionCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastCatalogueCall = new CatalogueCall(subjectId, permissionCode, now);
            return ValueTask.FromResult(CatalogueResult);
        }

        /// <inheritdoc />
        public ValueTask<AgentRevocationOutcome> RevokeAgentAsync(
            string subjectId,
            Guid agentId,
            string permissionCode,
            string reasonCode,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            RevokeCalls++;
            LastRevocationCall = new RevocationCall(subjectId, agentId, permissionCode, reasonCode, now);
            return ValueTask.FromResult(RevocationResult ?? new AgentRevocationOutcome(
                AgentRevocationDisposition.Denied,
                agentId,
                null,
                "agent.denied"));
        }
    }
}
