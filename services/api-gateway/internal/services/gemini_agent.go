package services

// gemini_agent.go — Agentic AI layer cho FraudGuard
//
// Thay vì chỉ classify text (reactive), Gemini Agent tự:
//  1. Nhận transcript + context
//  2. Quyết định cần dùng tool nào
//  3. Gọi tool (check_blacklist, get_call_history, auto_report)
//  4. Dùng kết quả tool để ra quyết định cuối

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

	"github.com/fraudguard/api-gateway/internal/db"
)

// --- Tool definitions cho Gemini Function Calling ---

// AgentTool mô tả một tool mà Gemini có thể gọi
type AgentTool struct {
	FunctionDeclarations []FunctionDeclaration `json:"functionDeclarations"`
}

type FunctionDeclaration struct {
	Name        string          `json:"name"`
	Description string          `json:"description"`
	Parameters  FunctionParams  `json:"parameters"`
}

type FunctionParams struct {
	Type       string              `json:"type"`
	Properties map[string]Property `json:"properties"`
	Required   []string            `json:"required"`
}

type Property struct {
	Type        string `json:"type"`
	Description string `json:"description"`
}

// --- Request/Response cho Agentic loop ---

type AgentRequest struct {
	Contents         []GeminiContent        `json:"contents"`
	Tools            []AgentTool            `json:"tools"`
	GenerationConfig GeminiGenerationConfig `json:"generationConfig"`
}

type AgentResponse struct {
	Candidates []struct {
		Content struct {
			Parts []AgentPart `json:"parts"`
			Role  string      `json:"role"`
		} `json:"content"`
		FinishReason string `json:"finishReason"`
	} `json:"candidates"`
	Error *struct {
		Message string `json:"message"`
		Code    int    `json:"code"`
	} `json:"error,omitempty"`
}

// AgentPart có thể là text hoặc function call
type AgentPart struct {
	Text         string        `json:"text,omitempty"`
	FunctionCall *FunctionCall `json:"functionCall,omitempty"`
}

type FunctionCall struct {
	Name string                 `json:"name"`
	Args map[string]interface{} `json:"args"`
}

// FunctionResponse để trả kết quả tool về cho Gemini
type FunctionResponse struct {
	Name     string                 `json:"name"`
	Response map[string]interface{} `json:"response"`
}

// AgentAnalysisResult kết quả từ agentic loop
type AgentAnalysisResult struct {
	IsFraud      bool     `json:"is_fraud"`
	RiskScore    int      `json:"risk_score"`
	FraudType    string   `json:"fraud_type"`
	Explanation  string   `json:"explanation"`
	Confidence   string   `json:"confidence"`
	ToolsUsed    []string `json:"tools_used"`    // tools Gemini đã gọi
	AutoReported bool     `json:"auto_reported"` // có tự report vào blacklist không
}

// --- Tool definitions cố định ---

var fraudDetectionTools = []AgentTool{
	{
		FunctionDeclarations: []FunctionDeclaration{
			{
				Name:        "check_blacklist",
				Description: "Kiểm tra xem số điện thoại có trong danh sách đen lừa đảo không. Gọi tool này khi phát hiện số điện thoại trong transcript.",
				Parameters: FunctionParams{
					Type: "object",
					Properties: map[string]Property{
						"phone_number": {
							Type:        "string",
							Description: "Số điện thoại cần kiểm tra, định dạng quốc tế hoặc nội địa VN (ví dụ: +84912345678 hoặc 0912345678)",
						},
					},
					Required: []string{"phone_number"},
				},
			},
			{
				Name:        "get_fraud_stats",
				Description: "Lấy thống kê lừa đảo gần đây từ blacklist: tổng số, tỷ lệ, loại phổ biến nhất. Dùng để đánh giá mức độ nguy hiểm của cuộc gọi.",
				Parameters: FunctionParams{
					Type:       "object",
					Properties: map[string]Property{},
					Required:   []string{},
				},
			},
			{
				Name:        "auto_report_fraud",
				Description: "Tự động báo cáo số điện thoại lừa đảo vào blacklist cộng đồng khi có bằng chứng rõ ràng (độ tin cậy cao). Chỉ gọi khi CHẮC CHẮN là lừa đảo.",
				Parameters: FunctionParams{
					Type: "object",
					Properties: map[string]Property{
						"phone_number": {
							Type:        "string",
							Description: "Số điện thoại cần report",
						},
						"fraud_type": {
							Type:        "string",
							Description: "Loại lừa đảo phát hiện (ví dụ: giả mạo công an, lừa chuyển tiền, deepfake...)",
						},
						"evidence": {
							Type:        "string",
							Description: "Bằng chứng ngắn gọn từ transcript (tối đa 200 ký tự)",
						},
					},
					Required: []string{"phone_number", "fraud_type", "evidence"},
				},
			},
		},
	},
}

