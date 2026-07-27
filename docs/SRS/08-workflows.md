# End-to-End Workflows

## Linked repository analysis

1. Owner/admin links a repository.
2. Developer requests analysis and receives `202 jobId`.
3. Worker clones/fetches repository into managed workspace.
4. Worker resolves immutable commit SHA.
5. Inventory and deterministic parser create metadata/diagnostics.
6. Rule engine produces measured health findings.
7. Context builder selects text, applies limits and redacts secrets.
8. Gemini runs only when requested/enabled.
9. Worker stores output and completes the durable job.
10. Frontend reads job/revision/report state from API.

## Hosted repository push

1. GodForge provisions repository through Forgejo.
2. User clones/pushes using Forgejo URL/token.
3. Forgejo sends signed push webhook.
4. GodForge idempotently creates the same revision-analysis pipeline.

## Degraded behavior

Parser/rule success plus Gemini failure is a successful deterministic analysis with AI status `failed/disabled`, not a total pipeline failure.
