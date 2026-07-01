# Git / Repository

## Purpose

The Git module allows projects to connect to a Git repository, perform common Git operations via the web interface, and ensures dangerous operations are locked, audited, and safely handled.

## Actors

- Project Admin
- Developer
- Reviewer/QA
- Viewer

## Scope

- Connect HTTPS repositories using Personal Access Tokens or equivalent credentials.
- Clone/fetch repositories into server-side workspaces.
- Git status, stage, unstage, commit, push, pull.
- Branch list, create, checkout, delete, merge.
- Commit history and commit detail.
- Conflict handling (no automatic resolution).

## Functional Requirements

| ID | Requirement Name | Description | Priority | Actor |
| --- | --- | --- | --- | --- |
| FR-04 | Connect Repository | Connect a project to a Git repository via HTTPS URL and encrypted credentials. | Must | Project Admin |
| FR-05 | Git UI | Provide status, stage, unstage, commit, push, pull, and conflict visibility. | Must | Developer |
| FR-06 | Commit History | Display commit list, commit detail, file diff, and scene diff links for `.tscn`. | Must | Viewer+ |
| FR-07 | Branch Management | Create, switch, delete, and merge branches according to permissions. | Should | Developer, Project Admin |
| BR-27 | One Repo Per Project | Each project has only one active repository in the MVP. | Must | System |
| BR-31 | Repository Lock | Mutating Git operations must acquire a distributed lock. | Must | System |
| BR-34 | Commit Message | Commit messages are required and must be at least 5 characters long. | Must | Developer |

## Main Workflow

### Connect Repository

1. Project Admin enters HTTPS remote URL and PAT.
2. Backend validates the URL, checks that the project has no active repository, and encrypts the credential.
3. System saves `repositories` with `credential_ref`.
4. API creates a clone job and returns `202 Accepted`.
5. Clone Worker acquires the lock, clones/fetches the workspace, and updates repository/job status.
6. If the clone succeeds and `auto_parse_on_push = true`, the system triggers an initial parse/analyze job.
7. System logs a `repo.connected` activity.

### Commit and Push

1. Developer opens Git status.
2. User selects files to stage or stages all.
3. User enters a valid commit message.
4. Backend verifies that there are staged files, the workspace has no conflicts, and the actor has permission.
5. Git Service acquires the lock when mutating the workspace, creates a commit, and logs activity.
6. User selects push; system acquires the lock, pushes to remote, and handles non-fast-forward scenarios if any.

### Pull / Merge Conflict

1. User selects pull or merge.
2. Backend checks permissions, repository state, and acquires the lock.
3. Git operation runs in the workspace.
4. If there is a conflict, the system saves/displays the list of conflicting files and does not auto-resolve.
5. User resolves the conflict via UI; then stages and commits the merge.

### Commit History

1. User opens the commit history by branch/filter.
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
| Commit with no staged files | 400 | `NO_STAGED_FILES` | Do not create a commit. |
| Push non-fast-forward | 409 | `NON_FAST_FORWARD` | Require pull first. |
| Pull/merge conflict | 409 | `GIT_CONFLICT` | Display conflicting files, do not auto-resolve. |

## Acceptance Criteria

- AC-34: Valid URL + PAT creates a clone job and returns 202.
- AC-35: Successful clone creates workspace and sets repository status to `ready`.
- AC-36: Failed clone saves job status as `failed` with a clear error code.
- AC-37: PAT is encrypted and cannot be read as plaintext in DB/logs/API responses.
- AC-38: Connecting a repo when the project already has one returns 409.
- AC-39: Git status correctly displays changed/staged/unstaged/untracked files.
- AC-40: Stage/unstage file updates state correctly.
- AC-41: Commit with a valid message succeeds and working tree is updated.
- AC-42: Push successfully updates the remote.
- AC-43: Push when repo is locked returns 423.
- AC-44: Pull with conflicts displays conflict files and does not auto-resolve.
- AC-45: Commit history displays hash, author, date, and message.
- AC-46: Commit detail displays changed files and diffs.
- AC-47: Branch filter only returns commits belonging to that branch.
- AC-48: Changed `.tscn` files have a link to the Scene Diff Viewer.
- AC-49: Creating a branch makes the branch appear in the list.
- AC-50: Checkout with a dirty working tree is blocked and issues a warning.
- AC-51: Successful merge creates a merge commit.
- AC-52: Viewer attempting to merge receives a 403.

