# Publish OcrSnippingApp for Windows (single-file) and create Desktop shortcut
# Usage: Run in PowerShell (ExecutionPolicy may require Bypass)
# Example: powershell -ExecutionPolicy Bypass -File .\tools\publish_windows.ps1

param(
    [string]$ProjectPath = "OcrSnippingApp.csproj",
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$SelfContained
)

$SelfContainedFlag = if ($SelfContained) { "--self-contained true" } else { "--self-contained true" }

try {
    Write-Host "Publish: Project=$ProjectPath, Runtime=$Runtime, Configuration=$Configuration" -ForegroundColor Cyan

    $publishDir = Join-Path $env:USERPROFILE "Desktop\OcrSnippingApp"
    if (Test-Path $publishDir) {
        Write-Host "既存の出力を削除: $publishDir" -ForegroundColor Yellow
        Remove-Item $publishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishDir | Out-Null

    $publishArgs = @(
        "publish",
        "$ProjectPath",
        "-c", "$Configuration",
        "-r", "$Runtime",
        "/p:PublishSingleFile=true",
        "/p:IncludeNativeLibrariesForSelfExtract=true",
        "/p:PublishTrimmed=false",
        "/p:DebugType=none",
        "-o", "$publishDir"
    )

    if ($SelfContained) {
        $publishArgs += "/p:SelfContained=true"
    } else {
        $publishArgs += "/p:SelfContained=true"
    }

    $cmd = "dotnet " + ($publishArgs -join ' ')
    Write-Host "Running: $cmd" -ForegroundColor Gray

    $proc = Start-Process -FilePath dotnet -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        throw "dotnet publish failed with exit code $($proc.ExitCode)"
    }

    # Create shortcut on Desktop
    $exePath = Join-Path $publishDir "OcrSnippingApp.exe"
    if (-not (Test-Path $exePath)) {
        throw "Published exe not found: $exePath"
    }

    $desktop = [Environment]::GetFolderPath('Desktop')
    $shortcutPath = Join-Path $desktop "OcrSnippingApp.lnk"

    $WshShell = New-Object -ComObject WScript.Shell
    $shortcut = $WshShell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $publishDir
    $shortcut.IconLocation = $exePath
    $shortcut.Save()

    Write-Host "Publish succeeded. Files placed in: $publishDir" -ForegroundColor Green
    Write-Host "Shortcut created on Desktop: $shortcutPath" -ForegroundColor Green
    exit 0
}
catch {
    Write-Error "Publish failed: $_"
    exit 1
}
