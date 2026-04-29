# TASK-018

## Goal
Create a complete, professional, and highly detailed `README.md` for this repository that explains the platform exactly as it exists today and can onboard new developers without additional guidance.

## Context
This repository is an AI development platform template with orchestrator docs, task workflow folders, CLI tooling, scripts, and specialized agent guidance. The current root README is missing/incomplete for the expected level of onboarding detail. We need a comprehensive README that documents structure, commands, workflows, roles, limitations, and troubleshooting using current repository reality.

## Steps

1. Inspect architecture and orchestration documentation (`ai/architecture-index.md`, `ai/orchestrator/*`, task template, existing task history) to ensure the README reflects current behavior.
2. Inspect CLI source in `ai-platform-cli/Program.cs` and scripts in `scripts/` and root installer files to document only commands and behaviors that actually exist.
3. Inspect specialized agent definitions under `agents/` (and any related files) to document each included agent and when it is used.
4. Create/replace root `README.md` with a detailed, structured guide including required sections, repository tree examples, command examples, workflow examples, and explicit future-work labeling for anything not implemented.
5. Validate README accuracy against actual repository files and command availability before marking the task complete.

## Files to Read First

- ai/architecture-index.md
- ai/orchestrator/feature-planner.md
- ai/orchestrator/planning-memory.md
- ai/task-template.md
- ai-platform-cli/Program.cs
- AGENTS.md

## Expected Files to Modify

- README.md

## Constraints

- Reflect repository behavior exactly as implemented today.
- Do not invent commands, files, or agents unless explicitly labeled as future work.
- Keep examples concrete and practical for first-time users.
- Use clear Markdown structure, tables, and runnable command examples.

## Validation

Before completing the task ensure:

- `README.md` includes all required sections from the request.
- Documented CLI commands match `ai-platform-cli/Program.cs`.
- Documented repository structure and specialized agents match actual files/directories.
- Any non-existent future features are clearly marked as future work.

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines where practical; if exceeding for documentation quality, keep scope strictly to README updates.
- Keep commits focused to this documentation task only.
