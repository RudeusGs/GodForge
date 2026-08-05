# 16. Graduation Thesis Research and Evaluation

## Research questions

1. How accurately can deterministic parsing model scene, script, resource and asset relationships in representative Godot projects?
2. Can incremental analysis reduce processing time while preserving equivalence with full analysis?
3. Does dependency visualization improve developer understanding and impact assessment?
4. How reliable and useful is AI advisory when grounded in deterministic metadata compared with unstructured prompting?
5. Can independent asset visibility protect selected assets while preserving a public source workflow?

## Datasets

- Curated synthetic projects for exact ground truth.
- Open-source Godot projects with compatible licenses.
- Generated stress projects for size/depth/cycle cases.
- Malicious/safety corpus for paths, symlinks, secrets and oversized input.

Record repository commit/license, dataset hash, Godot version and expected annotations.

## Parser evaluation

- Precision/recall for supported reference types against labeled ground truth.
- Missing-reference detection.
- Reproducibility across repeated runs.
- Unsupported/malformed file behavior.

## Incremental analysis evaluation

- Full versus incremental result equivalence.
- Files processed and stage duration.
- CPU/memory/disk usage.
- Fallback frequency and reason.

## AI evaluation

- JSON schema validity rate.
- Evidence-reference correctness.
- Recommendation usefulness rated by developers/reviewers.
- Hallucination/unsupported-claim count.
- Token usage, latency and estimated cost.
- Behavior under prompt injection and secret-containing corpus.

AI evaluation must not treat model output as ground truth.

## Usability evaluation

Tasks may include locating a dependency, identifying impact of a script change, finding unused assets and understanding score regression. Measure task completion, time, errors and user-rated clarity.

## Security and reliability evaluation

- Execute the security test plan.
- Demonstrate duplicate/retry/cancellation/DLQ behavior.
- Perform backup/restore drill.
- Record limits and residual risks honestly.

## Reproducibility

Every experiment records application commit, configuration, parser/rule/prompt versions, environment resources, dataset hashes, warm/cold cache state and raw anonymized measurements.
