---
name: frontend-feature
description: Skill for implementing frontend features in the Vue 3 GodForge-FE application.
---

# Frontend Feature Implementation

## When to Use
Use this skill when building UI components, pages, forms, or routing logic in `GodForge-FE`.

## Required Reading
- `docs/FRONTEND_ARCHITECTURE.md`
- `docs/SRS/09-ui-ux.md`
- `docs/RBAC_MATRIX.md`

## Workflow
1. Identify the required Vue components and Pinia state.
2. Build UI using Composition API.
3. Connect Axios client to backend API.
4. Implement Loading, Empty, and Error states.
5. Add route guards if RBAC is required.

## Mandatory Checks
- Centralized Axios client MUST attach authorization tokens.
- Server-side pagination MUST be supported for lists.
- Proper Skeleton Loaders or spinners must be used.

## Forbidden Actions
- Do not hardcode backend URLs.
- Do not rely solely on the UI for security (backend is the source of truth).
- Do not alert raw errors to the user.

## Completion Checklist
- [ ] Components use Composition API.
- [ ] Loading/Error states implemented.
- [ ] Tests written for composables.
- [ ] No secrets exposed in code.
