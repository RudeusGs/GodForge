# Setup Checklist

This checklist guarantees the project foundation is solid before any functional feature code is written.

## Prerequisites

- [ ] Node.js, .NET 9 SDK, and Docker are installed locally. (Proved by `node -v`, `dotnet --version`, `docker --version` returning valid versions; blocked if failing)
- [ ] `docs/ADR/` contains foundational architecture decisions. (Proved by `ls docs/ADR/` having files; blocked if empty)
- [ ] `docs/MILESTONES.md` outlines the rollout plan. (Proved by file existence; blocked if missing)
- [ ] `docs/DEFINITION_OF_READY.md` is strictly enforced. (Proved by team adherence)

## Foundational Root Files

- [ ] `.editorconfig` exists and defines standard formatting rules. (Proved by file existence; blocked if missing)
- [ ] `global.json` pins the correct .NET SDK version. (Proved by file existence; blocked if missing)
- [ ] `Directory.Build.props` enforces compiler flags (e.g., nullable reference types, warnings as errors). (Proved by file existence; blocked if missing)
- [x] `.env.example` defines all necessary local environment variables without containing real secrets. (Proved by file inspection; blocked if missing or contains real secrets)
- [ ] `docker-compose.yml` supports spinning up PostgreSQL, Redis, RabbitMQ, and MinIO locally. (Proved by `docker-compose up -d` success; blocked if failing)

## CI/CD and Commands

- [ ] Local build script / command (`cd GodForge-BE && dotnet build`) succeeds with zero warnings. (Proved by `dotnet build` exit code 0; blocked if failing)
- [ ] Local test script / command (`cd GodForge-BE && dotnet test`) executes correctly. (Proved by `dotnet test` exit code 0; blocked if failing)
- [ ] Local frontend script (`cd GodForge-FE && npm run lint && npm run typecheck && npm run build`) executes cleanly. (Proved by exit code 0; blocked if failing)
- [ ] Database migration commands are documented and functional. (Proved by `dotnet ef` CLI tests; blocked if failing)
- [ ] API Health checks are implemented. (Proved by `GET /health` returning 200 OK; blocked if failing)

## AI Agent Rigging

- [ ] `docs/SRS/04-database.md` conforms to standard schema rules. (Proved by file existence and content; blocked if missing)
- [ ] `.agents/AGENTS.md` is updated with strict DoR and docs-sync rules. (Proved by content presence; blocked if missing)
- [ ] `frontend-feature`, `project-bootstrap`, `test-quality`, `security-review`, and `docs-sync` agent skills are populated. (Proved by content presence; blocked if missing)

## Blockers & Implementation Rules

- If `docs/SRS/04-database.md` is missing, implementation is blocked.
- If RBAC matrix is inconsistent with SRS roles, implementation is blocked.
- If error code registry is incomplete, API implementation is blocked.
- If quality gates are not runnable yet, only bootstrap milestone may proceed.
- If skeleton/root files are missing, feature implementation is blocked.

**Only once all applicable items are checked may standard feature implementation begin.**
