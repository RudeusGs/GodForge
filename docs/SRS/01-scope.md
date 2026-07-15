# Scope

## MVP in scope

- Authentication, projects, members and project-scoped RBAC.
- External linked HTTP(S) Git repository.
- Async clone/fetch, revision snapshot and job monitoring.
- Branch/commit/file read views with size and content-type limits.
- Deterministic Godot parser for `project.godot`, `.tscn`, `.tres` and `.gd`.
- Health rules, dependency graph and revision history.
- Bounded, redacted context generation.
- Manual Gemini advisory report with structured output and evidence references.
- Optional hosted Git through Forgejo after linked-repository pipeline is stable.

## Explicit non-goals

- Implementing Git Smart HTTP/SSH protocol.
- Full GitHub replacement: Actions, Packages, Wiki, full Issues/PR engine.
- Web IDE, local uncommitted-file watcher or merge-conflict editor.
- Running/exporting untrusted games on API/worker hosts.
- Automatic AI code changes or automatic pushes.
- Using Gemini as the health score source.
