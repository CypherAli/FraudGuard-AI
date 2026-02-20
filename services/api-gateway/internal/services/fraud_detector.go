package services

import (
	"context"
	"fmt"
	"log"
	"regexp"
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
	config     *FraudDetectionConfig
	sendAlert  func(models.AlertMessage)
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
	LastAlertTime     time.Time // For alert cooldown enforcement
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
	criticalKeywords  map[string]int
	warningKeywords   map[string]int
	suspiciousPhrases map[string]int
	negativeKeywords  map[string]int
	negativePatterns  []*regexp.Regexp
	negativeScores    []int
}

// NewFraudDetector creates a new fraud detector for a session
func NewFraudDetector(deviceID string) *FraudDetector {
	return &FraudDetector{
		deviceID:  deviceID,
		session:   newSessionState(deviceID),
		keywords:  initializeKeywordMatcher(),
		startTime: time.Now(),
		config:    LoadFromEnvironment(),
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

// SetAlertCallback sets the callback function for sending alerts to mobile client
func (fd *FraudDetector) SetAlertCallback(sendAlert func(models.AlertMessage)) {
	fd.mu.Lock()
	defer fd.mu.Unlock()
	fd.sendAlert = sendAlert
}

func newSessionState(deviceID string) *SessionState {
	return &SessionState{
		DeviceID:          deviceID,
		SessionID:         uuid.New().String(),
		DetectedPatterns:  make([]string, 0),
		TranscriptHistory: make([]string, 0),
		StartTime:         time.Now(),
		LastUpdateTime:    time.Now(),
	}
}

// initializeKeywordMatcher sets up keyword patterns for fraud detection
func initializeKeywordMatcher() *KeywordMatcher {
	km := &KeywordMatcher{
		criticalKeywords: map[string]int{
			"chuyển tiền": 50, "chuyển khoản": 50,
			"mã otp": 45, "mã xác nhận": 45,
			"số tài khoản": 40, "thẻ tín dụng": 40, "thẻ atm": 40,
			"cccd": 35, "cmnd": 35, "căn cước": 35,
			"bị bắt": 40, "bị tạm giữ": 40, "lệnh bắt": 40, "truy nã": 45,
			"cài app": 35, "cài ứng dụng": 35, "tải app": 35,
			"anydesk": 50, "teamviewer": 50, "ultraviewer": 50,
		},
		warningKeywords: map[string]int{
			"công an": 25, "cảnh sát": 25, "viện kiểm sát": 25, "tòa án": 25,
			"ngân hàng": 20, "vietcombank": 20, "techcombank": 20, "bidv": 20, "agribank": 20,
			"bảo hiểm xã hội": 20, "bhxh": 20, "thuế": 20, "cục thuế": 20,
			"điện lực": 15, "evn": 15, "bưu điện": 15,
			"viettel": 15, "mobifone": 15, "vinaphone": 15,
			"trúng thưởng": 20, "giải thưởng": 20, "khuyến mãi": 15,
		},
		suspiciousPhrases: map[string]int{
			"gấp lắm": 25, "ngay lập tức": 25,
			"trong 5 phút": 30, "trong 10 phút": 30,
			"không làm sẽ bị": 35, "nếu không làm": 30,
			"bị khóa tài khoản": 35, "tài khoản bị đóng băng": 35,
			"có người tố cáo": 30, "liên quan đến vụ án": 35,
			"đường dây": 30, "rửa tiền": 40, "ma túy": 35, "buôn người": 35, "lừa đảo": 30,
			"bí mật": 25, "không được nói": 25, "đừng nói ai": 30, "giữ bí mật": 25,
			"lợi nhuận cao": 25, "kiếm tiền dễ dàng": 25, "thu nhập thêm": 20, "làm việc tại nhà": 15,
		},
		negativeKeywords: map[string]int{
			"phim": 100, "phim ảnh": 100, "bộ phim": 100, "rạp chiếu": 90,
			"truyện": 100, "tiểu thuyết": 100, "truyện tranh": 100, "truyện ngắn": 90,
			"tin tức": 80, "bản tin": 80, "thời sự": 80, "báo chí": 70,
			"sách": 90, "tiểu luận": 70,
			"giáo dục": 60, "học tập": 50, "giảng dạy": 60, "bài giảng": 60,
			"thảo luận": 50, "phân tích": 40, "nghiên cứu": 50,
			"diễn viên": 70, "đạo diễn": 70, "kịch bản": 70,
			"câu chuyện": 40, "nội dung": 30, "drama": 60, "series": 60,
		},
	}

	km.negativePatterns = []*regexp.Regexp{
		regexp.MustCompile(`(?i)p[h\-\. _]*[h1i!][\-\. _]*[i1!][\-\. _]*m`),
		regexp.MustCompile(`(?i)t[r\-\. _]*[u\-\. _]*[y\-\. _]*[e3ê\-\. _]*[n\-\. _]`),
		regexp.MustCompile(`(?i)t[i1!\-\. _]*n[\-\. _]+t[u\-\. _]*[c\-\. _]`),
		regexp.MustCompile(`(?i)s[a@4\-\. _]*[c\-\. _]*h`),
		regexp.MustCompile(`(?i)d[i1!\-\. _]*[e3ê\-\. _]*n[\-\. _]+v[i1!\-\. _]*[e3ê\-\. _]*n`),
		regexp.MustCompile(`(?i)k[i1!\-\. _]*[c\-\. _]*h[\-\. _]+b[a@4\-\. _]*n`),
	}
	km.negativeScores = []int{100, 100, 80, 90, 70, 70}

	return km
}

// AnalyzeText analyzes transcript text for fraud patterns
func (fd *FraudDetector) AnalyzeText(text string) FraudAnalysisResult {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	fd.session.TranscriptHistory = append(fd.session.TranscriptHistory, text)
	fd.session.LastUpdateTime = time.Now()

	normalizedText := strings.ToLower(text)
	score, patterns := fd.calculateRiskScore(normalizedText)

	fd.session.AccumulatedScore += score

	// Clamp accumulated score to [0, 100] range
	// CRITICAL for database constraints and confidence_score calculation
	if fd.session.AccumulatedScore < 0 {
		fd.session.AccumulatedScore = 0
	} else if fd.session.AccumulatedScore > 100 {
		log.Printf("⚠️ [%s] Accumulated score exceeds 100 (%d), capping at 100",
			fd.deviceID, fd.session.AccumulatedScore)
		fd.session.AccumulatedScore = 100
	}
	fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, patterns...)

	currentScore := fd.session.AccumulatedScore

	log.Printf("[%s] Analysis: score=%d (delta=%d), patterns=%d", fd.deviceID, currentScore, score, len(patterns))

	result := FraudAnalysisResult{
		RiskScore: currentScore,
		Patterns:  patterns,
	}

	// Gemini Agentic AI — tự dùng tools (check_blacklist, auto_report) rồi ra quyết định
	if GlobalGeminiClient != nil && GeminiCircuitBreaker.Allow() {
		alertCallback := fd.sendAlert
		sessionID := fd.session.SessionID
		// Copy slice để tránh race condition với goroutine
		historyCopy := make([]string, len(fd.session.TranscriptHistory))
		copy(historyCopy, fd.session.TranscriptHistory)

		go func(deviceID string, txt string, history []string) {
			defer func() {
				if r := recover(); r != nil {
					log.Printf("[%s] Recovered panic in Gemini Agent: %v", deviceID, r)
				}
			}()

			// Dùng Agentic loop thay vì classify đơn thuần
			agentResult, err := GlobalGeminiClient.RunFraudDetectionAgent(txt, history)
			if err != nil {
				log.Printf("[%s] Gemini Agent error: %v", deviceID, err)
				GeminiCircuitBreaker.RecordFailure()
				return
			}
			GeminiCircuitBreaker.RecordSuccess()

			if agentResult == nil || !agentResult.IsFraud || agentResult.RiskScore <= 30 {
				return
			}

			log.Printf("[%s] Gemini Agent fraud detected: Type=%s, Score=%d, Confidence=%s, Tools=%v, AutoReported=%v",
				deviceID, agentResult.FraudType, agentResult.RiskScore, agentResult.Confidence,
				agentResult.ToolsUsed, agentResult.AutoReported)

			geminiBoost := agentResult.RiskScore / 2
			fd.mu.Lock()
			if fd.session.SessionID == sessionID {
				fd.session.AccumulatedScore += geminiBoost
				if fd.session.AccumulatedScore > 100 {
					fd.session.AccumulatedScore = 100
				}
				patternNote := fmt.Sprintf("Agent: %s (+%d, confidence=%s, tools=%v)",
					agentResult.FraudType, geminiBoost, agentResult.Confidence, agentResult.ToolsUsed)
				if agentResult.AutoReported {
					patternNote += " [AUTO-REPORTED]"
				}
				fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, patternNote)
			}
			fd.mu.Unlock()

			if alertCallback != nil {
				fd.mu.RLock()
				currentAccumulated := fd.session.AccumulatedScore
				fd.mu.RUnlock()

				alertType := "MEDIUM"
				if currentAccumulated >= 80 || agentResult.RiskScore >= 80 {
					alertType = "CRITICAL"
				} else if currentAccumulated >= 60 || agentResult.RiskScore >= 60 {
					alertType = "HIGH"
				}

				msg := fmt.Sprintf("AI Agent phát hiện: %s - %s (Độ tin cậy: %s)",
					agentResult.FraudType, agentResult.Explanation, agentResult.Confidence)
				if agentResult.AutoReported {
					msg += " | Đã tự động báo cáo vào blacklist"
				}

				alert := models.AlertMessage{
					Type:       "alert",
					AlertType:  alertType,
					Confidence: float64(agentResult.RiskScore) / 100.0,
					Transcript: txt,
					Keywords:   []string{fmt.Sprintf("Agent: %s (%s)", agentResult.FraudType, agentResult.Confidence)},
					Timestamp:  time.Now().Unix(),
					Message:    msg,
				}
				alertCallback(alert)
			}
		}(fd.deviceID, text, historyCopy)
	}

	// Determine alert level based on accumulated score

	// Spam guards: respect MaxAlertsPerSession and AlertCooldown
	alertsExhausted := fd.session.AlertsSent >= fd.config.MaxAlertsPerSession
	cooldownActive := fd.config.AlertCooldownMs > 0 &&
		!fd.session.LastAlertTime.IsZero() &&
		time.Since(fd.session.LastAlertTime) < time.Duration(fd.config.AlertCooldownMs)*time.Millisecond
	switch {
	case currentScore >= fd.config.CriticalThreshold:
		result.Action = "CRITICAL"
		result.Message = fmt.Sprintf("CẢNH BÁO NGHIÊM TRỌNG: Phát hiện dấu hiệu lừa đảo rất cao! (Điểm rủi ro: %d/100)", currentScore)
		if !alertsExhausted && !cooldownActive {
			result.IsAlert = true
			fd.session.AlertsSent++
			fd.session.LastAlertTime = time.Now()
			fd.alertCount++
			log.Printf("[%s] CRITICAL ALERT: Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)
		} else {
			log.Printf("[%s] CRITICAL suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
		}

	case currentScore >= fd.config.HighThreshold:
		result.Action = "HIGH"
		result.Message = fmt.Sprintf("CẢNH BÁO CAO: Cuộc gọi có dấu hiệu đáng ngờ! (Điểm rủi ro: %d/100)", currentScore)
		if !alertsExhausted && !cooldownActive {
			result.IsAlert = true
			fd.session.AlertsSent++
			fd.session.LastAlertTime = time.Now()
			fd.alertCount++
			log.Printf("[%s] HIGH ALERT: Score=%d", fd.deviceID, currentScore)
		} else {
			log.Printf("[%s] HIGH suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
		}

	case currentScore >= fd.config.MediumThreshold:
		result.Action = "MEDIUM"
		result.Message = fmt.Sprintf("CẢNH BÁO: Phát hiện một số dấu hiệu bất thường (Điểm rủi ro: %d/100)", currentScore)
		if !alertsExhausted && !cooldownActive {
			result.IsAlert = true
			fd.session.AlertsSent++
			fd.session.LastAlertTime = time.Now()
			fd.alertCount++
		} else {
			log.Printf("[%s] MEDIUM suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
		}

	case currentScore >= fd.config.LowThreshold:
		result.Action = "LOW"
		result.Message = fmt.Sprintf("Lưu ý: Có một số từ khóa đáng chú ý (Điểm rủi ro: %d/100)", currentScore)

	default:
		result.Action = "SAFE"
		result.Message = "Cuộc gọi bình thường"
	}

	return result
}

// calculateRiskScore calculates risk score based on keyword matching
func (fd *FraudDetector) calculateRiskScore(text string) (int, []string) {
	totalScore := 0
	var detectedPatterns []string

	for keyword, score := range fd.keywords.criticalKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("CRITICAL: %s (+%d)", keyword, score))
		}
	}

	for keyword, score := range fd.keywords.warningKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WARNING: %s (+%d)", keyword, score))
		}
	}

	for phrase, score := range fd.keywords.suspiciousPhrases {
		if strings.Contains(text, phrase) {
			totalScore += score
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("SUSPICIOUS: %s (+%d)", phrase, score))
		}
	}

	for keyword, penalty := range fd.keywords.negativeKeywords {
		if strings.Contains(text, keyword) {
			totalScore -= penalty
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST: %s (-%d)", keyword, penalty))
		}
	}

	for i, pattern := range fd.keywords.negativePatterns {
		if pattern.MatchString(text) {
			penalty := fd.keywords.negativeScores[i]
			totalScore -= penalty
			match := pattern.FindString(text)
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST_REGEX: %s (-%d)", match, penalty))
		}
	}

	return totalScore, detectedPatterns
}