// --- Tool execution functions ---

// executeTool thực thi tool mà Gemini yêu cầu gọi
func executeTool(toolName string, args map[string]interface{}) map[string]interface{} {
	switch toolName {
	case "check_blacklist":
		return executeCheckBlacklist(args)
	case "get_fraud_stats":
		return executeGetFraudStats()
	case "auto_report_fraud":
		return executeAutoReportFraud(args)
	default:
		return map[string]interface{}{
			"error": fmt.Sprintf("unknown tool: %s", toolName),
		}
	}
}

// executeCheckBlacklist kiểm tra số điện thoại trong blacklist PostgreSQL
func executeCheckBlacklist(args map[string]interface{}) map[string]interface{} {
	phone, ok := args["phone_number"].(string)
	if !ok || phone == "" {
		return map[string]interface{}{
			"found":   false,
			"message": "Không có số điện thoại để kiểm tra",
		}
	}

	bl, err := CheckBlacklist(phone)
	if err != nil {
		// Số không có trong blacklist
		return map[string]interface{}{
			"found":        false,
			"phone_number": phone,
			"message":      "Số điện thoại không có trong danh sách đen",
		}
	}

	log.Printf("🔍 [Agent:check_blacklist] Found %s in blacklist (confidence=%.2f, reports=%d)",
		phone, bl.ConfidenceScore, bl.ReportedCount)

	return map[string]interface{}{
		"found":            true,
		"phone_number":     phone,
		"reason":           bl.Reason,
		"confidence_score": bl.ConfidenceScore,
		"reported_count":   bl.ReportedCount,
		"status":           bl.Status,
		"message":          fmt.Sprintf("CẢNH BÁO: Số %s đã bị báo cáo %d lần, độ tin cậy %.0f%%", phone, bl.ReportedCount, bl.ConfidenceScore*100),
	}
}

// executeGetFraudStats lấy thống kê blacklist
func executeGetFraudStats() map[string]interface{} {
	if db.Pool == nil {
		return map[string]interface{}{
			"error": "Database không khả dụng",
		}
	}

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()

	var totalCount int
	var highConfidenceCount int
	err := db.Pool.QueryRow(ctx,
		`SELECT COUNT(*) FROM blacklist WHERE status = 'active'`,
	).Scan(&totalCount)
	if err != nil {
		return map[string]interface{}{"error": err.Error()}
	}

	err = db.Pool.QueryRow(ctx,
		`SELECT COUNT(*) FROM blacklist WHERE status = 'active' AND confidence_score >= 0.80`,
	).Scan(&highConfidenceCount)
	if err != nil {
		highConfidenceCount = 0
	}

	log.Printf("📊 [Agent:get_fraud_stats] total=%d, high_confidence=%d", totalCount, highConfidenceCount)

	return map[string]interface{}{
		"total_blacklisted":       totalCount,
		"high_confidence_numbers": highConfidenceCount,
		"message":                 fmt.Sprintf("Hệ thống đang theo dõi %d số điện thoại lừa đảo, trong đó %d có độ tin cậy cao", totalCount, highConfidenceCount),
	}
}

