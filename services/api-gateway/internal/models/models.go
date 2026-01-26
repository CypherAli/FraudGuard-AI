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
	ID               int       `json:"id"`
	PhoneNumber      string    `json:"phone_number"`
	Reason           string    `json:"reason"`
	ConfidenceScore  float64   `json:"confidence_score"`
	ReportedCount    int       `json:"reported_count"`
	FirstReportedAt  time.Time `json:"first_reported_at"`
	LastReportedAt   time.Time `json:"last_reported_at"`
	Status           string    `json:"status"` // active, inactive
	CreatedAt        time.Time `json:"created_at"`
	UpdatedAt        time.Time `json:"updated_at"`
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
