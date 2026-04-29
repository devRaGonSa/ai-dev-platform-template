# Dependency Injection Analysis

Analyze the repository's dependency wiring to understand how components are connected.

## Objectives

Identify how entrypoints, services, helpers, and infrastructure components depend on each other before introducing new code.

## Steps

1. Inspect the repository's composition root or wiring mechanism.
2. Locate registrations, factory methods, constructor dependencies, or setup scripts.
3. Map the relationship:

Abstraction or caller -> implementation or dependency

4. Identify which entrypoints depend on those components.
5. Determine downstream dependencies such as storage, external services, CLI commands, scripts, or configuration.

## Common places to inspect

- `Program.cs`
- `Startup.cs`
- dependency registration modules
- factory or bootstrap classes
- shell or PowerShell scripts that assemble commands
- workflow files under `.github/workflows/`

## Example

Dependency map:

Entrypoint:
- `scripts/codex-runner.ps1`

CLI:
- `ai-platform run` -> `scripts/codex-runner.ps1`

Infrastructure:
- worker script -> `git`
- worker script -> `codex`

## Output

Produce a dependency map that identifies:

- the component being changed
- the components that call it
- the services, tools, or infrastructure it relies on

This information should guide which components are modified in a task.
