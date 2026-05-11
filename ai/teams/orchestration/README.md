# Orchestration Team

## Responsibilities

- Own routing between teams, task format, task states, handoffs, workflow coordination, and lifecycle transition rules.

## Task state model

- `pending`: ready to be selected.
- `in-progress`: actively being worked.
- `review`: implemented and awaiting review.
- `done`: reviewed and accepted.
- `blocked`: unable to proceed because of a concrete dependency.
- `obsolete`: no longer relevant and should not be implemented.

## Typical inputs

- Roadmap items, task templates, team ownership rules, workflow risks, current task state.

## Typical outputs

- Task routing guidance, lifecycle rules, handoff notes, workflow updates, coordination decisions.

## When to involve this team

- Involve Orchestration for task generation, team assignment, lifecycle changes, handoffs, and future `plan`, `reconcile`, `implement`, or `review` behavior.

## What this team should not do

- Do not own product value, stack-specific implementation, or validation details that belong to another team.
