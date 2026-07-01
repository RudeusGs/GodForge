# 6. Security

## 6.1 Security Objectives

GodForge manages repositories, project metadata, Git credentials, and activity histories, so project data must be treated as sensitive. Primary security objectives:

- Authenticate users using JWT access tokens and refresh token rotation.
- Enforce authorization via RBAC at the system and project levels.
- Do not store credentials, passwords, or tokens in plaintext.
- Do not expose internal paths, credentials, stack traces, or sensitive raw command outputs.
- Record audit/activity logs for critical operations with a correlation ID.
- Validate all user input, repository URLs, file paths, and worker messages.

## 6.2 Authentication

| Component | Policy |
| --- | --- |
| Access token | JWT, lifetime 15 minutes. |
| Refresh token | Opaque random token, lifetime 7 days, hash SHA-256 stored in DB. |
| Rotation | A new refresh token is issued and the old one is revoked upon every refresh. |
| JWT issuer | `godforge-api` or configured per environment. |
| JWT audience | `godforge-client` or configured per environment. |
| Clock skew | Maximum 30 seconds. |

Minimum JWT payload:

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "name": "Display Name",
  "role": "system_admin",
  "iat": 1719792000,
  "exp": 1719792900,
  "iss": "godforge-api",
  "aud": "godforge-client",
  "jti": "unique-token-id"
}
```

## 6.3 Password Hashing

| Policy | Value |
| --- | --- |
| Algorithm | bcrypt |
| Cost factor | Minimum 12 |
| Minimum length | 8 characters |
| Require uppercase | Yes |
| Require lowercase | Yes |
| Require digit | Yes |
| Require special character | Optional in MVP |
| Max length | 128 characters |

Do not use simple MD5, SHA-1, SHA-256, or custom hashes for passwords.

## 6.4 Authorization / RBAC

### System-level Roles

| Action | system_admin | user |
| --- | :---: | :---: |
| Manage users | Yes | No |
| View all projects for operations | Yes | No |
| Restore deleted projects | Yes | No |
| System configuration | Yes | No |
| Create project | Yes | Yes |

### Project-level Roles

| Action | project_owner | project_admin | developer | reviewer | viewer |
| --- | :---: | :---: | :---: | :---: | :---: |
| Delete project | Yes | No | No | No | No |
| Manage members | Yes | Yes | No | No | No |
| Configure repository | Yes | Yes | No | No | No |
| Update project settings | Yes | Yes | No | No | No |
| Git operations | Yes | Yes | Yes | No | No |
| Trigger parse/analyze | Yes | Yes | Yes | No | No |
| View scenes/assets | Yes | Yes | Yes | Yes | Yes |
| View dependency/diff/health | Yes | Yes | Yes | Yes | Yes |
| View activity | Yes | Yes | Yes | Yes | Yes |

### Authorization Rules

- Enforce permissions at the Application/Use Case layer, not just at the Controller or UI layer.
- All project data queries must include a project scope.
- System Admin bypasses project RBAC for administrative purposes, but activities/audits are still logged.
- Frontend role-based visibility is merely a UX aid, not a security measure.

## 6.5 Git Credential Encryption

| Area | Policy |
| --- | --- |
| Git PAT | Encrypt using AES-256-GCM or equivalent mechanism before saving. |
| Key | Store in a secure secret manager or protected environment variable, never hardcoded. |
| API response | Never return plaintext; only return configuration status or masked value. |
| Logs | Do not log PATs, remote URLs with embedded credentials, tokens, or secrets. |
| Decrypt | Only decrypt within the worker/service when needed for Git operations. |

Proposed environment variable: `GODFORGE_ENCRYPTION_KEY`.

## 6.6 Secret Management

- Do not commit secrets into the repository.
- Separate dev/staging/prod configurations.
- Production secrets must be provided via a secret manager or secure environment variables.
- Key/credential rotation must have a clear process.
- Log scrubbers must filter out tokens, passwords, PATs, and authorization headers.

## 6.7 Rate Limiting

| Endpoint/Group | Limit | Window |
| --- | --- | --- |
| `/api/v1/auth/login` | 10 requests | 1 minute / IP |
| `/api/v1/auth/refresh` | 20 requests | 1 minute / IP |
| General API | 100 requests | 1 minute / user |
| Search | 30 requests | 1 minute / user |
| Git operations | 10 requests | 1 minute / user |

Brute-force protection: After 5 consecutive failed logins, the account is locked for 15 minutes.

## 6.8 Input Validation

| Input | Policy |
| --- | --- |
| Repository URL | HTTPS only in MVP; validate scheme, host, length, and deny private/reserved networks by default. Allow overrides only via an allowlist in dev/staging environments. |
| File path | Normalize paths; reject absolute paths, `..`, symlink escapes, and paths outside the workspace. |
| Git arguments | Do not build shell commands from raw strings; use structured arguments. |
| Search query | Maximum 200 characters, parameterized queries. |
| Pagination | Maximum `pageSize` is 100. |
| Worker message | Validate schema version, job ID, project ID, correlation ID, input hash, and attempt count. |
| Godot file | Apply size limits and parse timeouts; do not execute scripts. |

## 6.9 Audit Logging

All critical operations must record an activity/audit log:

- Login success/failure, logout.
- User created, role changed, member added/removed.
- Project created/updated/deleted/restored.
- Repository connected/credential updated/disconnected.
- Git commit/push/pull/merge.
- Job started/retrying/completed/failed/cancelled/timeout/dead-lettered.
- Permission denied on sensitive actions if audit policy is enabled.

Activity logs must include a `correlation_id`, actor, project (if applicable), action, target, and sanitized metadata.

## 6.10 No Internal Information Exposure

API responses and the UI must not expose:

- Server filesystem paths.
- Credentials, tokens, passwords, or invite tokens.
- Production stack traces.
- Raw Git stderr/stdout containing secrets or internal paths.
- Connection strings, broker URLs, or MinIO keys.
- Full internal worker exceptions.

## 6.11 Network Security

- Public APIs must use HTTPS/TLS 1.2+.
- PostgreSQL, Redis, RabbitMQ, MinIO, and workers must reside in a private container network.
- Do not expose DB/Redis/RabbitMQ to the public internet.
- Implement CORS allowlist based on the environment.
- Minimum security headers:

```text
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
Referrer-Policy: strict-origin-when-cross-origin
```

## 6.12 Minimum Threat Model

| Threat | Risk | Control Measures | Test/Verification |
| --- | --- | --- | --- |
| Git credential leak | PAT exposed via DB, API response, logs, or exceptions. | Encrypt credentials, mask responses, implement log scrubbers, prevent logging of Authorization/PAT, secret rotation. | Security tests verifying the DB has no plaintext PATs and logs do not contain sample tokens. |
| Git command injection | User input such as branch/path/message injected into Git commands. | Do not concatenate command strings; validate branches/paths; pass structured arguments; deny dangerous characters/paths. | Test malicious branches/paths to confirm no unintended execution. |
| SSRF via repository URL | Clone/fetch against internal IPs or metadata services. | Require HTTPS, validate host, deny private/reserved IPs by default, use provider allowlists if necessary. | Test rejecting private IPs, localhost, and link-local URLs. |
| Path traversal when reading repo | User requests `../` or symlinks to read files outside the workspace. | Normalize paths, reject absolute/parent traversals, ensure resolved paths remain within the workspace, handle symlinks safely. | Test path traversal and symlink escape vulnerabilities. |
| Malicious Godot file | `.tscn`, `.tres`, `.gd` files causing parser crashes, resource exhaustion, or parser exploitation. | Do not execute scripts, enforce size limits and timeouts, isolate the parser, support partial failures and categorized retries. | Fuzz/fixture testing with corrupt files, large files, and unusual encodings. |
| Permission bypass | User reads scenes/diffs/notifications/activities of unauthorized projects. | Server-side RBAC, project-scoped queries, audit denied actions, role-based integration tests. | Cross-project IDOR tests for main APIs. |
| Token theft | Access/refresh tokens stolen and reused. | Short-lived access tokens, refresh rotation, revocation, HTTPS, rate limits, audit anomalous logins/refreshes. | Test that reused refresh tokens are revoked/denied. |

## 6.13 MVP Decisions

- Repository URLs pointing to private/reserved networks are blocked by default across all environments; dev/staging environments require explicit allowlist configurations.
- Devs must use user secrets or environment variables. Production must use the deployment infrastructure's secret manager; if one is not available, environment variable injection by the deployment platform is the absolute minimum requirement.
