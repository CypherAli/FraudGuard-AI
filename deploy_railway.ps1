# Railway Deployment Script
Write-Host "🚂 Deploying FraudGuard AI to Railway..." -ForegroundColor Cyan

# Navigate to project root
cd E:\FraudGuard-AI

# Check git status
Write-Host "`n📝 Checking git status..." -ForegroundColor Yellow
git status

# Add all changes
Write-Host "`n➕ Adding files..." -ForegroundColor Yellow
git add .

# Commit
Write-Host "`n💾 Committing changes..." -ForegroundColor Yellow
git commit -m "Add Railway deployment configuration"

# Push to GitHub
Write-Host "`n🚀 Pushing to GitHub..." -ForegroundColor Yellow
git push origin UImobile

Write-Host "`n✅ Code pushed to GitHub!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Go to Railway Dashboard: https://railway.com/project/7d19619a-5951-411e-b69e-bfda6de6fec5"
Write-Host "2. Click '+ New' → 'GitHub Repo'"
Write-Host "3. Select 'CypherAli/FraudGuard-AI'"
Write-Host "4. Root Directory: services/api-gateway"
Write-Host "5. Add environment variables (DEEPGRAM_API_KEY, etc.)"
