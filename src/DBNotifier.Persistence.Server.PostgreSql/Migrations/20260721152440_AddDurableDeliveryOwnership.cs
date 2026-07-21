// Module purpose: Adds durable PostgreSQL delivery ownership while preserving all existing queue evidence.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddDurableDeliveryOwnership : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ambiguous_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "dead_lettered_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "error_code",
            table: "outbox_messages",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "handoff_started_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "lease_expires_at",
            table: "outbox_messages",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "lease_fence",
            table: "outbox_messages",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<Guid>(
            name: "lease_owner_id",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ambiguous_at",
            table: "notification_deliveries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "available_at",
            table: "notification_deliveries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "dead_lettered_at",
            table: "notification_deliveries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "handoff_started_at",
            table: "notification_deliveries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "lease_expires_at",
            table: "notification_deliveries",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "lease_fence",
            table: "notification_deliveries",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<Guid>(
            name: "lease_owner_id",
            table: "notification_deliveries",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_published_at_dead_lettered_at_ambiguous_at_~",
            table: "outbox_messages",
            columns: new[] { "published_at", "dead_lettered_at", "ambiguous_at", "available_at", "lease_expires_at" });

        migrationBuilder.AddCheckConstraint(
            name: "ck_server_outbox_fence",
            table: "outbox_messages",
            sql: "lease_fence >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_server_outbox_lease_tuple",
            table: "outbox_messages",
            sql: "(lease_owner_id IS NULL AND lease_expires_at IS NULL AND handoff_started_at IS NULL) OR (lease_owner_id IS NOT NULL AND lease_expires_at IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_server_outbox_terminal_evidence",
            table: "outbox_messages",
            sql: "NOT ((published_at IS NOT NULL AND dead_lettered_at IS NOT NULL) OR (published_at IS NOT NULL AND ambiguous_at IS NOT NULL) OR (dead_lettered_at IS NOT NULL AND ambiguous_at IS NOT NULL))");

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_state_available_at_lease_expires_at~",
            table: "notification_deliveries",
            columns: new[] { "state", "available_at", "lease_expires_at", "created_at" });

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_fence",
            table: "notification_deliveries",
            sql: "lease_fence >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_lease_tuple",
            table: "notification_deliveries",
            sql: "(lease_owner_id IS NULL AND lease_expires_at IS NULL AND handoff_started_at IS NULL) OR (lease_owner_id IS NOT NULL AND lease_expires_at IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries",
            sql: "state IN ('Pending','Delivering','Delivered','Failed','Cancelled','Quarantined','DeadLettered','Ambiguous')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_terminal_evidence",
            table: "notification_deliveries",
            sql: "(state = 'DeadLettered' AND dead_lettered_at IS NOT NULL AND ambiguous_at IS NULL) OR (state = 'Ambiguous' AND ambiguous_at IS NOT NULL AND dead_lettered_at IS NULL) OR (state NOT IN ('DeadLettered','Ambiguous') AND dead_lettered_at IS NULL AND ambiguous_at IS NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Ownership, fences and ambiguous outcomes cannot be represented by the preceding schema. Refuse
        // downgrade instead of deleting evidence or making an uncertain hand-off retryable.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                RAISE EXCEPTION 'delivery.durable_ownership_downgrade_blocked';
            END
            $$;
            """);
    }
}
