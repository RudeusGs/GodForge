# Repository and Git

## Repository modes

- `ExternalLinked`: GodForge clone/fetches an existing HTTP(S) repository.
- `InternalHosted`: Forgejo owns Git protocol/storage; GodForge provisions and synchronizes permissions through an adapter.

## MVP workflow

1. Project owner/admin links a repository.
2. API validates URL/provider and stores sanitized clone metadata.
3. Developer triggers sync/analysis.
4. API creates job and publishes message, returning `202`.
5. Worker syncs a managed workspace, resolves commit SHA and stores a revision snapshot.
6. File/commit read views use persisted metadata and bounded Git reads.

## Security

- No embedded credentials in clone URL.
- Credentials encrypted at rest and never logged.
- Workspace path is derived from repository ID under one configured root.
- Only HTTP(S) linked repositories are supported by the initial adapter.
- Hosted Git uses Forgejo; GodForge does not implement Git protocol.
