# Auth / RBAC / User Management

## Purpose

The Auth module provides user authentication, session management, refresh token rotation, account management, and RBAC authorization for GodForge.

## Actors

- System Admin
- Organization Owner (Note: Mapped to project-level owner/admin in MVP; no standalone Organization entity exists)
- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Login, logout, and session refresh using JWT access tokens and refresh tokens.
- User management using an invite-only model.
- System role and project-level role management.
- Authorization checks at the backend/application layer.
- Activity logging for login, logout, user creation, and role changes.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-01 | User Authentication | Allow login, refresh token, logout, and protect APIs using JWT. | Must | All users |
| FR-02 | User and Role Management | System Admin creates invite-only users; Project Admin manages members/roles within a project. | Must | System Admin, Project Admin |
| BR-01 | Account Lockout | After 5 consecutive failed login attempts, the account is locked for 15 minutes. | Must | System |
| BR-02 | Refresh Token Rotation | Each refresh must issue a new refresh token and revoke the old one. | Must | System |
| BR-05 | Invite-only | Self-registration is not supported in the current scope. | Must | System Admin |
| BR-07 | Last Owner | The last owner of a project cannot be removed or demoted. | Must | Project Admin |

## Main Workflow

### Login

1. User enters email and password.
2. Backend validates input, checks rate limits and account status.
3. System compares the password using bcrypt hash.
4. If valid, the system issues a JWT access token with a 15-minute lifetime and an opaque refresh token with a 7-day lifetime.
5. The refresh token is only stored as a hash in `refresh_tokens`.
6. The system records a `user.login.success` activity and returns the session payload to the client.

### Refresh Session

1. Client sends the refresh token when the access token expires.
2. Backend verifies the token hash, expiry, and `revoked_at` status.
3. System issues a new access token and a new refresh token.
4. The old refresh token is revoked to prevent reuse.

### Logout

1. Client sends a logout request with the current session's refresh token.
2. Backend revokes the refresh token in the database.
3. System records a `user.logout` activity.

### Invite-only User Creation

1. System Admin enters email, display name, and system role.
2. Backend verifies that the email does not exist.
3. Creates a user with `pending_activation` status.
4. Sends an invite/password setup token or saves the invite state for dev environment testing.
5. User sets a valid password; the account transitions to `active`.

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Invalid email/password | 401 | `INVALID_CREDENTIALS` | Do not reveal if the email exists; increment `failed_login_count`. |
| Account locked | 403 | `ACCOUNT_LOCKED` | Deny login until `locked_until` expires. |
| Account disabled | 403 | `ACCOUNT_DISABLED` | Deny login. |
| Access token expired | 401 | `TOKEN_EXPIRED` | Client calls refresh endpoint. |
| Invalid token signature | 401 | `INVALID_TOKEN` | Reject the request. |
| Refresh token revoked | 401 | `TOKEN_REVOKED` | Force re-login. |
| Email already exists | 409 | `EMAIL_ALREADY_EXISTS` | Do not create a new user. |
| Self-promotion | 403 | `CANNOT_SELF_PROMOTE` | Deny role change. |
| Remove last owner | 400 | `LAST_OWNER_CANNOT_BE_REMOVED` | Do not update membership. |

## Acceptance Criteria

- AC-01: Logging in with correct email/password returns an access token, refresh token, expiry, and user profile.
- AC-02: Failed login returns 401 and does not reveal whether the user exists.
- AC-03: An expired access token can be refreshed using a valid refresh token.
- AC-04: A revoked or expired refresh token returns 401 and requires re-login.
- AC-05: Logout revokes the current session's refresh token.
- AC-06: After 5 failed password attempts, the account is locked for 15 minutes.
- AC-07: System Admin creates an invite-only user and the user successfully sets a password.
- AC-08: Project Admin invites a user to a project and the user sees the project in their list.
- AC-09: User lacking admin API access receives 403.
- AC-10: The last owner of a project cannot be removed or demoted.
- AC-11: A user removed from a project can no longer see that project.
- AC-12: A password that does not meet the policy returns a clear validation error.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous | `email`, `password` | Access token, refresh token, user | `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED` |
| POST | `/api/v1/auth/refresh` | Refresh token | `refreshToken` | New token pair | `TOKEN_EXPIRED`, `TOKEN_REVOKED` |
| POST | `/api/v1/auth/logout` | Authenticated | `refreshToken` | Logout success | `INVALID_TOKEN` |
| POST | `/api/v1/auth/setup-password` | Invite token | token, password | Account activated | `INVITE_TOKEN_EXPIRED`, `VALIDATION_ERROR` |
| GET | `/api/v1/users` | `system_admin` | pagination/filter | User list | `FORBIDDEN` |
| POST | `/api/v1/users` | `system_admin` | email, displayName, systemRole | User invited | `EMAIL_ALREADY_EXISTS` |
| GET | `/api/v1/users/{id}` | `system_admin` or self | user id | User detail | `USER_NOT_FOUND`, `FORBIDDEN` |
| PUT | `/api/v1/users/{id}` | `system_admin` or self | displayName, avatarUrl | User updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| PUT | `/api/v1/users/{id}/status` | `system_admin` | status | User status updated | `USER_NOT_FOUND`, `VALIDATION_ERROR` |
| GET | `/api/v1/users/me` | Any authenticated | none | Current user | `INVALID_TOKEN` |
| PUT | `/api/v1/users/me/settings` | Any authenticated | theme, notification prefs | User settings updated | `VALIDATION_ERROR` |
| PUT | `/api/v1/users/me/password` | Any authenticated | oldPassword, newPassword | Password changed | `INVALID_CREDENTIALS`, `VALIDATION_ERROR` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `users` | Stores identity, password hash, system role, status, failed login count, and lock state. |
| `refresh_tokens` | Stores refresh token hash, expiry, revoke state, user agent, and IP. |
| `project_members` | Stores the user's project-level roles. |
| `activities` | Records login/logout/user created/role changed and permission-sensitive actions. |
| `user_settings` | Stores personal preferences after the user becomes active. |

## Security / Authorization Notes

- Password hashing uses bcrypt with a minimum cost factor of 12.
- Refresh tokens are stored as hashes, never in plaintext.
- JWT issuer/audience uses the GodForge name according to the environment, not a legacy product name.
- RBAC must be enforced at the backend/application layer, not relying on the frontend.
- System Admins can bypass project-level RBAC for administrative purposes, but activities/audits must still be recorded.
- Passwords, tokens, invite tokens, or sensitive claims must not be logged.
