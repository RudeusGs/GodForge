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
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
dotnet run --project src/GodForge.Api
```

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

See `../docs/GODFORGE_FEASIBLE_SYSTEM_BLUEPRINT.md` and `../docs/BLUEPRINT_MIGRATION_GUIDE.md` before adding repository, parser, worker or AI features.
