# Local Development Guide

## Prerequisites
- **.NET 9 SDK**
- **Node.js 20+**
- **Docker & Docker Compose**

## Setup Steps

1. **Clone the repository.**
2. **Environment**: Copy `GodForge-BE/.env.example` to `GodForge-BE/.env`.
3. **Backing Services**: Run `docker-compose up -d` from the root to start PostgreSQL, Redis, RabbitMQ, and MinIO.
4. **Backend Build**: Navigate to `GodForge-BE` and run `dotnet build`.
5. **Database Migrations**: Run `dotnet ef database update --project src/GodForge.Infrastructure --startup-project src/GodForge.Api`.
6. **Backend Run**: Run `dotnet run --project src/GodForge.Api`. The API will start on `https://localhost:5001`.
7. **Worker Run**: Run `dotnet run --project src/GodForge.Worker`.
8. **Frontend Build**: Navigate to `GodForge-FE`, run `npm install`, then `npm run dev`.

## Working with Workers
Local workers will listen to the local RabbitMQ queues. Ensure RabbitMQ management console (usually `localhost:15672`) is accessible to monitor queues.
