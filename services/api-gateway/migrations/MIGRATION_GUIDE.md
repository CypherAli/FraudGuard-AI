# Database Migration Guide

## Schema Changes History

### 002_fix_blacklist_schema.sql
**Date**: 2026-02-10  
**Breaking Change**: YES ⚠️

**Changes:**
- Changed `blacklist.id` from UUID to SERIAL (auto-increment integer)
- Renamed `blacklists` → `blacklist` (singular)
- Unified columns: `report_count` → `reported_count`, `risk_level` → `confidence_score`
- Added missing columns: `first_reported_at`, `status`

**Impact:**
- ✅ **Safe for development**: No foreign key dependencies
- ⚠️ **Production**: Drops all existing blacklist data

**Migration Steps:**

#### Development/Testing:
```bash
# Run migration (will drop & recreate table)
psql -d fraudguard_db -f migrations/002_fix_blacklist_schema.sql

# Re-seed data
go run cmd/server/main.go --migrate
```

#### Production (if you have live data):
```sql
-- 1. Backup existing data
CREATE TABLE blacklist_backup AS 
SELECT phone_number, reason, 
       confidence_score, reported_count,
       created_at 
FROM blacklist;

-- 2. Run migration
\i migrations/002_fix_blacklist_schema.sql

-- 3. Restore data with new schema
INSERT INTO blacklist 
  (phone_number, reason, confidence_score, reported_count, 
   first_reported_at, last_reported_at, status)
SELECT 
  phone_number, reason, confidence_score, reported_count,
  created_at, NOW(), 'active'
FROM blacklist_backup
ON CONFLICT (phone_number) DO NOTHING;

-- 4. Verify and drop backup
SELECT COUNT(*) FROM blacklist;
DROP TABLE blacklist_backup;
```

## Migration Safety Checklist

Before running ANY migration:
- [ ] Backup database: `pg_dump fraudguard_db > backup_$(date +%Y%m%d).sql`
- [ ] Test migration on staging/dev first
- [ ] Check for foreign key dependencies
- [ ] Verify data can be re-seeded or backed up
- [ ] Plan rollback strategy

## Rollback Strategy

If migration fails:
```bash
# Restore from backup
psql -d fraudguard_db < backup_YYYYMMDD.sql
```

## Future Migrations

When adding new migrations:
1. Name files: `00X_description.sql` (sequential numbering)
2. Document breaking changes in this file
3. Test on dev/staging first
4. Always provide rollback instructions
