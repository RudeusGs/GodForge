# Definition of Ready (DoR)

Before any feature ticket or development task begins implementation, it must meet the following criteria. AI agents must explicitly verify these elements are defined and clear before writing code. NO FEATURE CAN BE IMPLEMENTED unless it has:

- [ ] **SRS Requirement ID**: Maps directly to a Functional or Non-Functional Requirement.
- [ ] **Affected Functional Doc**: Which `docs/SRS/03-functional/*` file covers this.
- [ ] **API Contract**: The exact route, method, request schema, and response schema.
- [ ] **Database Impact**: Which tables are read from or written to, and if any schema changes are needed.
- [ ] **RBAC Permission**: The specific role or project-level permission required to execute the action.
- [ ] **Sync/Async Decision**: Is this a fast synchronous HTTP response, or a long-running async job?
- [ ] **Worker/Job Behavior**: If async, the RabbitMQ queue, idempotency key, DLQ strategy, and timeout rules.
- [ ] **Error Codes**: Exact `SCREAMING_SNAKE_CASE` error codes that the feature can produce.
- [ ] **Acceptance Criteria**: Clear, testable conditions that prove the feature works.
- [ ] **Tests Required**: What unit, integration, or regression tests must be created or updated.
- [ ] **Observability Requirements**: Required correlation IDs, activity log events, metrics, and sanitized logging.
- [ ] **Security Considerations**: How the feature handles sensitive data, inputs, or workspace state.
- [ ] **Docs That Must Be Updated**: Which docs (SRS, ADR, etc.) require changes.

## Agent Prohibition
If a task lacks any of the above information, the AI agent MUST NOT write implementation code. Instead, the agent must ask the user for clarification or update the documentation to satisfy the DoR.
