# TASK-011

## Goal
Refactor `scripts/codex-runner.ps1` so the platform template is project-agnostic and does not depend on repository-specific files or workflows.

## Context
The current worker script includes commands tied to specific projects (for example custom solution files, EF migrations, Docker test compose files, and project-specific paths like FormularioBoda). These assumptions break in generic repositories using the platform template.

## Steps

1. Review `scripts/codex-runner.ps1` for project-specific commands and paths.
2. Remove references to:
   - FormularioBoda
   - specific `.sln` files
   - specific project paths
   - `docker-compose.test.yml`
   - EF migration commands tied to one project
3. Keep worker behavior limited to a generic loop:
   - `git pull`
   - detect pending tasks
   - run Codex
   - commit/push results if applicable
4. Ensure logging/output remains clear for each step.
5. Ensure script handles missing pending tasks gracefully without failing.

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
