# Repository contracts — M2

Requirements: FR-04–FR-07, FR-21. Acceptance remains open until real provider
clone/push, signed delivery and external revocation pass.

All project endpoints require an active authenticated GodForge user, active
organization and project memberships and the named permission. Existence is
masked with 404 for actors without repository read access. Mutations require an
active project. Organization administration alone does not grant source access.

## Hosted provisioning

`POST /api/v1/projects/{projectId}/repository/hosted` requires
`repository.manage`. JSON: `name` (1–60 lowercase letters, digits and interior
hyphens), `defaultBranch` (valid Git branch, at most 150 characters, default
`main`), `isPrivate` (must be true), `autoAnalyzeEnabled` (default false).
Public hosting is intentionally rejected until a visibility policy is defined.
The provider owner is server configuration, never client input.

Returns 202 `{data:{jobId},meta:{correlationId}}`. One primary repository is the
project quota. Equivalent repeated requests return the same durable job;
different input returns 409 `REPOSITORY_ALREADY_CONNECTED`. Failed jobs can be
retried through the same request, preserving repository/provision identity.
Repository, job and outbox commit atomically under membership and repository
locks. Provider creation runs in the Worker. Pending rows have no provider ID.
Provision names contain the GodForge repository UUID; a server-generated
ownership marker must match before recovering an uncertain creation result.
An unrelated provider repository is never adopted.

States: `ProvisioningPending`, `Provisioning`, `Ready`, `ProvisioningFailed`,
`Disabled`. Ready means the provider confirmed repository creation; it does not
claim that collaborator reconciliation or Git acceptance passed. GET repository
exposes sanitized status and clone URL; pending repositories have an empty URL.

Provider calls use bounded bodies, timeout, no redirects and a configured origin.
Only explicit loopback or the `forgejo` development service may use HTTP.
Provider tokens and secrets stay in server configuration. Transient failures
retry through the durable outbox with bounded exponential backoff; permanent or
exhausted failures persist sanitized codes and reach the dead letter queue.
Worker revalidates project lifecycle and current requesting actor permission.

## Repository browsing

GET `.../repository/branches?page=1&pageSize=20`,
`.../repository/commits?branch=main&page=1&pageSize=20`,
`.../repository/tree?commitSha=<40-hex>&path=`, and
`.../repository/blob?commitSha=<40-hex>&path=project.godot`
require `repository.read`. Provider-backed browsing supports hosted Forgejo
repositories; unsupported linked adapters return a stable error.
Pages are capped at 100; tree responses at 1,000 immediate children; text at
512 KiB with strict UTF-8 and binary rejection. Tree/blob require immutable SHA.
Paths reject traversal, encoded separators, backslashes, controls and absolute
paths. Symlinks and submodules are never followed. Responses use private no-store
caching and the standard envelope. Source is rendered as text.

Errors include `REPOSITORY_NOT_CONNECTED`, `REPOSITORY_ALREADY_CONNECTED`,
`REPOSITORY_PROVISIONING`, `REPOSITORY_PROVISION_FAILED`,
`REPOSITORY_PERMISSION_DENIED`, `FORGEJO_NOT_CONFIGURED`,
`FORGEJO_UNAVAILABLE`, `REPOSITORY_PATH_INVALID`, `REPOSITORY_BLOB_TOO_LARGE`,
`REPOSITORY_BINARY_FILE`, `REPOSITORY_BROWSE_UNSUPPORTED`.

## Evidence and operations

Hardening contract (FR-04–FR-07, FR-21, FR-27.2): M2 worker operations commit
their claim and recovery intent before provider I/O. No provider call holds a
membership transaction lock. A bounded repository-operation lock serializes
provider mutations separately from membership writes. Finalization rechecks the
durable claim token and current lifecycle in a new short transaction. Monotonic
access generation is incremented transactionally on desired-state changes; only
the same generation may record provider confirmation. Stale work schedules the
latest generation without declaring convergence. Expired claims cannot publish
results. Transient failures use the existing outbox retry path; broker-level
infrastructure recovery pauses before redelivery and never acknowledges an
unpersisted outcome. Automatic managed webhook setup is part of hosted readiness.
Tests use real PostgreSQL delay barriers, an isolated pinned Forgejo instance,
real Git authentication and actual provider callbacks.

