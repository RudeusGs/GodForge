using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GodForge.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(GodForgeDbContext))]
[Migration("20260715113000_AllowRepeatedAnalysisForSnapshot")]
public partial class AllowRepeatedAnalysisForSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_health_reports_snapshot",
            schema: "analysis",
            table: "health_reports");

        migrationBuilder.CreateIndex(
            name: "ix_health_reports_snapshot",
            schema: "analysis",
            table: "health_reports",
            column: "snapshot_id");

        migrationBuilder.CreateIndex(
            name: "ux_health_reports_analysis_run",
            schema: "analysis",
            table: "health_reports",
            column: "analysis_run_id",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_health_reports_analysis_run",
            schema: "analysis",
            table: "health_reports");

        migrationBuilder.DropIndex(
            name: "ix_health_reports_snapshot",
            schema: "analysis",
            table: "health_reports");

        migrationBuilder.CreateIndex(
            name: "ux_health_reports_snapshot",
            schema: "analysis",
            table: "health_reports",
            column: "snapshot_id",
            unique: true);
    }
}
