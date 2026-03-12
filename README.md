# AI Development Platform Template

A reusable repository template for **structured AI-assisted development** using Codex, task files, orchestration guides, a worker loop, and optional automation.

---

## 1. What this repository is

This repository is a **reusable AI development platform template**. It is designed to be copied or installed into other repositories so teams can run a consistent workflow:

- Feature request intake
- Planning and task decomposition
- Task execution by an AI worker
- Validation and commit/push automation
- Continuous review loop

Today, this template provides:

- A task system (`ai/tasks/pending`, `in-progress`, `done`)
- Orchestrator guidance documents (`ai/orchestrator/*`)
- Repository context and architecture prompts (`ai/*.md`)
- A worker script (`scripts/codex-runner.ps1`)
- A .NET CLI entrypoint (`ai-platform-cli/Program.cs`)
- GitHub workflow wiring (`.github/workflows/codex-worker.yml`)
- An installer scaffold script (`install-ai-platform.ps1`)

It can also improve itself by using the same task workflow in this repository.

---

## 2. Why it exists

This template exists to solve common problems in AI-assisted development:

| Problem | How this platform addresses it |
|---|---|
| Unstructured AI output | Uses task files with explicit scope, constraints, and validation |
| Inconsistent implementation flow | Standardizes pending → in-progress → done lifecycle |
| Weak planner/worker coordination | Adds dedicated orchestrator docs and prompts |
| Hard to reuse across projects | Provides installable folder structure + CLI + scripts |
| Review quality drift over time | Includes repository review loop guidance |

---

## 3. Core concepts

- **Orchestrator**: Planning guidance documents under `ai/orchestrator/` that define how to analyze scope and create executable tasks.
- **Planner**: The planning stage (typically via prompt + orchestrator docs) that turns feature requests into concrete `TASK-XXX` markdown files.
- **Worker**: The execution loop (`scripts/codex-runner.ps1`) that detects pending tasks and invokes Codex.
- **Tasks**: Markdown units of work following `ai/task-template.md`.
- **Specialized agents**: Repository-provided role docs in `ai/orchestrator/` (feature planner, repo reviewer, PR generator, etc.) plus global AGENTS workflow rules.
- **CLI**: `ai-platform-cli` command surface (`init`, `run`, `plan`, `doctor`) implemented in `ai-platform-cli/Program.cs`.
- **Template repository**: This repository itself (`ai-dev-platform-template`).
- **Consumer repository**: Any project into which you install/copy this platform.
- **Review loop**: When no pending tasks exist, inspect repo health and generate improvement tasks.
- **Task lifecycle**:
  - `ai/tasks/pending` — waiting to start
  - `ai/tasks/in-progress` — currently active
  - `ai/tasks/done` — completed tasks

---

## 4. Repository structure

### High-level tree (current)

```text
.
├── AGENTS.md
├── README.md
├── install-ai-platform.ps1
├── .github/
│   └── workflows/
│       └── codex-worker.yml
├── ai/
│   ├── architecture-index.md
│   ├── repo-context.md
│   ├── system-metrics.md
│   ├── task-template.md
│   ├── prompts/
│   │   └── plan-feature.md
│   ├── orchestrator/
│   │   ├── component-discovery.md
│   │   ├── di-analysis.md
│   │   ├── feature-planner.md
│   │   ├── planning-memory.md
│   │   ├── pr-generator.md
│   │   └── repo-reviewer.md
│   └── tasks/
│       ├── pending/
│       ├── in-progress/
│       └── done/
├── scripts/
│   ├── codex-runner.ps1
│   └── run-integration-tests.ps1
└── ai-platform-cli/
    ├── Program.cs
    └── ai-platform-cli.csproj
```

### Important paths explained

| Path | Purpose |
|---|---|
| `AGENTS.md` | Core behavioral rules for AI task processing and workflow constraints. |
| `ai/` | Core platform knowledge base: architecture docs, templates, prompts, orchestration. |
| `ai/orchestrator/` | Specialized planning/review/PR guidance documents. |
| `ai/tasks/` | Task lifecycle folders (`pending`, `in-progress`, `done`). |
| `scripts/` | Worker automation and optional integration test script. |
| `ai-platform-cli/` | .NET command-line entrypoint for install/run/doctor/plan operations. |
| `.github/workflows/` | GitHub Actions automation for task-triggered Codex execution. |
| `install-ai-platform.ps1` | Template bootstrap installer script for populating platform files. |

