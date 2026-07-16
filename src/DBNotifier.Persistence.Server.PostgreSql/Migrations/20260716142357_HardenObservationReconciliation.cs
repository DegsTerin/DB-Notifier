// Module purpose: Adds fail-closed durable observation reconciliation and reversible PostgreSQL backfill guards.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class HardenObservationReconciliation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "attempt_count",
            table: "health_samples",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "payload_hash",
            table: "health_samples",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "agent_observation_cursors",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                highest_contiguous_sequence = table.Column<long>(type: "bigint", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_observation_cursors", x => x.agent_id);
                table.CheckConstraint("ck_agent_observation_cursor_sequence", "highest_contiguous_sequence >= 0");
                table.ForeignKey(
                    name: "FK_agent_observation_cursors_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "instance_observation_states",
            columns: table => new
            {
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                last_processed_sequence = table.Column<long>(type: "bigint", nullable: false),
                observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_instance_observation_states", x => x.instance_id);
                table.CheckConstraint("ck_instance_observation_state_sequence", "last_processed_sequence >= 1");
                table.CheckConstraint("ck_instance_observation_state_status", "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");
                table.ForeignKey(
                    name: "FK_instance_observation_states_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_instance_observation_states_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM health_samples
                    GROUP BY agent_id
                    HAVING MIN(sequence) <> 1 OR COUNT(*) <> MAX(sequence)
                ) THEN
                    RAISE EXCEPTION 'observation history is not contiguous per Agent';
                END IF;
            END
            $$;

            INSERT INTO agent_observation_cursors (
                agent_id,
                highest_contiguous_sequence,
                updated_at,
                concurrency_token)
            SELECT
                agent_id,
                MAX(sequence),
                MAX(received_at),
                agent_id
            FROM health_samples
            GROUP BY agent_id;

            INSERT INTO instance_observation_states (
                instance_id,
                agent_id,
                last_processed_sequence,
                observation_id,
                status,
                observed_at,
                received_at,
                concurrency_token)
            SELECT DISTINCT ON (sample.instance_id)
                sample.instance_id,
                sample.agent_id,
                sample.sequence,
                sample.observation_id,
                sample.status,
                sample.observed_at,
                sample.received_at,
                sample.observation_id
            FROM health_samples AS sample
            INNER JOIN database_instances AS instance_row
                ON instance_row.instance_id = sample.instance_id
                AND instance_row.assigned_agent_id = sample.agent_id
            ORDER BY
                sample.instance_id,
                sample.sequence DESC,
                sample.received_at DESC,
                sample.observation_id DESC;
            """);

        migrationBuilder.AddCheckConstraint(
            name: "ck_health_attempts",
            table: "health_samples",
            sql: "attempt_count >= 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_health_evidence",
            table: "health_samples",
            sql: "evidence_level IN ('ProviderAuthenticated','ProviderReadiness','TransportOnly','Synthetic','Unknown')");

        migrationBuilder.CreateIndex(
            name: "IX_instance_observation_states_agent_id_last_processed_sequence",
            table: "instance_observation_states",
            columns: new[] { "agent_id", "last_processed_sequence" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "agent_observation_cursors");

        migrationBuilder.DropTable(
            name: "instance_observation_states");

        migrationBuilder.DropCheckConstraint(
            name: "ck_health_attempts",
            table: "health_samples");

        migrationBuilder.DropCheckConstraint(
            name: "ck_health_evidence",
            table: "health_samples");

        migrationBuilder.DropColumn(
            name: "attempt_count",
            table: "health_samples");

        migrationBuilder.DropColumn(
            name: "payload_hash",
            table: "health_samples");
    }
}
