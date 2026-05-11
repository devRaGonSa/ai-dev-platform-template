# Project Risks

This file records current risks that future tasks should consider.

## Current risks

- The installer, worker, and documented workflow can diverge as the template evolves.
- Remote versioning and upgrade paths are not defined yet.
- A future refresh flow could overwrite local changes if it lacks backups or merge intelligence.
- The GitHub Actions workflow differs from the local worker, which can produce different behavior in CI and locally.
- Automatically moving tasks to `done` without review could hide incomplete or unsafe work.
- The roadmap can become stale if it is not updated during each meaningful platform iteration.
- Introducing real multi-agent orchestration too early could add complexity before command boundaries are stable.
- Git automation may be unsafe for repositories that do not want automatic pull, commit, or push behavior.
- Integration validation may be skipped in consumer repositories that have not adapted the placeholder script.
