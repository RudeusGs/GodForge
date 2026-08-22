# 10. Traceability Matrix

This matrix maps stable requirements to acceptance criteria, API contracts, permissions, primary data and automated test families. Detailed endpoint/table definitions remain in module documents.

## 10.1 M1 implementation-ready traceability

| Requirement | Acceptance criteria | API contract | Primary permission | Primary data | Primary tests |
|---|---|---|---|---|---|
| `FR-01.1` | `AC-FR-01.1-01`, `AC-FR-01.1-02` | `05-api-contracts/auth.md`: registration OTP/register | anonymous with rate limit | users, auth_challenges, security/audit, outbox | `TC-AUTH-REG-*` |
| `FR-01.2` | `AC-FR-01.2-01`, `AC-FR-01.2-02` | login | anonymous with abuse controls | users, sessions, login_events | `TC-AUTH-LOGIN-*` |
| `FR-01.3` | `AC-FR-01.3-01`, `AC-FR-01.3-02` | refresh | active user/session/token family | refresh_tokens, sessions, security_events | `TC-AUTH-REFRESH-*` |
| `FR-01.4` | `AC-FR-01.4-01`, `AC-FR-01.4-02` | logout/session list/revoke | own active session | sessions, refresh_tokens, audit | `TC-AUTH-SESSION-*` |
| `FR-01.5` | `AC-FR-01.5-01`, `AC-FR-01.5-02` | forgot/reset password | valid challenge | auth_challenges, users, sessions, security_events | `TC-AUTH-RESET-*` |
| `FR-01.6` | `AC-FR-01.6-01` | future MFA extension in auth contract | privileged account policy | future MFA tables | `TC-AUTH-MFA-*` |
| `FR-01.7` | `AC-FR-01.7-01`, `AC-FR-01.7-02` | login/session list/revoke | authenticated account session policy | user_sessions | `TC-AUTH-LOGIN-*`, PostgreSQL concurrency tests |
| `FR-27` | `AC-FR-27-01`, `AC-FR-27-02` | `05-api-contracts/organizations.md` | organization membership/role | organizations, organization_members, invitations | `TC-ORG-*`, `TC-TENANT-*` |
| `FR-27.1` | `AC-FR-27.1-01`, `AC-FR-27.1-02` | project member add/update | `projectMembers.add/updateRole` | project_members composite tenant FKs | `TC-RBAC-MEMBERSHIP-*` |
| `FR-27.2` | `AC-FR-27.2-01`, `AC-FR-27.2-02` | organization member suspend/remove | `organizationMembers.remove/updateRole` | organization_members, project_members, audit, outbox | `TC-ORG-MEMBER-REMOVE-*` |
| `FR-27.3` | `AC-FR-27.3-01`, `AC-FR-27.3-02` | all organization/project routes | effective-permission evaluator | memberships/settings/policies | `TC-RBAC-*`, cross-tenant tests |
| `FR-03` | `AC-FR-03-01` to `AC-FR-03-04` | `05-api-contracts/projects.md`: project CRUD/archive/restore | organization create/admin plus project role | projects, project_settings, project_members, audit/outbox | `TC-PROJ-*` |
| `FR-03.1` | `AC-FR-03.1-01` to `AC-FR-03.1-03` | project membership/ownership | project membership permissions | project_members, audit, outbox | `TC-PROJ-MEMBER-*`, `TC-PROJ-OWNER-*` |
| `FR-17` | `AC-FR-17-01` and `AC-FR-17.*` | project settings route; settings module | settings permission by scope | user/org/project settings | `TC-SET-*` |

M1 physical schema: `04-database-m1-physical.md`.

## 10.2 Remaining module traceability

| Requirement | Module | Primary API group | Primary data | Primary tests |
|---|---|---|---|---|
| `FR-00.1` | Public landing experience | none; public Vue route | none | `TC-LANDING-*` |
| `FR-04` to `FR-07`, `FR-21` | Repository/Git | repository, branches, commits, webhooks | repo tables, Forgejo | `TC-REPO-*`, `TC-WEBHOOK-*` |
| `FR-20.*` | Validation | revision validation | validation runs/findings | `TC-VALID-*` |
| `FR-08.*` | Parser | analysis pipeline/read models | metadata tables | `TC-PARSER-*` |
| `FR-09.*` | Scene Explorer | revision scenes | scene metadata | `TC-SCENE-*` |
| `FR-10.*` | Asset Explorer | revision assets | asset metadata | `TC-ASSET-EXP-*` |
| `FR-11.*` | Dependency Graph | revision graph/impact | graph tables | `TC-GRAPH-*` |
| `FR-12.*`, `FR-26` | Health/Incremental | health, analysis | analysis tables | `TC-HEALTH-*`, `TC-INCR-*` |
| `FR-22.*` | AI Advisory | AI trigger/report | AI run/finding | `TC-AI-*` |
| `FR-23.*` | Asset Vault | project assets/manifest/download | storage asset tables, MinIO | `TC-VAULT-*` |
| `FR-24.*` | Finding Collaboration | finding state/comments | collaboration tables | `TC-FIND-*` |
| `FR-13.*` | Diff | comparison endpoints | scene diff/artifact | `TC-DIFF-*` |
| `FR-19.*` | Jobs | jobs/cancel/retry | ops tables, RabbitMQ | `TC-JOB-*` |
| `FR-16`, `FR-18.*` | Notifications/Activity/Audit | notifications/activity/admin audit | collab/audit tables | `TC-NOTIF-*`, `TC-AUDIT-*` |
| `FR-14.*` | Dashboard | dashboard | read models/cache | `TC-DASH-*` |
| `FR-15.*` | Search | search | search tables | `TC-SEARCH-*` |
| `FR-25.*` | Report Export | reports | report/artifact | `TC-REPORT-*` |
| `FR-17.*` | Settings/Policies | settings/policies | settings/profile tables | `TC-SET-*` |
| `SEC-01` to `SEC-34` | Security | all | all | `TC-SEC-*` |
| `NFR-01` to `NFR-08` | Performance | read/analysis endpoints | query/metrics | `TC-PERF-*` |
| `NFR-20` to `NFR-26` | Reliability | worker/jobs/providers | ops/outbox/inbox | `TC-REL-*` |

## 10.3 Maintenance rules

- Every new Must requirement has at least one objective `AC-*` and one automated `TC-*` mapping before implementation.
- Every new endpoint/table maps to a functional requirement and permission.
- A feature is not Complete in `../IMPLEMENTATION_STATUS.md` if traceability is incomplete.
- Existing IDs are not reused. See `../REQUIREMENT_REGISTRY.md`.
