package main

import (
	"fmt"

	"github.com/fraudguard/api-gateway/internal/services"
)

// TestFraudDetector demonstrates the fraud detection logic
func main() {
	fmt.Println("=== FraudGuard AI - Fraud Detection Test ===\n")

	// Create a fraud detector for testing
	detector := services.NewFraudDetector("test-device-001")

	// Test cases
	testCases := []struct {
		name       string
		transcript string
		expected   string
	}{
		{
			name:       "Test 1: Critical Fraud - Fake Police",
			transcript: "Tôi là công an, bạn phải chuyển tiền ngay trong 5 phút",
			expected:   "CRITICAL",
		},
		{
			name:       "Test 2: High Risk - OTP Request",
			transcript: "Vui lòng cung cấp mã OTP để xác nhận giao dịch",
			expected:   "HIGH",
		},
		{
			name:       "Test 3: Medium Risk - Bank Call",
			transcript: "Đây là ngân hàng Vietcombank, tài khoản của bạn có vấn đề",
			expected:   "MEDIUM",
		},
		{
			name:       "Test 4: Low Risk - Promotion",
			transcript: "Chúc mừng bạn trúng thưởng giải thưởng lớn",
			expected:   "LOW",
		},
		{
			name:       "Test 5: Safe - Normal Call",
			transcript: "Xin chào, tôi muốn hỏi về sản phẩm của công ty",
			expected:   "SAFE",
		},
		{
			name:       "Test 6: Accumulated Score",
			transcript: "Tôi là công an",
			expected:   "LOW",
		},
		{
			name:       "Test 7: Accumulated Score (continued)",
			transcript: "Bạn phải chuyển tiền ngay",
			expected:   "CRITICAL",
		},
	}

	// Run tests
	for i, tc := range testCases {
		fmt.Printf("\n--- %s ---\n", tc.name)
		fmt.Printf("Transcript: \"%s\"\n", tc.transcript)

		result := detector.AnalyzeText(tc.transcript)

		fmt.Printf("Risk Score: %d/100\n", result.RiskScore)
		fmt.Printf("Action: %s\n", result.Action)
		fmt.Printf("Is Alert: %v\n", result.IsAlert)
		fmt.Printf("Message: %s\n", result.Message)

		if len(result.Patterns) > 0 {
			fmt.Println("Detected Patterns:")
			for _, pattern := range result.Patterns {
				fmt.Printf("  - %s\n", pattern)
			}
		}

		// Show accumulated score
		fmt.Printf("Accumulated Score: %d/100\n", detector.GetCurrentRiskScore())

		// Reset for next independent test (except for accumulated test)
		if i != 5 { // Don't reset before test 7 to show accumulation
			detector.ResetSession()
		}
	}

	fmt.Println("\n=== Test Complete ===")
}
