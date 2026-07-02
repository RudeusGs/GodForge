# 5. API

## 5.1 API Standards

GodForge uses a REST API implemented under the `/api/v1` prefix.

| Convention | Value |
| --- | --- |
| Base URL | `/api/v1` |
| Format | `application/json` |
| Authentication | `Authorization: Bearer {accessToken}` |
| Correlation | Header `X-Correlation-Id`; server generates if client does not send |
| Time format | ISO 8601 UTC |
| Pagination | Offset-based `page`, `pageSize` |
| Max pageSize | 100 |

## 5.2 Response Format

### Success

```json
{
  "data": {},
  "meta": {
    "correlationId": "abc-123"
  }
}
```

### Pagination

```json
{
  "data": [],
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8,
    "correlationId": "abc-123"
  }
}
```

### Error

```json
{
  "error": {
    "code": "PROJECT_NOT_FOUND",
    "message": "Project does not exist or you do not have access.",
    "correlationId": "abc-123",
    "details": [
      { "field": "name", "message": "Project name already exists." }
    ]
  }
}
```

### Async Job

```json
{
  "data": {
    "jobId": "uuid",
    "type": "parse",
    "status": "queued"
  },
  "meta": {
    "correlationId": "abc-123"
  }
}
```

Canonical job status values returned by async APIs and SignalR events: `queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`.

## 5.3 API Rules

- The API does not return stack traces, server workspace paths, credentials, tokens, or raw command output containing secrets.
- Validation errors return 400 with `details[]`.
- Permission denied returns 403; accessing resources outside of permissions may return 404 to avoid exposing their existence.
- Async operations return 202 when the job is created.
- Error codes use `SCREAMING_SNAKE_CASE` and are stable for UI/QA handling.

## 5.4 Endpoint Catalog

### Auth

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous | `email`, `password` | Access token, refresh token, expiresIn, user | `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED`, `ACCOUNT_DISABLED` |
| POST | `/api/v1/auth/refresh` | Refresh token | `refreshToken` | New access token, new refresh token | `TOKEN_EXPIRED`, `TOKEN_REVOKED` |
| POST | `/api/v1/auth/logout` | Authenticated | `refreshToken` | Logout success | `INVALID_TOKEN` |
| POST | `/api/v1/auth/setup-password` | Invite token | `token`, `password` | Account activated | `INVITE_TOKEN_EXPIRED`, `VALIDATION_ERROR` |

### Users

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/users` | `system_admin` | `page`, `pageSize`, `search`, `status` | User list | `FORBIDDEN` |
| POST | `/api/v1/users` | `system_admin` | `email`, `displayName`, `systemRole` | User invited | `EMAIL_ALREADY_EXISTS`, `VALIDATION_ERROR` |
| GET | `/api/v1/users/{id}` | `system_admin` or self | user id | User detail | `USER_NOT_FOUND`, `FORBIDDEN` |
| PUT | `/api/v1/users/{id}` | `system_admin` or self | displayName, avatarUrl | User updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| PUT | `/api/v1/users/{id}/status` | `system_admin` | status | User status updated | `USER_NOT_FOUND`, `VALIDATION_ERROR` |
| GET | `/api/v1/users/me` | Authenticated | none | Current user | `INVALID_TOKEN` |
| PUT | `/api/v1/users/me/settings` | Authenticated | theme, notification prefs | User settings updated | `VALIDATION_ERROR` |
| PUT | `/api/v1/users/me/password` | Authenticated | oldPassword, newPassword | Password changed | `INVALID_CREDENTIALS`, `VALIDATION_ERROR` |

### Projects

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects` | Authenticated | pagination, search, status | Projects visible to user | `FORBIDDEN` |
| POST | `/api/v1/projects` | Authenticated | name, description, godotVersion, visibility | Project created | `PROJECT_NAME_EXISTS`, `VALIDATION_ERROR` |
| GET | `/api/v1/projects/{projectId}` | Project member | project id | Project detail | `PROJECT_NOT_FOUND`, `FORBIDDEN` |
| PUT | `/api/v1/projects/{projectId}` | `project_admin+` | name, description, visibility, godotVersion | Project updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| DELETE | `/api/v1/projects/{projectId}` | `project_owner` | confirmation | Project soft-deleted | `FORBIDDEN`, `PROJECT_NOT_FOUND` |
| POST | `/api/v1/projects/{projectId}/restore` | `system_admin` | none | Project restored | `PROJECT_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/dashboard` | `viewer+` | optional time range | Dashboard summary | `PROJECT_NOT_FOUND`, `FORBIDDEN` |

### Members

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/members` | Project member | pagination | Member list | `FORBIDDEN` |
| POST | `/api/v1/projects/{projectId}/members` | `project_admin+` | email, role | Member invited/added | `USER_NOT_FOUND`, `ALREADY_MEMBER`, `FORBIDDEN` |
| PUT | `/api/v1/projects/{projectId}/members/{userId}` | `project_admin+` | role | Member role updated | `LAST_OWNER_CANNOT_BE_REMOVED`, `FORBIDDEN` |
| DELETE | `/api/v1/projects/{projectId}/members/{userId}` | `project_admin+` | none | Member removed | `LAST_OWNER_CANNOT_BE_REMOVED`, `FORBIDDEN` |

### Repositories

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{projectId}/repository` | `project_admin+` | remoteUrl, personalAccessToken, defaultBranch | Clone job | `INVALID_REPO_URL`, `REPO_ALREADY_CONNECTED` |
| GET | `/api/v1/projects/{projectId}/repository` | Project member | none | Repository detail, masked credential state | `REPO_NOT_CONNECTED` |
| PUT | `/api/v1/projects/{projectId}/repository/credential` | `project_admin+` | personalAccessToken | Credential updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| DELETE | `/api/v1/projects/{projectId}/repository` | `project_admin+` | confirmation | Repository disconnected | `REPO_LOCKED`, `FORBIDDEN` |
| POST | `/api/v1/projects/{projectId}/repository/sync` | `developer+` | branch/options | Fetch/sync job | `REPO_NOT_READY`, `REPO_LOCKED` |
| GET | `/api/v1/projects/{projectId}/repository/branches` | `viewer+` | none | Branch list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/repository/commits` | `viewer+` | branch, author, date range, pagination | Commit list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/repository/commits/{hash}` | `viewer+` | commit hash | Commit detail | `COMMIT_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/repository/commits/{hash}/diff` | `viewer+` | commit hash | File diff summary | `COMMIT_NOT_FOUND` |

