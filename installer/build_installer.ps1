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
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# 2. Publish self-contained Windows 11 executable
Write-Host "Publishing Windows 11 win-x64 standalone package..." -ForegroundColor White
$publishDir = "$root\publish\win11-x64"
dotnet publish "$root\src\Timer2.App\Timer2.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

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

# 4. Publish a standalone setup executable with the application embedded
$setupPublishDir = "$root\dist\setup"
$payloadPath = Join-Path $publishDir "Timer2.App.exe"
dotnet publish "$root\src\Timer2.Installer\Timer2.Installer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true "-p:InstallerPayloadPath=$payloadPath" -o $setupPublishDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Copy-Item "$setupPublishDir\Timer2.Installer.exe" "$root\dist\TimeRecorder_2.0_Setup.exe" -Force

Write-Host "Distribution bundle created at: $distDir" -ForegroundColor Green
Write-Host "Standalone setup created at: $root\dist\TimeRecorder_2.0_Setup.exe" -ForegroundColor Green
Write-Host "Done!" -ForegroundColor Cyan
