# codex-exec

Run Codex CLI in non-interactive mode for the first pending AI platform task.

Current implementation is the PowerShell script `scripts/codex-exec-runner.ps1`.

The runner creates these status files:

- `ai/status/latest-run.md`
- `ai/status/latest-run.json`
- `ai/status/latest-codex-output.log`
- `ai/status/latest-codex-summary.md`

`latest-run.json` is the machine-readable completion signal.

`latest-codex-summary.md` is the human-readable implementation summary that Codex CLI is instructed to write.

Because this path uses non-interactive execution, the Codex CLI process is expected to finish and exit after the prompt completes.

Codex App or a reviewer should read the status files after completion instead of trying to inspect the Codex CLI process directly.
