# TASK-0022 - Add platform update CLI command

## Metadata

- status: done
- type: platform
- priority: high
- team: platform-cli

## Goal

Add a first-class platform update command:

```powershell
ai-platform update
```

The command should run the update script:

```text
scripts/update-platform.ps1
```

This command updates the AI Platform itself safely whether it is being run from the platform template repository or from a client project where the platform is installed.

## Context

The repository already contains:

- `scripts/update-platform.ps1`
- `ai/commands/update.md`

The update script supports two modes:

1. Template repository mode:
   - if the current Git remote points to `ai-dev-platform-template`, it runs `git pull`.

2. Client repository mode:
   - reads `ai-platform.json`
   - downloads `templateSourceZip`
   - copies only `managedArtifacts`
   - does not run `git pull` on the client project
   - does not touch project source code unless listed as a managed artifact

## Files to Read First

- `ai-platform-cli/Program.cs`
- `ai-platform.json`
- `README.md`
- `scripts/update-platform.ps1`
- `ai/commands/update.md`

## Expected Files to Modify

- `ai-platform-cli/Program.cs`
- `ai-platform.json`
- `README.md`

Optional only if needed:

- `AGENTS.md`
- `ai/commands/update.md`

## Required Implementation

1. Add a new top-level CLI command named `update`.

2. The command should call:

```csharp
RunScript("scripts/update-platform.ps1");
```

3. Keep existing commands unchanged:

```text
ai-platform run
ai-platform codex-exec
ai-platform watch
```

4. Add `update` to CLI help output.

5. Register the update script in `ai-platform.json` managed artifacts:

```json
"scripts/update-platform.ps1"
```

It should appear near the other managed script artifacts.

6. Update README command table to include:

```powershell
ai-platform update
```

7. Explain in README that:

- in the template repository, `update` performs `git pull`
- in a client repository, `update` downloads the template ZIP and updates only managed platform artifacts
- it should not update the whole client project with `git pull`

8. Mention the status file:

```text
ai/status/latest-update.json
```

## Acceptance Criteria

- `ai-platform update` is recognized by the CLI.
- `ai-platform update` launches `scripts/update-platform.ps1`.
- CLI help includes `update`.
- README documents `update`.
- `ai-platform.json` includes `scripts/update-platform.ps1` in `managedArtifacts`.
- Existing commands `run`, `codex-exec`, and `watch` still work.
- No project source files are added to `managedArtifacts` accidentally.

## Validation

Run from repository root:

```powershell
dotnet build ai-platform-cli/ai-platform-cli.csproj
```

Then verify CLI help:

```powershell
dotnet run --project ai-platform-cli/ai-platform-cli.csproj
```

Then run a dry-run update:

```powershell
dotnet run --project ai-platform-cli/ai-platform-cli.csproj -- update -DryRun
```

If argument forwarding is not supported by the CLI wrapper, validate the script directly:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/update-platform.ps1 -DryRun
```

## Constraints

- Keep the change focused.
- Do not rewrite `Program.cs` broadly.
- Do not remove or alter existing commands.
- Do not commit runtime files from `ai/status/` except `.gitkeep`.
- Do not add `ai/status`, `ai/logs`, `ai/backups`, or lock files to managed artifacts.
