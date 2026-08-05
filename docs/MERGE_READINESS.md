# Merge Readiness

A pull request is ready only when:

- [ ] Scope and requirement IDs are stated.
- [ ] Architecture and security impact are documented.
- [ ] Build, format, unit and relevant integration tests pass.
- [ ] API, error codes and RBAC remain synchronized.
- [ ] Migration is reviewed and reversible by forward fix.
- [ ] No secret or generated artifact is committed.
- [ ] Logs contain safe structured context.
- [ ] Async work is idempotent and cancellation-aware.
- [ ] Documentation and implementation status are updated.
- [ ] Reviewer can reproduce the main acceptance flow.
