package repository

import (
	"context"
	"fmt"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/jackc/pgx/v5"
)

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

// SaveCallLog saves a call log entry to PostgreSQL.
func SaveCallLog(logEntry *models.CallLog) error {
	if db.Pool == nil {
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

// GetHistory retrieves call history from PostgreSQL ordered by most recent first.
// If deviceID is empty, returns history for all devices.
func GetHistory(deviceID string, limit int) ([]models.CallLog, error) {
	if db.Pool == nil {
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

// GetFraudCallsOnly retrieves only fraudulent calls from PostgreSQL.
func GetFraudCallsOnly(deviceID string, limit int) ([]models.CallLog, error) {
	if db.Pool == nil {
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
