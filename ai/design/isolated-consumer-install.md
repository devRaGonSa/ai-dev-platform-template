# Isolated Consumer Install Design

This document studies a possible future evolution for installing the AI Platform inside consumer repositories under `.ai-platform/`.

It is a design note only. It does not change the current runtime, path conventions, configuration defaults, or installation behavior.

## Problem

The current consumer-local model keeps the platform usable without committing it, but it still places AI tooling in the visible project root:

- platform files are mixed with application files;
- `.gitignore` is required to avoid publishing local tooling;
- names such as `ai/`, `scripts/`, and `AGENTS.md` may collide with real project files;
- consumer-local works as a Git policy, but it does not isolate the tool physically;
- `refresh` and `managedArtifacts` still operate on root-visible paths.

## Current model

The current install/use model exposes:

- `AGENTS.md`
- `ai-platform.json`
- `ai/`
- `scripts/`
- `.github/workflows/codex-worker.yml`
- `ai-platform-cli/`

Advantages:

- simple;
- compatible with the template source repository;
- easy to read and debug;
- does not require complex path routing.

Disadvantages:

- visually noisy in consumer repositories;
- depends on `.gitignore`;
- can be tracked accidentally;
- may collide with real project folders or conventions;
- update and cleanup boundaries are less obvious.

## Proposed isolated model

A future isolated layout could group nearly all internal tooling under one root:

```text
.ai-platform/
  AGENTS.md
  ai-platform.json
  ai/
  scripts/
  ai-platform-cli/
  reports/
```

An optional extension could isolate workflow assets too:

```text
.ai-platform/workflows/
```

In the simplest consumer model, the project would need one ignore rule:

```text
.ai-platform/
```

## Source of truth options

### Option A - Everything under .ai-platform/

All AI platform files live inside `.ai-platform/`.

Pros:

- very clean consumer repositories;
- one ignore rule;
- much lower risk of polluting the project tree.

Cons:

- Codex or other tooling may expect `AGENTS.md` at the repository root;
- every command would need path-base resolution;
- migration cost is higher.

### Option B - Hybrid model

Keep one minimal file in the project root, for example:

```text
AGENTS.md
.ai-platform/
```

Pros:

- preserves compatibility with tools that read root-level `AGENTS.md`;
- isolates the bulk of platform internals.

Cons:

- isolation is not total;
- one AI-owned root artifact remains.

### Option C - Current model with stronger git-ignore

Keep the current root layout and improve `.gitignore`, `doctor`, and `refresh`.

Pros:

- lowest implementation cost;
- already works today.

Cons:

- no physical isolation;
- platform files remain mixed with application files.

## Impact by command

| Command | Paths used today | What changes with `.ai-platform/` | Risks | Needs configurable path base? |
|---|---|---|---|---|
| `status` | `ai-platform.json`, task paths, local essentials in root | Must report platform-root state and project-root state separately | Misreporting current install shape | Yes |
| `doctor` | root `ai/`, `scripts/`, `AGENTS.md`, task paths | Must validate either rooted or isolated installs | False negatives during dual-mode transition | Yes |
| `refresh` | root-relative `managedArtifacts` | Managed artifacts should become relative to `platformRoot` | Updating the wrong tree | Yes |
| `git-ignore` | root `.gitignore`, current AI root paths | Could prefer `.ai-platform/` only, or hybrid rules | Leaving stale ignore entries behind | Partially |
| `plan` | configured task paths, root docs | Should read/write configured metadata location | Generating tasks into wrong ownership boundary | Yes |
| `implement` | pending task path, report path | Prompt/report generation must honor isolated or project-owned metadata | Moving tasks in an unintended tree | Yes |
| `review` | task file and roadmap lookup | Should resolve task/report paths consistently | Reviewing the wrong task copy | Yes |
| `task move` | configured lifecycle paths | Same transition rules, but paths become base-aware | Cross-root moves or ambiguous duplicates | Yes |
| `analyze` | root docs, roadmap, teams, commands, reports | Needs explicit distinction between platform docs and project metadata | Blurring platform state with project state | Yes |
| `roadmap-status` | `ai/roadmap.md`, report path | Depends on where roadmap lives in the chosen ownership model | False roadmap absence | Yes |
| `reconcile` | roadmap/current-state/known-gaps/tasks | Must compare across whichever ownership split is chosen | Reconciling incomplete data | Yes |
| `run` | `scripts/codex-runner.ps1` | Worker launch path changes; worker internals may need root discovery | Worker cannot locate config or tasks | Yes |

