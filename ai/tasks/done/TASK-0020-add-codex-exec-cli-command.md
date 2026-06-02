# TASK-0020 - Add codex-exec CLI command

## Metadata

- status: done
- type: platform
- priority: high
- team: platform-cli
- roadmap item: N/A - platform orchestration improvement

## Goal

Add a first-class CLI command so users can run the new non-interactive Codex execution flow with:

```powershell
ai-platform codex-exec
```

The command must execute the existing script:

```text
scripts/codex-exec-runner.ps1
```

This should behave similarly to `ai-platform run`, but must use the new single-shot Codex exec runner instead of the long-running polling worker.

## Context

The repository already contains:

- `scripts/codex-exec-runner.ps1`
- `ai/status/.gitkeep`
- `ai/commands/codex-exec.md`
- `worker.codexExecLockFile` in `ai-platform.json`

The missing part is the .NET CLI alias inside `ai-platform-cli/Program.cs`.

## Files to Read First

- `ai-platform-cli/Program.cs`
- `ai-platform.json`
- `scripts/codex-exec-runner.ps1`
- `ai/commands/codex-exec.md`
- `README.md`

## Expected Files to Modify

- `ai-platform-cli/Program.cs`
- `README.md`

Optional only if needed:

- `ai-platform.json`
- `ai/commands/codex-exec.md`

## Required Implementation

1. Add a new top-level CLI command named `codex-exec`.
2. The command should call the existing PowerShell script:

   ```csharp
   RunScript("scripts/codex-exec-runner.ps1");
   ```

3. Keep `ai-platform run` unchanged.
4. Update CLI help output so `codex-exec` appears as a supported command.
5. Update the README command table to include:

   ```text
   ai-platform codex-exec
   ```

6. Add a short README note explaining that:
   - `run` starts the existing worker flow.
   - `codex-exec` starts a single non-interactive `codex exec` run.
   - completion is communicated through `ai/status/latest-run.json` and `ai/status/latest-codex-summary.md`.

## Acceptance Criteria

- `ai-platform codex-exec` is recognized by the CLI.
- The command launches `scripts/codex-exec-runner.ps1`.
- `ai-platform run` continues launching `scripts/codex-runner.ps1`.
- CLI help includes `codex-exec`.
- README documents the new command and the status files.
- No unrelated platform behavior is changed.

## Validation

Run from the repository root:

```powershell
dotnet build ai-platform-cli/ai-platform-cli.csproj
```

If possible, run:

```powershell
dotnet run --project ai-platform-cli/ai-platform-cli.csproj -- codex-exec
```

If no pending tasks exist, the command should exit cleanly after writing a no-pending-tasks status to `ai/status/latest-run.json`.

## Constraints

- Do not rewrite `Program.cs` broadly.
- Make the smallest safe change around the command switch and help output.
- Do not modify the runner script unless a clear bug is found while validating.
- Do not remove or change `ai-platform run`.
- Keep the change focused to the CLI alias and documentation.
