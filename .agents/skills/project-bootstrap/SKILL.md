---
name: project-bootstrap
description: Skill for initializing and validating the structural integrity of the GodForge workspace and build pipeline.
---

# Project Bootstrap

## When to Use
Use this skill when initializing the repository, validating the environment setup, or modifying CI/CD build scripts.

## Required Reading
- `docs/SETUP_CHECKLIST.md`
- `docs/QUALITY_GATES.md`
- `docs/ENVIRONMENT.md`

## Workflow
1. Verify the existence of `.editorconfig`, `global.json`, `Directory.Build.props`, and `.env.example`.
2. Validate Docker Compose configuration for backing services.
3. Verify `dotnet build` and `npm run build` scripts.
4. Establish CI/CD pipelines.

## Mandatory Checks
- Root files (`.editorconfig`, `global.json`, `Directory.Build.props`) must be present and correctly configured.
- `.env.example` must contain keys for PostgreSQL, Redis, RabbitMQ, and MinIO but WITHOUT real secrets.
- `docker-compose.yml` must start correctly.
- Solution/project creation for backend (`GodForge-BE`) and frontend (`GodForge-FE`) Vite setup must be valid.
- Backend test projects must be linked properly.
- API Health checks must be implemented.
- CI workflow and quality gates must be validated.

## Forbidden Actions
- Do not commit real secrets or passwords to Git.
- Do not bypass linter errors.

## Completion Checklist
- [ ] Root configuration files are present.
- [ ] Docker Compose starts all backing services.
- [ ] Backend test projects linked.
- [ ] Frontend Vite setup configured.
- [ ] CI gates defined and validated.
- [ ] No real secrets in `.env.example`.

## Output Expectations
The agent must report the result of the initialization, including which root files were verified and whether the CI workflow and quality gates passed successfully.
