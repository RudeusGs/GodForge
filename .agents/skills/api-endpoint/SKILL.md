---
name: api-endpoint
description: Add or modify ASP.NET Core API endpoints and contracts.
---

# Api Endpoint

## Use when

Add or modify ASP.NET Core API endpoints and contracts.

## Required reading

- `docs/SRS/05-api.md`
- Relevant functional SRS
- `docs/OPENAPI_CONVENTIONS.md`
- `docs/ERROR_CODES.md`
- `docs/RBAC_MATRIX.md`
- `docs/API_CHANGE_CHECKLIST.md`

## Workflow

1. Confirm method, route, actor, permission and sync/async behavior.
2. Define request/response DTOs and validation.
3. Implement command/query and Application authorization.
4. Keep controller thin and map typed result to standard envelope.
5. Add rate limit/idempotency/activity/audit as required.
6. Add OpenAPI and integration tests.

## Mandatory checks

- No domain entity in request/response.
- Pagination and limits for lists/files/graphs.
- 202 plus job for heavy work.
- Safe stable errors and correlation ID.
- Cross-tenant test.

## Forbidden

- No business rules or database/provider access in controller.
- No project authorization based only on `[Authorize]`.
- No raw exception/provider output.

## Completion output

List route, permission, request/response, errors, tests and docs updated.
