# Error Codes — Blueprint

Errors use `SCREAMING_SNAKE_CASE`, a safe message, correlation ID and optional validation details. Stack traces, credentials, workspace paths and provider payloads are never returned.

## Authentication and authorization

| Code | Status | Meaning |
|---|---:|---|
| `AUTH_INVALID_CREDENTIALS` | 401 | Email/password is invalid. |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token expired. |
| `AUTH_TOKEN_REVOKED` | 401 | Token/session was revoked. |
| `SECURITY_FORBIDDEN` | 403 | Actor lacks the required permission. |

## Project and repository

| Code | Status | Meaning |
|---|---:|---|
| `PROJECT_NOT_FOUND` | 404 | Project does not exist or is not visible. |
| `REPOSITORY_NOT_CONNECTED` | 404 | Project has no active repository. |
| `REPOSITORY_ALREADY_CONNECTED` | 409 | Project already has an active repository. |
| `REPOSITORY_INVALID_URL` | 400 | Clone URL is invalid or unsupported. |
| `REPOSITORY_PROVIDER_INVALID` | 400 | Provider/mode combination is invalid. |
| `REPOSITORY_CREDENTIAL_INVALID` | 401 | External Git credential is invalid. |
| `REPOSITORY_SIZE_LIMIT_EXCEEDED` | 413 | Repository exceeds configured quota. |
| `REPOSITORY_FILE_LIMIT_EXCEEDED` | 413 | Repository exceeds file-count quota. |
| `GIT_AUTH_FAILED` | 401 | Clone/fetch authentication failed. |
| `GIT_WORKSPACE_LOCKED` | 409 | Another mutation owns the repository lock. |
| `GIT_COMMAND_TIMEOUT` | 504 | Managed Git operation timed out. |
| `WEBHOOK_SIGNATURE_INVALID` | 401 | Provider webhook signature is invalid. |
| `WEBHOOK_DUPLICATE` | 202 | Event was already accepted and is idempotently ignored. |

## Parser, health and AI

| Code | Status | Meaning |
|---|---:|---|
| `GODOT_PROJECT_FILE_MISSING` | 200 finding | `project.godot` not found at repository root. |
| `PARSER_FAILED` | 500/job failed | Deterministic parser failed. |
| `PARSE_REQUIRED` | 409 | Requested read model requires parser output. |
| `HEALTH_REPORT_NOT_FOUND` | 404 | No health report exists for the revision. |
| `AI_PROVIDER_NOT_CONFIGURED` | 200 degraded | AI was requested but provider is disabled/misconfigured. |
| `AI_PROVIDER_TIMEOUT` | 200 degraded | Provider timed out; deterministic report remains valid. |
| `AI_PROVIDER_UNAVAILABLE` | 200 degraded | Provider transport failure. |
| `AI_RESPONSE_EMPTY` | 200 degraded | Provider returned no usable result. |
| `AI_RESPONSE_INVALID` | 200 degraded | Provider output failed JSON/schema validation. |

## Jobs and infrastructure

| Code | Status | Meaning |
|---|---:|---|
| `JOB_NOT_FOUND` | 404 | Job does not exist or is not visible. |
| `JOB_NOT_CANCELLABLE` | 400 | Job is already terminal. |
| `JOB_PUBLISH_FAILED` | 500 | Durable job was created but queue publish failed and job was marked failed. |
| `JOB_TRANSIENT_FAILURE` | job retrying | Retryable dependency failure. |
| `JOB_TIMEOUT` | 504/job timeout | Job exceeded its budget. |
| `JOB_DEAD_LETTERED` | 500/job terminal | Retry exhausted or message invalid. |
| `WORKER_MESSAGE_INVALID` | job dead-lettered | Message schema or required identifier is invalid. |
| `VALIDATION_ERROR` | 400 | Request validation failed. |
| `INTERNAL_SERVER_ERROR` | 500 | Sanitized unhandled error. |


## Additional authentication codes used by the current implementation

| Code | Status | Meaning |
|---|---:|---|
| `AUTH_ACCOUNT_DISABLED` | 403 | The account was disabled by an administrator. |
| `AUTH_ACCOUNT_LOCKED` | 403 | Login is temporarily locked after repeated failures. |
| `AUTH_EMAIL_EXISTS` | 409 | Registration email is already in use. |
| `AUTH_INVALID_RESET_TOKEN` | 400 | Password reset token is invalid or expired. |
| `AUTH_INVALID_TOKEN` | 401 | Token payload or signature is invalid. |
| `AUTH_INVALID_USER` | 401 | Token refers to a user that is no longer active. |
| `AUTH_OTP_EXPIRED` | 400 | Registration OTP has expired. |
| `AUTH_OTP_INVALID` | 400 | Registration OTP is invalid. |

## Deterministic Godot findings and processing failures

| Code | Status | Meaning |
|---|---:|---|
| `GODOT_RESOURCE_MISSING` | finding | A `res://` reference points to a missing file. |
| `GODOT_RESOURCE_PATH_INVALID` | finding | A parsed resource path attempts to escape the workspace root. |
| `GODOT_SCENE_NOT_FOUND` | finding | No `.tscn` scene was found. |
| `GODOT_SCENE_OVERSIZED` | finding | A scene exceeds the maintainability threshold. |
| `GODOT_TEXT_FILE_TOO_LARGE` | finding | A Godot text file exceeded the parser limit. |
| `PARSER_FILE_READ_FAILED` | finding | A Godot text file could not be decoded or read. |
| `REPOSITORY_ANALYSIS_FAILED` | job terminal | Repository analysis failed after retries were exhausted. |
| `AI_ANALYSIS_FAILED` | degraded | AI returned a failure not covered by a provider-specific code. |
| `AI_PROVIDER_REQUEST_FAILED` | degraded | AI rejected a non-retryable request. |
| `PROJECT_NAME_EXISTS` | 409 | The owner already has a project with the same name. |
