using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class InitialAgentSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agent_registration",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "TEXT", nullable: false),
                installation_id = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                environment = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                identity_certificate_reference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                active_configuration_version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                concurrency_token = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_registration", x => x.agent_id);
            });

        migrationBuilder.CreateTable(
            name: "checkpoints",
            columns: table => new
            {
                stream_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                sequence = table.Column<long>(type: "INTEGER", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_checkpoints", x => x.stream_name);
                table.CheckConstraint("ck_checkpoint_sequence", "sequence >= 0");
            });

        migrationBuilder.CreateTable(
            name: "health_observations",
            columns: table => new
            {
                observation_id = table.Column<Guid>(type: "TEXT", nullable: false),
                instance_id = table.Column<Guid>(type: "TEXT", nullable: false),
                provider_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                provider_version = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                method = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                evidence_level = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                observed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                duration_milliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                error_code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                safe_error_message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                redacted_details_json = table.Column<string>(type: "TEXT", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_health_observations", x => x.observation_id);
                table.CheckConstraint("ck_agent_health_attempts", "attempt_count >= 1");
                table.CheckConstraint("ck_agent_health_duration", "duration_milliseconds >= 0");
            });

        migrationBuilder.CreateTable(
            name: "inbox_commands",
            columns: table => new
            {
                command_id = table.Column<Guid>(type: "TEXT", nullable: false),
                idempotency_key = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                instance_id = table.Column<Guid>(type: "TEXT", nullable: false),
                capability_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                typed_parameters_json = table.Column<string>(type: "TEXT", nullable: false),
                state = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                requested_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                acknowledged_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                completed_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                result_json = table.Column<string>(type: "TEXT", nullable: true),
                concurrency_token = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_inbox_commands", x => x.command_id);
            });

        migrationBuilder.CreateTable(
            name: "instance_assignments",
            columns: table => new
            {
                instance_id = table.Column<Guid>(type: "TEXT", nullable: false),
                display_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                provider_type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                endpoint_json = table.Column<string>(type: "TEXT", nullable: false),
                monitoring_credential_reference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                administrative_credential_reference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                interval_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                timeout_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                retry_count = table.Column<int>(type: "INTEGER", nullable: false),
                enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                policy_version = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                concurrency_token = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_instance_assignments", x => x.instance_id);
                table.CheckConstraint("ck_instance_assignment_interval", "interval_seconds >= 1");
                table.CheckConstraint("ck_instance_assignment_retries", "retry_count >= 0");
                table.CheckConstraint("ck_instance_assignment_timeout", "timeout_seconds >= 1");
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                message_id = table.Column<Guid>(type: "TEXT", nullable: false),
                sequence = table.Column<long>(type: "INTEGER", nullable: false),
                message_type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                schema_version = table.Column<int>(type: "INTEGER", nullable: false),
                payload_json = table.Column<string>(type: "TEXT", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                available_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                attempt_count = table.Column<int>(type: "INTEGER", nullable: false),
                acknowledged_at = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.message_id);
                table.CheckConstraint("ck_agent_outbox_attempts", "attempt_count >= 0");
                table.CheckConstraint("ck_agent_outbox_schema", "schema_version >= 1");
                table.CheckConstraint("ck_agent_outbox_sequence", "sequence >= 0");
            });

        migrationBuilder.CreateIndex(
            name: "IX_agent_registration_installation_id",
            table: "agent_registration",
            column: "installation_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_health_observations_created_at",
            table: "health_observations",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "IX_health_observations_instance_id_observed_at",
            table: "health_observations",
            columns: new[] { "instance_id", "observed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_inbox_commands_idempotency_key",
            table: "inbox_commands",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_inbox_commands_state_expires_at",
            table: "inbox_commands",
            columns: new[] { "state", "expires_at" });

        migrationBuilder.CreateIndex(
            name: "IX_instance_assignments_provider_type_enabled",
            table: "instance_assignments",
            columns: new[] { "provider_type", "enabled" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_acknowledged_at_available_at",
            table: "outbox_messages",
            columns: new[] { "acknowledged_at", "available_at" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_sequence",
            table: "outbox_messages",
            column: "sequence",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "agent_registration");

        migrationBuilder.DropTable(
            name: "checkpoints");

        migrationBuilder.DropTable(
            name: "health_observations");

        migrationBuilder.DropTable(
            name: "inbox_commands");

        migrationBuilder.DropTable(
            name: "instance_assignments");

        migrationBuilder.DropTable(
            name: "outbox_messages");
    }
}
