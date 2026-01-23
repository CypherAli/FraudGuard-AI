package models

import (
	"time"

	"github.com/google/uuid"
)

// User represents a registered device
type User struct {
	ID        uuid.UUID `json:"id"`
	DeviceID  string    `json:"device_id"`
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}

// Blacklist represents a reported fraudulent phone number
type Blacklist struct {
	ID          uuid.UUID `json:"id"`
	PhoneNumber string    `json:"phone_number"`
	ReportCount int       `json:"report_count"`
	RiskLevel   string    `json:"risk_level"` // LOW, MEDIUM, HIGH, CRITICAL
	CreatedAt   time.Time `json:"created_at"`
	UpdatedAt   time.Time `json:"updated_at"`
}

// CallLog represents a call record with AI analysis
type CallLog struct {
	ID          uuid.UUID              `json:"id"`
	UserID      uuid.UUID              `json:"user_id"`
	PhoneNumber string                 `json:"phone_number,omitempty"`
	Transcript  string                 `json:"transcript,omitempty"`
	Duration    int                    `json:"duration"`           // in seconds
	Metadata    map[string]interface{} `json:"metadata,omitempty"` // JSONB - AI analysis results
	CreatedAt   time.Time              `json:"created_at"`
}

// AlertMessage represents a fraud alert sent to client
type AlertMessage struct {
	RiskScore int    `json:"risk_score"` // 0-100
	Message   string `json:"message"`
	Action    string `json:"action"` // SHOW_WARNING, BLOCK_CALL, ALLOW
	Timestamp int64  `json:"timestamp"`
}

// ReportRequest represents a user report of fraudulent number
type ReportRequest struct {
	PhoneNumber string `json:"phone_number"`
	Reason      string `json:"reason"`
	DeviceID    string `json:"device_id"`
}
