# Project Decisions

This file records decisions that should guide future platform work.

## Current decisions

- Use `ai-platform.json` as the minimal platform configuration file.
- Keep configuration small until stable behavior exists.
- Use conservative refresh behavior in the future, with dry-run as the default.
- Track managed artifacts explicitly before updating existing installations.
- Keep `doctor` focused on readiness checks.
- Keep future `status` behavior separate from `doctor`; status should report state, while doctor should diagnose readiness.
- Keep `run` as the current execution flow until a dedicated `implement` command exists.
- Do not implement real multi-agent orchestration before the team model and command specs are clear.
- Do not introduce complex template versioning before the update and reconciliation model is defined.
- Do not move tasks automatically to `done` without review.
- Prefer documentation that describes current behavior honestly over aspirational command descriptions.
- Adopt a documented specialized team model under `ai/teams/`.
- Treat teams as planning and review ownership boundaries, not autonomous agents yet.
- Future tasks may assign one primary `team` and optional `supporting_teams`.
- Adopt `ai/commands/` as the documentation contract for future commands.
- Specify commands before implementing them.
- Do not promise CLI availability just because a command spec exists.
- Adopt an extended task lifecycle with `review`, `blocked`, and `obsolete` states.
- Treat `review` as the barrier before `done`.
- Use `blocked` and `obsolete` to avoid executing tasks that are not actionable or no longer valid.
- Future automation must not jump directly from `pending` to `done` without validation.
