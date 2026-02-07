package handlers

import (
	"context"
	"crypto/rand"
	"encoding/json"
	"fmt"
	"log"
	"math/big"
	"net/http"
	"net/smtp"
	"os"
	"strings"
	"sync"
	"time"
)

// OTP storage (in-memory for simplicity, use Redis/DB for production)
type OTPEntry struct {
	Code      string
	Email     string
	ExpiresAt time.Time
	Verified  bool
}

var (
	otpStore = make(map[string]*OTPEntry)
	otpMutex sync.RWMutex
)

// SendOTPRequest is the request body for sending OTP
type SendOTPRequest struct {
	Email string `json:"email"`
}

// VerifyOTPRequest is the request body for verifying OTP
type VerifyOTPRequest struct {
	Email string `json:"email"`
	OTP   string `json:"otp"`
}

// GenerateOTP generates a 6-digit OTP
func GenerateOTP() string {
	n, _ := rand.Int(rand.Reader, big.NewInt(1000000))
	return fmt.Sprintf("%06d", n.Int64())
}

// SendOTP handles the OTP sending request
func SendOTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req SendOTPRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		sendJSONError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	email := strings.TrimSpace(strings.ToLower(req.Email))
	if email == "" || !strings.Contains(email, "@") {
		sendJSONError(w, "Email không hợp lệ", http.StatusBadRequest)
		return
	}

	// Generate OTP
	otp := GenerateOTP()
	expiresAt := time.Now().Add(5 * time.Minute) // OTP expires in 5 minutes

	// Store OTP
	otpMutex.Lock()
	otpStore[email] = &OTPEntry{
		Code:      otp,
		Email:     email,
		ExpiresAt: expiresAt,
		Verified:  false,
	}
	otpMutex.Unlock()

	// Send email
	if err := sendOTPEmail(email, otp); err != nil {
		log.Printf("❌ [Auth] Failed to send OTP to %s: %v", email, err)
		sendJSONError(w, "Không thể gửi email. Vui lòng thử lại.", http.StatusInternalServerError)
		return
	}

	log.Printf("✅ [Auth] OTP sent to %s", email)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success":    true,
		"message":    "OTP đã được gửi đến email của bạn",
		"expires_in": 300, // 5 minutes in seconds
	})
}

// VerifyOTP handles the OTP verification request
func VerifyOTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req VerifyOTPRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		sendJSONError(w, "Invalid request body", http.StatusBadRequest)
		return
	}

	email := strings.TrimSpace(strings.ToLower(req.Email))
	otp := strings.TrimSpace(req.OTP)

	if email == "" || otp == "" {
		sendJSONError(w, "Email và OTP là bắt buộc", http.StatusBadRequest)
		return
	}

	// Check OTP
	otpMutex.RLock()
	entry, exists := otpStore[email]
	otpMutex.RUnlock()

	if !exists {
		sendJSONError(w, "OTP không tồn tại. Vui lòng yêu cầu OTP mới.", http.StatusBadRequest)
		return
	}

	if time.Now().After(entry.ExpiresAt) {
		// Remove expired OTP
		otpMutex.Lock()
		delete(otpStore, email)
		otpMutex.Unlock()
		sendJSONError(w, "OTP đã hết hạn. Vui lòng yêu cầu OTP mới.", http.StatusBadRequest)
		return
	}

	if entry.Code != otp {
		sendJSONError(w, "OTP không chính xác", http.StatusBadRequest)
		return
	}

	// Mark as verified and generate session token
	otpMutex.Lock()
	entry.Verified = true
	otpMutex.Unlock()

	// Generate a simple session token (in production, use JWT)
	sessionToken := generateSessionToken(email)

	log.Printf("✅ [Auth] OTP verified for %s", email)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"message": "Đăng nhập thành công!",
		"token":   sessionToken,
		"email":   email,
		"user_id": generateUserID(email),
	})

	// Clean up used OTP
	otpMutex.Lock()
	delete(otpStore, email)
	otpMutex.Unlock()
}

