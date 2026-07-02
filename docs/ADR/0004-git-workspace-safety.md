# ADR 0004: Git Workspace Safety

## Status
Accepted

## Context
GodForge manages local clones of remote Git repositories. Running concurrent operations (like fetch, pull, or checkout) on the same workspace can corrupt the `.git` directory or lead to inconsistent states. Furthermore, handling Git credentials (PATs, tokens) improperly poses a severe security risk.

## Decision
Git operations will be governed by strict safety and isolation rules:

1. **Distributed Locks**: Any operation that mutates or relies on the state of a local repository workspace (clone, fetch, analyze, push, merge) MUST acquire a distributed Redis lock specific to that repository ID. The lock must include a TTL and an owner token.
2. **Execution Context**: We will not pass raw, unvalidated user input into shell commands to execute Git. Library APIs (like libgit2sharp, though C# native execution preferred) or highly sanitized argument arrays will be used. No string concatenation for shell commands.
3. **Credential Security**: Personal Access Tokens (PATs) and other Git credentials must be encrypted using AES-256-GCM before storage in the database. They must NEVER be logged or returned in API responses.

## Consequences
### Positive
- Guaranteed safety against concurrent modifications of Git workspaces.
- High security standards for user credentials and shell execution.

### Negative
- Performance overhead of acquiring distributed locks.
- Possibility of deadlocks or stale locks if workers fail ungracefully, mitigated by TTLs.

## Constraints enforced on AI agents
- Always validate RBAC before attempting any Git operation.
- Always use the distributed lock mechanism for workspace access.
- Never log full repository URLs containing credentials or output containing credentials. Sanitize `stderr`/`stdout`.
- Do not automatically resolve Git merge conflicts without an explicit policy action.
