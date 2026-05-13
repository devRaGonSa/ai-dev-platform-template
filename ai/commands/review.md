# `ai-platform review`

## Command purpose

Review implemented task work before it is marked done.

## Teams

- Primary team: qa
- Supporting teams: orchestration, product, security, docs

## Inputs

- implemented task file
- git diff and changed files
- validation results
- acceptance criteria
- roadmap/current-state context when relevant
- security or docs requirements when relevant

## Outputs

- Expected report path: `ai/reports/task-review.md`
- Review outcome, findings, validation summary, recommended next step, and recommended explicit `ai-platform task move` command when applicable.

## Review outcomes

- `done`: evidence supports completion.
- `blocked`: external dependency prevents completion.
- `rework`: changes are needed before completion.
- `obsolete`: task is no longer valid and should not be executed.

## Behavior

- Must review acceptance criteria and changed files.
- Must identify risks, missing tests, and documentation drift.
- Must not approve high-risk changes automatically without evidence.
- Must not move tasks automatically; `ai-platform task move` is the explicit lifecycle action.
- Must recommend the next `ai-platform task move` command when the outcome maps to a clear state transition.
- Must not execute the recommended command.
- Should not move tasks to `done` without an explicit policy and sufficient evidence.
- Should treat `review` as the normal state before `done`.
- May recommend `blocked` or `obsolete` instead of `done` when the task is not safely completable.

## Acceptance criteria

- Produces an evidence-backed review report.
- Validates task scope and expected files.
- Flags missing validation or high-risk changes.
- Recommends done, blocked, or rework.
- Includes a `## Recommended next command` section in the report.
