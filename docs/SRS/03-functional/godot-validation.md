# Godot Project Validation Gateway

## Purpose

Validate that a revision is a supported, safe Godot project before parser and analysis stages.

## Actors

Worker, Project members reading results, Maintainer configuring policy.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-20.1 | Require and parse root `project.godot` | Must |
| FR-20.2 | Enforce repository, file and path quotas | Must |
| FR-20.3 | Reject traversal, symlink escape and dangerous content | Must |
| FR-20.4 | Detect secrets, generated/cache files and suspicious binaries | Must |
| FR-20.5 | Produce versioned validation status and diagnostics | Must |

## Main flow

1. Worker inventories immutable checkout without executing files.
2. Validate root marker, supported Godot version and path safety.
3. Apply count/size/type/secret/generated-file policies.
4. Persist `valid`, `warning`, `invalid`, `suspicious` or `review-required` result.
5. Continue or stop later stages according to profile and severity.

## Error and edge cases

- Missing/malformed `project.godot`.
- Unsupported version.
- Symlink or canonical path outside workspace.
- File count/size/repository quota.
- Binary/executable or secret detected.
- Partial read failure.

## Authorization and security

- Repository content is untrusted and not executed.
- Validation is deterministic and versioned.
- Secret values are never persisted in diagnostics; only rule, file and redacted evidence.
- Policy cannot be weakened below platform minimums.

## Async processing and idempotency

- Executed as the first repository pipeline stage after secure checkout.

## Acceptance criteria

- `AC-FR-20-01`: Malicious corpus cases are rejected without host escape.
- `AC-FR-20-02`: Same input/profile produces same validation result.
- `AC-FR-20-03`: Valid project advances to parsing; invalid project cannot be labeled successfully analyzed.

## Related API

- `GET /revisions/{sha}/validation`, analysis trigger/profile endpoints

## Related data

- `analysis.validation_runs`, `analysis.validation_findings`, `repo.repository_files`

## Tests and observability

- Test suite: `TC-VALID-*` against valid, malformed and malicious corpora.
- Metrics: validation duration, file/byte counts, finding code/severity and blocked revisions.
- Security tests cover traversal, symlink escape, dangerous file and secret redaction.
