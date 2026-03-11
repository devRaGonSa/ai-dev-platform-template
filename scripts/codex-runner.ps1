Write-Host "Codex worker started..."

$lockFile = "ai/worker.lock"

while ($true) {

    if (Test-Path $lockFile) {
        Write-Host "Worker already running. Waiting..."
        Start-Sleep -Seconds 30
        continue
    }

    Write-Host "Checking repository state..."

    git pull

    $tasks = Get-ChildItem "ai/tasks/pending" -Filter *.md -ErrorAction SilentlyContinue

    if ($tasks) {

        Write-Host "Tasks detected. Starting Codex..."

        New-Item $lockFile -ItemType File | Out-Null

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

        Remove-Item $lockFile
    }
    else {
        Write-Host "No pending tasks found."
	}

    Start-Sleep -Seconds 30
}
