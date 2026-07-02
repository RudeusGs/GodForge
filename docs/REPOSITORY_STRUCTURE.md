# Repository Structure

GodForge is a monorepo containing both the backend and frontend.

```text
GodForge/
├── GodForge-BE/                         # Backend (ASP.NET Core .NET 9)
│   ├── src/
│   │   ├── GodForge.Api/                # API controllers, WebHost
│   │   ├── GodForge.Application/        # MediatR CQRS, Interfaces
│   │   ├── GodForge.Domain/             # Entities, Enums
│   │   ├── GodForge.Infrastructure/     # EF Core, external adapters
│   │   └── GodForge.Worker/             # Background job host
│   └── tests/
│       ├── GodForge.UnitTests/          # Fast Domain/Application tests
│       └── GodForge.IntegrationTests/   # Slower DB/API tests
├── GodForge-FE/                         # Frontend (Vue 3 / Vite)
│   ├── src/
│   │   ├── assets/
│   │   ├── components/
│   │   ├── composables/
│   │   ├── pages/
│   │   ├── router/
│   │   └── stores/
├── docs/                                # Documentation, ADRs, SRS
└── .agents/                             # AI Agent constraints and skills
```

AI Agents must respect these boundaries. Do not place UI logic in the backend, and do not place DB connections in the frontend.
