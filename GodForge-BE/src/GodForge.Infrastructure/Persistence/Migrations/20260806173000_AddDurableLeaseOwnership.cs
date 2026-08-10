using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260806173000_AddDurableLeaseOwnership")]
public partial class AddDurableLeaseOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "claim_token",
            schema: "ops",
            table: "jobs",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "lease_id",
            schema: "ops",
            table: "outbox_messages",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "lease_expires_at",
            schema: "ops",
            table: "outbox_messages",
            type: "timestamptz",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE ops.outbox_messages
            SET status = 'failed',
                available_at = LEAST(available_at, CURRENT_TIMESTAMP),
                error_message = COALESCE(error_message, 'Dispatcher lease was reset during deployment.')
            WHERE status = 'processing';
            """);

        migrationBuilder.CreateIndex(
            name: "ix_outbox_status_lease_expires",
            schema: "ops",
            table: "outbox_messages",
            columns: new[] { "status", "lease_expires_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_outbox_status_lease_expires",
            schema: "ops",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "claim_token",
            schema: "ops",
            table: "jobs");

        migrationBuilder.DropColumn(
            name: "lease_expires_at",
            schema: "ops",
            table: "outbox_messages");

        migrationBuilder.DropColumn(
            name: "lease_id",
            schema: "ops",
            table: "outbox_messages");
    }
}
