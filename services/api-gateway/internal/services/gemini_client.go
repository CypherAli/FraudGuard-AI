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
	APIKey        string
	HTTPClient    *http.Client // For real-time fraud analysis (short timeout)
	SummaryClient *http.Client // For post-call summary (longer timeout)
	Model         string
}

// GeminiRequest represents the request body for Gemini API
type GeminiRequest struct {
	Contents         []GeminiContent        `json:"contents"`
	GenerationConfig GeminiGenerationConfig `json:"generationConfig"`
	SafetySettings   []GeminiSafetySetting  `json:"safetySettings,omitempty"`
}

// GeminiContent represents content in Gemini API
type GeminiContent struct {
	Parts []GeminiPart `json:"parts"`
}

// GeminiPart represents a part of content
type GeminiPart struct {
	Text string `json:"text"`
}

// GeminiGenerationConfig holds generation settings
type GeminiGenerationConfig struct {
	Temperature      float64 `json:"temperature"`
	MaxOutputTokens  int     `json:"maxOutputTokens"`
	TopP             float64 `json:"topP,omitempty"`
	ResponseMimeType string  `json:"responseMimeType,omitempty"`
}

// GeminiSafetySetting represents safety configuration
type GeminiSafetySetting struct {
	Category  string `json:"category"`
	Threshold string `json:"threshold"`
}

// GeminiResponse represents the response from Gemini API
type GeminiResponse struct {
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

// GeminiAnalysisResult holds the parsed result from Gemini fraud analysis
type GeminiAnalysisResult struct {
	IsFraud     bool   `json:"is_fraud"`
	RiskScore   int    `json:"risk_score"`
	FraudType   string `json:"fraud_type"`
	Explanation string `json:"explanation"`
	Confidence  string `json:"confidence"`
}

// IsEnabled returns whether the Gemini client is active and configured
func (g *GeminiClient) IsEnabled() bool {
	return g != nil && g.APIKey != ""
}

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
		Model: "gemini-2.0-flash",
	}
}

