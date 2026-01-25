package models

import (
	"time"
)

// CallLog represents a fraud detection call session log
type CallLog struct {
	ID        uint      `gorm:"primaryKey" json:"id"`
	DeviceID  string    `gorm:"index;not null" json:"device_id"`
	StartTime time.Time `json:"start_time"`
	EndTime   time.Time `json:"end_time"`
	Duration  int64     `json:"duration_seconds"`          // Duration in seconds
	RiskScore int       `json:"risk_score"`                // Accumulated risk score
	IsFraud   bool      `json:"is_fraud"`                  // True if risk score >= threshold
	Evidence  string    `gorm:"type:text" json:"evidence"` // Transcript snippets or detected keywords
	CreatedAt time.Time `json:"created_at"`
}

// TableName specifies the table name for GORM
func (CallLog) TableName() string {
	return "call_logs"
}
