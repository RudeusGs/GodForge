# 5. API Contract Catalog

All public routes use `/api/v1` and conventions in `../OPENAPI_CONVENTIONS.md`. Route existence does not make a feature implementation-ready; the module must also satisfy Definition of Ready.

## 5.1 Contract index

Implementation-ready M1 contracts:

- `05-api-contracts/auth.md`
- `05-api-contracts/organizations.md`
- `05-api-contracts/projects.md`

Later milestone routes remain catalog entries until their detailed contract is added.

## 5.2 Identity and sessions - M1

```http
POST   /auth/register/send-otp
POST   /auth/register
POST   /auth/login
POST   /auth/refresh
POST   /auth/logout
POST   /auth/forgot-password
POST   /auth/reset-password
GET    /users/me
GET    /users/me/sessions
DELETE /users/me/sessions/{sessionId}
```

## 5.3 Organizations - M1

```http
GET    /organizations
POST   /organizations
GET    /organizations/{organizationId}
PATCH  /organizations/{organizationId}
DELETE /organizations/{organizationId}
POST   /organizations/{organizationId}/transfer-ownership

GET    /organizations/{organizationId}/members
PATCH  /organizations/{organizationId}/members/{userId}
DELETE /organizations/{organizationId}/members/{userId}

GET    /organizations/{organizationId}/invitations
POST   /organizations/{organizationId}/invitations
DELETE /organizations/{organizationId}/invitations/{invitationId}
POST   /organization-invitations/accept
```

M1 does not create a project membership for a user who is not an active organization member. External users first join through an organization invitation.

## 5.4 Projects and project members - M1

```http
GET    /projects
GET    /organizations/{organizationId}/projects
POST   /organizations/{organizationId}/projects
GET    /projects/{projectId}
PATCH  /projects/{projectId}
DELETE /projects/{projectId}
POST   /projects/{projectId}/restore
POST   /projects/{projectId}/transfer-ownership

GET    /projects/{projectId}/members
POST   /projects/{projectId}/members
PATCH  /projects/{projectId}/members/{userId}
DELETE /projects/{projectId}/members/{userId}

GET    /projects/{projectId}/settings
PUT    /projects/{projectId}/settings
```

OrganizationOwner/Admin may list minimal administration metadata for organization projects. Project content requires an active project role.

## 5.5 Repository and revisions

```http
POST /projects/{projectId}/repository/link
POST /projects/{projectId}/repository/hosted
GET  /projects/{projectId}/repository
PATCH/DELETE /projects/{projectId}/repository
POST /projects/{projectId}/repository/sync
POST /projects/{projectId}/repository/analyze
GET  /projects/{projectId}/repository/branches
GET  /projects/{projectId}/repository/commits
GET  /projects/{projectId}/repository/tree?commitSha=&path=
GET  /projects/{projectId}/repository/blob?commitSha=&path=
GET  /projects/{projectId}/revisions
GET  /projects/{projectId}/revisions/{commitSha}
```

Heavy operations return `202` with job summary. Tree/blob endpoints normalize path, require commit SHA, cap results and never return binary bytes.

## 5.6 Analysis read models

```http
GET  /projects/{projectId}/revisions/{sha}/validation
GET  /projects/{projectId}/revisions/{sha}/scenes
GET  /projects/{projectId}/revisions/{sha}/scenes/{sceneId}
GET  /projects/{projectId}/revisions/{sha}/assets
GET  /projects/{projectId}/revisions/{sha}/graph
GET  /projects/{projectId}/revisions/{sha}/health
GET  /projects/{projectId}/revisions/{sha}/ai-advisory
POST /projects/{projectId}/revisions/{sha}/ai-advisory
POST /projects/{projectId}/revisions/compare
GET  /projects/{projectId}/comparisons/{comparisonId}
```

## 5.7 Findings and collaboration

```http
GET    /projects/{projectId}/findings
GET    /projects/{projectId}/findings/{findingKey}
POST   /projects/{projectId}/findings/{findingKey}/comments
POST   /projects/{projectId}/findings/{findingKey}/assignment
PATCH  /projects/{projectId}/findings/{findingKey}/status
POST   /projects/{projectId}/findings/{findingKey}/suppressions
DELETE /projects/{projectId}/findings/{findingKey}/suppressions/{id}
```

Mutable collaboration state uses optimistic concurrency/version.

## 5.8 Asset Vault

```http
GET/POST /projects/{projectId}/assets
GET/PATCH/DELETE /projects/{projectId}/assets/{assetId}
POST /projects/{projectId}/assets/{assetId}/versions
GET  /projects/{projectId}/assets/{assetId}/versions
PUT  /projects/{projectId}/assets/{assetId}/permissions
POST /projects/{projectId}/assets/{assetId}/download
GET/PUT /projects/{projectId}/asset-manifest
POST /projects/{projectId}/asset-manifest/validate
```

Upload may use multipart or a pre-signed upload session. Download authorizes at request time and returns a short-lived URL, not bucket credentials.

## 5.9 Jobs, dashboard, search, notifications and reports

```http
GET  /projects/{projectId}/jobs
GET  /projects/{projectId}/jobs/{jobId}
POST /projects/{projectId}/jobs/{jobId}/cancel
POST /projects/{projectId}/jobs/{jobId}/retry
GET  /projects/{projectId}/dashboard
GET  /projects/{projectId}/activities
GET  /search
GET  /notifications
POST /notifications/{id}/read
GET/PUT /users/me/notification-preferences
POST /projects/{projectId}/reports
GET  /projects/{projectId}/reports
GET  /projects/{projectId}/reports/{reportId}
POST /projects/{projectId}/reports/{reportId}/download
```

## 5.10 Webhooks and operations

```http
POST /webhooks/forgejo
POST /webhooks/git/{provider}
GET  /admin/operations/jobs
GET  /admin/operations/dead-letters
POST /admin/operations/dead-letters/{id}/requeue
```

Webhooks use provider signatures and replay protection, not user JWT. Admin operations require SystemAdmin and audit reason.

## 5.11 Endpoint specification requirement

Before implementation, every endpoint defines:

- authentication and permission key;
- request/response DTO;
- validation and normalization;
- status and stable error codes;
- sync/async behavior;
- transaction and concurrency behavior;
- idempotency behavior;
- rate limit/quota;
- audit/security event;
- test IDs and observability.
