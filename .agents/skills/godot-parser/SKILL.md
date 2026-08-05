---
name: godot-parser
description: Implement deterministic Godot validation, parsing or health rules.
---

# Godot Parser

## Use when

Implement deterministic Godot validation, parsing or health rules.

## Required reading

- `docs/SRS/03-functional/godot-validation.md`
- `docs/SRS/03-functional/parser.md`
- `docs/SRS/03-functional/analyzer.md`
- ADR 0009, 0010, 0013

## Workflow

1. Define supported syntax/version and canonical normalized output.
2. Add safe bounded reader; never execute project content.
3. Produce diagnostics with redacted evidence.
4. Add corpus fixtures with expected canonical output.
5. Version parser/rule behavior.
6. Add incremental invalidation/fallback if affected.
7. Measure correctness and performance.

## Mandatory checks

- Canonical ordering and stable IDs.
- Path remains under workspace.
- Malformed file isolation.
- Historical version preservation.
- Full/incremental equivalence test.

## Forbidden

- Do not invoke Godot Editor or scripts.
- Do not silently ignore parse errors.
- Do not let AI create authoritative metadata/findings.

## Completion output

Report supported input, version change, corpus coverage, diagnostics and benchmarks.
