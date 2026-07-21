// Module purpose: Owns the isolated SQLite schema used only by the execution-ineligible command-transport sandbox.
using DBNotifier.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DBNotifier.Persistence.Agent.Sqlite;

/// <summary>
/// Persists sandbox fencing, pending replay and receipt-only command evidence in a database that is separate
/// from the normal Agent inbox and therefore cannot be consumed by an operational command path.
/// </summary>
/// <param name="options">SQLite options bound to the exact fixture-owned receipt database.</param>
internal sealed class AgentCommandTransportSandboxDbContext(
    DbContextOptions<AgentCommandTransportSandboxDbContext> options) : DbContext(options)
{
    /// <summary>Gets the durable fencing and pending-message state for each synthetic Agent identity.</summary>
    internal DbSet<AgentCommandTransportSandboxStateRow> TransportStates =>
        Set<AgentCommandTransportSandboxStateRow>();

    /// <summary>Gets execution-ineligible receipt records whose immutable policy is always <c>Never</c>.</summary>
    internal DbSet<AgentCommandTransportSandboxReceiptRow> Receipts =>
        Set<AgentCommandTransportSandboxReceiptRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentCommandTransportSandboxStateRow>(entity =>
        {
            entity.ToTable("sandbox_command_transport_state", table =>
            {
                table.HasCheckConstraint("ck_sandbox_command_next_sequence", "next_sequence >= 1");
                table.HasCheckConstraint("ck_sandbox_command_next_fence", "next_fence >= 1");
                table.HasCheckConstraint("ck_sandbox_command_attempts", "pending_attempt_count >= 0");
                table.HasCheckConstraint(
                    "ck_sandbox_command_pending",
                    "(pending_message_id IS NULL AND pending_sequence IS NULL AND pending_message_kind IS NULL AND pending_payload_json IS NULL AND pending_payload_sha256 IS NULL AND pending_attempt_count = 0 AND pending_last_attempt_at IS NULL) OR " +
                    "(pending_message_id IS NOT NULL AND pending_sequence >= 1 AND pending_message_kind IN ('Poll','Acknowledgement') AND pending_payload_json IS NOT NULL AND pending_payload_sha256 IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_sandbox_command_lease",
                    "(lease_owner IS NULL AND lease_fence IS NULL AND lease_expires_at IS NULL) OR " +
                    "(lease_owner IS NOT NULL AND lease_fence >= 1 AND lease_expires_at IS NOT NULL)");
            });
            entity.HasKey(row => row.AgentId);
            entity.Property(row => row.PendingMessageKind).HasMaxLength(32);
            entity.Property(row => row.PendingPayloadJson).HasMaxLength(64 * 1024);
            entity.Property(row => row.PendingPayloadSha256).HasMaxLength(64);
            entity.Property(row => row.LeaseOwner).HasMaxLength(160);
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
        });

        modelBuilder.Entity<AgentCommandTransportSandboxReceiptRow>(entity =>
        {
            entity.ToTable("sandbox_command_receipts", table =>
            {
                table.HasCheckConstraint("ck_sandbox_command_execution_policy", "execution_policy = 'Never'");
                table.HasCheckConstraint(
                    "ck_sandbox_command_receipt_state",
                    "state IN ('ReceiptPending','ReceiptAcknowledged','ReceiptExpired','ReceiptRejected','ReceiptUnsupported')");
            });
            entity.HasKey(row => row.CommandId);
            entity.Property(row => row.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(row => row.IdempotencyKey).IsUnique();
            entity.Property(row => row.ProviderId).HasMaxLength(64).IsRequired();
            entity.Property(row => row.CapabilityId).HasMaxLength(100).IsRequired();
            entity.Property(row => row.TypedParametersJson).IsRequired();
            entity.Property(row => row.State).HasMaxLength(32).IsRequired();
            entity.Property(row => row.ExpectedAgentVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ExpectedProviderVersion).HasMaxLength(64).IsRequired();
            entity.Property(row => row.ExecutionPolicy).HasMaxLength(16).IsRequired();
            entity.Property(row => row.ConcurrencyToken).IsConcurrencyToken();
        });

        SnakeCaseModelConvention.Apply(modelBuilder);
    }
}

