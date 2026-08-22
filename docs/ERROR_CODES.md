# Error Code Catalog

Public errors use stable `SCREAMING_SNAKE_CASE`, safe messages and correlation IDs. Provider payloads, stack traces, SQL, secrets and workspace paths are never returned.

## Common

| Code | HTTP/job state | Meaning |
|---|---:|---|
| `VALIDATION_ERROR` | 400 | Request validation failed. |
| `UNAUTHORIZED` | 401 | Authentication is missing or invalid. |
| `SECURITY_FORBIDDEN` | 403 | Actor lacks permission. |
| `RESOURCE_NOT_FOUND` | 404 | Resource is absent or intentionally masked. |
| `CONCURRENCY_CONFLICT` | 409 | Version/state changed concurrently. |
| `RATE_LIMIT_EXCEEDED` | 429 | Request or quota limit exceeded. |
| `INTERNAL_SERVER_ERROR` | 500 | Sanitized unexpected failure. |
| `DEPENDENCY_UNAVAILABLE` | 503 | Required dependency is unavailable. |

## Identity and tenancy

| Code | Status | Meaning |
|---|---:|---|
| `AUTH_INVALID_CREDENTIALS` | 401 | Login failed. |
| `AUTH_ACCOUNT_DISABLED` | 403 | Account disabled. |
| `AUTH_ACCOUNT_LOCKED` | 403 | Temporary security lock. |
| `AUTH_EMAIL_EXISTS` | 409 | Email already registered. |
| `AUTH_OTP_INVALID` | 400 | OTP invalid. |
| `AUTH_OTP_EXPIRED` | 400 | OTP expired. |
| `AUTH_TOKEN_EXPIRED` | 401 | Access/refresh token expired. |
| `AUTH_TOKEN_REVOKED` | 401 | Session/token revoked. |
| `AUTH_REFRESH_REUSED` | 401 | Rotated refresh token replay detected. |
| `AUTH_RESET_TOKEN_INVALID` | 400 | Reset token invalid/expired. |
| `AUTH_SESSION_LIMIT_REACHED` | 409 | Active-session limit reached; revoke an existing session before signing in again. |
| `ORGANIZATION_NOT_FOUND` | 404 | Organization absent/inaccessible. |
| `ORGANIZATION_SLUG_EXISTS` | 409 | Slug already exists. |
| `ORGANIZATION_SLUG_RESERVED` | 400 | Organization slug is reserved for platform routing or administration. |
| `ORGANIZATION_NOT_ACTIVE` | 409 | Organization lifecycle state blocks the operation. |
| `ORGANIZATION_QUOTA_EXCEEDED` | 429 | Organization quota reached. |
| `ORGANIZATION_INVITATION_QUOTA_EXCEEDED` | 429 | Pending organization invitation quota reached. |
| `PROJECT_QUOTA_EXCEEDED` | 429 | Project quota reached for the organization. |
| `PROJECT_NOT_FOUND` | 404 | Project absent/inaccessible. |
| `PROJECT_NAME_EXISTS` | 409 | Project name conflict in organization. |
| `PROJECT_SLUG_EXISTS` | 409 | Project slug conflict in organization. |
| `PROJECT_NOT_ARCHIVED` | 409 | Restore requires an archived project. |
| `PROJECT_SETTINGS_NOT_FOUND` | 404 | Project settings row is absent. |
| `PROJECT_ARCHIVED` | 409 | Mutation blocked for archived project. |
| `MEMBERSHIP_NOT_FOUND` | 404 | Membership absent or inaccessible. |
| `MEMBERSHIP_ALREADY_EXISTS` | 409 | An active membership already exists. |
| `LAST_OWNER_REQUIRED` | 409 | Operation would leave no owner. |
| `INVITE_INVALID_OR_EXPIRED` | 401 | Invitation invalid, expired, revoked or does not match the verified actor email. |
| `INVITE_ALREADY_PENDING` | 409 | An active organization invitation already exists for the normalized email. |
| `IDEMPOTENCY_KEY_REUSED` | 409 | Idempotency key was reused for a different or concurrent request. |
| `IDEMPOTENCY_RESOURCE_UNAVAILABLE` | 409 | The resource previously recorded for the idempotency key is no longer available. |

