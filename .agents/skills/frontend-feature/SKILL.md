---
name: frontend-feature
description: Implement a Vue 3 product feature.
---

# Frontend Feature

## Use when

Implement a Vue 3 product feature.

## Required reading

- `docs/FRONTEND_ARCHITECTURE.md`
- `docs/SRS/09-ui-ux.md`
- Relevant functional/API/RBAC docs

## Workflow

1. Define route, actor and API contracts.
2. Add typed API models/client.
3. Implement feature components/composables/store only where needed.
4. Add loading, empty, error, degraded, stale and forbidden states.
5. Add pagination/virtualization and accessible controls.
6. Escape/sanitize repository/user content.
7. Add component/store/e2e tests and build checks.

## Mandatory checks

- Server state re-fetchable.
- UI does not assume authorization.
- Large graph/tree bounded.
- Job progress falls back to REST.
- AI is visibly labeled.

## Forbidden

- No `any` for convenience in domain contracts.
- No token/source content in logs.
- No unbounded render or raw HTML from repository.

## Completion output

Report route/components/API/state/tests/accessibility and known browser/performance limits.
