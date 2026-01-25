# Quick Database Setup Commands
# Copy and paste these commands one by one into PowerShell (Run as Administrator)

# Step 1: Set your postgres password (replace with your actual password)
$env:PGPASSWORD = "YOUR_POSTGRES_PASSWORD_HERE"

# Step 2: Create user
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres -h localhost -p 5433 -c "CREATE USER fraudguard WITH PASSWORD 'fraudguard_secure_2024';"

# Step 3: Create database
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres -h localhost -p 5433 -c "CREATE DATABASE fraudguard_db OWNER fraudguard;"

# Step 4: Grant privileges
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres -h localhost -p 5433 -d fraudguard_db -c "GRANT ALL ON SCHEMA public TO fraudguard; ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO fraudguard;"

# Step 5: Create tables
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U fraudguard -h localhost -p 5433 -d fraudguard_db -c @"
CREATE TABLE IF NOT EXISTS blacklist (
    id SERIAL PRIMARY KEY,
    phone_number VARCHAR(20) NOT NULL UNIQUE,
    reason TEXT,
    confidence_score FLOAT DEFAULT 0.0,
    reported_count INTEGER DEFAULT 1,
    first_reported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_reported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'active',
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS call_logs (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(100) NOT NULL,
    phone_number VARCHAR(20) NOT NULL,
    call_type VARCHAR(20),
    duration INTEGER,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_fraud BOOLEAN DEFAULT FALSE,
    fraud_score FLOAT DEFAULT 0.0,
    fraud_reasons TEXT[],
    audio_transcript TEXT,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS fraud_reports (
    id SERIAL PRIMARY KEY,
    phone_number VARCHAR(20) NOT NULL,
    reporter_device_id VARCHAR(100),
    report_type VARCHAR(50),
    description TEXT,
    evidence JSONB,
    status VARCHAR(20) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_blacklist_phone ON blacklist(phone_number);
CREATE INDEX IF NOT EXISTS idx_call_logs_device ON call_logs(device_id);
CREATE INDEX IF NOT EXISTS idx_call_logs_phone ON call_logs(phone_number);
CREATE INDEX IF NOT EXISTS idx_fraud_reports_phone ON fraud_reports(phone_number);

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count)
VALUES 
    ('0123456789', 'Lừa đảo vay tiền', 0.95, 15),
    ('0987654321', 'Giả mạo ngân hàng', 0.98, 23),
    ('0369852147', 'Lừa đảo đầu tư', 0.92, 8)
ON CONFLICT (phone_number) DO NOTHING;
"@

# Step 6: Verify
& "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U fraudguard -h localhost -p 5433 -d fraudguard_db -c "SELECT tablename FROM pg_tables WHERE schemaname = 'public';"

# Step 7: Clean up password
Remove-Item Env:\PGPASSWORD

Write-Host ""
Write-Host "✅ Setup complete! Now run:" -ForegroundColor Green
Write-Host "   cd E:\FraudGuard-AI\services\api-gateway" -ForegroundColor Cyan
Write-Host "   go run cmd/api/main.go" -ForegroundColor Cyan