> Note: There is currently **no `agents/` directory** and **no `installer/` directory** in this repository.

---

## 5. How the platform works

End-to-end operational flow:

1. **Feature request arrives**
2. **Planning** uses `ai/orchestrator/feature-planner.md` + context files
3. **Task files are generated** using `ai/task-template.md`
4. Tasks are placed in **`ai/tasks/pending`**
5. **Worker loop** (`scripts/codex-runner.ps1`) checks pending tasks
6. Worker invokes Codex with AGENTS workflow instruction
7. Task is moved to **`in-progress`**, implemented, validated
8. Task is moved to **`done`**
9. Worker may run integration tests script if present
10. Worker auto-commits and pushes if changes exist
11. If no pending tasks, review loop can generate improvements

Simplified pipeline:

```text
Feature Request
  -> Planner (orchestrator docs)
  -> TASK files (ai/tasks/pending)
  -> Worker executes task
  -> Validation
  -> Commit/Push
  -> Review loop
```

---

## 6. Role separation

| Role | Responsibility |
|---|---|
| Human user | Defines goals, reviews output, approves merges, sets priorities. |
| Orchestrator | Produces clear plan + task decomposition from request/context. |
| Codex | Executes implementation steps according to task + AGENTS constraints. |
| Worker script | Detects tasks, handles lock, invokes Codex, runs optional validation/push flow. |
| Template repository | Provides reusable AI workflow framework. |
| Consumer repository | Hosts actual application/business code that uses this framework. |

---

## 7. Specialized agents

This repository currently models specialized agent behavior through `ai/orchestrator/*.md` documents.

| Agent spec file | Purpose | Used when | Task/planning influence |
|---|---|---|---|
| `feature-planner.md` | Convert feature request into actionable plan/tasks | New feature planning | Scope, task ordering, test strategy |
| `component-discovery.md` | Identify relevant components/files | Before implementation/planning | Better `Files to Read First` quality |
| `di-analysis.md` | Map dependency registrations/relations | Service/controller changes | Avoid duplicate services, preserve DI patterns |
| `planning-memory.md` | Capture lessons learned | During planning | Improves future task sizing/safety |
| `repo-reviewer.md` | Detect quality gaps and generate tasks | No pending tasks / review mode | Backlog maintenance and technical debt tasks |
| `pr-generator.md` | Standardize PR title/body content | After implementation | Consistent PR summaries and validation notes |

Additionally, `AGENTS.md` acts as the central policy layer for workflow execution.

---

## 8. Task system

### Lifecycle folders

- `ai/tasks/pending`: queued tasks
- `ai/tasks/in-progress`: active tasks
- `ai/tasks/done`: completed tasks

### Task template structure

Tasks follow `ai/task-template.md` and include:

- Goal
- Context
- Steps
- Files to Read First
- Expected Files to Modify
- Constraints
- Validation
- Change Budget

### Key governance fields

- **Files to Read First**: mandatory pre-read context before coding
- **Expected Files to Modify**: scope guardrail against unrelated edits
- **Validation**: required checks before completion
- **Change Budget**: encourages small, safe deltas

### Example task

```markdown
# TASK-999

## Goal
Add README section clarifying worker lock handling.

## Context
The worker behavior is documented but lock recovery behavior is unclear.

## Steps
1. Inspect scripts/codex-runner.ps1 lock logic.
2. Update README worker section.
3. Validate references to lock file path and behavior.

## Files to Read First
- scripts/codex-runner.ps1
- AGENTS.md
- README.md

## Expected Files to Modify
- README.md

## Constraints
- Do not modify unrelated files
- Keep change minimal

## Validation
- Verify lock file path matches script (`ai/worker.lock`)
- Verify no non-existent command is documented

## Change Budget
- Prefer fewer than 5 files
- Prefer under 200 lines
```

