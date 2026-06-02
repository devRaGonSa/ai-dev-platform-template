# update

Update the AI Platform files in the current repository.

Current implementation:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/update-platform.ps1
```

Intended CLI alias:

```powershell
ai-platform update
```

## Modes

The script detects where it is running.

### Template repository mode

If the current repository is the AI Platform template repository, the update runs:

```powershell
git pull
```

This updates the template repository itself.

### Client repository mode

If the current repository is a project that has AI Platform installed inside it, the update does not run a normal `git pull` for the project.

Instead, it:

1. Reads `ai-platform.json`.
2. Reads `templateSourceZip`.
3. Downloads the template ZIP.
4. Copies only the files listed in `managedArtifacts`.
5. Leaves project source code untouched.
6. Leaves task/status/runtime folders untouched unless those paths are explicitly managed artifacts.

## Why not always git pull?

In a client project, a normal `git pull` updates the client repository, not the AI Platform template.

The safe update mechanism must only update platform-managed files.

## Managed artifacts

The list comes from:

```json
managedArtifacts
```

inside:

```text
ai-platform.json
```

Typical managed artifacts include:

```text
AGENTS.md
ai-platform.json
scripts/codex-runner.ps1
scripts/codex-exec-runner.ps1
scripts/task-watcher.ps1
scripts/update-platform.ps1
scripts/install-local-cli.ps1
scripts/run-integration-tests.ps1
.github/workflows/codex-worker.yml
ai/task-template.md
```

## Runtime files not intended for update commits

Do not commit generated runtime files unless explicitly required:

```text
ai/status/latest-*.json
ai/status/latest-*.md
ai/logs/*
ai/*.lock
ai/backups/*
```

## Options

```powershell
powershell -ExecutionPolicy Bypass -File scripts/update-platform.ps1 -DryRun
powershell -ExecutionPolicy Bypass -File scripts/update-platform.ps1 -NoBackup
powershell -ExecutionPolicy Bypass -File scripts/update-platform.ps1 -TemplateZipUrl "https://..."
```

## Status file

The update writes:

```text
ai/status/latest-update.json
```
