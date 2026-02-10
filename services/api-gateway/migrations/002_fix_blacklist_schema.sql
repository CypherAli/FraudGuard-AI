-- Migration: Fix blacklist schema (UUID → SERIAL, unify columns)
-- Safe to run: No foreign key dependencies on blacklist.id
-- WARNING: This will DROP and RECREATE the blacklist table!

-- Step 1: Backup existing data (optional, uncomment if needed)
-- CREATE TABLE blacklist_backup AS SELECT * FROM blacklist;

-- Step 2: Drop old table (with any conflicting schema)
-- CASCADE automatically removes dependent views, indexes, and constraints
-- Safe because we verified no FK references exist to blacklist.id
DROP TABLE IF EXISTS blacklists CASCADE;  -- Drop plural version if exists
DROP TABLE IF EXISTS blacklist CASCADE;   -- Drop current version

-- Step 3: Create new unified schema with SERIAL ID
-- SERIAL = auto-incrementing integer (1, 2, 3...)
-- IMPORTANT: Never manually insert ID values - let PostgreSQL manage the sequence
CREATE TABLE blacklist (
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

-- Step 4: Create indexes for performance
CREATE INDEX idx_blacklist_phone_number ON blacklist(phone_number);
CREATE INDEX idx_blacklist_confidence ON blacklist(confidence_score);
CREATE INDEX idx_blacklist_status ON blacklist(status);

-- Step 5: Create trigger for auto-update timestamp
CREATE TRIGGER update_blacklist_updated_at BEFORE UPDATE ON blacklist
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Step 6: Re-seed data (will be handled by migrate.go AutoMigrate)
-- Or insert your production data here if needed

-- Success message
DO $$
BEGIN
    RAISE NOTICE 'Blacklist schema migration completed! Old data was dropped.';
    RAISE NOTICE 'Run AutoMigrate() to re-seed fraud phone numbers.';
END $$;
