# Codex Repository Agent Rules

This repository uses an AI-driven task workflow.

## Task locations

Pending tasks:
ai/tasks/pending

Tasks being worked on:
ai/tasks/in-progress

Completed tasks:
ai/tasks/done

All tasks created in ai/tasks/pending must follow the template defined in ai/task-template.md.

## Workflow

1. Always process the first task in ai/tasks/pending
2. Move the task to ai/tasks/in-progress
3. Implement the task
Before marking a task as completed:

1. Run git diff --stat
2. Verify that the change budget limits are respected.
3. If limits are exceeded, split the work into new tasks.

4. Commit the changes
5. Move the task to ai/tasks/done
6. Continue until no tasks remain

## Review loop

When no pending tasks exist:

1. Review the repository
2. Detect bugs, missing tests or architecture issues
3. If issues exist create new tasks in ai/tasks/pending
4. Repeat the workflow

## Rules

- Follow ASP.NET Core MVC architecture
- Do not modify unrelated files
- Prefer small commits
- Create tests when necessary

## Git workflow

Before starting any task:

1. Run git pull to synchronize the repository.

After finishing a task cycle:

1. Run dotnet build
2. Run dotnet test
3. If build and tests succeed, commit changes
4. Run git push

## Branch workflow

When implementing a new set of tasks for a feature:

1. Create a new branch using the format:

feature/<short-feature-name>

2. All task commits must happen in this branch.

3. After finishing the tasks and passing build and tests:

- push the branch
- create a Pull Request to main

4. The Pull Request should include:

- summary of the feature
- list of implemented tasks
- test validation results

## Change limits

To keep the repository stable:

- A task should modify only files directly related to the task goal.
- Prefer modifying fewer than 5 files per task.
- Prefer changes smaller than 200 lines of code.
- If the change would exceed these limits, create additional tasks instead.

## AI Change Budget

To keep the repository stable, every task must respect the following limits:

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Prefer fewer than 3 commits per task.

If a change exceeds these limits:

- stop implementation
- create a follow-up task in ai/tasks/pending
- continue the work in that new task.

This ensures tasks remain small, safe and reviewable.

## Loop Protection

The system must avoid infinite task generation.

Rules:

- Prefer generating no more than 3 follow-up tasks at once.
- If multiple improvements are found, prioritize the most critical ones.
- Do not generate tasks that repeat previously completed tasks.

## Execution Budget

To control resource usage:

- Prefer short prompts.
- Avoid scanning the entire repository when not required.
- Prefer incremental analysis.
- Limit each planning or review step to the minimum context required.

## Integration Validation

If a task modifies:

- database schema
- EF Core models
- services interacting with storage
- MinIO or external services

Run integration tests only if the script exists:

scripts/run-integration-tests.ps1

If it does not exist, skip integration tests and log:

No integration tests configured.

Only mark the task completed if configured integration tests succeed.

## File Change Validation

Before completing a task:

1. Run git diff --name-only
2. Compare modified files with the "Expected Files to Modify" section in the task.
3. If unrelated files were modified:
   - revert those changes
   - or create a new follow-up task explaining the required changes.

This prevents accidental modifications across the repository.

## Context Discovery

Before implementing a task:

1. Read the files listed in "Files to Read First".
2. Understand how the existing code implements similar behavior.
3. Only then start implementing the task.

Do not modify code before reviewing these files.

## Component Discovery

Before implementing a task:

1. Use ai/orchestrator/component-discovery.md
2. Identify controllers, services, models and DbContext related to the task.
3. Read those files before modifying code.
4. Prefer modifying existing components rather than creating duplicates.

## Dependency Analysis

Before modifying services or controllers:

1. Analyze dependency injection configuration using ai/orchestrator/di-analysis.md.
2. Identify existing services and their dependencies.
3. Prefer extending existing services instead of creating new ones.
4. Ensure new services are registered in the DI container.

## Architecture Index

Before exploring the repository structure, read:

ai/architecture-index.md

This file provides a high level overview of the project structure.

Use it to identify the relevant layers and components before scanning the codebase.
