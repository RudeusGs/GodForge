using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncBlueprintModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "identity");

            migrationBuilder.RenameColumn(
                name: "clone_status",
                schema: "repo",
                table: "repositories",
                newName: "status");

            migrationBuilder.RenameIndex(
                name: "ix_repositories_org_status",
                schema: "repo",
                table: "repositories",
                newName: "ix_repositories_project_status");

            migrationBuilder.AlterColumn<string>(
                name: "commit_hash",
                schema: "repo",
                table: "repository_snapshots",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<bool>(
                name: "auto_analyze_enabled",
                schema: "repo",
                table: "repositories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "clone_url_sanitized",
                schema: "repo",
                table: "repositories",
                type: "character varying(800)",
                maxLength: 800,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "external_repository_id",
                schema: "repo",
                table: "repositories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hosted_repository_id",
                schema: "repo",
                table: "repositories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error_code",
                schema: "repo",
                table: "repositories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mode",
                schema: "repo",
                table: "repositories",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ai_analysis_runs",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commit_sha = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    analysis_profile = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    input_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    raw_artifact_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    input_token_count = table.Column<int>(type: "integer", nullable: true),
                    output_token_count = table.Column<int>(type: "integer", nullable: true),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_analysis_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_findings",
                schema: "analysis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    recommendation = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    evidence_refs = table.Column<string>(type: "jsonb", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_findings", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_findings_ai_analysis_runs_run_id",
                        column: x => x.run_id,
                        principalSchema: "analysis",
                        principalTable: "ai_analysis_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_repositories_provider_external_id",
                schema: "repo",
                table: "repositories",
                columns: new[] { "provider", "external_repository_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analysis_runs_cache",
                schema: "analysis",
                table: "ai_analysis_runs",
                columns: new[] { "repository_id", "commit_sha", "analysis_profile", "provider", "model", "prompt_version", "input_hash", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_analysis_runs_project_started",
                schema: "analysis",
                table: "ai_analysis_runs",
                columns: new[] { "project_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ux_ai_findings_run_fingerprint",
                schema: "analysis",
                table: "ai_findings",
                columns: new[] { "run_id", "fingerprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_findings",
                schema: "analysis");

            migrationBuilder.DropTable(
                name: "ai_analysis_runs",
                schema: "analysis");

            migrationBuilder.DropIndex(
                name: "ix_repositories_provider_external_id",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "auto_analyze_enabled",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "clone_url_sanitized",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "external_repository_id",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "hosted_repository_id",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "last_error_code",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "mode",
                schema: "repo",
                table: "repositories");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "repo",
                table: "repositories",
                newName: "clone_status");

            migrationBuilder.RenameIndex(
                name: "ix_repositories_project_status",
                schema: "repo",
                table: "repositories",
                newName: "ix_repositories_org_status");

            migrationBuilder.AlterColumn<string>(
                name: "commit_hash",
                schema: "repo",
                table: "repository_snapshots",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
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
        }
    }
}
