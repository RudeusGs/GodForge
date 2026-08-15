using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260814133000_AddTrigramSearchIndexes")]
public partial class AddTrigramSearchIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_projects_name_trgm_active
            ON core.projects USING gin (name gin_trgm_ops)
            WHERE deleted_at IS NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_projects_description_trgm_active
            ON core.projects USING gin (description gin_trgm_ops)
            WHERE deleted_at IS NULL;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_users_normalized_email_trgm
            ON identity.users USING gin (normalized_email gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_users_display_name_trgm
            ON identity.users USING gin (display_name gin_trgm_ops);
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_user_invites_normalized_email_trgm
            ON identity.user_invites USING gin (normalized_email gin_trgm_ops);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS core.ix_projects_name_trgm_active;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS core.ix_projects_description_trgm_active;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS identity.ix_users_normalized_email_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS identity.ix_users_display_name_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS identity.ix_user_invites_normalized_email_trgm;");
    }
}
