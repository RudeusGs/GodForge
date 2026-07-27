# Security — Blueprint

- JWT and project RBAC are enforced server-side.
- Repository credentials are encrypted using authenticated encryption and never returned after creation.
- Clone URLs stored/exposed by APIs are sanitized and contain no user info.
- Webhooks use HMAC/provider signatures and replay protection.
- Workspaces are under one configured root; path traversal and symlink escapes are rejected.
- Git commands use argument arrays, not shell concatenation.
- Worker containers run with constrained CPU/memory/disk and no Docker socket.
- Context generation excludes binaries, generated folders, `.env`, private keys and detected secrets.
- External AI is opt-in, server-side only, quota controlled and audited.
- Repository content is treated as untrusted data; prompt injection inside source files cannot override system instructions.
