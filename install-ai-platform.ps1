param(
    [string]$RepositoryRoot = (Get-Location).Path,
    [switch]$Force,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$templateSourceRoot = $PSScriptRoot
$templateSourceRequiredPaths = @(
    "ai-platform.json",
    "AGENTS.md",
    "scripts/codex-runner.ps1",
    ".github/workflows/codex-worker.yml"
)
$hasTemplateSourceCheckout = $true
foreach ($requiredPath in $templateSourceRequiredPaths) {
    $candidatePath = Join-Path $templateSourceRoot $requiredPath
    if (-not (Test-Path -Path $candidatePath)) {
        $hasTemplateSourceCheckout = $false
        break
    }
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-Path -Path $Path)) {
        if ($DryRun) {
            Write-Host "[DryRun] Create directory: $Path"
            return
        }

        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-Host "Created directory: $Path"
    }
}

function Write-TemplateFile {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$Content,
        [switch]$OnlyIfMissing
    )

    $targetPath = Join-Path $RepositoryRoot $RelativePath
    $targetDir = Split-Path -Parent $targetPath
    Ensure-Directory -Path $targetDir

    $exists = Test-Path -Path $targetPath
    if ($exists -and $OnlyIfMissing -and -not $Force) {
        if ($DryRun) {
            Write-Host "[DryRun] Skip (exists): $RelativePath"
            return
        }

        Write-Host "Skipped (exists): $RelativePath"
        return
    }

    if ($exists -and -not $Force) {
        if ($DryRun) {
            Write-Host "[DryRun] Skip (exists, use -Force to overwrite): $RelativePath"
            return
        }

        Write-Host "Skipped (exists, use -Force to overwrite): $RelativePath"
        return
    }

    if ($DryRun) {
        Write-Host "[DryRun] Install: $RelativePath"
        return
    }

    Set-Content -Path $targetPath -Value $Content -Encoding UTF8
    Write-Host "Installed: $RelativePath"
}

function Write-TemplateFilePreferSource {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [Parameter(Mandatory = $true)][string]$FallbackContent,
        [switch]$OnlyIfMissing
    )

    $sourcePath = Join-Path $templateSourceRoot $RelativePath
    $sourceLabel = "embedded fallback"
    $contentToWrite = $FallbackContent

    if ($hasTemplateSourceCheckout -and (Test-Path -Path $sourcePath -PathType Leaf)) {
        $contentToWrite = Get-Content -Path $sourcePath -Raw -ErrorAction Stop
        $sourceLabel = "template source checkout"
    }

    $targetPath = Join-Path $RepositoryRoot $RelativePath
    $targetDir = Split-Path -Parent $targetPath
    Ensure-Directory -Path $targetDir

    $exists = Test-Path -Path $targetPath
    if ($exists -and $OnlyIfMissing -and -not $Force) {
        if ($DryRun) {
            Write-Host "[DryRun] Skip (exists): $RelativePath"
            return
        }

        Write-Host "Skipped (exists): $RelativePath"
        return
    }

    if ($exists -and -not $Force) {
        if ($DryRun) {
            Write-Host "[DryRun] Skip (exists, use -Force to overwrite): $RelativePath"
            return
        }

        Write-Host "Skipped (exists, use -Force to overwrite): $RelativePath"
        return
    }

    if ($DryRun) {
        Write-Host "[DryRun] Install from $sourceLabel: $RelativePath"
        return
    }

    Set-Content -Path $targetPath -Value $contentToWrite -Encoding UTF8
    Write-Host "Installed from $sourceLabel: $RelativePath"
}

$directories = @(
    "ai",
    "ai/prompts",
    "ai/tasks",
    "ai/tasks/pending",
    "ai/tasks/in-progress",
    "ai/tasks/done",
    "ai/orchestrator",
    "scripts",
    ".github",
    ".github/workflows"
)

foreach ($dir in $directories) {
    Ensure-Directory -Path (Join-Path $RepositoryRoot $dir)
}

