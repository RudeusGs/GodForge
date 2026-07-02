# Repository Integration

## Purpose

The Repository module allows projects to connect to a remote Git repository, perform server-side clone/fetch operations, and provide snapshots for analysis.

**Scope Note:** The Repository integration provides analysis snapshots. It does **not** replace a Git Desktop Client, it does **not** track real-time uncommitted local changes, and it does **not** handle complex operations like commit, push, pull, or merge conflicts in the Web UI.

## Actors

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Connect HTTPS repositories using Personal Access Tokens or equivalent credentials.
- Clone/fetch repositories into server-side workspaces to create analysis snapshots.
- View branch list and commit history metadata.
- Select branch/commit context for parsing and analysis.

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-04 | Connect Repository | Connect a project to a Git repository via HTTPS URL and encrypted credentials. | Must | Project Admin |
| FR-06 | Snapshot History | Display commit list, commit detail, file diff, and scene diff links for `.tscn`. | Must | Viewer+ |
| BR-27 | One Repo Per Project | Each project has only one active repository in the MVP. | Must | System |
| BR-31 | Repository Lock | Mutating operations (clone/fetch) must acquire a distributed lock. | Must | System |

## Main Workflow

### Connect Repository

1. Project Admin enters HTTPS remote URL and PAT.
2. Backend validates the URL, checks that the project has no active repository, and encrypts the credential.
3. System saves `repositories` with `credential_ref`.
4. API creates a clone job and returns `202 Accepted`.
5. Clone Worker acquires the lock, clones/fetches the workspace, and updates repository/job status.
6. If the clone succeeds and `auto_parse_on_sync = true`, the system triggers an initial parse/analyze job.
7. System logs a `repo.connected` activity.

### Sync Repository (Fetch)

1. Developer triggers a repository sync.
2. API creates a fetch job and returns `202 Accepted`.
3. Fetch Worker acquires the lock, pulls updates from the remote.
4. Updates repository snapshot metadata.

### View Snapshot History

1. User opens the snapshot history by branch/filter.
2. Backend reads the Git log from the workspace, paginates, and returns a commit summary.
3. When opening commit details, the system returns changed files and diffs.
4. Changed `.tscn` files have a link to the Scene Diff Viewer.

## Exceptions / Errors

| Situation | HTTP Status / Job State | Error Code | Behavior |
| --- | --- | --- | --- |
| Invalid repository URL | 400 | `INVALID_REPO_URL` | Do not save credential. |
| Project already has a repo | 409 | `REPO_ALREADY_CONNECTED` | Do not create a new repo. |
| Incorrect Git credential | Job failed / 401 | `CLONE_AUTH_FAILED` | Do not log PAT; ask to update credential. |
| Network clone/fetch error | Job retry / failed | `CLONE_NETWORK_ERROR` | Retry according to policy. |
| Repo exceeds 2 GB | Job failed | `REPO_SIZE_EXCEEDED` | Stop clone and notify. |
| Repository is locked | 423 | `REPO_LOCKED` | Return lock expiration time if safe. |

## Acceptance Criteria

- AC-34: Valid URL + PAT creates a clone job and returns 202.
- AC-35: Successful clone creates workspace and sets repository status to `ready`.
- AC-36: Failed clone saves job status as `failed` with a clear error code.
- AC-37: PAT is encrypted and cannot be read as plaintext in DB/logs/API responses.
- AC-38: Connecting a repo when the project already has one returns 409.
- AC-45: Snapshot history displays hash, author, date, and message.
- AC-46: Snapshot detail displays changed files and diffs.
- AC-47: Branch filter only returns commits belonging to that branch.
- AC-48: Changed `.tscn` files have a link to the Scene Diff Viewer.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{projectId}/repository` | `project_admin+` | remoteUrl, personalAccessToken | Clone job | `INVALID_REPO_URL`, `REPO_ALREADY_CONNECTED` |
| GET | `/api/v1/projects/{projectId}/repository` | Project member | none | Repository detail | `REPO_NOT_CONNECTED` |
| PUT | `/api/v1/projects/{projectId}/repository/credential` | `project_admin+` | personalAccessToken | Credential updated | `FORBIDDEN` |
| DELETE | `/api/v1/projects/{projectId}/repository` | `project_admin+` | confirmation | Repository disconnected | `REPO_LOCKED` |
| POST | `/api/v1/projects/{projectId}/repository/sync` | `developer+` | branch/options | Fetch job/result | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/repository/branches` | `viewer+` | none | Branch list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/repository/commits` | `viewer+` | pagination/filter | Commit list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{projectId}/repository/commits/{hash}` | `viewer+` | hash | Commit detail | `COMMIT_NOT_FOUND` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `repositories` | Remote URL, credential reference, default branch, workspace path, clone status. |
| `jobs` | Clone/fetch/sync jobs, progress, status, errors, and correlation id. |
| `activities` | `repo.connected`, `repo.disconnected`, `repo.sync`. |
| `project_settings` | Auto parse/analyze policy after sync/clone. |
| `notifications` | Job results, conflict, or credential issue notifications. |

## Security / Authorization Notes

- Only HTTPS repositories are supported in the MVP.
- Credentials must be encrypted using AES-256-GCM or an equivalent mechanism.
- Do not pass raw user input directly to shell commands; Git arguments must be validated and passed using a structured API.
- File paths from users must be normalized and checked so they don't escape the workspace.
- Do not expose server internal paths, remote credentials, stack traces, or raw Git stderr that may contain secrets.
