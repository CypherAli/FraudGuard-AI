# Threshold Tuning Guide - Quick Reference

## 🎯 Mục đích

Hướng dẫn nhanh cách điều chỉnh ngưỡng (threshold) để tối ưu độ chính xác của fraud detection.

---

## 📊 Ngưỡng mặc định (Default)

```go
CriticalThreshold: 90   // CRITICAL alert
HighThreshold:     70   // HIGH alert  
MediumThreshold:   50   // MEDIUM alert
LowThreshold:      30   // LOW warning
```

---

## 🔧 Cách sử dụng

### 1. Sử dụng config mặc định (Recommended cho Hackathon)

```go
// Tự động sử dụng default config
detector := services.NewFraudDetector(deviceID)
```

### 2. Sử dụng Conservative Config (Ít cảnh báo hơn)

```go
// Nếu bị quá nhiều false positives
config := services.ConservativeConfig()
detector := services.NewFraudDetectorWithConfig(deviceID, config)
```

**Conservative thresholds:**
- Critical: 100 (khó trigger hơn)
- High: 85
- Medium: 65
- Low: 40

### 3. Sử dụng Aggressive Config (Nhiều cảnh báo hơn)

```go
// Nếu bỏ sót nhiều fraud cases
config := services.AggressiveConfig()
detector := services.NewFraudDetectorWithConfig(deviceID, config)
```

**Aggressive thresholds:**
- Critical: 80 (dễ trigger hơn)
- High: 60
- Medium: 40
- Low: 20

### 4. Custom Config (Tùy chỉnh hoàn toàn)

```go
config := &services.FraudDetectionConfig{
    CriticalThreshold: 95,  // Tùy chỉnh
    HighThreshold:     75,
    MediumThreshold:   55,
    LowThreshold:      35,
    
    // Multipliers để điều chỉnh điểm từ khóa
    CriticalMultiplier:   1.1,  // Tăng 10%
    WarningMultiplier:    0.9,  // Giảm 10%
    SuspiciousMultiplier: 1.0,  // Giữ nguyên
}

detector := services.NewFraudDetectorWithConfig(deviceID, config)
```

---

## 🧪 Testing & Tuning Process

### Bước 1: Thu thập data
```
- Ghi lại 50-100 cuộc gọi thật
- Label thủ công: Fraud / Not Fraud
- Chạy qua detector với config hiện tại
```

### Bước 2: Đánh giá kết quả

**Tính metrics:**
```
True Positives (TP)   = Phát hiện đúng fraud
False Positives (FP)  = Báo nhầm (không fraud mà báo)
True Negatives (TN)   = Đúng là không fraud
False Negatives (FN)  = Bỏ sót (fraud mà không báo)

Precision = TP / (TP + FP)  // Độ chính xác khi báo
Recall    = TP / (TP + FN)  // Tỷ lệ phát hiện được
F1 Score  = 2 * (Precision * Recall) / (Precision + Recall)
```

### Bước 3: Điều chỉnh

**Nếu Precision thấp (nhiều false positives):**
```go
// Tăng thresholds
config.CriticalThreshold += 10
config.HighThreshold += 10
// Hoặc giảm multipliers
config.CriticalMultiplier = 0.8
```

**Nếu Recall thấp (bỏ sót fraud):**
```go
// Giảm thresholds
config.CriticalThreshold -= 10
config.HighThreshold -= 10
// Hoặc tăng multipliers
config.CriticalMultiplier = 1.2
```

---

## 📈 Ví dụ thực tế

### Scenario 1: Demo cho khách hàng

**Mục tiêu:** Ít false positives, tránh làm khách nghi ngờ

```go
config := services.ConservativeConfig()
// Hoặc
config := &services.FraudDetectionConfig{
    CriticalThreshold: 100,
    HighThreshold:     85,
    MediumThreshold:   70,
    LowThreshold:      50,
}
```

### Scenario 2: Bảo vệ người già

**Mục tiêu:** Phát hiện tối đa, chấp nhận false positives

