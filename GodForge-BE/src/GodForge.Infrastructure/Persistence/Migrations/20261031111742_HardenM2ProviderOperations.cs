using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenM2ProviderOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "access_confirmed_generation",
                schema: "repo",
                table: "repositories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "access_generation",
                schema: "repo",
                table: "repositories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "webhook_confirmed_at",
                schema: "repo",
                table: "repositories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "webhook_id",
                schema: "repo",
                table: "repositories",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "access_confirmed_generation",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "access_generation",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "webhook_confirmed_at",
                schema: "repo",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "webhook_id",
                schema: "repo",
                table: "repositories");
        }
    }
}
