# TASK-0021 - Add watch CLI alias and summary rules

## Metadata

- status: pending
- type: platform
- priority: high
- team: platform-cli

## Goal

Add the remaining CLI alias for the local task watcher and document the required validation details in implementation summaries.

## Context

The repository already has:

- `scripts/task-watcher.ps1`
- `scripts/codex-exec-runner.ps1`
- `ai/commands/watch.md`
- `ai/commands/codex-exec.md`
- `ai/status/.gitkeep`

The missing CLI command is:

```powershell
ai-platform watch
```

It should launch:

```text
scripts/task-watcher.ps1
```

## Files to Read First

- `ai-platform-cli/Program.cs`
- `README.md`
- `AGENTS.md`
- `ai/commands/watch.md`
- `ai/commands/codex-exec.md`
- `scripts/task-watcher.ps1`
- `scripts/codex-exec-runner.ps1`

## Expected Files to Modify

- `ai-platform-cli/Program.cs`
- `README.md`
- `AGENTS.md`

Optional:

- `ai/commands/watch.md`
- `ai/commands/codex-exec.md`
- `scripts/codex-exec-runner.ps1`

## Implementation Steps

1. Add a top-level CLI command named `watch`.
2. Make `watch` execute `scripts/task-watcher.ps1` through the existing script runner pattern.
3. Keep `run` mapped to `scripts/codex-runner.ps1`.
4. Keep `codex-exec` mapped to `scripts/codex-exec-runner.ps1`.
5. Add `watch` to CLI help output.
6. Update README so the command table includes `ai-platform watch`.
7. Update README so the distinction is clear:
   - `run`: polling worker
   - `codex-exec`: one non-interactive execution
   - `watch`: file watcher that triggers `codex-exec`
8. Update `AGENTS.md` so summaries include validation details when build or tests are requested.

## Summary Contract

When a task is completed through the non-interactive execution path, `ai/status/latest-codex-summary.md` should include:

```md
## Validation

- Build:
- Build command:
- Tests:
- Test command:
- Additional validation:
- Not run reason:
```

If the user requested build, tests, compilation, or validation, the summary should include the commands used and the results.

## Acceptance Criteria

- `ai-platform watch` is recognized.
- `ai-platform watch` starts the task watcher script.
- Existing `run` and `codex-exec` commands still work.
- Help output lists `watch`.
- README documents `watch`.
- AGENTS.md mentions validation details in summaries.

## Validation

Run:

```powershell
dotnet build ai-platform-cli/ai-platform-cli.csproj
```

Then verify help and command routing:

```powershell
dotnet run --project ai-platform-cli/ai-platform-cli.csproj -- watch
```

Stop the watcher manually after confirming that it starts.

Also verify:

```powershell
dotnet run --project ai-platform-cli/ai-platform-cli.csproj -- codex-exec
dotnet run --project ai-platform-cli/ai-platform-cli.csproj -- run
```

Do not leave long-running watcher processes active after validation.

## Constraints

- Keep the change small.
- Do not rewrite Program.cs broadly.
- Do not change the behavior of `run` or `codex-exec`.
- Do not commit runtime files from `ai/status/` except `.gitkeep`.
