# GodForge Backend (GodForge-BE)

GodForge Backend is an ASP.NET Core .NET 9 backend for managing, analyzing, and visualizing Godot projects.

## Current Status

This repository currently contains backend readiness assets: .NET build configuration, Docker Compose infrastructure, Git hook configuration, and `scaffold.ps1`.

The C# solution is not committed yet. Run `.\scaffold.ps1` from this directory to generate:

- `GodForge.Backend.sln`
- `src/GodForge.Domain`
- `src/GodForge.Application`
- `src/GodForge.Infrastructure`
- `src/GodForge.Api`
- `src/GodForge.Worker`
- `tests/GodForge.UnitTests`

The generated dependency direction must follow `.agents/AGENTS.md`: Domain has no project reference; Application references Domain; Infrastructure references Application; API and Worker reference Application + Infrastructure.

## Prerequisites

- .NET 9 SDK
- Docker Desktop or compatible Docker Compose runtime
- Node.js only for Husky, Commitlint, lint-staged, and optional Prettier formatting of Markdown/YAML/JSON files

## Run Local Infrastructure

From `GodForge-BE`:

```bash
docker compose up -d
```

This starts PostgreSQL, Redis, RabbitMQ, and MinIO using local-development credentials:

- PostgreSQL user: `godforge_user`
- PostgreSQL database: `godforge_db`
- RabbitMQ/MinIO local password: `godforge_password`

Monitoring is optional and remains under the `monitoring` profile:

```bash
docker compose --profile monitoring up -d
```

Port mappings live in `docker-compose.override.yml` for local development only. Do not use those mappings as production exposure guidance.

## Scaffold The Backend

From `GodForge-BE`:

```powershell
.\scaffold.ps1
```

The scaffold generates a .NET 9 Clean Architecture solution with API, Worker, Domain, Application, Infrastructure, and UnitTests projects.

## Build And Test

After scaffolding:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

Entity Framework commands, once EF Core is added to the Infrastructure/API projects:

```bash
dotnet ef migrations add <Name> --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

## Run API And Worker

After scaffolding and restoring:

```bash
dotnet run --project src/GodForge.Api
dotnet run --project src/GodForge.Worker
```

## Git Hooks And Commitlint

Node tooling is limited to repository workflow checks. It is not the backend build system.

```bash
npm install
npm run prepare
```

Commit messages must follow `.agents/AGENTS.md`, for example:

```text
chore(config): prepare backend scaffold
```

## Do Not Start Feature Implementation Until This Checklist Passes

- No legacy project-name remnants remain in scripts, docs, or compose files.
- `.\scaffold.ps1` generates `GodForge.Backend.sln` and GodForge-named projects.
- `dotnet restore` succeeds for the generated solution.
- `dotnet build --no-restore` succeeds.
- `dotnet test --no-build` succeeds.
- `dotnet format --verify-no-changes` succeeds or intentional formatting changes are committed.
- Docker Compose starts local infrastructure with GodForge names.
- Worker lifecycle states match `docs/SRS/12-worker-processing.md`, `docs/SRS/04-database.md`, `docs/SRS/05-api.md`, and `.agents/skills/worker-job/SKILL.md`.
- No obsolete Node backend build/test instructions remain.
