# ==============================================================================
# Time Recorder 2.0 - Build & Packaging Script
# ==============================================================================

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$root = Resolve-Path (Join-Path $scriptDir "..")

Write-Host "Building Time Recorder 2.0..." -ForegroundColor Cyan

# 1. Run Tests
Write-Host "Running tests..." -ForegroundColor White
dotnet test "$root\tests\Timer2.Tests\Timer2.Tests.csproj" -c Release

# 2. Publish self-contained Windows 11 executable
Write-Host "Publishing Windows 11 win-x64 standalone package..." -ForegroundColor White
$publishDir = "$root\publish\win11-x64"
dotnet publish "$root\src\Timer2.App\Timer2.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir

# 3. Create Distribution Bundle
$distDir = "$root\dist\TimeRecorder_2.0_Windows11"
if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

Copy-Item "$publishDir\Timer2.App.exe" "$distDir\TimeRecorder.exe"
Copy-Item "$root\LICENSE" "$distDir\LICENSE"
Copy-Item "$root\README.md" "$distDir\README.md"
Copy-Item "$scriptDir\install_windows11.ps1" "$distDir\Install.ps1"

Write-Host "Distribution bundle created at: $distDir" -ForegroundColor Green
Write-Host "Done!" -ForegroundColor Cyan

