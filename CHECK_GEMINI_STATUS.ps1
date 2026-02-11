# Script kiểm tra trạng thái Gemini AI integration

Write-Host "=== FraudGuard AI - Gemini Status Check ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check if gemini_client.go exists
$geminiFile = "services\api-gateway\internal\services\gemini_client.go"
if (Test-Path $geminiFile) {
    Write-Host "✅ gemini_client.go exists" -ForegroundColor Green
} else {
    Write-Host "❌ gemini_client.go NOT FOUND" -ForegroundColor Red
    Write-Host "   → Run: git merge claude/zealous-perlman" -ForegroundColor Yellow
}

# 2. Check go.mod for genai dependency
$goMod = Get-Content "services\api-gateway\go.mod" -Raw
if ($goMod -match "google\.golang\.org/genai") {
    Write-Host "✅ google.golang.org/genai dependency exists" -ForegroundColor Green
} else {
    Write-Host "❌ Missing genai dependency" -ForegroundColor Red
    Write-Host "   → Run: go get google.golang.org/genai@v1.46.0" -ForegroundColor Yellow
}

# 3. Check .env for GEMINI_API_KEY
Write-Host ""
if (Test-Path "services\api-gateway\.env") {
    $envContent = Get-Content "services\api-gateway\.env" -Raw
    if ($envContent -match "GEMINI_API_KEY=.+") {
        Write-Host "✅ GEMINI_API_KEY configured in .env" -ForegroundColor Green
    } else {
        Write-Host "⚠️  GEMINI_API_KEY not set in .env" -ForegroundColor Yellow
        Write-Host "   → Add: GEMINI_API_KEY=your_key_here" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️  .env file not found" -ForegroundColor Yellow
}

# 4. Check if running on Railway/Render
Write-Host ""
Write-Host "💡 For production deployment:" -ForegroundColor Cyan
Write-Host "   Railway: railway variables set GEMINI_API_KEY=your_key" -ForegroundColor Gray
Write-Host "   Render: Add GEMINI_API_KEY in Environment Variables" -ForegroundColor Gray

# 5. Check current branch
Write-Host ""
$currentBranch = git branch --show-current
Write-Host "📍 Current branch: $currentBranch" -ForegroundColor Cyan
if ($currentBranch -eq "main") {
    Write-Host "   Good! You're on main branch" -ForegroundColor Green
} else {
    Write-Host "   Consider merging fixes to main" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== End of Check ===" -ForegroundColor Cyan
