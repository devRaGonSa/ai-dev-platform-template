# `ai-platform reconcile`

## Command purpose

Cross-check roadmap, current state, tasks, code, and reports to detect workflow drift.

## Teams

- Primary team: orchestration
- Supporting teams: qa, product, platform

## Inputs

- `ai/roadmap.md`
- `ai/current-state.md`
- `ai/tasks/pending`
- `ai/tasks/in-progress`
- `ai/tasks/review`
- `ai/tasks/blocked`
- `ai/tasks/obsolete`
- `ai/tasks/done`
- `ai/reports/` when present
- repository evidence from relevant files

## Outputs

- Expected report path: `ai/reports/task-reconciliation.md`
- Candidate actions with evidence and risk notes.

## What it should detect

- Obsolete tasks.
- Duplicate tasks.
- Tasks completed in code but still pending.
- Done tasks without enough evidence.
- Roadmap/current-state/task mismatches.

## Candidate actions

- propose `review`
- propose `blocked`
- propose `obsolete`
- propose new planning task
- propose current-state or roadmap documentation update

## Behavior

- Must not move tasks to `done` automatically.
- May propose lifecycle changes to `review`, `blocked`, or `obsolete`, but should not apply them without an explicit implementation/review flow.

## Acceptance criteria

- Produces a report with evidence-backed candidate actions.
- Separates recommendations from mutations.
- Avoids destructive changes.
- Preserves human or review-step control before completion.
