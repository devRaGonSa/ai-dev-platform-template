# Platform Roadmap v2

This is the central direction document for the AI development platform template. Planned commands listed here are not available until implemented.

| ID | Title | Status | Goal | Expected outcome | Related future commands |
|---|---|---|---|---|---|
| R-001 | Roadmap foundation | done | Establish the roadmap, current-state snapshot, and project memory. | The repository has baseline direction, decisions, risks, and known gaps documented. | `roadmap-status`, `analyze`, `reconcile` |
| R-002 | Team model | done | Define specialized agent/team roles and responsibilities. | Planning, implementation, review, documentation, and orchestration roles are described consistently. | `plan`, `implement`, `review` |
| R-003 | Command specs | done | Specify behavior, inputs, outputs, and safety boundaries for roadmap-driven commands. | Commands can be implemented from stable specs. | `analyze`, `roadmap-status`, `plan`, `reconcile`, `implement`, `review` |
| R-004 | Read-only analysis commands | done | Inspect repository state without modifying files. | Users can understand platform health, roadmap progress, and gaps safely. | `analyze`, `roadmap-status` |
| R-005 | Planning commands | in-progress | Generate structured task proposals from roadmap items and repository state. | Roadmap items produce reviewable task files that follow the task template. | `plan` |
| R-006 | Reconciliation commands | done | Compare roadmap, code, docs, and tasks to detect drift. | Mismatches between intended direction and actual implementation are visible. | `reconcile` |
| R-007 | Implement command | in-progress | Introduce a safer task execution command to eventually replace `run`. | Task execution starts from an explicit pending task, generates an operational prompt, and stays aligned with roadmap/task state without closing directly to done. | `implement` |
| R-008 | Review workflow | in-progress | Formalize review states before tasks are completed. | Implementation, explicit task-state movement, and read-only review are distinct before tasks are marked done. | `review` |
| R-009 | Multi-team orchestration | planned | Coordinate specialized teams or agents while preserving reviewable task boundaries. | Complex work can be decomposed across roles without losing traceability. | `plan`, `implement`, `review`, `reconcile` |
| R-010 | Future versioning and OpenAI/Codex improvements | in-progress | Prepare for template versioning, upgrades, and OpenAI/Codex improvements. | A conservative managed-artifact refresh exists, and isolated consumer install has an initial design note, while versioning, rollback, migration, and upgrade intelligence remain future work. | `refresh`, `analyze`, `reconcile` |
