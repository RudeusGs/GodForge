using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "collab");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "analysis");

            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.EnsureSchema(
                name: "governance");

            migrationBuilder.EnsureSchema(
                name: "storage");

            migrationBuilder.EnsureSchema(
                name: "metadata");

            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "repo");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "search");

            migrationBuilder.CreateTable(
                name: "app_versions",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    git_sha = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    deployed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    deployed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "archive_records",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_table = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_archive_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_backfill_runs",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    processed_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_backfill_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_classifications",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    table_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    column_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    classification = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_classifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "db_maintenance_runs",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_db_maintenance_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dead_letter_messages",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    queue_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    error_details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dead_letter_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "git_commit_files",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    commit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    old_file_path = table.Column<string>(type: "text", nullable: true),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    additions = table.Column<int>(type: "integer", nullable: true),
                    deletions = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_git_commit_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "health_rules",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    default_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "migration_runs",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    migration_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    migration_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    checksum = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    executed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_migration_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    headers = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "retention_policies",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    retention_days = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seed_history",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seed_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    checksum = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seed_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_health_checks",
                schema: "admin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    component = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    checked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_health_checks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    system_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    password_changed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    failed_login_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    security_stamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    concurrency_stamp = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_heartbeats",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    worker_instance_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    queues = table.Column<List<string>>(type: "text[]", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_worker_heartbeats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "health_rule_versions",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_rule_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_rule_versions_health_rules_rule_id",
                        column: x => x.rule_id,
                        principalSchema: "analysis",
                        principalTable: "health_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retention_runs",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    affected_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_retention_runs_retention_policies_policy_id",
                        column: x => x.policy_id,
                        principalSchema: "governance",
                        principalTable: "retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "admin_actions",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_actions_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "data_access_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_data_access_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_change_requests",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_email_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_change_requests_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "legal_holds",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_legal_holds", x => x.id);
                    table.ForeignKey(
                        name: "fk_legal_holds_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "login_events",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_login_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    in_app = table.Column<bool>(type: "boolean", nullable: false),
                    email = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    godot_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    visibility = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    health_score = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purge_requests",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purge_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_purge_requests_users_approved_by",
                        column: x => x.approved_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purge_requests_users_requested_by",
                        column: x => x.requested_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    device_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_searches",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    query = table.Column<string>(type: "text", nullable: false),
                    filters = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saved_searches", x => x.id);
                    table.ForeignKey(
                        name: "fk_saved_searches_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_audit_events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_audit_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "security_events",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_security_events_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_invites",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_invites", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_invites_users_invited_by",
                        column: x => x.invited_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bio = table.Column<string>(type: "text", nullable: true),
                    timezone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    locale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notification_in_app = table.Column<bool>(type: "boolean", nullable: false),
                    notification_email = table.Column<bool>(type: "boolean", nullable: false),
                    notification_digest = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "retention_run_items",
                schema: "governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    retention_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_table = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_retention_run_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_retention_run_items_retention_runs_retention_run_id",
                        column: x => x.retention_run_id,
                        principalSchema: "governance",
                        principalTable: "retention_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artifacts",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    object_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artifacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_artifacts_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artifacts_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_comments_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comments_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_issue_suppressions",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    node_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    suppressed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    suppressed_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_issue_suppressions", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_issue_suppressions_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_health_issue_suppressions_users_suppressed_by",
                        column: x => x.suppressed_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preview_requests",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    output_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preview_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_preview_requests_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_invites",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_invites", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_invites_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_invites_users_invited_by",
                        column: x => x.invited_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_member_history",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_member_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_member_history_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_member_history_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_members_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_settings",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    features = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_settings_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_exports",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_exports", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_exports_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    remote_url = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    default_branch = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    workspace_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    clone_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    repo_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    current_commit_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repositories", x => x.id);
                    table.ForeignKey(
                        name: "fk_repositories_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artifact_access_logs",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    accessed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artifact_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_artifact_access_logs_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "storage",
                        principalTable: "artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artifact_access_logs_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "artifact_versions",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artifact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    object_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artifact_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_artifact_versions_artifacts_artifact_id",
                        column: x => x.artifact_id,
                        principalSchema: "storage",
                        principalTable: "artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artifact_versions_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_log_hashes",
                schema: "audit",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    current_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    algorithm = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log_hashes", x => x.audit_log_id);
                    table.ForeignKey(
                        name: "fk_audit_log_hashes_audit_logs_audit_log_id",
                        column: x => x.audit_log_id,
                        principalSchema: "audit",
                        principalTable: "audit_logs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_activities_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_activities_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_activities_users_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "git_commits",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tree_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    author_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    author_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    authored_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    committed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_git_commits", x => x.id);
                    table.ForeignKey(
                        name: "fk_git_commits_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "git_refs",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    commit_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_git_refs", x => x.id);
                    table.ForeignKey(
                        name: "fk_git_refs_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    queue_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    result = table.Column<string>(type: "jsonb", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    max_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    timeout_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_heartbeat_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    cancellation_requested_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_jobs_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_jobs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_jobs_users_triggered_by",
                        column: x => x.triggered_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "repository_credentials",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repository_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_repository_credentials_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repository_files",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    last_commit_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repository_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_repository_files_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repository_snapshots",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commit_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repository_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_repository_snapshots_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repository_sync_runs",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repository_sync_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_repository_sync_runs_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_threads",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_threads", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_threads_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_threads_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_threads_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scene_diffs",
                schema: "storage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_commit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    head_commit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    scene_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    diff_json_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scene_diffs", x => x.id);
                    table.ForeignKey(
                        name: "fk_scene_diffs_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "webhook_events",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    delivery_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_events_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workspace_states",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    head_commit = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    branch_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    locked_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    locked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workspace_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_workspace_states_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_attempts",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    worker_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    worker_instance_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    stack_trace_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_attempts", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_attempts_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_cancellations",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_cancellations", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_cancellations_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_cancellations_users_requested_by",
                        column: x => x.requested_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_dependencies",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depends_on_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_dependencies_jobs_depends_on_job_id",
                        column: x => x.depends_on_job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_job_dependencies_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_events",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_events_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "job_leases",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    worker_instance_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    lease_token = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    leased_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    acquired_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    renewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_leases", x => x.id);
                    table.ForeignKey(
                        name: "fk_job_leases_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "repository_credential_versions",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    encrypted_secret = table.Column<string>(type: "text", nullable: false),
                    encryption_key_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repository_credential_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_repository_credential_versions_repository_credentials_crede",
                        column: x => x.credential_id,
                        principalSchema: "repo",
                        principalTable: "repository_credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_repository_credential_versions_users_created_by",
                        column: x => x.created_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_versions",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commit_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    blob_hash = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_file_versions_repository_files_file_id",
                        column: x => x.file_id,
                        principalSchema: "repo",
                        principalTable: "repository_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metadata_runs",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    run_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    schema_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0"),
                    file_count = table.Column<int>(type: "integer", nullable: false),
                    scene_count = table.Column<int>(type: "integer", nullable: false),
                    asset_count = table.Column<int>(type: "integer", nullable: false),
                    script_count = table.Column<int>(type: "integer", nullable: false),
                    resource_count = table.Column<int>(type: "integer", nullable: false),
                    dependency_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metadata_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_metadata_runs_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_metadata_runs_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_metadata_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_metadata_runs_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "search_documents",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: true),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_search_documents_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_search_documents_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_search_documents_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "search_index_runs",
                schema: "search",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    document_count = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_index_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_search_index_runs_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_search_index_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_search_index_runs_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "review_thread_comments",
                schema: "collab",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_thread_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_thread_comments_review_threads_thread_id",
                        column: x => x.thread_id,
                        principalSchema: "collab",
                        principalTable: "review_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_thread_comments_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "analysis_runs",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_runs_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_analysis_runs_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_analysis_runs_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_analysis_runs_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_analysis_runs_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    asset_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    dimensions = table.Column<string>(type: "jsonb", nullable: true),
                    thumbnail_artifact_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    imported_metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_assets_artifacts_thumbnail_artifact_id",
                        column: x => x.thumbnail_artifact_id,
                        principalSchema: "storage",
                        principalTable: "artifacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_assets_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assets_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assets_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dependencies",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    source_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    relation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_missing = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dependencies", x => x.id);
                    table.ForeignKey(
                        name: "fk_dependencies_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dependencies_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dependencies_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parser_diagnostics",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    line = table.Column<int>(type: "integer", nullable: true),
                    column = table.Column<int>(type: "integer", nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parser_diagnostics", x => x.id);
                    table.ForeignKey(
                        name: "fk_parser_diagnostics_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_parser_diagnostics_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    uid = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    file_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    properties = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resources", x => x.id);
                    table.ForeignKey(
                        name: "fk_resources_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_resources_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_resources_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scenes",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    scene_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    format_version = table.Column<int>(type: "integer", nullable: true),
                    uid = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    load_steps = table.Column<int>(type: "integer", nullable: true),
                    root_node_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    root_node_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    node_count = table.Column<int>(type: "integer", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    parsed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scenes", x => x.id);
                    table.ForeignKey(
                        name: "fk_scenes_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scenes_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scenes_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scripts",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    class_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    extends_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    line_count = table.Column<int>(type: "integer", nullable: false),
                    public_method_count = table.Column<int>(type: "integer", nullable: false),
                    signal_count = table.Column<int>(type: "integer", nullable: false),
                    exported_property_count = table.Column<int>(type: "integer", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    parse_summary = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scripts", x => x.id);
                    table.ForeignKey(
                        name: "fk_scripts_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scripts_metadata_runs_metadata_run_id",
                        column: x => x.metadata_run_id,
                        principalSchema: "metadata",
                        principalTable: "metadata_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_scripts_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dependency_graph_snapshots",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    graph_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    node_count = table.Column<int>(type: "integer", nullable: false),
                    edge_count = table.Column<int>(type: "integer", nullable: false),
                    cycle_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dependency_graph_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_dependency_graph_snapshots_analysis_runs_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalSchema: "analysis",
                        principalTable: "analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dependency_graph_snapshots_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dependency_graph_snapshots_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_dependency_graph_snapshots_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "health_reports",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: false),
                    total_issues = table.Column<int>(type: "integer", nullable: false),
                    critical_count = table.Column<int>(type: "integer", nullable: false),
                    warning_count = table.Column<int>(type: "integer", nullable: false),
                    info_count = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_reports_analysis_runs_analysis_run_id",
                        column: x => x.analysis_run_id,
                        principalSchema: "analysis",
                        principalTable: "analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_health_reports_git_repositories_repository_id",
                        column: x => x.repository_id,
                        principalSchema: "repo",
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_health_reports_jobs_job_id",
                        column: x => x.job_id,
                        principalSchema: "ops",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_health_reports_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "core",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_health_reports_repository_snapshots_snapshot_id",
                        column: x => x.snapshot_id,
                        principalSchema: "repo",
                        principalTable: "repository_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_imports",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    importer = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    import_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    uid = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    source_file = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    dest_files = table.Column<List<string>>(type: "text[]", nullable: false),
                    import_options = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_imports", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_imports_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "metadata",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene_connections",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_node_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    to_node_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    method_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    flags = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scene_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_scene_connections_scenes_scene_id",
                        column: x => x.scene_id,
                        principalSchema: "metadata",
                        principalTable: "scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene_nodes",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    node_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    node_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    parent_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    node_order = table.Column<int>(type: "integer", nullable: false),
                    script_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    groups = table.Column<List<string>>(type: "text[]", nullable: true),
                    important_properties = table.Column<string>(type: "jsonb", nullable: true),
                    properties = table.Column<string>(type: "jsonb", nullable: true),
                    warning_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scene_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_scene_nodes_scenes_scene_id",
                        column: x => x.scene_id,
                        principalSchema: "metadata",
                        principalTable: "scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "script_symbols",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    script_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    signature = table.Column<string>(type: "text", nullable: true),
                    line_number = table.Column<int>(type: "integer", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_script_symbols", x => x.id);
                    table.ForeignKey(
                        name: "fk_script_symbols_scripts_script_id",
                        column: x => x.script_id,
                        principalSchema: "metadata",
                        principalTable: "scripts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dependency_graph_edges",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    graph_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_node_key = table.Column<string>(type: "character varying(900)", maxLength: 900, nullable: false),
                    target_node_key = table.Column<string>(type: "character varying(900)", maxLength: 900, nullable: false),
                    relation = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(12,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dependency_graph_edges", x => x.id);
                    table.ForeignKey(
                        name: "fk_dependency_graph_edges_dependency_graph_snapshots_graph_sna",
                        column: x => x.graph_snapshot_id,
                        principalSchema: "analysis",
                        principalTable: "dependency_graph_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dependency_graph_nodes",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    graph_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_key = table.Column<string>(type: "character varying(900)", maxLength: 900, nullable: false),
                    node_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    metrics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dependency_graph_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_dependency_graph_nodes_dependency_graph_snapshots_graph_sna",
                        column: x => x.graph_snapshot_id,
                        principalSchema: "analysis",
                        principalTable: "dependency_graph_snapshots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_issues",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issue_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    node_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    message = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    is_suppressed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_health_issues", x => x.id);
                    table.ForeignKey(
                        name: "fk_health_issues_health_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "analysis",
                        principalTable: "health_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_health_issues_health_rules_rule_id",
                        column: x => x.rule_id,
                        principalSchema: "analysis",
                        principalTable: "health_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "scene_node_properties",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    property_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    property_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    property_value_hash = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scene_node_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_scene_node_properties_scene_nodes_scene_node_id",
                        column: x => x.scene_node_id,
                        principalSchema: "metadata",
                        principalTable: "scene_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scene_node_references",
                schema: "metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scene_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    target_uid = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    target_exists = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scene_node_references", x => x.id);
                    table.ForeignKey(
                        name: "fk_scene_node_references_scene_nodes_scene_node_id",
                        column: x => x.scene_node_id,
                        principalSchema: "metadata",
                        principalTable: "scene_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_actor",
                schema: "collab",
                table: "activities",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "ix_activities_project_time",
                schema: "collab",
                table: "activities",
                columns: new[] { "project_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_activities_repo",
                schema: "collab",
                table: "activities",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_actions_admin_user_id",
                schema: "audit",
                table: "admin_actions",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_job_id",
                schema: "analysis",
                table: "analysis_runs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_metadata_run_id",
                schema: "analysis",
                table: "analysis_runs",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_project_id",
                schema: "analysis",
                table: "analysis_runs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_repository_id",
                schema: "analysis",
                table: "analysis_runs",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_snapshot_id",
                schema: "analysis",
                table: "analysis_runs",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_artifact_access_logs_artifact_time",
                schema: "storage",
                table: "artifact_access_logs",
                columns: new[] { "artifact_id", "accessed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_artifact_access_logs_user_id",
                schema: "storage",
                table: "artifact_access_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_artifact_versions_created_by",
                schema: "storage",
                table: "artifact_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ux_artifact_versions_artifact_number",
                schema: "storage",
                table: "artifact_versions",
                columns: new[] { "artifact_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artifacts_created_by",
                schema: "storage",
                table: "artifacts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_artifacts_project_type",
                schema: "storage",
                table: "artifacts",
                columns: new[] { "project_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ux_artifacts_project_name",
                schema: "storage",
                table: "artifacts",
                columns: new[] { "project_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_imports_asset_id",
                schema: "metadata",
                table: "asset_imports",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_metadata_run_id",
                schema: "metadata",
                table: "assets",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_repository_id",
                schema: "metadata",
                table: "assets",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_snapshot_type",
                schema: "metadata",
                table: "assets",
                columns: new[] { "snapshot_id", "asset_type" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_thumbnail_artifact_id",
                schema: "metadata",
                table: "assets",
                column: "thumbnail_artifact_id");

            migrationBuilder.CreateIndex(
                name: "ux_assets_snapshot_path",
                schema: "metadata",
                table: "assets",
                columns: new[] { "snapshot_id", "file_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_actor_created",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "actor_user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_correlation",
                schema: "audit",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_project_id",
                schema: "audit",
                table: "audit_logs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_author_id",
                schema: "collab",
                table: "comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_project_id",
                schema: "collab",
                table: "comments",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_comments_target",
                schema: "collab",
                table: "comments",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_data_access_logs_user_id",
                schema: "audit",
                table: "data_access_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_dead_letter_messages_queue_time",
                schema: "ops",
                table: "dead_letter_messages",
                columns: new[] { "queue_name", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_dependencies_metadata_run_id",
                schema: "metadata",
                table: "dependencies",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependencies_missing",
                schema: "metadata",
                table: "dependencies",
                columns: new[] { "snapshot_id", "is_missing" });

            migrationBuilder.CreateIndex(
                name: "ix_dependencies_repository_id",
                schema: "metadata",
                table: "dependencies",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependencies_source",
                schema: "metadata",
                table: "dependencies",
                columns: new[] { "snapshot_id", "source_type", "source_path" });

            migrationBuilder.CreateIndex(
                name: "ix_dependencies_target",
                schema: "metadata",
                table: "dependencies",
                columns: new[] { "snapshot_id", "target_type", "target_path" });

            migrationBuilder.CreateIndex(
                name: "ux_dependencies_snapshot_edge",
                schema: "metadata",
                table: "dependencies",
                columns: new[] { "snapshot_id", "source_path", "target_path", "relation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_edges_graph_snapshot_id",
                schema: "analysis",
                table: "dependency_graph_edges",
                column: "graph_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_nodes_graph_snapshot_id",
                schema: "analysis",
                table: "dependency_graph_nodes",
                column: "graph_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_snapshots_analysis_run_id",
                schema: "analysis",
                table: "dependency_graph_snapshots",
                column: "analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_snapshots_project_id",
                schema: "analysis",
                table: "dependency_graph_snapshots",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_snapshots_repository_id",
                schema: "analysis",
                table: "dependency_graph_snapshots",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_dependency_graph_snapshots_snapshot_id",
                schema: "analysis",
                table: "dependency_graph_snapshots",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_change_requests_user",
                schema: "identity",
                table: "email_change_requests",
                columns: new[] { "user_id", "expires_at" },
                filter: "completed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_email_change_requests_token",
                schema: "identity",
                table: "email_change_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_file_versions_file_commit",
                schema: "repo",
                table: "file_versions",
                columns: new[] { "file_id", "commit_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_git_commit_files_commit",
                schema: "repo",
                table: "git_commit_files",
                column: "commit_id");

            migrationBuilder.CreateIndex(
                name: "ix_git_commit_files_path",
                schema: "repo",
                table: "git_commit_files",
                column: "file_path");

            migrationBuilder.CreateIndex(
                name: "ix_git_commits_repo_time",
                schema: "repo",
                table: "git_commits",
                columns: new[] { "repository_id", "committed_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_git_commits_repo_hash",
                schema: "repo",
                table: "git_commits",
                columns: new[] { "repository_id", "hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_git_refs_repo_default",
                schema: "repo",
                table: "git_refs",
                columns: new[] { "repository_id", "is_default" },
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ux_git_refs_repo_type_name",
                schema: "repo",
                table: "git_refs",
                columns: new[] { "repository_id", "type", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_issue_suppressions_project_id",
                schema: "analysis",
                table: "health_issue_suppressions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_issue_suppressions_suppressed_by",
                schema: "analysis",
                table: "health_issue_suppressions",
                column: "suppressed_by");

            migrationBuilder.CreateIndex(
                name: "ix_health_issues_file",
                schema: "analysis",
                table: "health_issues",
                column: "file_path");

            migrationBuilder.CreateIndex(
                name: "ix_health_issues_report_severity",
                schema: "analysis",
                table: "health_issues",
                columns: new[] { "report_id", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_health_issues_rule_id",
                schema: "analysis",
                table: "health_issues",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_reports_analysis_run_id",
                schema: "analysis",
                table: "health_reports",
                column: "analysis_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_reports_job_id",
                schema: "analysis",
                table: "health_reports",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_health_reports_project_created",
                schema: "analysis",
                table: "health_reports",
                columns: new[] { "project_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_health_reports_repository_id",
                schema: "analysis",
                table: "health_reports",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ux_health_reports_snapshot",
                schema: "analysis",
                table: "health_reports",
                column: "snapshot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_health_rule_versions_rule_id",
                schema: "analysis",
                table: "health_rule_versions",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ux_health_rules_code",
                schema: "analysis",
                table: "health_rules",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_inbox_message_consumer",
                schema: "ops",
                table: "inbox_messages",
                columns: new[] { "message_id", "consumer_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_job_attempts_job_number",
                schema: "ops",
                table: "job_attempts",
                columns: new[] { "job_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_cancellations_job_id",
                schema: "ops",
                table: "job_cancellations",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_cancellations_requested_by",
                schema: "ops",
                table: "job_cancellations",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_job_dependencies_depends_on_job_id",
                schema: "ops",
                table: "job_dependencies",
                column: "depends_on_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_dependencies_job_id",
                schema: "ops",
                table: "job_dependencies",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_events_job_created",
                schema: "ops",
                table: "job_events",
                columns: new[] { "job_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_job_leases_expired",
                schema: "ops",
                table: "job_leases",
                column: "leased_until");

            migrationBuilder.CreateIndex(
                name: "ux_job_leases_active",
                schema: "ops",
                table: "job_leases",
                column: "job_id",
                unique: true,
                filter: "released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_project_created",
                schema: "ops",
                table: "jobs",
                columns: new[] { "project_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_queue_poll",
                schema: "ops",
                table: "jobs",
                columns: new[] { "status", "available_at", "priority", "created_at" },
                descending: new[] { false, false, true, false });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_repository_id",
                schema: "ops",
                table: "jobs",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_triggered_by",
                schema: "ops",
                table: "jobs",
                column: "triggered_by");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_type_status",
                schema: "ops",
                table: "jobs",
                columns: new[] { "type", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_jobs_idempotency",
                schema: "ops",
                table: "jobs",
                columns: new[] { "project_id", "type", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_legal_holds_created_by",
                schema: "governance",
                table: "legal_holds",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_login_events_ip",
                schema: "identity",
                table: "login_events",
                columns: new[] { "ip_address", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_login_events_user",
                schema: "identity",
                table: "login_events",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_metadata_runs_job_id",
                schema: "metadata",
                table: "metadata_runs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_metadata_runs_project_id",
                schema: "metadata",
                table: "metadata_runs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_metadata_runs_repo_created",
                schema: "metadata",
                table: "metadata_runs",
                columns: new[] { "repository_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_metadata_runs_snapshot_completed",
                schema: "metadata",
                table: "metadata_runs",
                column: "snapshot_id",
                unique: true,
                filter: "status = 'completed'");

            migrationBuilder.CreateIndex(
                name: "ux_notification_preferences_user_event",
                schema: "collab",
                table: "notification_preferences",
                columns: new[] { "user_id", "event_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_project_id",
                schema: "collab",
                table: "notifications",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_read",
                schema: "collab",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_time",
                schema: "collab",
                table: "notifications",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_status_available",
                schema: "ops",
                table: "outbox_messages",
                columns: new[] { "status", "available_at" });

            migrationBuilder.CreateIndex(
                name: "ix_parser_diagnostics_metadata_run_id",
                schema: "metadata",
                table: "parser_diagnostics",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_parser_diagnostics_snapshot_id",
                schema: "metadata",
                table: "parser_diagnostics",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user",
                schema: "identity",
                table: "password_reset_tokens",
                columns: new[] { "user_id", "expires_at" },
                filter: "used_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_password_reset_tokens_hash",
                schema: "identity",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preview_requests_project_asset",
                schema: "storage",
                table: "preview_requests",
                columns: new[] { "project_id", "asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_invites_invited_by",
                schema: "core",
                table: "project_invites",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_project_invites_project_email_status",
                schema: "core",
                table: "project_invites",
                columns: new[] { "project_id", "email", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_project_invites_token",
                schema: "core",
                table: "project_invites",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_member_history_project",
                schema: "core",
                table: "project_member_history",
                columns: new[] { "project_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_project_member_history_user_id",
                schema: "core",
                table: "project_member_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_project_role",
                schema: "core",
                table: "project_members",
                columns: new[] { "project_id", "role" });

            migrationBuilder.CreateIndex(
                name: "ix_project_members_user",
                schema: "core",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_project_members_active",
                schema: "core",
                table: "project_members",
                columns: new[] { "project_id", "user_id" },
                unique: true,
                filter: "removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_project_settings_project",
                schema: "core",
                table: "project_settings",
                column: "project_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_projects_created_by",
                schema: "core",
                table: "projects",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_projects_status",
                schema: "core",
                table: "projects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_projects_slug_active",
                schema: "core",
                table: "projects",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_purge_requests_approved_by",
                schema: "governance",
                table: "purge_requests",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_purge_requests_requested_by",
                schema: "governance",
                table: "purge_requests",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_expires",
                schema: "identity",
                table: "refresh_tokens",
                columns: new[] { "user_id", "expires_at" },
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_hash",
                schema: "identity",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_exports_project_time",
                schema: "storage",
                table: "report_exports",
                columns: new[] { "project_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_repositories_org_status",
                schema: "repo",
                table: "repositories",
                columns: new[] { "project_id", "clone_status" });

            migrationBuilder.CreateIndex(
                name: "ux_repositories_project",
                schema: "repo",
                table: "repositories",
                column: "project_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_repository_credential_versions_created_by",
                schema: "repo",
                table: "repository_credential_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_repository_credential_versions_time",
                schema: "repo",
                table: "repository_credential_versions",
                columns: new[] { "credential_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_repository_credentials_repo_active",
                schema: "repo",
                table: "repository_credentials",
                columns: new[] { "repository_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_repository_files_repo_commit",
                schema: "repo",
                table: "repository_files",
                columns: new[] { "repository_id", "last_commit_hash" });

            migrationBuilder.CreateIndex(
                name: "ux_repository_files_repo_path",
                schema: "repo",
                table: "repository_files",
                columns: new[] { "repository_id", "path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_repository_snapshots_repo_time",
                schema: "repo",
                table: "repository_snapshots",
                columns: new[] { "repository_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_repository_snapshots_repo_commit",
                schema: "repo",
                table: "repository_snapshots",
                columns: new[] { "repository_id", "commit_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_repository_sync_runs_repo_time",
                schema: "repo",
                table: "repository_sync_runs",
                columns: new[] { "repository_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_repository_sync_runs_status",
                schema: "repo",
                table: "repository_sync_runs",
                columns: new[] { "repository_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_resources_metadata_run_id",
                schema: "metadata",
                table: "resources",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_resources_repository_id",
                schema: "metadata",
                table: "resources",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ux_resources_snapshot_path",
                schema: "metadata",
                table: "resources",
                columns: new[] { "snapshot_id", "file_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retention_run_items_retention_run_id",
                schema: "governance",
                table: "retention_run_items",
                column: "retention_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_retention_runs_policy_id",
                schema: "governance",
                table: "retention_runs",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_thread_comments_author_id",
                schema: "collab",
                table: "review_thread_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_thread_comments_thread_time",
                schema: "collab",
                table: "review_thread_comments",
                columns: new[] { "thread_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_review_threads_created_by",
                schema: "collab",
                table: "review_threads",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_review_threads_project_id",
                schema: "collab",
                table: "review_threads",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_threads_target",
                schema: "collab",
                table: "review_threads",
                columns: new[] { "repository_id", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_saved_searches_user_id",
                schema: "search",
                table: "saved_searches",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_scene_connections_scene_id",
                schema: "metadata",
                table: "scene_connections",
                column: "scene_id");

            migrationBuilder.CreateIndex(
                name: "ux_scene_diffs_repo_commits_path",
                schema: "storage",
                table: "scene_diffs",
                columns: new[] { "repository_id", "base_commit", "head_commit", "scene_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scene_node_properties_node",
                schema: "metadata",
                table: "scene_node_properties",
                column: "scene_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_scene_node_references_node",
                schema: "metadata",
                table: "scene_node_references",
                column: "scene_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_scene_node_references_target",
                schema: "metadata",
                table: "scene_node_references",
                column: "target_path");

            migrationBuilder.CreateIndex(
                name: "ix_scene_nodes_order",
                schema: "metadata",
                table: "scene_nodes",
                columns: new[] { "scene_id", "node_order" });

            migrationBuilder.CreateIndex(
                name: "ix_scene_nodes_type",
                schema: "metadata",
                table: "scene_nodes",
                columns: new[] { "scene_id", "node_type" });

            migrationBuilder.CreateIndex(
                name: "ux_scene_nodes_scene_path",
                schema: "metadata",
                table: "scene_nodes",
                columns: new[] { "scene_id", "node_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scenes_metadata_run_id",
                schema: "metadata",
                table: "scenes",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_scenes_repo_path",
                schema: "metadata",
                table: "scenes",
                columns: new[] { "repository_id", "file_path" });

            migrationBuilder.CreateIndex(
                name: "ux_scenes_snapshot_path",
                schema: "metadata",
                table: "scenes",
                columns: new[] { "snapshot_id", "file_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_script_symbols_script_id",
                schema: "metadata",
                table: "script_symbols",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "ix_scripts_class_name",
                schema: "metadata",
                table: "scripts",
                columns: new[] { "snapshot_id", "class_name" });

            migrationBuilder.CreateIndex(
                name: "ix_scripts_metadata_run_id",
                schema: "metadata",
                table: "scripts",
                column: "metadata_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_scripts_repository_id",
                schema: "metadata",
                table: "scripts",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ux_scripts_snapshot_path",
                schema: "metadata",
                table: "scripts",
                columns: new[] { "snapshot_id", "file_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_project_type",
                schema: "search",
                table: "search_documents",
                columns: new[] { "project_id", "entity_type" });

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_repository_id",
                schema: "search",
                table: "search_documents",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_snapshot_id",
                schema: "search",
                table: "search_documents",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_documents_vector",
                schema: "search",
                table: "search_documents",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_runs_job_id",
                schema: "search",
                table: "search_index_runs",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_runs_project_id",
                schema: "search",
                table: "search_index_runs",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_search_index_runs_snapshot_id",
                schema: "search",
                table: "search_index_runs",
                column: "snapshot_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_user_id",
                schema: "audit",
                table: "security_audit_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_security_events_user",
                schema: "identity",
                table: "security_events",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_user_invites_email_status",
                schema: "identity",
                table: "user_invites",
                columns: new[] { "email", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_user_invites_invited_by",
                schema: "identity",
                table: "user_invites",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ux_user_invites_token",
                schema: "identity",
                table: "user_invites",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_user_profiles_user_id",
                schema: "identity",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_active",
                schema: "identity",
                table: "user_sessions",
                columns: new[] { "user_id", "expires_at" },
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_user_settings_user_id",
                schema: "identity",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                schema: "identity",
                table: "users",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                schema: "identity",
                table: "users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                schema: "identity",
                table: "users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_events_repository_id",
                schema: "repo",
                table: "webhook_events",
                column: "repository_id");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_events_provider_delivery",
                schema: "repo",
                table: "webhook_events",
                columns: new[] { "provider", "delivery_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_worker_heartbeats_instance",
                schema: "ops",
                table: "worker_heartbeats",
                column: "worker_instance_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_workspace_states_repo",
                schema: "repo",
                table: "workspace_states",
                column: "repository_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "activities",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "admin_actions",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "app_versions",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "archive_records",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "artifact_access_logs",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "artifact_versions",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "asset_imports",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "audit_log_hashes",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "comments",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "data_access_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "data_backfill_runs",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "data_classifications",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "db_maintenance_runs",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "dead_letter_messages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "dependencies",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "dependency_graph_edges",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "dependency_graph_nodes",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "email_change_requests",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "file_versions",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "git_commit_files",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "git_commits",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "git_refs",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "health_issue_suppressions",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "health_issues",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "health_rule_versions",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_attempts",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_cancellations",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_dependencies",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_events",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "job_leases",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "legal_holds",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "login_events",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "migration_runs",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "notification_preferences",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "parser_diagnostics",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "preview_requests",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "project_invites",
                schema: "core");

            migrationBuilder.DropTable(
                name: "project_member_history",
                schema: "core");

            migrationBuilder.DropTable(
                name: "project_members",
                schema: "core");

            migrationBuilder.DropTable(
                name: "project_settings",
                schema: "core");

            migrationBuilder.DropTable(
                name: "purge_requests",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "report_exports",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "repository_credential_versions",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "repository_sync_runs",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "resources",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "retention_run_items",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "review_thread_comments",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "saved_searches",
                schema: "search");

            migrationBuilder.DropTable(
                name: "scene_connections",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "scene_diffs",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "scene_node_properties",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "scene_node_references",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "script_symbols",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "search_documents",
                schema: "search");

            migrationBuilder.DropTable(
                name: "search_index_runs",
                schema: "search");

            migrationBuilder.DropTable(
                name: "security_audit_events",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "security_events",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "seed_history",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "system_health_checks",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "user_invites",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_settings",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "webhook_events",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "worker_heartbeats",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "workspace_states",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "dependency_graph_snapshots",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "repository_files",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "health_reports",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "health_rules",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "repository_credentials",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "retention_runs",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "review_threads",
                schema: "collab");

            migrationBuilder.DropTable(
                name: "scene_nodes",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "scripts",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "artifacts",
                schema: "storage");

            migrationBuilder.DropTable(
                name: "analysis_runs",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "retention_policies",
                schema: "governance");

            migrationBuilder.DropTable(
                name: "scenes",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "metadata_runs",
                schema: "metadata");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "repository_snapshots",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "repositories",
                schema: "repo");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "core");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");
        }
    }
}