## Repository and Git

| Code | Status | Meaning |
|---|---:|---|
| `REPOSITORY_NOT_CONNECTED` | 404 | Project has no active repository. |
| `REPOSITORY_ALREADY_CONNECTED` | 409 | Active repository already exists. |
| `REPOSITORY_INVALID_URL` | 400 | Remote URL unsupported or malformed. |
| `REPOSITORY_PROVIDER_INVALID` | 400 | Repository provider value is unsupported. |
| `REPOSITORY_REMOTE_FORBIDDEN` | 403 | Remote violates network/allow-list policy. |
| `REPOSITORY_CREDENTIAL_INVALID` | 401 | Git credential rejected. |
| `REPOSITORY_SIZE_LIMIT_EXCEEDED` | 413 | Repository quota exceeded. |
| `REPOSITORY_FILE_LIMIT_EXCEEDED` | 413 | File-count quota exceeded. |
| `REPOSITORY_LOCKED` | 409 | Another job owns repository lock. |
| `GIT_AUTH_FAILED` | 401 | Provider authentication failed. |
| `GIT_NOT_FOUND` | 404 | Repository/ref/commit not found. |
| `GIT_COMMAND_TIMEOUT` | 504 | Git operation timed out. |
| `GIT_PROVIDER_UNAVAILABLE` | 503 | Provider unavailable. |
| `GIT_BRANCH_PROTECTED` | 409 | Branch policy rejects mutation. |
| `FORGEJO_PROVISION_FAILED` | job failed | Hosted repository provisioning failed. |
| `FORGEJO_PERMISSION_SYNC_FAILED` | job retry/failed | Permission synchronization failed. |
| `WEBHOOK_SIGNATURE_INVALID` | 401 | Signature invalid. |
| `WEBHOOK_REPLAY_REJECTED` | 401 | Replay/expired event rejected. |
| `WEBHOOK_DUPLICATE` | 202 | Duplicate event ignored idempotently. |
| `WEBHOOK_REPOSITORY_MISMATCH` | 400 | Event does not match configured repository. |

## Godot validation and parsing

| Code | Status | Meaning |
|---|---:|---|
| `GODOT_PROJECT_FILE_MISSING` | validation invalid | Root `project.godot` missing. |
| `GODOT_PROJECT_FILE_INVALID` | validation invalid | Marker malformed. |
| `GODOT_VERSION_UNSUPPORTED` | validation invalid/warning | Version not supported by profile. |
| `GODOT_PATH_INVALID` | validation invalid | Unsafe/invalid normalized path. |
| `GODOT_SYMLINK_ESCAPE` | validation invalid | Symlink resolves outside workspace. |
| `GODOT_SECRET_DETECTED` | validation suspicious | Potential secret detected; value redacted. |
| `GODOT_DANGEROUS_FILE` | validation suspicious/invalid | Executable or disallowed content. |
| `GODOT_TEXT_FILE_TOO_LARGE` | validation/finding | Text parser limit exceeded. |
| `GODOT_RESOURCE_MISSING` | finding | Reference target missing. |
| `PARSER_REQUIRED` | 409 | Read model requires parser output. |
| `PARSER_FAILED` | job failed | Parser stage failed. |
| `PARSER_FILE_READ_FAILED` | diagnostic | File could not be safely read. |
| `PARSER_VERSION_INCOMPATIBLE` | 409 | Requested comparison requires compatible/recomputed metadata. |

## Analysis and AI

