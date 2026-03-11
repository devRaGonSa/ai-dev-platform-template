# Feature Planner

## Purpose
Provide a repeatable planning scaffold for AI agents to turn a feature request into a clear, implementation-ready plan.

## Required Inputs
- Feature request summary
- User and business goal
- Current repository context (relevant modules, constraints, existing behavior)
- Success criteria
- Explicit non-goals or out-of-scope items
- Risks, dependencies, or blockers known at planning time

## Planning Workflow
1. Clarify the feature objective in one sentence.
2. Gather repository context from the minimum relevant files.
3. Define scope boundaries (in scope vs out of scope).
4. Break implementation into small, ordered work items.
5. Identify interface or contract changes (APIs, models, config, docs).
6. Define test scenarios (happy path, edge cases, regressions).
7. Add validation steps and rollout considerations.
8. Review the plan against constraints and simplify where possible.

## Output Format
The planner output should include:
1. Title
2. Summary
3. Implementation Changes
4. Test Plan
5. Assumptions and Defaults

Use concise Markdown with clear bullets and ordered steps. Keep it decision-complete so another engineer or agent can implement without guessing.

## Constraints
- Follow ASP.NET Core MVC architecture.
- Do not modify unrelated files.
- Keep each task focused and small.
- Prefer changes under 5 files and under 200 lines when feasible.
- Split large changes into follow-up tasks.
- Prefer small commits.

## Acceptance Checklist
- The goal is clear and measurable.
- Scope and non-goals are explicit.
- Implementation steps are ordered and actionable.
- Interface changes are identified (or explicitly noted as none).
- Test scenarios cover functional and regression risk.
- Constraints are respected.
- Assumptions are documented.

## Task Generation
After producing the implementation plan, convert the plan into executable tasks.

Rules:

- Generate tasks in the directory: `ai/tasks/pending`
- Each task must follow the template defined in `ai/task-template.md`
- Every generated task must include a section: `## Files to Read First`
- The planner should analyze the repository structure and select a small set of files that are relevant to the feature
- The section should contain 3-6 files when possible
- Guidelines for `Files to Read First`:
  - Controllers related to the feature
  - Services implementing similar logic
  - EF Core DbContext or models
  - Existing tests related to the area
- Every generated task must include a section: `## Expected Files to Modify`
- The planner should analyze the repository structure and predict which files are likely to change for the task
- Each task should implement a single logical change
- Prefer tasks under 200 lines of code
- Prefer modifying fewer than 5 files per task
- If business logic is added or modified, create an additional task for tests

Task file naming:

`TASK-XXX-short-description.md`

Example flow:

`Feature -> Planning -> Task files -> Codex worker execution`

Example:

Task: Add email reminder service

Expected Files to Modify:

- Services/ReminderService.cs
- Controllers/AdminController.cs
- Models/Reminder.cs
- Data/ApplicationDbContext.cs

## Planning Memory

Before generating tasks, read:

`ai/orchestrator/planning-memory.md`

Use the lessons learned to improve task planning.

Avoid repeating mistakes documented in the planning memory.

## Component Discovery Integration

When generating tasks:

1. Use component-discovery.md to analyze the repository.
2. Determine the relevant components for the feature.
3. Include those components in the "Files to Read First" section.

## Dependency Awareness

When planning tasks:

1. Use di-analysis.md to understand service dependencies.
2. Avoid creating duplicate services.
3. Prefer extending existing services when possible.

## Architecture Awareness

Before planning tasks:

1. Read ai/architecture-index.md
2. Identify the relevant layers for the feature.
3. Only then scan specific files in the repository.
