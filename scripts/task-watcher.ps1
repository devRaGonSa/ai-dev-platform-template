[CmdletBinding()]
param (
    [int]$DebounceSeconds = 2,
    [int]$PollSeconds = 5,
    [switch]$SkipInitialRun,
    [switch]$UsePollingFallback
)

Write-Host "AI Platform task watcher started..."

$defaultPendingTaskPath = "ai/tasks/pending"
$defaultWatcherLockFile = "ai/task-watcher.lock"
$defaultCodexExecLockFile = "ai/codex-exec.lock"
$statusDirectory = "ai/status"
$logsDirectory = "ai/logs"
$watcherStatusPath = Join-Path $statusDirectory "latest-watcher-run.json"

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

function Write-WatcherStatus {
    param (
        [string]$Status,
        [string]$Reason,
        [int]$ExitCode
    )

    $statusObject = [ordered]@{
        status = $Status
        reason = $Reason
        exitCode = $ExitCode
        pendingTaskPath = $pendingTaskPath
        completedAt = (Get-Date).ToString("o")
    }

    $statusObject | ConvertTo-Json -Depth 5 | Set-Content -Path $watcherStatusPath -Encoding UTF8
}

function Test-ProcessLockActive {
    param ([string]$LockFilePath)

    if (-not (Test-Path $LockFilePath)) {
        return $false
    }

    $lockContent = Get-Content $LockFilePath -ErrorAction SilentlyContinue | Select-Object -First 1
    $lockPid = 0

    if ([int]::TryParse(($lockContent -as [string]), [ref]$lockPid) -and $lockPid -gt 0) {
        try {
            Get-Process -Id $lockPid -ErrorAction Stop | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }

    return $false
}

function Invoke-CodexExecIfPending {
    param ([string]$Reason)

    if (Test-ProcessLockActive -LockFilePath $codexExecLockFile) {
        Write-Host "Codex exec is already running. Skipping trigger: $Reason"
        Write-WatcherStatus -Status "skipped" -Reason "codex-exec-already-running" -ExitCode 0
        return
    }

    $tasks = Get-ChildItem $pendingTaskPath -Filter *.md -ErrorAction SilentlyContinue | Sort-Object Name
    if (-not $tasks) {
        Write-Host "No pending tasks found after trigger: $Reason"
        Write-WatcherStatus -Status "no-pending-tasks" -Reason $Reason -ExitCode 0
        return
    }

    Write-Host "Pending task detected. Running ai-platform codex-exec..."
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $watcherLogPath = Join-Path $logsDirectory "task-watcher-$timestamp.log"

    if (Test-CommandAvailable -CommandName "ai-platform") {
        & ai-platform codex-exec *>> $watcherLogPath
    }
    else {
        powershell -ExecutionPolicy Bypass -File scripts/codex-exec-runner.ps1 *>> $watcherLogPath
    }

    $exitCode = $LASTEXITCODE
    if ($exitCode -eq 0) {
        Write-WatcherStatus -Status "success" -Reason $Reason -ExitCode $exitCode
    }
    else {
        Write-WatcherStatus -Status "failed" -Reason $Reason -ExitCode $exitCode
    }
}

$pendingTaskPath = Get-PlatformConfigValue -Path "taskPaths.pending" -DefaultValue $defaultPendingTaskPath
$watcherLockFile = Get-PlatformConfigValue -Path "worker.taskWatcherLockFile" -DefaultValue $defaultWatcherLockFile
$codexExecLockFile = Get-PlatformConfigValue -Path "worker.codexExecLockFile" -DefaultValue $defaultCodexExecLockFile

[System.IO.Directory]::CreateDirectory($statusDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($logsDirectory) | Out-Null

if (-not (Test-Path $pendingTaskPath)) {
    Write-WatcherStatus -Status "failed" -Reason "pending-task-path-not-found" -ExitCode 2
    Write-Error "Pending task path not found: $pendingTaskPath"
    exit 2
}

if (Test-Path $watcherLockFile) {
    if (Test-ProcessLockActive -LockFilePath $watcherLockFile) {
        Write-WatcherStatus -Status "locked" -Reason "task-watcher-already-running" -ExitCode 423
        Write-Error "Task watcher is already running: $watcherLockFile"
        exit 423
    }

    Remove-Item $watcherLockFile -Force -ErrorAction SilentlyContinue
}

Set-Content -Path $watcherLockFile -Value "$PID" -NoNewline

try {
    Write-WatcherStatus -Status "running" -Reason "watcher-started" -ExitCode 0

    if (-not $SkipInitialRun) {
        Invoke-CodexExecIfPending -Reason "initial-scan"
    }

    if ($UsePollingFallback) {
        Write-Host "Using polling fallback every $PollSeconds seconds. Press Ctrl+C to stop."
        while ($true) {
            Invoke-CodexExecIfPending -Reason "polling-scan"
            Start-Sleep -Seconds $PollSeconds
        }
    }

    $watcher = New-Object System.IO.FileSystemWatcher
    $watcher.Path = (Resolve-Path $pendingTaskPath).Path
    $watcher.Filter = "*.md"
    $watcher.IncludeSubdirectories = $false
    $watcher.EnableRaisingEvents = $true

    $action = {
        Start-Sleep -Seconds $Event.MessageData.DebounceSeconds
        Invoke-CodexExecIfPending -Reason $Event.SourceEventArgs.ChangeType
    }

    $messageData = @{ DebounceSeconds = $DebounceSeconds }

    Register-ObjectEvent -InputObject $watcher -EventName Created -SourceIdentifier "AiPlatformTaskCreated" -MessageData $messageData -Action $action | Out-Null
    Register-ObjectEvent -InputObject $watcher -EventName Changed -SourceIdentifier "AiPlatformTaskChanged" -MessageData $messageData -Action $action | Out-Null
    Register-ObjectEvent -InputObject $watcher -EventName Renamed -SourceIdentifier "AiPlatformTaskRenamed" -MessageData $messageData -Action $action | Out-Null

    Write-Host "Watching $pendingTaskPath for new Markdown tasks. Press Ctrl+C to stop."

    while ($true) {
        Start-Sleep -Seconds 60
    }
}
finally {
    Remove-Item $watcherLockFile -Force -ErrorAction SilentlyContinue
}
