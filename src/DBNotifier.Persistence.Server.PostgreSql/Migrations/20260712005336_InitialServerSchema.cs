using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBNotifier.Persistence.Server.PostgreSql.Migrations;

/// <inheritdoc />
public partial class InitialServerSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "agents",
            columns: table => new
            {
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                installation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                agent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                certificate_thumbprint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agents", x => x.agent_id);
            });

        migrationBuilder.CreateTable(
            name: "alert_rules",
            columns: table => new
            {
                alert_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                instance_scope_json = table.Column<string>(type: "jsonb", nullable: true),
                rule_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                configuration_json = table.Column<string>(type: "jsonb", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_alert_rules", x => x.alert_rule_id);
            });

        migrationBuilder.CreateTable(
            name: "audit_entries",
            columns: table => new
            {
                audit_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                actor_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                actor_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                target_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                details_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_entries", x => x.audit_entry_id);
            });

        migrationBuilder.CreateTable(
            name: "notification_channels",
            columns: table => new
            {
                notification_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                channel_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                non_secret_configuration_json = table.Column<string>(type: "jsonb", nullable: false),
                credential_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_channels", x => x.notification_channel_id);
            });

        migrationBuilder.CreateTable(
            name: "outbox_messages",
            columns: table => new
            {
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                message_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                schema_version = table.Column<int>(type: "integer", nullable: false),
                payload_json = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox_messages", x => x.message_id);
                table.CheckConstraint("ck_server_outbox_attempts", "attempt_count >= 0");
                table.CheckConstraint("ck_server_outbox_schema", "schema_version >= 1");
            });

        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_permissions", x => x.permission_id);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_system = table.Column<bool>(type: "boolean", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.role_id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                subject_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.user_id);
            });

        migrationBuilder.CreateTable(
            name: "agent_capabilities",
            columns: table => new
            {
                agent_capability_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                capability_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                provider_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                provider_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_capabilities", x => x.agent_capability_id);
                table.ForeignKey(
                    name: "FK_agent_capabilities_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "agent_heartbeats",
            columns: table => new
            {
                heartbeat_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                agent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                protocol_minimum = table.Column<int>(type: "integer", nullable: false),
                protocol_maximum = table.Column<int>(type: "integer", nullable: false),
                queue_depth = table.Column<long>(type: "bigint", nullable: false),
                oldest_queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                agent_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_agent_heartbeats", x => x.heartbeat_id);
                table.CheckConstraint("ck_heartbeat_protocol", "protocol_minimum >= 1 AND protocol_maximum >= protocol_minimum");
                table.CheckConstraint("ck_heartbeat_queue", "queue_depth >= 0");
                table.CheckConstraint("ck_heartbeat_sequence", "sequence >= 0");
                table.ForeignKey(
                    name: "FK_agent_heartbeats_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "database_instances",
            columns: table => new
            {
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                provider_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                endpoint_json = table.Column<string>(type: "jsonb", nullable: false),
                monitoring_credential_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                administrative_credential_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                assigned_agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                tags_json = table.Column<string>(type: "jsonb", nullable: false),
                interval_seconds = table.Column<int>(type: "integer", nullable: false),
                timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                retry_count = table.Column<int>(type: "integer", nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_database_instances", x => x.instance_id);
                table.CheckConstraint("ck_instance_interval", "interval_seconds >= 1");
                table.CheckConstraint("ck_instance_retries", "retry_count >= 0");
                table.CheckConstraint("ck_instance_timeout", "timeout_seconds >= 1");
                table.ForeignKey(
                    name: "FK_database_instances_agents_assigned_agent_id",
                    column: x => x.assigned_agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                table.ForeignKey(
                    name: "FK_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "permission_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "role_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "role_assignments",
            columns: table => new
            {
                role_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                scope_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                scope_value = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_assignments", x => x.role_assignment_id);
                table.ForeignKey(
                    name: "FK_role_assignments_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "role_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_role_assignments_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "administrative_commands",
            columns: table => new
            {
                command_id = table.Column<Guid>(type: "uuid", nullable: false),
                idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                capability_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                typed_parameters_json = table.Column<string>(type: "jsonb", nullable: false),
                requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                authorization_snapshot_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                expected_agent_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                expected_provider_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_administrative_commands", x => x.command_id);
                table.CheckConstraint("ck_command_expiry", "expires_at > requested_at");
                table.ForeignKey(
                    name: "FK_administrative_commands_agents_assigned_agent_id",
                    column: x => x.assigned_agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_administrative_commands_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_administrative_commands_users_requested_by_user_id",
                    column: x => x.requested_by_user_id,
                    principalTable: "users",
                    principalColumn: "user_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "health_samples",
            columns: table => new
            {
                observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                message_id = table.Column<Guid>(type: "uuid", nullable: false),
                sequence = table.Column<long>(type: "bigint", nullable: false),
                provider_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                provider_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                evidence_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                duration_milliseconds = table.Column<long>(type: "bigint", nullable: false),
                error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                redacted_details_json = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_health_samples", x => x.observation_id);
                table.CheckConstraint("ck_health_duration", "duration_milliseconds >= 0");
                table.ForeignKey(
                    name: "FK_health_samples_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_health_samples_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "incidents",
            columns: table => new
            {
                incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                closed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_incidents", x => x.incident_id);
                table.ForeignKey(
                    name: "FK_incidents_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "maintenance_windows",
            columns: table => new
            {
                maintenance_window_id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_maintenance_windows", x => x.maintenance_window_id);
                table.CheckConstraint("ck_maintenance_range", "ends_at > starts_at");
                table.ForeignKey(
                    name: "FK_maintenance_windows_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "command_attempts",
            columns: table => new
            {
                command_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                command_id = table.Column<Guid>(type: "uuid", nullable: false),
                attempt_number = table.Column<int>(type: "integer", nullable: false),
                started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                post_probe_observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                diagnostic_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_command_attempts", x => x.command_attempt_id);
                table.CheckConstraint("ck_command_attempt_number", "attempt_number >= 1");
                table.ForeignKey(
                    name: "FK_command_attempts_administrative_commands_command_id",
                    column: x => x.command_id,
                    principalTable: "administrative_commands",
                    principalColumn: "command_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_command_attempts_health_samples_post_probe_observation_id",
                    column: x => x.post_probe_observation_id,
                    principalTable: "health_samples",
                    principalColumn: "observation_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                source_observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                details_json = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_events", x => x.event_id);
                table.ForeignKey(
                    name: "FK_events_agents_agent_id",
                    column: x => x.agent_id,
                    principalTable: "agents",
                    principalColumn: "agent_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_events_database_instances_instance_id",
                    column: x => x.instance_id,
                    principalTable: "database_instances",
                    principalColumn: "instance_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_events_health_samples_source_observation_id",
                    column: x => x.source_observation_id,
                    principalTable: "health_samples",
                    principalColumn: "observation_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "notification_deliveries",
            columns: table => new
            {
                notification_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                notification_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                attempt_count = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notification_deliveries", x => x.notification_delivery_id);
                table.CheckConstraint("ck_delivery_attempts", "attempt_count >= 0");
                table.ForeignKey(
                    name: "FK_notification_deliveries_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "event_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_notification_deliveries_notification_channels_notification_~",
                    column: x => x.notification_channel_id,
                    principalTable: "notification_channels",
                    principalColumn: "notification_channel_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_administrative_commands_assigned_agent_id_state_expires_at",
            table: "administrative_commands",
            columns: new[] { "assigned_agent_id", "state", "expires_at" });

        migrationBuilder.CreateIndex(
            name: "IX_administrative_commands_idempotency_key",
            table: "administrative_commands",
            column: "idempotency_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_administrative_commands_instance_id_requested_at",
            table: "administrative_commands",
            columns: new[] { "instance_id", "requested_at" });

        migrationBuilder.CreateIndex(
            name: "IX_administrative_commands_requested_by_user_id",
            table: "administrative_commands",
            column: "requested_by_user_id");

        migrationBuilder.CreateIndex(
            name: "IX_agent_capabilities_agent_id_capability_id_provider_type_pro~",
            table: "agent_capabilities",
            columns: new[] { "agent_id", "capability_id", "provider_type", "provider_version", "platform" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agent_heartbeats_agent_id_received_at",
            table: "agent_heartbeats",
            columns: new[] { "agent_id", "received_at" });

        migrationBuilder.CreateIndex(
            name: "IX_agent_heartbeats_message_id",
            table: "agent_heartbeats",
            column: "message_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agents_certificate_thumbprint",
            table: "agents",
            column: "certificate_thumbprint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_agents_environment_state",
            table: "agents",
            columns: new[] { "environment", "state" });

        migrationBuilder.CreateIndex(
            name: "IX_agents_installation_id",
            table: "agents",
            column: "installation_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_alert_rules_enabled_rule_type",
            table: "alert_rules",
            columns: new[] { "enabled", "rule_type" });

        migrationBuilder.CreateIndex(
            name: "IX_audit_entries_actor_type_actor_id_occurred_at",
            table: "audit_entries",
            columns: new[] { "actor_type", "actor_id", "occurred_at" });

        migrationBuilder.CreateIndex(
            name: "IX_audit_entries_correlation_id",
            table: "audit_entries",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "IX_audit_entries_occurred_at",
            table: "audit_entries",
            column: "occurred_at");

        migrationBuilder.CreateIndex(
            name: "IX_command_attempts_command_id_attempt_number",
            table: "command_attempts",
            columns: new[] { "command_id", "attempt_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_command_attempts_post_probe_observation_id",
            table: "command_attempts",
            column: "post_probe_observation_id");

        migrationBuilder.CreateIndex(
            name: "IX_database_instances_assigned_agent_id",
            table: "database_instances",
            column: "assigned_agent_id");

        migrationBuilder.CreateIndex(
            name: "IX_database_instances_environment_provider_type_enabled",
            table: "database_instances",
            columns: new[] { "environment", "provider_type", "enabled" });

        migrationBuilder.CreateIndex(
            name: "IX_events_agent_id",
            table: "events",
            column: "agent_id");

        migrationBuilder.CreateIndex(
            name: "IX_events_correlation_id",
            table: "events",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "IX_events_event_type_observed_at",
            table: "events",
            columns: new[] { "event_type", "observed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_events_instance_id_observed_at",
            table: "events",
            columns: new[] { "instance_id", "observed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_events_source_observation_id",
            table: "events",
            column: "source_observation_id");

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_agent_id",
            table: "health_samples",
            column: "agent_id");

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_instance_id_observed_at",
            table: "health_samples",
            columns: new[] { "instance_id", "observed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_message_id",
            table: "health_samples",
            column: "message_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_health_samples_status_observed_at",
            table: "health_samples",
            columns: new[] { "status", "observed_at" });

        migrationBuilder.CreateIndex(
            name: "IX_incidents_instance_id_status",
            table: "incidents",
            columns: new[] { "instance_id", "status" });

        migrationBuilder.CreateIndex(
            name: "IX_incidents_opened_at",
            table: "incidents",
            column: "opened_at");

        migrationBuilder.CreateIndex(
            name: "IX_maintenance_windows_instance_id_starts_at_ends_at",
            table: "maintenance_windows",
            columns: new[] { "instance_id", "starts_at", "ends_at" });

        migrationBuilder.CreateIndex(
            name: "IX_notification_channels_channel_type_enabled",
            table: "notification_channels",
            columns: new[] { "channel_type", "enabled" });

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_event_id",
            table: "notification_deliveries",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_notification_channel_id_event_id",
            table: "notification_deliveries",
            columns: new[] { "notification_channel_id", "event_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_notification_deliveries_state_created_at",
            table: "notification_deliveries",
            columns: new[] { "state", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_outbox_messages_published_at_available_at",
            table: "outbox_messages",
            columns: new[] { "published_at", "available_at" });

        migrationBuilder.CreateIndex(
            name: "IX_permissions_code",
            table: "permissions",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_assignments_role_id",
            table: "role_assignments",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "IX_role_assignments_user_id_role_id_scope_type_scope_value",
            table: "role_assignments",
            columns: new[] { "user_id", "role_id", "scope_type", "scope_value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_permissions_permission_id",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "IX_roles_name",
            table: "roles",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_users_subject_id",
            table: "users",
            column: "subject_id",
            unique: true);

        migrationBuilder.Sql(
            """
            CREATE FUNCTION prevent_audit_entry_mutation()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'audit_entries are append-only';
            END;
            $$;

            CREATE TRIGGER audit_entries_append_only
            BEFORE UPDATE OR DELETE ON audit_entries
            FOR EACH ROW EXECUTE FUNCTION prevent_audit_entry_mutation();
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS audit_entries_append_only ON audit_entries;
            DROP FUNCTION IF EXISTS prevent_audit_entry_mutation();
            """);

        migrationBuilder.DropTable(
            name: "agent_capabilities");

        migrationBuilder.DropTable(
            name: "agent_heartbeats");

        migrationBuilder.DropTable(
            name: "alert_rules");

        migrationBuilder.DropTable(
            name: "audit_entries");

        migrationBuilder.DropTable(
            name: "command_attempts");

        migrationBuilder.DropTable(
            name: "incidents");

        migrationBuilder.DropTable(
            name: "maintenance_windows");

        migrationBuilder.DropTable(
            name: "notification_deliveries");

        migrationBuilder.DropTable(
            name: "outbox_messages");

        migrationBuilder.DropTable(
            name: "role_assignments");

        migrationBuilder.DropTable(
            name: "role_permissions");

        migrationBuilder.DropTable(
            name: "administrative_commands");

        migrationBuilder.DropTable(
            name: "events");

        migrationBuilder.DropTable(
            name: "notification_channels");

        migrationBuilder.DropTable(
            name: "permissions");

        migrationBuilder.DropTable(
            name: "roles");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropTable(
            name: "health_samples");

        migrationBuilder.DropTable(
            name: "database_instances");

        migrationBuilder.DropTable(
            name: "agents");
    }
}
