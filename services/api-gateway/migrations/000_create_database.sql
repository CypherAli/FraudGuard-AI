-- FraudGuard AI Database Setup Script
-- Run this script as postgres superuser

-- 1. Create user if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_user WHERE usename = 'fraudguard') THEN
        CREATE USER fraudguard WITH PASSWORD 'fraudguard_secure_2024';
    END IF;
END
$$;

-- 2. Create database if not exists
SELECT 'CREATE DATABASE fraudguard_db OWNER fraudguard'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'fraudguard_db')\gexec

-- 3. Connect to the database
\c fraudguard_db

-- 4. Grant privileges
GRANT ALL PRIVILEGES ON DATABASE fraudguard_db TO fraudguard;
GRANT ALL ON SCHEMA public TO fraudguard;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO fraudguard;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO fraudguard;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO fraudguard;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO fraudguard;

-- 5. Create initial tables
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

-- 6. Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_blacklist_phone ON blacklist(phone_number);
CREATE INDEX IF NOT EXISTS idx_call_logs_device ON call_logs(device_id);
CREATE INDEX IF NOT EXISTS idx_call_logs_phone ON call_logs(phone_number);
CREATE INDEX IF NOT EXISTS idx_call_logs_timestamp ON call_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_fraud_reports_phone ON fraud_reports(phone_number);

-- 7. Insert sample data
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count)
VALUES 
    ('0123456789', 'Lừa đảo vay tiền', 0.95, 15),
    ('0987654321', 'Giả mạo ngân hàng', 0.98, 23),
    ('0369852147', 'Lừa đảo đầu tư', 0.92, 8)
ON CONFLICT (phone_number) DO NOTHING;

-- 8. Show summary
SELECT 'Database setup completed!' AS status;
SELECT COUNT(*) AS blacklist_count FROM blacklist;
SELECT COUNT(*) AS call_logs_count FROM call_logs;
SELECT COUNT(*) AS fraud_reports_count FROM fraud_reports;
