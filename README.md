# AI Development Platform Template

Reusable repository template for structured AI-assisted development with Codex, task files, orchestration guides, a worker loop, and optional automation.

---

## 1. What this repository is

This repository is a template for teams that want a repeatable AI-driven development workflow inside a Git repository.

It currently provides:

- a task system (`ai/tasks/pending`, `in-progress`, `review`, `done`, `blocked`, `obsolete`)
- repository context and architecture prompts (`ai/*.md`)
- orchestrator guidance documents (`ai/orchestrator/*`)
- a local worker script (`scripts/codex-runner.ps1`)
- a small CLI entrypoint (`ai-platform-cli/Program.cs`)
- a bootstrap installer script (`install-ai-platform.ps1`)
- GitHub Actions wiring for automated worker execution

This repository is also used to improve the template itself through the same task workflow.

---

## 2. What it is not

This repository is not a complete multi-stack platform.

Important limitations:

- the worker automation is currently PowerShell-based
- the bundled CLI is currently implemented in .NET
- some automation assumes a Git-based workflow with commit and push steps
- the integration test hook is only a placeholder until a consumer repository adapts it

The template is reusable, but not fully stack-agnostic in implementation details yet. Where behavior is still opinionated, this README calls that out explicitly.

---

## 3. Core concepts

- **Tasks**: Markdown units of work following `ai/task-template.md`
- **Orchestrator docs**: guidance under `ai/orchestrator/` used for planning, review, and dependency discovery
- **Worker**: the loop in `scripts/codex-runner.ps1` that checks pending tasks and invokes Codex
- **CLI**: the `ai-platform-cli` command surface (`init`, `status`, `refresh`, `git-ignore`, `analyze`, `roadmap-status`, `reconcile`, `review`, `implement`, `task move`, `run`, `plan`, `doctor`)
- **Template repository**: this repository
- **Consumer repository**: a repository that installs or copies this platform
- **Review loop**: when no pending tasks exist, the repository can generate improvement tasks for itself

Task lifecycle:

- `ai/tasks/pending`: waiting for execution
- `ai/tasks/in-progress`: currently being executed
- `ai/tasks/review`: implemented or candidate work waiting for review
- `ai/tasks/done`: completed and validated
- `ai/tasks/blocked`: cannot move forward because of an external dependency or decision
- `ai/tasks/obsolete`: should not be executed because it is no longer valid

`ai-platform reconcile` may propose candidates for review, blocked, or obsolete states, but it does not move tasks automatically. Tasks should not be moved to `done` without validation.

---

## 4. Repository structure

```text
.
|-- ai-platform.json
|-- AGENTS.md
|-- README.md
|-- install-ai-platform.ps1
|-- .github/
|   `-- workflows/
|       `-- codex-worker.yml
|-- ai/
|   |-- architecture-index.md
|   |-- repo-context.md
|   |-- system-metrics.md
|   |-- task-template.md
|   |-- prompts/
|   |   `-- plan-feature.md
|   |-- orchestrator/
|   |   |-- component-discovery.md
|   |   |-- di-analysis.md
|   |   |-- feature-planner.md
|   |   |-- planning-memory.md
|   |   |-- pr-generator.md
|   |   `-- repo-reviewer.md
|   `-- tasks/
|       |-- pending/
|       |-- in-progress/
|       |-- review/
|       |-- blocked/
|       |-- obsolete/
|       `-- done/
|-- scripts/
|   |-- codex-runner.ps1
|   `-- run-integration-tests.ps1
`-- ai-platform-cli/
    |-- Program.cs
    `-- ai-platform-cli.csproj
