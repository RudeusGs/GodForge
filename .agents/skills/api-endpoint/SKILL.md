---
name: api-endpoint
description: guidance for adding or modifying api endpoints in the godforge backend. use this skill when implementing asp.net core endpoints, controller actions, request/response dtos, command/query handlers, fluentvalidation rules, rbac checks, standardized api responses, error mapping, activity logging, async job endpoints, git/repository endpoints, or any backend api work that must follow the godforge srs conventions.
---


Use this skill to add or update API endpoints in the GodForge backend while staying aligned with the SRS, API specification, RBAC model, async-job rules, and backend architecture boundaries.


- Treat `docs/SRS/05-api.md` as the source of truth for method, path, request body, response body, validation, permissions, success status, and error cases.
- Cross-check security requirements in `docs/SRS/06-security.md`, non-functional requirements in `docs/SRS/07-non-functional.md`, workflow rules in `docs/SRS/08-workflows.md`, and worker/job rules in `docs/SRS/12-worker-processing.md` when relevant.
- Keep controllers thin: controllers may bind input, call MediatR/application services, and format responses, but must not contain business rules.
- Put business rules, RBAC decisions, ownership checks, repository state checks, and idempotency checks in the Application layer or domain service, not in the controller.
- Never return EF/domain entities directly from the API. Always map to response DTOs.
- Never expose secrets, stack traces, raw internal exception details, Git credentials, token values, or sensitive paths in responses, logs, activity messages, or notifications.


Before writing code, inspect the SRS and record:

- Feature requirement ID, such as `FR-03`, `FR-04`, or `FR-12`.
- HTTP method and full route, including `/api/v1` prefix if the project uses versioned routes.
- Required authentication state: anonymous, authenticated, project member, owner, admin, or system admin.
- RBAC permission from the security matrix.
- Request DTO fields and validation rules.
- Response DTO shape and HTTP status codes.
- Error codes and safe error details.
- Whether the endpoint is synchronous or must create an async job.
- Whether the operation must write an activity log or notification.
- Whether the operation touches Git or repository state and therefore requires a lock.

Do not invent a new route or response shape if the endpoint exists in `05-api.md`. If the SRS is missing or ambiguous, implement the smallest consistent endpoint and add a TODO or update request for the SRS/API spec.


Place request contracts at the API boundary, for example:

- `GodForge.Api/Contracts/{Module}/Requests/`
- `GodForge.Api/Contracts/{Module}/Responses/`

Place application-facing DTOs or result models near the feature slice, for example:

- `GodForge.Application/Features/{Module}/DTOs/`
- `GodForge.Application/Features/{Module}/{UseCase}/`

Use immutable records unless mutation is required.


Avoid over-posting by only exposing fields the client is allowed to set.


Create a validator for every request body or complex query model.


Validation failures must map to the standard error response and must not leak internal implementation details.


Use a command for write operations and a query for read operations. Include actor context explicitly.


The handler should:

- Check RBAC/ownership in the Application layer.
- Validate domain invariants and uniqueness rules.
- Call the correct domain service or repository.
- Write activity logs for state-changing operations.
- Return typed failures with stable error codes.
- Avoid direct HTTP concepts unless the project has an established application result abstraction.


Controller actions should bind input, call MediatR, and convert the result to the standardized API response.


Use `[Authorize]` for authenticated endpoints, but do not rely only on controller attributes for project-level authorization. Project-level RBAC must be enforced in the application use case because it needs project membership and target-resource context.


Successful single-resource response:


Successful paginated response:


Error response:


Error details must be safe for clients. Do not include stack traces, raw SQL, full Git command output, credential values, or internal filesystem paths.


Use stable, SRS-aligned error codes. Prefer module-prefixed codes when available.

Common mappings:

| Case | HTTP status | Example code |
| --- | ---: | --- |
| Validation failure | 400 | `VALIDATION_ERROR` |
| Missing or invalid token | 401 | `UNAUTHORIZED` |
| Authenticated but insufficient permission | 403 | `FORBIDDEN` |
| Resource not found or inaccessible | 404 | `PROJECT_NOT_FOUND` |
| Duplicate or conflicting state | 409 | `PROJECT_NAME_EXISTS` |
| Repository locked or concurrent Git operation | 409 | `REPOSITORY_LOCKED` |
| Async job accepted | 202 | none |
| Unexpected safe server error | 500 | `INTERNAL_ERROR` |

For security-sensitive resources, follow the project convention on whether to return `403` or mask existence with `404`.


Use the RBAC matrix from `06-security.md`.

General pattern:

- System admin may bypass project-level restrictions when SRS allows it.
- project_owner/admin can manage settings, members, and repository configuration.
- Developer can perform allowed Git operations and trigger permitted parse/analyze operations.
- Reviewer and viewer are read-oriented unless the SRS explicitly grants write permissions.
- Notification ownership must be checked against the actor.

Example application-layer check:


Do not put project membership queries inside the controller.


For list endpoints, use a query model rather than many loose parameters when filters grow.


Rules:

- Default `page` to `1`.
- Default `pageSize` to `20`.
- Cap `pageSize` at `100` unless the SRS says otherwise.
- Validate sort fields against an allow-list.
- Apply permission filters before returning results.


Endpoints that trigger clone, parse, analyze, diff, preview, or other heavy work must not run the work inside the request.

Return `202 Accepted` with a job DTO.


Job-producing handlers should:

- Check permission before creating the job.
- Use an idempotency key when the SRS or workflow requires retry safety.
- Create a `jobs` record with type, status, progress, and error fields.
- Publish a RabbitMQ message after persisting the job.
- Return quickly and let the worker update progress, heartbeat, timeout, and final status.


For repository and Git operations:

- Use Git Service or the approved Git engine only.
- Lock the repository before operations that can conflict, such as clone, fetch, pull, merge, commit, checkout, analyze working tree, or branch mutation.
- Never store or return Git credentials in plaintext.
- Sanitize command output before returning it to the client.
- Convert known Git failures into stable API errors, such as rejected push, merge conflict, dirty working tree, invalid branch, or repository locked.
- Write activity logs for successful and failed write operations.


Write activity logs for important state changes, including:

- Project create, update, delete, restore.
- Member invite, role change, member removal.
- Repository connect, clone, fetch.
- Git commit, push, pull, merge, checkout, branch changes.
- Analyze, parse, diff, and preview job creation/completion/failure.
- Settings changes.

Activity logs must include actor, action, target type, target id, status, timestamp, and correlation id. Failed operations that matter operationally should also be logged with a safe error code.

Create notifications only when the workflow requires them, and ensure the recipient is authorized to see the linked object.


Every request should carry or create a correlation id. Ensure errors and important operational events can be traced through logs and job records.

For endpoint code and handlers:

- Use structured logs.
- Include correlation id, actor id, project id, job id, and target id when safe.
- Do not log secrets, tokens, credentials, raw authorization headers, or sensitive files.
- Preserve cancellation token usage through async calls.


Add or update tests for:

- Route, method, status codes, and response format.
- Request validation errors.
- Unauthorized and forbidden access.
- Project-level RBAC for each role in the SRS matrix.
- Success path.
- Not found and conflict cases.
- Activity log creation for write operations.
- Job creation and queue publication for async endpoints.
- Repository lock behavior for Git operations.


