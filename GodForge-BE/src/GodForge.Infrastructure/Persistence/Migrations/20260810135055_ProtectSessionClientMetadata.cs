using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProtectSessionClientMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip_address",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.AddColumn<string>(
                name: "ip_hash",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent_hash",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ip_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "user_agent_hash",
                schema: "identity",
                table: "user_sessions");

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                schema: "identity",
                table: "user_sessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
