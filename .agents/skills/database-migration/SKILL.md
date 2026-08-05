---
name: database-migration
description: Change EF Core model, PostgreSQL schema, indexes or backfill.
---

# Database Migration

## Use when

Change EF Core model, PostgreSQL schema, indexes or backfill.

## Required reading

- `docs/SRS/04-database.md`
- `docs/DATABASE_CHANGE_CHECKLIST.md`
- `docs/SRS/14-data-retention.md`
- Relevant ADR/functional SRS

## Workflow

1. Identify ownership, data class, retention and query patterns.
2. Add domain/entity and Infrastructure configuration without framework leakage.
3. Add constraints, indexes and deterministic identifiers.
4. Generate forward migration.
5. Define/backfill data safely when needed.
6. Test clean migration and upgrade from prior schema.
7. Update database and implementation docs.

## Mandatory checks

- Tenant scope/index.
- Delete behavior and soft-delete policy.
- Unique/idempotency constraints.
- No large binary/source storage.
- Migration SQL review and rollback/forward-fix plan.

## Forbidden

- Do not delete applied migrations.
- Do not reset production database.
- Do not hide schema in unbounded JSONB.
- Do not use raw interpolated SQL.

## Completion output

Report schema change, migration name, indexes/constraints, data risk and test commands.
