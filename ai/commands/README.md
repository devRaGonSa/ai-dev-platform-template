# Command Specs

Command specs are documentation contracts for platform CLI behavior. They describe future commands before implementation so later tasks can build against stable intent.

A spec does not mean the command is implemented. Existing commands are the behavior currently present in `ai-platform-cli/Program.cs`; planned commands are documented here and must still be implemented in future tasks.

These specs connect roadmap phases, team ownership, and task workflow. Future CLI work should read the relevant spec, preserve its safety rules, and update the spec when behavior changes.

| Command | Status | Primary team | Purpose | Mutates files? | Output |
|---|---|---|---|---|---|
| `ai-platform analyze` | Spec only | Orchestration | Analyze repository/platform state | No | `ai/reports/project-analysis.md` |
| `ai-platform roadmap-status` | Spec only | Product | Compare roadmap with evidence | No | `ai/reports/roadmap-status.md` |
| `ai-platform plan` | Future spec; current CLI is only a simple helper | Product | Generate roadmap-driven tasks | Yes, creates pending tasks | Task files and summary |
| `ai-platform reconcile` | Spec only | Orchestration | Detect drift between roadmap, tasks, reports, and repo evidence | No by default | `ai/reports/task-reconciliation.md` |
| `ai-platform implement` | Spec only | Platform | Safely implement a selected task | Yes | Changed files, validation, commit/push when allowed |
| `ai-platform review` | Spec only | QA | Review implemented work before done | Report only by default | `ai/reports/task-review.md` |
