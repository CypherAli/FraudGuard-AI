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

// GetBlacklist returns the list of blacklisted phone numbers
func GetBlacklist(w http.ResponseWriter, r *http.Request) {
	ctx, cancel := context.WithTimeout(r.Context(), 5*time.Second)
	defer cancel()

	// Query all blacklisted numbers
	rows, err := db.Pool.Query(ctx,
		`SELECT id, phone_number, reason, confidence_score, reported_count, 
		        first_reported_at, last_reported_at, status, created_at, updated_at 
		 FROM blacklist 
		 ORDER BY confidence_score DESC, reported_count DESC`,
	)
	if err != nil {
		log.Printf(" Error querying blacklist: %v", err)
		http.Error(w, "Internal server error", http.StatusInternalServerError)
		return
	}
	defer rows.Close()

	var blacklists []models.Blacklist
	for rows.Next() {
		var bl models.Blacklist
		err := rows.Scan(
			&bl.ID,
			&bl.PhoneNumber,
			&bl.Reason,
			&bl.ConfidenceScore,
			&bl.ReportedCount,
			&bl.FirstReportedAt,
			&bl.LastReportedAt,
			&bl.Status,
			&bl.CreatedAt,
			&bl.UpdatedAt,
		)
		if err != nil {
			log.Printf(" Error scanning blacklist row: %v", err)
			continue
		}
		blacklists = append(blacklists, bl)
	}

	// Return JSON response
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"count":   len(blacklists),
		"data":    blacklists,
	})
}

// CheckNumber checks if a specific phone number is blacklisted
func CheckNumber(w http.ResponseWriter, r *http.Request) {
	phoneNumber := r.URL.Query().Get("phone")
	if phoneNumber == "" {
		http.Error(w, "phone parameter is required", http.StatusBadRequest)
		return
	}

	blacklist, err := services.CheckBlacklist(phoneNumber)
	if err != nil {
		// Number not found in blacklist
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"success":      true,
			"is_blacklist": false,
			"phone_number": phoneNumber,
		})
		return
	}

	// Number found in blacklist
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success":      true,
		"is_blacklist": true,
		"data":         blacklist,
	})
}

// HealthCheck returns the health status of the API
func HealthCheck(w http.ResponseWriter, r *http.Request) {
	// Check database connection
	if err := db.HealthCheck(r.Context()); err != nil {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusServiceUnavailable)
		json.NewEncoder(w).Encode(map[string]interface{}{
			"status":   "unhealthy",
			"database": "disconnected",
			"error":    err.Error(),
		})
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"status":   "healthy",
		"database": "connected",
		"service":  "FraudGuard AI",
	})
}
