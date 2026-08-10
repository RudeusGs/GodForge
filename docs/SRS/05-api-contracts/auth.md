# M1 API Contract - Authentication and Sessions

Base path: `/api/v1`
Requirements: `FR-01.1` to `FR-01.6`
Acceptance criteria: `AC-FR-01.*` in `../03-functional/auth.md`

## Common DTOs

```json
// UserSummary
{
  "id": "uuid",
  "email": "user@example.com",
  "displayName": "Nguyen Van A",
  "status": "active",
  "emailVerifiedAt": "2026-08-04T12:00:00Z",
  "createdAt": "2026-08-04T12:00:00Z",
  "version": 1
}
```

```json
// SessionSummary
{
  "id": "uuid",
  "deviceName": "Chrome on Windows",
  "createdAt": "2026-08-04T12:00:00Z",
  "lastSeenAt": "2026-08-04T12:10:00Z",
  "expiresAt": "2026-09-03T12:00:00Z",
  "current": true,
  "revokedAt": null
}
```

```json
// AuthSessionResponse
{
  "user": { "id": "uuid", "email": "user@example.com", "displayName": "Nguyen Van A", "status": "active", "version": 1 },
  "session": { "id": "uuid", "deviceName": "Chrome on Windows", "createdAt": "utc", "lastSeenAt": "utc", "expiresAt": "utc", "current": true, "revokedAt": null },
  "accessToken": "opaque-jwt-string",
  "accessTokenExpiresAt": "utc",
  "refreshTokenExpiresAt": "utc"
}
```

The raw refresh token is returned only as the `godforge_refresh` cookie. The cookie is `HttpOnly`, `SameSite=Strict`, scoped to `/api/v1/auth`, and `Secure` outside development. The JSON response never exposes the raw refresh token.

## POST `/auth/register/send-otp`

- **Actor:** anonymous.
- **Permission:** none.
- **Request:** `{ "email": "user@example.com" }`.
- **Validation:** trim; normalize email; maximum 255 characters; reject malformed email.
- **Response:** `202 Accepted` with `{ "requestAccepted": true, "resendAfterSeconds": 60 }` in standard envelope.
- **Behavior:** create/reuse one active registration challenge within cooldown and enqueue email through outbox.
- **Idempotency:** normalized email + purpose + cooldown window.
- **Rate limit:** per IP and normalized email; configuration must satisfy `SEC-03`.
- **Errors:** `VALIDATION_ERROR`, `RATE_LIMIT_EXCEEDED`, sanitized `DEPENDENCY_UNAVAILABLE` only when policy does not require uniform acceptance.
- **Audit/security:** safe challenge-request event; no raw OTP.
- **Tests:** `TC-AUTH-REG-001` to `TC-AUTH-REG-004`.

## POST `/auth/register`

- **Actor:** anonymous.
- **Permission:** none.
- **Request:**

```json
{
  "email": "user@example.com",
  "otp": "123456",
  "password": "secret",
  "displayName": "Nguyen Van A"
}
```

- **Validation:** normalized email up to 255 characters; display name up to 120 characters; password length 8-256 plus complexity policy; OTP format.
- **Response:** `201 Created` with `UserSummary` and `Location: /api/v1/users/me`.
- **Transaction:** lock/consume challenge, enforce unique normalized email, create verified user and security event atomically.
- **Idempotency:** no generic retry key; unique email and consumed challenge prevent duplicates.
- **Rate limit:** per IP/email/challenge.
- **Errors:** `AUTH_OTP_INVALID`, `AUTH_OTP_EXPIRED`, `AUTH_EMAIL_EXISTS`, `VALIDATION_ERROR`, `RATE_LIMIT_EXCEEDED`.
- **Audit/security:** `identity.user.registered`; never log password/OTP.
- **Tests:** `TC-AUTH-REG-005` to `TC-AUTH-REG-010`.

## POST `/auth/login`

- **Actor:** anonymous.
- **Permission:** none.
- **Request:** `{ "email": "user@example.com", "password": "secret", "deviceName": "Chrome on Windows" }`.
- **Validation:** email required and limited to 255 characters; password required and limited to 256 characters; device name optional and bounded.
- **Response:** `200 OK` with `AuthSessionResponse`.
- **Transaction:** verify account and password; create session and first refresh token; record login outcome.
- **Idempotency:** none; each successful login creates a distinct session.
- **Rate limit:** per IP/email with lockout policy.
- **Errors:** `AUTH_INVALID_CREDENTIALS`, `AUTH_ACCOUNT_DISABLED`, `AUTH_ACCOUNT_LOCKED`, `RATE_LIMIT_EXCEEDED`.
- **Audit/security:** success/failure login event with safe identifiers/IP hash.
- **Tests:** `TC-AUTH-LOGIN-001` to `TC-AUTH-LOGIN-008`.

