# TASK-XXX

---
id: TASK-XXX
title: Clear task title
status: pending
type: platform
team: platform
supporting_teams: []
roadmap_item: ""
priority: medium
---

Allowed task statuses:

- `pending`
- `in-progress`
- `review`
- `done`
- `blocked`
- `obsolete`

## Goal
Clear description of the objective.

## Context
Explain where in the repository the change happens and why it matters.

## Implementation steps

1. Step one
2. Step two
3. Step three

## Files to read first

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

## Expected files to modify

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

## Acceptance criteria

- The requested change is implemented or the task is refined with clear blockers.
- Documentation is updated if behavior changes.
- Validation commands pass.
- No build artifacts are committed.

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

## Commit and push

At the end:

1. Run `git status`.
2. Stage the intended files.
3. Commit with a clear message.
4. Push the branch to the remote.

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Split the work into additional tasks if limits are exceeded.
