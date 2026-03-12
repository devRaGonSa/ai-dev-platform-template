# Repository Context

## Project Overview

This repository is an AI-driven development platform template.

It provides a reusable workflow for repositories that want structured AI assistance through:

- planning guides
- task files
- a worker loop
- repository conventions
- optional automation helpers

The template is intentionally lightweight. It does not implement a business application by itself; instead, it supplies process and orchestration scaffolding that can be adapted inside another repository.

---

## Current Repository Purpose

This repository currently acts as both:

- the source template
- a self-hosted example that improves the template using its own task workflow

That means some folders contain platform code, while others contain documentation and operating instructions for AI agents.

---

## Main Areas

### AI workflow assets

The `ai/` directory contains:

- repository context
- architecture summary
- task template
- orchestrator guidance
- task lifecycle folders
- lightweight metrics/history files

These files are normative for the workflow and should stay aligned with the actual repository state.

### Automation scripts

The `scripts/` directory contains automation entrypoints used by the worker flow.

Rules:

- Scripts should be safe by default.
- Repository-specific assumptions should be documented explicitly.
- Optional scripts should fail clearly when required project configuration is missing.

### CLI

The `ai-platform-cli/` directory contains a small .NET CLI used to bootstrap or run parts of the platform.

Rules:

- Keep behavior simple and predictable.
- Prefer explicit messages when an operation depends on a repository convention.
- Avoid implying support that is not implemented.

### GitHub automation

The `.github/workflows/` directory contains workflow wiring for automated task execution.

Rules:

- Treat workflows as optional platform automation.
- Document any assumptions about credentials, remotes, or installed tools.

---

## Architecture Expectations

This template does not require one universal application architecture.

When used inside a consumer repository, agents should follow:

- the architecture that actually exists in that repository
- the dominant stack and tooling already present
- the conventions documented in that repository's own context files

If the consumer repository uses layered MVC, API services, worker processes, or another structure, tasks and validation should adapt accordingly.

---

## Testing and Validation

Validation is repository-aware.

Rules:

- Use the build and test commands that match the current repository.
- Do not assume `.NET`, EF Core, Docker, or any external service unless the repository clearly uses them.
- If integration test infrastructure is not configured, report that clearly instead of pretending it exists.
- Prefer the smallest useful validation set for the files being changed.

---

## Coding Guidelines

- Follow existing naming conventions in the current repository.
- Prefer small focused changes.
- Do not modify unrelated files.
- Keep tasks under the documented change budget when feasible.
- Prefer extending existing workflow assets instead of duplicating them.

---

## AI Workflow

The standard operating model is:

`Request -> Planning -> Tasks -> Worker execution -> Validation -> Commit/Push`

Tasks are processed from:

`ai/tasks/pending`

Each task should follow:

`ai/task-template.md`
