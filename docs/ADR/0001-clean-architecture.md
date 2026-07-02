# ADR 0001: Clean Architecture

## Status
Accepted

## Context
GodForge is a complex platform dealing with Git operations, metadata parsing, and Godot project management. We need an architecture that ensures business logic is independent of UI, database, and external agencies, allowing for robust testing and maintainability.

## Decision
We will use **Clean Architecture** combined with the **CQRS** pattern for the backend (.NET 9).

The solution structure will strictly enforce the following dependency rule:
- `GodForge.Domain` -> (No external dependencies)
- `GodForge.Application` -> depends only on `GodForge.Domain`
- `GodForge.Infrastructure` -> depends on `GodForge.Application` and `GodForge.Domain`
- `GodForge.Api` -> depends on `GodForge.Application` and `GodForge.Infrastructure`
- `GodForge.Worker` -> depends on `GodForge.Application` and `GodForge.Infrastructure`

CQRS is implemented using MediatR. Every use case must be represented by exactly one Command or Query.

## Consequences
### Positive
- Business logic is completely decoupled from frameworks and infrastructure.
- High testability of Domain and Application layers.
- Clear separation of read vs. write operations.

### Negative
- Initial development overhead (creating handlers, models, validation for each operation).
- Data mapping is required between layers to avoid domain entities bleeding into the API.

## Constraints enforced on AI agents
- Never reference infrastructure components from Domain/Application.
- Never add database `DbContext`, Redis, or RabbitMQ logic inside MediatR handlers directly.
- API controllers must not contain business logic; they only act as routing and orchestration layers.