```go
config := services.AggressiveConfig()
// Hoặc
config := &services.FraudDetectionConfig{
    CriticalThreshold: 70,
    HighThreshold:     50,
    MediumThreshold:   30,
    LowThreshold:      15,
}
```

### Scenario 3: Hackathon Demo

**Mục tiêu:** Cân bằng, impressive nhưng không quá nhạy

```go
// Dùng default - đã được optimize
config := services.DefaultFraudDetectionConfig()
```

---

## 🎯 Quick Decision Tree

```
Có quá nhiều cảnh báo sai?
├─ YES → Dùng ConservativeConfig()
└─ NO
    │
    Bỏ sót nhiều fraud?
    ├─ YES → Dùng AggressiveConfig()
    └─ NO → Giữ DefaultConfig() ✅
```

---

## 💡 Tips cho Hackathon

### DO ✅
- Dùng DefaultConfig() để bắt đầu
- Test với 5-10 cuộc gọi mẫu
- Điều chỉnh nhẹ nếu cần (+/- 5-10 điểm)
- Ghi chú lại config đã test

### DON'T ❌
- Không over-tune (waste time)
- Không thay đổi quá nhiều lần
- Không optimize quá sớm
- Không quên document config đã chọn

---

## 📝 Template để test

```go
package main

import (
    "fmt"
    "github.com/fraudguard/api-gateway/internal/services"
)

func main() {
    // Test cases
    testCases := []struct{
        name string
        text string
    }{
        {"Fraud 1", "Tôi là công an, chuyển tiền ngay"},
        {"Fraud 2", "Cung cấp mã OTP để xác nhận"},
        {"Normal 1", "Xin chào, tôi muốn đặt hàng"},
        {"Normal 2", "Cảm ơn bạn đã gọi"},
    }
    
    // Test với các config khác nhau
    configs := map[string]*services.FraudDetectionConfig{
        "Default":      services.DefaultFraudDetectionConfig(),
        "Conservative": services.ConservativeConfig(),
        "Aggressive":   services.AggressiveConfig(),
    }
    
    for configName, config := range configs {
        fmt.Printf("\n=== Testing with %s Config ===\n", configName)
        detector := services.NewFraudDetectorWithConfig("test", config)
        
        for _, tc := range testCases {
            result := detector.AnalyzeText(tc.text)
            fmt.Printf("%s: Score=%d, Action=%s, Alert=%v\n",
                tc.name, result.RiskScore, result.Action, result.IsAlert)
        }
        
        detector.ResetSession()
    }
}
```

---

## 🚀 Recommended Config cho từng giai đoạn

### Phase 1: Hackathon Demo (Hiện tại)
```go
services.DefaultFraudDetectionConfig()
// Critical: 90, High: 70, Medium: 50, Low: 30
```

### Phase 2: Beta Testing
```go
services.ConservativeConfig()
// Ít false positives để user không bực mình
```

### Phase 3: Production
```go
// Custom based on real data
config := &services.FraudDetectionConfig{
    CriticalThreshold: 85,  // Từ A/B testing
    HighThreshold:     65,
    MediumThreshold:   45,
    LowThreshold:      25,
}
```

---

## 📞 Khi nào cần điều chỉnh?

**Điều chỉnh ngay nếu:**
- ❌ Demo bị quá nhiều false alarms
- ❌ Bỏ sót fraud rõ ràng trong demo
- ❌ Feedback từ judges/users

**Giữ nguyên nếu:**
- ✅ Demo chạy mượt
- ✅ Accuracy chấp nhận được
- ✅ Không có complaints

---

## 🎉 Kết luận

**Cho Hackathon:**
1. ✅ Dùng DefaultConfig()
2. ✅ Test với 5-10 cases
3. ✅ Chỉ điều chỉnh nếu thực sự cần
4. ✅ Document config cuối cùng

**Remember:**
> "Perfect is the enemy of good" - Voltaire

Đừng waste time tuning quá kỹ. Focus vào features và presentation! 🚀