// AddDeepfakeBoost adds a deepfake score boost to the session's AccumulatedScore.
// This ensures the persistent session score reflects deepfake detection impact,
// not just the transient result.RiskScore local copy in audio_processor.go.
func (fd *FraudDetector) AddDeepfakeBoost(boost int) {
	fd.mu.Lock()
	defer fd.mu.Unlock()
	fd.session.AccumulatedScore += boost
	if fd.session.AccumulatedScore > 100 {
		fd.session.AccumulatedScore = 100
	}
	if fd.session.AccumulatedScore < 0 {
		fd.session.AccumulatedScore = 0
	}
	log.Printf("[%s] Deepfake boost applied: +%d → AccumulatedScore=%d",
		fd.deviceID, boost, fd.session.AccumulatedScore)
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

// ResetSession resets the session state
func (fd *FraudDetector) ResetSession() {
	fd.mu.Lock()
	defer fd.mu.Unlock()
	fd.session = newSessionState(fd.deviceID)
	fd.alertCount = 0
}

// ProcessFraudReport handles user reports of fraudulent phone numbers
func ProcessFraudReport(report models.ReportRequest) {
	if db.Pool == nil {
		log.Printf("[fraud_report] Database not available, cannot process report for %s", report.PhoneNumber)
		return
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	var existingID int
	var reportedCount int
	err := db.Pool.QueryRow(ctx,
		"SELECT id, reported_count FROM blacklist WHERE phone_number = $1",
		report.PhoneNumber,
	).Scan(&existingID, &reportedCount)

	if err != nil {
		_, err = db.Pool.Exec(ctx,
			`INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status)
			 VALUES ($1, $2, 0.50, 1, 'active')`,
			report.PhoneNumber, report.Reason,
		)
		if err != nil {
			log.Printf("[fraud_report] Error inserting blacklist entry: %v", err)
			return
		}
		log.Printf("[fraud_report] Added %s to blacklist", report.PhoneNumber)
	} else {
		newCount := reportedCount + 1
		newConfidence := calculateConfidenceScore(newCount)

		_, err = db.Pool.Exec(ctx,
			`UPDATE blacklist
			 SET reported_count = $1, confidence_score = $2, last_reported_at = NOW(), updated_at = NOW()
			 WHERE id = $3`,
			newCount, newConfidence, existingID,
		)
		if err != nil {
			log.Printf("[fraud_report] Error updating blacklist entry: %v", err)
			return
		}
		log.Printf("[fraud_report] Updated %s (reports: %d, confidence: %.2f)", report.PhoneNumber, newCount, newConfidence)
	}
}

func calculateConfidenceScore(reportCount int) float64 {
	switch {
	case reportCount >= 10:
		return 0.95
	case reportCount >= 5:
		return 0.85
	case reportCount >= 2:
		return 0.70
	default:
		return 0.50
	}
}

// CheckBlacklist checks if a phone number is in the blacklist
func CheckBlacklist(phoneNumber string) (*models.Blacklist, error) {
	if db.Pool == nil {
		return nil, fmt.Errorf("database not available")
	}

	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	var blacklist models.Blacklist
	err := db.Pool.QueryRow(ctx,
		`SELECT id, phone_number, reason, confidence_score, reported_count,
		        first_reported_at, last_reported_at, status, created_at, updated_at
		 FROM blacklist WHERE phone_number = $1 AND status = 'active'`,
		phoneNumber,
	).Scan(
		&blacklist.ID, &blacklist.PhoneNumber, &blacklist.Reason,
		&blacklist.ConfidenceScore, &blacklist.ReportedCount,
		&blacklist.FirstReportedAt, &blacklist.LastReportedAt,
		&blacklist.Status, &blacklist.CreatedAt, &blacklist.UpdatedAt,
	)
	if err != nil {
		return nil, err
	}

	return &blacklist, nil
}

// EndSession saves the call log to database when a session ends
func (fd *FraudDetector) EndSession() {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	session := fd.session
	if session == nil {
		return
	}

	endTime := time.Now()
	duration := int64(endTime.Sub(session.StartTime).Seconds())

	// Build evidence
	var evidence strings.Builder
	if len(session.DetectedPatterns) > 0 {
		evidence.WriteString("Patterns: ")
		evidence.WriteString(strings.Join(session.DetectedPatterns, "; "))
	}
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

	// Apply data masking before saving to database
	evidenceStr = MaskSensitiveData(evidenceStr)

	// Determine if call is fraudulent using configurable threshold
	isFraud := session.AccumulatedScore >= fd.config.HighThreshold

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

	log.Printf("[%s] Session ended - Duration: %ds, RiskScore: %d, IsFraud: %v, Alerts: %d",
		fd.deviceID, duration, session.AccumulatedScore, isFraud, session.AlertsSent)

	go func() {
		defer func() {
			if r := recover(); r != nil {
				log.Printf("[%s] Recovered panic in SaveCallLog: %v", fd.deviceID, r)
			}
		}()
		if err := repository.SaveCallLog(callLog); err != nil {
			log.Printf("[%s] Failed to save call log: %v", fd.deviceID, err)
		}
	}()
}
