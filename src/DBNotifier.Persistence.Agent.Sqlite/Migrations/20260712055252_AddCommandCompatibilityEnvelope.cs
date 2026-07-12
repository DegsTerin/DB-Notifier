using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Agent.Sqlite.Migrations;

/// <inheritdoc />
public partial class AddCommandCompatibilityEnvelope : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "expected_agent_version",
            table: "inbox_commands",
            type: "TEXT",
            maxLength: 64,
            nullable: false,
            defaultValue: "unknown");

        migrationBuilder.AddColumn<string>(
            name: "expected_provider_version",
            table: "inbox_commands",
            type: "TEXT",
            maxLength: 64,
            nullable: false,
            defaultValue: "unknown");

        migrationBuilder.AddColumn<string>(
            name: "provider_id",
            table: "inbox_commands",
            type: "TEXT",
            maxLength: 64,
            nullable: false,
            defaultValue: "unknown");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "expected_agent_version",
            table: "inbox_commands");

        migrationBuilder.DropColumn(
            name: "expected_provider_version",
            table: "inbox_commands");

        migrationBuilder.DropColumn(
            name: "provider_id",
            table: "inbox_commands");
    }
}