/// <summary>Persists one sandbox-only pending message and monotonic lease without an operational Agent foreign key.</summary>
internal sealed class AgentCommandTransportSandboxStateRow
{
    /// <summary>Gets or sets the synthetic Agent identity that owns this isolated stream.</summary>
    public Guid AgentId { get; set; }
    /// <summary>Gets or sets the next sequence reserved after committing a pending message.</summary>
    public long NextSequence { get; set; }
    /// <summary>Gets or sets the sole pending message identifier.</summary>
    public Guid? PendingMessageId { get; set; }
    /// <summary>Gets or sets the sole pending message sequence.</summary>
    public long? PendingSequence { get; set; }
    /// <summary>Gets or sets the closed pending message kind.</summary>
    public string? PendingMessageKind { get; set; }
    /// <summary>Gets or sets the exact bounded pending payload.</summary>
    public string? PendingPayloadJson { get; set; }
    /// <summary>Gets or sets the SHA-256 digest of the exact pending payload.</summary>
    public string? PendingPayloadSha256 { get; set; }
    /// <summary>Gets or sets the bounded durable attempt count.</summary>
    public int PendingAttemptCount { get; set; }
    /// <summary>Gets or sets the last UTC attempt time.</summary>
    public DateTimeOffset? PendingLastAttemptAt { get; set; }
    /// <summary>Gets or sets the next monotonic lease fence.</summary>
    public long NextFence { get; set; }
    /// <summary>Gets or sets the bounded current lease owner.</summary>
    public string? LeaseOwner { get; set; }
    /// <summary>Gets or sets the current lease fence.</summary>
    public long? LeaseFence { get; set; }
    /// <summary>Gets or sets the exclusive UTC lease expiry.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}

/// <summary>Persists receipt-only synthetic content with an immutable execution policy and no result fields.</summary>
internal sealed class AgentCommandTransportSandboxReceiptRow
{
    /// <summary>Gets or sets the synthetic command identifier.</summary>
    public Guid CommandId { get; set; }
    /// <summary>Gets or sets the bounded synthetic idempotency key.</summary>
    public required string IdempotencyKey { get; set; }
    /// <summary>Gets or sets the synthetic target instance identifier.</summary>
    public Guid InstanceId { get; set; }
    /// <summary>Gets or sets the synthetic provider identifier.</summary>
    public required string ProviderId { get; set; }
    /// <summary>Gets or sets the sandbox-only capability identifier.</summary>
    public required string CapabilityId { get; set; }
    /// <summary>Gets or sets the bounded typed fixture payload.</summary>
    public required string TypedParametersJson { get; set; }
    /// <summary>Gets or sets the receipt-only state, which has no execution states.</summary>
    public required string State { get; set; }
    /// <summary>Gets or sets the synthetic UTC request time.</summary>
    public DateTimeOffset RequestedAt { get; set; }
    /// <summary>Gets or sets the synthetic UTC expiry.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Gets or sets the exact expected Agent version.</summary>
    public required string ExpectedAgentVersion { get; set; }
    /// <summary>Gets or sets the exact expected provider fixture version.</summary>
    public required string ExpectedProviderVersion { get; set; }
    /// <summary>Gets or sets the immutable <c>Never</c> execution policy enforced by SQLite.</summary>
    public required string ExecutionPolicy { get; set; }
    /// <summary>Gets or sets the UTC time at which receipt acknowledgement was committed.</summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }
    /// <summary>Gets or sets the optimistic concurrency token.</summary>
    public Guid ConcurrencyToken { get; set; }
}
