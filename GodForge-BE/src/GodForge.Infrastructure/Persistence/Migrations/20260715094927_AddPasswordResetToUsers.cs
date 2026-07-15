using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GodForgeDbContext))]
    [Migration("20260715094927_AddPasswordResetToUsers")]
    public partial class AddPasswordResetToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password_reset_token_hash",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_token_expiry",
                schema: "identity",
                table: "users",
                type: "timestamptz",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_reset_token_hash",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_expiry",
                schema: "identity",
                table: "users");
        }
    }
}
