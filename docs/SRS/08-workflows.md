# 8. End-to-End Workflows

## WF-01 — Hosted repository analysis

1. Organization member creates a project.
2. ProjectOwner/Maintainer requests a hosted repository.
3. API creates repository state and outbox event.
4. Provisioning worker creates Forgejo repository and synchronizes permissions.
5. Developer clones and pushes a Godot project.
6. Forgejo sends a signed webhook.
7. API validates signature/replay and creates an idempotent pipeline job.
8. Worker resolves commit, acquires lock and creates isolated workspace.
9. Validation, parser, graph and health stages run.
10. Optional AI advisory runs after redaction.
11. Results commit; dashboard, activity and notifications update.

## WF-02 — Linked external repository

1. Maintainer submits sanitized HTTPS remote and credential reference.
2. API validates remote policy and creates sync job.
3. Worker clone/fetches immutable revision.
4. Same analysis pipeline as hosted mode runs.
5. Manual sync or provider webhook creates later revisions.

## WF-03 — Incremental revision analysis

1. New commit is compared with a compatible completed baseline.
2. Changed files are calculated from Git.
3. Reverse dependencies expand affected scope.
4. If baseline/version/impact is unsafe, full analysis is selected and reason recorded.
5. Incremental output is merged into a new immutable result model.
6. Health and graph comparison are displayed.

## WF-04 — Finding remediation

1. Reviewer opens a health finding.
2. Reviewer comments, assigns and sets priority.
3. Developer changes source and pushes a new commit.
4. Analysis identifies whether stable finding key remains.
5. Finding is resolved with commit reference or reopened if it reappears.
6. Activity and notifications record the lifecycle.

## WF-05 — Public source with protected asset

1. Developer uploads asset to Asset Vault.
2. Service validates, stores and versions object.
3. Project manifest maps `res://` path to object/version/checksum and visibility.
4. Source repository can be public without protected bytes.
5. Authorized client requests hydration and receives short-lived URL.
6. Client downloads and verifies checksum.
7. Download is audited.

## WF-06 — Report export

1. Reviewer selects revision/comparison and format.
2. API creates idempotent export job.
3. Worker renders report with provenance and AI labels.
4. Artifact is stored in MinIO and metadata committed.
5. Authorized user receives signed download.

## WF-07 — Failure and degraded behavior

- Gemini failure: deterministic report completes, AI status is degraded.
- RabbitMQ outage: job and outbox remain pending until dispatch recovery.
- Worker crash: stale heartbeat triggers timeout/retry policy.
- Forgejo outage: hosted Git mutation pauses; persisted analysis remains readable.
- MinIO outage: asset/report job does not claim completed publication.
- Project archived while job queued: worker revalidates and fails/cancels safely.
