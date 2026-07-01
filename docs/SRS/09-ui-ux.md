# 9. UI/UX

## Purpose

The UI/UX of GodForge must serve repetitive technical workflows: managing projects, checking Git, viewing metadata, reviewing diffs, tracking health, and auditing. The interface prioritizes clarity, appropriate information density, comprehensible system states, and required confirmations for dangerous operations.

## Main Navigation

| Area | Content |
| --- | --- |
| Global header | Project switcher, search, notification bell, user menu. |
| Project sidebar | Dashboard, Repository/Git, Scenes, Assets, Dependency Graph, Health, Scene Diff, Activity, Settings. |
| Admin area | User Management, System Activity, system settings if user is System Admin. |
| Context actions | Screen-specific buttons: parse, analyze, commit, push, pull, invite member, connect repository. |

## Screen List

| Screen | Purpose | Main Components |
| --- | --- | --- |
| Login | System login. | Email/password form, validation, lock/disabled messages. |
| Dashboard | Project overview. | Health score, repo status, metadata summary, jobs, activity, top issues. |
| Project List | View/search projects. | List/table, search, filters, create project. |
| Project Detail | View project information. | Summary, members, settings link, repository state. |
| Repository Settings | Connect/update repository. | Remote URL, credential state, default branch, clone status. |
| Git UI | Git operations. | Status, staged/unstaged files, commit form, push/pull, conflict panel. |
| Commit History | View commit history. | Commit list, filters, commit detail, file diff links. |
| Scene Explorer | View scene tree. | Scene list, node tree, node detail, search/filter, breadcrumb. |
| Asset Explorer | View assets and usage. | Asset grid/list, detail panel, usage, warnings. |
| Dependency Graph | View dependencies. | Graph canvas, filter, legend, node detail, cycle highlight. |
| Health Report | View project health. | Score, issue list, severity filter, trend/history. |
| Scene Diff Viewer | Review scene diff. | Revision selector, tree diff, property diff, summary. |
| Notification Center | Manage notifications. | Notification list, unread filter, mark read/all. |
| Activity Log | View audit/activity. | Timeline/table, action filter, actor filter, correlation ID. |
| Admin/User Management | Manage users. | User list, invite, status, role. |

## Role-based Visibility

| UI Capability | Owner/Admin | Developer | Reviewer/QA | Viewer |
| --- | :---: | :---: | :---: | :---: |
| Create/update/delete project | Yes | No | No | No |
| Manage members | Yes | No | No | No |
| Configure repository | Yes | No | No | No |
| Git commit/push/pull/merge | Yes | Yes | No | No |
| Trigger parse/analyze | Yes | Yes | No | No |
| View scene/asset/graph/health/diff | Yes | Yes | Yes | Yes |
| View activity | Yes | Yes | Yes | Yes |
| Admin user management | System Admin only | No | No | No |

Note: Role-based visibility is merely a UX aid. The backend must enforce permissions.

## Empty States

| State | Display |
| --- | --- |
| No projects | Instructions to create a project if authorized. |
| Project lacks repository | CTA to connect repository for Project Admin; readonly message for Viewer. |
| Repository is cloning | Job progress, queue/running state, and job detail link. |
| Metadata not parsed | CTA to parse/analyze for Developer+; readonly message for Viewer. |
| Graph lacks data | Explanation of need to analyze and analyze button if authorized. |
| No notifications/activities | Concise empty list, no error displayed. |

## Loading States

- API list/table uses skeleton or light spinner.
- Long jobs display progress, status, and start time.
- Large scenes/graphs have specific loading for canvas/tree.
- Loading states must not obscure known error states.

## Error States

Error messages must answer:

1. What happened.
2. What the user can do.
3. Correlation ID for error reporting.

Do not display stack traces, server paths, or credentials.

## Confirmation Dialogs for Dangerous Operations

| Operation | Confirmation |
| --- | --- |
| Delete project | Must clearly confirm project name. |
| Disconnect repository | Must confirm impact on workspace/metadata. |
| Update Git credential | Confirm replacing old credentials. |
| Push | Confirm branch/remote if pending commits exist. |
| Pull/Merge | Warning for potential conflicts. |
| Delete branch | Warning that checked out branch cannot be deleted. |
| Cancel job | Confirm job may stop in a partial state. |

## Accessibility

- Forms and tables support keyboard navigation.
- Color is not the sole indicator of state.
- Errors placed near relevant fields.
- Important icons have labels/tooltips.
- Sufficient contrast for dashboards, graphs, and diffs.

## MVP Decisions

- MVP UI prioritizes English to match the updated business documentation. The API must return stable error codes so the frontend can localize messages in the future.
