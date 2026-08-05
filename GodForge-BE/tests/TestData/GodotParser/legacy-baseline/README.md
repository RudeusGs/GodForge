# Legacy parser baseline

This corpus freezes behavior of `DeterministicProjectAnalyzer` before the parser
alignment refactor. It is test data only and must never be executed by Godot or by
project-provided scripts.

## Covered current behavior

- root `project.godot` existence only;
- deterministic file counts and ignored `.git`/`.godot` directories;
- missing `ext_resource` diagnostics;
- `res://../` traversal diagnostics;
- file/repository quota tests built around this corpus;
- cooperative cancellation before supported text-file processing.

## Known legacy limitations

- `project.godot` content is not parsed;
- summary output has no parser/schema/profile identity or canonical hash;
- `.tscn`, `.tres` and `.gd` metadata is not normalized;
- dependency extraction rescans files separately and may silently ignore reads;
- symlink/reparse-point handling and path-depth policy are not represented in the
  returned summary;
- repository byte accounting is based on the truncated file list after the file
  count limit is applied;
- diagnostics have no line/column/parser version.
