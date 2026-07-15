# RBAC Matrix — Blueprint

Permission keys use dot notation consistently.

| Permission | Owner | Admin | Developer | Reviewer | Viewer |
|---|:---:|:---:|:---:|:---:|:---:|
| `projects.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `projects.update` | ✓ | ✓ |  |  |  |
| `projects.delete` | ✓ |  |  |  |  |
| `members.manage` | ✓ | ✓ |  |  |  |
| `repositories.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `repositories.manage` | ✓ | ✓ |  |  |  |
| `repositories.push` |  |  |  |  |  |
| `repositories.sync` | ✓ | ✓ | ✓ |  |  |
| `revisions.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `analysis.trigger` | ✓ | ✓ | ✓ |  |  |
| `analysis.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `analysis.manage` | ✓ | ✓ |  |  |  |
| `jobs.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `jobs.cancel` | ✓ | ✓ | ✓ |  |  |
| `activity.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `settings.update` | ✓ | ✓ |  |  |  |

## Rules

- All API and worker-triggering commands re-check server-side permission.
- Removing a member must set `RemovedAt`; repository lookups must ignore removed memberships.
- Hosted Git permission synchronization uses outbox events and is required before production enablement.
- Webhooks authenticate with provider signatures, not user JWT.
