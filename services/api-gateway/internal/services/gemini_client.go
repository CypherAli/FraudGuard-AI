package services

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"strings"
	"time"
)

// GeminiClient handles communication with Google Gemini API for advanced fraud detection
type GeminiClient struct {
	APIKey          string
	HTTPClient      *http.Client // For real-time fraud analysis (short timeout)
	SummaryClient   *http.Client // For post-call summary (longer timeout)
	enabled         bool
}

// GeminiAnalysisResult holds Gemini's fraud analysis
type GeminiAnalysisResult struct {
	IsFraud         bool     `json:"is_fraud"`
	Confidence      float64  `json:"confidence"`
	ScamType        string   `json:"scam_type"`
	UrgencyScore    int      `json:"urgency_score"`
	PressureTactics []string `json:"pressure_tactics"`
	Explanation     string   `json:"explanation"`
}

// Gemini API request/response structures
type geminiRequest struct {
	Contents         []geminiContent        `json:"contents"`
	GenerationConfig geminiGenerationConfig `json:"generationConfig"`
}

type geminiContent struct {
	Parts []geminiPart `json:"parts"`
}

type geminiPart struct {
	Text string `json:"text"`
}

type geminiGenerationConfig struct {
	Temperature     float64 `json:"temperature"`
	MaxOutputTokens int     `json:"maxOutputTokens"`
	ResponseMimeType string `json:"responseMimeType,omitempty"`
}

type geminiResponse struct {
	Candidates []struct {
		Content struct {
			Parts []struct {
				Text string `json:"text"`
			} `json:"parts"`
		} `json:"content"`
	} `json:"candidates"`
	Error *struct {
		Message string `json:"message"`
		Code    int    `json:"code"`
	} `json:"error,omitempty"`
}

// GeminiCircuitBreaker protects against Gemini API failures
var GeminiCircuitBreaker = NewCircuitBreaker("Gemini", 3, 60*time.Second)

// NewGeminiClient creates a new Gemini client
func NewGeminiClient(apiKey string) *GeminiClient {
	return &GeminiClient{
		APIKey: apiKey,
		HTTPClient: &http.Client{
			Timeout: 5 * time.Second, // Real-time fraud analysis: tight timeout
		},
		SummaryClient: &http.Client{
			Timeout: 15 * time.Second, // Post-call summary: longer timeout for larger responses
		},
		enabled: true,
	}
}

// IsEnabled returns whether the Gemini client is active
func (g *GeminiClient) IsEnabled() bool {
	return g.enabled && g.APIKey != ""
}

// AnalyzeFraud sends transcript to Gemini for semantic fraud analysis + urgency detection
func (g *GeminiClient) AnalyzeFraud(transcript string, contextHistory []string) (*GeminiAnalysisResult, error) {
	if !g.IsEnabled() {
		return nil, fmt.Errorf("gemini client not enabled")
	}

	if !GeminiCircuitBreaker.Allow() {
		return nil, fmt.Errorf("gemini circuit breaker open")
	}

	// Build context from previous transcripts
	contextStr := ""
	if len(contextHistory) > 0 {
		// Use last 5 transcripts for context (limit token usage)
		start := 0
		if len(contextHistory) > 5 {
			start = len(contextHistory) - 5
		}
		contextStr = "Ngữ cảnh trước đó: " + strings.Join(contextHistory[start:], " | ")
	}

	prompt := fmt.Sprintf(`Bạn là hệ thống phát hiện lừa đảo qua điện thoại tại Việt Nam.
Phân tích đoạn transcript cuộc gọi sau để xác định có phải là lừa đảo không.

Transcript mới nhất: "%s"
%s

Trả lời bằng JSON với format chính xác sau (không có markdown, không có text thừa):
{
  "is_fraud": true/false,
  "confidence": 0.0-1.0,
  "scam_type": "police_impersonation" | "bank_scam" | "lottery_scam" | "investment_scam" | "tech_support_scam" | "loan_scam" | "extortion" | "other" | "none",
  "urgency_score": 0-100,
  "pressure_tactics": ["tactic1", "tactic2"],
  "explanation": "giải thích ngắn gọn bằng tiếng Việt"
}

Lưu ý:
- is_fraud: true nếu có dấu hiệu lừa đảo (giả danh công an, ngân hàng, đe dọa, ép chuyển tiền...)
- urgency_score: cao nếu có áp lực thời gian, đe dọa, giục giã
- pressure_tactics: liệt kê các chiến thuật gây áp lực (ví dụ: "đe dọa bắt giữ", "yêu cầu chuyển tiền ngay")
- Nếu nội dung về phim/truyện/tin tức thì is_fraud=false, confidence=0`, transcript, contextStr)

	// Build Gemini API request
	reqBody := geminiRequest{
		Contents: []geminiContent{
			{
				Parts: []geminiPart{
					{Text: prompt},
				},
			},
		},
		GenerationConfig: geminiGenerationConfig{
			Temperature:     0.1,
			MaxOutputTokens: 300,
			ResponseMimeType: "application/json",
		},
	}

	bodyJSON, err := json.Marshal(reqBody)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal request: %w", err)
	}

	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=%s", g.APIKey)

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewReader(bodyJSON))
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	startTime := time.Now()
	resp, err := g.HTTPClient.Do(req)
	latency := time.Since(startTime)

	if err != nil {
		GeminiCircuitBreaker.RecordFailure()
		log.Printf("❌ [Gemini] Request failed (%.2fs): %v", latency.Seconds(), err)
		return nil, fmt.Errorf("gemini request failed: %w", err)
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("failed to read response: %w", err)
	}

	log.Printf("🤖 [Gemini] Response (%.2fs, status %d): %s", latency.Seconds(), resp.StatusCode, string(body))

	if resp.StatusCode != http.StatusOK {
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("gemini API error (status %d): %s", resp.StatusCode, string(body))
	}

	// Parse Gemini response
	var geminiResp geminiResponse
	if err := json.Unmarshal(body, &geminiResp); err != nil {
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("failed to parse gemini response: %w", err)
	}

	if geminiResp.Error != nil {
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("gemini API error: %s", geminiResp.Error.Message)
	}

	if len(geminiResp.Candidates) == 0 || len(geminiResp.Candidates[0].Content.Parts) == 0 {
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("empty response from gemini")
	}

	// Parse the JSON from Gemini's response text
	responseText := geminiResp.Candidates[0].Content.Parts[0].Text
	responseText = strings.TrimSpace(responseText)

	// Strip markdown code fences if present
	responseText = strings.TrimPrefix(responseText, "```json")
	responseText = strings.TrimPrefix(responseText, "```")
	responseText = strings.TrimSuffix(responseText, "```")
	responseText = strings.TrimSpace(responseText)

	var result GeminiAnalysisResult
	if err := json.Unmarshal([]byte(responseText), &result); err != nil {
		log.Printf("⚠️ [Gemini] Failed to parse analysis JSON: %v (raw: %s)", err, responseText)
		GeminiCircuitBreaker.RecordFailure()
		return nil, fmt.Errorf("failed to parse analysis result: %w", err)
	}

	GeminiCircuitBreaker.RecordSuccess()

	log.Printf("🤖 [Gemini] Analysis: IsFraud=%v, Confidence=%.2f, ScamType=%s, Urgency=%d",
		result.IsFraud, result.Confidence, result.ScamType, result.UrgencyScore)

	return &result, nil
}

