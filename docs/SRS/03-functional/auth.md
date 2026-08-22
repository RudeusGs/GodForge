# Identity, Authentication and Session Management

## Purpose

Provide secure account registration, authentication, session rotation, recovery and administrative account controls for M1.

## Actors

Anonymous user, authenticated user, SystemAdmin.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-01.1 | Email/OTP registration and verified account creation | Must |
| FR-01.2 | Login with lockout and rate limiting | Must |
| FR-01.3 | Short-lived access token and rotating refresh token | Must |
| FR-01.4 | Logout, session listing and revocation | Must |
| FR-01.5 | Password reset and security-event recording | Must |
| FR-01.6 | Optional MFA for privileged roles | Should |
| FR-01.7 | Configurable concurrent active-session limit without implicit session eviction | Must |

## Main flow

1. Anonymous user requests a registration OTP.
2. The system normalizes the email, applies abuse controls and stores only a hash of the OTP challenge.
3. User submits email, OTP, password and profile fields.
4. The system validates the challenge, creates a verified active account and consumes the challenge atomically.
5. Login validates account state and adaptive password hash, then creates a server-side session.
6. API returns a short-lived access token and writes the one-time rotating refresh token to an HttpOnly, SameSite=Strict cookie.
7. Refresh atomically revokes/replaces the cookie token, returns a new access token and rotates the refresh cookie.
8. Logout or session revocation invalidates the selected server-side session.
9. Password reset consumes a single-use challenge, changes the password/security stamp and revokes configured active sessions.

## Error and edge cases

- Invalid, expired, consumed or attempt-exhausted OTP/reset challenge.
- Disabled, deleted or temporarily locked account.
- Invalid credentials with uniform error behavior.
- Reused, revoked or expired refresh token.
- Concurrent refresh attempts: at most one request succeeds.
- Existing normalized email.
- Email provider delay or duplicate delivery.
- Security-event persistence failure must fail the related privileged state change when the event is mandatory.

## Authorization and security

- Passwords use an approved adaptive hash and are never recoverable.
- OTP and reset tokens are stored only as cryptographic hashes. Refresh tokens are stored as hashes server-side and are exposed to the browser only through an HttpOnly cookie.
- Access tokens are short-lived and contain stable identity/session references, not mutable authorization snapshots.
- Tokens, OTP values, password fields and provider payloads are never logged.
- Login, registration OTP, password reset and refresh endpoints have independent rate limits.
- Authentication responses do not reveal whether an email exists except where product policy explicitly allows it.
- Refresh-token reuse marks the token family compromised and revokes the configured session/family scope.
- Password reset and privileged session changes emit append-oriented security/audit events.
- Session authorization always verifies current user/session state; a valid JWT does not override a revoked session.

## Async processing and idempotency

- Registration OTP and password-reset email delivery may use outbox-backed email jobs.
- Creating a challenge is idempotent within the configured resend cooldown; repeated requests do not create unlimited active challenges.
- Login, refresh, logout and session reads are synchronous.
- Refresh rotation uses a database transaction and concurrency protection; duplicate/replayed tokens do not produce multiple valid descendants.

## Acceptance criteria

- `AC-FR-01.1-01`: A valid unconsumed registration challenge creates exactly one verified user for the normalized email and consumes the challenge in the same transaction.
- `AC-FR-01.1-02`: Invalid, expired or consumed registration challenges return a safe error and create no user.
- `AC-FR-01.2-01`: Login with valid credentials for an active account creates one server-side session, returns an access token and writes the refresh token as an HttpOnly cookie.
- `AC-FR-01.2-02`: Repeated failed login attempts trigger configured rate-limit/lockout behavior without leaking account existence.
- `AC-FR-01.3-01`: Concurrent refresh requests using the same refresh token result in at most one successful rotation.
- `AC-FR-01.3-02`: Reuse of a replaced refresh token is rejected, records a security event and revokes the configured token family/session scope.
- `AC-FR-01.4-01`: Logout revokes the current session and subsequent refresh for that session fails.
- `AC-FR-01.4-02`: A user can list only their own active/recent sessions and revoke another own session by ID.
- `AC-FR-01.5-01`: A valid password-reset challenge changes the password, consumes the challenge and revokes configured existing sessions atomically.
- `AC-FR-01.5-02`: Public responses never expose password hashes, raw token values, internal exception details or user-enumeration metadata.
- `AC-FR-01.6-01`: When MFA is enabled for a privileged account, password-only login cannot complete an authenticated session.
- `AC-FR-01.7-01`: At least two independent sessions are supported; the default maximum is 10 and is configurable to no less than 2.
- `AC-FR-01.7-02`: Login at the configured limit is rejected with `AUTH_SESSION_LIMIT_REACHED` and does not revoke an existing session; concurrent boundary logins cannot exceed the limit.

## Related API

Detailed contracts: `../05-api-contracts/auth.md`.

- `POST /api/v1/auth/send-otp`
- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `GET /api/v1/users/me`
- `GET /api/v1/users/me/sessions`
- `DELETE /api/v1/users/me/sessions/{sessionId}`

## Related data

- `identity.users`
- `identity.auth_challenges`
- `identity.user_sessions`
- `identity.refresh_tokens`
- `identity.login_events`
- `identity.security_events`
- `audit.audit_logs`
- `ops.outbox_messages`

Physical design: `../04-database-m1-physical.md`.

## Tests and observability

- Test suites: `TC-AUTH-REG-*`, `TC-AUTH-LOGIN-*`, `TC-AUTH-REFRESH-*`, `TC-AUTH-SESSION-*`, `TC-AUTH-RESET-*`.
- Required integration tests use real PostgreSQL constraints/transactions for refresh concurrency and challenge consumption.
- Metrics: login success/failure/lockout, challenge request/consume, refresh success/reuse, active session count and endpoint rate-limit rejection.
- Logs contain correlation ID, safe user/session IDs and error code; no credentials or raw tokens.
- Alerts cover abnormal refresh-reuse rate, login abuse and email-job backlog.
