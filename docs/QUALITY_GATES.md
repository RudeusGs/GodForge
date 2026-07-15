# CI/CD Quality Gates

A feature is complete only after the commands below pass from a clean checkout.

## Backend

```bash
cd GodForge-BE
dotnet restore
dotnet build --no-restore
dotnet test --no-build --collect:"XPlat Code Coverage"
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```

Requirements: zero build warnings, all tests pass, no known vulnerable direct/transitive package accepted without documented exception.

## Frontend

For the first migration run, create and commit a lockfile:

```bash
cd GodForge-FE
npm install
```

After `package-lock.json` exists, the canonical gate is:

```bash
npm ci
npm run lint
npm run typecheck
npm run test:unit
npm run build
npm audit --audit-level=critical
```

## Infrastructure

```bash
cp .env.example .env
docker compose config
docker compose up -d
docker compose ps
dotnet run --project GodForge-BE/src/GodForge.Api --no-build
```

The API and worker must start with RabbitMQ enabled, and one fixture repository analysis must reach a terminal job state.

## Blueprint-specific security gate

- Context fixture containing `.env`, bearer token and private key produces no secret in output.
- Gemini disabled mode still completes deterministic analysis.
- Invalid AI JSON produces degraded status, not worker crash.
- Duplicate repository/commit/input hash does not duplicate snapshot or AI run.
- Removed project member cannot read repository or jobs.
