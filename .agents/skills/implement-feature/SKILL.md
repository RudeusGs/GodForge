---
name: implement-feature
description: Implement an approved end-to-end GodForge feature.
---

# Implement Feature

## Use when

Implement an approved end-to-end GodForge feature.

## Required reading

- `.agents/AGENTS.md`
- Relevant functional SRS
- Database/API/security/NFR/workflow/test docs
- `docs/DEFINITION_OF_READY.md`

## Workflow

1. Map requirement IDs and confirm readiness.
2. Inspect current code and tests; do not assume docs equal implementation.
3. Implement smallest complete vertical slice across required layers.
4. Add authorization, validation, errors, activity/audit and observability.
5. Add migrations/jobs/providers only when documented.
6. Add tests and synchronize documentation.
7. Run quality gates.

## Mandatory checks

- Clean Architecture dependencies.
- Tenant scope and threat controls.
- Async/idempotency behavior.
- Loading/error/degraded UI states when frontend is included.
- Definition of Done.

## Forbidden

- No placeholder success paths.
- No broad unrelated refactor.
- No unverified production-ready claim.

## Completion output

Report requirement IDs, files, behavior, tests/commands, security decisions and remaining limitations.
