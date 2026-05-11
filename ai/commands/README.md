# Command Specs

Command specs are documentation contracts for platform CLI behavior. They describe future commands before implementation so later tasks can build against stable intent.

A spec does not mean the command is implemented. Existing commands are the behavior currently present in `ai-platform-cli/Program.cs`; planned commands are documented here and must still be implemented in future tasks.

These specs connect roadmap phases, team ownership, and task workflow. Future CLI work should read the relevant spec, preserve its safety rules, and update the spec when behavior changes.

| Command | Status | Primary team | Purpose | Mutates files? | Output |
|---|---|---|---|---|---|
| `ai-platform analyze` | Implemented | Orchestration | Analyze repository/platform state | Writes report only | `ai/reports/project-analysis.md` |
| `ai-platform roadmap-status` | Implemented | Product | Compare roadmap with documented status | Writes report only | `ai/reports/roadmap-status.md` |
| `ai-platform plan` | First implementation | Product | Generate one roadmap-driven pending task | Yes, creates pending task | Task file and console summary |
| `ai-platform reconcile` | First read-only implementation | Orchestration | Detect drift between roadmap, tasks, reports, and repo evidence | Writes report only | `ai/reports/task-reconciliation.md` |
| `ai-platform implement` | Spec only | Platform | Safely implement a selected task | Yes | Changed files, validation, commit/push when allowed |
| `ai-platform review` | First read-only implementation | QA | Review one task before done | Writes report only | `ai/reports/task-review.md` |
