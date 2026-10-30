# RBAC Matrix

Settings scope extension: authenticated active users read/write only their own `/users/me/settings`. Active OrganizationOwner/Admin may GET/PUT organization capability policy through `organizations.update`; OrganizationMember cannot. Project settings retain `projects.read`/`settings.update` and active membership intersection. Organization administration does not grant project settings access. Effective capability ceilings never grant a missing role permission. [Typed contracts](SRS/05-api-contracts/settings.md).

## 1. Role scopes

### Platform role

- **SystemAdmin**: platform operations and audited break-glass support. It is not a normal organization or project role.

### Organization roles

- **OrganizationOwner**: organization lifecycle, ownership, administrators, policy, quota and organization-wide administration.
- **OrganizationAdmin**: organization members, invitations, projects and allowed organization policies.
- **OrganizationMember**: active tenant membership without organization-administration authority.

### Project roles

- **ProjectOwner**: full project authority except organization-only actions.
- **Maintainer**: repository, analysis, member and operational management within the project.
- **Developer**: push/sync, trigger analysis and normal collaboration.
- **Reviewer**: read analysis and collaborate on findings without repository mutation.
- **Viewer**: read-only project access.

## 2. Membership invariants

1. Every project belongs to exactly one organization.
2. Every active project member must be an active member of that same organization.
3. A project role never survives suspension or removal of its organization membership.
4. Organization membership removal/suspension revokes affected project memberships in the same business transaction and creates outbox events for Forgejo permission reconciliation.
5. OrganizationOwner/OrganizationAdmin can administer organization and project metadata, but they do not automatically read repository source, protected assets or analysis content unless they also hold a project role.
6. ProjectOwner/Maintainer may add only active organization members to a project in M1. External users must first join the organization through an organization invitation.
7. At least one active OrganizationOwner and one active ProjectOwner must remain. Ownership transfer is atomic.
8. Worker and webhook identities never inherit user roles; they operate through narrowly scoped service permissions and durable project context.

## 3. Effective permission

```text
EffectivePermission =
  PlatformMinimum
  INTERSECT OrganizationPolicy
  INTERSECT ProjectRolePermission
  INTERSECT ResourcePolicy
```

A higher-level policy may reduce permissions. It must not silently increase a project role's permissions. Asset visibility and explicit grants are evaluated after project permission.

## 4. Organization permissions

| Permission | Org Owner | Org Admin | Org Member |
|---|:---:|:---:|:---:|
| `organizations.read` | ✓ | ✓ | ✓ |
| `organizations.update` | ✓ | ✓ |  |
| `organizations.delete` | ✓ |  |  |
| `organizations.transferOwnership` | ✓ |  |  |
| `organizationMembers.read` | ✓ | ✓ | ✓ |
| `organizationMembers.invite` | ✓ | ✓ |  |
| `organizationMembers.updateRole` | ✓ | limited |  |
| `organizationMembers.remove` | ✓ | limited |  |
| `organizationAdmins.manage` | ✓ |  |  |
| `organizationProjects.listMetadata` | ✓ | ✓ | assigned only |
| `organizationProjects.create` | ✓ | ✓ |  |
| `organizationProjects.archiveAny` | ✓ | ✓ |  |
| `organizationPolicies.read` | ✓ | ✓ | effective only |
| `organizationPolicies.manage` | ✓ | ✓ |  |
| `organizationAudit.read` | ✓ | ✓ |  |

`limited` means an OrganizationAdmin cannot promote to Owner, demote/remove an Owner or bypass last-owner rules.

## 5. Project permissions

| Permission | Owner | Maintainer | Developer | Reviewer | Viewer |
|---|:---:|:---:|:---:|:---:|:---:|
| `projects.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `projects.update` | ✓ | ✓ |  |  |  |
| `projects.archive` | ✓ | ✓ |  |  |  |
| `projects.restore` | ✓ | ✓ |  |  |  |
| `projects.delete` | ✓ |  |  |  |  |
| `projectMembers.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `projectMembers.add` | ✓ | ✓ |  |  |  |
| `projectMembers.updateRole` | ✓ | limited |  |  |  |
| `projectMembers.remove` | ✓ | limited |  |  |  |
| `projectMembers.transferOwnership` | ✓ |  |  |  |  |
| `repositories.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `repositories.manage` | ✓ | ✓ |  |  |  |
| `repositories.push` | ✓ | ✓ | ✓ |  |  |
| `repositories.sync` | ✓ | ✓ | ✓ |  |  |
| `branches.protect` | ✓ | ✓ |  |  |  |
| `revisions.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `analysis.trigger` | ✓ | ✓ | ✓ |  |  |
| `analysis.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `analysis.configure` | ✓ | ✓ |  |  |  |
| `findings.comment` | ✓ | ✓ | ✓ | ✓ |  |
| `findings.assign` | ✓ | ✓ | ✓ | ✓ |  |
| `findings.resolve` | ✓ | ✓ | ✓ | ✓ |  |
| `findings.suppress` | ✓ | ✓ |  |  |  |
| `assets.readPublic` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `assets.readProtected` | policy | policy | policy | policy | policy |
| `assets.upload` | ✓ | ✓ | ✓ |  |  |
| `assets.managePolicy` | ✓ | ✓ |  |  |  |
| `jobs.read` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `jobs.cancel` | ✓ | ✓ | own |  |  |
| `reports.export` | ✓ | ✓ | ✓ | ✓ |  |
| `projectAudit.read` | ✓ | ✓ |  |  |  |

For project membership changes, `limited` means Maintainer cannot add, promote, demote or remove ProjectOwner and cannot grant a role above Maintainer.

## 6. Authorization behavior

- Authorization is evaluated in Application, not only through controller attributes or frontend visibility.
- Private resource existence is masked with `404` when disclosure would leak tenant information.
- Removed/suspended memberships are excluded from authorization queries immediately.
- Project list queries return full project content only for project members. OrganizationOwner/Admin may receive minimal administration metadata for projects they do not belong to.
- Hosted Git permissions are synchronized through outbox events and periodic reconciliation. GodForge access is revoked immediately even if provider reconciliation is pending.
- Webhooks authenticate as providers, not users.
- Worker jobs carry actor, organization and project context but revalidate current state before publishing results.
- SystemAdmin break-glass access requires reason capture and separate audit records.


## Project browser permission projection — 2026-10-30

The UI projects RolePermissions: active ProjectOwner/Maintainer see metadata, membership administration and settings; ProjectOwner additionally sees ownership/deletion. Maintainer cannot manage/grant ProjectOwner. All active project roles can read the member directory and self-leave subject to backend owner protection. Archived reads and authorized restore remain available. Organization catalog administration does not enable project content/navigation. Backend authorization remains authoritative, including existing documented organization lifecycle administration exceptions. Ownership/assignment targets require active accounts and membership intersection. [Role and race evidence](PROJECT_MANAGEMENT_ACCEPTANCE_EVIDENCE.md).
