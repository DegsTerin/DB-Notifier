// Module purpose: Persists bounded Agent Fleet identity, heartbeat and read-only assignment state without provider execution or secret material.
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBNotifier.Application.AgentFleet;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>
/// Implements the Agent Fleet persistence port with atomic enrollment, monotonic revocation, durable heartbeat
/// anti-replay state and server-side human authorisation. It never stores token values, private keys or full
/// certificates and never invokes providers, commands or external infrastructure.
/// </summary>
/// <param name="contextFactory">Factory for short-lived central persistence contexts.</param>
public sealed class AgentFleetStore(IDbContextFactory<ServerDbContext> contextFactory) : IAgentFleetStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 32,
    };

    /// <inheritdoc />
    public async ValueTask<AgentEnrollmentTokenChallenge?> GetEnrollmentTokenAsync(
        Guid enrollmentTokenId,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AgentEnrollmentTokenRow? row = await context.AgentEnrollmentTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.EnrollmentTokenId == enrollmentTokenId,
                cancellationToken)
            .ConfigureAwait(false);
        return row is null
            ? null
            : new AgentEnrollmentTokenChallenge(
                row.EnrollmentTokenId,
                row.Salt.ToArray(),
                row.SecretHash.ToArray(),
                row.HashAlgorithm,
                row.ExpectedInstallationId,
                row.ExpectedEnvironment,
                row.ExpectedPlatform,
                row.Scope,
                row.IssuedAt,
                row.ExpiresAt,
                row.ConsumedAt,
                row.RevokedAt,
                row.ConcurrencyToken);
    }

    /// <inheritdoc />
    public async ValueTask<AgentEnrollmentCommitResult> CompleteEnrollmentAsync(
        AgentEnrollmentCommit commit,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commit);
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        AgentEnrollmentTokenRow? token = await context.AgentEnrollmentTokens
            .SingleOrDefaultAsync(
                row => row.EnrollmentTokenId == commit.EnrollmentTokenId,
                cancellationToken)
            .ConfigureAwait(false);
        if (!EnrollmentCommitMatches(token, commit, now))
        {
            return await DenyEnrollmentCommitAsync(
                context,
                transaction,
                commit.EnrollmentTokenId,
                "enrollment.commit_denied",
                now,
                cancellationToken).ConfigureAwait(false);
        }

        string certificateThumbprint = commit.Certificate.CertificateThumbprint!;
        bool identityExists = await context.Agents
            .AsNoTracking()
            .AnyAsync(
                row => row.InstallationId == commit.Request.InstallationId ||
                    row.CertificateThumbprint == certificateThumbprint,
                cancellationToken)
            .ConfigureAwait(false);
        bool certificateExists = await context.AgentCertificates
            .AsNoTracking()
            .AnyAsync(
                row => row.Thumbprint == certificateThumbprint,
                cancellationToken)
            .ConfigureAwait(false);
        if (identityExists || certificateExists)
        {
            return await DenyEnrollmentCommitAsync(
                context,
                transaction,
                commit.EnrollmentTokenId,
                "enrollment.identity_conflict",
                now,
                cancellationToken,
                AgentEnrollmentCommitDisposition.Conflict).ConfigureAwait(false);
        }

        Guid agentId = Guid.NewGuid();
        RegisteredAgentRow agent = new()
        {
            AgentId = agentId,
            InstallationId = commit.Request.InstallationId,
            DisplayName = commit.Request.DisplayName,
            Environment = commit.Request.Environment,
            Platform = commit.Request.Platform,
            AgentVersion = commit.Request.AgentVersion,
            // This pointer remains only as a compatibility bridge; certificate authority belongs to the normalised table.
            CertificateThumbprint = certificateThumbprint,
            State = "Active",
            EnrolledAt = now,
            RevokedAt = null,
            LastSeenAt = null,
            ConcurrencyToken = Guid.NewGuid(),
        };
        AgentCertificateRow certificate = new()
        {
            AgentCertificateId = Guid.NewGuid(),
            AgentId = agentId,
            Thumbprint = certificateThumbprint,
            PublicKeySha256 = commit.Certificate.PublicKeySha256,
            CertificateSigningRequestSha256 = Convert.ToHexString(
                commit.Certificate.CertificateSigningRequestSha256.Span),
            State = "Active",
            IssuedAt = now,
            NotBefore = commit.Certificate.NotBefore,
            NotAfter = commit.Certificate.NotAfter,
            RevokedAt = null,
            RevocationReasonCode = null,
            ConcurrencyToken = Guid.NewGuid(),
        };
        token!.State = "Consumed";
        token.ConsumedAt = now;
        token.ConsumedByAgentId = agentId;
        token.ConcurrencyToken = Guid.NewGuid();
        context.Agents.Add(agent);
        context.AgentCertificates.Add(certificate);
        AddAudit(
            context,
            "EnrollmentToken",
            commit.EnrollmentTokenId.ToString("D"),
            "agent.enroll",
            "Agent",
            agentId.ToString("D"),
            "Succeeded",
            commit.MessageId,
            "enrollment.completed",
            now);

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Enrolled,
                agentId,
                null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Conflict,
                null,
                "enrollment.concurrency_conflict");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AgentEnrollmentCommitResult(
                AgentEnrollmentCommitDisposition.Conflict,
                null,
                "enrollment.persistence_conflict");
        }
    }

    /// <inheritdoc />
    public async ValueTask AuditEnrollmentDenialAsync(
        string internalCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        AddAudit(
            context,
            "Anonymous",
            "enrollment",
            "agent.enroll",
            "AgentEnrollment",
            "*",
            "Denied",
            Guid.NewGuid(),
            SanitiseCode(internalCode, "enrollment.denied"),
            now);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentHeartbeatOutcome> RecordHeartbeatAsync(
        ValidatedAgentHeartbeat heartbeat,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(heartbeat);
        AgentHeartbeatRequest request = heartbeat.Request;
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        RegisteredAgentRow? agent = await context.Agents
            .SingleOrDefaultAsync(row => row.AgentId == request.AgentId, cancellationToken)
            .ConfigureAwait(false);
        if (agent is null || agent.State != "Active" || agent.RevokedAt is not null)
        {
            return new AgentHeartbeatOutcome(
                AgentHeartbeatDisposition.AgentInactive,
                null,
                0,
                null,
                "heartbeat.agent_inactive");
        }

        AgentHeartbeatCursorRow? cursor = await context.AgentHeartbeatCursors
            .SingleOrDefaultAsync(row => row.AgentId == request.AgentId, cancellationToken)
            .ConfigureAwait(false);
        AgentHeartbeatRow? messageReplay = await context.AgentHeartbeats
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.MessageId == request.MessageId, cancellationToken)
            .ConfigureAwait(false);
        if (messageReplay is not null)
        {
            bool exactReplay = cursor is not null &&
                cursor.HighestAcceptedSequence >= messageReplay.Sequence &&
                messageReplay.AgentId == request.AgentId &&
                messageReplay.Sequence == request.Sequence &&
                messageReplay.PayloadSha256 is not null &&
                CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(messageReplay.PayloadSha256),
                    Encoding.ASCII.GetBytes(heartbeat.PayloadSha256));
            return exactReplay
                ? new AgentHeartbeatOutcome(
                    AgentHeartbeatDisposition.Duplicate,
                    messageReplay.ReceivedAt,
                    cursor!.HighestAcceptedSequence,
                    CalculateClockSkewMilliseconds(messageReplay.ReceivedAt, messageReplay.AgentTime),
                    null)
                : HeartbeatConflict(cursor?.HighestAcceptedSequence ?? 0, "heartbeat.message_conflict");
        }

        AgentHeartbeatRow? sequenceReplay = await context.AgentHeartbeats
            .AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.AgentId == request.AgentId && row.Sequence == request.Sequence,
                cancellationToken)
            .ConfigureAwait(false);
        if (sequenceReplay is not null)
        {
            return HeartbeatConflict(cursor?.HighestAcceptedSequence ?? 0, "heartbeat.sequence_conflict");
        }

        if (cursor is null)
        {
            bool retainedHistoryExists = await context.AgentHeartbeats
                .AsNoTracking()
                .AnyAsync(row => row.AgentId == request.AgentId, cancellationToken)
                .ConfigureAwait(false);
            if (retainedHistoryExists)
            {
                return HeartbeatConflict(0, "heartbeat.cursor_missing");
            }

            cursor = new AgentHeartbeatCursorRow
            {
                AgentId = request.AgentId,
                HighestAcceptedSequence = 0,
                LastMessageId = Guid.Empty,
                UpdatedAt = receivedAt,
                ConcurrencyToken = Guid.NewGuid(),
            };
            context.AgentHeartbeatCursors.Add(cursor);
        }

        if (request.Sequence <= cursor.HighestAcceptedSequence)
        {
            return HeartbeatConflict(cursor.HighestAcceptedSequence, "heartbeat.sequence_replayed");
        }

        long previousSequence = cursor.HighestAcceptedSequence;
        bool gapDetected = request.Sequence > previousSequence + 1;
        context.AgentHeartbeats.Add(new AgentHeartbeatRow
        {
            HeartbeatId = Guid.NewGuid(),
            AgentId = request.AgentId,
            MessageId = request.MessageId,
            Sequence = request.Sequence,
            AgentVersion = request.AgentVersion,
            ProtocolMinimum = request.ProtocolMinimum,
            ProtocolMaximum = request.ProtocolMaximum,
            QueueDepth = request.QueueDepth,
            OldestQueuedAt = request.OldestQueuedAt,
            AgentTime = request.AgentTime,
            ReceivedAt = receivedAt,
            PayloadSha256 = heartbeat.PayloadSha256,
            PreviousAcceptedSequence = previousSequence,
            GapDetected = gapDetected,
        });
        cursor.HighestAcceptedSequence = request.Sequence;
        cursor.LastMessageId = request.MessageId;
        cursor.UpdatedAt = cursor.UpdatedAt > receivedAt ? cursor.UpdatedAt : receivedAt;
        cursor.ConcurrencyToken = Guid.NewGuid();
        agent.AgentVersion = request.AgentVersion;
        agent.LastSeenAt = agent.LastSeenAt is null || agent.LastSeenAt < receivedAt
            ? receivedAt
            : agent.LastSeenAt;
        agent.ConcurrencyToken = Guid.NewGuid();

        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AgentHeartbeatOutcome(
                gapDetected
                    ? AgentHeartbeatDisposition.AcceptedWithGap
                    : AgentHeartbeatDisposition.Accepted,
                receivedAt,
                request.Sequence,
                CalculateClockSkewMilliseconds(receivedAt, request.AgentTime),
                null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return HeartbeatConflict(previousSequence, "heartbeat.concurrency_conflict");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return HeartbeatConflict(previousSequence, "heartbeat.persistence_conflict");
        }
    }

    /// <inheritdoc />
    public async ValueTask<AgentAssignmentOutcome> GetAssignmentsAsync(
        Guid agentId,
        string agentVersion,
        string? afterVersion,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        ActiveAgentProjection? agent = await context.Agents
            .AsNoTracking()
            .Where(row => row.AgentId == agentId && row.State == "Active" && row.RevokedAt == null)
            .Select(row => new ActiveAgentProjection(row.AgentId, row.Environment, row.AgentVersion))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agent is null || !string.Equals(agent.AgentVersion, agentVersion, StringComparison.Ordinal))
        {
            return new AgentAssignmentOutcome(
                AgentAssignmentDisposition.AgentInactive,
                null,
                null,
                "assignments.agent_inactive");
        }

        IQueryable<AssignmentProjection> assignmentQuery = context.Instances
            .AsNoTracking()
            .Where(row => row.AssignedAgentId == agentId &&
                row.Environment == agent.Environment &&
                row.Enabled &&
                row.ArchivedAt == null)
            .OrderBy(row => row.InstanceId)
            .Select(row => new AssignmentProjection(
                row.InstanceId,
                row.DisplayName,
                row.ProviderType,
                row.Environment,
                row.EndpointJson,
                row.MonitoringCredentialReference,
                row.TagsJson,
                row.IntervalSeconds,
                row.TimeoutSeconds,
                row.RetryCount,
                row.UpdatedAt,
                row.ConcurrencyToken))
            .Take(AgentFleetProtocol.MaximumAssignments + 1);
        List<AssignmentProjection> rows = new(AgentFleetProtocol.MaximumAssignments);
        long payloadBytes = 0;
        await foreach (AssignmentProjection row in assignmentQuery
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            if (rows.Count == AgentFleetProtocol.MaximumAssignments)
            {
                return AssignmentLimitExceeded();
            }

            try
            {
                payloadBytes = checked(
                    payloadBytes +
                    Encoding.UTF8.GetByteCount(row.EndpointJson) +
                    Encoding.UTF8.GetByteCount(row.TagsJson));
            }
            catch (OverflowException)
            {
                return AssignmentLimitExceeded();
            }

            if (payloadBytes > AgentFleetProtocol.MaximumAssignmentPayloadBytes)
            {
                return AssignmentLimitExceeded();
            }

            rows.Add(row);
        }

        List<AgentReadOnlyAssignment> assignments = new(rows.Count);
        try
        {
            foreach (AssignmentProjection row in rows)
            {
                using JsonDocument endpointDocument = JsonDocument.Parse(row.EndpointJson, DocumentOptions);
                using JsonDocument tagsDocument = JsonDocument.Parse(row.TagsJson, DocumentOptions);
                if (endpointDocument.RootElement.ValueKind != JsonValueKind.Object ||
                    tagsDocument.RootElement.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
                {
                    return InvalidStoredAssignments();
                }

                assignments.Add(new AgentReadOnlyAssignment(
                    row.InstanceId,
                    row.DisplayName,
                    row.ProviderType,
                    row.Environment,
                    endpointDocument.RootElement.Clone(),
                    row.MonitoringCredentialReference,
                    tagsDocument.RootElement.Clone(),
                    row.IntervalSeconds,
                    row.TimeoutSeconds,
                    row.RetryCount,
                    row.UpdatedAt));
            }
        }
        catch (JsonException)
        {
            return InvalidStoredAssignments();
        }

        string version = AgentAssignmentVersion.Compute(agentId, assignments);
        if (string.Equals(afterVersion, version, StringComparison.Ordinal))
        {
            return new AgentAssignmentOutcome(
                AgentAssignmentDisposition.NotModified,
                null,
                version,
                null);
        }

        AgentAssignmentSnapshot snapshot = new(
            AgentFleetProtocol.CurrentSchemaVersion,
            agentId,
            version,
            generatedAt,
            assignments);
        return new AgentAssignmentOutcome(
            AgentAssignmentDisposition.Available,
            snapshot,
            version,
            null);
    }

    /// <inheritdoc />
    public async ValueTask<AgentFleetCatalogue> GetAuthorisedAgentsAsync(
        string subjectId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        Guid? userId = await context.Users
            .AsNoTracking()
            .Where(row => row.SubjectId == subjectId && row.State == "Active")
            .Select(row => (Guid?)row.UserId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (userId is null)
        {
            return new AgentFleetCatalogue(false, []);
        }

        AuthorizationScope[] scopes = await GetScopesAsync(
            context,
            userId.Value,
            permissionCode,
            now,
            cancellationToken).ConfigureAwait(false);
        bool global = scopes.Any(scope => scope.ScopeType == "Global" && scope.ScopeValue == "*");
        string[] environments = scopes
            .Where(scope => scope.ScopeType == "Environment")
            .Select(scope => scope.ScopeValue)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (!global && environments.Length == 0)
        {
            return new AgentFleetCatalogue(false, []);
        }

        AgentFleetView[] agents = await context.Agents
            .AsNoTracking()
            .Where(row => global || environments.Contains(row.Environment))
            .OrderBy(row => row.Environment)
            .ThenBy(row => row.DisplayName)
            .ThenBy(row => row.AgentId)
            .Select(row => new AgentFleetView(
                row.AgentId,
                row.DisplayName,
                row.Environment,
                row.Platform,
                row.AgentVersion,
                row.State,
                row.EnrolledAt,
                row.RevokedAt,
                row.LastSeenAt))
            .Take(AgentFleetProtocol.MaximumCatalogueEntries + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (agents.Length > AgentFleetProtocol.MaximumCatalogueEntries)
        {
            return new AgentFleetCatalogue(
                true,
                [],
                false,
                "agent.catalogue_limit_exceeded");
        }

        return new AgentFleetCatalogue(true, agents);
    }

    /// <inheritdoc />
    public async ValueTask<AgentRevocationOutcome> RevokeAgentAsync(
        string subjectId,
        Guid agentId,
        string permissionCode,
        string reasonCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using ServerDbContext context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);
        PlatformUserRow? user = await context.Users
            .SingleOrDefaultAsync(
                row => row.SubjectId == subjectId && row.State == "Active",
                cancellationToken)
            .ConfigureAwait(false);
        RegisteredAgentRow? agent = await context.Agents
            .SingleOrDefaultAsync(row => row.AgentId == agentId, cancellationToken)
            .ConfigureAwait(false);
        AuthorizationScope[] scopes = user is null
            ? []
            : await GetScopesAsync(
                context,
                user.UserId,
                permissionCode,
                now,
                cancellationToken).ConfigureAwait(false);
        bool authorised = agent is not null && scopes.Any(scope =>
            (scope.ScopeType == "Global" && scope.ScopeValue == "*") ||
            (scope.ScopeType == "Environment" && scope.ScopeValue == agent.Environment));
        if (!authorised)
        {
            AddAudit(
                context,
                "Human",
                subjectId,
                "agent.revoke",
                "Agent",
                agentId.ToString("D"),
                "Denied",
                Guid.NewGuid(),
                "authorization.denied",
                now);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AgentRevocationOutcome(
                AgentRevocationDisposition.Denied,
                agentId,
                null,
                "authorization.denied");
        }

        bool alreadyRevoked = agent!.State == "Revoked" || agent.RevokedAt is not null;
        DateTimeOffset effectiveRevokedAt = agent.RevokedAt ?? now;
        agent.State = "Revoked";
        agent.RevokedAt = effectiveRevokedAt;
        agent.ConcurrencyToken = Guid.NewGuid();
        AgentCertificateRow[] certificates = await context.AgentCertificates
            .Where(row => row.AgentId == agentId)
            .Take(AgentFleetProtocol.MaximumCertificatesPerAgent + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (certificates.Length > AgentFleetProtocol.MaximumCertificatesPerAgent)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AgentRevocationOutcome(
                AgentRevocationDisposition.Conflict,
                agentId,
                null,
                "agent.certificate_limit_exceeded");
        }

        foreach (AgentCertificateRow certificate in certificates)
        {
            if (certificate.State == "Revoked" && certificate.RevokedAt is not null)
            {
                continue;
            }

            certificate.State = "Revoked";
            certificate.RevokedAt ??= effectiveRevokedAt;
            certificate.RevocationReasonCode ??= reasonCode;
            certificate.ConcurrencyToken = Guid.NewGuid();
        }

        AddAudit(
            context,
            "Human",
            subjectId,
            "agent.revoke",
            "Agent",
            agentId.ToString("D"),
            "Succeeded",
            Guid.NewGuid(),
            alreadyRevoked ? "agent.already_revoked" : reasonCode,
            now);
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new AgentRevocationOutcome(
                alreadyRevoked
                    ? AgentRevocationDisposition.AlreadyRevoked
                    : AgentRevocationDisposition.Revoked,
                agentId,
                effectiveRevokedAt,
                null);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AgentRevocationOutcome(
                AgentRevocationDisposition.Conflict,
                agentId,
                null,
                "agent.revocation_conflict");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new AgentRevocationOutcome(
                AgentRevocationDisposition.Conflict,
                agentId,
                null,
                "agent.revocation_persistence_conflict");
        }
    }

    /// <summary>Checks all mutable enrollment facts again inside the serializable transaction.</summary>
    /// <param name="token">Tracked token row, if present.</param>
    /// <param name="commit">Validated evidence received from the Application boundary.</param>
    /// <param name="now">Trusted transaction instant.</param>
    /// <returns><see langword="true"/> only for an exact, current, one-time commit.</returns>
    private static bool EnrollmentCommitMatches(
        AgentEnrollmentTokenRow? token,
        AgentEnrollmentCommit commit,
        DateTimeOffset now)
    {
        AgentCertificateIssueResult certificate = commit.Certificate;
        return token is not null && token.State == "Active" &&
            token.ConcurrencyToken == commit.ExpectedTokenConcurrency &&
            token.ConsumedAt is null && token.ConsumedByAgentId is null && token.RevokedAt is null &&
            token.IssuedAt <= now && token.ExpiresAt > now &&
            string.Equals(
                token.HashAlgorithm,
                AgentEnrollmentTokenProof.HashAlgorithm,
                StringComparison.Ordinal) &&
            token.Salt.Length == AgentEnrollmentTokenProof.SaltLength &&
            token.SecretHash.Length == SHA256.HashSizeInBytes &&
            commit.PresentedSecretHash.Length == SHA256.HashSizeInBytes &&
            CryptographicOperations.FixedTimeEquals(token.SecretHash, commit.PresentedSecretHash.Span) &&
            string.Equals(token.ExpectedInstallationId, commit.Request.InstallationId, StringComparison.Ordinal) &&
            string.Equals(token.ExpectedEnvironment, commit.Request.Environment, StringComparison.Ordinal) &&
            string.Equals(token.ExpectedPlatform, commit.Request.Platform, StringComparison.Ordinal) &&
            string.Equals(token.Scope, commit.Request.RequestedScope, StringComparison.Ordinal) &&
            certificate.Disposition == AgentCertificateIssueDisposition.Issued &&
            IsUpperHex(certificate.CertificateThumbprint, 40, 160) &&
            IsUpperHex(certificate.PublicKeySha256, 64, 64) &&
            certificate.NotBefore is not null && certificate.NotAfter is not null &&
            certificate.NotBefore <= now + AgentFleetProtocol.MaximumFutureClockSkew &&
            certificate.NotAfter > now && certificate.NotAfter > certificate.NotBefore &&
            certificate.CertificateSigningRequestSha256.Length == SHA256.HashSizeInBytes &&
            commit.Request.CertificateSigningRequestSha256.Length == SHA256.HashSizeInBytes &&
            CryptographicOperations.FixedTimeEquals(
                certificate.CertificateSigningRequestSha256.Span,
                commit.Request.CertificateSigningRequestSha256.Span);
    }

    /// <summary>Persists a generic enrollment denial without storing any presented token or certificate material.</summary>
    /// <param name="context">Current persistence context.</param>
    /// <param name="transaction">Current serializable transaction.</param>
    /// <param name="tokenId">Non-secret public token identifier.</param>
    /// <param name="code">Sanitised internal result code.</param>
    /// <param name="now">Trusted decision instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <param name="disposition">Typed result returned after committing the audit.</param>
    /// <returns>The committed denial or conflict result.</returns>
    private static async ValueTask<AgentEnrollmentCommitResult> DenyEnrollmentCommitAsync(
        ServerDbContext context,
        IDbContextTransaction transaction,
        Guid tokenId,
        string code,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        AgentEnrollmentCommitDisposition disposition = AgentEnrollmentCommitDisposition.Denied)
    {
        AddAudit(
            context,
            "EnrollmentToken",
            tokenId.ToString("D"),
            "agent.enroll",
            "AgentEnrollment",
            "*",
            disposition == AgentEnrollmentCommitDisposition.Conflict ? "Failed" : "Denied",
            Guid.NewGuid(),
            code,
            now);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new AgentEnrollmentCommitResult(disposition, null, code);
    }

    /// <summary>Loads unexpired server-side RBAC scopes for one active human identity.</summary>
    /// <param name="context">Current persistence context.</param>
    /// <param name="userId">Resolved platform user identifier.</param>
    /// <param name="permissionCode">Exact permission required by the operation.</param>
    /// <param name="now">Trusted authorisation instant.</param>
    /// <param name="cancellationToken">Cancellation signal.</param>
    /// <returns>Applicable scopes without trusting client-provided claims.</returns>
    private static async ValueTask<AuthorizationScope[]> GetScopesAsync(
        ServerDbContext context,
        Guid userId,
        string permissionCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        AuthorizationScope[] assigned = await (
            from assignment in context.RoleAssignments.AsNoTracking()
            join rolePermission in context.RolePermissions.AsNoTracking()
                on assignment.RoleId equals rolePermission.RoleId
            join permission in context.Permissions.AsNoTracking()
                on rolePermission.PermissionId equals permission.PermissionId
            where assignment.UserId == userId && permission.Code == permissionCode
            select new AuthorizationScope(
                assignment.ScopeType,
                assignment.ScopeValue,
                assignment.ExpiresAt))
            .Take(AgentFleetProtocol.MaximumAuthorisationScopes + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        if (assigned.Length > AgentFleetProtocol.MaximumAuthorisationScopes)
        {
            return [];
        }

        return assigned
            .Where(scope => scope.ExpiresAt is null || scope.ExpiresAt > now)
            .ToArray();
    }

    /// <summary>Calculates the signed server-minus-Agent clock-skew estimate in whole milliseconds.</summary>
    /// <param name="receivedAt">Trusted server receipt instant.</param>
    /// <param name="agentTime">Agent-reported clock instant.</param>
    /// <returns>Signed whole-millisecond estimate.</returns>
    private static long CalculateClockSkewMilliseconds(DateTimeOffset receivedAt, DateTimeOffset agentTime) =>
        (receivedAt - agentTime).Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>Creates a fail-closed heartbeat conflict without acceptance evidence.</summary>
    /// <param name="highestSequence">Last trustworthy cursor known in this transaction.</param>
    /// <param name="errorCode">Sanitised conflict code.</param>
    /// <returns>Conflict outcome.</returns>
    private static AgentHeartbeatOutcome HeartbeatConflict(long highestSequence, string errorCode) =>
        new(AgentHeartbeatDisposition.Conflict, null, highestSequence, null, errorCode);

    /// <summary>Creates a fail-closed result for malformed persisted assignment JSON.</summary>
    /// <returns>Invalid stored-configuration outcome without a partial snapshot.</returns>
    private static AgentAssignmentOutcome InvalidStoredAssignments() =>
        new(
            AgentAssignmentDisposition.InvalidStoredConfiguration,
            null,
            null,
            "assignments.invalid_stored_configuration");

    /// <summary>Creates a bounded-work refusal without exposing a partial assignment snapshot.</summary>
    /// <returns>Limit-exceeded outcome without partial data.</returns>
    private static AgentAssignmentOutcome AssignmentLimitExceeded() =>
        new(
            AgentAssignmentDisposition.LimitExceeded,
            null,
            null,
            "assignments.limit_exceeded");

    /// <summary>Bounds an internal machine code before including it in audit JSON.</summary>
    /// <param name="candidate">Candidate internal code.</param>
    /// <param name="fallback">Safe fallback when the candidate is outside policy.</param>
    /// <returns>Safe machine code.</returns>
    private static string SanitiseCode(string? candidate, string fallback) =>
        candidate is not null && candidate.Length is >= 3 and <= 100 &&
        candidate.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')
            ? candidate
            : fallback;

    /// <summary>Checks a bounded uppercase hexadecimal value without accepting separators or whitespace.</summary>
    /// <param name="candidate">Candidate hexadecimal text.</param>
    /// <param name="minimumLength">Minimum admitted length.</param>
    /// <param name="maximumLength">Maximum admitted length.</param>
    /// <returns><see langword="true"/> only for an exact canonical hexadecimal value.</returns>
    private static bool IsUpperHex(string? candidate, int minimumLength, int maximumLength) =>
        candidate is not null && candidate.Length >= minimumLength && candidate.Length <= maximumLength &&
        candidate.All(character => char.IsAsciiDigit(character) || character is >= 'A' and <= 'F');

    /// <summary>Adds one append-only, sanitised security audit row to the caller's transaction.</summary>
    /// <param name="context">Current persistence context.</param>
    /// <param name="actorType">Stable actor category.</param>
    /// <param name="actorId">Sanitised actor identifier.</param>
    /// <param name="action">Stable audited action.</param>
    /// <param name="targetType">Stable target category.</param>
    /// <param name="targetId">Target identifier.</param>
    /// <param name="outcome">Canonical audit outcome.</param>
    /// <param name="correlationId">Correlation identifier for the audited request.</param>
    /// <param name="code">Sanitised machine-readable result code.</param>
    /// <param name="occurredAt">Trusted audit instant.</param>
    private static void AddAudit(
        ServerDbContext context,
        string actorType,
        string actorId,
        string action,
        string targetType,
        string targetId,
        string outcome,
        Guid correlationId,
        string code,
        DateTimeOffset occurredAt) =>
        context.AuditEntries.Add(new AuditEntryRow
        {
            AuditEntryId = Guid.NewGuid(),
            OccurredAt = occurredAt,
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Outcome = outcome,
            CorrelationId = correlationId,
            DetailsJson = JsonSerializer.Serialize(new { code }, SerializerOptions),
        });

    /// <summary>Projects only the Agent facts required to authorise assignment retrieval.</summary>
    /// <param name="AgentId">Owning Agent identifier.</param>
    /// <param name="Environment">Exact assignment environment.</param>
    /// <param name="AgentVersion">Currently accepted Agent version.</param>
    private sealed record ActiveAgentProjection(Guid AgentId, string Environment, string AgentVersion);

    /// <summary>Projects assignment-safe columns and deliberately omits the administrative credential reference.</summary>
    /// <param name="InstanceId">Database-instance identifier.</param>
    /// <param name="DisplayName">Instance display name.</param>
    /// <param name="ProviderType">Stable provider identifier.</param>
    /// <param name="Environment">Exact environment.</param>
    /// <param name="EndpointJson">Non-secret endpoint JSON.</param>
    /// <param name="MonitoringCredentialReference">Opaque monitoring-only reference.</param>
    /// <param name="TagsJson">Non-secret tag JSON.</param>
    /// <param name="IntervalSeconds">Configured observation interval.</param>
    /// <param name="TimeoutSeconds">Configured provider timeout.</param>
    /// <param name="RetryCount">Configured retry count.</param>
    /// <param name="UpdatedAt">Configuration update instant.</param>
    /// <param name="ConcurrencyToken">Version token included in the snapshot digest.</param>
    private sealed record AssignmentProjection(
        Guid InstanceId,
        string DisplayName,
        string ProviderType,
        string Environment,
        string EndpointJson,
        string? MonitoringCredentialReference,
        string TagsJson,
        int IntervalSeconds,
        int TimeoutSeconds,
        int RetryCount,
        DateTimeOffset UpdatedAt,
        Guid ConcurrencyToken);

    /// <summary>Represents one server-resolved, unexpired human authorisation scope.</summary>
    /// <param name="ScopeType">Canonical scope category.</param>
    /// <param name="ScopeValue">Canonical scope value.</param>
    /// <param name="ExpiresAt">Optional exclusive expiry instant.</param>
    private sealed record AuthorizationScope(
        string ScopeType,
        string ScopeValue,
        DateTimeOffset? ExpiresAt);
}
