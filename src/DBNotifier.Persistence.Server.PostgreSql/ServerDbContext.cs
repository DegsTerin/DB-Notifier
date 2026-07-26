// Module purpose: Implements Server Db Context for central PostgreSQL persistence with transactional and authorisation boundaries.
using DBNotifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Server.PostgreSql;

/// <summary>Maps the central PostgreSQL persistence model without connecting directly to monitored databases.</summary>
/// <param name="options">Provider-specific Entity Framework options supplied by the server host or tests.</param>
public sealed class ServerDbContext(DbContextOptions<ServerDbContext> options) : DbContext(options)
{
    public DbSet<DatabaseInstanceRow> Instances => Set<DatabaseInstanceRow>();
    public DbSet<RegisteredAgentRow> Agents => Set<RegisteredAgentRow>();

    /// <summary>Gets the hashed, scope-bound one-time Agent enrollment challenges.</summary>
    public DbSet<AgentEnrollmentTokenRow> AgentEnrollmentTokens => Set<AgentEnrollmentTokenRow>();

    /// <summary>Gets the normalised revocable Agent certificate metadata.</summary>
    public DbSet<AgentCertificateRow> AgentCertificates => Set<AgentCertificateRow>();

    public DbSet<AgentCapabilityRow> AgentCapabilities => Set<AgentCapabilityRow>();
    public DbSet<AgentHeartbeatRow> AgentHeartbeats => Set<AgentHeartbeatRow>();

    /// <summary>Gets durable heartbeat anti-replay cursors independent of heartbeat-detail retention.</summary>
    public DbSet<AgentHeartbeatCursorRow> AgentHeartbeatCursors => Set<AgentHeartbeatCursorRow>();

    public DbSet<HealthSampleRow> HealthSamples => Set<HealthSampleRow>();

    /// <summary>Gets the durable per-Agent contiguous observation cursors.</summary>
    public DbSet<AgentObservationCursorRow> AgentObservationCursors => Set<AgentObservationCursorRow>();

    /// <summary>Gets immutable protocol evidence for terminally rejected observation-stream positions.</summary>
    public DbSet<RejectedObservationSequenceRow> RejectedObservationSequences =>
        Set<RejectedObservationSequenceRow>();

    /// <summary>Gets the durable current reconciled state for each database instance.</summary>
    public DbSet<InstanceObservationStateRow> InstanceObservationStates => Set<InstanceObservationStateRow>();
    public DbSet<EventRecordRow> Events => Set<EventRecordRow>();
    public DbSet<IncidentRow> Incidents => Set<IncidentRow>();
    public DbSet<MaintenanceWindowRow> MaintenanceWindows => Set<MaintenanceWindowRow>();
    public DbSet<AlertRuleRow> AlertRules => Set<AlertRuleRow>();
    public DbSet<NotificationChannelRow> NotificationChannels => Set<NotificationChannelRow>();

    /// <summary>Gets explicit environment-scoped alert-rule to notification-channel routes.</summary>
    public DbSet<AlertRuleChannelBindingRow> AlertRuleChannelBindings => Set<AlertRuleChannelBindingRow>();

    public DbSet<NotificationDeliveryRow> NotificationDeliveries => Set<NotificationDeliveryRow>();
    public DbSet<AdministrativeCommandRow> AdministrativeCommands => Set<AdministrativeCommandRow>();
    public DbSet<CommandAttemptRow> CommandAttempts => Set<CommandAttemptRow>();
    /// <summary>Gets durable per-Agent cursors for the isolated command-transport sandbox.</summary>
    public DbSet<ServerCommandTransportCursorRow> CommandTransportCursors => Set<ServerCommandTransportCursorRow>();

