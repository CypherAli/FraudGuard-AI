package repository

import (
	"context"
	"fmt"
	"log"
	"sync"
	"sync/atomic"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/jackc/pgx/v5"
)

// ── In-memory fallback store ─────────────────────────────────────────────────
// Used when PostgreSQL is unavailable (local dev / offline mode).
// InitSQLite activates this store; all repository functions fall back to it
// automatically whenever db.Pool is nil.

var (
	memMu      sync.RWMutex
	memStore   []models.CallLog   // ordered oldest-first
	memEnabled bool               // set to true by InitSQLite
	memNextID  atomic.Int64       // auto-increment surrogate key
)

// InitSQLite initialises the in-memory fallback store used when PostgreSQL is
// not available.  The dataSourceName argument is accepted for API compatibility
// but is not used — data lives only in process memory for the lifetime of the
// server process.
//
// Call this once from main() before starting the HTTP server.  It is safe to
// call even when PostgreSQL is also available; the in-memory store is only
// consulted when db.Pool is nil.
func InitSQLite(dataSourceName string) error {
	memMu.Lock()
	defer memMu.Unlock()

	if memEnabled {
		log.Println("⚠️  InitSQLite: already initialised, skipping")
		return nil
	}

	memStore = make([]models.CallLog, 0, 64)
	memEnabled = true
	log.Printf("📦 In-memory fallback store initialised (dataSource=%q ignored — using RAM)", dataSourceName)
	return nil
}

const selectCallLogCols = `
	SELECT id, device_id, start_time, end_time, duration, risk_score, deepfake_score,
	       is_fraud, COALESCE(evidence,''), COALESCE(transcript,''), created_at
	FROM call_logs`

// scanCallLogs reads pgx.Rows into a []models.CallLog slice.
func scanCallLogs(rows pgx.Rows) ([]models.CallLog, error) {
	defer rows.Close()
	logs := make([]models.CallLog, 0)
	for rows.Next() {
		var cl models.CallLog
		if err := rows.Scan(
			&cl.ID, &cl.DeviceID, &cl.StartTime, &cl.EndTime,
			&cl.Duration, &cl.RiskScore, &cl.DeepfakeScore,
			&cl.IsFraud, &cl.Evidence, &cl.Transcript, &cl.CreatedAt,
		); err != nil {
			return nil, err
		}
		logs = append(logs, cl)
	}
	return logs, rows.Err()
}

