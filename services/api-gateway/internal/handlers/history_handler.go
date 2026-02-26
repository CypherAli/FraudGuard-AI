package handlers

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"

	"github.com/fraudguard/api-gateway/internal/repository"
	"github.com/go-chi/chi/v5"
)

// GetHistory handles GET /api/history requests
// Query parameters:
//   - device_id: (optional) filter by device ID
//   - limit: (optional) maximum number of records to return (default: 20, max: 100)
//   - fraud_only: (optional) if "true", return only fraudulent calls
func GetHistory(w http.ResponseWriter, r *http.Request) {
	// Extract query parameters
	deviceID := r.URL.Query().Get("device_id")
	limitStr := r.URL.Query().Get("limit")
	fraudOnly := r.URL.Query().Get("fraud_only") == "true"

	// Parse limit with validation
	limit := 20 // Default limit
	if limitStr != "" {
		if l, err := strconv.Atoi(limitStr); err == nil && l > 0 {
			limit = l
			// Cap at 500 to prevent excessive data transfer
			if limit > 500 {
				limit = 500
			}
		}
	}

	log.Printf(" History request: device_id=%s, limit=%d, fraud_only=%v", deviceID, limit, fraudOnly)

	// Fetch data from repository
	var logs interface{}
	var err error

	if fraudOnly {
		logs, err = repository.GetFraudCallsOnly(deviceID, limit)
	} else {
		logs, err = repository.GetHistory(deviceID, limit)
	}

	if err != nil {
		log.Printf(" Error fetching history: %v", err)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusInternalServerError)
		json.NewEncoder(w).Encode(map[string]string{
			"error": "Failed to fetch call history",
		})
		return
	}

	// Return JSON response
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)

	response := map[string]interface{}{
		"success": true,
		"data":    logs,
	}

	if err := json.NewEncoder(w).Encode(response); err != nil {
		log.Printf(" Error encoding response: %v", err)
	}
}

// GetCallDetail handles GET /api/call/{id} requests
// Returns detailed information for a single call log entry
func GetCallDetail(w http.ResponseWriter, r *http.Request) {
	idStr := chi.URLParam(r, "id")
	id, err := strconv.ParseUint(idStr, 10, 64)
	if err != nil {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusBadRequest)
		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"error":   "Invalid call ID",
		})
		return
	}

	callLog, err := repository.GetCallLogByID(uint(id))
	if err != nil {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusNotFound)
		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": false,
			"error":   "Call not found",
		})
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"data":    callLog,
	})
}
