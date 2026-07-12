using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class EnforceAgentObservationSequence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_health_samples_agent_id",
            table: "health_samples");

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_agent_id_sequence",
            table: "health_samples",
            columns: new[] { "agent_id", "sequence" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_health_samples_agent_id_sequence",
            table: "health_samples");

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_agent_id",
            table: "health_samples",
            column: "agent_id");
    }
}
