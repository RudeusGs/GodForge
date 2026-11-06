# GodForge

A specialized Git and project-intelligence platform for Godot Engine development teams.

GodForge bridges the gap between raw Git repositories and game engine architecture. While generic Git hosting services treat Godot projects as opaque collections of text and binary files, GodForge provides tenant-aware project management, asset visibility boundaries, and an engine-aware analysis pipeline tailored specifically to Godot games and tools.

GodForge does not reinvent Git protocols or replace Git servers: external Git providers or self-hosted Forgejo instances serve as the authoritative Git-object storage. GodForge owns the multi-tenant collaboration layer, scene/script validation, architecture dependency graph extraction, and project health analysis.

---

## Architecture Overview

GodForge follows **Clean Architecture** and **CQRS (Command Query Responsibility Segregation)**:

```text
Browser Client (Vue 3 + TypeScript + Vite + Tailwind CSS)
                       |
                       v
         ASP.NET Core Web API (.NET 10 LTS)
         +-- MediatR CQRS Pipelines & FluentValidation
         +-- JWT Authentication & HttpOnly Session Cookies
         +-- Server-Evaluated Multi-Tenant RBAC Matrix
                       |
        +--------------+--------------+
        |              |              |
        v              v              v
  PostgreSQL 16     Redis 7       RabbitMQ 3.13
  (Business Data   (Distributed   (Asynchronous
   & Outbox)       Locks & Cache)  Job Transport)
                       |
                       v
         GodForge Background Worker (.NET 10 LTS)
         +-- Outbox Message Dispatcher
         +-- Isolated Workspace Execution
         +-- Analysis Pipeline & Engine Validation
```

### Architectural Invariants
- **Database Authority:** PostgreSQL is the source of truth for business data, tenancy, and job state. Redis is strictly for caching and distributed locks; RabbitMQ is strictly transport.
- **Tenant Isolation:** Every project belongs to exactly one organization. Effective permissions represent the intersection of platform minimums, organization policy, project role, and resource policies.
- **Untrusted Code Sandbox:** Repository contents are treated as untrusted data. GodForge never executes untrusted Godot game scripts, editor plugins, or binary extensions on host servers or worker containers.

---

## Current Implemented Functionality

### Implemented & Verified Today
- **Identity & Authentication (M1):** Email registration with one-time verification challenge (OTP), secure login with memory-only access tokens and `HttpOnly` `SameSite=Strict` refresh cookies, multi-device session management, remote revocation, and password recovery.
- **Organization Management & Tenancy (M1):** Creation, slug normalization, member directory with server-side pagination (`pageSize = 20`) and search/filtering, role hierarchy (`OrganizationOwner`, `OrganizationAdmin`, `OrganizationMember`), invitations with memory-only token handling and automatic URL scrubbing, atomic ownership transfers, and deletion requests.
- **Project Lifecycle & Workspace (M1):** Project creation, slug uniqueness within organization, assigned project navigation, separation of administrative catalog from project content, project member role management, ownership transfers, and project archive/restore.
- **Settings & Policy Management (M1):** Versioned own-user preferences, organization-wide AI advisory restrictions, and hierarchical policy precedence resolution (Platform > Organization > Project).
- **Automated Verification Pipeline (M0):** Local Docker Compose verification harnesses, real-PostgreSQL integration testing, Playwright browser E2E suites, and container vulnerability scanning.

### Planned Roadmap Features (Not Yet Implemented)
- **Hosted Git Integration (M2, partial):** Provisioning, permission reconciliation, signed webhook, browsing and repository UI code exists; real Forgejo acceptance and linked-mode completion remain open. See [provider setup](docs/ENVIRONMENT.md).
- **Durable Worker Pipelines (M3):** Dead-letter queues (DLQ), distributed job cancellation, and background analysis orchestration.
- **Godot Scene & Script Parser (M4–M6):** Deterministic AST parser for `.tscn`, `.tres`, and `.gd` files, dependency graph visualization, and health score engine.
- **AI Advisory Services (M7):** Optional Gemini analysis from redacted, structured context summaries.
- **Asset Vault (M8):** MinIO binary asset storage with independent visibility boundaries (public, project, organization, owner-only).

---

## Technology Stack

- **Backend:** .NET 10 LTS, ASP.NET Core Web API, MediatR (CQRS), FluentValidation, Entity Framework Core 10.
- **Frontend:** Vue 3 Composition API, TypeScript (strict mode), Vite, Pinia, Vue Router, Tailwind CSS.
- **Data & Infrastructure:** PostgreSQL 16 (relational data & outbox), Redis 7 (locks & abuse limits), RabbitMQ 3.13 (message broker), Mailpit (local SMTP capture), Docker & Docker Compose.
- **Testing & QA:** xUnit, ASP.NET Core `WebApplicationFactory` (real PostgreSQL/Redis integration), Vitest & Vue Test Utils, Playwright (Chromium E2E), Gitleaks (secret detection), Trivy (container scanning).

---

## Current Status

