# Security Test Plan

## Authentication

- Invalid credential response does not reveal account existence beyond documented policy.
- Access token expiry and refresh rotation.
- Reused/revoked refresh token rejection.
- Brute-force and OTP rate limits.
- Password reset token expiry and one-time use.

## Authorization

For every project-scoped endpoint, test Owner, Maintainer, Developer, Reviewer, Viewer, non-member and removed member. Include cross-organization resource IDs.

## Repository boundary

- Reject local, loopback, link-local, private and unsupported remote URLs when not allow-listed.
- Re-check redirects and resolved addresses.
- Reject embedded credentials in stored/exposed URL.
- Test path traversal, symlink escape, reserved paths, unusual Unicode names and excessive nesting.
- Test repository/file/command timeout and disk quotas.

## Webhooks

- Valid signature accepted.
- Invalid/missing signature rejected.
- Duplicate event is idempotent.
- Expired timestamp/replay rejected.
- Payload repository identity mismatch rejected.

## AI

- `.env`, keys and tokens excluded.
- Prompt injection in source cannot alter system behavior or trigger tools.
- Invalid JSON output produces degraded status.
- Organization AI opt-out prevents provider call.
- Token/context quotas enforced.

## Asset Vault

- Public asset access.
- Protected asset denied to anonymous/non-member.
- Selected-member grant and revocation.
- Signed URL expiry and audit.
- MIME/magic mismatch, oversized file and malware-test sample handling.
- Manifest checksum mismatch detection.

## API/browser

- Stored XSS payloads in filenames, comments and README are escaped/sanitized.
- CORS and security headers.
- Validation rejects mass assignment and unknown privileged fields.
- Error responses contain no stack trace, SQL, credential or workspace path.

## Required release evidence

- Automated security regression suite result.
- Dependency and secret scan result.
- Manual threat-model review.
- No unresolved Critical/High issue.
