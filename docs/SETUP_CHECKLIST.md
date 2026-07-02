# Setup Checklist

This checklist guarantees the project foundation is solid before any functional feature code is written.

## Prerequisites

- [ ] Node.js, .NET 9 SDK, and Docker are installed locally.
- [ ] `docs/ADR/` contains foundational architecture decisions.
- [ ] `docs/MILESTONES.md` outlines the rollout plan.
- [ ] `docs/DEFINITION_OF_READY.md` is strictly enforced.

## Foundational Root Files

- [ ] `.editorconfig` exists and defines standard formatting rules.
- [ ] `global.json` pins the correct .NET SDK version.
- [ ] `Directory.Build.props` enforces compiler flags (e.g., nullable reference types, warnings as errors).
- [ ] `.env.example` defines all necessary local environment variables without containing real secrets.
- [ ] `docker-compose.yml` supports spinning up PostgreSQL, Redis, RabbitMQ, and MinIO locally.

## CI/CD and Commands

- [ ] Local build script / command (`dotnet build`) succeeds with zero warnings.
- [ ] Local test script / command (`dotnet test`) executes correctly.
- [ ] Local frontend script (`npm run lint && npm run typecheck && npm run build`) executes cleanly.
- [ ] Database migration commands are documented and functional.
- [ ] API Health checks are implemented.

## AI Agent Rigging

- [ ] `docs/SRS/04-database.md` conforms to standard schema rules.
- [ ] `.agents/AGENTS.md` is updated with strict DoR and docs-sync rules.
- [ ] `frontend-feature`, `project-bootstrap`, `test-quality`, `security-review`, and `docs-sync` agent skills are populated.

**Only once all items are checked may standard feature implementation begin.**
