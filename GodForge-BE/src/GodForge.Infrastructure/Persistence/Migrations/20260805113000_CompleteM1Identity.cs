using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260805113000_CompleteM1Identity")]
public partial class CompleteM1Identity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "version",
            schema: "identity",
            table: "users",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);

        migrationBuilder.AddColumn<string>(
            name: "concurrency_stamp",
            schema: "identity",
            table: "user_sessions",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "legacy");

        migrationBuilder.Sql("DELETE FROM identity.refresh_tokens;");
        migrationBuilder.DropIndex(name: "ix_refresh_tokens_user_expires", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "device_name", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "ip_address", schema: "identity", table: "refresh_tokens");
        migrationBuilder.AddColumn<Guid>(
            name: "session_id",
            schema: "identity",
            table: "refresh_tokens",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<Guid>(
            name: "family_id",
            schema: "identity",
            table: "refresh_tokens",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<string>(
            name: "revoked_reason",
            schema: "identity",
            table: "refresh_tokens",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "concurrency_stamp",
            schema: "identity",
            table: "refresh_tokens",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "legacy");
        migrationBuilder.Sql("ALTER TABLE identity.refresh_tokens ALTER COLUMN session_id DROP DEFAULT;");
        migrationBuilder.Sql("ALTER TABLE identity.refresh_tokens ALTER COLUMN family_id DROP DEFAULT;");
        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_family",
            schema: "identity",
            table: "refresh_tokens",
            column: "family_id");
        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_session_expires",
            schema: "identity",
            table: "refresh_tokens",
            columns: new[] { "session_id", "expires_at" },
            filter: "revoked_at IS NULL");
        migrationBuilder.AddForeignKey(
            name: "fk_refresh_tokens_user_sessions_session_id",
            schema: "identity",
            table: "refresh_tokens",
            column: "session_id",
            principalSchema: "identity",
            principalTable: "user_sessions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.Sql("DELETE FROM identity.user_invites;");
        migrationBuilder.DropIndex(name: "ix_user_invites_email_status", schema: "identity", table: "user_invites");
        migrationBuilder.AlterColumn<string>(
            name: "email",
            schema: "identity",
            table: "user_invites",
            type: "character varying(320)",
            maxLength: 320,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);
        migrationBuilder.AddColumn<Guid>(
            name: "organization_id",
            schema: "identity",
            table: "user_invites",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<string>(
            name: "normalized_email",
            schema: "identity",
            table: "user_invites",
            type: "character varying(320)",
            maxLength: 320,
            nullable: false,
            defaultValue: string.Empty);
        migrationBuilder.AddColumn<string>(
            name: "role",
            schema: "identity",
            table: "user_invites",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "OrganizationMember");
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "accepted_at",
            schema: "identity",
            table: "user_invites",
            type: "timestamptz",
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "revoked_at",
            schema: "identity",
            table: "user_invites",
            type: "timestamptz",
            nullable: true);
        migrationBuilder.AddColumn<long>(
            name: "version",
            schema: "identity",
            table: "user_invites",
            type: "bigint",
            nullable: false,
            defaultValue: 1L);
        migrationBuilder.AddColumn<string>(
            name: "concurrency_stamp",
            schema: "identity",
            table: "user_invites",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "legacy");
        migrationBuilder.Sql("ALTER TABLE identity.user_invites ALTER COLUMN organization_id DROP DEFAULT;");
        migrationBuilder.CreateIndex(
            name: "ix_user_invites_org_email_status",
            schema: "identity",
            table: "user_invites",
            columns: new[] { "organization_id", "normalized_email", "status" });
        migrationBuilder.CreateIndex(
            name: "ux_user_invites_active_org_email",
            schema: "identity",
            table: "user_invites",
            columns: new[] { "organization_id", "normalized_email" },
            unique: true,
            filter: "status = 'Pending'");
        migrationBuilder.AddForeignKey(
            name: "fk_user_invites_organizations_organization_id",
            schema: "identity",
            table: "user_invites",
            column: "organization_id",
            principalSchema: "core",
            principalTable: "organizations",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "auth_challenges",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                purpose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                secret_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                failed_attempts = table.Column<int>(type: "integer", nullable: false),
                max_attempts = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                resend_available_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                consumed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                concurrency_stamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_auth_challenges", x => x.id));

        migrationBuilder.CreateIndex(
            name: "ix_auth_challenges_lookup",
            schema: "identity",
            table: "auth_challenges",
            columns: new[] { "normalized_email", "purpose", "expires_at" });
        migrationBuilder.CreateIndex(
            name: "ix_auth_challenges_secret_hash",
            schema: "identity",
            table: "auth_challenges",
            column: "secret_hash");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "auth_challenges", schema: "identity");

        migrationBuilder.DropForeignKey(name: "fk_user_invites_organizations_organization_id", schema: "identity", table: "user_invites");
        migrationBuilder.DropIndex(name: "ix_user_invites_org_email_status", schema: "identity", table: "user_invites");
        migrationBuilder.DropIndex(name: "ux_user_invites_active_org_email", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "organization_id", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "normalized_email", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "role", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "accepted_at", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "revoked_at", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "version", schema: "identity", table: "user_invites");
        migrationBuilder.DropColumn(name: "concurrency_stamp", schema: "identity", table: "user_invites");
        migrationBuilder.AlterColumn<string>(
            name: "email",
            schema: "identity",
            table: "user_invites",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(320)",
            oldMaxLength: 320);
        migrationBuilder.CreateIndex(
            name: "ix_user_invites_email_status",
            schema: "identity",
            table: "user_invites",
            columns: new[] { "email", "status" });

        migrationBuilder.DropForeignKey(name: "fk_refresh_tokens_user_sessions_session_id", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropIndex(name: "ix_refresh_tokens_family", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropIndex(name: "ix_refresh_tokens_session_expires", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "session_id", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "family_id", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "revoked_reason", schema: "identity", table: "refresh_tokens");
        migrationBuilder.DropColumn(name: "concurrency_stamp", schema: "identity", table: "refresh_tokens");
        migrationBuilder.AddColumn<string>(
            name: "device_name",
            schema: "identity",
            table: "refresh_tokens",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ip_address",
            schema: "identity",
            table: "refresh_tokens",
            type: "character varying(45)",
            maxLength: 45,
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_expires",
            schema: "identity",
            table: "refresh_tokens",
            columns: new[] { "user_id", "expires_at" },
            filter: "revoked_at IS NULL");

        migrationBuilder.DropColumn(name: "concurrency_stamp", schema: "identity", table: "user_sessions");
        migrationBuilder.DropColumn(name: "version", schema: "identity", table: "users");
    }
}
