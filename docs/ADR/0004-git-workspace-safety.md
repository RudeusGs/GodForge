# ADR 0004: Secure isolated Git workspaces

## Status
Accepted

## Context
Repositories are untrusted and may contain malicious paths, symlinks, large files, secrets or crafted Git data.

## Decision
Repository jobs use isolated directories under one configured root, owner-token distributed locks, quotas, command timeouts and cleanup. Git commands use argument arrays. No repository code is executed. Remote URLs pass SSRF controls.

## Consequences
### Positive
- Reduced host compromise and cross-project leakage risk.

### Negative
- More validation, cleanup and resource accounting.

## Constraints enforced on implementation and AI agents
- Never concatenate shell commands.
- Never follow a path or symlink outside the workspace.
- Never mount the Docker socket or broad host directories into workers.
