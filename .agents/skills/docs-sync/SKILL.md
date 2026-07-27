---
name: docs-sync
description: Skill for keeping documentation synchronized with code changes.
---

# Documentation Sync

## When to Use
Use this skill whenever architectural, API, database, or security behavior changes in the codebase.

## Required Reading
- `docs/SRS/README.md`
- The relevant functional SRS file.
- `docs/SRS/10-traceability.md`
- `docs/SRS/11-testing-acceptance.md`
- `docs/SRS/04-database.md` if DB changes.
- `docs/SRS/05-api.md` if API changes.
- `docs/SRS/06-security.md` if auth/RBAC/security changes.
- `docs/SRS/12-worker-processing.md` if jobs/workers change.
- `docs/ERROR_CODES.md` if errors change.
- `docs/RBAC_MATRIX.md` if permissions change.

## Workflow
1. Identify the impacted subsystems (e.g., API, DB schema).
2. Open the corresponding `docs/SRS/*` or `docs/*` files.
3. Update the documentation to reflect the new reality.
4. Ensure no contradictions exist with other docs.

## Mandatory Checks
- If an API changes, `05-api.md` and `OPENAPI_CONVENTIONS.md` must be updated.
- If DB changes, `04-database.md` must be updated.
- If errors change, `ERROR_CODES.md` must be updated.
- If permissions change, `RBAC_MATRIX.md` must be updated.

## Forbidden Actions
- Do not delete useful historical SRS context unless explicitly deprecated.
- Do not introduce placeholder documentation (e.g. "TBD").
- Do not weaken existing SRS constraints.

## Completion Checklist


## Output Expectations
The agent must declare what files were updated and why they were updated to keep them in sync with codebase changes.
