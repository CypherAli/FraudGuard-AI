-- FraudGuard AI - Initial Database Schema
-- PostgreSQL 16

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Users table: Store device registrations
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    device_id VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Blacklists table: Store reported fraudulent phone numbers
CREATE TABLE IF NOT EXISTS blacklists (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    phone_number VARCHAR(20) UNIQUE NOT NULL,
    report_count INTEGER DEFAULT 1,
    risk_level VARCHAR(20) CHECK (risk_level IN ('LOW', 'MEDIUM', 'HIGH', 'CRITICAL')) DEFAULT 'MEDIUM',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Call logs table: Store call records with AI analysis metadata
CREATE TABLE IF NOT EXISTS call_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    phone_number VARCHAR(20),
    transcript TEXT,
    duration INTEGER, -- Duration in seconds
    metadata JSONB, -- Flexible storage for AI analysis results
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes for performance
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES users(id)
);

-- Indexes for better query performance
CREATE INDEX idx_users_device_id ON users(device_id);
CREATE INDEX idx_blacklists_phone_number ON blacklists(phone_number);
CREATE INDEX idx_blacklists_risk_level ON blacklists(risk_level);
CREATE INDEX idx_call_logs_user_id ON call_logs(user_id);
CREATE INDEX idx_call_logs_phone_number ON call_logs(phone_number);
CREATE INDEX idx_call_logs_created_at ON call_logs(created_at DESC);

-- GIN index for JSONB metadata queries
CREATE INDEX idx_call_logs_metadata ON call_logs USING GIN (metadata);

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers to auto-update updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_blacklists_updated_at BEFORE UPDATE ON blacklists
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert sample data for testing
INSERT INTO users (device_id) VALUES 
    ('test-device-001'),
    ('test-device-002')
ON CONFLICT (device_id) DO NOTHING;

INSERT INTO blacklists (phone_number, report_count, risk_level) VALUES
    ('+84123456789', 15, 'HIGH'),
    ('+84987654321', 3, 'MEDIUM')
ON CONFLICT (phone_number) DO NOTHING;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'FraudGuard AI database schema initialized successfully!';
END $$;