## POST `/auth/refresh`

- **Actor:** holder of refresh token; access JWT not required.
- **Permission:** active user and active session.
- **Request:** empty body; the rotating refresh token is read from the `godforge_refresh` HttpOnly cookie.
- **Response:** `200 OK` with `AuthSessionResponse`; the replacement refresh token is rotated through `Set-Cookie` and is not present in JSON.
- **Transaction:** locate token by hash, lock token/family, validate user/session, revoke current token and insert replacement atomically.
- **Concurrency:** at most one successful rotation for the same token.
- **Idempotency:** repeated token is not successful idempotency; it is replay/reuse and follows compromise policy.
- **Rate limit:** per IP/session/family.
- **Errors:** `AUTH_TOKEN_EXPIRED`, `AUTH_TOKEN_REVOKED`, `AUTH_REFRESH_REUSED`, `UNAUTHORIZED`, `RATE_LIMIT_EXCEEDED`.
- **Audit/security:** token reuse records high-value security event and revokes configured family/session scope.
- **Tests:** `TC-AUTH-REFRESH-001` to `TC-AUTH-REFRESH-008`.

## POST `/auth/logout`

- **Actor:** authenticated user.
- **Permission:** active current session.
- **Request:** empty body.
- **Response:** `204 No Content`.
- **Transaction:** revoke current session and active refresh tokens in that session.
- **Idempotency:** repeated logout returns `204` when the actor identity can still be resolved safely; otherwise normal `401` behavior.
- **Rate limit:** standard authenticated write limit.
- **Errors:** `UNAUTHORIZED`.
- **Audit/security:** `identity.session.revoked` with current-session reason.
- **Tests:** `TC-AUTH-SESSION-001`, `TC-AUTH-SESSION-002`.

## POST `/auth/forgot-password`

- **Actor:** anonymous.
- **Permission:** none.
- **Request:** `{ "email": "user@example.com" }`.
- **Validation:** normalized email, valid format, maximum 255 characters.
- **Response:** always `202 Accepted` for syntactically valid email.
- **Behavior:** create/reuse password-reset challenge only when eligible; enqueue email through outbox.
- **Idempotency:** normalized email + purpose + cooldown window.
- **Rate limit:** strict per IP/email.
- **Errors:** `VALIDATION_ERROR`, `RATE_LIMIT_EXCEEDED`.
- **Audit/security:** uniform response prevents enumeration.
- **Tests:** `TC-AUTH-RESET-001` to `TC-AUTH-RESET-003`.

## POST `/auth/reset-password`

- **Actor:** anonymous with reset challenge.
- **Permission:** valid reset challenge.
- **Request:** `{ "email": "user@example.com", "token": "secret", "newPassword": "new-secret" }`.
- **Validation:** email maximum 255 characters; new password length 8-256 plus complexity policy.
- **Response:** `204 No Content`.
- **Transaction:** consume challenge, update password/security stamp, revoke configured active sessions/tokens and record security event atomically.
- **Idempotency:** consumed token cannot be reused.
- **Rate limit:** per IP/email/challenge.
- **Errors:** `AUTH_RESET_TOKEN_INVALID`, `VALIDATION_ERROR`, `RATE_LIMIT_EXCEEDED`.
- **Tests:** `TC-AUTH-RESET-004` to `TC-AUTH-RESET-008`.

## GET `/users/me`

- **Actor:** authenticated user.
- **Permission:** active user/session.
- **Response:** `200 OK` with `UserSummary`.
- **Errors:** `UNAUTHORIZED`, `AUTH_ACCOUNT_DISABLED`.
- **Caching:** private/no-store for security-sensitive fields.
- **Tests:** `TC-AUTH-ME-001`, `TC-AUTH-ME-002`.

## GET `/users/me/sessions`

- **Actor:** authenticated user.
- **Permission:** active user/session.
- **Response:** `200 OK` with `SessionSummary[]`; no token hashes or raw IP/user-agent values.
- **Sort:** current first, then `lastSeenAt desc`.
- **Errors:** `UNAUTHORIZED`.
- **Tests:** `TC-AUTH-SESSION-003`, `TC-AUTH-SESSION-004`.

## DELETE `/users/me/sessions/{sessionId}`

- **Actor:** authenticated user.
- **Permission:** session belongs to current user.
- **Response:** `204 No Content`.
- **Transaction:** revoke session and active refresh tokens.
- **Errors:** masked `RESOURCE_NOT_FOUND`, `UNAUTHORIZED`.
- **Audit/security:** session revocation event; current session may also be revoked intentionally.
- **Tests:** `TC-AUTH-SESSION-005` to `TC-AUTH-SESSION-008`.
