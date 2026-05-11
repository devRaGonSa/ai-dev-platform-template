# Current Platform State

This document describes the repository as it exists now.

## Existing commands

The .NET CLI currently implements:

- `ai-platform init`: downloads a compatible template ZIP and copies missing platform files.
- `ai-platform analyze`: writes a read-only operational/documentation snapshot to `ai/reports/project-analysis.md`.
- `ai-platform roadmap-status`: writes a deterministic read-only roadmap status report to `ai/reports/roadmap-status.md`.
- `ai-platform run`: starts `scripts/codex-runner.ps1`.
- `ai-platform plan`: prints a planning guidance message only; it does not generate tasks.
- `ai-platform doctor`: runs basic local readiness checks.

It does not implement `refresh`, `status`, `reconcile`, `implement`, or `review`.

## Existing configuration

`ai-platform.json` currently defines:

- `platformVersion`
- `requiredTemplatePaths`
- task lifecycle paths for pending, in-progress, and done tasks
- worker lock file path
- worker polling interval

The CLI uses it for compatibility checks and doctor output. The worker uses its pending task path, lock file path, and polling interval with fallbacks.

## Current worker

`scripts/codex-runner.ps1`:

- runs as a PowerShell polling loop
- uses a lock file to avoid overlapping local workers
- detects Markdown tasks in the pending task directory
- runs `git pull`
- invokes Codex with the AGENTS workflow instruction
- appends metrics to `ai/system-metrics.md`
- runs `scripts/run-integration-tests.ps1` when present
- commits and pushes when changes are present

This is the current execution flow until a future `implement` command exists.

## Current refresh behavior

There is no implemented `refresh` command. Future refresh behavior should be conservative, explicit about managed artifacts, and dry-run by default.

## Current status and doctor behavior

`doctor` checks basic readiness: required directories, `AGENTS.md`, task paths, `.git`, and Codex availability.

There is no separate `status` command. Some status-like information appears inside `doctor`, but status is not dedicated yet.

## Existing documentation and workflow assets

The repository includes:

- `AGENTS.md` workflow rules
- `ai/task-template.md`
- `ai/architecture-index.md`
- `ai/repo-context.md`
- `ai/teams/` documentation for specialized team responsibilities
- `ai/commands/` specs for future roadmap-driven commands
- orchestrator guidance under `ai/orchestrator/`
- task lifecycle directories under `ai/tasks/`
- GitHub Actions wiring for automated Codex execution

## Current team model

`ai/teams/` defines a documented model of specialized teams for Product, Platform, Orchestration, Frontend, Backend, Database, QA, DevOps, Security, and Docs.

This model is for planning, ownership, and review guidance. There is no automatic team routing, autonomous multi-team execution, or enforced `team` metadata in tasks yet.

## Current command specs

`ai/commands/` defines documentation contracts for future roadmap-driven commands: `analyze`, `roadmap-status`, roadmap-driven `plan`, `reconcile`, `implement`, and `review`.

These specs do not imply CLI implementation by themselves. `analyze` and `roadmap-status` now have first read-only CLI implementations; the other roadmap-driven command specs remain contracts for future tasks.

## Current analyze behavior

`ai-platform analyze` reads platform configuration, core docs, task directories, roadmap markers, team docs, command specs, known gaps, and risks. It creates or updates `ai/reports/project-analysis.md` and prints a short console summary.

It does not move tasks, modify roadmap/current-state/team/command docs, execute Codex, call `run` or `refresh`, commit, push, download templates, or perform network operations.

## Current roadmap-status behavior

`ai-platform roadmap-status` reads `ai/roadmap.md`, detects roadmap item IDs, titles, and statuses, counts items by status, and creates or updates `ai/reports/roadmap-status.md`.

It does not modify roadmap/current-state/task files, execute Codex, call `run` or `refresh`, commit, push, download templates, perform network operations, or validate whether code implements each roadmap item.

## Known limitations

- The worker is PowerShell-first.
- The CLI is .NET-based and small.
- `plan` is only a message, not a real roadmap-aware planner.
- Command specs exist, but most roadmap-driven CLI behavior is not implemented yet.
- Local worker behavior and GitHub workflow behavior are not identical.
- Integration tests are only a placeholder until adapted by a consumer repository.
- Git automation assumes pull, commit, and push are acceptable for the target repository.
- There is no formal roadmap-to-task reconciliation yet.
- The team model is documentation only; routing and execution are not automated.

## Not yet implemented

- deeper roadmap-driven analysis beyond the first read-only reports
- CLI support for task planning from roadmap items
- CLI support for reconciliation between roadmap, code, docs, and tasks
- a dedicated `implement` command
- a formal review command or task review state
- multi-agent orchestration
- template versioning or remote upgrade management
