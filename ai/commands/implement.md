# `ai-platform implement`

## Command purpose

Progressively replace `run` with a safer task implementation flow.

`run` remains the compatibility path until `implement` exists and is proven.

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

- Implemented changes.
- Validation results.
- Task lifecycle update when safe.
- Commit and push when policy allows.

## Task lifecycle

- Select or receive a task.
- Move from `pending` to `in-progress`.
- Read required context.
- Implement scoped changes.
- Validate.
- Move to `review` after implementation and validation.
- Use `blocked` when a concrete dependency prevents progress.
- Use `obsolete` only when an explicit review/reconciliation decision says the task should not be executed.
- Do not close directly to `done` unless a future explicit policy allows it.

## Validation requirements

- Run build/test commands relevant to the changed files.
- Run integration tests only when configured.
- Report skipped validation explicitly.
- Do not mark completion if required validation fails.

## Commit/push policy

- Commit only scoped, related changes.
- Push only when the branch has a configured upstream and repository policy allows it.
- Do not commit generated build artifacts such as `bin/`, `obj/`, or packages.

## What it must not do

- Must not ignore task scope.
- Must not auto-complete high-risk work without review.
- Must not overwrite unrelated user changes.
- Must not replace `run` until implemented and documented.
- Must not skip the `review` barrier before `done` without explicit future policy.

## Acceptance criteria

- Implements one selected task safely.
- Honors `team` and `supporting_teams` metadata.
- Runs appropriate validation.
- Preserves review before final done state when review is configured.