## Related API

| Method | Path | Permission | Main Request | Main Response | Main Errors |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/v1/projects/{id}/repository` | `project_admin+` | remoteUrl, personalAccessToken | Clone job | `INVALID_REPO_URL`, `REPO_ALREADY_CONNECTED` |
| GET | `/api/v1/projects/{id}/repository` | Project member | none | Repository detail | `REPO_NOT_CONNECTED` |
| PUT | `/api/v1/projects/{id}/repository/credential` | `project_admin+` | personalAccessToken | Credential updated | `FORBIDDEN` |
| DELETE | `/api/v1/projects/{id}/repository` | `project_admin+` | confirmation | Repository disconnected | `REPO_LOCKED` |
| POST | `/api/v1/projects/{id}/repository/sync` | `developer+` | branch/options | Fetch job/result | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/git/status` | `developer+` | branch | Git status | `REPO_NOT_READY` |
| POST | `/api/v1/projects/{id}/git/stage` | `developer+` | file paths | Stage result | `PATH_NOT_ALLOWED` |
| POST | `/api/v1/projects/{id}/git/unstage` | `developer+` | file paths | Unstage result | `PATH_NOT_ALLOWED` |
| POST | `/api/v1/projects/{id}/git/commit` | `developer+` | message | Commit result | `NO_STAGED_FILES` |
| POST | `/api/v1/projects/{id}/git/push` | `developer+` | branch/remote | Push result | `NON_FAST_FORWARD`, `REPO_LOCKED` |
| POST | `/api/v1/projects/{id}/git/pull` | `developer+` | branch | Pull result | `GIT_CONFLICT`, `REPO_LOCKED` |
| GET | `/api/v1/projects/{id}/git/branches` | `viewer+` | none | Branch list | `REPO_NOT_READY` |
| POST | `/api/v1/projects/{id}/git/branches` | `developer+` | name, source | Branch created | `BRANCH_EXISTS` |
| POST | `/api/v1/projects/{id}/git/branches/checkout` | `developer+` | branch | Checkout result | `DIRTY_WORKTREE` |
| DELETE | `/api/v1/projects/{id}/git/branches/{name}` | `project_admin+` | branch name | Branch deleted | `CURRENT_BRANCH_DELETE_DENIED` |
| POST | `/api/v1/projects/{id}/git/merge` | `developer+` | source, target | Merge result | `GIT_CONFLICT` |
| GET | `/api/v1/projects/{id}/git/commits` | `viewer+` | pagination/filter | Commit list | `REPO_NOT_READY` |
| GET | `/api/v1/projects/{id}/git/commits/{hash}` | `viewer+` | hash | Commit detail | `COMMIT_NOT_FOUND` |

## Related Database Tables

| Table | Role |
| --- | --- |
| `repositories` | Remote URL, credential reference, default branch, workspace path, clone status. |
| `jobs` | Clone/fetch/sync jobs, progress, status, errors, and correlation id. |
| `activities` | `repo.connected`, `repo.disconnected`, `git.commit`, `git.push`, `git.pull`, `git.merge`. |
| `project_settings` | Auto parse/analyze policy after push/clone. |
| `notifications` | Job results, conflict, or credential issue notifications. |

## Security / Authorization Notes

- Only HTTPS repositories are supported in the MVP.
- Credentials must be encrypted using AES-256-GCM or an equivalent mechanism.
- Do not pass raw user input directly to shell commands; Git arguments must be validated and passed using a structured API.
- File paths from users must be normalized and checked so they don't escape the workspace.
- Do not expose server internal paths, remote credentials, stack traces, or raw Git stderr that may contain secrets.
