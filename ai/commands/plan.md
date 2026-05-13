# `ai-platform plan`

## Command purpose

Convert roadmap items or functional requests into detailed task files in `ai/tasks/pending`.

The current CLI `plan` command is only a simple guidance helper. This spec describes a future roadmap-driven task generation command.

## Teams

- Primary team: product
- Supporting teams: orchestration, docs, qa

## Inputs

- roadmap item ID or functional request
- `ai/roadmap.md`
- `ai/current-state.md`
- `ai/task-template.md`
- `ai/teams/`
- `ai/project-memory/`
- relevant repository evidence

## Outputs

- One or more Markdown task files in `ai/tasks/pending`.
- A planning summary listing generated tasks, assumptions, and non-goals.

## Task format expected

Generated tasks must follow `ai/task-template.md` and include:

- `team`: one primary team.
- `supporting_teams`: optional list of directly involved teams.
- files to read first.
- expected files to modify.
- validation plan.
- change budget notes.

## Splitting and generation rules

- Prefer one logical change per task.
- Prefer fewer than five files and under 200 lines per task.
- Do not generate more than three follow-up tasks at once unless explicitly requested.
- Do not create duplicate tasks for work already pending, in progress, or done.
- Do not generate tasks without enough context to make them actionable.

## Acceptance criteria

- Creates task files only under `ai/tasks/pending`.
- Assigns clear `team` and `supporting_teams`.
- Keeps tasks reviewable and scoped.
- Distinguishes roadmap-driven planning from the current simple CLI helper.