| Area | Status | Verified Evidence |
|---|---|---|
| **Foundation & Runtime (M0)** | Complete | .NET 10 LTS target, Docker Compose, clean CI pipeline |
| **Authentication & Sessions (M1)** | Complete | Real PostgreSQL HTTP tests, multi-session revocation |
| **Organization Management (M1)** | Complete | Server-paginated directories, RBAC matrix, live E2E |
| **Project Workspace (M1)** | Complete | Project CRUD, archive/restore, member assignment |
| **Settings & Policy (M1)** | Complete | User preferences, AI policy precedence, schema upgrades |
| **Hosted Git (M2)** | Partial | Local code/persistence regressions; real provider clone/push, webhook and external revocation acceptance remain open |
| **Godot Parser & Graph (M4–M6)** | Planned | Formal grammar and AST design phase |
| **Asset Vault (M8)** | Planned | Storage abstraction designed |

For the full detailed status matrix, see [docs/IMPLEMENTATION_STATUS.md](docs/IMPLEMENTATION_STATUS.md).

---

## Getting Started Locally

### Prerequisites
- [.NET 10 SDK](GodForge-BE/global.json)
- [Node.js 22 LTS](GodForge-FE/package.json) & npm
- [Docker & Docker Compose](docker-compose.yml)
- Git

### Mode A: Local Host Development (Fast Iteration)

Run infrastructure dependencies in Docker while executing the .NET API, Worker, and Vue frontend directly on your development host:

1. **Start Infrastructure Services:**
   ```bash
   cp .env.example .env
   docker compose up -d postgres redis rabbitmq mailpit
   ```

2. **Run Backend API & Background Worker:**
   In terminal 1 (API):
   ```bash
   cd GodForge-BE
   dotnet restore
   dotnet build
   dotnet run --project src/GodForge.Api
   ```
   In terminal 2 (Worker):
   ```bash
   cd GodForge-BE
   dotnet run --project src/GodForge.Worker
   ```
   - API Swagger UI: `http://localhost:5072/swagger`
   - Local Mailbox (Mailpit): `http://localhost:8025`

3. **Run Frontend Development Server:**
   In terminal 3 (Frontend):
   ```bash
   cd GodForge-FE
   npm ci
   npm run dev
   ```
   - Web Application: `http://localhost:5173`

### Mode B: Full Containerized Backend

Run the complete backend stack (PostgreSQL, Redis, RabbitMQ, Mailpit, API, and Worker) in isolated Docker containers:

```bash
# 1. Start backend stack
docker compose -f docker-compose.backend.yml up -d --build --wait

# 2. Run Frontend configured for Docker backend (targets published port 15072)
cd GodForge-FE
npm ci
npm run dev:docker

# Alternatively, configure .env.local for standard 'npm run dev':
# Windows PowerShell: 'VITE_API_BASE_URL=http://localhost:15072/api/v1' | Set-Content .env.local
# Linux / macOS:     echo "VITE_API_BASE_URL=http://localhost:15072/api/v1" > .env.local

# 3. Stop backend stack when finished
docker compose -f docker-compose.backend.yml down
```
- Containerized API Swagger: `http://localhost:15072/swagger`
- Containerized Mailpit Mailbox: `http://localhost:18025`

### Mode C: Frontend-Only Development

To work strictly on the frontend against an existing running API instance:

```bash
cd GodForge-FE
npm ci
npm run dev
```
The frontend connects to `http://localhost:5072/api/v1` by default (configurable via `VITE_API_BASE_URL` in `.env`).

### Mode D: Automated Testing & Verification

GodForge provides comprehensive automated test suites across all layers. Detailed command references, architecture breakdowns, and scenario catalogs are documented in the [Testing and Verification Guide](docs/TESTING.md).

Quick verification commands from the repository root:

```powershell
# Full automated local verification gate (docs, containers, BE unit/integration, FE lint/test/build, browser E2E)
npm.cmd run verify

# Backend unit and integration tests only (disposable Postgres 16 & Redis 7)
npm.cmd run verify:backend

# Live browser acceptance E2E (builds API/Worker images, tests against live services without mock HTTP)
npm.cmd run test:workspace:live

# Isolated Forgejo adapter and complete HTTP/Worker/Git/browser acceptance
npm.cmd run test:m2:provider
npm.cmd run test:m2:acceptance

# Backend runtime smoke gate (41 HTTP assertions against docker-compose.backend.yml)
docker compose -f docker-compose.backend.yml up -d --build --wait
try { npm.cmd run test:backend:smoke } finally { docker compose -f docker-compose.backend.yml down }

# Documentation Markdown links validation
npm.cmd run docs:check
```

Detailed test catalogs, concurrency scenarios, and regression history are documented in the [Testing and Verification Guide](docs/TESTING.md).

---

## Technical Documentation Index

Key architecture and specification documents:
- [Product Vision](docs/PRODUCT_VISION.md) — Product requirements and differentiation.
- [System Architecture](docs/SRS/02-architecture.md) — Service boundaries, data flow, and components.
- [RBAC Matrix](docs/RBAC_MATRIX.md) — Permission model across platform, organization, and project scopes.
- [Quality Gates](docs/QUALITY_GATES.md) — Code review, build, and merge verification criteria.
- [Testing Guide](docs/TESTING.md) — Execution guide for unit, integration, and E2E suites.
- [Implementation Status](docs/IMPLEMENTATION_STATUS.md) — Current state of feature verification.
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md) — Ordered roadmap and milestone dependencies.
- [All Technical Documentation](docs/README.md) — Complete documentation index.
