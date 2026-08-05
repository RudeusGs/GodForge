# Repository Structure

```text
GodForge/
├── GodForge-BE/
│   ├── src/
│   │   ├── GodForge.Api/              # HTTP, middleware, contracts, DI
│   │   ├── GodForge.Application/      # CQRS, DTOs, interfaces, authorization
│   │   ├── GodForge.Domain/           # Entities, value objects, invariants
│   │   ├── GodForge.Infrastructure/   # EF Core and provider implementations
│   │   └── GodForge.Worker/           # Logical queue consumers and handlers
│   └── tests/
│       ├── GodForge.UnitTests/
│       └── GodForge.IntegrationTests/
├── GodForge-FE/                        # Vue 3 + TypeScript + Vite
├── docs/                               # Product and engineering source of truth
├── .agents/                            # AI-agent rules and task skills
├── .github/                            # CI workflows
├── docker-compose.yml                  # Local services
└── .env.example                        # Non-secret configuration template
```

## Backend feature layout

```text
GodForge.Application/Features/{Module}/
├── Commands/{Action}/
├── Queries/{ReadModel}/
└── DTOs/
```

Infrastructure implementation follows concern-specific folders such as `Persistence`, `Git`, `Messaging`, `Storage`, `Security`, `AI`, `Caching` and `Observability`.

## Worker layout target

```text
GodForge.Worker/
├── Consumers/
├── Handlers/
├── Contracts/
├── Scheduling/
└── Observability/
```

Each logical job type has an explicit consumer/handler pair. A shared host must not become a universal business-logic class.

## Documentation rule

Do not rename or relocate major projects without an accepted ADR and synchronized documentation.
