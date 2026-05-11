# `ai-platform roadmap-status`

## Command purpose

Compare `ai/roadmap.md` with `ai/current-state.md`, task state, and repository evidence to report roadmap progress.

## Teams

- Primary team: product
- Supporting teams: orchestration, platform, docs

## Inputs

- `ai/roadmap.md`
- `ai/current-state.md`
- `ai/tasks/`
- relevant repository files that provide evidence
- prior reports when available

## Outputs

- Expected report path: `ai/reports/roadmap-status.md`
- Roadmap item status table with evidence and uncertainty notes.

## Classification

- `done`: implemented or documented with clear evidence.
- `partial`: some evidence exists but the outcome is incomplete.
- `planned`: intended but not started.
- `blocked`: known dependency prevents progress.
- `unknown`: evidence is missing or contradictory.

## Behavior

- Read-only.
- Must not modify `ai/roadmap.md` automatically.
- Must not infer completion without evidence.

## Acceptance criteria

- Reports every roadmap item.
- Cites the local evidence used for each classification.
- Flags contradictions between roadmap and current state.
- Leaves roadmap and task files unchanged.
