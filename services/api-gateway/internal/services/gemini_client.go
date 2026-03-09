package services

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
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

// GeminiContent represents content in Gemini API.
// Role should be "user" or "model" for multi-turn / function-calling conversations.
type GeminiContent struct {
	Role  string       `json:"role,omitempty"`
	Parts []GeminiPart `json:"parts"`
}

// GeminiPart represents a part of content.
// Only one field should be non-zero per part: Text, FunctionCall, or FunctionResponse.
// FunctionCall and FunctionResponse are defined in gemini_agent.go (same package).
type GeminiPart struct {
	Text             string            `json:"text,omitempty"`
	FunctionCall     *FunctionCall     `json:"functionCall,omitempty"`
	FunctionResponse *FunctionResponse `json:"functionResponse,omitempty"`
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