// GenerateCallSummary generates a post-call summary using Gemini
func (g *GeminiClient) GenerateCallSummary(transcripts []string, riskScore int, patterns []string) (string, error) {
	if !g.IsEnabled() {
		return "", fmt.Errorf("gemini client not enabled")
	}

	if !GeminiCircuitBreaker.Allow() {
		return "", fmt.Errorf("gemini circuit breaker open")
	}

	fullTranscript := strings.Join(transcripts, " ")
	if len(fullTranscript) > 2000 {
		fullTranscript = fullTranscript[:2000] + "..."
	}

	patternStr := "Không có"
	if len(patterns) > 0 {
		patternStr = strings.Join(patterns, ", ")
	}

	prompt := fmt.Sprintf(`Tóm tắt cuộc gọi điện thoại này bằng tiếng Việt:

Nội dung: "%s"
Điểm rủi ro: %d/100
Mẫu phát hiện: %s

Viết tóm tắt ngắn gọn (3-5 câu) bao gồm:
1. Nội dung chính của cuộc gọi
2. Tại sao hệ thống đánh giá mức rủi ro này
3. Khuyến nghị cho người dùng

Chỉ trả về văn bản tóm tắt, không có markdown hay JSON.`, fullTranscript, riskScore, patternStr)

	reqBody := geminiRequest{
		Contents: []geminiContent{
			{Parts: []geminiPart{{Text: prompt}}},
		},
		GenerationConfig: geminiGenerationConfig{
			Temperature:     0.3,
			MaxOutputTokens: 500,
		},
	}

	bodyJSON, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=%s", g.APIKey)

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewReader(bodyJSON))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	// Use SummaryClient with longer timeout for post-call summary
	resp, err := g.SummaryClient.Do(req)
	if err != nil {
		GeminiCircuitBreaker.RecordFailure()
		return "", fmt.Errorf("gemini request failed: %w", err)
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		GeminiCircuitBreaker.RecordFailure()
		return "", fmt.Errorf("failed to read response: %w", err)
	}

	if resp.StatusCode != http.StatusOK {
		GeminiCircuitBreaker.RecordFailure()
		return "", fmt.Errorf("gemini API error (status %d)", resp.StatusCode)
	}

	var geminiResp geminiResponse
	if err := json.Unmarshal(body, &geminiResp); err != nil {
		GeminiCircuitBreaker.RecordFailure()
		return "", fmt.Errorf("failed to parse response: %w", err)
	}

	if len(geminiResp.Candidates) > 0 && len(geminiResp.Candidates[0].Content.Parts) > 0 {
		GeminiCircuitBreaker.RecordSuccess()
		return strings.TrimSpace(geminiResp.Candidates[0].Content.Parts[0].Text), nil
	}

	GeminiCircuitBreaker.RecordFailure()
	return "", fmt.Errorf("empty response from gemini")
}
