# Known Gaps

This file tracks functional gaps that are known but not yet implemented.

## Current gaps

- A first read-only `analyze` command exists, but deeper analysis and integration with `roadmap-status` or `reconcile` are still pending.
- A first read-only `roadmap-status` command exists, but real reconciliation against code and tasks is still pending.
- A first roadmap-driven `plan` command exists and generates one task per execution, but multi-task planning, deep analysis, and automatic team splitting are still pending.
- A first read-only `reconcile` command exists, but smarter proposed changes, review/implement integration, and safe movement to review/blocked/obsolete are still pending.
- A conservative `implement` v1 exists and recommends the explicit next command to move a task to review after implementation, but direct execution, richer safe automation, deeper review-outcome integration, evidence checks against real code changes, and automatic cycle closure are still pending.
- An explicit `task move` v1 exists, but richer integration between review outcomes and state transitions is still pending.
- A conservative `refresh` v1 exists, but semantic versioning, backups or rollback, merge intelligence, and richer local or remote comparison are still pending.
- `refresh` now uses a finer-grained managed artifact list by default, but future refresh behavior should still support profiles and per-artifact policies for consumer repositories.
- A first read-only `review` command exists and recommends an explicit next `task move` command, but guided execution, automatic outcome application under controlled policies, implement integration, and deeper validation against real code changes are still pending.
- Physical lifecycle directories for `review`, `blocked`, and `obsolete` exist, and an explicit move command now exists, but safe automated movement between states is still pending.
- A basic documented team model exists, but there is no automatic routing or real multi-team execution.
- The formal `review` state exists, but automated state transitions from review outcomes are still pending.
- There is no comparison between roadmap and real repository state.
- There is no automatic task generation from roadmap items.
- A managed artifact inventory now exists in `ai-platform.json` and is used by conservative refresh, but it is not yet part of a complete upgrade workflow or a richer local-versus-remote diff policy.
- There is no remote template versioning or upgrade policy.
- There is no evidence-aware gate before `done`; future workflow automation should combine review findings, validation evidence, and explicit state movement safely.
- Consumer-local installs now have an explicit `.gitignore` helper, but the platform is not yet isolated under a dedicated `.ai-platform/` layout; a preliminary design now exists in `ai/design/isolated-consumer-install.md`.
- There are no install profiles yet for balancing source-managed and local-only artifacts.
- There is no configurable path base such as `platformRoot`, and therefore no supported dual layout between root-based and isolated installs.
- There is no automatic migration for platform files that were already tracked before switching to consumer-local.
- Local tooling updates still lack full rollback, backup, and richer upgrade policy support.
- `git-ignore` prepares ignore rules only; it does not run `git rm --cached` automatically.
