---
name: requirements-planning
description: Plan or refine a GodForge feature before code is written.
---

# Requirements Planning

## Use when

Plan or refine a GodForge feature before code is written.

## Required reading

- `docs/DEFINITION_OF_READY.md`
- `docs/SRS/README.md`
- `docs/SRS/01-scope.md`
- `docs/SRS/10-traceability.md`

## Workflow

1. Identify product problem, actors and scope classification.
2. Assign or reuse stable FR/NFR/SEC IDs.
3. Define main/alternate/error flows and acceptance criteria.
4. Map API, data, RBAC, async, security, observability and tests.
5. Update traceability and milestone placement.
6. Stop before code until Definition of Ready passes.

## Mandatory checks

- No duplicate requirement IDs.
- Current implementation and target design are distinguished.
- Requirement is measurable and does not rely on vague “enterprise-grade” claims.

## Forbidden

- Do not invent a feature that conflicts with product scope.
- Do not implement code during requirements planning.
- Do not mark acceptance criteria with subjective words only.

## Completion output

List updated documents, final scope, unresolved decisions and readiness result.
