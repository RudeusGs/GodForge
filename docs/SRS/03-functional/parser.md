# Deterministic Godot Parser

The parser is authoritative for project structure. AI output cannot create or modify parser facts.

## Initial supported inputs

- repository inventory and `project.godot`;
- `.tscn` scene/node/resource references;
- `.tres` resources;
- `.gd` script inventory and attachments;
- diagnostics for malformed or unsupported syntax.

## Requirements

- Output is bound to repository ID, commit SHA and parser schema version.
- Re-running the same input is idempotent.
- Unsupported syntax produces diagnostics and does not crash the whole pipeline.
- Binary and generated directories are excluded.
- Every finding can reference a repository path and, when known, scene/node location.
