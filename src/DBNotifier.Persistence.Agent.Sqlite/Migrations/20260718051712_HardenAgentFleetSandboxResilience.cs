using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class HardenAgentFleetSandboxResilience : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "next_operation_fence",
            table: "agent_fleet_state",
            type: "INTEGER",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "operation_lease_expires_at",
            table: "agent_fleet_state",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "operation_lease_fence",
            table: "agent_fleet_state",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "operation_lease_kind",
            table: "agent_fleet_state",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "operation_lease_owner",
            table: "agent_fleet_state",
            type: "TEXT",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_fleet_next_operation_fence",
            table: "agent_fleet_state",
            sql: "next_operation_fence >= 1");

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_fleet_operation_lease",
            table: "agent_fleet_state",
            sql: "(operation_lease_owner IS NULL AND operation_lease_kind IS NULL AND operation_lease_fence IS NULL AND operation_lease_expires_at IS NULL) OR (operation_lease_owner IS NOT NULL AND operation_lease_kind IN ('Heartbeat','AssignmentReconciliation') AND operation_lease_fence >= 1 AND operation_lease_expires_at IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TEMP TABLE dbn_agent_fleet_resilience_down_guard
            (
                safe INTEGER NOT NULL CHECK (safe = 1)
            );
            INSERT INTO dbn_agent_fleet_resilience_down_guard (safe)
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM agent_fleet_state
                WHERE operation_lease_owner IS NOT NULL
                   OR operation_lease_kind IS NOT NULL
                   OR operation_lease_fence IS NOT NULL
                   OR operation_lease_expires_at IS NOT NULL
            ) THEN 0 ELSE 1 END;
            DROP TABLE dbn_agent_fleet_resilience_down_guard;
            """);

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_fleet_next_operation_fence",
            table: "agent_fleet_state");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_fleet_operation_lease",
            table: "agent_fleet_state");

        migrationBuilder.DropColumn(
            name: "next_operation_fence",
            table: "agent_fleet_state");

        migrationBuilder.DropColumn(
            name: "operation_lease_expires_at",
            table: "agent_fleet_state");

        migrationBuilder.DropColumn(
            name: "operation_lease_fence",
            table: "agent_fleet_state");

        migrationBuilder.DropColumn(
            name: "operation_lease_kind",
            table: "agent_fleet_state");

        migrationBuilder.DropColumn(
            name: "operation_lease_owner",
            table: "agent_fleet_state");
    }
}