// executeAutoReportFraud tự động báo cáo số điện thoại lừa đảo
func executeAutoReportFraud(args map[string]interface{}) map[string]interface{} {
	phone, _ := args["phone_number"].(string)
	fraudType, _ := args["fraud_type"].(string)
	evidence, _ := args["evidence"].(string)

	if phone == "" {
		return map[string]interface{}{
			"success": false,
			"message": "Thiếu số điện thoại",
		}
	}
	if db.Pool == nil {
		return map[string]interface{}{
			"success": false,
			"message": "Database không khả dụng",
		}
	}

	reason := fmt.Sprintf("[AI Auto-Report] %s | Evidence: %s", fraudType, evidence)
	if len(reason) > 500 {
		reason = reason[:500]
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	// Upsert: thêm mới hoặc tăng reported_count nếu đã có
	_, err := db.Pool.Exec(ctx,
		`INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status)
		 VALUES ($1, $2, 0.75, 1, 'active')
		 ON CONFLICT (phone_number) DO UPDATE
		 SET reported_count = blacklist.reported_count + 1,
		     reason = EXCLUDED.reason,
		     last_reported_at = NOW(),
		     updated_at = NOW()`,
		phone, reason,
	)
	if err != nil {
		log.Printf("❌ [Agent:auto_report] Failed to report %s: %v", phone, err)
		return map[string]interface{}{
			"success": false,
			"message": fmt.Sprintf("Lỗi khi báo cáo: %v", err),
		}
	}

	log.Printf("✅ [Agent:auto_report] Auto-reported %s as fraud (%s)", phone, fraudType)
	return map[string]interface{}{
		"success":      true,
		"phone_number": phone,
		"fraud_type":   fraudType,
		"message":      fmt.Sprintf("Đã tự động báo cáo %s vào blacklist cộng đồng", phone),
	}
}

// --- Agentic loop chính ---

// RunFraudDetectionAgent chạy agentic loop: Gemini tự quyết định gọi tools rồi ra kết quả
// Đây là core của Agentic AI — thay thế AnalyzeFraudContext cũ (chỉ classify)
func (g *GeminiClient) RunFraudDetectionAgent(transcript string, previousContext []string) (*AgentAnalysisResult, error) {
	if transcript == "" {
		return nil, fmt.Errorf("empty transcript")
	}

	// Build context string
	contextStr := ""
	if len(previousContext) > 0 {
		start := 0
		if len(previousContext) > 5 {
			start = len(previousContext) - 5
		}
		contextStr = "Ngữ cảnh trước:\n" + strings.Join(previousContext[start:], "\n") + "\n\n"
	}

	systemPrompt := fmt.Sprintf(`Bạn là FraudGuard AI Agent — hệ thống phát hiện lừa đảo điện thoại tự động tại Việt Nam.

%sTranscript mới nhất:
"%s"

NHIỆM VỤ của bạn:
1. Phân tích transcript để tìm dấu hiệu lừa đảo
2. Nếu phát hiện SỐ ĐIỆN THOẠI trong transcript → gọi tool check_blacklist ngay
3. Gọi get_fraud_stats nếu cần thêm context về mức độ lừa đảo hiện tại
4. Nếu CHẮC CHẮN là lừa đảo (confidence=high) VÀ có số điện thoại rõ ràng → gọi auto_report_fraud
5. Sau khi dùng tools, đưa ra kết quả JSON cuối cùng

Các dạng lừa đảo phổ biến tại VN:
- Giả mạo công an/viện kiểm sát, đe dọa bắt giữ
- Lừa chuyển tiền/OTP/mã xác nhận
- Giả mạo ngân hàng thông báo tài khoản bị khóa
- Yêu cầu cài app lạ (Anydesk, Teamviewer, Ultraviewer)
- Trúng thưởng giả, đầu tư dễ kiếm tiền
- Tạo áp lực thời gian, yêu cầu giữ bí mật

Kết quả cuối cùng phải là JSON:
{"is_fraud": true/false, "risk_score": 0-100, "fraud_type": "...", "explanation": "...", "confidence": "high/medium/low"}`,
		contextStr, transcript)

	// Khởi tạo conversation với system prompt
	messages := []GeminiContent{
		{Parts: []GeminiPart{{Text: systemPrompt}}},
	}

	result := &AgentAnalysisResult{
		ToolsUsed: []string{},
	}

	// Agentic loop — tối đa 4 vòng (1 call + 3 tool calls)
	maxIterations := 4
	for i := 0; i < maxIterations; i++ {
		agentResp, err := g.callGeminiWithTools(messages)
		if err != nil {
			return nil, fmt.Errorf("agent iteration %d failed: %w", i, err)
		}

		if len(agentResp.Candidates) == 0 {
			break
		}

		candidate := agentResp.Candidates[0]
		parts := candidate.Content.Parts

		// Kiểm tra từng part trong response
		hasFunctionCall := false
		var toolResponseParts []GeminiPart

		for _, part := range parts {
			if part.FunctionCall != nil {
				hasFunctionCall = true
				toolName := part.FunctionCall.Name
				toolArgs := part.FunctionCall.Args

				log.Printf("🤖 [Agent] Gemini calling tool: %s with args: %v", toolName, toolArgs)
				result.ToolsUsed = append(result.ToolsUsed, toolName)

				// Track auto_report
				if toolName == "auto_report_fraud" {
					result.AutoReported = true
				}

				// Thực thi tool
				toolResult := executeTool(toolName, toolArgs)
				toolResultJSON, _ := json.Marshal(toolResult)

				// Thêm function response vào conversation
				toolResponseParts = append(toolResponseParts, GeminiPart{
					Text: fmt.Sprintf(`{"name": "%s", "response": %s}`, toolName, string(toolResultJSON)),
				})
			} else if part.Text != "" {
				// Gemini trả text — có thể là kết quả cuối
				finalText := extractJSON(part.Text)
				if strings.HasPrefix(strings.TrimSpace(finalText), "{") {
					var finalResult GeminiAnalysisResult
					if err := json.Unmarshal([]byte(finalText), &finalResult); err == nil {
						result.IsFraud = finalResult.IsFraud
						result.RiskScore = finalResult.RiskScore
						result.FraudType = finalResult.FraudType
						result.Explanation = finalResult.Explanation
						result.Confidence = finalResult.Confidence
						log.Printf("🤖 [Agent] Final result: IsFraud=%v, Score=%d, Type=%s, Tools=%v",
							result.IsFraud, result.RiskScore, result.FraudType, result.ToolsUsed)
						return result, nil
					}
				}
			}
		}

		// Nếu có function call → thêm kết quả tools vào conversation và tiếp tục
		if hasFunctionCall && len(toolResponseParts) > 0 {
			// Convert []AgentPart → []GeminiPart để thêm vào conversation history
			geminiParts := make([]GeminiPart, 0, len(parts))
			for _, p := range parts {
				if p.Text != "" {
					geminiParts = append(geminiParts, GeminiPart{Text: p.Text})
				} else if p.FunctionCall != nil {
					// Serialize function call thành text để Gemini hiểu context
					fcJSON, _ := json.Marshal(p.FunctionCall)
					geminiParts = append(geminiParts, GeminiPart{Text: string(fcJSON)})
				}
			}
			messages = append(messages, GeminiContent{Parts: geminiParts})
			// Thêm tool responses vào history
			messages = append(messages, GeminiContent{Parts: toolResponseParts})
		} else {
			// Không có function call nữa → kết thúc loop
			break
		}
	}

	// Nếu không parse được JSON, trả về safe default
	log.Printf("⚠️ [Agent] Could not parse final JSON after agentic loop, returning safe default")
	return &AgentAnalysisResult{
		IsFraud:     false,
		RiskScore:   0,
		FraudType:   "unknown",
		Explanation: "Không thể phân tích",
		Confidence:  "low",
		ToolsUsed:   result.ToolsUsed,
	}, nil
}

// callGeminiWithTools gọi Gemini API với tool definitions
func (g *GeminiClient) callGeminiWithTools(messages []GeminiContent) (*AgentResponse, error) {
	reqBody := AgentRequest{
		Contents: messages,
		Tools:    fraudDetectionTools,
		GenerationConfig: GeminiGenerationConfig{
			Temperature:     0.1,
			MaxOutputTokens: 512,
			TopP:            0.8,
		},
	}

	jsonBody, err := json.Marshal(reqBody)
	if err != nil {
		return nil, fmt.Errorf("failed to marshal agent request: %w", err)
	}

	url := fmt.Sprintf("https://generativelanguage.googleapis.com/v1beta/models/%s:generateContent?key=%s",
		g.Model, g.APIKey)

	ctx, cancel := context.WithTimeout(context.Background(), 8*time.Second)
	defer cancel()

	req, err := http.NewRequestWithContext(ctx, "POST", url, bytes.NewReader(jsonBody))
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := g.HTTPClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("agent API request failed: %w", err)
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("failed to read response: %w", err)
	}

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("agent API error (status %d): %s", resp.StatusCode, string(body))
	}

	var agentResp AgentResponse
	if err := json.Unmarshal(body, &agentResp); err != nil {
		return nil, fmt.Errorf("failed to parse agent response: %w", err)
	}
	if agentResp.Error != nil {
		return nil, fmt.Errorf("agent error: %s", agentResp.Error.Message)
	}

	return &agentResp, nil
}