```

---

## 5. What is generic today

The following parts are intentionally generic and designed to be reused with minimal editing:

- task folder lifecycle under `ai/tasks/`
- the minimal platform config in `ai-platform.json`
- the task template in `ai/task-template.md`
- repository context and architecture seed docs
- orchestrator guidance for planning, review, and component discovery
- the installer script that seeds platform files

These pieces are meant to be adapted to the target repository, but they no longer assume one specific business domain.

---

## 6. What is opinionated today

The following parts are reusable but still opinionated:

- `scripts/codex-runner.ps1` uses PowerShell and an infinite polling loop
- the worker performs `git pull` and may commit/push changes automatically
- `.github/workflows/codex-worker.yml` uses a direct Codex CI flow instead of the local PowerShell worker
- `ai-platform-cli/Program.cs` uses a .NET CLI and defaults to downloading this template from a GitHub ZIP source

These are current conventions of the template, not universal requirements for all repositories or stacks.

---

## 7. How the platform works

High-level flow:

1. A request or improvement idea is identified
2. Planning guidance is used to create task files
3. Tasks are created in `ai/tasks/pending`
4. The worker or a human processes the first pending task
5. Validation is run using commands that make sense for the repository
6. The task moves through `in-progress`, then `review` when appropriate, and reaches `done` only after validation supports completion
7. When no pending tasks remain, a review loop can generate follow-up tasks

Simplified pipeline:

```text
Request
  -> Planning
  -> Task files
  -> Worker execution
  -> Validation
  -> Review
  -> Commit/Push
  -> Review loop
