package db

import (
	"context"
	"fmt"
	"log"
)

// AutoMigrate creates tables and seeds initial data.
// All statements are executed one-by-one — pgx v5 Pool.Exec does NOT support
// multi-statement queries (uses extended query protocol).
func AutoMigrate() error {
	if Pool == nil {
		return fmt.Errorf("database pool is nil, cannot run migrations")
	}

	log.Println("🔄 Running database migrations...")

	ctx := context.Background()

	// ── Step 1: Drop legacy table (UUID-based schema) ──────────────────────
	var oldTableExists bool
	_ = Pool.QueryRow(ctx, `
		SELECT EXISTS (SELECT FROM pg_tables WHERE tablename = 'blacklists')
	`).Scan(&oldTableExists)

	if oldTableExists {
		log.Println("⚠️  Detected old 'blacklists' table — dropping to migrate to unified schema...")
		if _, err := Pool.Exec(ctx, "DROP TABLE IF EXISTS blacklists CASCADE"); err != nil {
			log.Printf("❌ Failed to drop old table: %v", err)
		} else {
			log.Println("✅ Old table dropped")
		}
	}

	// ── Step 2: Create blacklist table (idempotent) ────────────────────────
	// Run each statement individually — pgx v5 does not support multi-statement Exec
	ddlStatements := []string{
		`CREATE TABLE IF NOT EXISTS blacklist (
			id               SERIAL PRIMARY KEY,
			phone_number     VARCHAR(20) UNIQUE NOT NULL CHECK (phone_number ~ '^[+0-9]+$'),
			reason           TEXT NOT NULL,
			confidence_score DECIMAL(3,2) DEFAULT 0.50 CHECK (confidence_score BETWEEN 0 AND 1),
			reported_count   INTEGER DEFAULT 1 CHECK (reported_count >= 0),
			first_reported_at TIMESTAMP DEFAULT NOW(),
			last_reported_at  TIMESTAMP DEFAULT NOW(),
			status           VARCHAR(20) DEFAULT 'active',
			created_at       TIMESTAMP DEFAULT NOW(),
			updated_at       TIMESTAMP DEFAULT NOW()
		)`,
		`CREATE INDEX IF NOT EXISTS idx_phone_number ON blacklist(phone_number)`,
		`CREATE INDEX IF NOT EXISTS idx_confidence   ON blacklist(confidence_score)`,
		`CREATE INDEX IF NOT EXISTS idx_status       ON blacklist(status)`,
		// call_logs stored in PostgreSQL (not SQLite) so data persists across deploys
		`CREATE TABLE IF NOT EXISTS call_logs (
			id             SERIAL PRIMARY KEY,
			device_id      VARCHAR(255) NOT NULL,
			start_time     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
			end_time       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
			duration       BIGINT NOT NULL DEFAULT 0,
			risk_score     INT NOT NULL DEFAULT 0,
			deepfake_score INT NOT NULL DEFAULT 0,
			is_fraud       BOOLEAN NOT NULL DEFAULT FALSE,
			evidence       TEXT,
			transcript     TEXT,
			created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
		)`,
		`CREATE INDEX IF NOT EXISTS idx_call_logs_device_id  ON call_logs(device_id)`,
		`CREATE INDEX IF NOT EXISTS idx_call_logs_created_at ON call_logs(created_at DESC)`,
		`CREATE INDEX IF NOT EXISTS idx_call_logs_is_fraud   ON call_logs(is_fraud)`,
		// sessions: persists auth tokens across server restarts
		`CREATE TABLE IF NOT EXISTS sessions (
			token      VARCHAR(64)  PRIMARY KEY,
			email      VARCHAR(255) NOT NULL,
			user_id    VARCHAR(64)  NOT NULL,
			device_id  VARCHAR(64)  NOT NULL DEFAULT '',
			created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
			expires_at TIMESTAMPTZ  NOT NULL
		)`,
		`CREATE INDEX IF NOT EXISTS idx_sessions_email      ON sessions(email)`,
		`CREATE INDEX IF NOT EXISTS idx_sessions_expires_at ON sessions(expires_at)`,
		// rate_limits: persists OTP send/verify rate counters across server restarts.
		// Without this, Render.com free-tier daily restarts silently reset all limits.
		`CREATE TABLE IF NOT EXISTS rate_limits (
			key        TEXT         PRIMARY KEY,
			count      INT          NOT NULL DEFAULT 1,
			first_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
			expires_at TIMESTAMPTZ  NOT NULL
		)`,
		`CREATE INDEX IF NOT EXISTS idx_rate_limits_expires_at ON rate_limits(expires_at)`,
	}

	for _, stmt := range ddlStatements {
		if _, err := Pool.Exec(ctx, stmt); err != nil {
			log.Printf("⚠️  DDL warning (may already exist): %v", err)
		}
	}
	log.Println("✅ Base schema applied")

	// ── Step 3: Add any missing columns (idempotent ALTER TABLE) ───────────
	// Needed when table existed from an older schema without these columns.
	alterStatements := []string{
		// sessions: add device_id for token-device binding (backward-compat default '')
		`ALTER TABLE sessions ADD COLUMN IF NOT EXISTS device_id VARCHAR(64) NOT NULL DEFAULT ''`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS status            VARCHAR(20)    DEFAULT 'active'`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS first_reported_at TIMESTAMP      DEFAULT NOW()`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS last_reported_at  TIMESTAMP      DEFAULT NOW()`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS created_at        TIMESTAMP      DEFAULT NOW()`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS updated_at        TIMESTAMP      DEFAULT NOW()`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS reported_count    INTEGER        DEFAULT 1`,
		`ALTER TABLE blacklist ADD COLUMN IF NOT EXISTS confidence_score  DECIMAL(3,2)   DEFAULT 0.50`,
		`CREATE INDEX IF NOT EXISTS idx_status ON blacklist(status)`,
	}

	for _, stmt := range alterStatements {
		if _, err := Pool.Exec(ctx, stmt); err != nil {
			log.Printf("⚠️  Alter warning: %v", err)
		}
	}
	log.Println("✅ Schema columns verified/added")

	// ── Step 4: Seed initial data (only if empty) ──────────────────────────
	var count int
	if err := Pool.QueryRow(ctx, "SELECT COUNT(*) FROM blacklist").Scan(&count); err != nil {
		return err
	}

	if count > 0 {
		log.Printf("ℹ️  Database already has %d records, skipping seed", count)
		return nil
	}

	log.Println("📦 Seeding fraud blacklist data...")

	// Each INSERT is its own Exec call (pgx v5 multi-statement restriction)
	seedRows := []struct {
		phone, reason string
		score         float64
		cnt           int
	}{
		// International (Wangiri / impersonation)
		{"+22375260052", "Lua dao quoc te (Mali)", 0.95, 100},
		{"+22382271520", "Lua dao quoc te (Mali)", 0.95, 100},
		{"+22379262886", "Lua dao quoc te", 0.95, 100},
		{"+8919008198", "Lua dao quoc te", 0.95, 100},
		{"+4422222202", "Lua dao tu Anh", 0.95, 100},
		{"+2240000000", "Guinea - Wangiri", 0.75, 50},
		{"+2310000000", "Liberia - Wangiri", 0.75, 50},
		{"+2320000000", "Sierra Leone - Wangiri", 0.75, 50},
		{"+2520000000", "Somalia - Wangiri", 0.75, 50},
		{"+2470000000", "Ascension - Wangiri", 0.75, 50},
		{"+3710000000", "Latvia - Wangiri", 0.75, 50},
		// Hanoi 024
		{"02499950060", "Gia danh Cong an HN", 0.95, 100},
		{"02499954266", "Gia danh vien thong", 0.95, 100},
		{"0249997041", "Spam chung khoan", 0.75, 50},
		{"02444508888", "Robocall dau tu", 0.75, 50},
		{"02499950412", "Gia danh Cuc Thue", 0.95, 100},
		{"02439446395", "Gia danh Toa an", 0.95, 100},
		// HCMC 028
		{"02899964439", "Gia danh Cong an HCM", 0.95, 100},
		{"02856786501", "Gia danh EVN", 0.95, 100},
		{"02899964438", "Lua dao tuyen dung", 0.95, 100},
		{"02899964437", "Gia danh buu cuc", 0.95, 100},
		{"02873034653", "Spam vay tin chap", 0.75, 50},
		{"02899950012", "Spam ban dat", 0.75, 50},
		{"02873065555", "Telesale ky nghi", 0.75, 50},
		{"02899964448", "Gia danh Shopee", 0.95, 100},
		{"02822000266", "Spam khoa hoc lua dao", 0.75, 50},
		{"0287108690", "Gia danh ngan hang", 0.95, 100},
		{"02899950015", "Spam lien tuc", 0.75, 50},
		{"02899958588", "Lua dao trung thuong", 0.95, 100},
		{"02871099082", "Spam Forex", 0.75, 50},
		{"02899996142", "Gia danh Bo Y te", 0.95, 100},
		// 1900 numbers
		{"19006600", "Spam dich vu chuyen tien", 0.75, 50},
		{"19001559", "Spam bao hiem", 0.75, 50},
		{"19002008", "Telesale bat dong san", 0.75, 50},
		{"19009999", "Spam quang cao", 0.75, 50},
		{"19008198", "Spam tong dai gia mao", 0.75, 50},
		{"19001900", "Spam khao sat lua dao", 0.75, 50},
		{"19003000", "Spam the tin dung", 0.75, 50},
		{"19005555", "Robocall quang cao", 0.75, 50},
		{"19007777", "Spam dau tu chung khoan", 0.75, 50},
		{"19008888", "Telesale bat dong san", 0.75, 50},
		{"19001234", "Spam tong hop", 0.75, 50},
	}

	seeded := 0
	for _, r := range seedRows {
		_, err := Pool.Exec(ctx,
			`INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status)
			 VALUES ($1, $2, $3, $4, 'active')
			 ON CONFLICT (phone_number) DO NOTHING`,
			r.phone, r.reason, r.score, r.cnt,
		)
		if err != nil {
			log.Printf("⚠️  Seed warning for %s: %v", r.phone, err)
		} else {
			seeded++
		}
	}

	log.Printf("✅ Seeded %d fraud phone numbers successfully!", seeded)
	return nil
}
