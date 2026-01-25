package services

import (
	"testing"
)

// TestFraudDetector_AnalyzeText tests the core fraud detection logic
func TestFraudDetector_AnalyzeText(t *testing.T) {
	t.Run("Normal conversation - should be safe", func(t *testing.T) {
		detector := NewFraudDetector("test-device-001")

		result := detector.AnalyzeText("Alo xin chào, tôi muốn hỏi mua rau")

		if result.IsAlert {
			t.Errorf("❌ Lỗi: Câu bình thường mà lại báo Fraud! Msg: %s", result.Message)
		} else {
			t.Logf("✅ Test Passed: Câu bình thường -> Safe (Score: %d)", result.RiskScore)
		}
	})

	t.Run("Accumulated fraud detection", func(t *testing.T) {
		detector := NewFraudDetector("test-device-002")

		// Câu 1: Giới thiệu công an (+25 điểm)
		result1 := detector.AnalyzeText("Tôi là cán bộ công an điều tra đây")
		t.Logf("📊 Câu 1: Score=%d, Action=%s", result1.RiskScore, result1.Action)

		// Câu 2: Đòi chuyển tiền (+50 điểm) -> Tổng 75
		result2 := detector.AnalyzeText("Yêu cầu anh chuyển tiền để xác minh")
		t.Logf("📊 Câu 2: Score=%d, Action=%s", result2.RiskScore, result2.Action)

		// Câu 3: Đòi mã OTP (+45 điểm) -> Tổng 120 -> CRITICAL!
		result3 := detector.AnalyzeText("Đọc mã OTP ngay")
		t.Logf("📊 Câu 3: Score=%d, Action=%s", result3.RiskScore, result3.Action)

		// Kỳ vọng: Phải báo Fraud
		if !result3.IsAlert {
			t.Errorf("❌ Lỗi: Đã nói đủ từ khóa lừa đảo mà không báo!")
		} else {
			t.Logf("✅ Test Passed: Phát hiện lừa đảo thành công!")
			t.Logf("   -> Cảnh báo: %s", result3.Message)
			t.Logf("   -> Hành động: %s", result3.Action)
			t.Logf("   -> Điểm tích lũy: %d/100", result3.RiskScore)
		}

		// Verify accumulated score
		if detector.GetCurrentRiskScore() < 90 {
			t.Errorf("❌ Lỗi: Điểm tích lũy không đúng. Expected >= 90, Got: %d",
				detector.GetCurrentRiskScore())
		}
	})

	t.Run("Critical keywords trigger immediate alert", func(t *testing.T) {
		detector := NewFraudDetector("test-device-003")

		result := detector.AnalyzeText("Tôi là công an, bạn phải chuyển tiền ngay và cung cấp mã OTP")

		if !result.IsAlert {
			t.Errorf("❌ Lỗi: Câu có nhiều từ khóa critical mà không báo!")
		}

		if result.RiskScore < 90 {
			t.Errorf("❌ Lỗi: Score quá thấp. Expected >= 90, Got: %d", result.RiskScore)
		}

		if result.Action != "CRITICAL" {
			t.Errorf("❌ Lỗi: Action không đúng. Expected: CRITICAL, Got: %s", result.Action)
		}

		t.Logf("✅ Test Passed: Critical alert triggered (Score: %d)", result.RiskScore)
	})

	t.Run("Session reset works correctly", func(t *testing.T) {
		detector := NewFraudDetector("test-device-004")

		// Add some score
		detector.AnalyzeText("Tôi là công an")
		scoreBefore := detector.GetCurrentRiskScore()

		if scoreBefore == 0 {
			t.Errorf("❌ Lỗi: Score không tăng sau khi phát hiện từ khóa")
		}

		// Reset
		detector.ResetSession()
		scoreAfter := detector.GetCurrentRiskScore()

		if scoreAfter != 0 {
			t.Errorf("❌ Lỗi: Reset không hoạt động. Expected: 0, Got: %d", scoreAfter)
		}

		t.Logf("✅ Test Passed: Session reset works (Before: %d, After: %d)",
			scoreBefore, scoreAfter)
	})
}

