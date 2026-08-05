# Requirement Registry

This registry assigns stable requirement families to modules. Existing IDs are not renumbered. New requirements use an unused child ID within the owning family.

| Requirement family | Owning module |
|---|---|
| `FR-01.*` | Identity, authentication and sessions |
| `FR-03`, `FR-03.*` | Project lifecycle and project membership |
| `FR-04` to `FR-07` | Linked repository, refs, commits and file browsing |
| `FR-08.*` | Godot parser |
| `FR-09.*` | Scene Explorer |
| `FR-10.*` | Asset Explorer |
| `FR-11.*` | Dependency graph and impact |
| `FR-12.*`, `FR-26` | Health analysis and incremental analysis |
| `FR-13.*` | Revision and scene diff |
| `FR-14.*` | Dashboard |
| `FR-15.*` | Search |
| `FR-16`, `FR-18.*` | Notifications, activity and audit |
| `FR-17.*` | User, project and organization settings/policies |
| `FR-19.*` | Durable jobs |
| `FR-20.*` | Godot validation |
| `FR-21` | Hosted Git through Forgejo |
| `FR-22.*` | Gemini AI advisory |
| `FR-23.*` | Asset Vault |
| `FR-24.*` | Finding collaboration |
| `FR-25.*` | Report export |
| `FR-27`, `FR-27.*` | Organization tenancy and organization membership |

## Duplicate-resolution record

`FR-17.3` belongs only to `docs/SRS/03-functional/settings-policy.md` and means organization quota/provider/asset policy. It was removed from `project.md`.

Organization/project membership invariants use newly added IDs:

- `FR-27.1`: active organization membership is required for active project membership.
- `FR-27.2`: organization membership suspension/removal revokes project memberships and schedules provider reconciliation.
- `FR-27.3`: effective permission is the intersection of platform, organization, project and resource policy.
