---
name: test-quality
description: Skill for writing and maintaining high-quality tests across the GodForge stack.
---

# Test Quality Standards

## When to Use
Use this skill when developing unit, integration, or regression tests for backend or frontend logic.

## Required Reading
- `docs/SRS/11-testing-acceptance.md`
- `docs/QUALITY_GATES.md`

## Workflow
1. Determine test boundary (Unit vs Integration).
2. For backend, setup `xUnit` and `Moq`. For frontend, setup `Vitest`.
3. Follow the `MethodName_StateUnderTest_ExpectedBehavior` naming convention.
4. Run tests locally and verify coverage.

## Mandatory Checks
- Domain and Application layers require Unit Tests.
- Infrastructure adapters require Integration Tests using real or containerized services.
- A failing test must be written to prove a bug exists before fixing it.

## Forbidden Actions
- Do not over-mock internal dependencies; only mock external boundaries.
- Do not change tests solely to make broken code pass.

## Completion Checklist
- [ ] Test names follow conventions.
- [ ] Boundary layers are respected.
- [ ] Tests pass locally.
- [ ] Code coverage is acceptable.