---

## 9. Planner and orchestration

Primary planner spec: `ai/orchestrator/feature-planner.md`.

Current planner behavior expects:

1. Read architecture and repository context
2. Define scope + non-goals
3. Break into small ordered tasks
4. Include validation scenarios
5. Generate task files in `ai/tasks/pending`
6. Consult planning memory to avoid repeated mistakes

Task priorities are determined by planner decomposition and task ordering in `pending` (the first pending task is processed first under AGENTS rules).

Planning context sources currently include:

- `ai/architecture-index.md`
- `ai/repo-context.md`
- `ai/orchestrator/planning-memory.md`
- `ai/orchestrator/component-discovery.md`
- `ai/orchestrator/di-analysis.md`

---

## 10. Worker behavior

Implemented in `scripts/codex-runner.ps1`.

Behavior summary:

1. Infinite polling loop (30s sleep)
2. Uses lock file `ai/worker.lock`
3. Detects stale lock by checking PID/process presence
4. Runs `git pull`
5. Detects pending markdown tasks
6. Invokes `codex "Follow the workflow defined in AGENTS.md and process the pending tasks."`
7. Logs task run metrics into `ai/system-metrics.md`
8. If integration script exists, executes `scripts/run-integration-tests.ps1`
9. If git changes exist, auto-commits and pushes
10. Always removes lock in `finally`

Integration tests are optional by existence of script and may require project-specific assets (`docker-compose.test.yml`, solution/projects).

---

## 11. CLI commands

The current CLI commands implemented in `ai-platform-cli/Program.cs` are:

| Command | What it does | When to use | Example | Expected output | Common failure modes |
|---|---|---|---|---|---|
| `ai-platform init` | Downloads template ZIP and copies missing platform files (`ai`, `scripts`, `.github`, `AGENTS.md`) | Bootstrap platform in a repo | `ai-platform init` | `Downloading platform...` then `AI platform installed.` | Network failure, file permissions, missing unzip/write rights |
| `ai-platform run` | Executes `scripts/codex-runner.ps1` via PowerShell | Start local worker loop | `ai-platform run` | `Codex worker started...` and polling logs | `powershell` missing, script path missing, `codex` missing |
| `ai-platform plan` | Prints planning guidance message | Reminder/entrypoint to planning flow | `ai-platform plan` | `Use the orchestrator prompts to generate tasks.` | None significant (informational command) |
| `ai-platform doctor` | Validates required platform readiness checks | Before first worker run or troubleshooting | `ai-platform doctor` | Check list + `Platform ready.` or missing item guidance | Missing `.git`, missing `ai/`, missing `scripts/`, missing `codex` |

### Commands requested but not currently implemented

- `ai-platform version` — **not implemented today**.

### Help output

Running without a known subcommand prints help with available commands.

---

## 12. Installation into another repository

### Prerequisites

- Git repository initialized (`.git` exists)
- Codex CLI available in PATH (`codex`)
- PowerShell available for worker scripts
- Optional: .NET SDK if building CLI locally

### Option A: CLI-based initialization

```bash
# If ai-platform is already installed globally/in PATH
ai-platform init
ai-platform doctor
```

### Option B: Script-based template bootstrap

```powershell
# From target repository root
powershell -ExecutionPolicy Bypass -File ./install-ai-platform.ps1
```

### Verify readiness

```bash
ai-platform doctor
```

Readiness checks include:

- `ai` directory
- `scripts` directory
- `AGENTS.md`
- `.git` directory
- `codex` command availability

### Start task workflow

1. Create task files in `ai/tasks/pending` using `ai/task-template.md`
2. Run worker (`ai-platform run`) or use CI workflow
3. Review produced commits and PRs

---

## 13. Example end-to-end workflow

### Feature request

> “Add a readiness troubleshooting section to docs and ensure lock handling is documented.”

### Planning

- Read `ai/architecture-index.md`, `ai/orchestrator/feature-planner.md`, `scripts/codex-runner.ps1`
- Create `TASK-020-update-readme-troubleshooting.md`

### Task file created in pending

