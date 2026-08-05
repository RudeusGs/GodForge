# Finding Collaboration

## Purpose

Turn health findings into team remediation work without building a general-purpose issue tracker.

## Actors

ProjectOwner, Maintainer, Developer, Reviewer; Viewer reads only.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-24.1 | Comment on a finding | Must |
| FR-24.2 | Assign finding, priority and due date | Must |
| FR-24.3 | Open, in-progress, resolved, ignored, false-positive and reopened states | Must |
| FR-24.4 | Link resolution to commit/revision | Must |
| FR-24.5 | Notify assignees and preserve history | Must |

## Main flow

1. User opens a deterministic finding.
2. Authorized collaborator comments or assigns it.
3. Status changes are appended to history.
4. A later analysis checks whether the underlying finding identity remains.
5. Resolved finding may reopen if it reappears.

## Error and edge cases

- Finding belongs to another project/revision.
- Assignee is not an active eligible member.
- Ignored/false-positive state lacks required reason.
- Concurrent status update conflict.

## Authorization and security

- Comments are escaped/sanitized.
- Status/suppression permissions are distinct.
- Complete actor/timestamp/history is retained.
- Notifications never reveal a finding to a user lacking project access.

## Async processing and idempotency

- No module-specific durable job is created by read operations.
- Writes, where present, use normal transaction/concurrency rules and do not perform hidden heavy work.

## Acceptance criteria

- `AC-FR-24-01`: State transition rules are enforced.
- `AC-FR-24-02`: Finding can be traced from first occurrence through resolution commit and recurrence.
- `AC-FR-24-03`: Removal of member prevents new access but preserves historical attribution.

## Related API

- Finding comments, assignment, state and history endpoints

## Related data

- `collab.finding_assignments`, `collab.finding_comments`, `collab.finding_status_history`, `analysis.health_findings`, `collab.notifications`

## Tests and observability

- Test suite: `TC-FIND-*`, including transition, assignment, suppression, recurrence and removed-member cases.
- Metrics: open findings by status/severity, transition failures and notification deduplication.
- Audit/activity records include actor, correlation and revision references.
