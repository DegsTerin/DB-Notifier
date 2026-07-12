using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class AddAgentStateConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_health_status",
            table: "health_observations",
            sql: "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_command_state",
            table: "inbox_commands");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_health_status",
            table: "health_observations");
    }
}
