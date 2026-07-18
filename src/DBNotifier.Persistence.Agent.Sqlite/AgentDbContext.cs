// Module purpose: Implements Agent Db Context for the Agent-local SQLite boundary without exposing monitored database secrets.
using DBNotifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

public sealed class AgentDbContext(DbContextOptions<AgentDbContext> options) : DbContext(options)
{
    public DbSet<AgentRegistrationRow> Registrations => Set<AgentRegistrationRow>();
    public DbSet<AgentInstanceAssignmentRow> InstanceAssignments => Set<AgentInstanceAssignmentRow>();
    public DbSet<AgentHealthObservationRow> HealthObservations => Set<AgentHealthObservationRow>();
    public DbSet<AgentOutboxMessageRow> OutboxMessages => Set<AgentOutboxMessageRow>();
    public DbSet<AgentInboxCommandRow> InboxCommands => Set<AgentInboxCommandRow>();
    public DbSet<AgentCheckpointRow> Checkpoints => Set<AgentCheckpointRow>();
    /// <summary>Gets the durable Agent Fleet heartbeat and assignment-reconciliation state.</summary>
    public DbSet<AgentFleetStateRow> AgentFleetStates => Set<AgentFleetStateRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentRegistrationRow>(entity =>
        {
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.InstallationId).HasMaxLength(160).IsRequired();
            entity.HasIndex(row => row.InstallationId).IsUnique();
            entity.Property(row => row.Environment).HasMaxLength(100).IsRequired();
            entity.Property(row => row.IdentityCertificateReference).HasMaxLength(500).IsRequired();
            entity.Property(row => row.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.Property(row => row.IdentityState).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ActiveConfigurationVersion).HasMaxLength(100);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.ToTable("agent_registration", table =>
            {
                table.HasCheckConstraint(
                    "ck_agent_registration_identity_state",
                    "identity_state IN ('NotEnrolled','Active','Offline','Stale','Incompatible','Expired','RevokedOrDenied','Conflict')");
            });
        });

        modelBuilder.Entity<AgentInstanceAssignmentRow>(entity =>
        {
            entity.ToTable("instance_assignments", table =>
            {
                table.HasCheckConstraint("ck_instance_assignment_interval", "interval_seconds >= 1");
                table.HasCheckConstraint("ck_instance_assignment_timeout", "timeout_seconds >= 1");
                table.HasCheckConstraint("ck_instance_assignment_retries", "retry_count >= 0");
            });
            entity.HasKey(row => row.InstanceId);
            entity.Property(row => row.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(row => row.ProviderType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EndpointJson).IsRequired();
            entity.Property(row => row.MonitoringCredentialReference).HasMaxLength(500);
            entity.Property(row => row.AdministrativeCredentialReference).HasMaxLength(500);
            entity.Property(row => row.TagsJson).IsRequired();
            entity.Property(row => row.PolicyVersion).HasMaxLength(100).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasIndex(row => new { row.ProviderType, row.Enabled });
        });

        modelBuilder.Entity<AgentHealthObservationRow>(entity =>
        {
            entity.ToTable("health_observations", table =>
            {
                table.HasCheckConstraint("ck_agent_health_duration", "duration_milliseconds >= 0");
                table.HasCheckConstraint("ck_agent_health_attempts", "attempt_count >= 1");
                table.HasCheckConstraint("ck_agent_health_status", "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");
            });
            entity.HasKey(row => row.ObservationId);
            entity.Property(row => row.ProviderType).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.Status).HasMaxLength(32).IsRequired();
            entity.Property(row => row.Method).HasMaxLength(64).IsRequired();
            entity.Property(row => row.EvidenceLevel).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ErrorCode).HasMaxLength(100);
            entity.Property(row => row.SafeErrorMessage).HasMaxLength(1000);
            entity.HasIndex(row => new { row.InstanceId, row.ObservedAt });
            entity.HasIndex(row => row.CreatedAt);
        });

        modelBuilder.Entity<AgentOutboxMessageRow>(entity =>
        {
            entity.ToTable("outbox_messages", table =>
            {
                table.HasCheckConstraint("ck_agent_outbox_sequence", "sequence >= 0");
                table.HasCheckConstraint("ck_agent_outbox_schema", "schema_version >= 1");
                table.HasCheckConstraint("ck_agent_outbox_attempts", "attempt_count >= 0");
            });
            entity.HasKey(row => row.MessageId);
            entity.HasIndex(row => row.Sequence).IsUnique();
            entity.HasIndex(row => new { row.AcknowledgedAt, row.AvailableAt });
            entity.Property(row => row.MessageType).HasMaxLength(100).IsRequired();
            entity.Property(row => row.PayloadJson).IsRequired();
        });

        modelBuilder.Entity<AgentInboxCommandRow>(entity =>
        {
            entity.ToTable("inbox_commands", table =>
                table.HasCheckConstraint("ck_agent_command_state", "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')"));
            entity.HasKey(row => row.CommandId);
            entity.Property(row => row.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(row => row.IdempotencyKey).IsUnique();
            entity.HasIndex(row => new { row.State, row.ExpiresAt });
            entity.Property(row => row.ProviderId).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CapabilityId).HasMaxLength(100).IsRequired();
            entity.Property(row => row.TypedParametersJson).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ExpectedAgentVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ExpectedProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
        });

        modelBuilder.Entity<AgentCheckpointRow>(entity =>
        {
            entity.ToTable("checkpoints", table => table.HasCheckConstraint("ck_checkpoint_sequence", "sequence >= 0"));
            entity.HasKey(row => row.StreamName);
            entity.Property(row => row.StreamName).HasMaxLength(100);
        });

        modelBuilder.Entity<AgentFleetStateRow>(entity =>
        {
            entity.ToTable("agent_fleet_state", table =>
            {
                table.HasCheckConstraint("ck_agent_fleet_next_heartbeat", "next_heartbeat_sequence >= 1");
                table.HasCheckConstraint(
                    "ck_agent_fleet_pending_heartbeat",
                    "(pending_heartbeat_message_id IS NULL AND pending_heartbeat_sequence IS NULL AND pending_heartbeat_payload_json IS NULL) OR (pending_heartbeat_message_id IS NOT NULL AND pending_heartbeat_sequence >= 1 AND pending_heartbeat_payload_json IS NOT NULL)");
            });
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.PendingHeartbeatPayloadJson);
            entity.Property(row => row.AssignmentEntityTag).HasMaxLength(66);
            entity.Property(row => row.LastErrorCode).HasMaxLength(100);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
            entity.HasOne<AgentRegistrationRow>()
                .WithOne()
                .HasForeignKey<AgentFleetStateRow>(row => row.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SnakeCaseModelConvention.Apply(modelBuilder);
    }
}
