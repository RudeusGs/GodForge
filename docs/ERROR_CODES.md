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

| INVALID_CREDENTIALS | 400 | Additional error code. | general | Missing definition. | Yes |
| ALREADY_MEMBER | 400 | Additional error code. | general | Missing definition. | Yes |
| PARSE_INVALID_SCENE | 400 | Additional error code. | general | Missing definition. | Yes |
| NO_STAGED_FILES | 400 | Additional error code. | general | Missing definition. | Yes |
| FORBIDDEN | 400 | Additional error code. | general | Missing definition. | Yes |
| CLONE_AUTH_FAILED | 400 | Additional error code. | general | Missing definition. | Yes |
| REPO_NOT_CONNECTED | 400 | Additional error code. | general | Missing definition. | Yes |
| ISSUE_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| ANALYZE_GRAPH_TOO_LARGE | 400 | Additional error code. | general | Missing definition. | Yes |
| DEPENDENCY_ROOT_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| SEARCH_INDEX_NOT_READY | 400 | Additional error code. | general | Missing definition. | Yes |
| REPORT_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| PREVIEW_GENERATION_FAILED | 400 | Additional error code. | general | Missing definition. | Yes |
| TOKEN_EXPIRED | 400 | Additional error code. | general | Missing definition. | Yes |
| REPO_NOT_READY | 400 | Additional error code. | general | Missing definition. | Yes |
| PARSE_FILE_TOO_LARGE | 400 | Additional error code. | general | Missing definition. | Yes |
| REPO_ALREADY_CONNECTED | 400 | Additional error code. | general | Missing definition. | Yes |
| TOKEN_REVOKED | 400 | Additional error code. | general | Missing definition. | Yes |
| BRANCH_EXISTS | 400 | Additional error code. | general | Missing definition. | Yes |
| CACHE_UNAVAILABLE | 400 | Additional error code. | general | Missing definition. | Yes |
| PROJECT_DELETED | 400 | Additional error code. | general | Missing definition. | Yes |
| COMMIT_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| ACCOUNT_LOCKED | 400 | Additional error code. | general | Missing definition. | Yes |
| PREVIEW_UNSUPPORTED | 400 | Additional error code. | general | Missing definition. | Yes |
| ANALYZE_INTERNAL_ERROR | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_MISSING_SCRIPT | 400 | Additional error code. | general | Missing definition. | Yes |
| ANALYZE_REQUIRED | 400 | Additional error code. | general | Missing definition. | Yes |
| DIRTY_WORKTREE | 400 | Additional error code. | general | Missing definition. | Yes |
| REPO_SIZE_EXCEEDED | 400 | Additional error code. | general | Missing definition. | Yes |
| REVISION_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| CURRENT_BRANCH_DELETE_DENIED | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_MISSING_IMPORT | 400 | Additional error code. | general | Missing definition. | Yes |
| BRANCH_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| CLONE_NETWORK_ERROR | 400 | Additional error code. | general | Missing definition. | Yes |
| UNAUTHORIZED | 400 | Additional error code. | general | Missing definition. | Yes |
| COMMIT_MESSAGE_TOO_SHORT | 400 | Additional error code. | general | Missing definition. | Yes |
| PARSE_REQUIRED | 400 | Additional error code. | general | Missing definition. | Yes |
| ACCOUNT_DISABLED | 400 | Additional error code. | general | Missing definition. | Yes |
| INTERNAL_ERROR | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_MISSING_RESOURCE | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_PARSER_WARNING | 400 | Additional error code. | general | Missing definition. | Yes |
| REPOSITORY_LOCKED | 400 | Additional error code. | general | Missing definition. | Yes |
| INVALID_TOKEN | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_DUPLICATE_RESOURCE | 400 | Additional error code. | general | Missing definition. | Yes |
| ANALYZE_RULE_CONFIG_INVALID | 400 | Additional error code. | general | Missing definition. | Yes |
| EMAIL_ALREADY_EXISTS | 400 | Additional error code. | general | Missing definition. | Yes |
| ASSET_QUERY_INVALID | 400 | Additional error code. | general | Missing definition. | Yes |
| INVITE_TOKEN_EXPIRED | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_UNRESOLVED_DEPENDENCY | 400 | Additional error code. | general | Missing definition. | Yes |
| THUMBNAIL_NOT_READY | 400 | Additional error code. | general | Missing definition. | Yes |
| ASSET_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| INVALID_REPO_URL | 400 | Additional error code. | general | Missing definition. | Yes |
| DIFF_CACHE_MISS | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_NOT_READY | 400 | Additional error code. | general | Missing definition. | Yes |
| DIFF_PARSE_FAILED | 400 | Additional error code. | general | Missing definition. | Yes |
| PARSE_ENCODING_UNSUPPORTED | 400 | Additional error code. | general | Missing definition. | Yes |
| JOB_ALREADY_RUNNING | 400 | Additional error code. | general | Missing definition. | Yes |
| SCENE_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |
| REPO_LOCKED | 400 | Additional error code. | general | Missing definition. | Yes |
| METADATA_VERSION_NOT_READY | 400 | Additional error code. | general | Missing definition. | Yes |
| CANNOT_SELF_PROMOTE | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_DEEP_NESTING | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_CYCLIC_DEPENDENCY | 400 | Additional error code. | general | Missing definition. | Yes |
| PATH_NOT_ALLOWED | 400 | Additional error code. | general | Missing definition. | Yes |
| LAST_OWNER_CANNOT_BE_REMOVED | 400 | Additional error code. | general | Missing definition. | Yes |
| NON_FAST_FORWARD | 400 | Additional error code. | general | Missing definition. | Yes |
| HEALTH_OVERSIZED_SCENE | 400 | Additional error code. | general | Missing definition. | Yes |
| NOTIFICATION_NOT_FOUND | 400 | Additional error code. | general | Missing definition. | Yes |

