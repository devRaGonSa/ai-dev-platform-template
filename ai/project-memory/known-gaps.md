# Known Gaps

This file tracks functional gaps that are known but not yet implemented.

## Current gaps

- A first read-only `analyze` command exists, but deeper analysis and integration with `roadmap-status` or `reconcile` are still pending.
- A first read-only `roadmap-status` command exists, but real reconciliation against code and tasks is still pending.
- A first roadmap-driven `plan` command exists and generates one task per execution, but multi-task planning, deep analysis, and automatic team splitting are still pending.
- A first read-only `reconcile` command exists, but smarter proposed changes, review/implement integration, and safe movement to review/blocked/obsolete are still pending.
- Command specs exist, but there is no implemented `implement` command as a replacement for `run`.
- Command specs exist, but there is no implemented `review` command.
- Physical lifecycle directories for `review`, `blocked`, and `obsolete` exist, but safe automated movement between states is still pending.
- A basic documented team model exists, but there is no automatic routing or real multi-team execution.
- There is no implemented review command for the formal `review` state.
- There is no comparison between roadmap and real repository state.
- There is no automatic task generation from roadmap items.
- There is no managed artifact inventory for safe refresh/update behavior.
- There is no remote template versioning or upgrade policy.
