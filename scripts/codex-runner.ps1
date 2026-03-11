Write-Host "Codex worker started..."

$lockFile = "ai/worker.lock"

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
            Start-Sleep -Seconds 30
            continue
        }

        Write-Host "Stale worker lock detected. Removing it."
        Remove-Item $lockFile -Force -ErrorAction SilentlyContinue
    }

    Write-Host "Checking repository state..."

    git pull

    $tasks = Get-ChildItem "ai/tasks/pending" -Filter *.md -ErrorAction SilentlyContinue

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

    Start-Sleep -Seconds 30
}
