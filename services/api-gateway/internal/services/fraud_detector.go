package services

import (
	"context"
	"fmt"
	"log"
	"strings"
	"sync"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/fraudguard/api-gateway/internal/repository"
	"github.com/google/uuid"
)

// FraudDetector handles real-time fraud detection with accumulated risk scoring
type FraudDetector struct {
	deviceID   string
	session    *SessionState
	mu         sync.RWMutex
	keywords   *KeywordMatcher
	alertCount int
	startTime  time.Time
	config     *FraudDetectionConfig // Configurable thresholds
}

// SessionState tracks accumulated risk for a call session
type SessionState struct {
	DeviceID          string
	SessionID         string
	AccumulatedScore  int
	DetectedPatterns  []string
	TranscriptHistory []string
	StartTime         time.Time
	LastUpdateTime    time.Time
	AlertsSent        int
}

// FraudAnalysisResult contains the result of fraud analysis
type FraudAnalysisResult struct {
	IsAlert   bool
	RiskScore int
	Message   string
	Action    string
	Patterns  []string
}

// KeywordMatcher handles keyword-based fraud detection
type KeywordMatcher struct {
	// Critical keywords (high risk)
	criticalKeywords map[string]int
	// Warning keywords (medium risk)
	warningKeywords map[string]int
	// Suspicious phrases (context-based)
	suspiciousPhrases map[string]int
}

// NewFraudDetector creates a new fraud detector for a session
func NewFraudDetector(deviceID string) *FraudDetector {
	return &FraudDetector{
		deviceID:  deviceID,
		session:   newSessionState(deviceID),
		keywords:  initializeKeywordMatcher(),
		startTime: time.Now(),
		config:    DefaultFraudDetectionConfig(), // Use default config
	}
}

// NewFraudDetectorWithConfig creates a fraud detector with custom config
func NewFraudDetectorWithConfig(deviceID string, config *FraudDetectionConfig) *FraudDetector {
	return &FraudDetector{
		deviceID:  deviceID,
		session:   newSessionState(deviceID),
		keywords:  initializeKeywordMatcher(),
		startTime: time.Now(),
		config:    config,
	}
}

// newSessionState creates a new session state
func newSessionState(deviceID string) *SessionState {
	return &SessionState{
		DeviceID:          deviceID,
		SessionID:         uuid.New().String(),
		AccumulatedScore:  0,
		DetectedPatterns:  make([]string, 0),
		TranscriptHistory: make([]string, 0),
		StartTime:         time.Now(),
		LastUpdateTime:    time.Now(),
		AlertsSent:        0,
	}
}

// initializeKeywordMatcher sets up keyword patterns for fraud detection
func initializeKeywordMatcher() *KeywordMatcher {
	return &KeywordMatcher{
		// Critical keywords - Very high risk (30-50 points each)
		criticalKeywords: map[string]int{
			"chuyển tiền":  50,
			"chuyển khoản": 50,
			"mã otp":       45,
			"mã xác nhận":  45,
			"số tài khoản": 40,
			"thẻ tín dụng": 40,
			"thẻ atm":      40,
			"cccd":         35,
			"cmnd":         35,
			"căn cước":     35,
			"bị bắt":       40,
			"bị tạm giữ":   40,
			"lệnh bắt":     40,
			"truy nã":      45,
			"cài app":      35,
			"cài ứng dụng": 35,
			"tải app":      35,
			"anydesk":      50,
			"teamviewer":   50,
			"ultraviewer":  50,
		},

		// Warning keywords - Medium risk (15-25 points each)
		warningKeywords: map[string]int{
			"công an":         25,
			"cảnh sát":        25,
			"viện kiểm sát":   25,
			"tòa án":          25,
			"ngân hàng":       20,
			"vietcombank":     20,
			"techcombank":     20,
			"bidv":            20,
			"agribank":        20,
			"bảo hiểm xã hội": 20,
			"bhxh":            20,
			"thuế":            20,
			"cục thuế":        20,
			"điện lực":        15,
			"evn":             15,
			"bưu điện":        15,
			"viettel":         15,
			"mobifone":        15,
			"vinaphone":       15,
			"trúng thưởng":    20,
			"giải thưởng":     20,
			"khuyến mãi":      15,
		},

		// Suspicious phrases - Context-based (20-35 points each)
		suspiciousPhrases: map[string]int{
			"gấp lắm":                25,
			"ngay lập tức":           25,
			"trong 5 phút":           30,
			"trong 10 phút":          30,
			"không làm sẽ bị":        35,
			"nếu không làm":          30,
			"bị khóa tài khoản":      35,
			"tài khoản bị đóng băng": 35,
			"có người tố cáo":        30,
			"liên quan đến vụ án":    35,
			"đường dây":              30,
			"rửa tiền":               40,
			"ma túy":                 35,
			"buôn người":             35,
			"lừa đảo":                30,
			"bí mật":                 25,
			"không được nói":         25,
			"đừng nói ai":            30,
			"giữ bí mật":             25,
			"lợi nhuận cao":          25,
			"kiếm tiền dễ dàng":      25,
			"thu nhập thêm":          20,
			"làm việc tại nhà":       15,
		},
	}
}

