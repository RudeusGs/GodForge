using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

[DbContext(typeof(GodForgeDbContext))]
[Migration("20260806140000_HardenWorkerClaimsAndProjectLookup")]
public partial class HardenWorkerClaimsAndProjectLookup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_projects_org_upper_name_active
            ON core.projects (organization_id, upper(name))
            WHERE deleted_at IS NULL;
            """);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS analysis.ix_analysis_runs_job_id;
            """);

        migrationBuilder.Sql("""
            WITH ranked AS (
                SELECT id,
                       row_number() OVER (
                           PARTITION BY job_id
                           ORDER BY (status = 'completed') DESC,
                                    completed_at DESC NULLS LAST,
                                    started_at DESC,
                                    id DESC
                       ) AS row_number
                FROM analysis.analysis_runs
                WHERE job_id IS NOT NULL
            )
            UPDATE analysis.analysis_runs AS analysis_run
            SET job_id = NULL
            FROM ranked
            WHERE analysis_run.id = ranked.id
              AND ranked.row_number > 1;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX IF NOT EXISTS ux_analysis_runs_job
            ON analysis.analysis_runs (job_id)
            WHERE job_id IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS analysis.ux_analysis_runs_job;
            """);

        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS ix_analysis_runs_job_id
            ON analysis.analysis_runs (job_id);
            """);

        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS core.ux_projects_org_upper_name_active;
            """);
    }
}
