# GodForge Backend

ASP.NET Core .NET 10 backend and worker for GodForge.

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

Development startup initializes the local database. Production migrations run as a controlled release step; API replicas must not migrate automatically.

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

Node-based repository tooling and Git hooks are configured at the repository root. Install them there with `npm ci`; the backend does not need a separate npm dependency tree. Run root `npm run verify` for the isolated combined gate.
