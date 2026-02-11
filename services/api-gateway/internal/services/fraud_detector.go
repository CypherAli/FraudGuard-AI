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

// Pre-compiled regex patterns (compile once, reuse across all detectors)
var (
	globalNegativePatterns []*regexp.Regexp
	globalNegativeScores  []int
	patternsOnce          sync.Once
)

func initGlobalPatterns() {
	patternsOnce.Do(func() {
		globalNegativePatterns = []*regexp.Regexp{
			regexp.MustCompile(`(?i)p[h\-\. _]*[h1i!][\-\. _]*[i1!][\-\. _]*m`),
			regexp.MustCompile(`(?i)t[r\-\. _]*[u\-\. _]*[y\-\. _]*[e3ê\-\. _]*[n\-\. _]`),
			regexp.MustCompile(`(?i)t[i1!\-\. _]*n[\-\. _]+t[u\-\. _]*[c\-\. _]`),
			regexp.MustCompile(`(?i)s[a@4\-\. _]*[c\-\. _]*h`),
			regexp.MustCompile(`(?i)d[i1!\-\. _]*[e3ê\-\. _]*n[\-\. _]+v[i1!\-\. _]*[e3ê\-\. _]*n`),
			regexp.MustCompile(`(?i)k[i1!\-\. _]*[c\-\. _]*h[\-\. _]+b[a@4\-\. _]*n`),
		}
		globalNegativeScores = []int{100, 100, 80, 90, 70, 70}
	})
}

// FraudDetector handles real-time fraud detection with accumulated risk scoring
type FraudDetector struct {
	deviceID       string
	session        *SessionState
	mu             sync.RWMutex
	keywords       *KeywordMatcher
	alertCount     int
	startTime      time.Time
	config         *FraudDetectionConfig
	lastGeminiCall time.Time // Rate limit Gemini API calls
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
	// Track per-chunk scores with timestamps for decay
	ScoreHistory []ScoredChunk
	// Track if negative context was established early in conversation
	HasNegativeContext bool
}

// ScoredChunk tracks a score with its timestamp for decay calculation
type ScoredChunk struct {
	Score     int
	Timestamp time.Time
	Patterns  []string
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
}

// FraudCombination defines keyword combinations that strongly indicate fraud
type FraudCombination struct {
	Keywords []string
	Bonus    int
}

// Known fraud keyword combinations (require 2+ indicators together)
var fraudCombinations = []FraudCombination{
	{Keywords: []string{"chuyển tiền", "ngay lập tức"}, Bonus: 30},
	{Keywords: []string{"chuyển tiền", "công an"}, Bonus: 25},
	{Keywords: []string{"chuyển tiền", "bị bắt"}, Bonus: 30},
	{Keywords: []string{"chuyển khoản", "gấp lắm"}, Bonus: 25},
	{Keywords: []string{"số tài khoản", "chuyển tiền"}, Bonus: 30},
	{Keywords: []string{"mã otp", "chuyển tiền"}, Bonus: 35},
	{Keywords: []string{"công an", "rửa tiền"}, Bonus: 30},
	{Keywords: []string{"công an", "bị bắt"}, Bonus: 25},
	{Keywords: []string{"tài khoản bị đóng băng", "chuyển tiền"}, Bonus: 35},
	{Keywords: []string{"bí mật", "chuyển tiền"}, Bonus: 30},
	{Keywords: []string{"đừng nói ai", "chuyển tiền"}, Bonus: 35},
	{Keywords: []string{"anydesk", "chuyển tiền"}, Bonus: 40},
	{Keywords: []string{"teamviewer", "số tài khoản"}, Bonus: 40},
	{Keywords: []string{"trúng thưởng", "chuyển tiền"}, Bonus: 35},
	{Keywords: []string{"cccd", "chuyển tiền"}, Bonus: 30},
	{Keywords: []string{"liên quan đến vụ án", "chuyển tiền"}, Bonus: 35},
}

// Score decay constants
const (
	scoreDecayInterval = 60 * time.Second // Decay scores older than 60s
	scoreDecayFactor   = 0.7              // Retain 70% of old scores
)

