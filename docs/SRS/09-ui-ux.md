# 9. UI/UX Requirements

## Navigation

- Organization switcher.
- Project list and project workspace.
- Repository, Revisions, Health, Graph, Scenes, Assets, Findings, Jobs, Activity, Reports and Settings.
- User notifications and session/settings menu.

## Screens

| Screen | Required content |
|---|---|
| Authentication | Login, registration/OTP, forgot/reset password. |
| Organization | Projects, members, policies, quotas and activity. |
| Project dashboard | Repository state, latest revision, health trend, critical findings, jobs and activity. |
| Repository | Mode/provider, clone information, branches, commits, tree and bounded text blob. |
| Revision detail | Validation, parser/analysis versions, status and provenance. |
| Scene Explorer | Searchable/lazy scene tree and node details. |
| Asset Explorer | Filters, usage, duplicates, health, preview and visibility. |
| Dependency Graph | Bounded graph, search, type filters, depth and impact mode. |
| Health | Score categories, finding list, evidence, suppression and comparison. |
| AI Advisory | Clearly labeled advisory, evidence links, model/prompt provenance and degraded state. |
| Findings | Assignment, comments, state and revision history. |
| Jobs | Status, progress, attempts, safe errors, cancel/retry actions. |
| Asset Vault | Versions, policy, manifest state, upload/download audit. |
| Reports | Export requests, status, provenance and download. |
| Settings | Effective platform/org/project policies and version conflicts. |

## Interaction rules

- A user always sees selected organization, project and revision context.
- Destructive actions require confirmation and explain retention effect.
- Long operations immediately show a durable job and navigable status.
- SignalR updates are optional acceleration; page refresh obtains authoritative job state.
- AI content is visually separated from deterministic findings.
- Partial and stale data are labeled with last successful revision/time.
- Permission-denied actions are hidden when appropriate, but backend remains authoritative.

## Large data

- Virtualize or paginate scene trees, commits, assets and findings.
- Graph defaults to a meaningful bounded subgraph, not the entire repository.
- Filters and search are shareable through safe URL query parameters where appropriate.

## Accessibility

- Keyboard-accessible forms, dialogs and navigation.
- Focus management on route/dialog changes.
- Severity has icon/text in addition to color.
- Error summary links to invalid fields.
- Graph has a tabular/list alternative for essential relationships.
