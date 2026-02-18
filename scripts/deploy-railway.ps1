# deploy-railway.ps1 - Deploy FraudGuard AI to Railway

Write-Host "Deploying FraudGuard AI to Railway..." -ForegroundColor Cyan

$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot

Write-Host "`nChecking git status..." -ForegroundColor Yellow
git status

Write-Host "`nAdding files..." -ForegroundColor Yellow
git add .

Write-Host "`nCommitting changes..." -ForegroundColor Yellow
git commit -m "Deploy: update Railway configuration"

Write-Host "`nPushing to GitHub..." -ForegroundColor Yellow
git push origin main

Write-Host "`nCode pushed to GitHub!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Go to Railway Dashboard"
Write-Host "2. Root Directory: services/api-gateway"
Write-Host "3. Add environment variables (DEEPGRAM_API_KEY, etc.)"