// NewFraudDetector creates a new fraud detector for a session
func NewFraudDetector(deviceID string) *FraudDetector {
	initGlobalPatterns()
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
	initGlobalPatterns()
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
		ScoreHistory:      make([]ScoredChunk, 0),
		StartTime:         time.Now(),
		LastUpdateTime:    time.Now(),
		AlertsSent:        0,
	}
}

// initializeKeywordMatcher sets up keyword patterns for fraud detection
func initializeKeywordMatcher() *KeywordMatcher {
	return &KeywordMatcher{
		// Critical keywords - Reduced base scores (boosted by combinations)
		// Single keyword alone = half score; combination with pressure tactics = full score
		criticalKeywords: map[string]int{
			"chuyển tiền":  25, // Was 50, now 25 alone (boosted by combos)
			"chuyển khoản": 25,
			"mã otp":       30, // Asking for OTP is always suspicious
			"mã xác nhận":  30,
			"số tài khoản": 20, // Was 40
			"thẻ tín dụng": 25,
			"thẻ atm":      25,
			"cccd":         20, // Was 35
			"cmnd":         20,
			"căn cước":     20,
			"bị bắt":       25, // Was 40
			"bị tạm giữ":   25,
			"lệnh bắt":     25,
			"truy nã":      30,
			"cài app":      25,
			"cài ứng dụng": 25,
			"tải app":      25,
			"anydesk":      40, // Remote access tools remain high
			"teamviewer":   40,
			"ultraviewer":  40,
		},

		// Warning keywords - Medium risk
		warningKeywords: map[string]int{
			"công an":          15, // Was 25, reduced for context awareness
			"cảnh sát":         15,
			"viện kiểm sát":    20,
			"tòa án":           20,
			"ngân hàng":        10, // Was 20, very common in legitimate calls
			"vietcombank":      10,
			"techcombank":      10,
			"bidv":             10,
			"agribank":         10,
			"bảo hiểm xã hội": 15,
			"bhxh":             15,
			"thuế":             15,
			"cục thuế":         15,
			"điện lực":         10,
			"evn":              10,
			"bưu điện":         10,
			"viettel":          10,
			"mobifone":         10,
			"vinaphone":        10,
			"trúng thưởng":     20,
			"giải thưởng":      15,
			"khuyến mãi":       10,
		},

		// Suspicious phrases - Pressure tactics (these are strong fraud indicators)
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
			"lừa đảo":                20, // Reduced - could be someone warning about fraud
			"bí mật":                 20, // Was 25
			"không được nói":         25,
			"đừng nói ai":            30,
			"giữ bí mật":             25,
			"lợi nhuận cao":          25,
			"kiếm tiền dễ dàng":      25,
			"thu nhập thêm":          15, // Was 20
			"làm việc tại nhà":       10, // Was 15
		},

		// Negative keywords - Legitimate context indicators
		negativeKeywords: map[string]int{
			"phim":         80,
			"phim ảnh":     90,
			"bộ phim":      90,
			"rạp chiếu":    80,
			"truyện":       80,
			"tiểu thuyết":  90,
			"truyện tranh": 90,
			"truyện ngắn":  80,
			"tin tức":      70,
			"bản tin":      70,
			"thời sự":      70,
			"báo chí":      60,
			"sách":         70,
			"giáo dục":     50,
			"học tập":      40,
			"giảng dạy":    50,
			"bài giảng":    50,
			"thảo luận":    40,
			"phân tích":    30,
			"nghiên cứu":   40,
			"diễn viên":    60,
			"đạo diễn":     60,
			"kịch bản":     60,
			"câu chuyện":   30,
			"drama":        50,
			"series":       50,
		},
	}
}

