# Known Gaps

This file tracks functional gaps that are known but not yet implemented.

## Current gaps

- A first read-only `analyze` command exists, but deeper analysis and integration with `roadmap-status` or `reconcile` are still pending.
- A first read-only `roadmap-status` command exists, but real reconciliation against code and tasks is still pending.
- A first roadmap-driven `plan` command exists and generates one task per execution, but multi-task planning, deep analysis, and automatic team splitting are still pending.
- A first read-only `reconcile` command exists, but smarter proposed changes, review/implement integration, and safe movement to review/blocked/obsolete are still pending.
- A conservative `implement` v1 exists, but direct execution, review integration, deeper validation, and automatic cycle closure are still pending.
- A conservative `refresh` v1 exists, but semantic versioning, backups or rollback, merge intelligence, and richer local or remote comparison are still pending.
- The current managed artifact scope is intentionally coarse (`ai`, `scripts`, `.github`, `AGENTS.md`, `ai-platform.json`); future refresh behavior should support finer-grained artifacts, profiles, or per-artifact policies for consumer repositories.
- A first read-only `review` command exists, but safe movement based on outcomes, implement integration, and deeper validation against real code changes are still pending.
- Physical lifecycle directories for `review`, `blocked`, and `obsolete` exist, but safe automated movement between states is still pending.
- A basic documented team model exists, but there is no automatic routing or real multi-team execution.
- The formal `review` state exists, but automated state transitions from review outcomes are still pending.
- There is no comparison between roadmap and real repository state.
- There is no automatic task generation from roadmap items.
- A managed artifact inventory now exists in `ai-platform.json` and is used by conservative refresh, but it is not yet part of a complete upgrade workflow.
- There is no remote template versioning or upgrade policy.
