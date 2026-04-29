# Architecture Index

This file provides a quick overview of the template repository architecture.

AI agents should read this file before scanning the repository so they can target the right platform areas with minimal exploration.

---

## Repository Type

AI workflow and automation template repository.

This repository is not the source code of a single product application. Its main responsibility is to provide reusable process scaffolding for AI-assisted development.

---

## Core Areas

### Root-level operating files

- `README.md`: human-facing documentation and usage guidance
- `AGENTS.md`: normative workflow rules for AI task execution
- `install-ai-platform.ps1`: bootstrap script for installing seed platform files into another repository

### AI knowledge base

Path:

`ai/`

Contains:

- `repo-context.md`
- `architecture-index.md`
- `task-template.md`
- `system-metrics.md`
- `prompts/`
- `orchestrator/`
- `tasks/`

Purpose:

- provide planning context
- standardize task creation
- guide worker and reviewer behavior

### Orchestrator guidance

Path:

`ai/orchestrator/`

Contains role-specific guidance such as:

- feature planning
- component discovery
- dependency analysis
- repository review
- PR generation

These files should stay generic enough to apply to different repositories while still giving concrete instructions.

### Task lifecycle

Path:

`ai/tasks/`

Folders:

- `pending`
- `in-progress`
- `done`

The worker flow processes the first pending task, moves it through the lifecycle, and records the result through normal repository changes.

### Automation scripts

Path:

`scripts/`

Current scripts include:

- `codex-runner.ps1`: local polling worker loop
- `run-integration-tests.ps1`: repository-level integration test hook that must be adapted by consumers when needed

### CLI

Path:

`ai-platform-cli/`

Purpose:

- expose lightweight platform commands such as `init`, `run`, `plan`, and `doctor`
- bootstrap a repository with platform files
- launch the worker loop from a consistent entrypoint

### CI automation

Path:

`.github/workflows/`

Purpose:

- trigger worker automation in GitHub Actions
- install required runtime tools for automation
- commit/push changes when the workflow is configured to do so

---

## Dependency and Stack Notes

- The template currently includes PowerShell-based automation.
- The CLI is currently implemented in .NET.
- Some validation guidance and examples reference common layered application patterns, but those examples are illustrative rather than mandatory for all consumer repositories.

Agents should not infer that every repository using this template is ASP.NET Core MVC or EF Core based.

---

## Change Guidance

When working in this repository:

- prefer updating existing platform files over creating duplicates
- keep examples concrete but generic
- align documentation with actual behavior
- document limitations instead of overstating genericity

When working in a consumer repository:

- follow the architecture that exists there
- inspect the repository before assuming controllers, services, database layers, or specific test tools