```

---

## 8. Role separation

| Role | Responsibility |
|---|---|
| Human user | Defines goals, reviews output, sets priorities, approves merges |
| Orchestrator | Produces implementation-ready plans and tasks |
| Codex | Executes tasks according to repository rules |
| Worker script | Detects tasks, invokes Codex, runs optional validation, and handles automation |
| Template repository | Supplies reusable workflow scaffolding |
| Consumer repository | Supplies the actual application code and stack-specific behavior |

---

## 9. Normative files

The following files are part of the operating contract of the template:

- `ai-platform.json`
- `AGENTS.md`
- `ai/repo-context.md`
- `ai/architecture-index.md`
- `ai/task-template.md`
- `ai/orchestrator/*.md`

These files should describe the current repository honestly. They should not pretend the repository is a different application or stack.

---

## 10. Worker behavior

Implemented in `scripts/codex-runner.ps1`.

Current behavior:

1. Runs in a polling loop
2. Uses the lock file path from `ai-platform.json` (`worker.lockFile`) with fallback to `ai/worker.lock`
3. Reads the pending task directory from `ai-platform.json` (`taskPaths.pending`) with fallback to `ai/tasks/pending`
4. Reads the polling interval from `ai-platform.json` (`worker.pollIntervalSeconds`) with fallback to `30`
5. Removes stale locks when the owning process is no longer active
6. Runs `git pull`
7. Detects pending Markdown tasks
8. Invokes Codex with the AGENTS workflow instruction
9. Appends execution metrics to `ai/system-metrics.md`
10. Calls `scripts/run-integration-tests.ps1` if it exists
11. Commits and pushes when changes are present

Notes:

- this behavior is current template behavior, not a universal best practice
- if your repository should not auto-push, adapt the script or workflow
- the integration test hook must be configured by each consumer repository if real integration tests are needed

The local worker and the GitHub workflow are intentionally not identical:

- `scripts/codex-runner.ps1` is the local PowerShell worker
- `.github/workflows/codex-worker.yml` currently runs Codex directly in CI as a lightweight automation path

---

## 11. CLI commands

The current CLI commands implemented in `ai-platform-cli/Program.cs` are:

| Command | What it does | Status |
|---|---|---|
| `ai-platform init` | Downloads a template ZIP, then copies missing platform files into the current repository | Implemented |
| `ai-platform status` | Shows a quick operational platform summary from config and local essentials | Implemented |
| `ai-platform refresh` | Refreshes managed platform artifacts from a compatible template ZIP; dry-run by default | v1 implemented |
| `ai-platform git-ignore` | Adds or updates the managed consumer-local `.gitignore` block | Implemented |
| `ai-platform analyze` | Generates a read-only operational/documentation report at `ai/reports/project-analysis.md` | Implemented |
| `ai-platform roadmap-status` | Generates a deterministic read-only roadmap status report at `ai/reports/roadmap-status.md` | Implemented |
| `ai-platform reconcile` | Generates a read-only task/roadmap consistency report at `ai/reports/task-reconciliation.md` | Implemented |
| `ai-platform review` | Reviews one task and writes a read-only report at `ai/reports/task-review.md` | Implemented |
| `ai-platform implement` | Prepares one pending task for implementation and writes `ai/reports/implementation-prompt.md` | v1 implemented |
| `ai-platform task move` | Moves one task between lifecycle states with explicit safety rules | v1 implemented |
| `ai-platform run` | Executes `scripts/codex-runner.ps1` via PowerShell | Implemented |
| `ai-platform plan` | Creates one roadmap-driven task file in `ai/tasks/pending` | Implemented |
| `ai-platform doctor` | Validates basic platform readiness checks | Implemented |
| `ai-platform version` | Not currently available | Not implemented |

Important note:

`ai-platform analyze` is read-only except for creating or updating `ai/reports/project-analysis.md`. It gives a simple operational snapshot of platform docs, task counts, roadmap markers, team docs, and command specs. It does not replace `doctor` and does not perform deep semantic analysis yet.

`ai-platform roadmap-status` is read-only except for creating or updating `ai/reports/roadmap-status.md`. It parses `ai/roadmap.md` deterministically and reports roadmap item status counts. It does not validate whether code implements each item and does not replace future `reconcile` behavior.

`ai-platform plan` creates one Markdown task in `ai/tasks/pending`. It requires `--title`, can associate the task with `--roadmap`, accepts optional `--team`, `--priority`, and `--type`, and supports `--dry-run`. It creates the task only; it does not implement it, move tasks, run Codex, commit, or push.

`ai-platform refresh` v1 refreshes only the managed artifacts declared in `ai-platform.json`. It is a dry-run by default, requires `--apply` to write changes, supports `--source` to override the ZIP source, and resolves the source in this order: `--source`, `AI_PLATFORM_TEMPLATE_ZIP`, `templateSourceZip` from `ai-platform.json`, then the built-in default. It never deletes files, never touches tasks, and never creates commits or pushes.

The default `managedArtifacts` list is intentionally fine-grained. By default it refreshes a small set of platform-owned files such as `AGENTS.md`, `ai-platform.json`, selected worker scripts, `.github/workflows/codex-worker.yml`, and `ai/task-template.md`. It does not refresh `ai/roadmap.md`, `ai/current-state.md`, `ai/project-memory/*`, `ai/tasks/*`, or generated files under `ai/reports/`.

Consumer repositories can adapt `managedArtifacts` to match their own policy. `refresh` v1 still does not provide merge intelligence, backups, or rollback, so the default scope stays narrow on purpose.

`ai-platform git-ignore` is an explicit helper for consumer repositories that want the AI platform tooling to remain local instead of becoming part of the project history. It adds or updates a managed `.gitignore` block, supports `--dry-run`, does not delete files, and does not commit or push.

`ai-platform reconcile` is read-only except for creating or updating `ai/reports/task-reconciliation.md`. It detects task/roadmap reference issues and stale or weak pending task candidates. It does not move tasks, mark anything done, or replace future review behavior.

`ai-platform review` accepts `--task` or `--file`, validates one task mechanically, and writes `ai/reports/task-review.md`. It recommends an outcome but does not move tasks, mark anything done, or execute follow-up actions.

`ai-platform implement` v1 selects a pending task, validates basic metadata, can move it to `ai/tasks/in-progress`, and writes `ai/reports/implementation-prompt.md` for Codex. It does not execute Codex, implement code automatically, move tasks to `done`, commit, or push. The Codex execution step still must implement the task, validate it, commit, and push.

`ai-platform task move` provides the explicit state transition step beyond `implement` v1. It accepts `--task`, `--to`, optional `--dry-run`, and optional `--force`. It moves only the selected task file inside `ai/tasks/*`, updates a recognized internal `status` line when possible, does not run review or implementation for you, and does not create commits or pushes.

Safe moves without `--force` are intentionally narrow in v1: `pending -> in-progress`, `in-progress -> review`, `review -> done`, `pending -> blocked`, `in-progress -> blocked`, `review -> blocked`, `pending -> obsolete`, `blocked -> pending`, and `review -> in-progress`. Higher-risk transitions such as direct jumps to `done` outside `review`, or reopening `done`/`obsolete`, require `--force`.

`ai-platform status` is a quick operational view. It reports config loading, refresh source, managed artifacts, task paths, and a few local essentials. It is intentionally lighter than `doctor`, which remains the fuller readiness check.

Generated reports under `ai/reports/` are local outputs. Keep `ai/reports/.gitkeep`, but do not commit generated files such as `project-analysis.md`, `roadmap-status.md`, `task-reconciliation.md`, `task-review.md`, or `implementation-prompt.md`.

Examples:

```bash
ai-platform plan --roadmap R-005 --title "Implement roadmap-driven plan command"
ai-platform plan --title "Add team routing metadata to tasks" --dry-run
ai-platform refresh
ai-platform refresh --apply
ai-platform refresh --source https://example.com/template.zip
ai-platform git-ignore --dry-run
ai-platform git-ignore
ai-platform implement
ai-platform implement --task TASK-0001
ai-platform implement --dry-run
ai-platform implement --task TASK-0001 --no-move
ai-platform task move --task TASK-0001 --to review
ai-platform task move --task TASK-0001 --to done
ai-platform task move --task TASK-0001 --to done --force
ai-platform task move --task TASK-0001 --to blocked --dry-run
```

By default, `ai-platform init` downloads this template from the repository's current GitHub ZIP URL. You can also pass a ZIP URL explicitly or set `AI_PLATFORM_TEMPLATE_ZIP` to point at another compatible source.

This improves portability, but `init` is still not a full multi-source package manager. It expects a compatible template archive layout.

Before copying files, `init` now validates that the downloaded template contains the minimum expected structure:

- `ai/`
- `scripts/`
- `AGENTS.md`
- `ai-platform.json`

If the ZIP does not contain that minimum structure, installation stops with a clear compatibility error instead of reporting a successful install.

At the end of a successful run, `init` prints a compact install summary showing:

- the source used
- whether the source came from the command argument, `AI_PLATFORM_TEMPLATE_ZIP`, or the built-in default
- which top-level platform items were copied
- which items were skipped because they already existed
- the minimal validation that was applied
- the recommended next step

---

## 12. Installation into another repository

### Prerequisites

- a Git repository (`.git` exists)
- Codex CLI available in PATH (`codex`)
- PowerShell available for worker scripts
- optional: .NET SDK if building or packaging the CLI locally

### Option A: CLI-based initialization

```bash
ai-platform init
ai-platform doctor
```

You can also override the source:

```bash
ai-platform init https://example.com/my-template.zip
```

or:

```bash
AI_PLATFORM_TEMPLATE_ZIP=https://example.com/my-template.zip ai-platform init
```

### Option B: Script-based bootstrap

```powershell
powershell -ExecutionPolicy Bypass -File ./install-ai-platform.ps1
```

The PowerShell installer is a bootstrap script that seeds a bundled snapshot of the platform files. It is useful when you want a local, file-based installation path without relying on the CLI download flow.

When `install-ai-platform.ps1` runs from a valid checkout of the template source repository, it now prefers copying a few sensitive artifacts from the real files next to the script:

- `ai-platform.json`
- `AGENTS.md`
- `scripts/codex-runner.ps1`
- `.github/workflows/codex-worker.yml`

If those real source files are not available, the installer falls back to its embedded snapshot content and says so in its output.

### After installation

1. Review and adapt `AGENTS.md`
2. Review `ai-platform.json` and keep its paths aligned with your repository conventions
3. Review and adapt `ai/repo-context.md`
4. Review and adapt `ai/architecture-index.md`
5. Adjust validation commands for the target repository
6. Replace the integration test placeholder if integration tests are needed

The template is meant to give you a starting point, not to eliminate repository-specific setup.

### Template source vs consumer-local installs

`ai-platform.json` now declares an `installMode`:

- `template-source`: use this in the template repository itself, where AI platform files are intentionally versioned.
- `consumer-local`: use this in consumer repositories when the AI platform should behave like local tooling that stays out of the application repo.

This repository remains `template-source`. In a consumer repository, set `installMode` to `consumer-local`, then preview the ignore block:

```bash
ai-platform git-ignore --dry-run
```

Apply it only when the local-only policy is desired:

```bash
ai-platform git-ignore
```

The command manages this ignore scope:

- `AGENTS.md`
- `ai-platform.json`
- `ai/`
- `scripts/codex-runner.ps1`
- `scripts/run-integration-tests.ps1`
- `.github/workflows/codex-worker.yml`
- `ai-platform-cli/`

`.gitignore` only affects untracked files. If those platform files were already committed earlier, stop tracking them without deleting local copies by reviewing and running:

```bash
git rm -r --cached AGENTS.md ai-platform.json ai scripts/codex-runner.ps1 scripts/run-integration-tests.ps1 .github/workflows/codex-worker.yml ai-platform-cli
```

`ai-platform git-ignore` does not run that command automatically. In consumer-local mode, update the local platform tooling with `ai-platform refresh --apply` when appropriate; `refresh` still never creates commits or pushes.

### Compatibility checks

`ai-platform doctor` validates the local repository after installation.

`doctor` also reports whether `ai-platform.json` was:

- loaded successfully
- missing and replaced with built-in defaults
- invalid and replaced with built-in defaults
- loaded partially, with fallback defaults applied to specific keys

`ai-platform init` validates the downloaded source before copying files.

Together, these checks cover two different failure modes:

- invalid template source
- incomplete local installation

---

## 13. Validation model

Validation is repository-aware.

Typical checks may include:

- build commands for the current stack
- unit or integration tests for the current stack
- lint or formatting checks when relevant
- targeted script verification

This template no longer assumes that every repository will use ASP.NET Core MVC, Entity Framework, SQL Server, or any specific business domain.

---

## 14. Windows build troubleshooting

On Windows, this command can fail even when the CLI code itself is valid:

```powershell
dotnet build ai-platform-cli\ai-platform-cli.csproj
```

Typical symptom:

- the build reports that `ai-platform-cli.exe` cannot be copied or deleted because it is locked by an active process.

Likely causes:

- a previous CLI execution is still alive
- a terminal or tool still keeps the process open
- Windows is still holding the generated apphost executable

Recommended recovery steps:

1. Close terminals or tools that may still be running the CLI.
2. If needed, stop the process explicitly:

```powershell
taskkill /IM ai-platform-cli.exe /F
```

3. Run:

```powershell
dotnet clean ai-platform-cli\ai-platform-cli.csproj
```

4. Retry:

```powershell
dotnet build ai-platform-cli\ai-platform-cli.csproj
```

5. As a safe validation workaround, use:

```powershell
dotnet build ai-platform-cli\ai-platform-cli.csproj -p:UseAppHost=false
```

If `UseAppHost=false` compiles cleanly and the functional CLI commands still pass, the issue is usually an environment or executable lock problem, not necessarily a code defect.

---

## 15. Minimal platform config

The root file `ai-platform.json` is a small, explicit configuration file for stable platform conventions.

Today it covers only a few things that are already useful:

- `platformVersion`: lightweight template/config version marker
- `installMode`: whether the repository is the versioned template source or a consumer-local install
- `templateSourceZip`: explicit default template ZIP source for install or future refresh-like flows
- `managedArtifacts`: explicit list of platform-owned artifacts refreshed by `ai-platform refresh`
- `requiredTemplatePaths`: minimum paths a compatible template source must contain
- `taskPaths`: default lifecycle directories for pending, in-progress, and done tasks
- extended `taskPaths`: review, blocked, and obsolete directories for safer future workflows
- `worker.lockFile`: current lock file path convention used by the worker
- `worker.pollIntervalSeconds`: polling interval for the worker loop

What it does not cover yet:

- repository stack or framework type
- multiple profiles or template variants
- validation command definitions
- workflow policy beyond a few stable path conventions

The current CLI already reads this file for minimal compatibility checks, status, doctor output, consumer-local `.gitignore` guidance, and conservative refresh behavior, and the worker already uses it for `worker.lockFile`, `taskPaths.pending`, and `worker.pollIntervalSeconds` with safe fallback defaults. The platform is not yet fully driven by config.

---

## 16. Current limitations

- The worker and installer experience are PowerShell-first.
- The CLI is implemented in .NET and `init` still defaults to this repository's ZIP source unless overridden.
- `init` validates only a minimal compatible structure from `ai-platform.json`; it does not verify every optional file or workflow asset.
- The installer script still contains embedded fallback content, so some divergence risk remains even though it now prefers real repository files for the most sensitive artifacts when available.
- Git automation assumes a repository where automated `pull`, `commit`, and `push` behavior is acceptable.
- `scripts/run-integration-tests.ps1` is intentionally a placeholder until adapted by the target repository.
- GitHub automation is provided as a direct Codex CI flow, not as a full execution of the local PowerShell worker, and may require repository-specific permissions or policy changes.

---

## 17. Best practices

- Keep tasks narrow, explicit, and validation-driven.
- Use `Files to Read First` to constrain discovery work.
- Keep `Expected Files to Modify` strict.
- Prefer one logical change per task.
- Align context docs with the real repository state.
- Document limitations instead of overstating genericity.

---

## 18. Suggested workflow for large features

1. Create a dedicated branch using the repository's branch naming conventions
2. Use orchestrator guidance to generate a small task set in `ai/tasks/pending`
3. Review tasks before execution
4. Run the worker loop or process tasks manually
5. Validate each completed task with repository-relevant checks
6. Merge stable increments through the repository's normal review process

---

## 19. Future improvements

These are future improvements, not claims about current behavior:

- provide a safer update story for existing installs
- offer alternative worker entrypoints beyond PowerShell
- improve validation and doctor diagnostics for more repository types
- move more stable conventions from hardcoded defaults into `ai-platform.json`
- support richer profiles for different repository architectures

---

## 20. Roadmap and project memory

The platform now uses a roadmap as its direction source for future work.

- `ai/roadmap.md` is the initial roadmap for the next platform evolution.
- `ai/current-state.md` describes the real current state of the repository.
- `ai/project-memory/` records decisions, risks, and known gaps that should inform future tasks.

Roadmap-driven commands are being implemented incrementally. The roadmap and memory files remain the direction source; command availability is documented in the CLI command table.

---

## 21. Specialized team model

The specialized team model lives under `ai/teams/`.

It documents ownership boundaries for Product, Platform, Orchestration, Frontend, Backend, Database, QA, DevOps, Security, and Docs. Future tasks may use `team` for the primary owner and `supporting_teams` for secondary contributors or reviewers.

This is still documentation only. It does not add autonomous agents, automatic routing, or multi-team execution yet.

---

## 22. Command specs

Roadmap-driven command specs live under `ai/commands/`.

They define intended behavior for commands such as `analyze`, `roadmap-status`, roadmap-driven `plan`, `reconcile`, `implement`, and `review`. These specs are implementation contracts, not proof that each command is available in the CLI today.

Future command implementations should follow these specs and update them when behavior changes.

---

## Quick command reference

```bash
ai-platform init
ai-platform status
ai-platform refresh
ai-platform git-ignore --dry-run
ai-platform analyze
ai-platform roadmap-status
ai-platform reconcile
ai-platform review --task TASK-0001
ai-platform implement --task TASK-0001
ai-platform task move --task TASK-0001 --to review
ai-platform doctor
ai-platform run
ai-platform plan
```

If `ai-platform` is not installed globally, you can still use the repository files directly or build the CLI locally with the .NET SDK.
