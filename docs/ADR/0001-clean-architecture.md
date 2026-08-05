# ADR 0001: Clean Architecture boundaries

## Status
Accepted

## Context
GodForge integrates HTTP, Git, PostgreSQL, Redis, RabbitMQ, MinIO, Forgejo, Gemini and filesystem processing. Without strict boundaries, business rules become coupled to frameworks and difficult to test.

## Decision
Use these dependencies:

```text
Domain -> no project dependency
Application -> Domain
Infrastructure -> Application + Domain
Api -> Application + Infrastructure
Worker -> Application + Infrastructure
```

Domain owns invariants and state transitions. Application owns use cases, authorization decisions, interfaces and transaction intent. Infrastructure implements persistence and providers. API and Worker are delivery hosts.

## Consequences
### Positive
- Testable business logic and replaceable providers.
- Shared use cases between API and Worker.
- Reduced framework leakage.

### Negative
- More abstractions and mapping code.
- Small changes may touch several layers.

## Constraints enforced on implementation and AI agents
- No EF Core, HTTP, Git, queue, Redis, MinIO or Gemini dependency in Domain/Application.
- Controllers and consumers orchestrate only.
- Domain entities are never returned directly through APIs.
