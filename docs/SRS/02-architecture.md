# Architecture

## Boundaries

```text
Vue Client -> ASP.NET Core API -> PostgreSQL/RabbitMQ
                                  |
                                  v
                           GodForge.Worker
                     Git workspace / Parser / AI
```

- Domain contains entities and state transitions only.
- Application contains CQRS, DTOs, permissions and infrastructure abstractions.
- Infrastructure contains EF Core, Redis, RabbitMQ, Git CLI, Forgejo and Gemini adapters.
- API validates/authenticates and creates durable jobs; it never runs heavy work.
- Worker consumes messages, owns repository workspaces and updates durable job state.

## Source-of-truth rules

- Git repository: source code.
- PostgreSQL: projects, membership, revisions, metadata, reports and jobs.
- RabbitMQ: transport only.
- Redis: cache, rate limiting and distributed repository locks only.
- MinIO: generated artifacts and large context/report payloads.
- Gemini: optional advisory output.

## Canonical pipeline

```text
revision discovery -> secure checkout -> inventory -> deterministic parse
                    -> health/dependencies -> bounded context -> Gemini advisory -> finalize
```

A pipeline is identified by repository, commit SHA, parser/rule/prompt versions and input hash. Duplicate messages must not duplicate output.
