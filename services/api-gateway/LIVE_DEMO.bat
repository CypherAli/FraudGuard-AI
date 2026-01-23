@echo off
cls
echo ========================================
echo   FRAUDGUARD AI - LIVE DEMONSTRATION
echo ========================================
echo.

echo [STEP 1] Verifying Database...
echo ----------------------------------------
echo Tables in database:
docker exec fraudguard-db psql -U fraudguard -d fraudguard_db -c "SELECT table_name FROM information_schema.tables WHERE table_schema='public';" -A -t
echo.

echo Sample Users:
docker exec fraudguard-db psql -U fraudguard -d fraudguard_db -c "SELECT device_id FROM users;" -A -t
echo.

echo Blacklisted Numbers:
docker exec fraudguard-db psql -U fraudguard -d fraudguard_db -c "SELECT phone_number, risk_level FROM blacklists;" -A -t
echo.

echo [STEP 2] Starting Server...
echo ----------------------------------------
echo Server will start in a new window...
echo.
start "FraudGuard Server" cmd /k "cd /d %~dp0 && go run cmd/api/main.go"

echo Waiting 3 seconds for server to start...
timeout /t 3 /nobreak >nul

echo.
echo [STEP 3] Testing Endpoints...
echo ----------------------------------------

echo.
echo Testing Health Check:
curl -s http://localhost:8080/health
echo.

echo.
echo Testing Blacklist API:
curl -s http://localhost:8080/api/blacklist
echo.

echo.
echo Testing Check Number:
curl -s "http://localhost:8080/api/check?phone=+84123456789"
echo.

echo.
echo ========================================
echo   DEMONSTRATION COMPLETE!
echo ========================================
echo.
echo Server is running in separate window.
echo.
echo To test WebSocket, run:
echo   wscat -c "ws://localhost:8080/ws?device_id=test"
echo.
echo To stop server, close the server window.
echo.
pause
