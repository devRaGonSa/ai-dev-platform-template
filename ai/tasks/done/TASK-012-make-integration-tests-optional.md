# TASK-012

## Goal
Make integration test execution optional so the platform only runs integration tests when a project explicitly configures them.

## Context
Current workflow instructions can require integration testing unconditionally. In template-based and project-agnostic use cases, `scripts/run-integration-tests.ps1` may not exist and should not cause failure.

## Steps

1. Locate where integration tests are invoked in platform scripts/workflow.
2. Add conditional behavior:
   - if `scripts/run-integration-tests.ps1` exists, run it
   - if it does not exist, skip integration tests
3. When integration tests are skipped, print:
   - `No integration tests configured.`
4. Do not assume `docker-compose.test.yml` exists.
5. Do not assume any specific solution or project name exists.
6. Ensure this behavior is documented where integration validation is described.

## Files to Read First

- AGENTS.md
- scripts/codex-runner.ps1
- ai/task-template.md

## Expected Files to Modify

- AGENTS.md
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
