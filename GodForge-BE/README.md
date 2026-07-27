# GodForge Backend

ASP.NET Core .NET 9 backend and worker for the Git-first GodForge blueprint.

## Architecture

- `GodForge.Api`: authentication, RBAC, REST contracts and durable job creation.
- `GodForge.Application`: CQRS, DTOs, permissions and infrastructure interfaces.
- `GodForge.Domain`: project, repository, revision, job and analysis state.
- `GodForge.Infrastructure`: PostgreSQL, Redis, RabbitMQ, Git CLI, Gemini and Forgejo adapters.
- `GodForge.Worker`: repository analysis pipeline consumer.

The API must not clone/fetch repositories, read large trees or call Gemini inside an HTTP request.

## Local run

From repository root:

```bash
cp .env.example .env
docker compose up -d
```

Backend:

```bash
cd GodForge-BE
dotnet restore
dotnet tool restore
dotnet run --project src/GodForge.Api
```

The API initializes an empty database from the current EF model and applies pending migrations for an existing database.

Worker in a second terminal:

```bash
cd GodForge-BE
dotnet run --project src/GodForge.Worker
```

## Quality gates

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```

See `../docs/SRS/02-architecture.md`, `../docs/MILESTONES.md`, and `../docs/STABILIZATION_REPORT.md` before adding repository, parser, worker or AI features.
