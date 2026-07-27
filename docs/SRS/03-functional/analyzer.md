# Analyzer, Health and AI Advisory

## Deterministic analyzer

The rule engine generates measured findings and health score. Initial rules include missing project file, broken resource/script reference, cyclic dependency and oversized scene/repository limits.

## AI advisory

Gemini receives only selected, bounded and redacted context plus deterministic facts. Output must be JSON, schema-validated and stored separately from deterministic findings.

AI failure results in a degraded analysis state; deterministic output remains available. UI labels AI content as generated recommendation and shows provider/model/prompt version and evidence references.