## Config implications

A future design may need explicit path-base settings such as:

```json
{
  "installMode": "consumer-local",
  "platformRoot": ".ai-platform",
  "projectRoot": ".",
  "pathMode": "rooted"
}
```

These keys are design candidates only. They are not implemented today.

They would need a clear compatibility story:

- default behavior for old installs;
- dual support for root-based and isolated installs;
- explicit ownership of project metadata versus platform runtime assets;
- predictable upgrade behavior when `refresh` runs.

## Refresh implications

In an isolated model, `refresh` should:

- refresh `.ai-platform/...` artifacts only;
- avoid touching project application code;
- interpret `managedArtifacts` relative to `platformRoot`;
- remain non-destructive;
- continue to avoid commits and pushes;
- avoid overwriting project roadmap/tasks if those remain project-owned metadata outside `.ai-platform/`.

This keeps refresh aligned with the current safety posture while making the update surface easier to reason about.

## Roadmap and tasks location

This is the central design fork.

### Platform-owned metadata

Roadmap, tasks, and project memory all live inside `.ai-platform/`.

Benefits:

- one self-contained tool directory;
- simplest ignore story.

Tradeoffs:

- AI workflow state becomes local-only by default;
- teams may lose the ability to review shared roadmap/tasks in Git.

### Project-owned AI metadata

Tooling lives under `.ai-platform/`, while roadmap/tasks/project memory remain part of the project tree.

Benefits:

- keeps planning state reviewable and versioned;
- isolates the runtime/tooling without hiding project workflow artifacts.

Tradeoffs:

- two ownership zones must be explained clearly;
- commands need to understand both roots.

### Hybrid

Tooling lives under `.ai-platform/`, project AI metadata remains under `ai/`, and root `AGENTS.md` may stay visible for compatibility.

Benefits:

- pragmatic bridge from today's model;
- preserves shared workflow artifacts where desired.

Tradeoffs:

- not perfectly clean;
- compatibility rules become more subtle.

Recommended direction:

- move tooling toward `.ai-platform/`;
- keep project-specific AI metadata in `ai/` or make it configurable;
- do not decide on an automatic migration yet.

This matches the current platform philosophy: isolate where safety improves, but keep collaborative project state explicit.

## Migration strategy

### Phase 1 - Document the design

Capture the tradeoffs, command impact, and open questions.

### Phase 2 - Add optional path-base configuration

Introduce optional config such as `platformRoot` without changing current defaults.

### Phase 3 - Teach `status` and `doctor` both models

Support root-based and isolated installs in read-only diagnostics first.

### Phase 4 - Teach `refresh` to honor `platformRoot`

Update only platform-root managed artifacts while preserving non-destructive behavior.

### Phase 5 - Add an explicit migration command

For example:

```bash
ai-platform migrate --to isolated
```

The migration should be explicit, reviewable, and dry-run first.

### Phase 6 - Validate in a real consumer repository

Use a consumer repo to verify Git behavior, command routing, refresh safety, and developer ergonomics.

No automatic migration should happen in v1.

## Recommendation

Do not change the runtime now.

Keep `consumer-local + git-ignore` as the practical v1 solution. Treat `.ai-platform/` as a v2 evolution that requires configurable path roots, dual compatibility, and migration planning before any files move.

The next technical priority should be path-base configuration plus read-only compatibility in `status` and `doctor`, not an immediate relocation of assets.

## Open questions

- Should `AGENTS.md` stay at repository root for Codex compatibility?
- Should tasks and roadmap be ignored or versioned in consumer repositories?
- Should there be a project-owned metadata mode?
- How is the CLI updated when it lives under `.ai-platform/`?
- How should GitHub Actions work in an isolated model?
- Should `refresh` update workflows if those workflows are ignored?
