using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddRejectedObservationSequenceLedger : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "rejection_ledger_start_sequence",
            table: "agent_observation_cursors",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM agent_observation_cursors
                    WHERE highest_contiguous_sequence = 9223372036854775807
                ) THEN
                    RAISE EXCEPTION 'ingestion.rejection_ledger_cutover_overflow';
                END IF;
            END $$;

            UPDATE agent_observation_cursors
            SET rejection_ledger_start_sequence = highest_contiguous_sequence + 1;
            """);

        migrationBuilder.AlterColumn<long>(
            name: "rejection_ledger_start_sequence",
            table: "agent_observation_cursors",
            type: "bigint",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldDefaultValue: 0L);

        migrationBuilder.CreateTable(
            name: "rejected_observation_sequences",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rejected_observation_sequences", x => new { x.agent_id, x.sequence });
                table.CheckConstraint("ck_rejected_observation_error_code", "length(error_code) BETWEEN 1 AND 100");
                table.CheckConstraint("ck_rejected_observation_sequence", "sequence >= 1");
                table.ForeignKey(
                    name: "FK_rejected_observation_sequences_agent_observation_cursors_ag~",
                    column: x => x.agent_id,
                    principalTable: "agent_observation_cursors",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_observation_rejection_ledger_start",
            table: "agent_observation_cursors",
            sql: "rejection_ledger_start_sequence >= 1 AND rejection_ledger_start_sequence <= highest_contiguous_sequence + 1");

        migrationBuilder.CreateIndex(
            name: "IX_rejected_observation_sequences_agent_id_message_id",
            table: "rejected_observation_sequences",
            columns: new[] { "agent_id", "message_id" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM rejected_observation_sequences) THEN
                    RAISE EXCEPTION 'ingestion.rejection_ledger_downgrade_blocked';
                END IF;
            END $$;
            """);

        migrationBuilder.DropTable(
            name: "rejected_observation_sequences");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_observation_rejection_ledger_start",
            table: "agent_observation_cursors");

        migrationBuilder.DropColumn(
            name: "rejection_ledger_start_sequence",
            table: "agent_observation_cursors");
    }
}
