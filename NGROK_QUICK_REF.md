# ⚡ NGROK QUICK REFERENCE
# Tóm tắt nhanh các lệnh thường dùng

## 🚀 QUICK START

```powershell
# 1. Start Backend
cd E:\FraudGuard-AI\services\api-gateway
go run cmd/api/main.go

# 2. Start Ngrok (Terminal mới)
ngrok http 8080

# 3. Copy URL từ output: https://xyz.ngrok-free.app
# 4. Update Mobile App Settings: xyz.ngrok-free.app
# 5. Test: .\test_ngrok.ps1
```

---

## 📦 INSTALLATION

```powershell
# Via Chocolatey:
choco install ngrok -y

# Manual:
# Download: https://ngrok.com/download
# Extract to: C:\Program Files\ngrok\
# Add to PATH
```

---

## 🔑 AUTHENTICATION

```powershell
# Get token: https://dashboard.ngrok.com/get-started/your-authtoken
ngrok config add-authtoken YOUR_TOKEN_HERE

# Verify:
ngrok config check
```

---

## 🌐 COMMON COMMANDS

```powershell
# Basic tunnel:
ngrok http 8080

# With region:
ngrok http 8080 --region=ap  # Asia Pacific

# With custom subdomain (paid):
ngrok http 8080 --subdomain=fraudguard

# With basic auth:
ngrok http 8080 --auth="user:pass"

# Multiple tunnels (ngrok.yml):
ngrok start --all
```

---

## 🔍 MONITORING

```powershell
# Web dashboard:
http://localhost:4040

# API endpoint:
curl http://localhost:4040/api/tunnels

# PowerShell:
Invoke-RestMethod http://localhost:4040/api/tunnels | 
    Select-Object -ExpandProperty tunnels | 
    Format-Table proto,public_url
```

---

## 🛠️ TROUBLESHOOTING

```powershell
# Check if ngrok is running:
Get-Process ngrok

# Kill all ngrok processes:
Get-Process ngrok | Stop-Process -Force

# Check port 8080:
netstat -ano | findstr :8080

# Test local backend:
curl http://localhost:8080/health

# Test public URL:
curl https://your-url.ngrok-free.app/health
```

---

## 📱 MOBILE APP CONFIG

```
❌ WRONG:
   Server IP: https://abc-123.ngrok-free.app/ws
   Server IP: wss://abc-123.ngrok-free.app
   Server IP: http://abc-123.ngrok-free.app:8080

✅ CORRECT:
   Server IP: abc-123.ngrok-free.app
   Port: 443 (or leave empty)
```

---

## 🔄 RESTART WORKFLOW

```powershell
# When ngrok URL changes:

# 1. Stop current tunnel:
Ctrl+C

# 2. Start new tunnel:
ngrok http 8080

# 3. Get new URL (wait 3 seconds)
# 4. Update Mobile App Settings
# 5. Save & Reconnect
```

---

## 📊 VERIFY SETUP

```powershell
# Run automated test:
cd E:\FraudGuard-AI\services\api-gateway
.\test_ngrok.ps1

# Manual test:
curl $(Invoke-RestMethod http://localhost:4040/api/tunnels).tunnels[0].public_url/health
```

---

## 🎯 PRODUCTION ALTERNATIVES

```powershell
# Instead of ngrok, use:

# Railway.app (free tier):
railway login
railway init
railway up

# Fly.io (free tier):
fly auth login
fly launch
fly deploy

# Heroku (free tier removed):
heroku create fraudguard-api
git push heroku main
```

---

## 📚 USEFUL LINKS

- Dashboard: https://dashboard.ngrok.com
- Docs: https://ngrok.com/docs
- Status: https://status.ngrok.com
- Pricing: https://ngrok.com/pricing

---

## 🆘 EMERGENCY COMMANDS

```powershell
# App can't connect? Run this:
.\test_ngrok.ps1

# Reset everything:
Get-Process ngrok -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item $env:USERPROFILE\.ngrok2\ngrok.yml -Force -ErrorAction SilentlyContinue
ngrok config add-authtoken YOUR_TOKEN
ngrok http 8080

# Check logs:
Get-Content $env:USERPROFILE\.ngrok2\ngrok.log -Tail 50
```

---

## ⏱️ FREE PLAN LIMITS

- **Session**: 2 hours max
- **Connections**: 40/minute
- **URL**: Random (changes on restart)
- **Bandwidth**: Limited
- **Tunnels**: 1 at a time

**Upgrade**: $10/month for unlimited + custom domain

---

**Created**: 2026-01-26  
**For**: FraudGuard AI Project  
**Keep this file handy during development!** 📌
