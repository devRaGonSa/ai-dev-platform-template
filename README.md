# AI Development Platform Template

Reusable repository template for structured AI-assisted development with Codex, task files, orchestration guides, a worker loop, and optional automation.

---

## 1. What this repository is

This repository is a template for teams that want a repeatable AI-driven development workflow inside a Git repository.

It currently provides:

- a task system (`ai/tasks/pending`, `in-progress`, `done`)
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
- **CLI**: the `ai-platform-cli` command surface (`init`, `run`, `plan`, `doctor`)
- **Template repository**: this repository
- **Consumer repository**: a repository that installs or copies this platform
- **Review loop**: when no pending tasks exist, the repository can generate improvement tasks for itself

Task lifecycle:

- `ai/tasks/pending`: waiting to start
- `ai/tasks/in-progress`: currently active
- `ai/tasks/done`: completed

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
6. The task is moved to `ai/tasks/done`
7. When no pending tasks remain, a review loop can generate follow-up tasks

Simplified pipeline:

```text
Request
  -> Planning
  -> Task files
  -> Worker execution
  -> Validation
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
| `ai-platform run` | Executes `scripts/codex-runner.ps1` via PowerShell | Implemented |
| `ai-platform plan` | Prints a planning guidance message | Implemented |
| `ai-platform doctor` | Validates basic platform readiness checks | Implemented |
| `ai-platform version` | Not currently available | Not implemented |

Important note:

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

## 14. Minimal platform config

The root file `ai-platform.json` is a small, explicit configuration file for stable platform conventions.

Today it covers only a few things that are already useful:

- `platformVersion`: lightweight template/config version marker
- `requiredTemplatePaths`: minimum paths a compatible template source must contain
- `taskPaths`: default lifecycle directories for pending, in-progress, and done tasks
- `worker.lockFile`: current lock file path convention used by the worker
- `worker.pollIntervalSeconds`: polling interval for the worker loop

What it does not cover yet:

- repository stack or framework type
- multiple profiles or template variants
- validation command definitions
- workflow policy beyond a few stable path conventions

The current CLI already reads this file for minimal compatibility checks and doctor output, and the worker already uses it for `worker.lockFile`, `taskPaths.pending`, and `worker.pollIntervalSeconds` with safe fallback defaults. The platform is not yet fully driven by config.

---

## 15. Current limitations

- The worker and installer experience are PowerShell-first.
- The CLI is implemented in .NET and `init` still defaults to this repository's ZIP source unless overridden.
- `init` validates only a minimal compatible structure from `ai-platform.json`; it does not verify every optional file or workflow asset.
- The installer script still contains embedded fallback content, so some divergence risk remains even though it now prefers real repository files for the most sensitive artifacts when available.
- Git automation assumes a repository where automated `pull`, `commit`, and `push` behavior is acceptable.
- `scripts/run-integration-tests.ps1` is intentionally a placeholder until adapted by the target repository.
- GitHub automation is provided as a direct Codex CI flow, not as a full execution of the local PowerShell worker, and may require repository-specific permissions or policy changes.

---

## 16. Best practices

- Keep tasks narrow, explicit, and validation-driven.
- Use `Files to Read First` to constrain discovery work.
- Keep `Expected Files to Modify` strict.
- Prefer one logical change per task.
- Align context docs with the real repository state.
- Document limitations instead of overstating genericity.

---

## 17. Suggested workflow for large features

1. Create a dedicated branch using the repository's branch naming conventions
2. Use orchestrator guidance to generate a small task set in `ai/tasks/pending`
3. Review tasks before execution
4. Run the worker loop or process tasks manually
5. Validate each completed task with repository-relevant checks
6. Merge stable increments through the repository's normal review process

---

## 18. Future improvements

These are future improvements, not claims about current behavior:

- provide a safer update story for existing installs
- offer alternative worker entrypoints beyond PowerShell
- improve validation and doctor diagnostics for more repository types
- move more stable conventions from hardcoded defaults into `ai-platform.json`
- support richer profiles for different repository architectures

---

## 19. Roadmap and project memory

The platform now uses a roadmap as its direction source for future work.

- `ai/roadmap.md` is the initial roadmap for the next platform evolution.
- `ai/current-state.md` describes the real current state of the repository.
- `ai/project-memory/` records decisions, risks, and known gaps that should inform future tasks.

Roadmap-driven commands such as analysis, roadmap status, planning, reconciliation, implementation, and review will be implemented in later phases. These files are documentation foundations only; they do not add executable CLI behavior yet.

---

## 20. Specialized team model

The specialized team model lives under `ai/teams/`.

It documents ownership boundaries for Product, Platform, Orchestration, Frontend, Backend, Database, QA, DevOps, Security, and Docs. Future tasks may use `team` for the primary owner and `supporting_teams` for secondary contributors or reviewers.

This is still documentation only. It does not add autonomous agents, automatic routing, or multi-team execution yet.

---

## Quick command reference

```bash
ai-platform init
ai-platform doctor
ai-platform run
ai-platform plan
```

If `ai-platform` is not installed globally, you can still use the repository files directly or build the CLI locally with the .NET SDK.