    /// <summary>Gets exact request/response replay evidence for the isolated command-transport sandbox.</summary>
    public DbSet<ServerCommandTransportJournalRow> CommandTransportJournal => Set<ServerCommandTransportJournalRow>();
    public DbSet<PlatformUserRow> Users => Set<PlatformUserRow>();
    public DbSet<RoleRow> Roles => Set<RoleRow>();
    public DbSet<PermissionRow> Permissions => Set<PermissionRow>();
    public DbSet<RolePermissionRow> RolePermissions => Set<RolePermissionRow>();
    public DbSet<RoleAssignmentRow> RoleAssignments => Set<RoleAssignmentRow>();
    public DbSet<AuditEntryRow> AuditEntries => Set<AuditEntryRow>();
    public DbSet<ServerOutboxMessageRow> OutboxMessages => Set<ServerOutboxMessageRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureInventory(modelBuilder);
        ConfigureAgentFleetIdentity(modelBuilder);
        ConfigureMonitoring(modelBuilder);
        ConfigureAlerting(modelBuilder);
        ConfigureCommands(modelBuilder);
        ConfigureIdentityAndAudit(modelBuilder);
        SnakeCaseModelConvention.Apply(modelBuilder);
    }

    /// <summary>Configures one-time enrollment, normalised certificate identity and durable heartbeat cursors.</summary>
    /// <param name="modelBuilder">Model builder owned by this context.</param>
    private static void ConfigureAgentFleetIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentEnrollmentTokenRow>(entity =>
        {
            entity.ToTable("agent_enrollment_tokens", table =>
            {
                table.HasCheckConstraint(
                    "ck_agent_enrollment_token_state",
                    "state IN ('Active','Consumed','Revoked')");
                table.HasCheckConstraint(
                    "ck_agent_enrollment_token_expiry",
                    "expires_at > issued_at");
                table.HasCheckConstraint(
                    "ck_agent_enrollment_token_proof",
                    "hash_algorithm = 'SHA256' AND length(salt) = 16 AND length(secret_hash) = 32");
                table.HasCheckConstraint(
                    "ck_agent_enrollment_token_consumption",
                    "(state = 'Consumed' AND consumed_at IS NOT NULL AND consumed_by_agent_id IS NOT NULL AND revoked_at IS NULL) OR " +
                    "(state = 'Revoked' AND consumed_at IS NULL AND consumed_by_agent_id IS NULL AND revoked_at IS NOT NULL) OR " +
                    "(state = 'Active' AND consumed_at IS NULL AND consumed_by_agent_id IS NULL AND revoked_at IS NULL)");
            });
            entity.HasKey(row => row.EnrollmentTokenId);
            entity.Property(row => row.Salt).HasMaxLength(64).IsRequired();
            entity.Property(row => row.SecretHash).HasMaxLength(64).IsRequired();
            entity.Property(row => row.HashAlgorithm).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ExpectedInstallationId).HasMaxLength(160).IsRequired();
            entity.Property(row => row.ExpectedEnvironment).HasMaxLength(100).IsRequired();
            entity.Property(row => row.ExpectedPlatform).HasMaxLength(100).IsRequired();
            entity.Property(row => row.Scope).HasMaxLength(160).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.State, row.ExpiresAt });
            entity.HasIndex(row => row.ExpectedInstallationId);
            entity.HasOne<RegisteredAgentRow>()
                .WithMany()
                .HasForeignKey(row => row.ConsumedByAgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentCertificateRow>(entity =>
        {
            entity.ToTable("agent_certificates", table =>
            {
                table.HasCheckConstraint(
                    "ck_agent_certificate_state",
                    "state IN ('Active','Revoked','Superseded')");
                table.HasCheckConstraint(
                    "ck_agent_certificate_validity",
                    "not_before IS NULL OR not_after IS NULL OR not_after > not_before");
                table.HasCheckConstraint(
                    "ck_agent_certificate_revocation",
                    "(state = 'Revoked' AND revoked_at IS NOT NULL AND revocation_reason_code IS NOT NULL) OR " +
                    "(state IN ('Active','Superseded') AND revoked_at IS NULL AND revocation_reason_code IS NULL)");
            });
            entity.HasKey(row => row.AgentCertificateId);
            entity.Property(row => row.Thumbprint).HasMaxLength(160).IsRequired();
            entity.HasIndex(row => row.Thumbprint).IsUnique();
            entity.Property(row => row.PublicKeySha256).HasMaxLength(64);
            entity.Property(row => row.CertificateSigningRequestSha256).HasMaxLength(64);
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.RevocationReasonCode).HasMaxLength(100);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.AgentId, row.State });
            entity.HasOne<RegisteredAgentRow>()
                .WithMany()
                .HasForeignKey(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentHeartbeatCursorRow>(entity =>
        {
            entity.ToTable("agent_heartbeat_cursors", table =>
                table.HasCheckConstraint(
                    "ck_agent_heartbeat_cursor_sequence",
                    "highest_accepted_sequence >= 0"));
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasOne<RegisteredAgentRow>()
                .WithMany()
                .HasForeignKey(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegisteredAgentRow>(entity =>
        {
            entity.ToTable("agents", table =>
                table.HasCheckConstraint("ck_agent_state", "state IN ('Active','Revoked','Disabled','Unknown')"));
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.InstallationId).HasMaxLength(160).IsRequired();
            entity.HasIndex(row => row.InstallationId).IsUnique();
            entity.Property(row => row.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(row => row.Environment).HasMaxLength(100).IsRequired();
            entity.Property(row => row.Platform).HasMaxLength(100).IsRequired();
            entity.Property(row => row.AgentVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.HasIndex(row => row.CertificateThumbprint).IsUnique();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.Environment, row.State });
        });

        modelBuilder.Entity<DatabaseInstanceRow>(entity =>
        {
            entity.ToTable("database_instances", table =>
            {
                table.HasCheckConstraint("ck_instance_interval", "interval_seconds >= 1");
                table.HasCheckConstraint("ck_instance_timeout", "timeout_seconds >= 1");
                table.HasCheckConstraint("ck_instance_retries", "retry_count >= 0");
            });
            entity.HasKey(row => row.InstanceId);
            entity.Property(row => row.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(row => row.ProviderType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Environment).HasMaxLength(100).IsRequired();
            entity.Property(row => row.EndpointJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.TagsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.MonitoringCredentialReference).HasMaxLength(500);
            entity.Property(row => row.AdministrativeCredentialReference).HasMaxLength(500);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.Environment, row.ProviderType, row.Enabled });
            entity.HasIndex(row => row.AssignedAgentId);
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AssignedAgentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentCapabilityRow>(entity =>
        {
            entity.ToTable("agent_capabilities", table =>
                table.HasCheckConstraint("ck_agent_capability_state", "state IN ('Supported','Unsupported','Unavailable','Unknown')"));
            entity.HasKey(row => row.AgentCapabilityId);
            entity.Property(row => row.CapabilityId).HasMaxLength(100).IsRequired();
            entity.Property(row => row.ProviderType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Platform).HasMaxLength(100).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ReasonCode).HasMaxLength(100);
            entity.HasIndex(row => new { row.AgentId, row.CapabilityId, row.ProviderType, row.ProviderVersion, row.Platform }).IsUnique();
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentHeartbeatRow>(entity =>
        {
            entity.ToTable("agent_heartbeats", table =>
            {
                table.HasCheckConstraint("ck_heartbeat_sequence", "sequence >= 0");
                table.HasCheckConstraint("ck_heartbeat_protocol", "protocol_minimum >= 1 AND protocol_maximum >= protocol_minimum");
                table.HasCheckConstraint("ck_heartbeat_queue", "queue_depth >= 0");
            });
            entity.HasKey(row => row.HeartbeatId);
            entity.Property(row => row.AgentVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.PayloadSha256).HasMaxLength(64);
            entity.HasIndex(row => row.MessageId).IsUnique();
            entity.HasIndex(row => new { row.AgentId, row.Sequence }).IsUnique();
            entity.HasIndex(row => new { row.AgentId, row.ReceivedAt });
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMonitoring(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthSampleRow>(entity =>
        {
            entity.ToTable("health_samples", table =>
            {
                table.HasCheckConstraint("ck_health_duration", "duration_milliseconds >= 0");
                table.HasCheckConstraint("ck_health_attempts", "attempt_count >= 1");
                table.HasCheckConstraint("ck_health_status", "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");
                table.HasCheckConstraint("ck_health_evidence", "evidence_level IN ('ProviderAuthenticated','ProviderReadiness','TransportOnly','Synthetic','Unknown')");
            });
            entity.HasKey(row => row.ObservationId);
            entity.Property(row => row.ProviderType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Status).HasMaxLength(32).IsRequired();
            entity.Property(row => row.Method).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EvidenceLevel).HasMaxLength(64).IsRequired();
            entity.Property(row => row.AttemptCount).HasDefaultValue(1);
            entity.Property(row => row.ErrorCode).HasMaxLength(100);
            entity.Property(row => row.RedactedDetailsJson).HasColumnType("jsonb");
            entity.Property(row => row.PayloadHash).HasMaxLength(64);
            entity.HasIndex(row => row.MessageId).IsUnique();
            entity.HasIndex(row => new { row.AgentId, row.Sequence }).IsUnique();
            entity.HasIndex(row => new { row.InstanceId, row.ObservedAt });
            entity.HasIndex(row => new { row.Status, row.ObservedAt });
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgentObservationCursorRow>(entity =>
        {
            entity.ToTable("agent_observation_cursors", table =>
            {
                table.HasCheckConstraint("ck_agent_observation_cursor_sequence", "highest_contiguous_sequence >= 0");
                table.HasCheckConstraint(
                    "ck_agent_observation_rejection_ledger_start",
                    "rejection_ledger_start_sequence >= 1 AND " +
                    "rejection_ledger_start_sequence <= highest_contiguous_sequence + 1");
            });
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RejectedObservationSequenceRow>(entity =>
        {
            entity.ToTable("rejected_observation_sequences", table =>
            {
                table.HasCheckConstraint(
                    "ck_rejected_observation_sequence",
                    "sequence >= 1");
                table.HasCheckConstraint(
                    "ck_rejected_observation_error_code",
                    "length(error_code) BETWEEN 1 AND 100");
            });
            entity.HasKey(row => new { row.AgentId, row.Sequence });
            entity.Property(row => row.ErrorCode).HasMaxLength(100).IsRequired();
            entity.HasIndex(row => new { row.AgentId, row.MessageId });
            entity.HasOne<AgentObservationCursorRow>()
                .WithMany()
                .HasForeignKey(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstanceObservationStateRow>(entity =>
        {
            entity.ToTable("instance_observation_states", table =>
            {
                table.HasCheckConstraint("ck_instance_observation_state_sequence", "last_processed_sequence >= 1");
                table.HasCheckConstraint("ck_instance_observation_state_status", "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");
            });
            entity.HasKey(row => row.InstanceId);
            entity.Property(row => row.Status).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.AgentId, row.LastProcessedSequence });
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventRecordRow>(entity =>
        {
            entity.ToTable("events", table =>
                table.HasCheckConstraint("ck_event_severity", "severity IN ('Info','Warning','Error','Critical')"));
            entity.HasKey(row => row.EventId);
            entity.Property(row => row.EventType).HasMaxLength(100).IsRequired();
            entity.Property(row => row.Severity).HasMaxLength(32).IsRequired();
            entity.Property(row => row.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(row => new { row.InstanceId, row.ObservedAt });
            entity.HasIndex(row => new { row.EventType, row.ObservedAt });
            entity.HasIndex(row => row.CorrelationId);
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AgentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HealthSampleRow>().WithMany().HasForeignKey(row => row.SourceObservationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IncidentRow>(entity =>
        {
            entity.ToTable("incidents", table =>
            {
                table.HasCheckConstraint("ck_incident_state", "status IN ('Open','Acknowledged','Closed')");
                table.HasCheckConstraint("ck_incident_severity", "severity IN ('Info','Warning','Error','Critical')");
            });
            entity.HasKey(row => row.IncidentId);
            entity.Property(row => row.Status).HasMaxLength(32).IsRequired();
            entity.Property(row => row.Severity).HasMaxLength(32).IsRequired();
            entity.Property(row => row.Title).HasMaxLength(300).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.InstanceId, row.Status });
            entity.HasIndex(row => row.OpenedAt);
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MaintenanceWindowRow>(entity =>
        {
            entity.ToTable("maintenance_windows", table => table.HasCheckConstraint("ck_maintenance_range", "ends_at > starts_at"));
            entity.HasKey(row => row.MaintenanceWindowId);
            entity.Property(row => row.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.InstanceId, row.StartsAt, row.EndsAt });
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAlerting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertRuleRow>(entity =>
        {
            entity.ToTable("alert_rules");
            entity.HasKey(row => row.AlertRuleId);
            entity.Property(row => row.Name).HasMaxLength(200).IsRequired();
            entity.Property(row => row.RuleType).HasMaxLength(100).IsRequired();
            entity.Property(row => row.InstanceScopeJson).HasColumnType("jsonb");
            entity.Property(row => row.ConfigurationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.Enabled, row.RuleType });
        });

        modelBuilder.Entity<NotificationChannelRow>(entity =>
        {
            entity.ToTable("notification_channels");
            entity.HasKey(row => row.NotificationChannelId);
            entity.Property(row => row.Name).HasMaxLength(200).IsRequired();
            entity.Property(row => row.ChannelType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.NonSecretConfigurationJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.CredentialReference).HasMaxLength(500);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.ChannelType, row.Enabled });
        });

        modelBuilder.Entity<AlertRuleChannelBindingRow>(entity =>
        {
            entity.ToTable("alert_rule_channel_bindings");
            entity.HasKey(row => row.AlertRuleChannelBindingId);
            entity.Property(row => row.Environment).HasMaxLength(100).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasAlternateKey(row => new
            {
                row.AlertRuleChannelBindingId,
                row.NotificationChannelId,
            });
            entity.HasIndex(row => new { row.AlertRuleId, row.NotificationChannelId, row.Environment }).IsUnique();
            entity.HasIndex(row => new { row.Environment, row.Enabled });
            entity.HasOne<AlertRuleRow>()
                .WithMany()
                .HasForeignKey(row => row.AlertRuleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<NotificationChannelRow>()
                .WithMany()
                .HasForeignKey(row => row.NotificationChannelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NotificationDeliveryRow>(entity =>
        {
            entity.ToTable("notification_deliveries", table =>
            {
                table.HasCheckConstraint("ck_delivery_attempts", "attempt_count >= 0");
                table.HasCheckConstraint(
                    "ck_delivery_state",
                    "state IN ('Pending','Delivering','Delivered','Failed','Cancelled','Quarantined','DeadLettered','Ambiguous')");
                table.HasCheckConstraint(
                    "ck_delivery_pending_provenance",
                    "state <> 'Pending' OR (alert_rule_channel_binding_id IS NOT NULL AND idempotency_key IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_delivery_quarantine_reason",
                    "state <> 'Quarantined' OR error_code IS NOT NULL");
                table.HasCheckConstraint("ck_delivery_fence", "lease_fence >= 0");
                table.HasCheckConstraint(
                    "ck_delivery_lease_tuple",
                    "(lease_owner_id IS NULL AND lease_expires_at IS NULL AND handoff_started_at IS NULL) OR " +
                    "(lease_owner_id IS NOT NULL AND lease_expires_at IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_delivery_terminal_evidence",
                    "(state = 'DeadLettered' AND dead_lettered_at IS NOT NULL AND ambiguous_at IS NULL) OR " +
                    "(state = 'Ambiguous' AND ambiguous_at IS NOT NULL AND dead_lettered_at IS NULL) OR " +
                    "(state NOT IN ('DeadLettered','Ambiguous') AND dead_lettered_at IS NULL AND ambiguous_at IS NULL)");
            });
            entity.HasKey(row => row.NotificationDeliveryId);
            entity.Property(row => row.IdempotencyKey).HasMaxLength(200);
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ErrorCode).HasMaxLength(100);
            entity.HasIndex(row => new { row.State, row.CreatedAt });
            entity.HasIndex(row => new { row.State, row.AvailableAt, row.LeaseExpiresAt, row.CreatedAt });
            entity.HasIndex(row => row.IdempotencyKey).IsUnique();
            entity.HasIndex(row => new { row.AlertRuleChannelBindingId, row.EventId }).IsUnique();
            entity.HasOne<NotificationChannelRow>().WithMany().HasForeignKey(row => row.NotificationChannelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<EventRecordRow>().WithMany().HasForeignKey(row => row.EventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AlertRuleChannelBindingRow>()
                .WithMany()
                .HasForeignKey(row => new
                {
                    row.AlertRuleChannelBindingId,
                    row.NotificationChannelId,
                })
                .HasPrincipalKey(row => new
                {
                    row.AlertRuleChannelBindingId,
                    row.NotificationChannelId,
                })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCommands(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdministrativeCommandRow>(entity =>
        {
            entity.ToTable("administrative_commands", table =>
            {
                table.HasCheckConstraint("ck_command_expiry", "expires_at > requested_at");
                table.HasCheckConstraint("ck_command_state", "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','Unsupported','UnknownOutcome')");
            });
            entity.HasKey(row => row.CommandId);
            entity.Property(row => row.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(row => row.IdempotencyKey).IsUnique();
            entity.Property(row => row.CapabilityId).HasMaxLength(100).IsRequired();
            entity.Property(row => row.TypedParametersJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(row => row.AuthorizationSnapshotReference).HasMaxLength(500).IsRequired();
            entity.Property(row => row.ExpectedAgentVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ExpectedProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.AssignedAgentId, row.State, row.ExpiresAt });
            entity.HasIndex(row => new { row.InstanceId, row.RequestedAt });
            entity.HasOne<DatabaseInstanceRow>().WithMany().HasForeignKey(row => row.InstanceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RegisteredAgentRow>().WithMany().HasForeignKey(row => row.AssignedAgentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRow>().WithMany().HasForeignKey(row => row.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommandAttemptRow>(entity =>
        {
            entity.ToTable("command_attempts", table =>
            {
                table.HasCheckConstraint("ck_command_attempt_number", "attempt_number >= 1");
                table.HasCheckConstraint("ck_command_attempt_state", "state IN ('Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");
            });
            entity.HasKey(row => row.CommandAttemptId);
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ErrorCode).HasMaxLength(100);
            entity.Property(row => row.DiagnosticReference).HasMaxLength(500);
            entity.HasIndex(row => new { row.CommandId, row.AttemptNumber }).IsUnique();
            entity.HasOne<AdministrativeCommandRow>().WithMany().HasForeignKey(row => row.CommandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HealthSampleRow>().WithMany().HasForeignKey(row => row.PostProbeObservationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServerCommandTransportCursorRow>(entity =>
        {
            entity.ToTable("agent_command_transport_cursors", table =>
                table.HasCheckConstraint(
                    "ck_agent_command_transport_cursor_sequence",
                    "highest_accepted_sequence >= 0"));
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasOne<RegisteredAgentRow>()
                .WithOne()
                .HasForeignKey<ServerCommandTransportCursorRow>(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServerCommandTransportJournalRow>(entity =>
        {
            entity.ToTable("command_transport_journal", table =>
                table.HasCheckConstraint("ck_command_transport_journal_sequence", "sequence >= 1"));
            entity.HasKey(row => row.MessageId);
            entity.Property(row => row.MessageType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.RequestPayloadSha256).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ResponsePayloadJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(row => new { row.AgentId, row.Sequence }).IsUnique();
            entity.HasIndex(row => row.ResponseMessageId).IsUnique();
            entity.HasOne<RegisteredAgentRow>()
                .WithMany()
                .HasForeignKey(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureIdentityAndAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlatformUserRow>(entity =>
        {
            entity.ToTable("users", table =>
                table.HasCheckConstraint("ck_user_state", "state IN ('Active','Disabled','Locked','Unknown')"));
            entity.HasKey(row => row.UserId);
            entity.Property(row => row.SubjectId).HasMaxLength(300).IsRequired();
            entity.HasIndex(row => row.SubjectId).IsUnique();
            entity.Property(row => row.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
        });

        modelBuilder.Entity<RoleRow>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(row => row.RoleId);
            entity.Property(row => row.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(row => row.Name).IsUnique();
            entity.Property(row => row.Description).HasMaxLength(500).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
        });

        modelBuilder.Entity<PermissionRow>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(row => row.PermissionId);
            entity.Property(row => row.Code).HasMaxLength(150).IsRequired();
            entity.HasIndex(row => row.Code).IsUnique();
            entity.Property(row => row.Description).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<RolePermissionRow>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(row => new { row.RoleId, row.PermissionId });
            entity.HasOne<RoleRow>().WithMany().HasForeignKey(row => row.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PermissionRow>().WithMany().HasForeignKey(row => row.PermissionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleAssignmentRow>(entity =>
        {
            entity.ToTable("role_assignments");
            entity.HasKey(row => row.RoleAssignmentId);
            entity.Property(row => row.ScopeType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ScopeValue).HasMaxLength(300).IsRequired();
            entity.HasIndex(row => new { row.UserId, row.RoleId, row.ScopeType, row.ScopeValue }).IsUnique();
            entity.HasOne<PlatformUserRow>().WithMany().HasForeignKey(row => row.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RoleRow>().WithMany().HasForeignKey(row => row.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEntryRow>(entity =>
        {
            entity.ToTable("audit_entries", table =>
                table.HasCheckConstraint("ck_audit_outcome", "outcome IN ('Succeeded','Failed','Denied','Unknown')"));
            entity.HasKey(row => row.AuditEntryId);
            entity.Property(row => row.ActorType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ActorId).HasMaxLength(300).IsRequired();
            entity.Property(row => row.Action).HasMaxLength(150).IsRequired();
            entity.Property(row => row.TargetType).HasMaxLength(100).IsRequired();
            entity.Property(row => row.TargetId).HasMaxLength(300).IsRequired();
            entity.Property(row => row.Outcome).HasMaxLength(32).IsRequired();
            entity.Property(row => row.DetailsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(row => row.OccurredAt);
            entity.HasIndex(row => new { row.ActorType, row.ActorId, row.OccurredAt });
            entity.HasIndex(row => row.CorrelationId);
        });

        modelBuilder.Entity<ServerOutboxMessageRow>(entity =>
        {
            entity.ToTable("outbox_messages", table =>
            {
                table.HasCheckConstraint("ck_server_outbox_schema", "schema_version >= 1");
                table.HasCheckConstraint("ck_server_outbox_attempts", "attempt_count >= 0");
                table.HasCheckConstraint("ck_server_outbox_fence", "lease_fence >= 0");
                table.HasCheckConstraint(
                    "ck_server_outbox_lease_tuple",
                    "(lease_owner_id IS NULL AND lease_expires_at IS NULL AND handoff_started_at IS NULL) OR " +
                    "(lease_owner_id IS NOT NULL AND lease_expires_at IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_server_outbox_terminal_evidence",
                    "NOT ((published_at IS NOT NULL AND dead_lettered_at IS NOT NULL) OR " +
                    "(published_at IS NOT NULL AND ambiguous_at IS NOT NULL) OR " +
                    "(dead_lettered_at IS NOT NULL AND ambiguous_at IS NOT NULL))");
            });
            entity.HasKey(row => row.MessageId);
            entity.Property(row => row.MessageType).HasMaxLength(100).IsRequired();
            entity.Property(row => row.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(row => row.ErrorCode).HasMaxLength(100);
            entity.HasIndex(row => new { row.PublishedAt, row.AvailableAt });
            entity.HasIndex(row => new
            {
                row.PublishedAt,
                row.DeadLetteredAt,
                row.AmbiguousAt,
                row.AvailableAt,
                row.LeaseExpiresAt,
            });
        });
    }
}
