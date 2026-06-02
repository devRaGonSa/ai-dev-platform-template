[CmdletBinding()]
param (
    [switch]$DryRun,
    [switch]$NoBackup,
    [string]$TemplateZipUrl,
    [string]$TemplateBranch = "main"
)

Write-Host "AI Platform update started..."

$configPath = "ai-platform.json"
$statusDirectory = "ai/status"
$logsDirectory = "ai/logs"
$updateStatusPath = Join-Path $statusDirectory "latest-update.json"

function Write-UpdateStatus {
    param (
        [string]$Status,
        [string]$Mode,
        [string[]]$UpdatedFiles,
        [string]$Notes
    )

    [System.IO.Directory]::CreateDirectory($statusDirectory) | Out-Null

    $statusObject = [ordered]@{
        status = $Status
        mode = $Mode
        updatedFiles = $UpdatedFiles
        notes = $Notes
        completedAt = (Get-Date).ToString("o")
    }

    $statusObject | ConvertTo-Json -Depth 8 | Set-Content -Path $updateStatusPath -Encoding UTF8
}

function Test-IsTemplateRepository {
    if (-not (Test-Path ".git")) {
        return $false
    }

    $remoteUrl = git config --get remote.origin.url 2>$null
    if ([string]::IsNullOrWhiteSpace($remoteUrl)) {
        return $false
    }

    return $remoteUrl -match "ai-dev-platform-template"
}

function Get-RelativeManagedPath {
    param ([string]$Path)

    return $Path.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Copy-ManagedArtifact {
    param (
        [string]$SourceRoot,
        [string]$RelativePath,
        [string]$BackupRoot
    )

    $relativeSystemPath = Get-RelativeManagedPath -Path $RelativePath
    $sourcePath = Join-Path $SourceRoot $relativeSystemPath
    $destinationPath = Join-Path (Get-Location) $relativeSystemPath

    if (-not (Test-Path $sourcePath)) {
        Write-Warning "Managed artifact not found in template: $RelativePath"
        return $false
    }

    if ($DryRun) {
        Write-Host "[dry-run] Would update $RelativePath"
        return $true
    }

    if (-not $NoBackup -and (Test-Path $destinationPath)) {
        $backupPath = Join-Path $BackupRoot $relativeSystemPath
        $backupDirectory = Split-Path $backupPath -Parent
        [System.IO.Directory]::CreateDirectory($backupDirectory) | Out-Null
        Copy-Item -Path $destinationPath -Destination $backupPath -Force
    }

    $destinationDirectory = Split-Path $destinationPath -Parent
    [System.IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null
    Copy-Item -Path $sourcePath -Destination $destinationPath -Force
    Write-Host "Updated $RelativePath"
    return $true
}

[System.IO.Directory]::CreateDirectory($statusDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($logsDirectory) | Out-Null

if (-not (Test-Path $configPath)) {
    Write-UpdateStatus -Status "failed" -Mode "unknown" -UpdatedFiles @() -Notes "ai-platform.json was not found."
    Write-Error "ai-platform.json was not found. Run this command from a repository that has AI Platform installed."
    exit 2
}

if (Test-IsTemplateRepository) {
    Write-Host "Template repository detected. Using git pull mode."

    if ($DryRun) {
        git fetch --dry-run
        Write-UpdateStatus -Status "dry-run" -Mode "template-git-pull" -UpdatedFiles @() -Notes "Dry run completed for template repository."
        exit 0
    }

    git pull
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-UpdateStatus -Status "success" -Mode "template-git-pull" -UpdatedFiles @() -Notes "Template repository updated with git pull."
    }
    else {
        Write-UpdateStatus -Status "failed" -Mode "template-git-pull" -UpdatedFiles @() -Notes "git pull failed with exit code $exitCode."
    }

    exit $exitCode
}

Write-Host "Client repository detected. Updating only managed AI Platform artifacts."

try {
    $config = Get-Content $configPath -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
}
catch {
    Write-UpdateStatus -Status "failed" -Mode "managed-artifacts" -UpdatedFiles @() -Notes "Could not read ai-platform.json: $($_.Exception.Message)"
    throw
}

if ([string]::IsNullOrWhiteSpace($TemplateZipUrl)) {
    $TemplateZipUrl = $config.templateSourceZip
}

if ([string]::IsNullOrWhiteSpace($TemplateZipUrl)) {
    $TemplateZipUrl = "https://github.com/devRaGonSa/ai-dev-platform-template/archive/refs/heads/$TemplateBranch.zip"
}

$managedArtifacts = @($config.managedArtifacts)
if (-not $managedArtifacts -or $managedArtifacts.Count -eq 0) {
    Write-UpdateStatus -Status "failed" -Mode "managed-artifacts" -UpdatedFiles @() -Notes "No managedArtifacts were defined in ai-platform.json."
    Write-Error "No managedArtifacts were defined in ai-platform.json."
    exit 3
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ai-platform-update-" + [Guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $tempRoot "template.zip"
$extractPath = Join-Path $tempRoot "template"
$backupRoot = Join-Path "ai/backups" (Get-Date -Format "yyyyMMdd-HHmmss")
$updatedFiles = New-Object System.Collections.Generic.List[string]

try {
    [System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
    [System.IO.Directory]::CreateDirectory($extractPath) | Out-Null

    Write-Host "Downloading template from $TemplateZipUrl"
    Invoke-WebRequest -Uri $TemplateZipUrl -OutFile $zipPath -UseBasicParsing

    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
    $templateRoot = Get-ChildItem $extractPath -Directory | Select-Object -First 1

    if ($null -eq $templateRoot) {
        throw "Template ZIP did not contain a root directory."
    }

    if ($DryRun) {
        Write-Host "Dry run enabled. No files will be changed."
    }
    elseif (-not $NoBackup) {
        [System.IO.Directory]::CreateDirectory($backupRoot) | Out-Null
        Write-Host "Backups will be stored in $backupRoot"
    }

    foreach ($artifact in $managedArtifacts) {
        if ([string]::IsNullOrWhiteSpace($artifact)) {
            continue
        }

        $updated = Copy-ManagedArtifact -SourceRoot $templateRoot.FullName -RelativePath $artifact -BackupRoot $backupRoot
        if ($updated) {
            $updatedFiles.Add($artifact) | Out-Null
        }
    }

    $mode = if ($DryRun) { "managed-artifacts-dry-run" } else { "managed-artifacts" }
    $notes = if ($DryRun) { "Dry run completed. No files were changed." } else { "Managed AI Platform artifacts updated from template ZIP." }
    Write-UpdateStatus -Status "success" -Mode $mode -UpdatedFiles $updatedFiles.ToArray() -Notes $notes
    Write-Host "AI Platform update completed."
}
catch {
    Write-UpdateStatus -Status "failed" -Mode "managed-artifacts" -UpdatedFiles $updatedFiles.ToArray() -Notes $_.Exception.Message
    Write-Error $_.Exception.Message
    exit 1
}
finally {
    Remove-Item $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}
