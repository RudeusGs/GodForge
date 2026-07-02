# RBAC Matrix

GodForge enforces Role-Based Access Control (RBAC) at two levels: **System Level** and **Project Level**.

## System Roles
- **System Admin**: Can manage all users, projects, system settings, and perform global maintenance.
- **User**: Standard authenticated user. Can create projects and manage their profile.

## Project Roles
Permissions are scoped to specific `project_id`s.

| Permission / Action | Project Owner | Project Maintainer | Project Viewer |
| --- | --- | --- | --- |
| Delete Project | Yes | No | No |
| Add/Remove Members | Yes | No | No |
| Manage Repositories | Yes | Yes | No |
| Trigger Async Jobs | Yes | Yes | No |
| View Metadata/Diffs | Yes | Yes | Yes |
| View Reports/Health | Yes | Yes | Yes |

## Enforcement
- Agents must enforce this matrix in `GodForge.Application` via MediatR Pipeline Behaviors or explicit Authorization services before command execution.
- UI components must hide elements if the user lacks the role, but the backend is the ultimate source of truth.