```text
ai/tasks/pending/TASK-020-update-readme-troubleshooting.md
```

### Worker execution

- Worker detects pending task
- Creates lock (`ai/worker.lock`)
- Invokes Codex to process task by AGENTS workflow
- Moves task pending → in-progress → done

### Validation and commit

- Task-defined validation commands run
- Worker auto-commit if changes exist
- Worker pushes branch if commit succeeded

---

## 14. How this platform was validated

The platform has been validated through real task-cycle usage inside this repository:

- Multiple historical tasks are present in `ai/tasks/done`
- Worker lock handling and Codex CLI detection were iterated via dedicated tasks
- CLI doctor command and integration-test optional flow were added through task-driven changes

Validation style is pragmatic:

- task-by-task implementation
- explicit constraints/change budget
- commit-based checkpoints

---

## 15. Troubleshooting

| Issue | Symptom | Resolution |
|---|---|---|
| `codex` not found in PATH | Doctor fails on codex check | Install Codex CLI and ensure executable is in PATH; rerun `ai-platform doctor` |
| No git repository | Doctor marks `.git` missing | Run `git init` or work in a cloned repository |
| Worker lock file present | Worker repeatedly says already running | If stale, script auto-removes stale lock; otherwise stop duplicate worker process |
| No pending tasks found | Worker loops with no execution | Add tasks to `ai/tasks/pending` |
| Platform files missing | Doctor reports missing `ai`/`scripts`/`AGENTS.md` | Run `ai-platform init` or installer script |
| Tasks generated but not pushed | Local commits exist only locally | Ensure remote and upstream are configured; run `git push` |
| Branch/upstream issues | `git pull` fails due to no tracking | Set upstream: `git branch --set-upstream-to <remote>/<branch> <local-branch>` |
| GitHub push or transient 502 issues | Push fails intermittently | Retry push; verify remote auth/network and branch protections |
| Stale local platform install after template updates | Older docs/scripts remain | Re-run installer/`init`; for overwrite behavior use script `-Force` carefully |

---

## 16. Current limitations

- Some context docs (`ai/repo-context.md`, `ai/architecture-index.md`) contain example application details not aligned with this template-only repository; they are intended as customizable seed content.
- CLI currently does not provide a `version` command.
- `ai-platform run` assumes PowerShell and script compatibility in host environment.
- Integration test script references project assets (`docker-compose.test.yml`, `FormularioBoda.*`) that may not exist in every consumer repository without adaptation.
- Worker uses a generic commit message (`chore: process pending tasks`) unless the task execution flow customizes it.

---

## 17. Best practices

- Keep tasks narrow, explicit, and validation-driven.
- Always fill `Files to Read First` with the minimum critical context.
- Keep `Expected Files to Modify` strict to reduce accidental edits.
- Prefer one logical change per task.
- Use orchestrator docs (`component-discovery`, `di-analysis`) before changing services/controllers in consumer projects.
- Use dedicated branches for large initiatives.
- Merge stable increments often instead of waiting for large batches.

---

## 18. Suggested workflow for large features

1. Create a dedicated feature branch (`feature/<short-name>`)
2. Use orchestrator flow to generate a task set in `ai/tasks/pending`
3. Review and refine tasks before execution
4. Run worker loop for incremental implementation
5. Validate each completed task (build/tests/lint as applicable)
6. Merge stable increments to main through PRs

---

## 19. Future improvements

The following are **future work ideas** (not all implemented today):

- Add `ai-platform version` command.
- Add cross-platform worker runner (non-PowerShell entrypoint).
- Improve structured metrics output (JSON/CSV) in addition to markdown log lines.
- Add stricter task schema validation tooling.
- Add explicit branch/upstream diagnostics in `doctor`.
- Add first-class support for multiple orchestrator profiles per tech stack.

---

## Quick command reference

```bash
# Initialize platform files in current repository
ai-platform init

# Check readiness before execution
ai-platform doctor

# Start worker loop
ai-platform run

# Show planning guidance message
ai-platform plan
```

If `ai-platform` is not installed globally, build/run the CLI project directly with .NET tooling in environments where .NET SDK is available.
