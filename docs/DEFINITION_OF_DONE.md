# Definition of Done

A feature is done only when implementation evidence satisfies every applicable gate.

## Functional completion

- All Must acceptance criteria pass.
- Alternate flows and expected failures are implemented.
- Authorization is enforced server-side for every resource path.
- API and UI states include loading, empty, success, degraded and error behavior.

## Code quality

- Clean Architecture dependency rules pass.
- No business rules are embedded in controllers, transport consumers or `Program.cs`.
- No secrets, raw credentials, internal paths or unbounded payloads are logged or returned.
- Static analysis, formatting and build checks pass.

## Data and reliability

- Migration is forward-only, reviewed and tested on a fresh database.
- Required constraints and indexes exist.
- Jobs are idempotent, retry-safe and cancellation-aware.
- Temporary workspaces and partial artifacts are cleaned safely.
- Cache invalidation and retention rules are implemented.

## Tests

- Unit tests cover business rules and state transitions.
- Integration tests cover persistence, API, authorization and infrastructure boundaries.
- Security regression tests cover the feature threat model.
- Worker tests cover duplicate delivery, retry, timeout and DLQ behavior when applicable.
- Performance tests meet the documented budget.

## Documentation and operations

- SRS, API, error codes, RBAC and traceability are synchronized.
- `IMPLEMENTATION_STATUS.md` includes evidence links or commands.
- Metrics, logs and dashboards are available.
- Deployment and rollback instructions are documented.
- No unresolved Critical or High security finding remains.
