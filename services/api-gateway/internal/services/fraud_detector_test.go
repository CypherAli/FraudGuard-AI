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
			expectAlert:   true, // 45 points > MEDIUM threshold (40)
			minScore:      45,
			expectedLevel: "MEDIUM",
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

// TestFraudDetector_NegativeKeywords tests the whitelist/negative keyword functionality
func TestFraudDetector_NegativeKeywords(t *testing.T) {
	t.Run("Movie/entertainment content should reduce score", func(t *testing.T) {
		detector := NewFraudDetector("test-device-negative-001")

		// First add fraud keywords to increase score
		result1 := detector.AnalyzeText("Cảnh lừa đảo")
		if result1.RiskScore == 0 {
			t.Errorf("❌ Error: Score should increase with fraud keyword")
		}
		t.Logf("📊 After fraud keyword: Score=%d", result1.RiskScore)

		// Then mention it's from a movie - should reduce score
		result2 := detector.AnalyzeText("trong phim hành động")
		t.Logf("📊 After negative keyword: Score=%d (reduced from %d)",
			result2.RiskScore, result1.RiskScore)

		if result2.RiskScore >= result1.RiskScore {
			t.Errorf("❌ Error: Score should be reduced after negative keyword. Before: %d, After: %d",
				result1.RiskScore, result2.RiskScore)
		} else {
			t.Logf("✅ Test Passed: Negative keyword reduced score by %d points",
				result1.RiskScore-result2.RiskScore)
		}
	})

	t.Run("Story/novel content should reduce score", func(t *testing.T) {
		detector := NewFraudDetector("test-device-negative-002")

		// Context: discussing fraud in a story
		result := detector.AnalyzeText("Trong truyện này có cảnh lừa đảo qua điện thoại")

		if result.IsAlert {
			t.Errorf("❌ Error: Should not trigger alert for story content. Score: %d", result.RiskScore)
		} else {
			t.Logf("✅ Test Passed: Story content recognized as legitimate (Score: %d)", result.RiskScore)
		}
	})

	t.Run("News/journalism content should reduce score", func(t *testing.T) {
		detector := NewFraudDetector("test-device-negative-003")

		result := detector.AnalyzeText("Tin tức hôm nay: công an bắt đường dây lừa đảo")

		if result.IsAlert {
			t.Errorf("❌ Error: Should not trigger alert for news content. Score: %d", result.RiskScore)
		} else {
			t.Logf("✅ Test Passed: News content recognized as legitimate (Score: %d)", result.RiskScore)
		}
	})

	t.Run("Score should never go negative", func(t *testing.T) {
		detector := NewFraudDetector("test-device-negative-004")

		// Only negative keywords, no fraud keywords
		result := detector.AnalyzeText("Tôi đang xem phim truyện và đọc sách")

		if result.RiskScore < 0 {
			t.Errorf("❌ Error: Score went negative: %d", result.RiskScore)
		} else {
			t.Logf("✅ Test Passed: Score clamped to minimum 0 (Score: %d)", result.RiskScore)
		}
	})
}

// TestFraudDetector_ObfuscatedKeywords tests regex-based detection of obfuscated keywords
func TestFraudDetector_ObfuscatedKeywords(t *testing.T) {
	testCases := []struct {
		name        string
		text        string
		shouldMatch bool
		description string
	}{
		{
			name:        "Normal 'phim' keyword",
			text:        "Đây là một bộ phim hay",
			shouldMatch: true,
			description: "Standard keyword should match",
		},
		{
			name:        "Obfuscated 'Ph1m' with number substitution",
			text:        "Tôi xem Ph1m hành động",
			shouldMatch: true,
			description: "Should detect '1' substitution for 'i'",
		},
		{
			name:        "Obfuscated 'P.h.i.m' with dots",
			text:        "Cảnh trong P.h.i.m này",
			shouldMatch: true,
			description: "Should detect dot separation",
		},
		{
			name:        "Obfuscated 'P-h-i-m' with dashes",
			text:        "Đây là P-h-i-m kinh dị",
			shouldMatch: true,
			description: "Should detect dash separation",
		},
		{
			name:        "Obfuscated 'PhIm' with mixed case",
			text:        "PhIm này rất hay",
			shouldMatch: true,
			description: "Should detect case variation",
		},
		{
			name:        "Obfuscated 'ph!m' with symbol",
			text:        "Xem ph!m này chưa",
			shouldMatch: true,
			description: "Should detect symbol substitution",
		},
		{
			name:        "Obfuscated 'truy3n' with number",
			text:        "Đọc truy3n này hay",
			shouldMatch: true,
			description: "Should detect '3' substitution for 'ê'",
		},
		{
			name:        "Obfuscated 't.r.u.y.ê.n' with dots",
			text:        "Nội dung t.r.u.y.ê.n này",
			shouldMatch: true,
			description: "Should detect dot separation in 'truyện'",
		},
		{
			name:        "Normal 'sách' keyword",
			text:        "Tôi đang đọc sách",
			shouldMatch: true,
			description: "Standard keyword should match",
		},
		{
			name:        "Obfuscated 's@ch' with symbol",
			text:        "Quyển s@ch này hay",
			shouldMatch: true,
			description: "Should detect '@' substitution for 'a'",
		},
	}

	for _, tc := range testCases {
		t.Run(tc.name, func(t *testing.T) {
			detector := NewFraudDetector("test-obfuscation")

			// Test strategy: Add fraud keywords WITHOUT triggering negative patterns
			// Use keywords that won't accidentally match negative regex
			detector.AnalyzeText("Ngân hàng yêu cầu xác minh") // +20 points (no negative patterns)
			scoreBefore := detector.GetCurrentRiskScore()

			if scoreBefore == 0 {
				t.Errorf("❌ Test setup error: Score should be > 0 after fraud keywords")
				return
			}

			// Then add obfuscated negative keyword
			result := detector.AnalyzeText(tc.text)
			scoreAfter := result.RiskScore

			if tc.shouldMatch {
				if scoreAfter < scoreBefore {
					t.Logf("✅ Test Passed: '%s' - Obfuscated keyword detected (Score: %d -> %d)",
						tc.description, scoreBefore, scoreAfter)
				} else {
					t.Errorf("❌ Error: '%s' - Should detect obfuscated keyword in: '%s' (Score: %d, expected < %d)",
						tc.description, tc.text, scoreAfter, scoreBefore)
				}
			}

			// Reset for next test
			detector.ResetSession()
		})
	}
}

