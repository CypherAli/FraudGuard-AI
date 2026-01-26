# Quick Ngrok Test for Mobile Deployment
Write-Host "`n=== NGROK TUNNEL CHECK ===" -ForegroundColor Cyan

# Get tunnel info
$tunnels = Invoke-RestMethod -Uri "http://localhost:4040/api/tunnels" -UseBasicParsing
$publicUrl = $tunnels.tunnels[0].public_url
$domain = $publicUrl -replace "https://", ""

Write-Host "OK Ngrok URL: $publicUrl" -ForegroundColor Green
Write-Host "`n MOBILE APP CONFIGURATION:" -ForegroundColor Yellow
Write-Host "   Server URL: $domain" -ForegroundColor Cyan
Write-Host "   (Enter this in Settings tab)" -ForegroundColor Gray

Write-Host "`nReady! Now deploy the app to your phone." -ForegroundColor Green
