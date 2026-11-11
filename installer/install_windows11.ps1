# ==============================================================================
# Time Recorder 2.0 - Windows 11 Automated Installer
# Installs application, Start Menu & Desktop shortcuts, and registers with Windows 11
# ==============================================================================

[CmdletBinding()]
param(
    [string]$InstallPath = "$env:LOCALAPPDATA\Programs\TimeRecorder",
    [switch]$NoDesktopShortcut
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Time Recorder 2.0 - Windows 11 Setup" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$sourceExe = Join-Path $scriptDir "..\publish\win11-x64\Timer2.App.exe"

if (-not (Test-Path $sourceExe)) {
    Write-Host "Published binary not found. Compiling release build..." -ForegroundColor Yellow
    $solutionRoot = Join-Path $scriptDir ".."
    dotnet publish "$solutionRoot\src\Timer2.App\Timer2.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "$solutionRoot\publish\win11-x64"
}

if (-not (Test-Path $sourceExe)) {
    Write-Error "Could not locate Timer2.App.exe at $sourceExe"
    exit 1
}

# 1. Create target installation directory
Write-Host "Installing to: $InstallPath" -ForegroundColor White
if (-not (Test-Path $InstallPath)) {
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# 2. Copy application executable and assets
Copy-Item -Path $sourceExe -Destination (Join-Path $InstallPath "TimeRecorder.exe") -Force
$licensePath = Join-Path $scriptDir "..\LICENSE"
if (Test-Path $licensePath) {
    Copy-Item -Path $licensePath -Destination (Join-Path $InstallPath "LICENSE") -Force
}

$targetExe = Join-Path $InstallPath "TimeRecorder.exe"

# 3. Create Windows 11 Start Menu Shortcut
$startMenuDir = [System.IO.Path]::Combine($env:APPDATA, "Microsoft\Windows\Start Menu\Programs\Time Recorder")
if (-not (Test-Path $startMenuDir)) {
    New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null
}
$startMenuLnk = Join-Path $startMenuDir "Time Recorder.lnk"

$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($startMenuLnk)
$shortcut.TargetPath = $targetExe
$shortcut.WorkingDirectory = $InstallPath
$shortcut.Description = "Time Recorder 2.0 - Precision Tracking & Logs"
$shortcut.Save()
Write-Host "Created Start Menu shortcut: $startMenuLnk" -ForegroundColor Green

# 4. Create Desktop Shortcut if not suppressed
if (-not $NoDesktopShortcut) {
    $desktopDir = [System.Environment]::GetFolderPath("Desktop")
    $desktopLnk = Join-Path $desktopDir "Time Recorder.lnk"
    $dShortcut = $wshShell.CreateShortcut($desktopLnk)
    $dShortcut.TargetPath = $targetExe
    $dShortcut.WorkingDirectory = $InstallPath
    $dShortcut.Description = "Time Recorder 2.0"
    $dShortcut.Save()
    Write-Host "Created Desktop shortcut: $desktopLnk" -ForegroundColor Green
}

# 5. Create uninstaller script in installation folder
$uninstallerScript = Join-Path $InstallPath "uninstall.ps1"
$uninstallerContent = @"
`$ErrorActionPreference = "SilentlyContinue"
Write-Host "Uninstalling Time Recorder 2.0..." -ForegroundColor Yellow
Remove-Item -Path "$startMenuLnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$([System.IO.Path]::Combine($env:APPDATA, 'Microsoft\Windows\Start Menu\Programs\Time Recorder'))" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "$([System.IO.Path]::Combine([System.Environment]::GetFolderPath('Desktop'), 'Time Recorder.lnk'))" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TimeRecorder" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Shortcuts and registry removed. Closing..." -ForegroundColor Green
"@
Set-Content -Path $uninstallerScript -Value $uninstallerContent -Encoding UTF8

# 6. Register with Windows 11 Installed Apps (Add/Remove Programs)
$uninstallRegKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\TimeRecorder"
if (-not (Test-Path $uninstallRegKey)) {
    New-Item -Path $uninstallRegKey -Force | Out-Null
}
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayName" -Value "Time Recorder 2.0"
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayVersion" -Value "2.0.0"
Set-ItemProperty -Path $uninstallRegKey -Name "Publisher" -Value "Time Recorder Project"
Set-ItemProperty -Path $uninstallRegKey -Name "InstallLocation" -Value $InstallPath
Set-ItemProperty -Path $uninstallRegKey -Name "DisplayIcon" -Value "$targetExe,0"
Set-ItemProperty -Path $uninstallRegKey -Name "UninstallString" -Value "powershell.exe -ExecutionPolicy Bypass -File `"$uninstallerScript`""
Set-ItemProperty -Path $uninstallRegKey -Name "NoModify" -Value 1
Set-ItemProperty -Path $uninstallRegKey -Name "NoRepair" -Value 1

Write-Host ""
Write-Host "SUCCESS: Time Recorder 2.0 has been installed for Windows 11!" -ForegroundColor Green
Write-Host "You can launch it from your Desktop or Start Menu." -ForegroundColor Cyan
Write-Host ""

