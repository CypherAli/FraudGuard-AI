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

-- Blacklist table: Store reported fraudulent phone numbers (unified schema)
-- SERIAL = auto-incrementing integer (never manually insert ID values)
CREATE TABLE IF NOT EXISTS blacklist (
    id SERIAL PRIMARY KEY,
    phone_number VARCHAR(20) UNIQUE NOT NULL CHECK (phone_number ~ '^[+0-9]+$'),
    reason TEXT NOT NULL,
    confidence_score DECIMAL(3,2) DEFAULT 0.50 CHECK (confidence_score BETWEEN 0 AND 1),
    reported_count INTEGER DEFAULT 1 CHECK (reported_count >= 0),
    first_reported_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_reported_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
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
CREATE INDEX idx_blacklist_phone_number ON blacklist(phone_number);
CREATE INDEX idx_blacklist_confidence ON blacklist(confidence_score);
CREATE INDEX idx_blacklist_status ON blacklist(status);
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

CREATE TRIGGER update_blacklist_updated_at BEFORE UPDATE ON blacklist
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert sample data for testing
INSERT INTO users (device_id) VALUES 
    ('test-device-001'),
    ('test-device-002')
ON CONFLICT (device_id) DO NOTHING;

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
    ('+84123456789', 'Reported scam caller', 0.95, 15, 'active'),
    ('+84987654321', 'Suspicious activity', 0.70, 3, 'active')
ON CONFLICT (phone_number) DO NOTHING;

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'FraudGuard AI database schema initialized successfully!';
END $$;
