# ADR 0013: Do not execute untrusted Godot projects

## Status
Accepted

## Context
Godot projects may contain editor plugins, native extensions, scripts and binaries. Running them introduces remote-code-execution risk.

## Decision
Core GodForge analysis is static. It does not open the project in Godot Editor, run GDScript, load native extensions, build or export games. If future dynamic analysis is researched, it must use a separate disposable sandbox service with no production credentials, restricted network and explicit opt-in.

## Consequences
### Positive
- Strongly reduced execution risk and simpler production controls.

### Negative
- Some runtime-only issues cannot be detected.

## Constraints enforced on implementation and AI agents
- Never invoke project-provided scripts or binaries in the API/standard worker.
- Do not add dynamic execution without a new ADR and threat-model update.
