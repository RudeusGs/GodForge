using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHostedRepositoryLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "access_error_code",
                schema: "repo",
                table: "repositories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "access_reconciled_at",
                schema: "repo",
                table: "repositories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "access_requested_at",
                schema: "repo",
                table: "repositories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hosted_name",
                schema: "repo",
                table: "repositories",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provision_identity",
                schema: "repo",
                table: "repositories",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "forgejo_identities",
                schema: "repo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_user_id = table.Column<long>(type: "bigint", nullable: false),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_forgejo_identities", x => x.id);
                    table.ForeignKey(
                        name: "fk_forgejo_identities_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_repositories_hosted_identity",
                schema: "repo",
                table: "repositories",
                columns: new[] { "provider", "hosted_repository_id" },
                unique: true,
                filter: "hosted_repository_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_forgejo_identities_provider_user_id",
                schema: "repo",
                table: "forgejo_identities",
                column: "provider_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_forgejo_identities_user_id",
                schema: "repo",
                table: "forgejo_identities",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "forgejo_identities",
                schema: "repo");

            migrationBuilder.DropIndex(
                name: "ux_repositories_hosted_identity",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "access_error_code",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "access_reconciled_at",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "access_requested_at",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "hosted_name",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "provision_identity",
                schema: "repo",
                table: "repositories");
        }
    }
}
