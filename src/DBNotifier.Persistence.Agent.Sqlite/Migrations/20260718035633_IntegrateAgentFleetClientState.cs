// Module purpose: Adds Agent Fleet identity, heartbeat replay and assignment-reconciliation state to local SQLite.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class IntegrateAgentFleetClientState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "tags_json",
            table: "instance_assignments",
            type: "TEXT",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "certificate_not_after",
            table: "agent_registration",
            type: "TEXT",
            nullable: false,
            defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

        migrationBuilder.AddColumn<string>(
            name: "certificate_thumbprint",
            table: "agent_registration",
            type: "TEXT",
            maxLength: 160,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "identity_state",
            table: "agent_registration",
            type: "TEXT",
            maxLength: 32,
            nullable: false,
            defaultValue: "Conflict");

        migrationBuilder.CreateTable(
            name: "agent_fleet_state",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "TEXT", nullable: false),
                next_heartbeat_sequence = table.Column<long>(type: "INTEGER", nullable: false),
                pending_heartbeat_message_id = table.Column<Guid>(type: "TEXT", nullable: true),
                pending_heartbeat_sequence = table.Column<long>(type: "INTEGER", nullable: true),
                pending_heartbeat_payload_json = table.Column<string>(type: "TEXT", nullable: true),
                last_heartbeat_accepted_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                assignment_entity_tag = table.Column<string>(type: "TEXT", maxLength: 66, nullable: true),
                assignment_generated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                assignment_last_succeeded_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                last_attempt_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                last_error_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                concurrency_token = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_fleet_state", x => x.agent_id);
                table.CheckConstraint("ck_agent_fleet_next_heartbeat", "next_heartbeat_sequence >= 1");
                table.CheckConstraint("ck_agent_fleet_pending_heartbeat", "(pending_heartbeat_message_id IS NULL AND pending_heartbeat_sequence IS NULL AND pending_heartbeat_payload_json IS NULL) OR (pending_heartbeat_message_id IS NOT NULL AND pending_heartbeat_sequence >= 1 AND pending_heartbeat_payload_json IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_agent_fleet_state_agent_registration_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agent_registration",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_registration_identity_state",
            table: "agent_registration",
            sql: "identity_state IN ('NotEnrolled','Active','Offline','Stale','Incompatible','Expired','RevokedOrDenied','Conflict')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TEMP TABLE dbn_agent_fleet_down_guard
            (
                safe INTEGER NOT NULL CHECK (safe = 1)
            );
            INSERT INTO dbn_agent_fleet_down_guard (safe)
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                    FROM agent_registration
            ) THEN 0 ELSE 1 END;
            DROP TABLE dbn_agent_fleet_down_guard;
            """);

        migrationBuilder.DropTable(
            name: "agent_fleet_state");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_registration_identity_state",
            table: "agent_registration");

        migrationBuilder.DropColumn(
            name: "tags_json",
            table: "instance_assignments");

        migrationBuilder.DropColumn(
            name: "certificate_not_after",
            table: "agent_registration");

        migrationBuilder.DropColumn(
            name: "certificate_thumbprint",
            table: "agent_registration");

        migrationBuilder.DropColumn(
            name: "identity_state",
            table: "agent_registration");
    }
}
