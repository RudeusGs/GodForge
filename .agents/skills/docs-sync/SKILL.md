---
name: docs-sync
description: Skill for keeping documentation synchronized with code changes.
---

# Documentation Sync

## When to Use
Use this skill whenever architectural, API, database, or security behavior changes in the codebase.

## Required Reading
- The modified source code.
- `docs/DEFINITION_OF_READY.md`

## Workflow
1. Identify the impacted subsystems (e.g., API, DB schema).
2. Open the corresponding `docs/SRS/*` or `docs/*` files.
3. Update the documentation to reflect the new reality.
4. Ensure no contradictions exist with other docs.

## Mandatory Checks
- If an API changes, `05-api.md` and `OPENAPI_CONVENTIONS.md` must be updated.
- If DB changes, `04-database.md` must be updated.

## Forbidden Actions
- Do not delete useful historical SRS context unless explicitly deprecated.
- Do not introduce placeholder documentation (e.g. "TBD").

## Completion Checklist
- [ ] Impacted docs identified.
- [ ] Content explicitly updated.
- [ ] Terminology remains consistent.
