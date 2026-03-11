Write-Host "Codex worker started..."

$lockFile = "ai/worker.lock"
$maxTasks = 3
$processedTasks = 0

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
        codex --auto "Follow the workflow defined in AGENTS.md and process the pending tasks."
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds

        Add-Content ai/system-metrics.md "$(Get-Date) | task-run | $duration sec | success"
        Write-Host "Running integration tests..."

        powershell -ExecutionPolicy Bypass -File scripts/run-integration-tests.ps1
        $processedTasks++

        if ($processedTasks -ge $maxTasks) {
            Write-Host "Max tasks reached. Waiting for next cycle."
            Start-Sleep -Seconds 300
            $processedTasks = 0
        }

        Remove-Item $lockFile
    }
    else {

		Write-Host "No tasks found. Running repository health review..."

		codex --auto "Review the repository using ai/orchestrator/repo-reviewer.md and create improvement tasks if needed."

	}

    Start-Sleep -Seconds 30
}
