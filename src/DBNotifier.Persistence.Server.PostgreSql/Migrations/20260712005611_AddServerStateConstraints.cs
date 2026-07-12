using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class AddServerStateConstraints : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_user_state",
            table: "users",
            sql: "state IN ('Active','Disabled','Locked','Unknown')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries",
            sql: "state IN ('Pending','Delivering','Delivered','Failed','Cancelled')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_incident_severity",
            table: "incidents",
            sql: "severity IN ('Info','Warning','Error','Critical')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_incident_state",
            table: "incidents",
            sql: "status IN ('Open','Acknowledged','Closed')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_health_status",
            table: "health_samples",
            sql: "status IN ('Healthy','Degraded','Unavailable','AuthFailed','Timeout','Maintenance','Unknown')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_event_severity",
            table: "events",
            sql: "severity IN ('Info','Warning','Error','Critical')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_command_attempt_state",
            table: "command_attempts",
            sql: "state IN ('Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_audit_outcome",
            table: "audit_entries",
            sql: "outcome IN ('Succeeded','Failed','Denied','Unknown')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_state",
            table: "agents",
            sql: "state IN ('Active','Revoked','Disabled','Unknown')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_agent_capability_state",
            table: "agent_capabilities",
            sql: "state IN ('Supported','Unsupported','Unavailable','Unknown')");

        migrationBuilder.AddCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands",
            sql: "state IN ('Pending','Available','Acknowledged','Running','Succeeded','Failed','Cancelled','Expired','Rejected','UnknownOutcome')");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_user_state",
            table: "users");

        migrationBuilder.DropCheckConstraint(
            name: "ck_delivery_state",
            table: "notification_deliveries");

        migrationBuilder.DropCheckConstraint(
            name: "ck_incident_severity",
            table: "incidents");

        migrationBuilder.DropCheckConstraint(
            name: "ck_incident_state",
            table: "incidents");

        migrationBuilder.DropCheckConstraint(
            name: "ck_health_status",
            table: "health_samples");

        migrationBuilder.DropCheckConstraint(
            name: "ck_event_severity",
            table: "events");

        migrationBuilder.DropCheckConstraint(
            name: "ck_command_attempt_state",
            table: "command_attempts");

        migrationBuilder.DropCheckConstraint(
            name: "ck_audit_outcome",
            table: "audit_entries");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_state",
            table: "agents");

        migrationBuilder.DropCheckConstraint(
            name: "ck_agent_capability_state",
            table: "agent_capabilities");

        migrationBuilder.DropCheckConstraint(
            name: "ck_command_state",
            table: "administrative_commands");
    }
}
