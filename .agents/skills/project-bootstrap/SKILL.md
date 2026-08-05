---
name: project-bootstrap
description: Prepare a clean GodForge development environment.
---

# Project Bootstrap

## Use when

Prepare a clean GodForge development environment.

## Required reading

- `docs/LOCAL_DEVELOPMENT.md`
- `docs/ENVIRONMENT.md`
- `docs/SETUP_CHECKLIST.md`

## Workflow

1. Verify SDK/runtime/tool prerequisites.
2. Copy environment template without committing secrets.
3. Start required Compose services.
4. Restore/build backend and frontend.
5. Apply/test database initialization.
6. Run health checks and smoke tests.
7. Document platform-specific issues.

## Mandatory checks

- Example secrets are local only.
- Forgejo/Gemini remain disabled unless needed/configured.
- Workspaces use safe local path.
- No company/private repository used as test data.

## Forbidden

- Do not commit `.env`.
- Do not weaken security globally to fix a local setup problem.
- Do not call environment ready if build/health fails.

## Completion output

Report versions, services, commands, health result and remaining setup issue.
