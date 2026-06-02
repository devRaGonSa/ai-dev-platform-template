[CmdletBinding()]
param (
    [string]$InstallDirectory = "D:\Tools\ai-platform",
    [switch]$UpdateUserPath
)

Write-Host "Publishing AI Platform CLI..."

dotnet publish ai-platform-cli\ai-platform-cli.csproj -c Release -o $InstallDirectory
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed."
    exit $LASTEXITCODE
}

$exePath = Join-Path $InstallDirectory "ai-platform.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Expected executable was not found: $exePath"
    exit 1
}

if ($UpdateUserPath) {
    $currentUserPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $paths = @()
    if (-not [string]::IsNullOrWhiteSpace($currentUserPath)) {
        $paths = $currentUserPath.Split(';') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    }

    $normalizedInstallDirectory = [System.IO.Path]::GetFullPath($InstallDirectory).TrimEnd('\')
    $alreadyInPath = $paths | Where-Object {
        [System.IO.Path]::GetFullPath($_).TrimEnd('\') -ieq $normalizedInstallDirectory
    }

    if (-not $alreadyInPath) {
        $newPath = "$InstallDirectory;$currentUserPath"
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Write-Host "Added $InstallDirectory to the user PATH. Open a new terminal to use it."
    }
    else {
        Write-Host "$InstallDirectory is already in the user PATH."
    }
}

Write-Host "AI Platform CLI installed at $exePath"
Write-Host "Run this executable directly or open a new terminal and run: ai-platform"
