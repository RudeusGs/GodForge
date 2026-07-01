---
name: database-migration
description: Skill for creating and managing Entity Framework Core migrations for the GodForge ASP.NET Core backend using PostgreSQL. Use when adding or changing database tables, columns, constraints, indexes, schemas, EF Core entity configurations, migration files, seed data, soft-delete behavior, metadata tables, job state tables, audit/activity tables, or any database change that must follow the GodForge SRS data model, schema versioning, security, and migration conventions.
---

# Database Migration

Use this skill when creating, reviewing, or updating Entity Framework Core database migrations for the GodForge backend.

## Core Principles

- Treat the SRS as the source of truth before changing schema.
- Keep core business data and generated metadata separate by schema or clearly separated namespace.
- Use PostgreSQL naming in `snake_case` for schemas, tables, columns, indexes, constraints, and foreign keys.
- Use EF Core migrations only. Do not use `EnsureCreated()`.
- Do not edit a migration that has already been applied to any shared database. Create a follow-up migration instead.
- Do not store secrets in plain text. Store only secret references such as `credential_ref`, or encrypted values if the architecture explicitly supports them.
- Prefer additive, backward-compatible schema changes when API, worker, and frontend deployments may not happen at the same time.

## 1. Read the SRS and Identify the Data Domain

Before writing code, inspect the relevant SRS section and confirm:

- Entity/table name and owning module.
- Whether the data belongs to the core schema or metadata schema.
- Columns, PostgreSQL types, nullability, defaults, unique constraints, indexes, and foreign keys.
- Lifecycle/state machine fields such as project state, repository state, job state, and soft-delete fields.
- Audit/activity requirements for write operations.
- Retention, security, and backup implications.

Use this default domain split unless the repository already defines a different convention:

| Domain | Suggested schema | Examples |
| --- | --- | --- |
| Core business data | `core` or default `public` | `users`, `roles`, `projects`, `project_members`, `repositories`, `branches`, `activities`, `notifications`, `jobs` |
| Metadata data | `metadata` | `metadata_versions`, `scenes`, `scene_nodes`, `assets`, `resources`, `scripts`, `dependencies`, `statistics`, `health_reports`, `health_issues`, search/materialized metadata tables |

## 2. Design the Entity and Configuration

Place EF Core configurations in:

```text
src/GodForge.Infrastructure/Persistence/Configurations/
```

Use `IEntityTypeConfiguration<TEntity>` for every persisted aggregate/entity. Keep mapping concerns out of the domain model where possible.

Example:

```csharp
namespace GodForge.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", "core");

        builder.HasKey(project => project.Id)
            .HasName("pk_projects");

        builder.Property(project => project.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(project => project.Visibility)
            .HasColumnName("visibility")
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(project => project.State)
            .HasColumnName("state")
            .HasColumnType("varchar(32)")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(project => project.DefaultBranch)
            .HasColumnName("default_branch")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255);

        builder.Property(project => project.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(project => project.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(project => project.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(project => project.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(project => new { project.CreatedBy, project.Name })
            .IsUnique()
            .HasDatabaseName("ux_projects_created_by_name");

        builder.HasIndex(project => project.State)
            .HasDatabaseName("ix_projects_state");

        builder.HasIndex(project => project.DeletedAt)
            .HasDatabaseName("ix_projects_deleted_at");

        builder.HasQueryFilter(project => project.DeletedAt == null);
    }
}
```

## 3. Type Mapping Rules

Use explicit PostgreSQL column types for important columns.

| C# type | PostgreSQL type | Notes |
| --- | --- | --- |
| `Guid` | `uuid` | Use `gen_random_uuid()` when DB-generated IDs are expected. |
| `string` | `varchar(n)` or `text` | Use bounded `varchar(n)` for codes, names, states, slugs, and refs. Use `text` for long descriptions/messages. |
| `DateTimeOffset` | `timestamptz` | Use UTC at application boundaries. |
| `int` | `integer` | Use for counts, progress, scores when range is small. |
| `long` | `bigint` | Use for file sizes and large counters. |
| `bool` | `boolean` | Prefer explicit defaults for flags. |
| `decimal` | `numeric(p,s)` | Specify precision. |
| `Dictionary`, object payloads, summaries | `jsonb` | Use for flexible metadata such as `properties_json`, `metrics_json`, `summary_json`, `permissions`. |
| enum/state value | `varchar(32)` or `varchar(64)` | Store controlled strings unless the project standard uses PostgreSQL enum types. |

## 4. Index and Constraint Strategy

Create indexes only for documented access patterns or high-value queries.

Default index rules:

- Index all foreign keys used for joins.
- Index project-scoped lookup columns: `(project_id, path)`, `(project_id, state)`, `(project_id, created_at)`, `(project_id, revision)`.
- Use unique constraints for natural uniqueness inside a scope, such as project member uniqueness or one repository per project if required.
- Use partial indexes for soft-delete uniqueness when the application allows reusing a value after delete.
- Use GIN indexes for `jsonb` only when real query predicates require them.
- Avoid over-indexing large metadata write tables because parser/analyzer workers may write many rows.

Recommended naming:

```text
pk_<table>
fk_<from_table>_<to_table>_<column>
ix_<table>_<column_or_purpose>
ux_<table>_<column_or_scope>
ck_<table>_<rule>
```

Example partial unique index:

