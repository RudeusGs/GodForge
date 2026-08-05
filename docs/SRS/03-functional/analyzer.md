# Project Health Analysis

## Purpose

Apply versioned deterministic rules and calculate reproducible project-health findings and scores.

## Actors

Worker generates; Viewer reads; Maintainer manages profile/suppression.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-12.1 | Run versioned structure, dependency, asset, security and maintainability rules | Must |
| FR-12.2 | Store evidence, severity, category and recommendation | Must |
| FR-12.3 | Calculate category and overall health score | Must |
| FR-12.4 | Support justified suppression without deleting evidence | Must |
| FR-26 | Support incremental affected-scope analysis with full fallback | Must |

## Main flow

1. Select parser run, rule-set version and analysis profile.
2. Evaluate deterministic rules.
3. Persist findings using stable rule/file/location identity.
4. Calculate scores from documented weights.
5. Apply valid suppressions as display/status metadata, not evidence deletion.
6. Publish report and compare with prior revision.

## Error and edge cases

- Required parser output missing.
- Rule execution error is isolated and reported.
- Incremental baseline incompatible or impact too broad.
- Health score cannot be calculated from incomplete critical categories.

## Authorization and security

- AI cannot create or alter authoritative findings/score.
- Suppression requires permission, reason, optional expiry and audit.
- Evidence must not contain secret values.

## Async processing and idempotency

- Health stage runs after parser/graph and before AI.

## Acceptance criteria

- `AC-FR-12-01`: Identical revision/parser/rule/profile yields identical report.
- `AC-FR-12-02`: Score formula is documented and test-covered.
- `AC-FR-26-01`: Incremental result equals full result for evaluation corpus.
- `AC-FR-12-03`: Suppressed issue remains historically auditable.

## Related API

- Health report, finding, suppression, comparison and profile endpoints

## Related data

- `analysis.analysis_runs`, `analysis.health_reports`, `analysis.health_findings`, `analysis.health_rules`, `analysis.health_rule_versions`, `analysis.health_issue_suppressions`

## Tests and observability

- Test suites: `TC-HEALTH-*` and `TC-INCR-*`.
- Golden-corpus tests verify deterministic score/finding output and incremental/full equivalence.
- Metrics: rule duration/failure, finding count by severity, fallback reason and analysis completeness.
