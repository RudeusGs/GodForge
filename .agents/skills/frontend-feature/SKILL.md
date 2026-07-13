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
2. Build UI using Composition API and TypeScript strict mode.
3. Connect Axios client to backend API.
4. Implement Loading, Empty, and Error states.
5. Add route guards if RBAC is required.

## Mandatory Checks
- Vue 3 Composition API must be used.
- TypeScript strict mode must be followed.
- Pinia store boundaries must be respected.
- Centralized Axios client MUST attach authorization tokens.
- API envelope handling must be consistent.
- Auth token refresh logic must be handled.
- 401/403/404 behavior must be correctly mapped to UX.
- Server-side pagination MUST be supported for lists.
- Proper Skeleton Loaders or spinners must be used.
- SignalR subscribe/unsubscribe must be managed correctly.
- RBAC UI visibility must be properly applied.
- Basic accessibility standards must be implemented.
- TailwindCSS must be used for styling; avoid Vanilla CSS.
- Bootstrap Icons must be used for all icons.

## Forbidden Actions
- Do not hardcode backend URLs.
- Do not rely solely on the UI for security (backend is the strict security boundary).
- Do not expose any secrets in the frontend.
- Do not alert raw errors to the user.

## Completion Checklist
- [ ] Components use Composition API.
- [ ] TypeScript strict mode applied.
- [ ] Loading/Error/Empty states implemented.
- [ ] Route guards and RBAC visibility correct.
- [ ] Tests written for composables with Vitest.
- [ ] No secrets exposed in code.
- [ ] Backend security boundary respected.

## Output Expectations
The agent must explain the Vue components and Pinia stores created, the error/loading states implemented, and confirm no secrets are exposed.
