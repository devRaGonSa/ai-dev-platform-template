# `ai-platform analyze`

## Command purpose

Analyze the current repository/platform state and produce a situation report.

## Teams

- Primary team: orchestration
- Supporting teams: platform, qa, docs

## Inputs

- `ai/roadmap.md`
- `ai/current-state.md`
- `ai/tasks/pending`, `ai/tasks/in-progress`, `ai/tasks/done`
- `ai/architecture-index.md`
- `ai/repo-context.md`
- `ai/teams/`
- `ai/project-memory/`

## Outputs

- Expected report path: `ai/reports/project-analysis.md`
- Summary of current platform state, task state, risks, gaps, and evidence found.

## Behavior

- Read-only.
- Must not modify code.
- Must not move tasks.
- Must not update roadmap, current-state, or memory files automatically.

## What it should detect

- Missing or stale platform docs.
- Mismatches between current-state and repository evidence.
- Pending/in-progress/done task counts and obvious anomalies.
- Roadmap phases that have no supporting evidence.
- Risks or gaps that appear unresolved.

## Acceptance criteria

- Reads only documented inputs and safe repository metadata.
- Produces a clear report at the expected path.
- Separates evidence from inference.
- Leaves code and task lifecycle untouched.
