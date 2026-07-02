# RBAC Matrix

GodForge enforces Role-Based Access Control (RBAC) at two levels: **System Level** and **Project Level**.

## System Roles
- **system_admin**: Can manage all users, projects, system settings, and perform global maintenance.
- **user**: Standard authenticated user. Can create projects and manage their profile.

## Project Roles
Permissions are scoped to specific `project_id`s. (Note: "Organization Owner" is a business concept only in MVP; it maps to `project_owner` or `project_admin`. No standalone organization entity/API exists in MVP.)

- **project_owner**: Creator/Owner of the project.
- **project_admin**: Can manage settings, credentials, and members.
- **developer**: Can perform Git operations, sync, trigger jobs.
- **reviewer**: Can view code, diffs, create reviews.
- **viewer**: Can view metadata, dashboards, reports.

| Permission / Action | Permission Key | Scope | Allowed Roles | Endpoints |
| --- | --- | --- | --- | --- |
| User Management | `users:manage` | System | `system_admin` | `/api/v1/users` |
| Project Create | `projects:create` | System | `system_admin`, `user` | `POST /api/v1/projects` |
| Project Read | `projects:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `GET /api/v1/projects/{id}` |
| Project Update | `projects:update` | Project | `project_owner`, `project_admin` | `PUT /api/v1/projects/{id}` |
| Project Delete | `projects:delete` | Project | `project_owner` | `DELETE /api/v1/projects/{id}` |
| Project Restore | `projects:restore` | System | `system_admin` | `POST /api/v1/projects/{id}/restore` |
| Member Invite/Add | `members:add` | Project | `project_owner`, `project_admin` | `POST /api/v1/projects/{id}/members` |
| Member List | `members:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `GET /api/v1/projects/{id}/members` |
| Member Update | `members:update` | Project | `project_owner`, `project_admin` | `PUT /api/v1/projects/{id}/members/{userId}` |
| Member Remove | `members:remove` | Project | `project_owner`, `project_admin` | `DELETE /api/v1/projects/{id}/members/{userId}` |
| Repository Connect | `repository:connect` | Project | `project_owner`, `project_admin` | `POST /api/v1/projects/{id}/repository` |
| Repository View | `repository:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `GET /api/v1/projects/{id}/repository` |
| Repository Credential | `repository:update_credential` | Project | `project_owner`, `project_admin` | `PUT /api/v1/projects/{id}/repository/credential` |
| Repository Disconnect | `repository:disconnect` | Project | `project_owner`, `project_admin` | `DELETE /api/v1/projects/{id}/repository` |
| Git Clone/Fetch/Sync | `git:sync` | Project | `project_owner`, `project_admin`, `developer` | `POST /api/v1/projects/{id}/repository/sync` |
| Git Operations | `git:operate` | Project | `project_owner`, `project_admin`, `developer` | `/api/v1/projects/{id}/git/*` |
| Parse/Analyze Jobs | `jobs:trigger` | Project | `project_owner`, `project_admin`, `developer` | `POST /api/v1/projects/{id}/parse`, `POST /api/v1/projects/{id}/analyze` |
| Job List/Detail | `jobs:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `GET /api/v1/projects/{id}/jobs` |
| Job Cancel/Retry | `jobs:manage` | Project | `project_owner`, `project_admin`, `developer` | `POST /api/v1/projects/{id}/jobs/{jobId}/cancel` |
| Dashboard Read | `dashboard:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `GET /api/v1/projects/{id}/dashboard` |
| Scene Explorer Read | `scenes:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/projects/{id}/scenes` |
| Asset Explorer Read | `assets:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/projects/{id}/assets` |
| Dependency Graph Read | `dependencies:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/projects/{id}/dependencies` |
| Scene Diff Request/Read | `diffs:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/projects/{id}/diff/*` |
| Search | `search:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/search` |
| Notifications Read/Mark | `notifications:manage` | System | (owner of notification) | `/api/v1/notifications` |
| Activity Log Read | `activities:read` | Project | `project_owner`, `project_admin`, `developer`, `reviewer`, `viewer` | `/api/v1/projects/{id}/activities` |
| System Operations | `system:operate` | System | `system_admin` | Various |

## Enforcement
- Agents must enforce this matrix in the backend `GodForge.Application` layer via MediatR Pipeline Behaviors or explicit Authorization services before command execution.
- UI elements can be hidden for unauthorized roles, but this is strictly cosmetic; it does NOT replace backend security enforcement.
