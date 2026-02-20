package repository

import (
	"fmt"
	"log"

	"github.com/fraudguard/api-gateway/internal/models"
	"gorm.io/driver/sqlite"
	"gorm.io/gorm"
	"gorm.io/gorm/logger"
)

// DB is the global GORM database instance for SQLite
var DB *gorm.DB

// InitSQLite initializes the SQLite database connection and performs auto-migration
func InitSQLite() error {
	var err error

	// Open SQLite database with WAL mode for safe concurrent writes
	// _journal_mode=WAL allows concurrent readers + single writer without corruption
	// _busy_timeout=5000 waits 5s on lock instead of failing immediately
	DB, err = gorm.Open(sqlite.Open("fraud_guard.db?_journal_mode=WAL&_busy_timeout=5000"), &gorm.Config{
		Logger: logger.Default.LogMode(logger.Info),
	})

	if err != nil {
		log.Printf("❌ Failed to connect to SQLite database: %v", err)
		return err
	}

	// Auto-migrate the CallLog model (creates/updates table schema)
	err = DB.AutoMigrate(&models.CallLog{})
	if err != nil {
		log.Printf("❌ Failed to migrate database schema: %v", err)
		return err
	}

	log.Println("✅ SQLite database initialized successfully (fraud_guard.db)")
	return nil
}

// SaveCallLog saves a call log entry to the database
func SaveCallLog(logEntry *models.CallLog) error {
	if DB == nil {
		log.Println("⚠️ Database not initialized, skipping call log save")
		return nil
	}

	result := DB.Create(logEntry)
	if result.Error != nil {
		log.Printf("❌ Error saving call log: %v", result.Error)
		return result.Error
	}

	log.Printf("💾 Saved Call Log [ID: %d] for Device %s (RiskScore: %d, IsFraud: %v)",
		logEntry.ID, logEntry.DeviceID, logEntry.RiskScore, logEntry.IsFraud)
	return nil
}

// GetHistory retrieves call history for a specific device or all devices
// If deviceID is empty, returns history for all devices
// Results are ordered by most recent first
func GetHistory(deviceID string, limit int) ([]models.CallLog, error) {
	if DB == nil {
		log.Println("⚠️ [GetHistory] Database not initialized - cannot retrieve call history")
		return nil, fmt.Errorf("database not initialized")
	}

	var logs []models.CallLog

	// Build query
	query := DB.Order("created_at desc")

	// Apply limit (default to 50 if not specified)
	if limit <= 0 {
		limit = 50
	}
	query = query.Limit(limit)

	// Filter by device ID if provided
	if deviceID != "" {
		query = query.Where("device_id = ?", deviceID)
	}

	// Execute query
	result := query.Find(&logs)
	if result.Error != nil {
		log.Printf("❌ Error fetching call history: %v", result.Error)
		return nil, result.Error
	}

	log.Printf("📋 Retrieved %d call log(s) for device: %s", len(logs), deviceID)
	return logs, nil
}

// GetAllHistory retrieves all call history (for admin/debugging)
func GetAllHistory(limit int) ([]models.CallLog, error) {
	return GetHistory("", limit)
}

// GetFraudCallsOnly retrieves only fraudulent calls
func GetFraudCallsOnly(deviceID string, limit int) ([]models.CallLog, error) {
	if DB == nil {
		log.Println("⚠️ [GetFraudCallsOnly] Database not initialized - cannot retrieve fraud calls")
		return nil, fmt.Errorf("database not initialized")
	}

	var logs []models.CallLog

	query := DB.Where("is_fraud = ?", true).Order("created_at desc")

	if limit <= 0 {
		limit = 50
	}
	query = query.Limit(limit)

	if deviceID != "" {
		query = query.Where("device_id = ?", deviceID)
	}

	result := query.Find(&logs)
	if result.Error != nil {
		log.Printf("❌ Error fetching fraud calls: %v", result.Error)
		return nil, result.Error
	}

	log.Printf("🚨 Retrieved %d fraud call(s) for device: %s", len(logs), deviceID)
	return logs, nil
}
