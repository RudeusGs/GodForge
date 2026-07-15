---
name: debug-issue
description: Skill for systematically debugging and fixing issues in the GodForge backend. Use when investigating API, worker, Git, parser, analyzer, metadata, RBAC, database, or background-job failures. Covers SRS-first diagnosis, correlation-id tracing, structured logs, architectural layer ownership, root-cause analysis, safe fixes, regression tests, and verification without leaking secrets or hiding failures.
---


Use this skill to debug and fix GodForge backend issues without violating the SRS architecture, security, observability, or testing conventions.


Always debug from the externally visible failure back to the owning layer. Do not patch the nearest symptom. Preserve module boundaries, keep controllers thin, keep business rules in Application/Domain, and keep infrastructure concerns in Infrastructure/engines/workers.


Collect the minimum facts before changing code:

- User-visible symptom, endpoint/job/action, environment, and reproduction steps.
- HTTP status, application error code, and `correlationId`.
- Actor/user id, project id, repository id, branch/revision, job id, and event id when applicable.
- Whether the failure is synchronous API, asynchronous worker, Git operation, parser/analyzer/diff, metadata/search, notification, or activity log.
- Expected behavior from the relevant SRS functional requirement, workflow, acceptance criteria, NFR, API contract, or error catalog.

For API errors, use the response `correlationId` to find request logs. For async failures, trace the job record, job logs, emitted events, worker attempts, DLQ status, and persisted job `error_code`.


Use structured logs and metrics; avoid relying only on raw text search.

Required trace fields:

- Request logs: timestamp, method, path, status, duration, user id, project id, correlation id.
- Job logs: job id, job type, state, attempt, duration, worker id, correlation id.
- Git logs: operation, repository id, branch, sanitized stderr, result, correlation id.
- Security logs: permission denied, token revoked, suspicious rate, failed login.
- Domain audit logs: actor, action, target, before/after summary, timestamp, status, correlation id.

Never log or expose:

- Access tokens, refresh tokens, passwords, Git credentials, credential-bearing remote URLs.
- Full repository file content, large stdout/stderr, stack traces in API responses, or sensitive artifact payloads.

Production default should remain `Information` for lifecycle logs and `Warning`/`Error` for abnormal events. Enable debug logs only temporarily and only after confirming they do not leak secrets.


Map the bug to the layer that owns the failing responsibility.

| Symptom | Primary owner | Typical fix location |
|---|---|---|
| Wrong invariant, invalid state transition, permission rule, role rule | Domain/Application | `GodForge.Domain` or `GodForge.Application` |
| Request validation, authorization orchestration, command/query flow | Application | handlers, validators, policies |
| Incorrect response shape, route binding, DTO mapping, HTTP status | API | controllers, contracts, filters/middleware |
| DB query, EF mapping, transaction, migration, external storage | Infrastructure | repositories, EF configuration, persistence services |
| Git command, conflict handling, workspace isolation, lock behavior | Git Engine / Infrastructure | Git engine, repository lock, Git service adapter |
| Parser/analyzer/diff algorithm output | Core Engine / Worker | parser/analyzer/diff engine and worker orchestration |
| Job timeout, retry, heartbeat, DLQ, idempotency | Worker/Application | worker host, job service, queue consumer |
| Cache staleness or dashboard/search inconsistency | Application/Infrastructure | cache invalidation, events, read model |
| Missing or unsafe logs/audit | Application/Infrastructure | logging middleware, audit service, activity writer |

Do not fix a domain/application bug inside a controller for convenience. Do not place business rules in workers just because the failure occurred during a job; workers should call the same application/domain logic used by API entry points.


Before writing the fix:

1. Locate the related SRS section: functional requirement, workflow, API endpoint, error catalog, event catalog, component boundary, logging strategy, security model, or NFR.
2. Identify the expected success path, error path, permission rule, and audit/logging requirement.
3. Confirm whether the problem is a code defect, outdated test, missing requirement, or ambiguous specification.
4. If the SRS and implementation disagree, treat the SRS as the source of truth unless the user explicitly asks to revise the SRS.

