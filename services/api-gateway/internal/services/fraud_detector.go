package services

import (
	"context"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/google/uuid"
)

// ProcessFraudReport handles user reports of fraudulent phone numbers
func ProcessFraudReport(report models.ReportRequest) {
	log.Printf("📝 Processing fraud report from device %s: %s (Reason: %s)",
		report.DeviceID, report.PhoneNumber, report.Reason)

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Check if number already exists in blacklist
	var existingID uuid.UUID
	var reportCount int
	err := db.Pool.QueryRow(ctx,
		"SELECT id, report_count FROM blacklists WHERE phone_number = $1",
		report.PhoneNumber,
	).Scan(&existingID, &reportCount)

	if err != nil {
		// Number not in blacklist, insert new entry
		_, err = db.Pool.Exec(ctx,
			`INSERT INTO blacklists (phone_number, report_count, risk_level) 
			 VALUES ($1, 1, 'LOW')`,
			report.PhoneNumber,
		)
		if err != nil {
			log.Printf("❌ Error inserting blacklist entry: %v", err)
			return
		}
		log.Printf("✅ Added %s to blacklist (Risk: LOW)", report.PhoneNumber)
	} else {
		// Number exists, increment report count and update risk level
		newCount := reportCount + 1
		newRiskLevel := calculateRiskLevel(newCount)

		_, err = db.Pool.Exec(ctx,
			`UPDATE blacklists 
			 SET report_count = $1, risk_level = $2, updated_at = CURRENT_TIMESTAMP 
			 WHERE id = $3`,
			newCount, newRiskLevel, existingID,
		)
		if err != nil {
			log.Printf("❌ Error updating blacklist entry: %v", err)
			return
		}
		log.Printf("✅ Updated %s in blacklist (Reports: %d, Risk: %s)",
			report.PhoneNumber, newCount, newRiskLevel)
	}
}

// calculateRiskLevel determines risk level based on report count
func calculateRiskLevel(reportCount int) string {
	switch {
	case reportCount >= 10:
		return "CRITICAL"
	case reportCount >= 5:
		return "HIGH"
	case reportCount >= 2:
		return "MEDIUM"
	default:
		return "LOW"
	}
}

// CheckBlacklist checks if a phone number is in the blacklist
func CheckBlacklist(phoneNumber string) (*models.Blacklist, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	var blacklist models.Blacklist
	err := db.Pool.QueryRow(ctx,
		`SELECT id, phone_number, report_count, risk_level, created_at, updated_at 
		 FROM blacklists WHERE phone_number = $1`,
		phoneNumber,
	).Scan(
		&blacklist.ID,
		&blacklist.PhoneNumber,
		&blacklist.ReportCount,
		&blacklist.RiskLevel,
		&blacklist.CreatedAt,
		&blacklist.UpdatedAt,
	)

	if err != nil {
		return nil, err
	}

	return &blacklist, nil
}
