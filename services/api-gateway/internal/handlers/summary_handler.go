package handlers

import (
	"encoding/json"
	"log"
	"net/http"

	"github.com/fraudguard/api-gateway/internal/services"
)

// CallSummaryRequest holds the request body for generating a call summary
type CallSummaryRequest struct {
	DeviceID    string   `json:"device_id"`
	Transcripts []string `json:"transcripts"`
	RiskScore   int      `json:"risk_score"`
	Patterns    []string `json:"patterns"`
}

// GenerateCallSummary handles POST /api/call-summary
// Generates a post-call summary using Gemini AI
func GenerateCallSummary(w http.ResponseWriter, r *http.Request) {
	if services.GlobalGeminiClient == nil || !services.GlobalGeminiClient.IsEnabled() {
		sendJSONError(w, "Gemini AI chưa được cấu hình", http.StatusServiceUnavailable)
		return
	}

	var req CallSummaryRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		sendJSONError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	if len(req.Transcripts) == 0 {
		sendJSONError(w, "transcripts is required", http.StatusBadRequest)
		return
	}

	// Generate summary using Gemini
	summary, err := services.GlobalGeminiClient.GenerateCallSummary(
		req.Transcripts,
		req.RiskScore,
		req.Patterns,
	)
	if err != nil {
		log.Printf("❌ [Summary] Gemini summary generation failed: %v", err)
		sendJSONError(w, "Không thể tạo tóm tắt cuộc gọi", http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"summary": summary,
	})
}
