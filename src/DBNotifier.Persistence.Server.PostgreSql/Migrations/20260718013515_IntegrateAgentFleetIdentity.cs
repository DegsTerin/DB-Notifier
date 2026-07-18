using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class IntegrateAgentFleetIdentity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "gap_detected",
            table: "agent_heartbeats",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "payload_sha256",
            table: "agent_heartbeats",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "previous_accepted_sequence",
            table: "agent_heartbeats",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "agent_certificates",
            columns: table => new
            {
                agent_certificate_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                thumbprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                public_key_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                certificate_signing_request_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                not_before = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                not_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revocation_reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_certificates", x => x.agent_certificate_id);
                table.CheckConstraint("ck_agent_certificate_revocation", "(state = 'Revoked' AND revoked_at IS NOT NULL AND revocation_reason_code IS NOT NULL) OR (state IN ('Active','Superseded') AND revoked_at IS NULL AND revocation_reason_code IS NULL)");
                table.CheckConstraint("ck_agent_certificate_state", "state IN ('Active','Revoked','Superseded')");
                table.CheckConstraint("ck_agent_certificate_validity", "not_before IS NULL OR not_after IS NULL OR not_after > not_before");
                table.ForeignKey(
                    name: "FK_agent_certificates_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "agent_enrollment_tokens",
            columns: table => new
            {
                enrollment_token_id = table.Column<Guid>(type: "uuid", nullable: false),
                salt = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
                secret_hash = table.Column<byte[]>(type: "bytea", maxLength: 64, nullable: false),
                hash_algorithm = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                expected_installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                expected_environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                expected_platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                scope = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                consumed_by_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_enrollment_tokens", x => x.enrollment_token_id);
                table.CheckConstraint("ck_agent_enrollment_token_consumption", "(state = 'Consumed' AND consumed_at IS NOT NULL AND consumed_by_agent_id IS NOT NULL AND revoked_at IS NULL) OR (state = 'Revoked' AND consumed_at IS NULL AND consumed_by_agent_id IS NULL AND revoked_at IS NOT NULL) OR (state = 'Active' AND consumed_at IS NULL AND consumed_by_agent_id IS NULL AND revoked_at IS NULL)");
                table.CheckConstraint("ck_agent_enrollment_token_expiry", "expires_at > issued_at");
                table.CheckConstraint("ck_agent_enrollment_token_proof", "hash_algorithm = 'SHA256' AND length(salt) = 16 AND length(secret_hash) = 32");
                table.CheckConstraint("ck_agent_enrollment_token_state", "state IN ('Active','Consumed','Revoked')");
                table.ForeignKey(
                    name: "FK_agent_enrollment_tokens_agents_consumed_by_agent_id",
                    column: x => x.consumed_by_agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "agent_heartbeat_cursors",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                highest_accepted_sequence = table.Column<long>(type: "bigint", nullable: false),
                last_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_heartbeat_cursors", x => x.agent_id);
                table.CheckConstraint("ck_agent_heartbeat_cursor_sequence", "highest_accepted_sequence >= 0");
                table.ForeignKey(
                    name: "FK_agent_heartbeat_cursors_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM agent_heartbeats
                    GROUP BY agent_id, sequence
                    HAVING COUNT(*) > 1)
                THEN
                    RAISE EXCEPTION 'Agent heartbeat history contains duplicate per-Agent sequences; Agent Fleet migration stopped fail-closed.';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM agents
                    GROUP BY UPPER(REPLACE(REPLACE(certificate_thumbprint, ' ', ''), ':', ''))
                    HAVING COUNT(*) > 1)
                THEN
                    RAISE EXCEPTION 'Agent certificate history contains duplicate canonical thumbprints; Agent Fleet migration stopped fail-closed.';
                END IF;
            END $$;
            """);

        migrationBuilder.Sql(
            """
            INSERT INTO agent_certificates (
                agent_certificate_id,
                agent_id,
                thumbprint,
                public_key_sha256,
                certificate_signing_request_sha256,
                state,
                issued_at,
                not_before,
                not_after,
                revoked_at,
                revocation_reason_code,
                concurrency_token)
            SELECT
                agent_id,
                agent_id,
                UPPER(REPLACE(REPLACE(certificate_thumbprint, ' ', ''), ':', '')),
                NULL,
                NULL,
                CASE
                    WHEN state = 'Active' AND revoked_at IS NULL THEN 'Active'
                    WHEN state = 'Revoked' AND revoked_at IS NOT NULL THEN 'Revoked'
                    ELSE 'Superseded'
                END,
                enrolled_at,
                NULL,
                NULL,
                CASE
                    WHEN state = 'Revoked' AND revoked_at IS NOT NULL THEN revoked_at
                    ELSE NULL
                END,
                CASE
                    WHEN state = 'Revoked' AND revoked_at IS NOT NULL THEN 'legacy.agent_revoked'
                    ELSE NULL
                END,
                concurrency_token
            FROM agents;
            """);

        migrationBuilder.Sql(
            """
            WITH ordered AS (
                SELECT
                    heartbeat_id,
                    sequence,
                    LAG(sequence, 1, 0) OVER (
                        PARTITION BY agent_id
                        ORDER BY sequence, received_at, heartbeat_id) AS previous_sequence
                FROM agent_heartbeats)
            UPDATE agent_heartbeats AS heartbeat
            SET
                previous_accepted_sequence = ordered.previous_sequence,
                gap_detected = ordered.sequence > ordered.previous_sequence + 1
            FROM ordered
            WHERE heartbeat.heartbeat_id = ordered.heartbeat_id;

            INSERT INTO agent_heartbeat_cursors (
                agent_id,
                highest_accepted_sequence,
                last_message_id,
                updated_at,
                concurrency_token)
            SELECT DISTINCT ON (heartbeat.agent_id)
                heartbeat.agent_id,
                heartbeat.sequence,
                heartbeat.message_id,
                heartbeat.received_at,
                agent.concurrency_token
            FROM agent_heartbeats AS heartbeat
            INNER JOIN agents AS agent ON agent.agent_id = heartbeat.agent_id
            ORDER BY
                heartbeat.agent_id,
                heartbeat.sequence DESC,
                heartbeat.received_at DESC,
                heartbeat.heartbeat_id DESC;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_agent_heartbeats_agent_id_sequence",
            table: "agent_heartbeats",
            columns: new[] { "agent_id", "sequence" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agent_certificates_agent_id_state",
            table: "agent_certificates",
            columns: new[] { "agent_id", "state" });

        migrationBuilder.CreateIndex(
            name: "IX_agent_certificates_thumbprint",
            table: "agent_certificates",
            column: "thumbprint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agent_enrollment_tokens_consumed_by_agent_id",
            table: "agent_enrollment_tokens",
            column: "consumed_by_agent_id");

        migrationBuilder.CreateIndex(
            name: "IX_agent_enrollment_tokens_expected_installation_id",
            table: "agent_enrollment_tokens",
            column: "expected_installation_id");

        migrationBuilder.CreateIndex(
            name: "IX_agent_enrollment_tokens_state_expires_at",
            table: "agent_enrollment_tokens",
            columns: new[] { "state", "expires_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM agents AS agent
                    INNER JOIN agent_certificates AS certificate
                        ON certificate.agent_id = agent.agent_id
                    LEFT JOIN agent_enrollment_tokens AS token
                        ON token.consumed_by_agent_id = agent.agent_id
                    WHERE
                        agent.state = 'Active'
                        AND agent.revoked_at IS NULL
                        AND (
                            certificate.public_key_sha256 IS NOT NULL
                            OR certificate.certificate_signing_request_sha256 IS NOT NULL
                            OR token.state = 'Consumed'))
                THEN
                    RAISE EXCEPTION 'Active Agents enrolled by Agent Fleet would remain trusted after downgrade; revoke them before rollback.';
                END IF;
            END $$;
            """);

        migrationBuilder.DropTable(
            name: "agent_certificates");

        migrationBuilder.DropTable(
            name: "agent_enrollment_tokens");

        migrationBuilder.DropTable(
            name: "agent_heartbeat_cursors");

        migrationBuilder.DropIndex(
            name: "IX_agent_heartbeats_agent_id_sequence",
            table: "agent_heartbeats");

        migrationBuilder.DropColumn(
            name: "gap_detected",
            table: "agent_heartbeats");

        migrationBuilder.DropColumn(
            name: "payload_sha256",
            table: "agent_heartbeats");

        migrationBuilder.DropColumn(
            name: "previous_accepted_sequence",
            table: "agent_heartbeats");
    }
}
