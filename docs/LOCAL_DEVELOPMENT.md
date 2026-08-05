# Local Development

## Prerequisites

- .NET SDK version from `global.json`.
- Node.js 22 and npm 10, matching `GodForge-FE/package.json`.
- Docker and Docker Compose.
- Git.

## Setup

```bash
cp .env.example .env
docker compose up -d
```

Enable hosted Git when required:

```bash
docker compose --profile hosted-git up -d
```

## Backend

```bash
cd GodForge-BE
dotnet restore
dotnet tool restore
dotnet build
```

Run API and Worker in separate terminals:

```bash
dotnet run --project src/GodForge.Api
dotnet run --project src/GodForge.Worker
```

## Frontend

```bash
cd GodForge-FE
npm install --no-audit --no-fund
npm run dev
```

## Before committing

```bash
cd GodForge-BE
dotnet format --verify-no-changes
dotnet test

cd ../GodForge-FE
npm run lint
npm run typecheck
npm run test:unit
npm run build
```

## Local safety

- Use disposable test repositories only.
- Do not point local workers at company repositories without approval.
- Keep Gemini disabled unless testing AI behavior.
- Clear `.workspaces` only after workers are stopped.

## Managed Windows Code Integrity

Some managed Windows environments require enterprise-signed assemblies and may
block locally built test or Worker DLLs with error `0x800711C7`. Do not disable or
bypass the machine policy. Use the official .NET 10 SDK container for verification:

```powershell
docker run --rm -v "${PWD}:/workspace" -w /workspace/GodForge-BE `
  mcr.microsoft.com/dotnet/sdk:10.0 `
  dotnet test GodForge.sln --artifacts-path /tmp/godforge-artifacts
```

The temporary artifacts remain inside the disposable container. Docker access is
still subject to the local organization's security policy.