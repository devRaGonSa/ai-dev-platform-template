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
- Separate analysis (`review`) from action (`task move`) in the CLI workflow.
- Require an explicit command to move tasks between lifecycle states.
- Require `--force` for dangerous jumps such as reopening `done` or skipping directly to `done`.
- Future automation must not move tasks between states without clear lifecycle rules.
- The platform must support installation as local tooling inside consumer repositories.
- The template source repository continues to version its AI platform files.
- Consumer repositories may opt into local-only behavior through a managed `.gitignore` block.
- v1 must not automatically untrack previously committed platform files; that remains an explicit Git decision.
- Do not move the runtime into `.ai-platform/` yet.
- Document isolated consumer install as a possible v2 evolution before changing path resolution or file layout.
- Keep `consumer-local` plus `git-ignore` as the supported v1 local-tooling story.
