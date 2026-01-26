# Script to install .NET MAUI workload
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"

Write-Host "=== Checking .NET SDK Version ===" -ForegroundColor Cyan
& $dotnetPath --version

Write-Host "`n=== Installing .NET MAUI Workload ===" -ForegroundColor Cyan
Write-Host "This will install Android SDK, ADB, and MAUI tools..." -ForegroundColor Yellow
& $dotnetPath workload install maui

Write-Host "`n=== Verifying Installation ===" -ForegroundColor Cyan
& $dotnetPath workload list

Write-Host "`n=== Setup Complete! ===" -ForegroundColor Green
Write-Host "Please restart VS Code to use 'dotnet' command directly." -ForegroundColor Yellow
