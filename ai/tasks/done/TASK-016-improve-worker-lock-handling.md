# TASK-016

## Goal
Improve worker lock handling in `scripts/codex-runner.ps1` so stale lock files do not block execution and waiting only happens when a real worker is running.

## Context
The worker can become stuck when `ai/worker.lock` exists from a previous interrupted run, or when old Codex-related processes are left behind. The script should distinguish between active and stale lock states and recover automatically.

## Steps

1. Review current lock handling logic in `scripts/codex-runner.ps1`.
2. If `ai/worker.lock` exists, verify whether a real worker process is active.
3. If no real worker process is active, treat the lock as stale.
4. Automatically remove stale lock files.
5. Only wait when a real worker is actually running.
6. Print clear messages:
   - `Stale worker lock detected. Removing it.`
   - `Worker already running. Waiting...`

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