// TestFraudDetector_RealWorldScenarios tests complete real-world scenarios
func TestFraudDetector_RealWorldScenarios(t *testing.T) {
	t.Run("Scenario 1: Movie discussion - should NOT alert", func(t *testing.T) {
		detector := NewFraudDetector("test-scenario-001")

		transcript := []string{
			"Mình vừa xem bộ phim về lừa đảo",
			"Trong phim có cảnh giả danh công an",
			"Diễn viên đóng vai kẻ lừa đảo rất hay",
			"Bộ phim cảnh báo về chiêu trò chuyển tiền lừa đảo",
		}

		var finalResult FraudAnalysisResult
		for _, text := range transcript {
			finalResult = detector.AnalyzeText(text)
		}

		if finalResult.IsAlert {
			t.Errorf("❌ Error: Movie discussion triggered false alert! Score: %d, Patterns: %v",
				finalResult.RiskScore, finalResult.Patterns)
		} else {
			t.Logf("✅ Test Passed: Movie discussion correctly identified as safe (Score: %d)",
				finalResult.RiskScore)
		}
	})

	t.Run("Scenario 2: News report - should NOT alert", func(t *testing.T) {
		detector := NewFraudDetector("test-scenario-002")

		// More realistic news report - keep context consistent
		transcript := []string{
			"Tin tức mới nhất:",
			"Theo báo chí, công an vừa triệt phá đường dây lừa đảo",
			"Thủ đoạn của băng nhóm: giả danh cán bộ ngân hàng",
			"Báo chí cho biết chúng yêu cầu nạn nhân chuyển tiền và mã OTP",
		}

		var finalResult FraudAnalysisResult
		for _, text := range transcript {
			finalResult = detector.AnalyzeText(text)
		}

		// With consistent "báo chí" / "tin tức" context, should NOT alert
		// Negative keywords throughout conversation keep score low
		if finalResult.IsAlert {
			t.Errorf("❌ Error: News report with consistent context triggered false alert! Score: %d",
				finalResult.RiskScore)
		} else {
			t.Logf("✅ Test Passed: News report correctly identified as safe (Score: %d)",
				finalResult.RiskScore)
		}
	})

	t.Run("Scenario 3: Actual fraud call - SHOULD alert", func(t *testing.T) {
		detector := NewFraudDetector("test-scenario-003")

		transcript := []string{
			"Tôi là công an điều tra",
			"Anh bị tố cáo liên quan đến vụ án rửa tiền",
			"Cần xác minh ngay, chuyển khoản vào tài khoản này",
			"Cung cấp mã OTP để hoàn tất thủ tục",
		}

		var finalResult FraudAnalysisResult
		for _, text := range transcript {
			finalResult = detector.AnalyzeText(text)
		}

		if !finalResult.IsAlert {
			t.Errorf("❌ Error: Actual fraud call NOT detected! Score: %d",
				finalResult.RiskScore)
		} else if finalResult.Action != "CRITICAL" {
			t.Errorf("❌ Error: Fraud severity incorrect. Expected: CRITICAL, Got: %s (Score: %d)",
				finalResult.Action, finalResult.RiskScore)
		} else {
			t.Logf("✅ Test Passed: Actual fraud correctly identified as CRITICAL (Score: %d)",
				finalResult.RiskScore)
		}
	})

	t.Run("Scenario 4: Obfuscated movie discussion - should NOT alert", func(t *testing.T) {
		detector := NewFraudDetector("test-scenario-004")

		result := detector.AnalyzeText("Trong Ph1m này có cảnh l.ừ.a đ.ả.o qua điện thoại")

		if result.IsAlert {
			t.Errorf("❌ Error: Obfuscated movie discussion triggered false alert! Score: %d",
				result.RiskScore)
		} else {
			t.Logf("✅ Test Passed: Obfuscated movie discussion correctly handled (Score: %d)",
				result.RiskScore)
		}
	})
}
