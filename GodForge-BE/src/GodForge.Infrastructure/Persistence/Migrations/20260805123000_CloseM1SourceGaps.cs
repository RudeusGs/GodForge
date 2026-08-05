using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260805123000_CloseM1SourceGaps")]
public partial class CloseM1SourceGaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "idempotency_records",
            schema: "ops",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                operation = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                resource_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_idempotency_records", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_idempotency_records_created",
            schema: "ops",
            table: "idempotency_records",
            column: "created_at");

        migrationBuilder.CreateIndex(
            name: "ux_idempotency_records_scope",
            schema: "ops",
            table: "idempotency_records",
            columns: new[] { "actor_user_id", "operation", "key" },
            unique: true);

        migrationBuilder.Sql("""
            WITH ranked AS (
                SELECT id,
                       row_number() OVER (
                           PARTITION BY normalized_email, purpose
                           ORDER BY updated_at DESC, created_at DESC, id DESC
                       ) AS row_number
                FROM identity.auth_challenges
                WHERE consumed_at IS NULL AND revoked_at IS NULL
            )
            UPDATE identity.auth_challenges AS challenge
            SET revoked_at = now(),
                updated_at = now(),
                concurrency_stamp = md5(random()::text || clock_timestamp()::text)
            FROM ranked
            WHERE challenge.id = ranked.id
              AND ranked.row_number > 1;
            """);

        migrationBuilder.CreateIndex(
            name: "ux_auth_challenges_active_scope",
            schema: "identity",
            table: "auth_challenges",
            columns: new[] { "normalized_email", "purpose" },
            unique: true,
            filter: "consumed_at IS NULL AND revoked_at IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_auth_challenges_active_scope",
            schema: "identity",
            table: "auth_challenges");

        migrationBuilder.DropTable(
            name: "idempotency_records",
            schema: "ops");
    }
}
