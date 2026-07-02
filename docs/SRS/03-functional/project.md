# Project / Members / Settings

## Purpose

The Project module manages the Godot project lifecycle within GodForge, as well as project members, roles, and project settings.

## Actors

- System Admin
- project_owner (Note: Mapped to project-level owner/admin in MVP; no standalone Organization entity exists)
- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Create, view, update, soft-delete, and restore projects.
- Automatically assign the creator as the `project_owner`.
- Manage members and project-level roles.
- Manage project settings and related user settings.
- Log Activity Log for all write operations.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-03 | Project CRUD | Create, view, update, soft-delete, and restore Godot projects. | Must | System Admin, Project Admin, User |
| FR-03.1 | Member Management | Invite, change roles, and remove members in a project. | Must | Project Admin |
| FR-17 | Settings | Configure project settings and user settings. | Should | Project Admin, User |
| BR-10 | Unique Project Name | Project names are globally unique and case-insensitive. | Must | System |
| BR-11 | Soft Delete | Soft-deleted projects retain data for 30 days before becoming eligible for hard-delete. | Must | System |
| BR-13 | Creator Owner | The project creator is assigned the `project_owner` role. | Must | System |

## Main Workflow

### Create Project

1. User enters name, description, Godot version, and visibility.
2. Backend validates that the name is 3-50 characters, unique case-insensitive, and visibility is valid.
3. System creates the project, slug, and default project settings.
4. The creator is assigned `project_owner`.
5. System logs a `project.created` activity.

### Update Project

1. Project Admin/Owner updates name, description, Godot version, or visibility.
2. Backend verifies that the project is not deleted/archived and the actor has permission.
3. System validates input, saves changes, and logs `project.updated`.

### Member Management

1. Project Admin enters the email and role to invite.
2. Backend verifies if the user exists or creates an invite according to policy.
3. System creates/updates `project_members`.
4. User receives a `member.invited` or `member.role_changed` notification.
5. System logs the corresponding activity.

### Soft Delete and Restore

1. project_owner confirms the project deletion.
2. Backend sets `deleted_at`, hides the project from normal lists, and logs `project.deleted`.
3. System Admin can restore a soft-deleted project by removing `deleted_at`.

## Exceptions / Errors

| Situation | HTTP Status | Error Code | Behavior |
| --- | --- | --- | --- |
| Project name already exists | 409 | `PROJECT_NAME_EXISTS` | Do not create/update the project. |
| Project not found or unauthorized | 404/403 | `PROJECT_NOT_FOUND` / `FORBIDDEN` | Do not expose information beyond permissions. |
| Remove last owner | 400 | `LAST_OWNER_CANNOT_BE_REMOVED` | Do not update members. |
| Invalid role | 400 | `VALIDATION_ERROR` | Return a field error. |
| Developer updating project settings | 403 | `FORBIDDEN` | Deny the action. |
| Project already deleted | 409 | `PROJECT_DELETED` | Require restoration first if permitted. |

## Acceptance Criteria

- AC-13: Creating a project with a valid name makes it appear in the creator's list.
- AC-14: Creating a project with a duplicate name returns 409.
- AC-15: Soft-deleting a project removes it from normal lists, but data remains in the DB.
- AC-16: System Admin successfully restores a soft-deleted project.
- AC-17: User lacking edit permission on a project receives 403.
- AC-27: Project Admin changes a setting and it is saved/applied.
- AC-28: Developer attempting to change a project setting receives 403.
- AC-29: User changes personal settings and the UI uses the new settings.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/v1/projects` | Authenticated | pagination/filter | Project list according to permission | `FORBIDDEN` |
| POST | `/api/v1/projects` | Authenticated | name, description, godotVersion, visibility | Project created | `PROJECT_NAME_EXISTS`, `VALIDATION_ERROR` |
| GET | `/api/v1/projects/{projectId}` | Project member | project id | Project detail | `PROJECT_NOT_FOUND` |
| PUT | `/api/v1/projects/{projectId}` | `project_admin+` | project fields | Project updated | `FORBIDDEN`, `VALIDATION_ERROR` |
| DELETE | `/api/v1/projects/{projectId}` | `project_owner` | confirmation | Project soft-deleted | `FORBIDDEN` |
| POST | `/api/v1/projects/{projectId}/restore` | `system_admin` | none | Project restored | `PROJECT_NOT_FOUND` |
| GET | `/api/v1/projects/{projectId}/members` | Project member | pagination | Member list | `FORBIDDEN` |
| POST | `/api/v1/projects/{projectId}/members` | `project_admin+` | email, role | Member added/invited | `ALREADY_MEMBER`, `USER_NOT_FOUND` |
| PUT | `/api/v1/projects/{projectId}/members/{userId}` | `project_admin+` | role | Role updated | `LAST_OWNER_CANNOT_BE_REMOVED` |
| DELETE | `/api/v1/projects/{projectId}/members/{userId}` | `project_admin+` | none | Member removed | `LAST_OWNER_CANNOT_BE_REMOVED` |
| GET | `/api/v1/projects/{projectId}/settings` | `project_admin+` | none | Project settings | `FORBIDDEN` |
| PUT | `/api/v1/projects/{projectId}/settings` | `project_admin+` | settings payload | Settings updated | `VALIDATION_ERROR` |
| PUT | `/api/v1/users/me/settings` | Authenticated | user settings | Settings updated | `VALIDATION_ERROR` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `projects` | Stores project core data, slug, visibility, health score cache, creator, and `deleted_at`. |
| `project_members` | Assigns users to projects with roles and `invited_by`. |
| `project_settings` | Stores auto parse/analyze policies, health schedules, and notification policies for the project. |
| `user_settings` | Stores themes and notification preferences for users. |
| `activities` | Records project/member/settings write operations. |
| `notifications` | Sends notifications for invites, role changes, or important project events. |

## Security / Authorization Notes

- Project lists and details must be filtered by membership or System Admin privileges.
- Do not return deleted project data to regular users.
- Do not allow self-promotion or demotion of the last owner.
- All write operations must log an activity with a correlation id.
- Settings related to credentials/repositories must hide secrets and check for `project_admin+` permissions.
