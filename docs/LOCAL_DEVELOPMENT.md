# Local Development

## Prerequisites

- .NET 9 SDK.
- Node.js 22 recommended.
- Git CLI.
- Docker with Compose.

## First setup

```bash
cp .env.example .env
docker compose up -d
```

Generate the frontend lockfile once and commit it:

```bash
cd GodForge-FE
npm install
```

Create/review the EF migration, then:

```bash
cd ../GodForge-BE
dotnet restore
dotnet tool restore
dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api
```

## Run

```bash
# terminal 1
cd GodForge-BE
dotnet run --project src/GodForge.Api

# terminal 2
cd GodForge-BE
dotnet run --project src/GodForge.Worker

# terminal 3
cd GodForge-FE
npm run dev
```

RabbitMQ management is at `http://localhost:15672`; MinIO console is at `http://localhost:9001` with the local credentials from `.env`.

Hosted Git is optional:

```bash
docker compose --profile hosted-git up -d
```

Do not enable Gemini or Forgejo production adapters with example credentials.
