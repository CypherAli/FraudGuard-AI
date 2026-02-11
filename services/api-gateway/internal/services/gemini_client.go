package services

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"strings"

	"google.golang.org/genai"
)

// GeminiClient wraps the Google Gemini API for context-aware fraud analysis
type GeminiClient struct {
	client *genai.Client
	model  string
}

// FraudContextResult contains the AI analysis result
type FraudContextResult struct {
	IsActualFraud bool    `json:"is_fraud"`
	Confidence    float64 `json:"confidence"`
	ContextType   string  `json:"context_type"`
	Reasoning     string  `json:"reasoning"`
}

// NewGeminiClient creates a new Gemini AI client
func NewGeminiClient(apiKey string) (*GeminiClient, error) {
	ctx := context.Background()
	client, err := genai.NewClient(ctx, &genai.ClientConfig{
		APIKey:  apiKey,
		Backend: genai.BackendGeminiAPI,
	})
	if err != nil {
		return nil, fmt.Errorf("failed to create Gemini client: %w", err)
	}

	return &GeminiClient{
		client: client,
		model:  "gemini-2.0-flash-lite", // Fast, low-cost model with good multilingual support
	}, nil
}

// AnalyzeFraudContext uses Gemini to analyze if detected keywords indicate real fraud
func (gc *GeminiClient) AnalyzeFraudContext(
	ctx context.Context,
	currentText string,
	detectedPatterns []string,
	recentTranscripts []string,
) (*FraudContextResult, error) {
	// Build the analysis prompt
	prompt := buildFraudAnalysisPrompt(currentText, detectedPatterns, recentTranscripts)

	log.Printf("🤖 [Gemini] Analyzing fraud context (text length: %d, patterns: %d)",
		len(currentText), len(detectedPatterns))

	// Call Gemini API
	result, err := gc.client.Models.GenerateContent(ctx, gc.model, genai.Text(prompt), &genai.GenerateContentConfig{
		Temperature:     genai.Ptr(float32(0.1)), // Low temperature for consistent analysis
		MaxOutputTokens: 200, // Short response
	})
	if err != nil {
		return nil, fmt.Errorf("Gemini API error: %w", err)
	}

	// Extract text from response
	if result == nil || len(result.Candidates) == 0 || result.Candidates[0].Content == nil {
		return nil, fmt.Errorf("empty response from Gemini")
	}

	var responseText string
	for _, part := range result.Candidates[0].Content.Parts {
		if part.Text != "" {
			responseText += part.Text
		}
	}

	log.Printf("🤖 [Gemini] Raw response: %s", responseText)

	// Parse JSON response
	fraudResult, err := parseFraudResult(responseText)
	if err != nil {
		log.Printf("⚠️ [Gemini] Failed to parse response: %v", err)
		return nil, fmt.Errorf("failed to parse Gemini response: %w", err)
	}

	log.Printf("🤖 [Gemini] Analysis: is_fraud=%v, confidence=%.2f, context=%s, reasoning=%s",
		fraudResult.IsActualFraud, fraudResult.Confidence, fraudResult.ContextType, fraudResult.Reasoning)

	return fraudResult, nil
}

// buildFraudAnalysisPrompt creates the prompt for Gemini fraud analysis
func buildFraudAnalysisPrompt(currentText string, detectedPatterns []string, recentTranscripts []string) string {
	history := ""
	if len(recentTranscripts) > 0 {
		history = strings.Join(recentTranscripts, " | ")
	}

	patterns := strings.Join(detectedPatterns, ", ")

	return fmt.Sprintf(`Bạn là chuyên gia phân tích lừa đảo qua điện thoại tại Việt Nam.

Phân tích đoạn hội thoại điện thoại sau và xác định đây có phải là cuộc gọi lừa đảo THẬT hay chỉ là ngữ cảnh bình thường (nói về phim, tin tức, giáo dục, công việc hợp pháp).

Từ khóa phát hiện: %s
Lịch sử hội thoại gần đây: %s
Đoạn mới nhất: %s

Quy tắc phân tích:
- Nếu người gọi YÊU CẦU chuyển tiền, cung cấp mã OTP, hoặc cài phần mềm điều khiển từ xa -> LỪA ĐẢO
- Nếu có áp lực thời gian (gấp, ngay lập tức) kết hợp yêu cầu tài chính -> LỪA ĐẢO
- Nếu nhắc đến công an/cảnh sát kết hợp đe dọa bắt giữ và yêu cầu tiền -> LỪA ĐẢO
- Nếu đang thảo luận/kể về phim, truyện, tin tức, bài giảng có chứa từ khóa lừa đảo -> KHÔNG PHẢI LỪA ĐẢO
- Nếu đang nói chuyện bình thường về ngân hàng, thuế, bảo hiểm -> KHÔNG PHẢI LỪA ĐẢO
- Nếu có người cảnh báo/khuyên về lừa đảo -> KHÔNG PHẢI LỪA ĐẢO

Trả lời CHÍNH XÁC bằng JSON (không markdown, không giải thích thêm):
{"is_fraud": true/false, "confidence": 0.0-1.0, "context_type": "fraud_attempt|movie|news|education|work|warning|legitimate", "reasoning": "giải thích ngắn gọn bằng tiếng Việt"}`, patterns, history, currentText)
}

// parseFraudResult extracts the JSON result from Gemini's response
func parseFraudResult(response string) (*FraudContextResult, error) {
	// Clean response - remove markdown code blocks if present
	response = strings.TrimSpace(response)
	response = strings.TrimPrefix(response, "```json")
	response = strings.TrimPrefix(response, "```")
	response = strings.TrimSuffix(response, "```")
	response = strings.TrimSpace(response)

	// Try to find JSON in the response
	startIdx := strings.Index(response, "{")
	endIdx := strings.LastIndex(response, "}")
	if startIdx == -1 || endIdx == -1 || endIdx <= startIdx {
		return nil, fmt.Errorf("no JSON found in response: %s", response)
	}
	jsonStr := response[startIdx : endIdx+1]

	var result FraudContextResult
	if err := json.Unmarshal([]byte(jsonStr), &result); err != nil {
		return nil, fmt.Errorf("JSON parse error: %w (raw: %s)", err, jsonStr)
	}

	// Validate confidence range
	if result.Confidence < 0 {
		result.Confidence = 0
	}
	if result.Confidence > 1 {
		result.Confidence = 1
	}

	return &result, nil
}