// AnalyzeText analyzes transcript text for fraud patterns
// This is the main entry point called from audio processor
func (fd *FraudDetector) AnalyzeText(text string) FraudAnalysisResult {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	log.Printf("🔍 [%s] Analyzing text: %s", fd.deviceID, text)

	// Update session
	fd.session.TranscriptHistory = append(fd.session.TranscriptHistory, text)
	fd.session.LastUpdateTime = time.Now()

	// Normalize text for matching
	normalizedText := strings.ToLower(text)

	// Calculate risk score from keywords
	score, patterns := fd.calculateRiskScore(normalizedText)

	// Add to accumulated score
	fd.session.AccumulatedScore += score
	fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, patterns...)

	// Determine alert level
	currentScore := fd.session.AccumulatedScore
	result := FraudAnalysisResult{
		RiskScore: currentScore,
		Patterns:  patterns,
	}

	// TODO: Integrate with OpenAI/Gemini for semantic analysis
	// This would provide more sophisticated fraud detection beyond keywords
	// Example:
	// if GlobalGeminiClient != nil {
	//     aiResult := GlobalGeminiClient.AnalyzeFraud(text)
	//     if aiResult.IsFraud {
	//         currentScore += aiResult.RiskScore
	//     }
	// }

	// Determine alert level based on accumulated score and config thresholds
	if currentScore >= fd.config.CriticalThreshold {
		result.IsAlert = true
		result.Action = "CRITICAL"
		result.Message = fmt.Sprintf("🚨 CẢNH BÁO NGHIÊM TRỌNG: Phát hiện dấu hiệu lừa đảo rất cao! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("🚨 [%s] CRITICAL ALERT: Score=%d, Patterns=%v",
			fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.HighThreshold {
		result.IsAlert = true
		result.Action = "HIGH"
		result.Message = fmt.Sprintf("⚠️ CẢNH BÁO CAO: Cuộc gọi có dấu hiệu đáng ngờ! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("⚠️ [%s] HIGH ALERT: Score=%d, Patterns=%v",
			fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.MediumThreshold {
		result.IsAlert = true
		result.Action = "MEDIUM"
		result.Message = fmt.Sprintf("⚡ CẢNH BÁO: Phát hiện một số dấu hiệu bất thường (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("⚡ [%s] MEDIUM ALERT: Score=%d, Patterns=%v",
			fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.LowThreshold {
		result.IsAlert = false
		result.Action = "LOW"
		result.Message = fmt.Sprintf("ℹ️ Lưu ý: Có một số từ khóa đáng chú ý (Điểm rủi ro: %d/100)", currentScore)

		log.Printf("ℹ️ [%s] LOW RISK: Score=%d, Patterns=%v",
			fd.deviceID, currentScore, patterns)

	} else {
		result.IsAlert = false
		result.Action = "SAFE"
		result.Message = "✅ Cuộc gọi bình thường"

		log.Printf("✅ [%s] SAFE: Score=%d", fd.deviceID, currentScore)
	}

	return result
}

// calculateRiskScore calculates risk score based on keyword matching
func (fd *FraudDetector) calculateRiskScore(text string) (int, []string) {
	totalScore := 0
	detectedPatterns := make([]string, 0)

	// Check critical keywords
	for keyword, score := range fd.keywords.criticalKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("CRITICAL: %s (+%d)", keyword, score))
			log.Printf("🔴 [%s] Critical keyword detected: '%s' (+%d points)",
				fd.deviceID, keyword, score)
		}
	}

	// Check warning keywords
	for keyword, score := range fd.keywords.warningKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WARNING: %s (+%d)", keyword, score))
			log.Printf("🟡 [%s] Warning keyword detected: '%s' (+%d points)",
				fd.deviceID, keyword, score)
		}
	}

	// Check suspicious phrases
	for phrase, score := range fd.keywords.suspiciousPhrases {
		if strings.Contains(text, phrase) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("SUSPICIOUS: %s (+%d)", phrase, score))
			log.Printf("🟠 [%s] Suspicious phrase detected: '%s' (+%d points)",
				fd.deviceID, phrase, score)
		}
	}

	return totalScore, detectedPatterns
}

