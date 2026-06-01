[CmdletBinding()]
param (
    [switch]$SkipGitPull,
    [switch]$NoCommitPush,
    [string]$Prompt = "Follow the workflow defined in AGENTS.md and process the first pending task. Use the repository rules, run the relevant validation commands, commit only when validation succeeds, and before finishing write or overwrite ai/status/latest-codex-summary.md with a concise implementation summary including task, files changed, validation, commit, and any follow-up notes."
)

Write-Host "Codex exec runner started..."

$defaultLockFile = "ai/codex-exec.lock"
$defaultPendingTaskPath = "ai/tasks/pending"
$statusDirectory = "ai/status"
$logsDirectory = "ai/logs"
$latestRunMarkdownPath = Join-Path $statusDirectory "latest-run.md"
$latestRunJsonPath = Join-Path $statusDirectory "latest-run.json"
$latestCodexOutputPath = Join-Path $statusDirectory "latest-codex-output.log"
$latestCodexSummaryPath = Join-Path $statusDirectory "latest-codex-summary.md"

function Get-PlatformConfigValue {
    param (
        [string]$Path,
        [object]$DefaultValue
    )

    $configPath = "ai-platform.json"
    if (-not (Test-Path $configPath)) {
        return $DefaultValue
    }

    try {
        $config = Get-Content $configPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        $current = $config

        foreach ($segment in $Path.Split('.')) {
            if ($null -eq $current) {
                return $DefaultValue
            }

            $property = $current.PSObject.Properties[$segment]
            if ($null -eq $property) {
                return $DefaultValue
            }

            $current = $property.Value
        }

        if ($null -eq $current -or [string]::IsNullOrWhiteSpace(($current -as [string]))) {
            return $DefaultValue
        }

        return $current
    }
    catch {
        return $DefaultValue
    }
}

