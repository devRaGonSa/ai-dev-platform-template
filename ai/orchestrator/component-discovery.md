# Component Discovery

Before implementing a task, analyze the repository to identify the most relevant components.

## Goals

Determine which parts of the codebase are related to the requested change with the least repository scanning possible.

## Steps

1. Identify the entrypoints affected by the change
2. Identify the modules or services that currently own the behavior
3. Identify related models, configuration, or shared utilities
4. Identify persistence, external integrations, or automation boundaries if they exist
5. Identify existing tests or docs related to the feature

## Search heuristics

Look for:

- file names or symbols matching the feature domain
- references from entrypoints to implementation modules
- configuration keys or environment variables related to the behavior
- test files covering similar scenarios
- scripts, workflows, or docs that describe the same flow

Example:

Feature: Add retry guidance for a worker command

Relevant components might be:

Docs:
- README.md
- AGENTS.md

Automation:
- scripts/codex-runner.ps1

CLI:
- ai-platform-cli/Program.cs

Tests:
- tests/WorkerRunnerTests.cs

## Output

Provide a short list of files to inspect before implementation.