Common SRS checks:

- Every protected operation must enforce JWT and RBAC server-side.
- Every important state-changing operation must write activity log for success and failure.
- Every backend error must carry a correlation id.
- Async jobs must persist status, progress/heartbeat, timeout/error code, and retry/DLQ outcome.
- Heavy tasks such as clone, parse, analyze, diff, and preview must not block HTTP request handling.
- Git operations must use repository locking and must not auto-resolve conflicts without explicit policy/user confirmation.
- Metadata-not-ready must be distinguished from an empty search/list result.


Create a deterministic reproduction before fixing:

- For API bugs: call the exact endpoint with minimal body/query/headers and capture response + correlation id.
- For authorization bugs: test both an allowed actor and a denied actor in the same project scope.
- For job bugs: enqueue or invoke the same job with the original project id, input hash, revision, attempt count, and correlation id where possible.
- For Git bugs: use a small test repository fixture that reproduces branch/conflict/lock behavior.
- For parser/analyzer/diff bugs: add a minimal `.tscn`, `.tres`, `.gd`, or metadata fixture that exposes the issue.
- For DB bugs: reproduce with a transaction-scoped integration test or containerized PostgreSQL when behavior depends on SQL semantics.

If the bug cannot be reproduced, improve observability first or add a failing characterization test around the suspected boundary.


Apply the smallest fix that corrects root cause and preserves SRS behavior.

Do:

- Keep controller logic limited to request/response orchestration.
- Put business decisions in Domain/Application.
- Put persistence, external service, Git, Redis, RabbitMQ, MinIO, and EF details in Infrastructure.
- Keep core engines deterministic and return structured results with warnings, error codes, duration, and metric tags.
- Propagate correlation id through API, Application, events, jobs, engines, and logs.
- Sanitize external command output before returning or logging it.
- Preserve idempotency for jobs using job id and input hash.
- Use retry with backoff only for transient errors; send poison messages to DLQ instead of retrying forever.

Do not:

- Swallow exceptions with broad `try/catch` blocks.
- Convert real failures into successful empty results.
- Disable warnings or tests to make the build pass.
- Return stack traces or internal exception details to clients.
- Log secrets, raw tokens, or credential-bearing Git URLs.
- Auto-resolve Git conflicts or overwrite repository state without explicit policy and confirmation.
- Break module dependency direction or introduce circular references.


Every bug fix must add or update tests that fail before the fix and pass after it.

Choose the right test level:

- Domain unit test for invariants, permissions, state transitions, value objects.
- Application handler test for business flow, RBAC, validation, activity log, event publishing, job creation.
- API integration test for route, auth, response envelope, error code, correlation id, and status code.
- Infrastructure integration test for EF/PostgreSQL behavior, transactions, indexes, queries, Redis locks, MinIO artifacts.
- Worker test for job state transitions, retry/backoff, heartbeat, timeout, DLQ, idempotency.
- Engine fixture test for Git/parser/analyzer/diff deterministic outputs.
- Security regression test for access denial, project scoping, token handling, or unsafe logging.

Test both the success path and the failure path if the bug involves permissions, async jobs, Git conflicts, parse errors, or error responses.


Run verification appropriate to the changed area:


When the fix touches PostgreSQL, Redis, RabbitMQ, MinIO, Git workspaces, or workers, also run the relevant integration test suite or Docker Compose scenario.

Manual verification checklist:

- API response uses the standard success/error envelope.
- Error response includes safe `code`, `message`, `correlationId`, and safe `details` only.
- Activity log records actor, action, target, status, timestamp, and correlation id.
- Job state is correct after success/failure/cancel/timeout.
- No secret appears in logs, response body, activity log, or persisted job error details.
- Metrics/logs allow tracing the same correlation id across request, job, event, engine, and worker.


Use Conventional Commits:


Examples:


In the PR or implementation notes, include:

- Root cause.
- SRS requirement or acceptance criteria verified.
- Reproduction steps.
- Tests added/updated.
- Operational notes, migrations, config changes, or dashboard/log queries if relevant.


