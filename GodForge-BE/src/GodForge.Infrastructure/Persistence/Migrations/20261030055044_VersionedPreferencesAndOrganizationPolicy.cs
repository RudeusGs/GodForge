using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedPreferencesAndOrganizationPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "identity",
                table: "user_settings",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.CreateTable(
                name: "organization_settings",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ai_advisory_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_settings", x => x.id);
                    table.CheckConstraint("ck_organization_settings_version", "version > 0");
                    table.ForeignKey(
                        name: "fk_organization_settings_organizations_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "core",
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_settings_version",
                schema: "identity",
                table: "user_settings",
                sql: "version > 0");

            migrationBuilder.CreateIndex(
                name: "ix_organization_settings_organization_id",
                schema: "core",
                table: "organization_settings",
                column: "organization_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_settings",
                schema: "core");

            migrationBuilder.DropCheckConstraint(
                name: "ck_user_settings_version",
                schema: "identity",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "identity",
                table: "user_settings");
        }
    }
}