### Scenes

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/scenes` | `viewer+` | search, page, pageSize | Scene list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/scenes/{sceneId}` | `viewer+` | scene id | Scene detail | `SCENE_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/scenes/{sceneId}/nodes` | `viewer+` | tree/flat, search, type | Scene node tree/list | `SCENE_NOT_FOUND` |

### Assets

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/assets` | `viewer+` | type, search, usage, page | Asset list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/assets/{assetId}` | `viewer+` | asset id | Asset detail and usage | `ASSET_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/scripts` | `viewer+` | search, page | Script list | `METADATA_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/resources` | `viewer+` | search, type, page | Resource list | `METADATA_NOT_READY` |

### Dependencies

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/dependencies` | `viewer+` | root, depth, type | Graph nodes/edges | `ANALYZE_REQUIRED`, `DEPENDENCY_ROOT_NOT_FOUND` |

### Health

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{projectId}/parse` | `developer+` | branch, commit, options | Parse job | `REPO_NOT_READY`, `REPO_NOT_CONNECTED` |
| POST | `/api/v1/projects/{projectId}/analyze` | `developer+` | branch, commit, options | Analyze job | `PARSE_REQUIRED`, `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/health` | `viewer+` | none | Latest health report | `HEALTH_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/health/history` | `viewer+` | pagination | Health report history | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/health/{reportId}/issues` | `viewer+` | severity, type, page | Health issues | `REPORT_NOT_FOUND` |

### Diff

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{projectId}/diff/scene` | `viewer+` | scenePath, commitA/branchA, commitB/branchB | Diff job or cached result | `REVISION_NOT_FOUND`, `PATH_NOT_ALLOWED`, `DIFF_PARSE_FAILED` |
| GET | `/api/v1/projects/{projectId}/diff/{diffId}` | `viewer+` | diff id | Diff result | `DIFF_NOT_FOUND` |

### Jobs

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/jobs` | Project member | type, status (`queued`, `running`, `retrying`, `completed`, `failed`, `cancelled`, `timeout`, `dead_lettered`), page | Job list | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/jobs/{jobId}` | Project member | job id | Job detail/progress | `JOB_NOT_FOUND` |
| POST | `/api/v1/projects/{projectId}/jobs/{jobId}/cancel` | `developer+` | none | Job cancelled | `JOB_NOT_CANCELLABLE`, `FORBIDDEN` |

### Notifications

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/notifications` | Authenticated | unread, page | Notification list | `FORBIDDEN` |
| GET | `/api/v1/notifications/unread-count` | Authenticated | none | Unread count | `FORBIDDEN` |
| PUT | `/api/v1/notifications/{id}/read` | Notification owner | notification id | Mark read result | `NOTIFICATION_NOT_FOUND` |
| PUT | `/api/v1/notifications/read-all` | Authenticated | none | Mark all read result | `FORBIDDEN` |

### Activities

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/activities` | Project member | action, user, page | Project activity log | `FORBIDDEN` |
| GET | `/api/v1/activities` | `system_admin` | action, project, user, page | System activity log | `FORBIDDEN` |

### Settings

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects/{projectId}/settings` | `project_admin+` | none | Project settings | `FORBIDDEN` |
| PUT | `/api/v1/projects/{projectId}/settings` | `project_admin+` | autoParseOnPush, autoAnalyzeOnParse, health schedule, notification prefs | Settings updated | `VALIDATION_ERROR`, `FORBIDDEN` |
| GET | `/api/v1/users/me/settings` | Authenticated | none | User settings | `FORBIDDEN` |
| PUT | `/api/v1/users/me/settings` | Authenticated | theme, notification prefs | Settings updated | `VALIDATION_ERROR` |

### Search

| Method | Path | Permission | Request body/query | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/search` | Authenticated | q, projectId, type, page, pageSize | Search results/facets | `VALIDATION_ERROR`, `FORBIDDEN` |

## 5.5 SignalR

Realtime hub: `/hubs/godforge`.

| Direction | Event | Payload | Description |
| --- | --- | --- | --- |
| Client -> Server | `JoinProject` | `{ "projectId": "uuid" }` | Subscribe to project updates. |
| Client -> Server | `LeaveProject` | `{ "projectId": "uuid" }` | Unsubscribe from project. |
| Server -> Client | `JobProgressUpdate` | jobId, progress, status | Update job progress. |
| Server -> Client | `JobCompleted` | jobId, type, result summary | Job completed. |
| Server -> Client | `JobFailed` | jobId, type, errorCode, message | Job failed. |
| Server -> Client | `NotificationReceived` | notification | New notification. |
| Server -> Client | `RepoStatusChanged` | repositoryId, status | Repository status changed. |
