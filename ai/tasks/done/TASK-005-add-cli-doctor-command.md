# TASK-005

## Goal
Add an `ai-platform doctor` command to validate whether a repository is ready to use the AI platform.

## Context
The CLI entry point is in `ai-platform-cli/Program.cs`. We need a doctor command that performs repository readiness checks and prints a clear, readable report with actionable guidance when something is missing.

## Steps

1. Extend command routing in `ai-platform-cli/Program.cs` to support `doctor`.
2. Implement a doctor routine that verifies:
   - `ai` directory exists
   - `scripts` directory exists
   - `AGENTS.md` exists
   - `.git` exists
   - `codex` is available in PATH
3. Print output in a clear, readable format:
   - header: `AI Platform Doctor`
   - one line per check showing pass/fail status
   - final line `Platform ready.` when all checks pass
4. If any check fails, print a helpful message explaining what to do next for that specific missing item.
5. Update help output so `doctor` appears in the command list.

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

- dotnet build succeeds
- dotnet test succeeds
- no new warnings introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Prefer fewer than 3 commits.
- Split the work into additional tasks if limits are exceeded.