// AnalyzeText analyzes transcript text for fraud patterns
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

	// Step 1: Apply score decay to old chunks
	fd.applyScoreDecay()

	// Step 2: Calculate risk score from keywords
	score, patterns := fd.calculateRiskScore(normalizedText)

	// Step 3: Check for fraud combinations (bonus points for multi-indicator patterns)
	comboBonus, comboPatterns := fd.checkFraudCombinations(normalizedText)
	score += comboBonus
	patterns = append(patterns, comboPatterns...)

	log.Printf("🔍 [%s] This chunk score: %d (combo bonus: %d), Patterns: %v",
		fd.deviceID, score, comboBonus, patterns)

	// Step 4: Record this chunk's score for future decay
	fd.session.ScoreHistory = append(fd.session.ScoreHistory, ScoredChunk{
		Score:     score,
		Timestamp: time.Now(),
		Patterns:  patterns,
	})

	// Step 5: Recalculate accumulated score from all chunks (with decay applied)
	fd.recalculateAccumulatedScore()

	fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, patterns...)

	// Step 6: Gemini AI context analysis (if available and score is concerning)
	currentScore := fd.session.AccumulatedScore
	if GlobalGeminiClient != nil && currentScore >= fd.config.MediumThreshold && score > 0 {
		aiAdjustment, aiPatterns := fd.analyzeWithGemini(normalizedText, patterns)
		if aiAdjustment != 0 {
			currentScore += aiAdjustment
			if currentScore < 0 {
				currentScore = 0
			}
			fd.session.AccumulatedScore = currentScore
			fd.session.DetectedPatterns = append(fd.session.DetectedPatterns, aiPatterns...)
			patterns = append(patterns, aiPatterns...)
		}
	}

	log.Printf("🔍 [%s] NEW accumulated score: %d (chunk added %d)", fd.deviceID, currentScore, score)
	log.Printf("🔍 [%s] Thresholds - LOW:%d, MEDIUM:%d, HIGH:%d, CRITICAL:%d",
		fd.deviceID, fd.config.LowThreshold, fd.config.MediumThreshold,
		fd.config.HighThreshold, fd.config.CriticalThreshold)

	result := FraudAnalysisResult{
		RiskScore: currentScore,
		Patterns:  patterns,
	}

	// Determine alert level based on accumulated score
	if currentScore >= fd.config.CriticalThreshold {
		result.IsAlert = true
		result.Action = "CRITICAL"
		result.Message = fmt.Sprintf("🚨 CẢNH BÁO NGHIÊM TRỌNG: Phát hiện dấu hiệu lừa đảo rất cao! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++
		log.Printf("🚨🚨🚨 [%s] CRITICAL ALERT! Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.HighThreshold {
		result.IsAlert = true
		result.Action = "HIGH"
		result.Message = fmt.Sprintf("⚠️ CẢNH BÁO CAO: Cuộc gọi có dấu hiệu đáng ngờ! (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++
		log.Printf("⚠️⚠️ [%s] HIGH ALERT! Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.MediumThreshold {
		result.IsAlert = true
		result.Action = "MEDIUM"
		result.Message = fmt.Sprintf("⚡ CẢNH BÁO: Phát hiện một số dấu hiệu bất thường (Điểm rủi ro: %d/100)", currentScore)
		fd.session.AlertsSent++
		fd.alertCount++
		log.Printf("⚡ [%s] MEDIUM ALERT! Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)

	} else if currentScore >= fd.config.LowThreshold {
		result.IsAlert = false
		result.Action = "LOW"
		result.Message = fmt.Sprintf("ℹ️ Lưu ý: Có một số từ khóa đáng chú ý (Điểm rủi ro: %d/100)", currentScore)
		log.Printf("ℹ️ [%s] LOW RISK: Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)

	} else {
		result.IsAlert = false
		result.Action = "SAFE"
		result.Message = "✅ Cuộc gọi bình thường"
		log.Printf("✅ [%s] SAFE: Score=%d", fd.deviceID, currentScore)
	}

	log.Printf("🔍 [%s] ===== FRAUD ANALYSIS END: IsAlert=%v, Action=%s =====",
		fd.deviceID, result.IsAlert, result.Action)

	return result
}

// applyScoreDecay reduces scores of old chunks to prevent stale data from causing false positives
func (fd *FraudDetector) applyScoreDecay() {
	now := time.Now()
	for i := range fd.session.ScoreHistory {
		chunk := &fd.session.ScoreHistory[i]
		age := now.Sub(chunk.Timestamp)
		if age > scoreDecayInterval && chunk.Score > 0 {
			oldScore := chunk.Score
			chunk.Score = int(float64(chunk.Score) * scoreDecayFactor)
			if chunk.Score < 1 {
				chunk.Score = 0
			}
			if oldScore != chunk.Score {
				log.Printf("📉 [%s] Score decay: %d -> %d (age: %v)",
					fd.deviceID, oldScore, chunk.Score, age.Round(time.Second))
			}
		}
	}
}

// recalculateAccumulatedScore sums all chunk scores
func (fd *FraudDetector) recalculateAccumulatedScore() {
	total := 0
	for _, chunk := range fd.session.ScoreHistory {
		total += chunk.Score
	}
	if total < 0 {
		total = 0
	}
	// Cap at 100 to prevent overflow
	if total > 100 {
		total = 100
	}
	fd.session.AccumulatedScore = total
}

// checkFraudCombinations checks for known fraud keyword combinations
func (fd *FraudDetector) checkFraudCombinations(text string) (int, []string) {
	totalBonus := 0
	var patterns []string

	// Also check against recent transcript history for cross-chunk combinations
	recentText := text
	if len(fd.session.TranscriptHistory) > 1 {
		// Include last 3 chunks for combination checking
		start := len(fd.session.TranscriptHistory) - 3
		if start < 0 {
			start = 0
		}
		recentText = strings.Join(fd.session.TranscriptHistory[start:], " ")
		recentText = strings.ToLower(recentText)
	}

	for _, combo := range fraudCombinations {
		allFound := true
		for _, kw := range combo.Keywords {
			if !strings.Contains(recentText, kw) {
				allFound = false
				break
			}
		}
		if allFound {
			totalBonus += combo.Bonus
			patterns = append(patterns,
				fmt.Sprintf("COMBO: [%s] (+%d)", strings.Join(combo.Keywords, " + "), combo.Bonus))
			log.Printf("🔥 [%s] Fraud combination detected: [%s] (+%d bonus)",
				fd.deviceID, strings.Join(combo.Keywords, " + "), combo.Bonus)
		}
	}

	return totalBonus, patterns
}

// analyzeWithGemini uses Gemini AI for context-aware fraud analysis
func (fd *FraudDetector) analyzeWithGemini(text string, detectedPatterns []string) (int, []string) {
	// Rate limit: max 1 call per 5 seconds per device
	if time.Since(fd.lastGeminiCall) < 5*time.Second {
		log.Printf("⏳ [%s] Gemini rate limited, skipping", fd.deviceID)
		return 0, nil
	}
	fd.lastGeminiCall = time.Now()

	// Build context from recent transcript history
	var recentTranscripts []string
	start := len(fd.session.TranscriptHistory) - 3
	if start < 0 {
		start = 0
	}
	recentTranscripts = fd.session.TranscriptHistory[start:]

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	result, err := GlobalGeminiClient.AnalyzeFraudContext(ctx, text, detectedPatterns, recentTranscripts)
	if err != nil {
		log.Printf("⚠️ [%s] Gemini analysis failed: %v", fd.deviceID, err)
		return 0, nil
	}

	var adjustment int
	var patterns []string

	if !result.IsActualFraud && result.Confidence > 0.7 {
		// Gemini says this is NOT fraud (legitimate context) with high confidence
		// Reduce score significantly
		adjustment = -int(float64(fd.session.AccumulatedScore) * 0.6)
		patterns = append(patterns,
			fmt.Sprintf("AI_CONTEXT: %s (confidence: %.0f%%, adjustment: %d)",
				result.ContextType, result.Confidence*100, adjustment))
		log.Printf("🤖 [%s] Gemini: LEGITIMATE context '%s' (%.0f%%), reducing score by %d",
			fd.deviceID, result.ContextType, result.Confidence*100, -adjustment)
	} else if result.IsActualFraud && result.Confidence > 0.8 {
		// Gemini confirms fraud with high confidence
		adjustment = int(float64(fd.session.AccumulatedScore) * 0.2)
		if adjustment < 10 {
			adjustment = 10
		}
		patterns = append(patterns,
			fmt.Sprintf("AI_FRAUD: confirmed (confidence: %.0f%%, +%d)",
				result.Confidence*100, adjustment))
		log.Printf("🤖 [%s] Gemini: FRAUD confirmed (%.0f%%), boosting score by %d",
			fd.deviceID, result.Confidence*100, adjustment)
	}

	return adjustment, patterns
}

// calculateRiskScore calculates risk score based on keyword matching
func (fd *FraudDetector) calculateRiskScore(text string) (int, []string) {
	totalScore := 0
	detectedPatterns := make([]string, 0)
	hasFraudKeywords := false

	// Check critical keywords
	for keyword, score := range fd.keywords.criticalKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			hasFraudKeywords = true
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("CRITICAL: %s (+%d)", keyword, score))
			log.Printf("🔴 [%s] Critical keyword: '%s' (+%d)", fd.deviceID, keyword, score)
		}
	}

	// Check warning keywords
	for keyword, score := range fd.keywords.warningKeywords {
		if strings.Contains(text, keyword) {
			totalScore += score
			hasFraudKeywords = true
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WARNING: %s (+%d)", keyword, score))
			log.Printf("🟡 [%s] Warning keyword: '%s' (+%d)", fd.deviceID, keyword, score)
		}
	}

	// Check suspicious phrases
	for phrase, score := range fd.keywords.suspiciousPhrases {
		if strings.Contains(text, phrase) {
			totalScore += score
			hasFraudKeywords = true
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("SUSPICIOUS: %s (+%d)", phrase, score))
			log.Printf("🟠 [%s] Suspicious phrase: '%s' (+%d)", fd.deviceID, phrase, score)
		}
	}

	// Check NEGATIVE keywords (reduce score for legitimate content)
	// IMPROVED: Only apply negative keywords if they appear contextually
	// If fraud keywords were NOT found in this chunk, negative keywords have full effect
	// If fraud keywords WERE found, negative keywords have reduced effect (scammer defense)
	for keyword, penalty := range fd.keywords.negativeKeywords {
		if strings.Contains(text, keyword) {
			effectivePenalty := penalty
			if hasFraudKeywords {
				// Reduce negative keyword effectiveness when fraud keywords are also present
				// This prevents scammers from saying "phim" to lower their score
				effectivePenalty = penalty / 3
				detectedPatterns = append(detectedPatterns,
					fmt.Sprintf("WHITELIST_REDUCED: %s (-%d, reduced from -%d due to fraud keywords)", keyword, effectivePenalty, penalty))
				log.Printf("🟡 [%s] Negative keyword '%s' reduced (-%d, was -%d) - fraud keywords present",
					fd.deviceID, keyword, effectivePenalty, penalty)
			} else {
				// No fraud keywords = likely legitimate context, full penalty
				detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST: %s (-%d)", keyword, effectivePenalty))
				log.Printf("🟢 [%s] Negative keyword: '%s' (-%d) - legitimate context",
					fd.deviceID, keyword, effectivePenalty)
				// Mark session as having negative context
				fd.session.HasNegativeContext = true
			}
			totalScore -= effectivePenalty
		}
	}

	// Check NEGATIVE patterns (regex-based)
	for i, pattern := range globalNegativePatterns {
		if pattern.MatchString(text) {
			penalty := globalNegativeScores[i]
			if hasFraudKeywords {
				penalty = penalty / 3
			}
			totalScore -= penalty
			match := pattern.FindString(text)
			detectedPatterns = append(detectedPatterns, fmt.Sprintf("WHITELIST_REGEX: %s (-%d)", match, penalty))
			log.Printf("🟢 [%s] Negative pattern: '%s' (-%d)", fd.deviceID, match, penalty)
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

	if db.Pool == nil {
		log.Printf("⚠️  Database not available - cannot process fraud report for %s", report.PhoneNumber)
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
			log.Printf("❌ Error inserting blacklist entry: %v", err)
			return
		}
		log.Printf("✅ Added %s to blacklist (Reason: %s)", report.PhoneNumber, report.Reason)
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

	evidence := strings.Builder{}

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
		evidenceStr = evidenceStr[:1000] + "... [truncated]"
	}

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

	log.Printf("🛑 [%s] Session ended - Duration: %ds, RiskScore: %d, IsFraud: %v, Alerts: %d",
		fd.deviceID, duration, session.AccumulatedScore, isFraud, session.AlertsSent)

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
