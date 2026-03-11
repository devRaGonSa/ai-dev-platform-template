# TASK-017

## Goal
Fix Codex CLI detection in `ai-platform doctor` so it reliably reports Codex availability in Windows environments.

## Context
`codex --help` works correctly in the terminal, but `ai-platform doctor` currently reports `[MISSING] codex in PATH`. The detection logic should match real shell availability and remain generic across environments where possible.

## Steps

1. Review Codex detection logic in `ai-platform-cli/Program.cs`.
2. Update detection to use a reliable Windows-compatible approach.
3. Ensure detection succeeds when `codex --help` works in the current shell.
4. Keep implementation generic and cross-environment friendly where possible.
5. Ensure doctor output reports:
   - `[OK] codex in PATH` when Codex is available
6. If Codex is not available, keep the current helpful error guidance message.

## Files to Read First

- ai-platform-cli/Program.cs
- AGENTS.md
- ai/task-template.md

## Expected Files to Modify

- ai-platform-cli/Program.cs

## Constraints

- Follow ASP.NET Core MVC architecture
- Do not modify unrelated files
- Keep the change minimal
- Prefer small commits

## Validation

Before completing the task ensure:

- `codex --help` works
- `ai-platform doctor` correctly reports Codex detection
- dotnet build succeeds
- dotnet test succeeds
- no new warnings introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Prefer fewer than 3 commits.
- Split the work into additional tasks if limits are exceeded.
