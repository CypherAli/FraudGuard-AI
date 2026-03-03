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
	AutoReported bool     `json:"auto_reported"` // đã xác nhận đủ điều kiện để report

	// Human-in-the-loop: khi Gemini muốn report nhưng cần user xác nhận
	PendingReportPhone  string `json:"pending_report_phone,omitempty"`
	PendingReportReason string `json:"pending_report_reason,omitempty"`
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
				Description: "Lấy thống kê lừa đảo từ blacklist theo từ khóa cụ thể. Dùng để đánh giá mức độ nguy hiểm của từ khóa/cụm từ xuất hiện trong cuộc gọi.",
				Parameters: FunctionParams{
					Type: "object",
					Properties: map[string]Property{
						"keyword": {
							Type:        "string",
							Description: "Từ khóa hoặc cụm từ cần tra cứu tần suất lừa đảo (ví dụ: 'chuyển tiền', 'công an', 'OTP'). Để trống để lấy thống kê tổng quát.",
						},
					},
					Required: []string{},
				},
			},
			{
				Name: "verify_scam_intent",
				Description: "BẮt BUỘC gọi trước auto_report. Xác minh NGƯỜI G\u1eccI (không phải chủ điện thoại) " +
					"có đủ ít nhất 2 dấu hiệu lừa đảo cụ thể: đòi OTP/mã xác nhận, yêu cầu chuyển tiền vào số lạ, " +
					"yêu cầu cài app điều khiển từ xa (AnyDesk/Teamviewer), giả mạo danh tính kết hợp đe dọa/áp lực.",
				Parameters: FunctionParams{
					Type: "object",
					Properties: map[string]Property{
						"phone_number": {
							Type:        "string",
							Description: "Số điện thoại của NGƯỜI G\u1eccI cần xác minh",
						},
						"scam_signals": {
							Type:        "string",
							Description: "Liệt kê dấu hiệu lừa đảo cụ thể từ lời nói NGƯỜI G\u1eccI (không phải người nghe)",
						},
						"caller_context": {
							Type:        "string",
							Description: "Người gọi tự xưng là ai (công an, ngân hàng, bưu điện, người quen...)",
						},
					},
					Required: []string{"phone_number", "scam_signals"},
				},
			},
			{
				Name: "auto_report",
				Description: "Gửi yêu cầu chặn số lừa đảo để người dùng xác nhận — KHÔNG ghi thẳng vào blacklist. " +
					"CHỈ gọi SAU KHI verify_scam_intent đã trả về verify_ok=true. " +
					"KHÔNG gọi nếu: số trong danh bạ, là ngân hàng/tổ chức hợp pháp, hoặc chưa gọi verify_scam_intent.",
				Parameters: FunctionParams{
					Type: "object",
					Properties: map[string]Property{
						"phone_number": {
							Type:        "string",
							Description: "Số điện thoại NGƯỜI G\u1eccI cần report",
						},
						"reason": {
							Type:        "string",
							Description: "Bằng chứng lừa đảo cụ thể từ transcript",
						},
					},
					Required: []string{"phone_number", "reason"},
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
		return executeGetFraudStats(args)
	case "verify_scam_intent":
		return executeVerifyScamIntent(args)
	case "auto_report":
		return executeAutoReport(args)
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

// executeGetFraudStats lấy thống kê blacklist, có thể lọc theo keyword
func executeGetFraudStats(args map[string]interface{}) map[string]interface{} {
	if db.Pool == nil {
		return map[string]interface{}{
			"error": "Database không khả dụng",
		}
	}

	keyword, _ := args["keyword"].(string)
	keyword = strings.TrimSpace(keyword)

	var totalCount int
	var highConfidenceCount int

	// Tổng số active trong blacklist — context riêng cho mỗi query để tránh shared-timeout race
	ctx1, cancel1 := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel1()
	err := db.Pool.QueryRow(ctx1,
		`SELECT COUNT(*) FROM blacklist WHERE status = 'active'`,
	).Scan(&totalCount)
	if err != nil {
		return map[string]interface{}{"error": err.Error()}
	}

	ctx2, cancel2 := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel2()
	err = db.Pool.QueryRow(ctx2,
		`SELECT COUNT(*) FROM blacklist WHERE status = 'active' AND confidence_score >= 0.80`,
	).Scan(&highConfidenceCount)
	if err != nil {
		highConfidenceCount = 0
	}

	result := map[string]interface{}{
		"total_blacklisted":       totalCount,
		"high_confidence_numbers": highConfidenceCount,
	}

	// Nếu có keyword, tra thêm tần suất xuất hiện trong reason của blacklist
	if keyword != "" {
		var keywordCount int
		ctx3, cancel3 := context.WithTimeout(context.Background(), 2*time.Second)
		defer cancel3()
		err = db.Pool.QueryRow(ctx3,
			`SELECT COUNT(*) FROM blacklist WHERE status = 'active' AND reason ILIKE $1`,
			"%"+keyword+"%",
		).Scan(&keywordCount)
		if err != nil {
			keywordCount = 0
		}

		var severity string
		switch {
		case keywordCount >= 50:
			severity = "very_high"
		case keywordCount >= 20:
			severity = "high"
		case keywordCount >= 5:
			severity = "medium"
		case keywordCount >= 1:
			severity = "low"
		default:
			severity = "none"
		}

		result["keyword"] = keyword
		result["keyword_fraud_count"] = keywordCount
		result["keyword_severity"] = severity
		result["message"] = fmt.Sprintf("Từ khóa '%s' xuất hiện trong %d ca lừa đảo (mức: %s). Tổng blacklist: %d số, %d có độ tin cậy cao",
			keyword, keywordCount, severity, totalCount, highConfidenceCount)

		log.Printf("📊 [Agent:get_fraud_stats] keyword='%s' count=%d severity=%s total=%d",
			keyword, keywordCount, severity, totalCount)
	} else {
		result["message"] = fmt.Sprintf("Hệ thống đang theo dõi %d số điện thoại lừa đảo, trong đó %d có độ tin cậy cao",
			totalCount, highConfidenceCount)
		log.Printf("📊 [Agent:get_fraud_stats] total=%d, high_confidence=%d", totalCount, highConfidenceCount)
	}

	return result
}

// isValidPhoneNumber performs a lightweight sanity-check on a phone number string.
// Accepts Vietnamese domestic (10 digits, leading 0) and international (+/digits, 7-15 digits).
// Rejects empty, too-short, too-long, or clearly non-numeric values that Gemini might hallucinate.
func isValidPhoneNumber(phone string) bool {
	// Strip spaces, hyphens, and parentheses that may appear in transcripts
	cleaned := strings.Map(func(r rune) rune {
		if r >= '0' && r <= '9' || r == '+' {
			return r
		}
		return -1
	}, strings.TrimSpace(phone))

	if len(cleaned) < 7 || len(cleaned) > 16 {
		return false
	}
	// Must start with + (international) or a digit
	return cleaned[0] == '+' || (cleaned[0] >= '0' && cleaned[0] <= '9')
}

// executeVerifyScamIntent xác minh xem có đủ bằng chứng lừa đảo cụ thể không.
// Đây là bước gate bắt buộc trước khi gọi auto_report.
func executeVerifyScamIntent(args map[string]interface{}) map[string]interface{} {
	phone, _ := args["phone_number"].(string)
	signals, _ := args["scam_signals"].(string)
	callerCtx, _ := args["caller_context"].(string)

	phone = strings.TrimSpace(phone)
	signals = strings.TrimSpace(signals)

	if phone == "" || signals == "" {
		return map[string]interface{}{
			"verify_ok": false,
			"message":   "Thiếu thông tin: cần phone_number và scam_signals",
		}
	}

	strongSignals := []string{"OTP", "mã xác nhận", "chuyển tiền", "AnyDesk", "Teamviewer", "Ultraviewer",
		"tài khoản bị khóa", "bắt giữ", "khởi tố", "triệu đồng", "công an", "viện kiểm sát",
		"toà án", "cài app", "mã PIN", "số tài khoản", "bảo hiểm xã hội"}

	signalCount := 0
	signalsLower := strings.ToLower(signals)
	for _, s := range strongSignals {
		if strings.Contains(signalsLower, strings.ToLower(s)) {
			signalCount++
		}
	}

	if signalCount < 2 {
		log.Printf("SHIELD: [Agent:verify_scam_intent] Rejected %s -- only %d signal(s): %s", phone, signalCount, signals)
		return map[string]interface{}{
			"verify_ok":    false,
			"signal_count": signalCount,
			"message":      fmt.Sprintf("Chưa đủ bằng chứng (%d/2). Cần ít nhất 2 dấu hiệu lừa đảo.", signalCount),
		}
	}

	log.Printf("OK: [Agent:verify_scam_intent] Approved %s -- %d signals: %s (ctx: %s)", phone, signalCount, signals, callerCtx)
	return map[string]interface{}{
		"verify_ok":    true,
		"phone_number": phone,
		"signal_count": signalCount,
		"message":      fmt.Sprintf("Xác nhận đủ bằng chứng (%d dấu hiệu). Có thể gọi auto_report.", signalCount),
	}
}

// executeAutoReport — human-in-the-loop version.
// Thay vì ghi thẳng vào DB, trả về pending_confirmation=true để mobile app
// hiển thị notification xác nhận cho người dùng trước khi block.
func executeAutoReport(args map[string]interface{}) map[string]interface{} {
	phone, _ := args["phone_number"].(string)
	reason, _ := args["reason"].(string)

	phone = strings.TrimSpace(phone)
	if phone == "" {
		return map[string]interface{}{
			"success": false,
			"message": "Thiếu số điện thoại",
		}
	}

	if !isValidPhoneNumber(phone) {
		log.Printf("WARNING: [Agent:auto_report] Invalid phone format: %q", phone)
		return map[string]interface{}{
			"success": false,
			"message": fmt.Sprintf("Định dạng số điện thoại không hợp lệ: %q", phone),
		}
	}
	if reason == "" {
		reason = "Phát hiện dấu hiệu lừa đảo"
	}
	if len(reason) > 500 {
		reason = reason[:500]
	}

	log.Printf("PENDING: [Agent:auto_report] Pending confirmation for %s: %s", phone, reason)
	return map[string]interface{}{
		"success":              false,
		"pending_confirmation": true,
		"phone_number":         phone,
		"reason":               reason,
		"message":              fmt.Sprintf("Yêu cầu xác nhận chặn số %s đã được gửi đến người dùng", phone),
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

PHÂN BIỆT NGƯỜI NÓI (Speaker Diarization):
- NGƯỜI G\u1eccI (Caller) = người chủ động gọi đến, có thể là kẻ lừa đảo. Thường hỏi/yêu cầu/đe dọa.
- NGƯỜI NGHE (User) = chủ điện thoại đang được bảo vệ. Thường trả lời/phản ứng.
- CHỈ phân tích hành vi và ngôn ngữ của NGƯỜI G\u1eccI khi đánh giá rủi ro lừa đảo.
- Nếu NGƯỜI NGHE nói "tôi muốn chuyển tiền" theo đề nghị của NGƯỜI G\u1eccI → đây là nạn nhân đang bị thao túng, không phải dấu hiệu lừa đảo của họ.

NHIỆM VỤ:
1. Phân tích transcript, xác định hành vi NGƯỜI G\u1eccI
2. Nếu phát hiện SỐ ĐIỆN THOẠI trong transcript → gọi check_blacklist ngay
3. Gọi get_fraud_stats nếu cần thêm context
4. Nếu NGƯỜI G\u1eccI có dấu hiệu lừa đảo RÕ RÀNG → gọi verify_scam_intent (BẮt BUỘC trước auto_report)
5. Chỉ gọi auto_report sau khi verify_scam_intent trả về verify_ok=true
6. Đưa ra kết quả JSON cuối cùng

Dạng lừa đảo phổ biến tại VN (chú ý hành vi NGƯỜI G\u1eccI):
- Giả mạo công an/viện kiểm sát → đe dọa bắt giữ → yêu cầu chuyển tiền "phong tỏa tài sản"
- Giả mạo ngân hàng → thông báo tài khoản bị khóa → yêu cầu OTP/mã xác nhận
- Yêu cầu cài AnyDesk/Teamviewer/Ultraviewer để "hỗ trợ kỹ thuật"
- Trúng thưởng/đầu tư → yêu cầu chuyển tiền "phí" trước
- Tạo áp lực khẩn cấp, yêu cầu giữ bí mật, không cho hỏi người thân

Kết quả cuối cùng phải là JSON:
{"is_fraud": true/false, "risk_score": 0-100, "fraud_type": "...", "explanation": "...", "confidence": "high/medium/low"}`,
		contextStr, transcript)

	// Khởi tạo conversation với system prompt
	messages := []GeminiContent{
		{Role: "user", Parts: []GeminiPart{{Text: systemPrompt}}},
	}

	result := &AgentAnalysisResult{
		ToolsUsed: []string{},
	}

	// Per-session duplicate-report guard: tracks phone numbers already auto-reported
	// in this single RunFraudDetectionAgent invocation.  Prevents Gemini from reporting
	// the same number more than once if it calls auto_report in multiple iterations.
	reportedNumbers := make(map[string]struct{})

	// Agentic loop — tối đa 4 vòng (1 call + 3 tool calls)
	maxIterations := 6
	for i := 0; i < maxIterations; i++ {
		agentResp, err := g.callGeminiWithTools(messages)
		if err != nil {
			return nil, fmt.Errorf("agent iteration %d failed: %w", i, err)
		}

		if len(agentResp.Candidates) == 0 {
			break
		}

		candidate := agentResp.Candidates[0]
		// Guard against safety-filtered candidates: Content.Parts will be empty
		// and FinishReason will be "SAFETY" or similar non-STOP reason.
		if len(candidate.Content.Parts) == 0 {
			log.Printf("⚠️ [Agent] Candidate has no Parts (FinishReason=%s, safety filter?) — stopping agentic loop", candidate.FinishReason)
			break
		}
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

				// Duplicate-report guard: reject if the same phone was already
				// auto_reported earlier in this agentic session.
				if toolName == "auto_report" {
					reportPhone, _ := toolArgs["phone_number"].(string)
					reportPhone = strings.TrimSpace(reportPhone)
					// Normalize Vietnamese phone: "0xxx" ↔ "+84xxx" so the same number
					// isn't reported twice just because Gemini formats it differently
					// across turns (e.g. "+84912345678" vs "0912345678").
					if strings.HasPrefix(reportPhone, "0") && len(reportPhone) >= 10 {
						reportPhone = "+84" + reportPhone[1:]
					}
					if _, alreadyReported := reportedNumbers[reportPhone]; alreadyReported {
						log.Printf("⚠️ [Agent] auto_report duplicate skipped for %q", reportPhone)
						toolResponseParts = append(toolResponseParts, GeminiPart{
							FunctionResponse: &FunctionResponse{
								Name: toolName,
								Response: map[string]interface{}{
									"success": false,
									"message": fmt.Sprintf("Số %s đã được báo cáo trong phiên này — bỏ qua", reportPhone),
								},
							},
						})
						continue
					}
					reportedNumbers[reportPhone] = struct{}{}
					result.AutoReported = true
				}

				// Thực thi tool
				toolResult := executeTool(toolName, toolArgs)

				// Khi auto_report trả về pending_confirmation — lưu vào result để fraud_detector
				// có thể gửi pending_report alert tới mobile thay vì ghi thẳng DB.
				if toolName == "auto_report" {
					if pending, ok := toolResult["pending_confirmation"].(bool); ok && pending {
						if ph, ok := toolResult["phone_number"].(string); ok && ph != "" && result.PendingReportPhone == "" {
							result.PendingReportPhone = ph
							result.PendingReportReason, _ = toolResult["reason"].(string)
						}
					}
				}

				// Thêm function response vào conversation
				toolResponseParts = append(toolResponseParts, GeminiPart{
					FunctionResponse: &FunctionResponse{
						Name:     toolName,
						Response: toolResult,
					},
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
			// Add model turn with function call(s) — role: "model"
			modelParts := make([]GeminiPart, 0, len(parts))
			for _, p := range parts {
				if p.Text != "" {
					modelParts = append(modelParts, GeminiPart{Text: p.Text})
				} else if p.FunctionCall != nil {
					modelParts = append(modelParts, GeminiPart{FunctionCall: p.FunctionCall})
				}
			}
			messages = append(messages, GeminiContent{Role: "model", Parts: modelParts})
			// Add function responses as user turn — role: "user"
			messages = append(messages, GeminiContent{Role: "user", Parts: toolResponseParts})
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
