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
- Review outcome, findings, validation summary, and recommended next step.

## Review outcomes

- `done`: evidence supports completion.
- `blocked`: external dependency prevents completion.
- `rework`: changes are needed before completion.

## Behavior

- Must review acceptance criteria and changed files.
- Must identify risks, missing tests, and documentation drift.
- Must not approve high-risk changes automatically without evidence.
- Should not move tasks to `done` without an explicit policy and sufficient evidence.

## Acceptance criteria

- Produces an evidence-backed review report.
- Validates task scope and expected files.
- Flags missing validation or high-risk changes.
- Recommends done, blocked, or rework.
