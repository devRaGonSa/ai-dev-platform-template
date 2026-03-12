Write-Host "Codex worker started..."

$defaultLockFile = "ai/worker.lock"
$defaultPendingTaskPath = "ai/tasks/pending"
$defaultPollIntervalSeconds = 30

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

$lockFile = Get-PlatformConfigValue -Path "worker.lockFile" -DefaultValue $defaultLockFile
$pendingTaskPath = Get-PlatformConfigValue -Path "taskPaths.pending" -DefaultValue $defaultPendingTaskPath
$pollIntervalValue = Get-PlatformConfigValue -Path "worker.pollIntervalSeconds" -DefaultValue $defaultPollIntervalSeconds
$pollIntervalSeconds = 0

if (-not [int]::TryParse(($pollIntervalValue -as [string]), [ref]$pollIntervalSeconds) -or $pollIntervalSeconds -le 0) {
    $pollIntervalSeconds = $defaultPollIntervalSeconds
}

function Test-WorkerProcessActive {
    param (
        [string]$LockFilePath
    )

    if (-not (Test-Path $LockFilePath)) {
        return $false
    }

    $lockContent = (Get-Content $LockFilePath -ErrorAction SilentlyContinue | Select-Object -First 1)
    $workerPid = 0

    if ([int]::TryParse(($lockContent -as [string]), [ref]$workerPid) -and $workerPid -gt 0) {
        try {
            Get-Process -Id $workerPid -ErrorAction Stop | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }

    $processes = Get-CimInstance Win32_Process -Filter "Name = 'powershell.exe' OR Name = 'pwsh.exe'" -ErrorAction SilentlyContinue
    foreach ($process in $processes) {
        if ($process.CommandLine -and $process.CommandLine -like "*scripts/codex-runner.ps1*" -and $process.ProcessId -ne $PID) {
            return $true
        }
    }

    return $false
}

while ($true) {

    if (Test-Path $lockFile) {
        if (Test-WorkerProcessActive -LockFilePath $lockFile) {
            Write-Host "Worker already running. Waiting..."
            Start-Sleep -Seconds $pollIntervalSeconds
            continue
        }

        Write-Host "Stale worker lock detected. Removing it."
        Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Checking repository state..."

    git pull

    $tasks = Get-ChildItem $pendingTaskPath -Filter *.md -ErrorAction SilentlyContinue

    if ($tasks) {

        Write-Host "Tasks detected. Starting Codex..."

        Set-Content -Path $lockFile -Value "$PID" -NoNewline

        try {
            $startTime = Get-Date
            codex "Follow the workflow defined in AGENTS.md and process the pending tasks."
            $codexExitCode = $LASTEXITCODE
            $endTime = Get-Date
            $duration = ($endTime - $startTime).TotalSeconds

            if ($codexExitCode -eq 0) {
                Add-Content ai/system-metrics.md "$(Get-Date) | task-run | $duration sec | success"

                if (Test-Path "scripts/run-integration-tests.ps1") {
                    Write-Host "Running integration tests..."
                    powershell -ExecutionPolicy Bypass -File scripts/run-integration-tests.ps1
                }
                else {
                    Write-Host "No integration tests configured."
                }

                $gitChanges = git status --porcelain
                if ($gitChanges) {
                    Write-Host "Changes detected. Committing and pushing..."
                    git add -A
                    git commit -m "chore: process pending tasks"
                    if ($LASTEXITCODE -eq 0) {
                        git push
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
                Write-Host "Codex run failed with exit code $codexExitCode. Continuing next cycle."
                Add-Content ai/system-metrics.md "$(Get-Date) | task-run | $duration sec | failed($codexExitCode)"
            }
        }
        finally {
            Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
        }
    }
    else {
        Write-Host "No pending tasks found."
	}

    Start-Sleep -Seconds $pollIntervalSeconds
}
