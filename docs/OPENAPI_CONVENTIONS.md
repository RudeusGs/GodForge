# OpenAPI and API Conventions

## Versioning

All public product APIs use `/api/v1`. Provider webhooks use explicit provider routes and signature authentication.

## Envelopes

Success:

```json
{
  "data": {},
  "meta": { "correlationId": "..." }
}
```

Paged success:

```json
{
  "data": [],
  "meta": {
    "correlationId": "...",
    "page": 1,
    "pageSize": 20,
    "totalCount": 42,
    "nextCursor": null
  }
}
```

Error:

```json
{
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "The project was not found.",
    "correlationId": "...",
    "details": null
  }
}
```

## Rules

- JSON uses camelCase.
- Timestamps are UTC ISO-8601.
- Identifiers are UUIDs unless an external provider identifier is explicitly named.
- Write requests use request DTOs; domain entities are never bound or returned.
- Pagination defaults to 20 and caps at 100 unless a module documents a smaller cap.
- File/tree/graph endpoints define strict size and node limits.
- `202 Accepted` returns a job summary and `Location` where practical.
- `Idempotency-Key` is supported for selected create/export/trigger endpoints.
- `ETag` may be used for immutable revision resources and safe optimistic updates.

## Status mapping

- 200: successful read/action.
- 201: resource created.
- 202: durable asynchronous work accepted.
- 204: successful action with no body.
- 400: validation or malformed request.
- 401: missing/invalid authentication or provider signature.
- 403: authenticated but forbidden when existence disclosure is acceptable.
- 404: absent or intentionally masked resource.
- 409: state conflict, duplicate or lock conflict.
- 413: configured size/count quota exceeded.
- 422: syntactically valid but semantically unsupported input where appropriate.
- 429: rate limit or quota.
- 500/502/503/504: sanitized infrastructure/provider failure.

## Documentation requirement

Every endpoint must document authentication, permission, request/response schema, errors, rate limit, idempotency, audit event and sync/async behavior.