| Code | Status | Meaning |
|---|---:|---|
| `ANALYSIS_NOT_FOUND` | 404 | Required analysis absent. |
| `ADVISORY_NOT_FOUND` | 404 | AI advisory absent or inaccessible. |
| `GRAPH_NOT_FOUND` | 404 | Dependency graph absent or inaccessible. |
| `ANALYSIS_INCOMPLETE` | 409 | Required stages incomplete. |
| `ANALYSIS_IDENTITY_CONFLICT` | 409 | Versions/input do not match expected identity. |
| `INCREMENTAL_BASELINE_UNAVAILABLE` | fallback | Full analysis selected. |
| `INCREMENTAL_FALLBACK_REQUIRED` | fallback | Safety condition requires full analysis. |
| `HEALTH_REPORT_NOT_FOUND` | 404 | Health report absent. |
| `FINDING_NOT_FOUND` | 404 | Finding absent/inaccessible. |
| `FINDING_STATE_INVALID` | 409 | Invalid collaboration transition. |
| `AI_PROVIDER_NOT_CONFIGURED` | degraded | AI disabled/misconfigured. |
| `AI_PROVIDER_TIMEOUT` | degraded | Provider timed out. |
| `AI_PROVIDER_UNAVAILABLE` | degraded | Provider unavailable. |
| `AI_CONTEXT_LIMIT_EXCEEDED` | degraded/400 | Approved context exceeds budget. |
| `AI_REDACTION_BLOCKED` | degraded | Context could not be safely prepared. |
| `AI_RESPONSE_EMPTY` | degraded | Empty result. |
| `AI_RESPONSE_INVALID` | degraded | Schema validation failed. |

## Asset Vault and reports

| Code | Status | Meaning |
|---|---:|---|
| `ASSET_NOT_FOUND` | 404 | Asset absent/inaccessible. |
| `ASSET_VERSION_NOT_FOUND` | 404 | Version absent. |
| `ASSET_SIZE_LIMIT_EXCEEDED` | 413 | Upload exceeds quota. |
| `ASSET_TYPE_INVALID` | 400/422 | Type/magic unsupported or mismatched. |
| `ASSET_QUARANTINED` | 409/403 | Asset not available due to scan state. |
| `ASSET_CHECKSUM_MISMATCH` | 409 | Bytes do not match manifest/version checksum. |
| `ASSET_PERMISSION_DENIED` | 404/403 | Asset policy denies access. |
| `ASSET_MANIFEST_INVALID` | 400/409 | Manifest inconsistent or malformed. |
| `SIGNED_URL_EXPIRED` | 401/403 | Signed object access expired. |
| `REPORT_NOT_FOUND` | 404 | Report absent/inaccessible. |
| `REPORT_GENERATION_FAILED` | job failed | Export failed. |
| `ARTIFACT_UNAVAILABLE` | 503/404 | Object unavailable or purged. |

## Jobs and operations

| Code | Status | Meaning |
|---|---:|---|
| `JOB_NOT_FOUND` | 404 | Job absent/inaccessible. |
| `JOB_NOT_CANCELLABLE` | 409 | Job terminal or cancellation unsafe. |
| `JOB_DUPLICATE_ACTIVE` | 200/202 | Equivalent active job returned. |
| `JOB_PUBLISH_PENDING` | 202 | Durable job exists; outbox not yet dispatched. |
| `JOB_TRANSIENT_FAILURE` | retrying | Retryable failure. |
| `JOB_TIMEOUT` | timeout | Time budget exceeded. |
| `JOB_CANCELLED` | cancelled | Cooperative cancellation completed. |
| `JOB_DEAD_LETTERED` | dead-lettered | Poison/retry-exhausted message. |
| `WORKER_MESSAGE_INVALID` | dead-lettered | Invalid message schema/identity. |
| `WORKSPACE_CLEANUP_FAILED` | warning/failed | Temporary cleanup failed and requires retry/alert. |
| `OUTBOX_DISPATCH_FAILED` | retrying | Durable event not yet published. |