function Test-CommandAvailable {
    param ([string]$CommandName)
    return $null -ne (Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Write-LatestRunStatus {
    param (
        [string]$Status,
        [string]$TaskName,
        [int]$ExitCode,
        [double]$DurationSeconds,
        [string]$CommitSha,
        [string]$Notes
    )

    $completedAt = (Get-Date).ToString("o")
    $safeTaskName = if ([string]::IsNullOrWhiteSpace($TaskName)) { "none" } else { $TaskName }
    $safeCommitSha = if ([string]::IsNullOrWhiteSpace($CommitSha)) { "none" } else { $CommitSha }

    $markdown = @"
# Latest Codex exec run

## Status
$Status

## Task
$safeTaskName

## Codex exit code
$ExitCode

## Duration seconds
$([math]::Round($DurationSeconds, 2))

## Commit
$safeCommitSha

## Output files
- ai/status/latest-codex-summary.md
- ai/status/latest-codex-output.log
- ai/status/latest-run.json

## Notes
$Notes

## Completed at
$completedAt
"@

    $jsonObject = [ordered]@{
        status = $Status
        task = $safeTaskName
        codexExitCode = $ExitCode
        durationSeconds = [math]::Round($DurationSeconds, 2)
        commit = $safeCommitSha
        latestCodexSummary = "ai/status/latest-codex-summary.md"
        latestCodexOutput = "ai/status/latest-codex-output.log"
        completedAt = $completedAt
        notes = $Notes
    }

    Set-Content -Path $latestRunMarkdownPath -Value $markdown -Encoding UTF8
    $jsonObject | ConvertTo-Json -Depth 5 | Set-Content -Path $latestRunJsonPath -Encoding UTF8
}

$lockFile = Get-PlatformConfigValue -Path "worker.codexExecLockFile" -DefaultValue $defaultLockFile
$pendingTaskPath = Get-PlatformConfigValue -Path "taskPaths.pending" -DefaultValue $defaultPendingTaskPath

[System.IO.Directory]::CreateDirectory($statusDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($logsDirectory) | Out-Null

if (-not (Test-CommandAvailable -CommandName "codex")) {
    Write-LatestRunStatus -Status "failed" -TaskName "none" -ExitCode 127 -DurationSeconds 0 -CommitSha "none" -Notes "Codex CLI was not found in PATH."
    Write-Error "Codex CLI was not found in PATH."
    exit 127
}

if (Test-Path $lockFile) {
    Write-LatestRunStatus -Status "locked" -TaskName "none" -ExitCode 423 -DurationSeconds 0 -CommitSha "none" -Notes "A Codex exec lock already exists at $lockFile."
    Write-Error "Codex exec runner is already locked: $lockFile"
    exit 423
}

$tasks = Get-ChildItem $pendingTaskPath -Filter *.md -ErrorAction SilentlyContinue | Sort-Object Name

if (-not $tasks) {
    Write-Host "No pending tasks found."
    Write-LatestRunStatus -Status "no-pending-tasks" -TaskName "none" -ExitCode 0 -DurationSeconds 0 -CommitSha "none" -Notes "No Markdown task files were found in $pendingTaskPath."
    exit 0
}

$task = $tasks[0]
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runLogPath = Join-Path $logsDirectory "codex-exec-run-$timestamp.log"

Set-Content -Path $lockFile -Value "$PID" -NoNewline

try {
    $startTime = Get-Date
    Set-Content -Path $latestCodexSummaryPath -Value "# Latest Codex implementation summary`n`nStatus: pending`nTask: $($task.Name)`n" -Encoding UTF8

    if (-not $SkipGitPull -and (Test-Path ".git")) {
        Write-Host "Synchronizing repository..."
        git pull *>> $runLogPath
    }

    Write-Host "Running Codex CLI in non-interactive exec mode for $($task.Name)..."
    & codex exec $Prompt *> $latestCodexOutputPath
    $codexExitCode = $LASTEXITCODE

    Get-Content $latestCodexOutputPath -ErrorAction SilentlyContinue | Add-Content $runLogPath

    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds
    $commitSha = "none"

    if ($codexExitCode -eq 0) {
        Add-Content ai/system-metrics.md "$(Get-Date) | codex-exec-run | $duration sec | success"

        if (Test-Path "scripts/run-integration-tests.ps1") {
            Write-Host "Running integration tests..."
            powershell -ExecutionPolicy Bypass -File scripts/run-integration-tests.ps1 *>> $runLogPath
            if ($LASTEXITCODE -ne 0) {
                Write-LatestRunStatus -Status "validation-failed" -TaskName $task.Name -ExitCode $LASTEXITCODE -DurationSeconds $duration -CommitSha "none" -Notes "Codex completed but integration validation failed. See $runLogPath."
                exit $LASTEXITCODE
            }
        }
        else {
            Write-Host "No integration tests configured."
        }

        if (-not $NoCommitPush) {
            $gitChanges = git status --porcelain
            if ($gitChanges) {
                Write-Host "Changes detected. Committing and pushing..."
                git add -A
                git commit -m "chore: process pending task via codex exec" *>> $runLogPath
                if ($LASTEXITCODE -eq 0) {
                    $commitSha = (git rev-parse --short HEAD).Trim()
                    git push *>> $runLogPath
                }
                else {
                    Write-Host "No commit created. Skipping push."
                }
            }
            else {
                Write-Host "No changes detected. Skipping commit and push."
            }
        }
        else {
            Write-Host "NoCommitPush enabled. Skipping commit and push."
        }

        Write-LatestRunStatus -Status "success" -TaskName $task.Name -ExitCode $codexExitCode -DurationSeconds $duration -CommitSha $commitSha -Notes "Codex exec completed. Review ai/status/latest-codex-summary.md and $runLogPath."
        exit 0
    }

    Add-Content ai/system-metrics.md "$(Get-Date) | codex-exec-run | $duration sec | failed($codexExitCode)"
    Write-LatestRunStatus -Status "failed" -TaskName $task.Name -ExitCode $codexExitCode -DurationSeconds $duration -CommitSha "none" -Notes "Codex exec failed. Review ai/status/latest-codex-output.log and $runLogPath."
    exit $codexExitCode
}
finally {
    Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
}
