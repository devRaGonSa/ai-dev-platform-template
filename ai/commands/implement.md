# `ai-platform implement`

## Command purpose

Progressively replace `run` with a safer task implementation flow.

`run` remains the compatibility path until `implement` fully replaces it.

`implement` v1 now exists as a conservative preparation step. It does not execute Codex directly or implement code automatically.

## Teams

- Primary team: platform
- Supporting teams: orchestration, qa, devops, docs

## Inputs

- selected task path or first eligible pending task
- task metadata including `team` and `supporting_teams`
- `AGENTS.md`
- `ai/task-template.md`
- team docs
- repository validation commands

## Outputs

- `ai/reports/implementation-prompt.md`
- selected task moved from `pending` to `in-progress` when safe
- console summary with warnings, next step guidance, and the recommended explicit `ai-platform task move --task TASK-xxxx --to review` command

## Task lifecycle

- Select or receive a task.
- In v1, only tasks in `pending` are eligible for selection.
- In v1, move from `pending` to `in-progress` unless `--dry-run` or `--no-move` is used.
- Generate an implementation prompt that includes the full task content.
- Include the recommended next command for moving the task to `review` after implementation and validation.
- Leave the actual implementation, validation, commit, and push to the Codex execution step.
- Use `ai-platform task move` for explicit lifecycle changes beyond the initial `pending -> in-progress` move.
- Move to `review` only through an explicit later action, not automatically in v1.
- Use `blocked` when a concrete dependency prevents progress.
- Use `obsolete` only when an explicit review/reconciliation decision says the task should not be executed.
- Do not close directly to `done` unless a future explicit policy allows it.

## Validation requirements

- In v1, validate task structure mechanically before generating the prompt.
- Warn when `team`, acceptance criteria, validation, roadmap item/justification, or commit/push guidance are weak or missing.
- Deeper build/test validation remains the responsibility of the later implementation step.

## Commit/push policy

- `implement` v1 must not commit or push.
- The generated prompt must remind Codex to commit scoped changes and push the branch after implementation.
- The generated prompt must recommend moving the task to `review` after implementation and validation, not directly to `done`.
- Do not commit generated build artifacts such as `bin/`, `obj/`, or packages.

## What it must not do

- Must not ignore task scope.
- Must not auto-complete high-risk work without review.
- Must not overwrite unrelated user changes.
- Must not execute Codex directly in v1.
- Must not implement application code automatically in v1.
- Must not move tasks directly to `done`.
- Must not move tasks automatically to `review`.
- Must not close the lifecycle automatically.
- Must not replace `run` until fully implemented and documented.
- Must not skip the `review` barrier before `done` without explicit future policy.

## Acceptance criteria

- Selects one pending task safely, or reports that no eligible task exists.
- Supports `--task`, `--dry-run`, and `--no-move`.
- Can move `pending` to `in-progress` without overwriting an existing destination task.
- Generates an operational prompt with the full task content.
- Recommends the explicit next task move command for review.
- Does not execute Codex, move to `done`, or close the task lifecycle automatically.
