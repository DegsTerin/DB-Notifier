// Module purpose: Adds the Agent-local durable command-transport state and safe terminal inbox disposition for the isolated sandbox.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class AddCommandTransportSafetySandbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands");

        migrationBuilder.CreateTable(
            name: "command_transport_state",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "TEXT", nullable: false),
                next_sequence = table.Column<long>(type: "INTEGER", nullable: false),
                pending_message_id = table.Column<Guid>(type: "TEXT", nullable: true),
                pending_sequence = table.Column<long>(type: "INTEGER", nullable: true),
                pending_message_kind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                pending_payload_json = table.Column<string>(type: "TEXT", maxLength: 65536, nullable: true),
                pending_payload_sha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                pending_attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                pending_last_attempt_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                next_fence = table.Column<long>(type: "INTEGER", nullable: false),
                lease_owner = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                lease_fence = table.Column<long>(type: "INTEGER", nullable: true),
                lease_expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                concurrency_token = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_command_transport_state", x => x.agent_id);
                table.CheckConstraint("ck_command_transport_attempts", "pending_attempt_count >= 0");
                table.CheckConstraint("ck_command_transport_lease", "(lease_owner IS NULL AND lease_fence IS NULL AND lease_expires_at IS NULL) OR (lease_owner IS NOT NULL AND lease_fence >= 1 AND lease_expires_at IS NOT NULL)");
                table.CheckConstraint("ck_command_transport_next_fence", "next_fence >= 1");
                table.CheckConstraint("ck_command_transport_next_sequence", "next_sequence >= 1");
                table.CheckConstraint("ck_command_transport_pending", "(pending_message_id IS NULL AND pending_sequence IS NULL AND pending_message_kind IS NULL AND pending_payload_json IS NULL AND pending_payload_sha256 IS NULL AND pending_attempt_count = 0 AND pending_last_attempt_at IS NULL) OR (pending_message_id IS NOT NULL AND pending_sequence >= 1 AND pending_message_kind IN ('Poll','Acknowledgement') AND pending_payload_json IS NOT NULL AND pending_payload_sha256 IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_command_transport_state_agent_registration_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agent_registration",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','Unsupported','UnknownOutcome')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "command_transport_state");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands");

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");
    }
}
