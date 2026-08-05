---
name: docs-sync
description: Synchronize documentation after an approved behavior or architecture change.
---

# Docs Sync

## Use when

Synchronize documentation after an approved behavior or architecture change.

## Required reading

- `docs/README.md`
- `docs/SRS/README.md`
- `docs/SRS/10-traceability.md`
- `docs/IMPLEMENTATION_STATUS.md`

## Workflow

1. Identify affected product, functional, API, data, security, workflow, tests and operations docs.
2. Preserve stable IDs.
3. Update ADR when foundational.
4. Update traceability and error/RBAC catalogs.
5. Update implementation status only with evidence.
6. Run local Markdown link/consistency check.

## Mandatory checks

- No contradictory Current/Target statements.
- Routes/table/permission names consistent.
- Deferred scope remains explicit.
- No false compliance or production claim.

## Forbidden

- Do not update one isolated document only when behavior spans multiple contracts.
- Do not renumber requirements.
- Do not copy secrets into docs.

## Completion output

List docs changed, behavioral contract, unresolved conflict and implementation-status evidence.
