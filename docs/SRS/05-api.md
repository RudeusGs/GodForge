# API Contract — Blueprint

All routes use `/api/v1`. Success responses use `{ data, meta }`; errors use `{ error: { code, message, correlationId, details } }`.

## Repository

```http
POST /api/v1/projects/{projectId}/repository/link
GET  /api/v1/projects/{projectId}/repository
POST /api/v1/projects/{projectId}/repository/analyze
POST /api/v1/projects/{projectId}/repository/hosted
POST /api/v1/projects/{projectId}/repository/sync
GET  /api/v1/projects/{projectId}/repository/branches
GET  /api/v1/projects/{projectId}/repository/commits
GET  /api/v1/projects/{projectId}/repository/tree
GET  /api/v1/projects/{projectId}/repository/blob
```

Heavy operations return `202` with `jobId`. Blob reads are text-only, path-normalized and size-limited.

## Revision and report

```http
GET  /api/v1/projects/{projectId}/revisions
GET  /api/v1/projects/{projectId}/revisions/{commitSha}
GET  /api/v1/projects/{projectId}/revisions/{commitSha}/health
GET  /api/v1/projects/{projectId}/revisions/{commitSha}/ai-report
POST /api/v1/projects/{projectId}/revisions/{commitSha}/analyze
```

## Webhooks

Provider-specific webhook endpoints validate signature before reading payload and use provider event ID/repository/commit as idempotency keys. They do not use end-user JWT authentication.
