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
	config     *FraudDetectionConfig // Configurable thresholds
	sendAlert  func(models.AlertMessage) // Callback to send alerts to mobile client
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
	// Negative keywords (reduce score - whitelist for legitimate content)
	negativeKeywords map[string]int
	// Negative patterns (regex for detecting obfuscated negative keywords)
	negativePatterns []*regexp.Regexp
	negativeScores   []int // Corresponding scores for each pattern
}

// NewFraudDetector creates a new fraud detector for a session
func NewFraudDetector(deviceID string) *FraudDetector {
	return &FraudDetector{
		deviceID:  deviceID,
		session:   newSessionState(deviceID),
		keywords:  initializeKeywordMatcher(),
		startTime: time.Now(),
		config:    LoadFromEnvironment(), // Load from environment for production tuning
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
// This is required for Gemini AI async results to trigger mobile alerts
func (fd *FraudDetector) SetAlertCallback(sendAlert func(models.AlertMessage)) {
	fd.mu.Lock()
	defer fd.mu.Unlock()
	fd.sendAlert = sendAlert
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
	km := &KeywordMatcher{
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

		// Negative keywords - REDUCE score (whitelist for legitimate content)
		// These indicate the conversation is about entertainment/news, not actual fraud
		negativeKeywords: map[string]int{
			"phim":         100, // Movie/film content
			"phim ảnh":     100,
			"bộ phim":      100,
			"rạp chiếu":    90,
			"truyện":       100, // Story/novel content
			"tiểu thuyết":  100,
			"truyện tranh": 100,
			"truyện ngắn":  90,
			"tin tức":      80, // News content
			"bản tin":      80,
			"thời sự":      80,
			"báo chí":      70,
			"sách":         90, // Books
			"tiểu luận":    70,
			"giáo dục":     60, // Education
			"học tập":      50,
			"giảng dạy":    60,
			"bài giảng":    60,
			"thảo luận":    50, // Discussion
			"phân tích":    40,
			"nghiên cứu":   50,
			"diễn viên":    70, // Entertainment industry
			"đạo diễn":     70,
			"kịch bản":     70,
			"câu chuyện":   40,
			"nội dung":     30,
			"drama":        60,
			"series":       60,
		},
	}

	// Initialize regex patterns for detecting obfuscated negative keywords
	// This prevents users from bypassing filters with variations like:
	// "Ph1m", "P.h.i.m", "P-h-i-m", "PhIm", "ph!m", etc.
	km.negativePatterns = []*regexp.Regexp{
		// "Phim" variations
		regexp.MustCompile(`(?i)p[h\-\. _]*[h1i!][\-\. _]*[i1!][\-\. _]*m`),
		// "Truyện" variations
		regexp.MustCompile(`(?i)t[r\-\. _]*[u\-\. _]*[y\-\. _]*[e3ê\-\. _]*[n\-\. _]`),
		// "Tin tức" variations
		regexp.MustCompile(`(?i)t[i1!\-\. _]*n[\-\. _]+t[u\-\. _]*[c\-\. _]`),
		// "Sách" variations
		regexp.MustCompile(`(?i)s[a@4\-\. _]*[c\-\. _]*h`),
		// "Diễn viên" variations
		regexp.MustCompile(`(?i)d[i1!\-\. _]*[e3ê\-\. _]*n[\-\. _]+v[i1!\-\. _]*[e3ê\-\. _]*n`),
		// "Kịch bản" variations
		regexp.MustCompile(`(?i)k[i1!\-\. _]*[c\-\. _]*h[\-\. _]+b[a@4\-\. _]*n`),
	}

	// Corresponding penalty scores for each pattern
	km.negativeScores = []int{100, 100, 80, 90, 70, 70}

	return km
}

// AnalyzeText analyzes transcript text for fraud patterns
// This is the main entry point called from audio processor
func (fd *FraudDetector) AnalyzeText(text string) FraudAnalysisResult {
	fd.mu.Lock()
	defer fd.mu.Unlock()

	log.Printf("🔍 [%s] ===== FRAUD ANALYSIS START =====", fd.deviceID)
	log.Printf("🔍 [%s] Input text: '%s'", fd.deviceID, text)
	log.Printf("🔍 [%s] Current accumulated score: %d", fd.deviceID, fd.session.AccumulatedScore)

	// Update session
	fd.session.TranscriptHistory = append(fd.session.TranscriptHistory, text)
	fd.session.LastUpdateTime = time.Now()

	// Normalize text for matching
	normalizedText := strings.ToLower(text)
	log.Printf("🔍 [%s] Normalized text: '%s'", fd.deviceID, normalizedText)

	// Calculate risk score from keywords
	score, patterns := fd.calculateRiskScore(normalizedText)
	log.Printf("🔍 [%s] This chunk score: %d, Patterns detected: %v", fd.deviceID, score, patterns)

	// Add to accumulated score (allow negative to reduce total)
	fd.session.AccumulatedScore += score

	// Clamp accumulated score to minimum 0 (can't go negative)
	// This is CRITICAL for database constraints and confidence_score calculation
	if fd.session.AccumulatedScore < 0 {
		log.Printf("✅ [%s] Accumulated score would be negative (%d), clamping to 0 (legitimate content)",
			fd.deviceID, fd.session.AccumulatedScore)
		fd.session.AccumulatedScore = 0
	}

	fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, patterns...)

	// Determine alert level
	currentScore := fd.session.AccumulatedScore
	log.Printf("🔍 [%s] NEW accumulated score: %d (added %d)", fd.deviceID, currentScore, score)
	log.Printf("🔍 [%s] Thresholds - LOW:%d, MEDIUM:%d, HIGH:%d, CRITICAL:%d",
		fd.deviceID, fd.config.LowThreshold, fd.config.MediumThreshold,
		fd.config.HighThreshold, fd.config.CriticalThreshold)

	result := FraudAnalysisResult{
		RiskScore: currentScore,
		Patterns:  patterns,
	}

	// Gemini AI contextual analysis - runs async for semantic fraud detection
	// This catches sophisticated scams that keyword matching misses
	// Results feed BACK into the alert pipeline via sendAlert callback
	if GlobalGeminiClient != nil && GeminiCircuitBreaker.Allow() {
		// Capture sendAlert callback and current session state under lock
		alertCallback := fd.sendAlert
		currentScore := fd.session.AccumulatedScore
		sessionID := fd.session.SessionID

		go func(deviceID string, txt string, history []string) {
			defer func() {
				if r := recover(); r != nil {
					log.Printf("🔥 [%s] Recovered panic in Gemini analysis: %v", deviceID, r)
				}
			}()

			aiResult, err := GlobalGeminiClient.AnalyzeFraudContext(txt, history)
			if err != nil {
				log.Printf("⚠️ [%s] Gemini analysis error: %v", deviceID, err)
				GeminiCircuitBreaker.RecordFailure()
				return
			}
			GeminiCircuitBreaker.RecordSuccess()

			if aiResult == nil {
				return
			}

			log.Printf("🤖 [%s] Gemini result: IsFraud=%v, Score=%d, Type=%s, Confidence=%s",
				deviceID, aiResult.IsFraud, aiResult.RiskScore, aiResult.FraudType, aiResult.Confidence)

			// If Gemini detects fraud with significant confidence, boost the accumulated score
			// and send alert to mobile client
			if aiResult.IsFraud && aiResult.RiskScore > 30 {
				log.Printf("🤖🚨 [%s] Gemini AI fraud detected: Type=%s, Score=%d, Reason=%s",
					deviceID, aiResult.FraudType, aiResult.RiskScore, aiResult.Explanation)

				// Boost accumulated score with Gemini's assessment
				// Use a fraction of Gemini score to complement keyword scoring
				geminiBoost := aiResult.RiskScore / 2 // 50% of Gemini's score as boost
				fd.mu.Lock()
				// Only apply if still the same session
				if fd.session.SessionID == sessionID {
					fd.session.AccumulatedScore += geminiBoost
					fd.session.DetectedPatterns = append(fd.session.DetectedPatterns,
						fmt.Sprintf("AI: %s (+%d, confidence=%s)", aiResult.FraudType, geminiBoost, aiResult.Confidence))
					log.Printf("🤖 [%s] Score boosted by Gemini: +%d → total=%d",
						deviceID, geminiBoost, fd.session.AccumulatedScore)
				}
				fd.mu.Unlock()

				// Send alert to mobile client if callback is set
				if alertCallback != nil {
					// Determine alert level based on combined score
					combinedScore := currentScore + geminiBoost
					alertType := "MEDIUM"
					if combinedScore >= 80 || aiResult.RiskScore >= 80 {
						alertType = "CRITICAL"
					} else if combinedScore >= 60 || aiResult.RiskScore >= 60 {
						alertType = "HIGH"
					}

					alert := models.AlertMessage{
						Type:       "alert",
						AlertType:  alertType,
						Confidence: float64(aiResult.RiskScore) / 100.0,
						Transcript: txt,
						Keywords:   []string{fmt.Sprintf("AI: %s (%s)", aiResult.FraudType, aiResult.Confidence)},
						Timestamp:  time.Now().Unix(),
						Message: fmt.Sprintf("🤖 AI phát hiện: %s - %s (Độ tin cậy: %s)",
							aiResult.FraudType, aiResult.Explanation, aiResult.Confidence),
					}

					log.Printf("🤖📤 [%s] Sending Gemini AI alert to mobile: %s", deviceID, alert.Message)
					alertCallback(alert)
					log.Printf("🤖✅ [%s] Gemini AI alert sent to mobile successfully", deviceID)
				} else {
					log.Printf("⚠️ [%s] Gemini detected fraud but no alert callback set!", deviceID)
				}
			}
		}(fd.deviceID, text, fd.session.TranscriptHistory)
	}

	// Determine alert level based on accumulated score and config thresholds
	if currentScore >= fd.config.CriticalThreshold {
		result.IsAlert = true
		result.Action = "CRITICAL"
		result.Message = fmt.Sprintf("🚨 CẢNH BÁO NGHIÊM TRỌNG: Phát hiện dấu hiệu lừa đảo rất cao! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("🚨🚨🚨 [%s] CRITICAL ALERT TRIGGERED! Score=%d (threshold=%d), Patterns=%v",
			fd.deviceID, currentScore, fd.config.CriticalThreshold, patterns)
		log.Printf("🚨 [%s] Alert count: %d, Total patterns: %d",
			fd.deviceID, fd.alertCount, len(fd.session.DetectedPatterns))

	} else if currentScore >= fd.config.HighThreshold {
		result.IsAlert = true
		result.Action = "HIGH"
		result.Message = fmt.Sprintf("⚠️ CẢNH BÁO CAO: Cuộc gọi có dấu hiệu đáng ngờ! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("⚠️⚠️ [%s] HIGH ALERT TRIGGERED! Score=%d (threshold=%d), Patterns=%v",
			fd.deviceID, currentScore, fd.config.HighThreshold, patterns)
		log.Printf("⚠️ [%s] Alert count: %d", fd.deviceID, fd.alertCount)

	} else if currentScore >= fd.config.MediumThreshold {
		result.IsAlert = true
		result.Action = "MEDIUM"
		result.Message = fmt.Sprintf("⚡ CẢNH BÁO: Phát hiện một số dấu hiệu bất thường (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++

		log.Printf("⚡ [%s] MEDIUM ALERT TRIGGERED! Score=%d (threshold=%d), Patterns=%v",
			fd.deviceID, currentScore, fd.config.MediumThreshold, patterns)

	} else if currentScore >= fd.config.LowThreshold {
		result.IsAlert = false
		result.Action = "LOW"
		result.Message = fmt.Sprintf("ℹ️ Lưu ý: Có một số từ khóa đáng chú ý (Điểm rủi ro: %d/100)", currentScore)

		log.Printf("ℹ️ [%s] LOW RISK (no alert): Score=%d (threshold=%d), Patterns=%v",
			fd.deviceID, currentScore, fd.config.LowThreshold, patterns)

	} else {
		result.IsAlert = false
		result.Action = "SAFE"
		result.Message = "✅ Cuộc gọi bình thường"

		log.Printf("✅ [%s] SAFE (no alert): Score=%d (below threshold=%d)",
			fd.deviceID, currentScore, fd.config.LowThreshold)
	}

	log.Printf("🔍 [%s] ===== FRAUD ANALYSIS END: IsAlert=%v, Action=%s =====",
		fd.deviceID, result.IsAlert, result.Action)

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

	// Check NEGATIVE keywords (REDUCE score for legitimate content)
	// This prevents false positives when discussing fraud in movies, books, news, etc.
	for keyword, penalty := range fd.keywords.negativeKeywords {
		if strings.Contains(text, keyword) {
			totalScore -= penalty
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST: %s (-%d)", keyword, penalty))
			log.Printf("🟢 [%s] Negative keyword detected (legitimate content): '%s' (-%d points)",
				fd.deviceID, keyword, penalty)
		}
	}

	// Check NEGATIVE patterns (regex-based detection for obfuscated keywords)
	// This catches variations like "Ph1m", "P.h.i.m", "PhIm", etc.
	for i, pattern := range fd.keywords.negativePatterns {
		if pattern.MatchString(text) {
			penalty := fd.keywords.negativeScores[i]
			totalScore -= penalty
			match := pattern.FindString(text)
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST_REGEX: %s (-%d)", match, penalty))
			log.Printf("🟢 [%s] Negative pattern matched (obfuscated): '%s' (-%d points)",
				fd.deviceID, match, penalty)
		}
	}

	// NOTE: totalScore can be negative here, which is intentional
	// The accumulated score will be clamped to 0 in AnalyzeText() after summing
	// This allows negative keywords to properly reduce the accumulated risk score

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

	// Check if DB is available
	if db.Pool == nil {
		log.Printf("⚠️  Database not available - cannot process fraud report for %s", report.PhoneNumber)
		return
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Check if number already exists in blacklist
	var existingID int
	var reportedCount int
	err := db.Pool.QueryRow(ctx,
		"SELECT id, reported_count FROM blacklist WHERE phone_number = $1",
		report.PhoneNumber,
	).Scan(&existingID, &reportedCount)

	if err != nil {
		// Number not in blacklist, insert new entry
		// IMPORTANT: Don't specify 'id' - SERIAL auto-generates it
		_, err = db.Pool.Exec(ctx,
			`INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) 
			 VALUES ($1, $2, 0.50, 1, 'active')`,
			report.PhoneNumber, report.Reason,
		)
		if err != nil {
			log.Printf("❌ Error inserting blacklist entry: %v", err)
			return
		}
		log.Printf("✅ Added %s to blacklist (Reason: %s)", report.PhoneNumber, report.Reason)
	} else {
		// Number exists, increment report count and update confidence
		newCount := reportedCount + 1
		newConfidence := calculateConfidenceScore(newCount)

		_, err = db.Pool.Exec(ctx,
			`UPDATE blacklist 
			 SET reported_count = $1, confidence_score = $2, last_reported_at = NOW(), updated_at = NOW() 
			 WHERE id = $3`,
			newCount, newConfidence, existingID,
		)
		if err != nil {
			log.Printf("❌ Error updating blacklist entry: %v", err)
			return
		}
		log.Printf("✅ Updated %s in blacklist (Reports: %d, Confidence: %.2f)",
			report.PhoneNumber, newCount, newConfidence)
	}
}

// calculateConfidenceScore determines confidence based on report count
func calculateConfidenceScore(reportCount int) float64 {
	switch {
	case reportCount >= 10:
		return 0.95 // CRITICAL
	case reportCount >= 5:
		return 0.85 // HIGH
	case reportCount >= 2:
		return 0.70 // MEDIUM
	default:
		return 0.50 // LOW
	}
}

// CheckBlacklist checks if a phone number is in the blacklist
func CheckBlacklist(phoneNumber string) (*models.Blacklist, error) {
	// Check if DB is available
	if db.Pool == nil {
		log.Printf("⚠️  Database not available - cannot check blacklist for %s", phoneNumber)
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
		defer func() {
			if r := recover(); r != nil {
				log.Printf("🔥 [%s] Recovered panic in SaveCallLog: %v", fd.deviceID, r)
			}
		}()
		if err := repository.SaveCallLog(callLog); err != nil {
			log.Printf("❌ [%s] Failed to save call log: %v", fd.deviceID, err)
		} else {
			log.Printf("✅ [%s] Call log saved successfully (ID: %d)", fd.deviceID, callLog.ID)
		}
	}()
}
