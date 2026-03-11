# TASK-010

## Goal
Fix Codex CLI invocation in `scripts/codex-runner.ps1` to ensure compatibility with the current CLI using a generic, platform-oriented invocation style.

## Context
The worker script invokes Codex as part of task processing. Some flags (for example `--auto`) are no longer supported by current Codex CLI versions and can cause worker failures.

## Steps

1. Review current Codex invocation in `scripts/codex-runner.ps1`.
2. Remove unsupported flags such as `--auto`.
3. Replace invocation with:
   - `codex "Follow the workflow defined in AGENTS.md and process the pending tasks."`
4. Improve resilience so the worker loop does not fail hard if Codex CLI arguments change again:
   - keep invocation minimal and stable
   - keep implementation generic and platform-oriented
   - handle Codex process failures gracefully and continue next cycle
5. Keep script behavior otherwise unchanged.

## Files to Read First

- scripts/codex-runner.ps1
- AGENTS.md
- ai/task-template.md

## Expected Files to Modify

- scripts/codex-runner.ps1

## Constraints

- Follow ASP.NET Core MVC architecture
- Do not modify unrelated files
- Keep the change minimal
- Prefer small commits

## Validation

Before completing the task ensure:

- dotnet build succeeds
- dotnet test succeeds
- no new warnings introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Prefer fewer than 3 commits.
- Split the work into additional tasks if limits are exceeded.
