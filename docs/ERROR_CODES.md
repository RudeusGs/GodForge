# Error Codes

GodForge uses explicit `SCREAMING_SNAKE_CASE` error codes mapped from domain/infrastructure exceptions. Do not return raw stack traces or internal exception details.

| Code | Status | Message | Module | When to use | Details Allowed? |
| --- | --- | --- | --- | --- | --- |
| `AUTH_INVALID_CREDENTIALS` | 401 | Invalid email or password. | auth | Login failure. | No |
| `AUTH_ACCOUNT_LOCKED` | 403 | Account is locked. | auth | Too many failed logins. | No |
| `AUTH_ACCOUNT_DISABLED` | 403 | Account is disabled. | auth | Admin disabled account. | No |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token expired. | auth | JWT expiration. | No |
| `AUTH_TOKEN_REVOKED` | 401 | Token was revoked. | auth | Refresh token revoked. | No |
| `AUTH_INVALID_TOKEN` | 401 | Invalid token. | auth | Malformed token. | No |
| `USER_NOT_FOUND` | 404 | User not found. | user | Querying non-existent user. | No |
| `USER_EMAIL_ALREADY_EXISTS` | 409 | Email already in use. | user | Registration collision. | No |
| `PROJECT_NOT_FOUND` | 404 | Project not found. | project | Querying non-existent project. | No |
| `PROJECT_NAME_EXISTS` | 409 | Project name exists. | project | Creating project with existing name. | No |
| `PROJECT_ALREADY_DELETED` | 409 | Project already deleted. | project | Deleting a deleted project. | No |
| `PROJECT_RESTORE_EXPIRED` | 400 | Project cannot be restored. | project | Restore window passed. | No |
| `PROJECT_MEMBER_EXISTS` | 409 | User is already a member. | member | Adding existing member. | No |
| `PROJECT_MEMBER_NOT_FOUND` | 404 | Member not found. | member | Modifying non-member. | No |
| `PROJECT_LAST_OWNER_CANNOT_BE_REMOVED` | 400 | Cannot remove the last owner. | member | Deleting last project_owner. | No |
| `REPOSITORY_NOT_CONNECTED` | 404 | No repository connected. | repository | Accessing unlinked repo. | No |
| `REPOSITORY_ALREADY_CONNECTED` | 409 | Repository already connected. | repository | Re-linking repo. | No |
| `REPOSITORY_INVALID_URL` | 400 | Invalid repository URL. | repository | Bad remote URL format. | Yes |
| `REPOSITORY_CREDENTIAL_INVALID` | 401 | Invalid repository credentials. | repository | Git auth failed with PAT. | No |
| `GIT_AUTH_FAILED` | 401 | Git authentication failed. | git | Clone/Fetch auth error. | No |
| `GIT_WORKSPACE_LOCKED` | 409 | Workspace is locked. | git | Concurrent Git operations. | No |
| `GIT_CONFLICT` | 409 | Git conflict occurred. | git | Merge/Pull conflict. | Yes |
| `GIT_PUSH_REJECTED` | 409 | Git push was rejected. | git | Push non-fast-forward. | Yes |
| `GIT_BRANCH_NOT_FOUND` | 404 | Git branch not found. | git | Checkout invalid branch. | No |
| `METADATA_NOT_READY` | 409 | Metadata is not ready. | metadata | Accessing scenes before parse. | No |
| `PARSER_FAILED` | 500 | Parser failed. | parser | Godot parsing crashed. | Yes |
| `HEALTH_REPORT_NOT_FOUND` | 404 | Health report not found. | health | Querying missing report. | No |
| `DIFF_NOT_FOUND` | 404 | Diff not found. | diff | Querying missing diff. | No |
| `JOB_NOT_FOUND` | 404 | Job not found. | job | Querying missing job. | No |
| `JOB_NOT_CANCELLABLE` | 400 | Job cannot be cancelled. | job | Cancelling completed job. | No |
| `JOB_TIMEOUT` | 504 | Job timed out. | job | Worker exceeded time limit. | No |
| `JOB_DEAD_LETTERED` | 500 | Job moved to DLQ. | job | Poison message. | Yes |
| `SECURITY_FORBIDDEN` | 403 | Forbidden access. | security | RBAC check failed. | No |
| `VALIDATION_ERROR` | 400 | Validation failed. | validation | FluentValidation failed. | Yes |
| `RESOURCE_NOT_FOUND` | 404 | Resource not found. | infrastructure | Generic 404. | No |
| `INTERNAL_SERVER_ERROR` | 500 | Internal server error. | infrastructure | Unhandled exceptions. | No |