Tests must cover tenant masking, role/lifecycle checks, atomic enqueue,
redelivery, timeout recovery, ownership-marker mismatch, path and resource
bounds. Logs contain job/repository IDs and stable codes only. Monitor oldest
outbox age, provider failure rate, dead letters and reconciliation lag. No
reconciliation intent may be marked successful without verified provider state.

## Verified provider identity and desired access

POST `/api/v1/users/me/forgejo-identity` accepts `{personalToken}` (20–1000
characters; request limit 2 KiB) for the current actor. The token is used only
to call Forgejo `/user`, never persisted or logged. Admin identities and the
hosting service identity are rejected. Unique GodForge user and provider user
IDs prevent cross-account binding. Rebinding another ID is rejected with 409
`FORGEJO_IDENTITY_CONFLICT`; account recovery requires operator review.

Repositories are owned by a dedicated non-admin service user, without provider
organization/team defaults. ProjectOwner maps to admin, Maintainer/Developer to
write, Reviewer/Viewer to read. Only active users with active memberships in
both the project and organization qualify. Non-active projects/organizations
have an empty desired set. Unbound users receive no provider grants. Username
is checked against stable provider ID before granting access.

The existing unit of work stages jobs and outbox records for changes to
project/organization memberships, project/organization lifecycle, user status,
identity binding and newly provisioned repositories. Organization membership
cascades performed by set-based statements are covered by the tracked parent
membership mutation. Reconciliation reads current authoritative rows under the
same membership locks in a short claim transaction. Provider calls execute after
commit. A fresh finalization transaction verifies the lease token, current
membership and monotonic access generation before confirming provider state.
Changed generations remain pending and enqueue fresh reconciliation. Repository
session locks serialize provider mutations without holding membership locks.
Failures remain degraded and retry; a five-minute
bounded drift scan schedules up to 100 repositories per pass. Access timestamps
and sanitized error codes are returned with repository metadata. Revocation is
asynchronous; M2 acceptance requires real Git authentication tests.

## Signed webhook

POST `/api/v1/webhooks/forgejo` uses no user JWT. JSON limit 1 MiB. Required:
`X-Forgejo-Signature` (raw-body HMAC-SHA256 hex), `X-Forgejo-Delivery` (UUID),
`X-Forgejo-Event` (`push`, `repository`, `create`, `delete`). Constant-time HMAC
validation precedes parsing. The numeric repository provider ID must resolve to
an active hosted mapping. Delivery ID is persistently unique. Unsupported
events return 202 ignored after authentication and repository checks. Malformed
metadata/body is 400; invalid signature 401; unknown mapping 404.

Push requests require valid `refs/heads/` branch and 40-hex `after`. A durable
metadata sync job and sanitized webhook record commit together. Revision job
identity includes repository/branch/commit, so different deliveries of the same
revision share work. Worker reads current provider refs and stores revision
metadata without executing project code or invoking downstream analysis.

## Branch protection

GET/PUT `.../repository/branch-protection?branch=main` require repository.read
and repository.manage respectively. PUT JSON `{enabled:true}` configures the
provider's force-push/deletion protection foundation. It does not implement
reviews or analysis-dependent policy. Provider state is read back after writes.
Mutations require an active project; reads permit archived project members.

## Explicit synchronization and operation status

POST `.../repository/sync` requires `repository.sync`, current repository read
access and an active project. It returns 202 `{jobId}` after committing a job and
outbox record. GET `/api/v1/projects/{projectId}/jobs/{jobId}` requires `jobs.read`
in that project and returns the existing job DTO; wrong-project jobs are 404.

External linked repositories support anonymous HTTPS fetch through the existing
DNS-pinned, redirect-disabled workspace adapter. Explicit synchronization caches
up to 200 commits per branch; branches, commit pages, trees and bounded UTF-8
blobs use this cache. An unsynchronized repository returns
`REPOSITORY_SYNC_REQUIRED`. Private external-provider credentials are unsupported.
No repository checkout or project code execution occurs. Hosted browsing reads
Forgejo directly. Both modes independently authorize every browser request.

M2 jobs use a three-minute durable lease with ten-second token-fenced heartbeats
and a 120-second execution bound. Claim and completion transactions are short.
A recovery outbox record survives crashes; retries use bounded exponential delay
and the existing job attempt budget. Failed durable recovery delays the broker
delivery before requeue; malformed messages and exhausted jobs are dead-lettered.

Hosted metadata includes `accessGeneration`, `accessConfirmedGeneration` and
`webhookConfirmedAt`. A managed webhook must be verified before provisioning is
ready. Callback destination and secret are server configuration, never user input.