// SaveCallLog saves a call log entry to PostgreSQL, or to the in-memory store
// when PostgreSQL is unavailable and InitSQLite has been called.
func SaveCallLog(logEntry *models.CallLog) error {
	if db.Pool == nil {
		if memEnabled {
			return memSaveCallLog(logEntry)
		}
		log.Println("⚠️ Database not available, skipping call log save")
		return nil
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	err := db.Pool.QueryRow(ctx,
		`INSERT INTO call_logs
		 (device_id, start_time, end_time, duration, risk_score, deepfake_score, is_fraud, evidence, transcript, created_at)
		 VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, NOW())
		 RETURNING id`,
		logEntry.DeviceID,
		logEntry.StartTime,
		logEntry.EndTime,
		logEntry.Duration,
		logEntry.RiskScore,
		logEntry.DeepfakeScore,
		logEntry.IsFraud,
		logEntry.Evidence,
		logEntry.Transcript,
	).Scan(&logEntry.ID)

	if err != nil {
		log.Printf("❌ Error saving call log: %v", err)
		return err
	}

	log.Printf("💾 Saved Call Log [ID: %d] for Device %s (RiskScore: %d, IsFraud: %v)",
		logEntry.ID, logEntry.DeviceID, logEntry.RiskScore, logEntry.IsFraud)
	return nil
}

// GetHistory retrieves call history from PostgreSQL (or the in-memory fallback),
// ordered by most recent first.  If deviceID is empty, returns history for all devices.
func GetHistory(deviceID string, limit int) ([]models.CallLog, error) {
	if db.Pool == nil {
		if memEnabled {
			return memGetHistory(deviceID, limit, false)
		}
		return nil, fmt.Errorf("database not available")
	}
	if limit <= 0 {
		limit = 50
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var (
		rows pgx.Rows
		err  error
	)
	if deviceID != "" {
		rows, err = db.Pool.Query(ctx,
			selectCallLogCols+` WHERE device_id = $1 ORDER BY created_at DESC LIMIT $2`,
			deviceID, limit)
	} else {
		rows, err = db.Pool.Query(ctx,
			selectCallLogCols+` ORDER BY created_at DESC LIMIT $1`,
			limit)
	}
	if err != nil {
		log.Printf("❌ Error fetching call history: %v", err)
		return nil, err
	}

	logs, err := scanCallLogs(rows)
	log.Printf("📋 Retrieved %d call log(s) for device: %s", len(logs), deviceID)
	return logs, err
}

// GetAllHistory retrieves all call history (for admin/debugging).
func GetAllHistory(limit int) ([]models.CallLog, error) {
	return GetHistory("", limit)
}

// GetCallLogByID retrieves a single call log entry by ID.
func GetCallLogByID(id uint) (*models.CallLog, error) {
	if db.Pool == nil {
		if memEnabled {
			return memGetCallLogByID(id)
		}
		return nil, fmt.Errorf("database not available")
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	var cl models.CallLog
	err := db.Pool.QueryRow(ctx,
		selectCallLogCols+` WHERE id = $1`,
		id,
	).Scan(
		&cl.ID, &cl.DeviceID, &cl.StartTime, &cl.EndTime,
		&cl.Duration, &cl.RiskScore, &cl.DeepfakeScore,
		&cl.IsFraud, &cl.Evidence, &cl.Transcript, &cl.CreatedAt,
	)
	if err != nil {
		return nil, err
	}
	return &cl, nil
}

// GetFraudCallsOnly retrieves only fraudulent calls from PostgreSQL (or the in-memory fallback).
func GetFraudCallsOnly(deviceID string, limit int) ([]models.CallLog, error) {
	if db.Pool == nil {
		if memEnabled {
			return memGetHistory(deviceID, limit, true)
		}
		return nil, fmt.Errorf("database not available")
	}
	if limit <= 0 {
		limit = 50
	}

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	var (
		rows pgx.Rows
		err  error
	)
	if deviceID != "" {
		rows, err = db.Pool.Query(ctx,
			selectCallLogCols+` WHERE is_fraud = TRUE AND device_id = $1 ORDER BY created_at DESC LIMIT $2`,
			deviceID, limit)
	} else {
		rows, err = db.Pool.Query(ctx,
			selectCallLogCols+` WHERE is_fraud = TRUE ORDER BY created_at DESC LIMIT $1`,
			limit)
	}
	if err != nil {
		log.Printf("❌ Error fetching fraud calls: %v", err)
		return nil, err
	}

	logs, err := scanCallLogs(rows)
	log.Printf("🚨 Retrieved %d fraud call(s) for device: %s", len(logs), deviceID)
	return logs, err
}

// ── In-memory helper functions ────────────────────────────────────────────────

// memSaveCallLog appends a call log to the in-memory store, assigning a
// monotonically-increasing surrogate ID and setting CreatedAt to now.
func memSaveCallLog(logEntry *models.CallLog) error {
	id := memNextID.Add(1)
	logEntry.ID = uint(id)
	logEntry.CreatedAt = time.Now().UTC()

	memMu.Lock()
	memStore = append(memStore, *logEntry)
	memMu.Unlock()

	log.Printf("💾 [mem] Saved Call Log [ID: %d] for Device %s (RiskScore: %d, IsFraud: %v)",
		logEntry.ID, logEntry.DeviceID, logEntry.RiskScore, logEntry.IsFraud)
	return nil
}

// memGetHistory retrieves call logs from the in-memory store ordered newest-first.
// Pass deviceID="" to get logs for all devices.
// Pass fraudOnly=true to return only entries where IsFraud is true.
func memGetHistory(deviceID string, limit int, fraudOnly bool) ([]models.CallLog, error) {
	if limit <= 0 {
		limit = 50
	}

	memMu.RLock()
	defer memMu.RUnlock()

	result := make([]models.CallLog, 0, min(limit, len(memStore)))
	// Iterate in reverse so the most recent entries come first.
	for i := len(memStore) - 1; i >= 0 && len(result) < limit; i-- {
		cl := memStore[i]
		if deviceID != "" && cl.DeviceID != deviceID {
			continue
		}
		if fraudOnly && !cl.IsFraud {
			continue
		}
		result = append(result, cl)
	}

	log.Printf("📋 [mem] Retrieved %d call log(s) for device: %q (fraudOnly=%v)",
		len(result), deviceID, fraudOnly)
	return result, nil
}

// memGetCallLogByID looks up a single call log by its surrogate ID.
func memGetCallLogByID(id uint) (*models.CallLog, error) {
	memMu.RLock()
	defer memMu.RUnlock()

	for i := range memStore {
		if memStore[i].ID == id {
			cl := memStore[i] // copy so caller cannot mutate the store
			return &cl, nil
		}
	}
	return nil, fmt.Errorf("call log %d not found in memory store", id)
}
