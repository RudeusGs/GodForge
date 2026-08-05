using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260805030755_M1TenantFoundation")]
public partial class M1TenantFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Legacy projects have no tenant ownership evidence. Refuse an unsafe automatic assignment.
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM core.projects) THEN
                    RAISE EXCEPTION 'M1TenantFoundation requires an explicit project-to-organization backfill before upgrade';
                END IF;
            END $$;
            """);

        migrationBuilder.DropForeignKey(name: "fk_project_members_projects_project_id", schema: "core", table: "project_members");
        migrationBuilder.DropForeignKey(name: "fk_project_settings_projects_project_id", schema: "core", table: "project_settings");
        migrationBuilder.DropIndex(name: "ux_projects_slug_active", schema: "core", table: "projects");
        migrationBuilder.DropIndex(name: "ix_project_members_project_role", schema: "core", table: "project_members");
        migrationBuilder.DropIndex(name: "ux_project_members_active", schema: "core", table: "project_members");
        migrationBuilder.DropColumn(name: "default_role", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "features", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "visibility", schema: "core", table: "project_settings");

        migrationBuilder.CreateTable(
            name: "organizations", schema: "core",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organizations", x => x.id);
                table.ForeignKey(name: "fk_organizations_users_created_by_user_id", column: x => x.created_by_user_id,
                    principalSchema: "identity", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "organization_members", schema: "core",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                joined_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                suspended_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                removed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_organization_members", x => x.id);
                table.UniqueConstraint("ak_organization_members_organization_id_user_id", x => new { x.organization_id, x.user_id });
                table.ForeignKey(name: "fk_organization_members_organizations_organization_id", column: x => x.organization_id,
                    principalSchema: "core", principalTable: "organizations", principalColumn: "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "fk_organization_members_users_changed_by_user_id", column: x => x.changed_by_user_id,
                    principalSchema: "identity", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey(name: "fk_organization_members_users_user_id", column: x => x.user_id,
                    principalSchema: "identity", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(name: "organization_id", schema: "core", table: "projects", type: "uuid", nullable: false);
        migrationBuilder.AddColumn<long>(name: "version", schema: "core", table: "projects", type: "bigint", nullable: false, defaultValue: 1L);
        migrationBuilder.AddColumn<Guid>(name: "organization_id", schema: "core", table: "project_members", type: "uuid", nullable: false);
        migrationBuilder.AddColumn<string>(name: "status", schema: "core", table: "project_members", type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "Active");
        migrationBuilder.AddColumn<DateTimeOffset>(name: "suspended_at", schema: "core", table: "project_members", type: "timestamptz", nullable: true);
        migrationBuilder.AddColumn<long>(name: "version", schema: "core", table: "project_members", type: "bigint", nullable: false, defaultValue: 1L);
        migrationBuilder.AddColumn<string>(name: "analysis_profile_key", schema: "core", table: "project_settings", type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "current-default-v1");
        migrationBuilder.AddColumn<bool>(name: "ai_advisory_enabled", schema: "core", table: "project_settings", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "default_asset_visibility", schema: "core", table: "project_settings", type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "private");
        migrationBuilder.AddColumn<int>(name: "notification_policy_version", schema: "core", table: "project_settings", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<long>(name: "version", schema: "core", table: "project_settings", type: "bigint", nullable: false, defaultValue: 1L);

        migrationBuilder.AddUniqueConstraint(name: "ak_projects_id_organization_id", schema: "core", table: "projects", columns: new[] { "id", "organization_id" });
        migrationBuilder.CreateIndex(name: "ux_organizations_slug", schema: "core", table: "organizations", column: "slug", unique: true);
        migrationBuilder.CreateIndex(name: "ix_organizations_status", schema: "core", table: "organizations", column: "status");
        migrationBuilder.CreateIndex(name: "ix_organizations_created_by_user_id", schema: "core", table: "organizations", column: "created_by_user_id");
        migrationBuilder.CreateIndex(name: "ix_organization_members_user_status", schema: "core", table: "organization_members", columns: new[] { "user_id", "status" });
        migrationBuilder.CreateIndex(name: "ix_organization_members_org_role_status", schema: "core", table: "organization_members", columns: new[] { "organization_id", "role", "status" });
        migrationBuilder.CreateIndex(name: "ix_organization_members_changed_by_user_id", schema: "core", table: "organization_members", column: "changed_by_user_id");
        migrationBuilder.CreateIndex(name: "ux_projects_org_slug_active", schema: "core", table: "projects", columns: new[] { "organization_id", "slug" }, unique: true, filter: "deleted_at IS NULL");
        migrationBuilder.CreateIndex(name: "ix_projects_org_status_created", schema: "core", table: "projects", columns: new[] { "organization_id", "status", "created_at" });
        migrationBuilder.CreateIndex(name: "ux_project_members_project_user", schema: "core", table: "project_members", columns: new[] { "project_id", "user_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "ix_project_members_org_user_status", schema: "core", table: "project_members", columns: new[] { "organization_id", "user_id", "status" });
        migrationBuilder.CreateIndex(name: "ix_project_members_project_role_status", schema: "core", table: "project_members", columns: new[] { "project_id", "role", "status" });
        migrationBuilder.CreateIndex(name: "ix_project_members_project_id_organization_id", schema: "core", table: "project_members", columns: new[] { "project_id", "organization_id" });

        migrationBuilder.AddForeignKey(name: "fk_projects_organizations_organization_id", schema: "core", table: "projects", column: "organization_id", principalSchema: "core", principalTable: "organizations", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "fk_project_settings_projects_project_id", schema: "core", table: "project_settings", column: "project_id", principalSchema: "core", principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "fk_project_members_projects_project_id_organization_id", schema: "core", table: "project_members",
            columns: new[] { "project_id", "organization_id" }, principalSchema: "core", principalTable: "projects", principalColumns: new[] { "id", "organization_id" }, onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "fk_project_members_organization_members_organization_id_user_id", schema: "core", table: "project_members",
            columns: new[] { "organization_id", "user_id" }, principalSchema: "core", principalTable: "organization_members", principalColumns: new[] { "organization_id", "user_id" }, onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "fk_project_members_organization_members_organization_id_user_id", schema: "core", table: "project_members");
        migrationBuilder.DropForeignKey(name: "fk_project_members_projects_project_id_organization_id", schema: "core", table: "project_members");
        migrationBuilder.DropForeignKey(name: "fk_projects_organizations_organization_id", schema: "core", table: "projects");
        migrationBuilder.DropForeignKey(name: "fk_project_settings_projects_project_id", schema: "core", table: "project_settings");
        migrationBuilder.DropIndex(name: "ux_projects_org_slug_active", schema: "core", table: "projects");
        migrationBuilder.DropIndex(name: "ix_projects_org_status_created", schema: "core", table: "projects");
        migrationBuilder.DropIndex(name: "ux_project_members_project_user", schema: "core", table: "project_members");
        migrationBuilder.DropIndex(name: "ix_project_members_org_user_status", schema: "core", table: "project_members");
        migrationBuilder.DropIndex(name: "ix_project_members_project_role_status", schema: "core", table: "project_members");
        migrationBuilder.DropIndex(name: "ix_project_members_project_id_organization_id", schema: "core", table: "project_members");
        migrationBuilder.DropUniqueConstraint(name: "ak_projects_id_organization_id", schema: "core", table: "projects");
        migrationBuilder.DropColumn(name: "organization_id", schema: "core", table: "projects");
        migrationBuilder.DropColumn(name: "version", schema: "core", table: "projects");
        migrationBuilder.DropColumn(name: "organization_id", schema: "core", table: "project_members");
        migrationBuilder.DropColumn(name: "status", schema: "core", table: "project_members");
        migrationBuilder.DropColumn(name: "suspended_at", schema: "core", table: "project_members");
        migrationBuilder.DropColumn(name: "version", schema: "core", table: "project_members");
        migrationBuilder.DropColumn(name: "analysis_profile_key", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "ai_advisory_enabled", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "default_asset_visibility", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "notification_policy_version", schema: "core", table: "project_settings");
        migrationBuilder.DropColumn(name: "version", schema: "core", table: "project_settings");
        migrationBuilder.DropTable(name: "organization_members", schema: "core");
        migrationBuilder.DropTable(name: "organizations", schema: "core");
        migrationBuilder.AddColumn<string>(name: "default_role", schema: "core", table: "project_settings", type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "viewer");
        migrationBuilder.AddColumn<string>(name: "features", schema: "core", table: "project_settings", type: "jsonb", nullable: true);
        migrationBuilder.AddColumn<string>(name: "visibility", schema: "core", table: "project_settings", type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "private");
        migrationBuilder.CreateIndex(name: "ux_projects_slug_active", schema: "core", table: "projects", column: "slug", unique: true, filter: "deleted_at IS NULL");
        migrationBuilder.CreateIndex(name: "ix_project_members_project_role", schema: "core", table: "project_members", columns: new[] { "project_id", "role" });
        migrationBuilder.CreateIndex(name: "ux_project_members_active", schema: "core", table: "project_members", columns: new[] { "project_id", "user_id" }, unique: true, filter: "removed_at IS NULL");
        migrationBuilder.AddForeignKey(name: "fk_project_members_projects_project_id", schema: "core", table: "project_members", column: "project_id", principalSchema: "core", principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(name: "fk_project_settings_projects_project_id", schema: "core", table: "project_settings", column: "project_id", principalSchema: "core", principalTable: "projects", principalColumn: "id", onDelete: ReferentialAction.Cascade);
    }
}
