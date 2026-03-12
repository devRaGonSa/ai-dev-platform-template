# TASK-XXX

## Goal
Clear description of the objective.

## Context
Explain where in the repository the change happens and why it matters.

## Steps

1. Step one
2. Step two
3. Step three

## Files to Read First

List the most relevant files that should be inspected before implementing the task.

Examples:

- README.md
- scripts/codex-runner.ps1
- ai/orchestrator/feature-planner.md
- src/FeatureModule/FeatureService.cs

Rules:

- The agent should read these files before making any change.
- Prefer reading the current implementation, related configuration, and nearby tests or docs.
- Keep the list small (3-6 files).

## Expected Files to Modify

List the files that are expected to change during this task.

Examples:

- README.md
- scripts/run-integration-tests.ps1
- ai/repo-context.md
- src/FeatureModule/FeatureService.cs

Rules:

- Prefer modifying only the files listed here.
- If additional files are required, explain why in the commit message.
- Do not modify unrelated files.

## Constraints

- Follow the architecture and conventions of the current repository
- Do not modify unrelated files
- Keep the change minimal
- Prefer small commits

## Validation

Before completing the task ensure:

- repository-relevant build steps succeed, if applicable
- repository-relevant tests succeed, if applicable
- no new warnings or obvious regressions are introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Split the work into additional tasks if limits are exceeded.