// TestFraudDetector_KeywordMatching tests individual keyword detection
func TestFraudDetector_KeywordMatching(t *testing.T) {
	testCases := []struct {
		name          string
		text          string
		expectAlert   bool
		minScore      int
		expectedLevel string
	}{
		{
			name:          "Critical keyword: chuyển tiền",
			text:          "Bạn phải chuyển tiền ngay",
			expectAlert:   true,
			minScore:      50,
			expectedLevel: "MEDIUM",
		},
		{
			name:          "Critical keyword: mã OTP",
			text:          "Vui lòng cung cấp mã OTP",
			expectAlert:   false, // 45 points = below 50 threshold
			minScore:      40,
			expectedLevel: "LOW",
		},
		{
			name:          "Warning keyword: công an",
			text:          "Tôi là công an",
			expectAlert:   false,
			minScore:      20,
			expectedLevel: "LOW",
		},
		{
			name:          "Suspicious phrase: trong 5 phút",
			text:          "Bạn phải làm ngay trong 5 phút",
			expectAlert:   false,
			minScore:      25,
			expectedLevel: "LOW",
		},
		{
			name:          "Multiple keywords",
			text:          "Tôi là công an, bạn phải chuyển tiền và cung cấp mã OTP trong 5 phút",
			expectAlert:   true,
			minScore:      90,
			expectedLevel: "CRITICAL",
		},
		{
			name:          "Normal text",
			text:          "Xin chào, tôi muốn đặt hàng sản phẩm",
			expectAlert:   false,
			minScore:      0,
			expectedLevel: "SAFE",
		},
	}

	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			detector := NewFraudDetector("test-keyword-" + tc.name)
			result := detector.AnalyzeText(tc.text)

			if result.IsAlert != tc.expectAlert {
				t.Errorf("❌ Alert mismatch. Expected: %v, Got: %v", tc.expectAlert, result.IsAlert)
			}

			if result.RiskScore < tc.minScore {
				t.Errorf("❌ Score too low. Expected >= %d, Got: %d", tc.minScore, result.RiskScore)
			}

			if result.Action != tc.expectedLevel {
				t.Errorf("❌ Level mismatch. Expected: %s, Got: %s", tc.expectedLevel, result.Action)
			}

			t.Logf("✅ Passed: '%s' -> Score=%d, Action=%s, Alert=%v",
				tc.text, result.RiskScore, result.Action, result.IsAlert)
		})
	}
}

// TestFraudDetector_ConfigurableThresholds tests different configurations
func TestFraudDetector_ConfigurableThresholds(t *testing.T) {
	text := "Tôi là công an, bạn phải chuyển tiền" // Score = 75

	t.Run("Default config", func(t *testing.T) {
		detector := NewFraudDetector("test-config-default")
		result := detector.AnalyzeText(text)

		// With default (Critical=90, High=70), score 75 should be HIGH
		if result.Action != "HIGH" {
			t.Errorf("❌ Expected HIGH, Got: %s", result.Action)
		}
		t.Logf("✅ Default config: Score=%d, Action=%s", result.RiskScore, result.Action)
	})

	t.Run("Conservative config", func(t *testing.T) {
		config := ConservativeConfig()
		detector := NewFraudDetectorWithConfig("test-config-conservative", config)
		result := detector.AnalyzeText(text)

		// With conservative (Critical=100, High=85, Medium=65), score 75 should be MEDIUM
		if result.Action != "MEDIUM" {
			t.Errorf("❌ Expected MEDIUM, Got: %s", result.Action)
		}
		t.Logf("✅ Conservative config: Score=%d, Action=%s", result.RiskScore, result.Action)
	})

	t.Run("Aggressive config", func(t *testing.T) {
		config := AggressiveConfig()
		detector := NewFraudDetectorWithConfig("test-config-aggressive", config)
		result := detector.AnalyzeText(text)

		// With aggressive (Critical=80, High=60), score 75 should be HIGH
		if result.Action != "HIGH" {
			t.Errorf("❌ Expected HIGH, Got: %s", result.Action)
		}
		t.Logf("✅ Aggressive config: Score=%d, Action=%s", result.RiskScore, result.Action)
	})
}

// BenchmarkFraudDetector_AnalyzeText benchmarks the fraud detection performance
func BenchmarkFraudDetector_AnalyzeText(b *testing.B) {
	detector := NewFraudDetector("benchmark-device")
	text := "Tôi là công an, bạn phải chuyển tiền ngay"

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		detector.AnalyzeText(text)
		if i%100 == 0 {
			detector.ResetSession() // Reset periodically to avoid overflow
		}
	}
}
