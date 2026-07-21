// Module purpose: Adds explicit alert routing provenance and conservatively quarantines unproven pending delivery work.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddExplicitAlertRouting : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_notification_deliveries_notification_channel_id_event_id",
            table: "notification_deliveries");

        migrationBuilder.DropCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries");

        migrationBuilder.AddColumn<Guid>(
            name: "alert_rule_channel_binding_id",
            table: "notification_deliveries",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "idempotency_key",
            table: "notification_deliveries",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "alert_rule_channel_bindings",
            columns: table => new
            {
                alert_rule_channel_binding_id = table.Column<Guid>(type: "uuid", nullable: false),
                alert_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                notification_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_alert_rule_channel_bindings", x => x.alert_rule_channel_binding_id);
                table.UniqueConstraint("AK_alert_rule_channel_bindings_alert_rule_channel_binding_id_n~", x => new { x.alert_rule_channel_binding_id, x.notification_channel_id });
                table.ForeignKey(
                    name: "FK_alert_rule_channel_bindings_alert_rules_alert_rule_id",
                    column: x => x.alert_rule_id,
                    principalTable: "alert_rules",
                    principalColumn: "alert_rule_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_alert_rule_channel_bindings_notification_channels_notificat~",
                    column: x => x.notification_channel_id,
                    principalTable: "notification_channels",
                    principalColumn: "notification_channel_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_alert_rule_channel_binding_id_event~",
            table: "notification_deliveries",
            columns: new[] { "alert_rule_channel_binding_id", "event_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_alert_rule_channel_binding_id_notif~",
            table: "notification_deliveries",
            columns: new[] { "alert_rule_channel_binding_id", "notification_channel_id" });

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_idempotency_key",
            table: "notification_deliveries",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_notification_channel_id",
            table: "notification_deliveries",
            column: "notification_channel_id");

        // Historical pending rows pre-date durable routing provenance. Retain them conservatively but make
        // them unsendable before the new pending-provenance constraint is installed.
        migrationBuilder.Sql(
            """
            UPDATE notification_deliveries
            SET state = 'Quarantined',
                error_code = 'notification.binding_unproven'
            WHERE state = 'Pending'
              AND alert_rule_channel_binding_id IS NULL;
            """);

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_pending_provenance",
            table: "notification_deliveries",
            sql: "state <> 'Pending' OR (alert_rule_channel_binding_id IS NOT NULL AND idempotency_key IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_quarantine_reason",
            table: "notification_deliveries",
            sql: "state <> 'Quarantined' OR error_code IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries",
            sql: "state IN ('Pending','Delivering','Delivered','Failed','Cancelled','Quarantined')");

        migrationBuilder.CreateIndex(
            name: "IX_alert_rule_channel_bindings_alert_rule_id_notification_chan~",
            table: "alert_rule_channel_bindings",
            columns: new[] { "alert_rule_id", "notification_channel_id", "environment" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_alert_rule_channel_bindings_environment_enabled",
            table: "alert_rule_channel_bindings",
            columns: new[] { "environment", "enabled" });

        migrationBuilder.CreateIndex(
            name: "IX_alert_rule_channel_bindings_notification_channel_id",
            table: "alert_rule_channel_bindings",
            column: "notification_channel_id");

        migrationBuilder.AddForeignKey(
            name: "FK_notification_deliveries_alert_rule_channel_bindings_alert_r~",
            table: "notification_deliveries",
            columns: new[] { "alert_rule_channel_binding_id", "notification_channel_id" },
            principalTable: "alert_rule_channel_bindings",
            principalColumns: new[] { "alert_rule_channel_binding_id", "notification_channel_id" },
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Proven bindings and quarantined work cannot be represented safely by the preceding schema. Refuse
        // downgrade instead of deleting provenance, inferring a channel or mutating ambiguous delivery state.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                RAISE EXCEPTION 'notification.explicit_binding_downgrade_blocked';
            END
            $$;
            """);
    }
}
