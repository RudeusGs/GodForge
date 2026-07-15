# Scope Alignment Report

## Executive Summary
This report summarizes the documentation refactoring efforts undertaken to align the GodForge Software Requirements Specification (SRS) with its intended product identity: **a web-based project management and metadata observability platform for Godot projects.**

The alignment explicitly removes any implications that GodForge is a real-time local file watcher, a Git desktop client replacement, or a web-based Godot Editor.

## Key Changes Made

1. **`README.md` & `docs/SRS/00-overview.md` & `docs/SRS/01-scope.md`**
   - Added explicit **Non-Goals** clarifying that MVP does not include in-browser code/scene editing, running game builds, tracking uncommitted local changes, auto-syncing local files, or auto-resolving Git conflicts.
   - Reframed Git operations as **"Repository Integration"** for the purpose of creating and analyzing server-side snapshots.

2. **`docs/MILESTONES.md`**
   - Updated the milestone roadmap to prioritize "Repository integration for analysis snapshots" over "Git UI workflows".
   - Moved "Local Agent / Desktop Agent for local uncommitted change tracking" explicitly to **Future/Post-MVP Milestones**.

3. **`docs/SRS/03-functional/git.md` (Repository Integration)**
   - Renamed module conceptually to "Repository Integration".
   - Removed all Git mutation workflows from the Web UI (e.g., stage, commit, push, pull, merge conflict resolution).
   - Replaced these with a simplified "Sync Snapshot (Fetch)" workflow that updates server-side analysis snapshots from remote repositories.

4. **`docs/SRS/05-api.md`**
   - Removed API endpoints for Git mutation (`POST /git/stage`, `POST /git/commit`, `POST /git/push`, `POST /git/pull`, `POST /git/merge`).
   - Rebranded the `### Git` section to `### Repositories` and exposed read-only snapshot/metadata APIs (`GET /repository/branches`, `GET /repository/commits`).

5. **`docs/SRS/08-workflows.md`**
   - Removed "Git Commit And Push" and "Pull With Conflict" workflows.
   - Introduced "Sync Snapshot (Fetch)" workflow.
   - Added MVP decision confirming manual conflict resolution occurs in user's local Git client, outside the GodForge Web UI.

6. **`docs/SRS/09-ui-ux.md`**
   - Replaced "Git UI" screen and capabilities with "Snapshot History" views.
   - Removed dangerous Git operations (Push, Pull/Merge, Delete branch) from the confirmation dialogs list, substituting them with "Sync Snapshot" warnings.

7. **Other Functional Documents (`parser.md`, `notification-activity.md`)**
   - Adjusted activity log events from `git.commit`, `git.push`, etc., to `repo.sync`.
   - Ensured the parser documentation correctly reflects that it triggers off snapshot syncs, not local file changes or Web UI commits.

## Conclusion
The GodForge documentation is now strictly aligned with the MVP scope. The development team can proceed with implementing the pagination rules and other features knowing the system boundaries are cleanly defined around remote snapshot analysis.