// CheckSession validates a session token
func CheckSession(w http.ResponseWriter, r *http.Request) {
	token := r.Header.Get("Authorization")
	if token == "" {
		token = r.URL.Query().Get("token")
	}

	if token == "" {
		sendJSONError(w, "Token không được cung cấp", http.StatusUnauthorized)
		return
	}

	// Remove "Bearer " prefix if present
	token = strings.TrimPrefix(token, "Bearer ")

	// Validate token (simple validation for demo)
	if len(token) < 32 {
		sendJSONError(w, "Token không hợp lệ", http.StatusUnauthorized)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"valid":   true,
	})
}

// sendOTPEmail sends the OTP via email
func sendOTPEmail(toEmail, otp string) error {
	// Get SMTP settings from environment
	smtpHost := getEnv("SMTP_HOST", "smtp.gmail.com")
	smtpPort := getEnv("SMTP_PORT", "587")
	smtpUser := getEnv("SMTP_USER", "")
	smtpPass := getEnv("SMTP_PASS", "")

	if smtpUser == "" || smtpPass == "" {
		// For demo/testing, just log the OTP
		log.Printf("📧 [Demo Mode] OTP for %s: %s", toEmail, otp)
		return nil // Return success for demo
	}

	// Create email message
	from := smtpUser
	to := []string{toEmail}
	subject := "FraudGuard AI - Mã xác thực OTP"
	body := fmt.Sprintf(`
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
</head>
<body style="font-family: Arial, sans-serif; background-color: #0D1B2A; color: #E0E6ED; padding: 20px;">
    <div style="max-width: 600px; margin: 0 auto; background-color: #1B2838; padding: 30px; border-radius: 12px;">
        <h1 style="color: #34D399; text-align: center;">🛡️ FraudGuard AI</h1>
        <h2 style="text-align: center;">Mã xác thực OTP của bạn</h2>
        <div style="background-color: #0D1B2A; padding: 20px; border-radius: 8px; text-align: center; margin: 20px 0;">
            <span style="font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #34D399;">%s</span>
        </div>
        <p style="text-align: center; color: #8B95A5;">Mã này sẽ hết hạn sau <strong>5 phút</strong></p>
        <p style="text-align: center; color: #8B95A5; font-size: 12px;">Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
    </div>
</body>
</html>
`, otp)

	message := fmt.Sprintf("From: %s\r\n"+
		"To: %s\r\n"+
		"Subject: %s\r\n"+
		"MIME-Version: 1.0\r\n"+
		"Content-Type: text/html; charset=UTF-8\r\n"+
		"\r\n%s", from, toEmail, subject, body)

	// Send email
	auth := smtp.PlainAuth("", smtpUser, smtpPass, smtpHost)
	err := smtp.SendMail(smtpHost+":"+smtpPort, auth, from, to, []byte(message))
	return err
}

// Helper functions
func sendJSONError(w http.ResponseWriter, message string, status int) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": false,
		"error":   message,
	})
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func generateSessionToken(email string) string {
	// Simple token generation (use JWT in production)
	b := make([]byte, 32)
	rand.Read(b)
	return fmt.Sprintf("%x", b)
}

func generateUserID(email string) string {
	// Simple user ID from email hash
	b := make([]byte, 8)
	rand.Read(b)
	return fmt.Sprintf("user_%x", b)
}

// CleanupExpiredOTPs removes expired OTPs periodically
func CleanupExpiredOTPs(ctx context.Context) {
	ticker := time.NewTicker(1 * time.Minute)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			otpMutex.Lock()
			now := time.Now()
			for email, entry := range otpStore {
				if now.After(entry.ExpiresAt) {
					delete(otpStore, email)
				}
			}
			otpMutex.Unlock()
		}
	}
}
