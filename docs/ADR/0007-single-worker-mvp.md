# ADR 0007: Single Worker MVP Architecture

## Status
Accepted

## Context
GodForge has multiple distinct background jobs (Clone, Parse, Analyze, Diff, Notification). According to the initial clean architecture, these could be separated into independent micro-workers for scalability. However, managing multiple deployment units for MVP adds unnecessary operational complexity.

## Decision
For the MVP phase, all worker logic must be contained within a single `GodForge.Worker` project. 
- **Single Host:** There will be only one worker generic host (`Program.cs`) that boots up all consumers.
- **Distinct Consumers:** Despite being in the same project, each job type MUST retain its own dedicated RabbitMQ queue, Consumer, and Handler abstraction.
- **Strict Isolation:** Code in the worker must not share transient state between different job types. The separation must be clean enough that we can easily split this project into multiple microservices in the future without refactoring core logic.

## Consequences
### Positive
- Simplifies deployment (only one API container and one Worker container required).
- Reduces memory overhead for low-throughput environments.
- Easier local debugging.

### Negative
- If one job type crashes the entire worker process (e.g. OOM), all other jobs are affected.
- Cannot independently scale specific job types horizontally.

## Constraints enforced on AI agents
- Do not create new `GodForge.Worker.*` projects.
- Put new consumers into the existing `GodForge.Worker` project but maintain strict namespace and handler separation.
