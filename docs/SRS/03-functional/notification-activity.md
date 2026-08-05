# Notifications, Activity and Audit

## Purpose

Inform users of relevant product events and retain trustworthy operational/security history.

## Actors

All users, Worker, system services, SystemAdmin.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-16 | User notifications and preferences | Must |
| FR-18.1 | Project activity timeline | Must |
| FR-18.2 | Security and administrative audit events | Must |
| FR-18.3 | Optional email dispatch | Should |

## Main flow

Product actions emit structured activity intent after successful commit. Notification rules select authorized recipients. Security/audit events are append-oriented and may use integrity hashes or immutable export.

## Error and edge cases

- Recipient removed before dispatch.
- Email provider unavailable.
- Duplicate terminal event.
- Sensitive event requiring redacted display.

## Authorization and security

- Notification content is permission-safe at send and read time.
- Audit entries cannot be edited through product API.
- Restricted values are never included.
- Administrative access includes actor and reason.

## Async processing and idempotency

- Email/large fan-out dispatch uses durable jobs/outbox.

## Acceptance criteria

- `AC-FR-18-01`: Important lifecycle events are traceable by correlation ID.
- `AC-FR-16-01`: Duplicate job completion creates at most one user notification.
- `AC-FR-16-02`: User preferences affect optional notifications, not mandatory security events.

## Related API

- Notification list/read/preferences; project activity; restricted admin audit endpoints

## Related data

- `collab.notifications`, `collab.notification_preferences`, `collab.activities`, `audit.audit_logs`, `audit.security_audit_events`, `audit.data_access_logs`

## Tests and observability

- Test suites: `TC-NOTIF-*` and `TC-AUDIT-*`.
- Metrics: delivery backlog/outcome, dedupe, unread count and audit-write failure.
- Security tests verify permission-safe rendering and append-only audit behavior.
