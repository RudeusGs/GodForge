# Error Codes

GodForge uses explicit `SCREAMING_SNAKE_CASE` error codes mapped from domain/infrastructure exceptions. Do not return raw stack traces or internal exception details.

## Authentication & Authorization
- `AUTH_INVALID_CREDENTIALS`
- `AUTH_TOKEN_EXPIRED`
- `AUTH_UNAUTHORIZED`
- `AUTH_FORBIDDEN`

## Users & Projects
- `USER_NOT_FOUND`
- `PROJECT_NOT_FOUND`
- `PROJECT_MEMBER_EXISTS`
- `PROJECT_MEMBER_NOT_FOUND`

## Repositories & Git
- `REPOSITORY_NOT_FOUND`
- `GIT_CLONE_FAILED`
- `GIT_AUTH_FAILED`
- `GIT_WORKSPACE_LOCKED`

## Async Jobs
- `JOB_NOT_FOUND`
- `JOB_CANCELED`
- `JOB_TIMEOUT`
- `JOB_FAILED`

## Validation
- `VALIDATION_ERROR` (used for 400 Bad Request with a details array)
- `RESOURCE_NOT_FOUND` (Generic 404 fallback)
- `INTERNAL_SERVER_ERROR` (Generic 500 fallback)
