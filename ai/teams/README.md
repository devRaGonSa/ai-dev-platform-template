# Team Model

The team model is a documentation layer for planning, assigning, and reviewing work. It does not represent real autonomous agents yet.

Future tasks may use `team` for the primary owner and `supporting_teams` for secondary reviewers or contributors. A task should have one primary team when possible, plus support teams only when their responsibility is directly involved.

Real multi-team routing and execution will arrive in later roadmap phases.

| Team | Responsibility | Typical work | Should not own |
|---|---|---|---|
| Product | Roadmap, priority, functional intent, value | Goals, requirements, acceptance direction | Technical implementation details |
| Platform | Platform CLI, config, core commands, worker behavior | `ai-platform`, `ai-platform.json`, command specs | Consumer app logic unless requested |
| Orchestration | Task flow, team routing, handoffs, states | Task format, lifecycle rules, coordination | Product value or stack-specific code |
| Frontend | UI, UX, client integration, basic accessibility | Views, components, client-side API use | Backend/domain ownership |
| Backend | APIs, services, domain rules, contracts | API behavior, business validation, service changes | UI design or database administration alone |
| Database | Data model, migrations, queries, integrity | Schema, persistence, performance, consistency | Product priority or UI behavior |
| QA | Acceptance criteria, tests, regressions, review gates | Test plans, validation, review before done | Feature ownership without a task owner |
| DevOps | CI/CD, scripts, workflows, environments | Worker automation, GitHub Actions, execution safety | Product requirements |
| Security | Secrets, permissions, auth, dangerous flows | Push/merge/deploy review, auth risk, sensitive data | General implementation when no security risk exists |
| Docs | README, guides, command docs, decisions | User docs, task docs, memory updates | Behavior changes without implementation owner |
