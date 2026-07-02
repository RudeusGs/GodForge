# Notification / Activity Log

## Purpose

The Notification and Activity Log module helps users receive notifications about important events and records business activities for auditing and traceability.

## Actors

- System Admin
- Project Admin
- Developer
- Reviewer/QA
- Viewer
- Worker/System

## Scope

- In-app notifications, unread count, mark as read, and mark all as read.
- Real-time notifications via SignalR when the user is online.
- Email notifications for invites and critical health issues if configured.
- Append-only Activity Log for login, project, repo, Git, job, and member events.
- Project activity feed and system activity view for System Admins.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-16 | Notifications | Send and manage notifications for users. | Should | User, Worker |
| FR-18 | Activity Log | Record important activities of users and the system. | Must | System |
| BR-19 | Realtime Notifications | Send real-time notifications via SignalR when the user is online. | Should | System |
| BR-20 | Read State | Users can mark notifications as read or mark all as read. | Should | User |
| BR-21 | Notification Retention | Default notification retention is 90 days. | Should | System |
| BR-22 | Write Activity | All data mutation operations must write an Activity Log. | Must | System |
| BR-24 | No Sensitive Logs | Activity logs must not contain passwords, tokens, or credentials. | Must | System |
| BR-26 | Append-only Activity | Activity Logs are append-only and cannot be edited/deleted by regular users. | Must | System |

## Main Workflow

### Notification

1. A domain event or worker result generates a notification candidate.
2. Notification Service determines recipients based on project membership and user settings.
3. System creates a `notifications` record.
4. If the user is online, SignalR sends a `NotificationReceived` event.
5. User opens the Notification Center, views the list, and marks them as read/mark all.

### Activity Log

1. A write operation or security-relevant event occurs.
2. Application layer creates an activity record with actor, project, action, target, safe metadata, IP, and correlation id.
3. Activity is saved append-only.
4. Project members view activities by project; System Admin views system-wide activities.

## Minimum Events/Actions

| Category | Action/Type | Description |
| --- | --- | --- |
| Auth | `user.login.success`, `user.login.failed`, `user.logout` | Login/logout events. |
| User | `user.created`, `user.role_changed` | User creation and role changes. |
| Project | `project.created`, `project.updated`, `project.deleted`, `project.restored` | Project lifecycle events. |
| Member | `project.member_added`, `project.member_removed`, `member.role_changed` | Member management events. |
| Repository | `repo.connected`, `repo.credential_updated`, `repo.disconnected` | Repository configuration events. |
| Repository | `repo.sync` | Fetching snapshots. |
| Job | `job.started`, `job.retrying`, `job.completed`, `job.failed`, `job.cancelled`, `job.timeout`, `job.dead_lettered` | Worker/job lifecycle events. |
| Health | `health.critical` | New critical health issues. |

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Notification does not belong to user | 404/403 | `NOTIFICATION_NOT_FOUND` | Do not expose the notification. |
| Activity outside project permissions | 403 | `FORBIDDEN` | Do not return activities. |
| SignalR offline | 200 | `REALTIME_UNAVAILABLE` | Users still see notifications when they open the app. |
| Metadata contains secrets | N/A | `ACTIVITY_SANITIZATION_REQUIRED` | Sanitize before saving; do not log secrets. |

## Acceptance Criteria

- AC-24: Job completion creates a real-time notification if the user is online or displays when opening the app.
- AC-25: Marking as read removes the notification from the unread badge.
- AC-26: Notifications older than 90 days are deleted by a background job.
- AC-30: Creating a project logs a `project.created` activity.
- AC-31: Failed logins log `user.login.failed` along with IP/correlation id.
- AC-32: Activity logs do not contain passwords, tokens, or credentials.
- AC-33: Viewing project activity logs only shows logs for that project with pagination.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/notifications` | Authenticated | unread/page | Notification list | `FORBIDDEN` |
| GET | `/api/v1/notifications/unread-count` | Authenticated | none | Unread count | `FORBIDDEN` |
| PUT | `/api/v1/notifications/{id}/read` | Owner | notification id | Read state updated | `NOTIFICATION_NOT_FOUND` |
| PUT | `/api/v1/notifications/read-all` | Authenticated | none | All read | `FORBIDDEN` |
| GET | `/api/v1/projects/{projectId}/activities` | Project member | pagination/filter | Project activity | `FORBIDDEN` |
| GET | `/api/v1/activities` | `system_admin` | pagination/filter | System activity | `FORBIDDEN` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `notifications` | Recipient, project, type, title, message, read state, and job link. |
| `activities` | Actor, action, target, metadata, IP, correlation id, and timestamp. |
| `jobs` | Job events linked to notification/activity. |
| `project_members` | Determine recipients and permissions when reading activities. |
| `user_settings`, `project_settings` | Notification preferences. |

## Security / Authorization Notes

- Activity metadata must be sanitized before saving.
- Notifications can only be read by the recipient.
- Project activities can only be read by project members; system-wide activities are only for System Admins.
- Do not log plaintext credentials, tokens, passwords, invite tokens, or internal server paths.
