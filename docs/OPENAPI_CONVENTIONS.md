# OpenAPI / Swagger Conventions

## Route Structure
- All routes must be prefixed with `/api/v1/`.
- Project collection routes use plural nouns (e.g., `/api/v1/projects`).
- **Exception**: Since MVP has one primary repository per project, the one-to-one project repository route uses `/api/v1/projects/{projectId}/repository` (singular).
- Collection-like nested resources use plural nouns (e.g., `/api/v1/projects/{projectId}/members`).
- Route parameter naming should prefer `{projectId}` over `{id}` in docs unless the endpoint is generic.

## Request/Response Envelopes
- Successful responses return `200 OK` or `201 Created` with a standardized envelope:
```json
{
  "data": { ... },
  "meta": { ... } // optional
}
```
- Long-running async jobs must return `202 Accepted`:
```json
{
  "data": { "jobId": "uuid" }
}
```

## Error Responses
- Errors must use standard HTTP status codes (`400`, `401`, `403`, `404`, `409`, `500`).
- The response body must be a structured error object:
```json
{
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "The project was not found.",
    "correlationId": "xyz",
    "details": []
  }
}
```

## OpenAPI Generation
- Use Swashbuckle / NSwag to auto-generate the OpenAPI spec.
- All endpoints must have XML documentation `/// <summary>` tags.
- Endpoints must declare explicit `ProducesResponseType` for all expected status codes.
