# ADR 0003: Async Job Processing

## Status
Accepted

## Context
Many of GodForge's core features—such as cloning repositories, fetching Git data, parsing Godot metadata, analyzing project health, and dispatching notifications—are long-running processes that would block HTTP requests and lead to timeout failures.

## Decision
All long-running tasks must be implemented as **Asynchronous Jobs**.

- The API layer will immediately persist a Job record in PostgreSQL and return `202 Accepted` with a Job ID.
- The system will publish a message to **RabbitMQ** to trigger the worker.
- **Workers** must process these queues. Each distinct type of job (e.g., clone, parse, diff) will have its own dedicated queue and consumer logic.

## Job Requirements
Every async job MUST support:
1. **Idempotency**: Retrying a job must not duplicate output or side effects. This implies tracking an `inputHash` or similar.
2. **Durability**: Job state (Pending, Running, Failed, Completed) is strictly managed in PostgreSQL.
3. **Retries and DLQ**: Transient failures must trigger exponential backoff. Poison messages must move to a Dead Letter Queue (DLQ).
4. **Timeouts & Cancellation**: Long-running jobs must respect cancellation tokens and enforce maximum execution times.
5. **Heartbeats**: Running jobs must report progress and heartbeat signals to prevent stale states if a worker crashes.

## Consequences
### Positive
- API remains responsive.
- Robust failure handling and clear observability into background task status.

### Negative
- Increased architectural complexity.
- Requires clients to poll or use SignalR to receive completion notifications.

## Constraints enforced on AI agents
- Never implement clone, fetch, parse, analyze, diff, or preview logic directly within an HTTP request pipeline.
- Do not mark a job as 'Complete' until all its database and artifact outputs are successfully committed to their respective stores.