```csharp
builder.HasIndex(project => new { project.CreatedBy, project.Name })
    .IsUnique()
    .HasFilter("deleted_at IS NULL")
    .HasDatabaseName("ux_projects_created_by_name_active");
```

## 5. Relationship and Delete Behavior Rules

Use explicit delete behavior. Do not rely on EF Core defaults.

- Owned/dependent records that cannot exist without the parent may use `OnDelete(DeleteBehavior.Cascade)`.
- Reference records that should preserve history should use `OnDelete(DeleteBehavior.SetNull)` or `Restrict` depending on business rules.
- Activity/audit records should generally not cascade-delete with normal user/project operations unless the retention policy explicitly requires physical cleanup.
- Metadata rows can usually cascade from `metadata_versions` or `projects` because they are derived from repository state and can be regenerated.
- Soft-deleted entities should use `deleted_at` and a global query filter where appropriate.

Example:

```csharp
builder.HasOne(member => member.Project)
    .WithMany(project => project.Members)
    .HasForeignKey(member => member.ProjectId)
    .OnDelete(DeleteBehavior.Cascade)
    .HasConstraintName("fk_project_members_projects_project_id");

builder.HasOne(activity => activity.Actor)
    .WithMany()
    .HasForeignKey(activity => activity.ActorId)
    .OnDelete(DeleteBehavior.SetNull)
    .HasConstraintName("fk_activities_users_actor_id");
```

## 6. State, Versioning, and Async Job Tables

When adding stateful entities, use controlled state values and make them compatible with API/worker flows.

Important examples:

- `projects.state`: `draft`, `repository_connected`, `analyzing`, `active`, `archived`, `deleted`.
- `repositories.state`: `unconfigured`, `configured`, `cloning`, `ready`, `error`, or the states already used in the codebase.
- `jobs.state`: `queued`, `running`, `succeeded`, `failed`, `cancelled`, `dead_lettered`.
- Metadata outputs should include `metadata_schema_version` or equivalent versioning when parser output or graph model changes.
- Health reports should store `rule_set_version` or `rule_version` so scores can be explained later.
- Event/outbox payloads, if implemented, should store `event_schema_version`.

For worker-generated data, include lineage columns when relevant:

```text
project_id
repository_id
branch_or_revision
commit_hash
job_id
metadata_version_id
correlation_id
created_at/generated_at
```

## 7. Create the Migration

Run EF Core commands from the backend repository root:

```bash
cd GodForge-BE
dotnet ef migrations add <MigrationName> --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

Use PascalCase names that describe the change:

```text
AddProjectsTable
AddProjectMembersTable
AddRepositoryStateColumns
AddMetadataVersionsTable
AddHealthReportsTables
AddSearchIndexes
```

If the repository uses multiple `DbContext` classes, specify the context explicitly:

```bash
dotnet ef migrations add <MigrationName> \
  --context GodForgeDbContext \
  --project src/GodForge.Infrastructure \
  --startup-project src/GodForge.Api
```

## 8. Review the Generated Migration

Before applying the migration, inspect the generated file carefully.

Checklist:

- `Up` and `Down` methods both exist and are reversible where practical.
- Table/schema names are `snake_case`.
- Column types, nullability, defaults, and max lengths match the SRS and entity configuration.
- Primary keys, foreign keys, unique constraints, and check constraints have explicit names.
- Index names follow the naming convention.
- Delete behavior matches ownership and retention rules.
- Large metadata tables are not over-indexed.
- Sensitive fields do not store raw credentials/tokens.
- New state/version columns have safe defaults when added to existing tables.
- Data backfill SQL is idempotent and safe to run once during deployment.

For destructive changes:

- Prefer a two-step migration: add new column/table, backfill, deploy code, then remove old column/table in a later migration.
- Never drop or rename production columns without a rollback/backfill plan.
- For renames, prefer explicit `RenameColumn`/`RenameTable` instead of drop/create when preserving data matters.

## 9. Apply and Validate the Migration

Apply locally or to the target environment according to deployment rules:

```bash
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

Then validate:

```bash
dotnet ef migrations list --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

Also verify through PostgreSQL when possible:

```sql
select table_schema, table_name
from information_schema.tables
where table_schema in ('core', 'metadata', 'public')
order by table_schema, table_name;
```

## 10. Rollback Guidance

For local development, rollback to a previous migration when needed:

```bash
dotnet ef database update <PreviousMigrationName> --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

For shared environments:

- Do not casually rollback if newer application code depends on the schema.
- Prefer a forward-fix migration unless the deployment plan explicitly supports rollback.
- Check worker compatibility because queued jobs may still reference older schema or payload versions.

## 11. Final Review Checklist

- [ ] SRS data model and relevant workflow/state machine were checked.
- [ ] Core data and metadata data are separated appropriately.
- [ ] Names are `snake_case` in PostgreSQL.
- [ ] EF configuration is explicit and placed in the infrastructure persistence configuration folder.
- [ ] Types, nullability, defaults, constraints, and indexes are intentional.
- [ ] Foreign key delete behavior is explicit.
- [ ] Soft-delete uses `deleted_at` and query filters where appropriate.
- [ ] Sensitive data is not stored in plain text.
- [ ] Activity/audit and job lineage fields are preserved where required.
- [ ] Metadata/versioned outputs include schema/rule/payload version fields when needed.
- [ ] Migration was reviewed before applying.
- [ ] Migration can be applied from a clean database and from the previous migration.
- [ ] Tests or smoke checks were run after applying the migration.