// AnalyzeFraudContext performs contextual fraud analysis using Gemini AI.
// This provides deeper semantic understanding beyond keyword matching.
func (g *GeminiClient) AnalyzeFraudContext(transcript string, previousContext []string) (*GeminiAnalysisResult, error) {
	if transcript == "" {
		return nil, fmt.Errorf("empty transcript")
	}

	// Build context from previous transcripts
	contextStr := ""
	if len(previousContext) > 0 {
		// Only use last 5 transcripts for context (limit token usage)
		start := 0
		if len(previousContext) > 5 {
			start = len(previousContext) - 5
		}
		contextStr = "Ngữ cảnh cuộc trò chuyện trước đó:\n" + strings.Join(previousContext[start:], "\n") + "\n\n"
	}

	prompt := fmt.Sprintf(`Bạn là một chuyên gia phân tích lừa đảo qua điện thoại tại Việt Nam.
Phân tích đoạn hội thoại sau và xác định xem có dấu hiệu lừa đảo không.

%sĐoạn hội thoại mới nhất cần phân tích:
"%s"

Các dạng lừa đảo phổ biến tại Việt Nam:
1. Giả mạo công an/viện kiểm sát đe dọa bắt giữ
2. Lừa chuyển tiền/cung cấp OTP/mã xác nhận
3. Giả mạo ngân hàng thông báo tài khoản bị khóa
4. Yêu cầu cài app lạ (Anydesk, Teamviewer, Ultraviewer)
5. Trúng thưởng/khuyến mãi giả
6. Yêu cầu cung cấp CCCD/CMND/số tài khoản
7. Tạo áp lực thời gian ("trong 5 phút", "ngay lập tức")
8. Yêu cầu giữ bí mật ("đừng nói ai", "bí mật")
9. Lừa đầu tư/kiếm tiền dễ

Trả lời CHÍNH XÁC theo format JSON sau (không thêm bất kỳ text nào khác):
{"is_fraud": true/false, "risk_score": 0-100, "fraud_type": "tên loại lừa đảo hoặc safe", "explanation": "giải thích ngắn gọn", "confidence": "high/medium/low"}`, contextStr, transcript)

	// Build request with safety settings to allow fraud-related content analysis
	reqBody := GeminiRequest{
		Contents: []GeminiContent{
			{
				Parts: []GeminiPart{
					{Text: prompt},
				},
			},
		},
		GenerationConfig: GeminiGenerationConfig{
			Temperature:     0.1, // Low temperature for consistent analysis
			MaxOutputTokens: 256,
			TopP:            0.8,
		},
		SafetySettings: []GeminiSafetySetting{
			{Category: "HARM_CATEGORY_HARASSMENT", Threshold: "BLOCK_NONE"},
			{Category: "HARM_CATEGORY_HATE_SPEECH", Threshold: "BLOCK_NONE"},
			{Category: "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold: "BLOCK_NONE"},
			{Category: "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold: "BLOCK_NONE"},
		},
	}

	jsonBody, err := json.Marshal(reqBody)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal request: %w", err)
	}

	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/%s:generateContent?key=%s",
		g.Model, g.APIKey)

	req, err := http.NewRequest("POST", url, bytes.NewReader(jsonBody))
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	log.Printf("🤖 [Gemini] Analyzing transcript: '%s' (context: %d previous)", truncate(transcript, 80), len(previousContext))

	resp, err := g.HTTPClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("gemini API request failed: %w", err)
	}
	defer resp.Body.Close()

	body, readErr := io.ReadAll(resp.Body)
	if readErr != nil {
		log.Printf("❌ [Gemini] Failed to read response body: %v", readErr)
		return nil, fmt.Errorf("failed to read gemini response: %w", readErr)
	}

	if resp.StatusCode != http.StatusOK {
		log.Printf("❌ [Gemini] API error (status %d): %s", resp.StatusCode, string(body))
		return nil, fmt.Errorf("gemini API error (status %d)", resp.StatusCode)
	}

	// Parse response
	var geminiResp GeminiResponse
	if err := json.Unmarshal(body, &geminiResp); err != nil {
		return nil, fmt.Errorf("failed to parse gemini response: %w", err)
	}

	if geminiResp.Error != nil {
		return nil, fmt.Errorf("gemini error: %s", geminiResp.Error.Message)
	}

	if len(geminiResp.Candidates) == 0 || len(geminiResp.Candidates[0].Content.Parts) == 0 {
		return nil, fmt.Errorf("empty gemini response")
	}

	// Extract JSON from response text (handles markdown code fences)
	responseText := geminiResp.Candidates[0].Content.Parts[0].Text
	responseText = extractJSON(responseText)

	var result GeminiAnalysisResult
	if err := json.Unmarshal([]byte(responseText), &result); err != nil {
		log.Printf("⚠️ [Gemini] Failed to parse analysis result: %v, raw: %s", err, responseText)
		// Return safe result on parse error
		return &GeminiAnalysisResult{
			IsFraud:     false,
			RiskScore:   0,
			FraudType:   "parse_error",
			Explanation: "Không thể phân tích phản hồi AI",
			Confidence:  "low",
		}, nil
	}

	log.Printf("🤖 [Gemini] Analysis: IsFraud=%v, RiskScore=%d, Type=%s, Confidence=%s",
		result.IsFraud, result.RiskScore, result.FraudType, result.Confidence)

	return &result, nil
}

// GenerateCallSummary generates a post-call summary using Gemini.
// Uses a longer timeout (SummaryClient) for larger responses.
func (g *GeminiClient) GenerateCallSummary(transcripts []string, riskScore int, patterns []string) (string, error) {
	if g.APIKey == "" {
		return "", fmt.Errorf("gemini client not configured")
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

	reqBody := GeminiRequest{
		Contents: []GeminiContent{
			{Parts: []GeminiPart{{Text: prompt}}},
		},
		GenerationConfig: GeminiGenerationConfig{
			Temperature:     0.3,
			MaxOutputTokens: 500,
		},
	}

	jsonBody, err := json.Marshal(reqBody)
	if err != nil {
		return "", fmt.Errorf("failed to marshal request: %w", err)
	}

	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/%s:generateContent?key=%s",
		g.Model, g.APIKey)

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewReader(jsonBody))
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

	var geminiResp GeminiResponse
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

// extractJSON extracts JSON object from text that may contain markdown formatting
func extractJSON(text string) string {
	text = strings.TrimSpace(text)

	// Remove markdown code block markers
	text = strings.TrimPrefix(text, "```json")
	text = strings.TrimPrefix(text, "```")
	text = strings.TrimSuffix(text, "```")
	text = strings.TrimSpace(text)

	// Find JSON object boundaries
	start := strings.Index(text, "{")
	end := strings.LastIndex(text, "}")
	if start >= 0 && end > start {
		return text[start : end+1]
	}

	return text
}

// truncate truncates a string to max length with ellipsis
func truncate(s string, maxLen int) string {
	if len(s) <= maxLen {
		return s
	}
	return s[:maxLen] + "..."
}
