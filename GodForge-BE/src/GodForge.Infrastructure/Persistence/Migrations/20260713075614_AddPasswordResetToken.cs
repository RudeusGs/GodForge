using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_token_expiry",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token_hash",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_reset_token_expiry",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_hash",
                schema: "identity",
                table: "users");
        }
    }
}
