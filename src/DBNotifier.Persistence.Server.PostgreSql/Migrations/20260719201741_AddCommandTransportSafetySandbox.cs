// Module purpose: Adds the Server command-transport cursor and replay journal plus the sandbox-only Unsupported terminal state.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddCommandTransportSafetySandbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands");

        migrationBuilder.CreateTable(
            name: "agent_command_transport_cursors",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                highest_accepted_sequence = table.Column<long>(type: "bigint", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_command_transport_cursors", x => x.agent_id);
                table.CheckConstraint("ck_agent_command_transport_cursor_sequence", "highest_accepted_sequence >= 0");
                table.ForeignKey(
                    name: "FK_agent_command_transport_cursors_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "command_transport_journal",
            columns: table => new
            {
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                message_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                request_payload_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                response_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                response_payload_json = table.Column<string>(type: "jsonb", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_command_transport_journal", x => x.message_id);
                table.CheckConstraint("ck_command_transport_journal_sequence", "sequence >= 1");
                table.ForeignKey(
                    name: "FK_command_transport_journal_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','Unsupported','UnknownOutcome')");

        migrationBuilder.CreateIndex(
            name: "IX_command_transport_journal_agent_id_sequence",
            table: "command_transport_journal",
            columns: new[] { "agent_id", "sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_command_transport_journal_response_message_id",
            table: "command_transport_journal",
            column: "response_message_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "agent_command_transport_cursors");

        migrationBuilder.DropTable(
            name: "command_transport_journal");

        migrationBuilder.DropCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands");

        migrationBuilder.AddCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");
    }
}
