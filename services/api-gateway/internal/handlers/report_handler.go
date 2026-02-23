package handlers

import (
	"context"
	"encoding/json"
	"log"
	"net/http"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/fraudguard/api-gateway/internal/services"
)

// ReportFraud handles POST /api/report
// Allows users to report fraudulent phone numbers
// Implements consensus: auto-verify at 3+ unique reports
func ReportFraud(w http.ResponseWriter, r *http.Request) {
	var report models.ReportRequest
	if err := json.NewDecoder(r.Body).Decode(&report); err != nil {
		sendJSONError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if report.PhoneNumber == "" {
		sendJSONError(w, "phone_number is required", http.StatusBadRequest)
		return
	}

	if report.DeviceID == "" {
		sendJSONError(w, "device_id is required", http.StatusBadRequest)
		return
	}

	// Process the fraud report (updates blacklist)
	if err := services.ProcessFraudReport(report); err != nil {
		log.Printf("[ReportFraud] Failed to process report for %s: %v", report.PhoneNumber, err)
		sendJSONError(w, "Failed to process report: "+err.Error(), http.StatusInternalServerError)
		return
	}

	// Check if the phone number should be auto-verified (consensus)
	autoVerified := checkConsensusVerification(report.PhoneNumber)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success":       true,
		"message":       "Báo cáo đã được ghi nhận",
		"phone_number":  report.PhoneNumber,
		"auto_verified": autoVerified,
	})
}

// ImportBlacklist handles POST /api/blacklist/import
// Allows batch import of blacklisted numbers (for community sync)
func ImportBlacklist(w http.ResponseWriter, r *http.Request) {
	if db.Pool == nil {
		sendJSONError(w, "Database not available", http.StatusServiceUnavailable)
		return
	}

	var request struct {
		Numbers []struct {
			PhoneNumber string `json:"phone_number"`
			Reason      string `json:"reason"`
		} `json:"numbers"`
	}

	if err := json.NewDecoder(r.Body).Decode(&request); err != nil {
		sendJSONError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if len(request.Numbers) == 0 {
		sendJSONError(w, "numbers array is required", http.StatusBadRequest)
		return
	}

	// Limit batch size
	if len(request.Numbers) > 100 {
		sendJSONError(w, "Maximum 100 numbers per import", http.StatusBadRequest)
		return
	}

	imported := 0
	for _, num := range request.Numbers {
		if num.PhoneNumber == "" {
			continue
		}
		if err := services.ProcessFraudReport(models.ReportRequest{
			PhoneNumber: num.PhoneNumber,
			Reason:      num.Reason,
			DeviceID:    "import",
		}); err != nil {
			log.Printf("[ImportBlacklist] Failed to import %s: %v", num.PhoneNumber, err)
			continue
		}
		imported++
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success":  true,
		"imported": imported,
		"message":  "Đã import thành công",
	})
}

// checkConsensusVerification checks if a phone number has enough reports to auto-verify.
// Consensus threshold: 3+ unique reports → confidence_score elevated to 0.95 (verified).
// Uses existing schema columns: reported_count and confidence_score (no 'verified' column).
func checkConsensusVerification(phoneNumber string) bool {
	if db.Pool == nil {
		return false
	}

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	var reportedCount int
	var confidenceScore float64

	err := db.Pool.QueryRow(ctx,
		`SELECT reported_count, confidence_score FROM blacklist WHERE phone_number = $1`,
		phoneNumber,
	).Scan(&reportedCount, &confidenceScore)

	if err != nil {
		return false
	}

	// Already at high-confidence (auto-verified previously)
	if confidenceScore >= 0.95 {
		return true
	}

	// Check consensus threshold (3+ reports = high confidence / verified)
	if reportedCount >= 3 {
		_, err := db.Pool.Exec(ctx,
			`UPDATE blacklist SET confidence_score = 0.95, updated_at = NOW() WHERE phone_number = $1`,
			phoneNumber,
		)
		if err != nil {
			log.Printf("⚠️ Failed to auto-verify %s: %v", phoneNumber, err)
			return false
		}
		log.Printf("✅ Auto-verified %s (consensus: %d reports, confidence → 0.95)", phoneNumber, reportedCount)
		return true
	}

	return false
}