$templateFiles = @{
    "ai-platform.json" = @'
{
  "platformVersion": "1.0",
  "requiredTemplatePaths": [
    "ai",
    "scripts",
    "AGENTS.md",
    "ai-platform.json"
  ],
  "taskPaths": {
    "pending": "ai/tasks/pending",
    "inProgress": "ai/tasks/in-progress",
    "done": "ai/tasks/done"
  },
  "worker": {
    "lockFile": "ai/worker.lock",
    "pollIntervalSeconds": 30
  }
}
'@
    "ai/task-template.md" = @'
# TASK-XXX

## Goal
Clear description of the objective.

## Context
Explain where in the project the change happens.

## Steps

1. Step one
2. Step two
3. Step three

## Files to Read First

List the most relevant files that should be inspected before implementing the task.

## Expected Files to Modify

List the files that are expected to change during this task.

## Constraints

- Do not modify unrelated files
- Keep the change minimal
- Prefer small commits

## Validation

Before completing the task ensure:

- build succeeds
- tests succeed
- no new warnings introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Split the work into additional tasks if limits are exceeded.
'@
    "ai/repo-context.md" = @'
# Repository Context

Describe the project architecture, core layers, and technical constraints used for implementation planning.
'@
    "ai/system-metrics.md" = @'
# System Metrics

This file tracks execution metrics of the AI development pipeline.

## Metrics

- Total tasks executed
- Average task duration
- Failed tasks
- Tasks generated by repository review

## Task History

Date | Task | Duration | Result
'@
    "ai/architecture-index.md" = @'
# Architecture Index

This file provides a quick overview of the repository architecture.

AI agents should read this file before scanning the repository.
'@
    "ai/orchestrator/feature-planner.md" = @'
# Feature Planner

## Purpose
Turn feature requests into implementation-ready plans and executable tasks.

## Planning Workflow
1. Read `ai/architecture-index.md`.
2. Discover relevant components and dependencies.
3. Produce a concise implementation plan.
4. Generate task files in `ai/tasks/pending`.

## Task Generation Rules
- Use `ai/task-template.md`.
- Include `## Files to Read First` and `## Expected Files to Modify` in every task.
- Keep tasks small and focused.
'@
    "ai/orchestrator/pr-generator.md" = @'
# Pull Request Generator

## PR Title
Feature: <short description>

## PR Description
### Summary
Describe the implemented feature.

### Tasks implemented
List the completed tasks.

### Validation
- build succeeded
- tests succeeded
'@
    "ai/orchestrator/repo-reviewer.md" = @'
# Repository Reviewer

Review repository health and create improvement tasks when needed.
'@
    "ai/orchestrator/planning-memory.md" = @'
# Planning Memory

Store lessons learned from previous tasks and use them during planning.
'@
    "ai/orchestrator/component-discovery.md" = @'
# Component Discovery

Identify controllers, services, models, data components, and tests related to a feature.
'@
    "ai/orchestrator/di-analysis.md" = @'
# Dependency Injection Analysis

Analyze service registrations and dependency relationships to avoid duplicate services.
'@
    "scripts/codex-runner.ps1" = @'
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
'@
    ".github/workflows/codex-worker.yml" = @'
name: Codex Task Automation (Direct CI)

on:
  push:
    paths:
      - 'ai/tasks/pending/**'

permissions:
  contents: write

jobs:
  run-codex-direct:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
      - name: Install Codex CLI
        run: npm install -g @openai/codex
      - name: Run Codex Directly In CI
        run: codex --auto "Follow the workflow defined in AGENTS.md and process the pending tasks."
      - name: Push Automated Changes
        run: |
          git config user.name "codex-worker"
          git config user.email "codex@automation.local"
          git add .
          git commit -m "chore: automated task execution" || echo "No changes"
          git push
'@
}

foreach ($path in @("ai/tasks/pending/.gitkeep", "ai/tasks/in-progress/.gitkeep", "ai/tasks/done/.gitkeep")) {
    Write-TemplateFile -RelativePath $path -Content "" -OnlyIfMissing
}

foreach ($entry in $templateFiles.GetEnumerator()) {
    # AGENTS.md is handled separately and only created when missing.
    if ($entry.Key -eq "AGENTS.md") { continue }

    if ($entry.Key -in @("ai-platform.json", "scripts/codex-runner.ps1", ".github/workflows/codex-worker.yml")) {
        Write-TemplateFilePreferSource -RelativePath $entry.Key -FallbackContent $entry.Value -OnlyIfMissing
        continue
    }

    Write-TemplateFile -RelativePath $entry.Key -Content $entry.Value -OnlyIfMissing
}

$agentsTemplate = @'
# Codex Repository Agent Rules

This repository uses an AI-driven task workflow.

## Task locations

Pending tasks: ai/tasks/pending
In-progress tasks: ai/tasks/in-progress
Done tasks: ai/tasks/done

## Workflow

1. Process the first task in `ai/tasks/pending`.
2. Move it to `ai/tasks/in-progress`.
3. Implement the task.
4. Validate build and tests.
5. Commit changes.
6. Move task to `ai/tasks/done`.
'@

Write-TemplateFilePreferSource -RelativePath "AGENTS.md" -FallbackContent $agentsTemplate -OnlyIfMissing

if ($DryRun) {
    Write-Host "[DryRun] AI platform seed installation preview completed."
}
else {
    if ($hasTemplateSourceCheckout) {
        Write-Host "AI platform installation completed using repository files when available, with embedded fallback for the rest."
    }
    else {
        Write-Host "AI platform installation completed using embedded fallback content."
    }
}