// GetCurrentRiskScore returns the current accumulated risk score
func (fd *FraudDetector) GetCurrentRiskScore() int {
	fd.mu.RLock()
	defer fd.mu.RUnlock()
	return fd.session.AccumulatedScore
}

// GetAlertCount returns the number of alerts sent
func (fd *FraudDetector) GetAlertCount() int {
	fd.mu.RLock()
	defer fd.mu.RUnlock()
	return fd.alertCount
}

// GetSessionState returns a copy of the current session state
func (fd *FraudDetector) GetSessionState() SessionState {
	fd.mu.RLock()
	defer fd.mu.RUnlock()
	return *fd.session
}

// ResetSession resets the session state (useful for new calls)
func (fd *FraudDetector) ResetSession() {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	log.Printf("🔄 [%s] Resetting fraud detection session", fd.deviceID)
	fd.session = newSessionState(fd.deviceID)
	fd.alertCount = 0
}

// ==================== Database Integration ====================

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
		`SELECT id, phone_number, reason, confidence_score, reported_count, 
		        first_reported_at, last_reported_at, status, created_at, updated_at 
		 FROM blacklist WHERE phone_number = $1 AND status = 'active'`,
		phoneNumber,
	).Scan(
		&blacklist.ID,
		&blacklist.PhoneNumber,
		&blacklist.Reason,
		&blacklist.ConfidenceScore,
		&blacklist.ReportedCount,
		&blacklist.FirstReportedAt,
		&blacklist.LastReportedAt,
		&blacklist.Status,
		&blacklist.CreatedAt,
		&blacklist.UpdatedAt,
	)

	if err != nil {
		return nil, err
	}

	return &blacklist, nil
}

// ==================== Session Management ====================

// EndSession saves the call log to database when a session ends
// This should be called when WebSocket connection is closed
func (fd *FraudDetector) EndSession() {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	session := fd.session
	if session == nil {
		log.Printf("⚠️ [%s] No active session to end", fd.deviceID)
		return
	}

	endTime := time.Now()
	duration := int64(endTime.Sub(session.StartTime).Seconds())

	// Build evidence from detected patterns and transcript history
	evidence := strings.Builder{}

	// Add detected patterns
	if len(session.DetectedPatterns) > 0 {
		evidence.WriteString("Patterns: ")
		evidence.WriteString(strings.Join(session.DetectedPatterns, "; "))
	}

	// Add transcript snippets (limit to 500 chars total)
	if len(session.TranscriptHistory) > 0 {
		if evidence.Len() > 0 {
			evidence.WriteString(" | ")
		}
		evidence.WriteString("Transcript: ")
		transcriptText := strings.Join(session.TranscriptHistory, " ")
		if len(transcriptText) > 400 {
			transcriptText = transcriptText[:400] + "..."
		}
		evidence.WriteString(transcriptText)
	}

	evidenceStr := evidence.String()
	if len(evidenceStr) > 1000 {
		evidenceStr = evidenceStr[:1000] + "..."
	}

	// Determine if call is fraudulent based on threshold
	// Using 60 as threshold (configurable via fd.config.HighThreshold)
	isFraud := session.AccumulatedScore >= 60

	// Create call log entry
	callLog := &models.CallLog{
		DeviceID:  fd.deviceID,
		StartTime: session.StartTime,
		EndTime:   endTime,
		Duration:  duration,
		RiskScore: session.AccumulatedScore,
		IsFraud:   isFraud,
		Evidence:  evidenceStr,
		CreatedAt: time.Now(),
	}

	log.Printf("🛑 [%s] Session ended - Duration: %ds, RiskScore: %d, IsFraud: %v, Alerts: %d",
		fd.deviceID, duration, session.AccumulatedScore, isFraud, session.AlertsSent)

	// Save to database asynchronously to avoid blocking
	go func() {
		if err := repository.SaveCallLog(callLog); err != nil {
			log.Printf("❌ [%s] Failed to save call log: %v", fd.deviceID, err)
		} else {
			log.Printf("✅ [%s] Call log saved successfully (ID: %d)", fd.deviceID, callLog.ID)
		}
	}()
}
