# watch

Run a local watcher for new task files in `ai/tasks/pending`.

Current implementation:

```text
scripts/task-watcher.ps1
```

The watcher observes Markdown files in the pending task directory. When a pending task exists, it starts the non-interactive Codex execution flow through the existing codex-exec command or script.

Status files:

- `ai/status/latest-watcher-run.json`
- `ai/status/latest-run.json`
- `ai/status/latest-codex-summary.md`
- `ai/status/latest-codex-output.log`

Lock files:

- `ai/task-watcher.lock`
- `ai/codex-exec.lock`

Useful options:

- `-SkipInitialRun`
- `-UsePollingFallback`
- `-DebounceSeconds 3`
- `-PollSeconds 10`

Intended future CLI alias:

```text
ai-platform watch
```
