package handlers

import (
	"context"
	"crypto/hmac"
	"crypto/rand"
	"crypto/sha256"
	"crypto/subtle"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"log"
	"math/big"
	"net/http"
	"bytes"
	"io"
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

// Session token storage
type SessionEntry struct {
	Email     string
	Token     string
	CreatedAt time.Time
	ExpiresAt time.Time
}

// Rate limiter entry
type RateLimitEntry struct {
	Count     int
	FirstSent time.Time
}

var (
	otpStore          = make(map[string]*OTPEntry)
	otpMutex          sync.RWMutex
	sessionStore      = make(map[string]*SessionEntry)
	sessionMutex      sync.RWMutex
	rateLimiter       = make(map[string]*RateLimitEntry)   // OTP send rate limiter
	verifyRateLimiter = make(map[string]*RateLimitEntry)   // OTP verify rate limiter
	rateMutex         sync.Mutex
)

const (
	maxOTPRequestsPerWindow   = 3
	rateLimitWindow           = 10 * time.Minute
	maxVerifyAttemptsPerWindow = 5
	verifyRateLimitWindow     = 15 * time.Minute
	sessionExpiry             = 30 * 24 * time.Hour // 30 days
)

// ── JWT (stateless — survives server restarts, no session store needed) ──────

type jwtClaims struct {
	Email string `json:"email"`
	Exp   int64  `json:"exp"`
	Iat   int64  `json:"iat"`
}

// getJWTSecret reads JWT_SECRET from env (set this on Render.com → stays across restarts).
func getJWTSecret() []byte {
	secret := os.Getenv("JWT_SECRET")
	if secret == "" {
		// Fallback for local dev — set JWT_SECRET in production!
		secret = "fraudguard-jwt-secret-CHANGE-IN-PROD"
		log.Println("⚠️  [Auth] JWT_SECRET not set — using default (unsafe for production)")
	}
	return []byte(secret)
}

// generateJWT creates a signed HS256 JWT that expires in sessionExpiry.
func generateJWT(email string) (string, error) {
	header := base64.RawURLEncoding.EncodeToString([]byte(`{"alg":"HS256","typ":"JWT"}`))
	claims := jwtClaims{
		Email: email,
		Exp:   time.Now().Add(sessionExpiry).Unix(),
		Iat:   time.Now().Unix(),
	}
	claimsJSON, err := json.Marshal(claims)
	if err != nil {
		return "", err
	}
	payload := base64.RawURLEncoding.EncodeToString(claimsJSON)
	signing := header + "." + payload
	mac := hmac.New(sha256.New, getJWTSecret())
	mac.Write([]byte(signing))
	sig := base64.RawURLEncoding.EncodeToString(mac.Sum(nil))
	return signing + "." + sig, nil
}

// validateJWT verifies the HS256 signature and expiry of a JWT.
// Returns true only if signature is valid AND token has not expired.
func validateJWT(tokenStr string) bool {
	parts := strings.Split(tokenStr, ".")
	if len(parts) != 3 {
		return false
	}
	// 1. Verify signature
	signing := parts[0] + "." + parts[1]
	mac := hmac.New(sha256.New, getJWTSecret())
	mac.Write([]byte(signing))
	expectedSig := base64.RawURLEncoding.EncodeToString(mac.Sum(nil))
	if !hmac.Equal([]byte(expectedSig), []byte(parts[2])) {
		return false
	}
	// 2. Verify expiry
	claimsJSON, err := base64.RawURLEncoding.DecodeString(parts[1])
	if err != nil {
		return false
	}
	var claims jwtClaims
	if err := json.Unmarshal(claimsJSON, &claims); err != nil {
		return false
	}
	return time.Now().Unix() < claims.Exp
}

// HandleValidateToken — GET /auth/validate-token
// Lightweight check used by the mobile app BEFORE attempting WebSocket connection.
// Returns 200 {"valid":true} or 401 {"valid":false}.
func HandleValidateToken(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	token := strings.TrimPrefix(r.Header.Get("Authorization"), "Bearer ")
	if token == "" {
		token = r.URL.Query().Get("token")
	}
	if !ValidateToken(token) {
		w.WriteHeader(http.StatusUnauthorized)
		json.NewEncoder(w).Encode(map[string]any{"valid": false, "message": "Token hết hạn hoặc không hợp lệ"})
		return
	}
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]any{"valid": true})
}

// ── End JWT section ───────────────────────────────────────────────────────────

// SendOTPRequest is the request body for sending OTP
type SendOTPRequest struct {
	Email string `json:"email"`
}

// VerifyOTPRequest is the request body for verifying OTP
type VerifyOTPRequest struct {
	Email string `json:"email"`
	OTP   string `json:"otp"`
}

// GenerateOTP generates a 6-digit OTP using cryptographic random
func GenerateOTP() (string, error) {
	n, err := rand.Int(rand.Reader, big.NewInt(1000000))
	if err != nil {
		return "", fmt.Errorf("failed to generate OTP: %w", err)
	}
	return fmt.Sprintf("%06d", n.Int64()), nil
}

// checkRateLimitFor is a generic rate-limit helper for any in-memory map.
// Returns true if allowed, false if the limit is exceeded.
func checkRateLimitFor(store map[string]*RateLimitEntry, key string, maxAttempts int, window time.Duration) bool {
	rateMutex.Lock()
	defer rateMutex.Unlock()

	entry, exists := store[key]
	if !exists {
		store[key] = &RateLimitEntry{Count: 1, FirstSent: time.Now()}
		return true
	}

	if time.Since(entry.FirstSent) > window {
		store[key] = &RateLimitEntry{Count: 1, FirstSent: time.Now()}
		return true
	}

	if entry.Count >= maxAttempts {
		return false
	}

	entry.Count++
	return true
}

// checkRateLimit checks if OTP sending is rate-limited for this email (3 per 10 min).
func checkRateLimit(email string) bool {
	return checkRateLimitFor(rateLimiter, email, maxOTPRequestsPerWindow, rateLimitWindow)
}

// checkVerifyRateLimit protects /auth/verify-otp from brute-force (5 per 15 min).
func checkVerifyRateLimit(email string) bool {
	return checkRateLimitFor(verifyRateLimiter, email, maxVerifyAttemptsPerWindow, verifyRateLimitWindow)
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

	// Rate limiting check
	if !checkRateLimit(email) {
		sendJSONError(w, "Bạn đã gửi quá nhiều yêu cầu OTP. Vui lòng thử lại sau 10 phút.", http.StatusTooManyRequests)
		return
	}

	// Generate OTP
	otp, err := GenerateOTP()
	if err != nil {
		log.Printf("❌ [Auth] Failed to generate OTP: %v", err)
		sendJSONError(w, "Lỗi hệ thống. Vui lòng thử lại.", http.StatusInternalServerError)
		return
	}
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

	// Brute-force protection: max 5 verify attempts per 15 minutes
	if !checkVerifyRateLimit(email) {
		sendJSONError(w, "Quá nhiều lần thử xác thực. Vui lòng đợi 15 phút.", http.StatusTooManyRequests)
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

	// Constant-time comparison to prevent timing attacks
	if subtle.ConstantTimeCompare([]byte(entry.Code), []byte(otp)) != 1 {
		sendJSONError(w, "OTP không chính xác", http.StatusBadRequest)
		return
	}

	// Mark as verified and generate session token
	otpMutex.Lock()
	entry.Verified = true
	otpMutex.Unlock()

	// Generate a session token and store it
	sessionToken, err := generateSessionToken(email)
	if err != nil {
		log.Printf("❌ [Auth] Failed to generate session token: %v", err)
		sendJSONError(w, "Lỗi hệ thống. Vui lòng thử lại.", http.StatusInternalServerError)
		return
	}

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
		sendJSONError(w, "Token không được cung cấp", http.StatusUnauthorized)
		return
	}

	// Remove "Bearer " prefix if present
	token = strings.TrimPrefix(token, "Bearer ")

	// Validate token against stored sessions
	sessionMutex.RLock()
	session, exists := sessionStore[token]
	sessionMutex.RUnlock()

	if !exists {
		sendJSONError(w, "Token không hợp lệ", http.StatusUnauthorized)
		return
	}

	if time.Now().After(session.ExpiresAt) {
		// Remove expired session
		sessionMutex.Lock()
		delete(sessionStore, token)
		sessionMutex.Unlock()
		sendJSONError(w, "Phiên đăng nhập đã hết hạn", http.StatusUnauthorized)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": true,
		"valid":   true,
		"email":   session.Email,
	})
}

// sendOTPEmail gửi OTP qua Brevo Transactional Email API.
// Biến môi trường cần set: BREVO_API_KEY
func sendOTPEmail(toEmail, otp string) error {
	apiKey := getEnv("BREVO_API_KEY", "")
	if apiKey == "" {
		log.Printf("⚠️  [Auth] BREVO_API_KEY chưa được cấu hình — bỏ qua gửi email cho %s", toEmail)
		return fmt.Errorf("BREVO_API_KEY not set")
	}

	fromEmail := getEnv("BREVO_FROM_EMAIL", "a2020lehong@gmail.com")
	fromName := getEnv("BREVO_FROM_NAME", "FraudGuard AI")

	htmlBody := fmt.Sprintf(`<!DOCTYPE html>
<html><head><meta charset="UTF-8"></head>
<body style="font-family:Arial,sans-serif;background:#0D1B2A;color:#E0E6ED;padding:20px;">
  <div style="max-width:600px;margin:0 auto;background:#1B2838;padding:30px;border-radius:12px;">
    <h1 style="color:#34D399;text-align:center;">FraudGuard AI</h1>
    <h2 style="text-align:center;">Mã xác thực OTP của bạn</h2>
    <div style="background:#0D1B2A;padding:20px;border-radius:8px;text-align:center;margin:20px 0;">
      <span style="font-size:36px;font-weight:bold;letter-spacing:8px;color:#34D399;">%s</span>
    </div>
    <p style="text-align:center;color:#8B95A5;">Mã này sẽ hết hạn sau <strong>5 phút</strong></p>
    <p style="text-align:center;color:#8B95A5;font-size:12px;">Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
  </div>
</body></html>`, otp)

	payload := fmt.Sprintf(`{
		"sender":      {"email": %q, "name": %q},
		"to":          [{"email": %q}],
		"subject":     "FraudGuard AI - Mã xác thực OTP",
		"htmlContent": %q
	}`, fromEmail, fromName, toEmail, htmlBody)

	req, err := http.NewRequest(http.MethodPost,
		"https://api.brevo.com/v3/smtp/email",
		bytes.NewBufferString(payload),
	)
	if err != nil {
		return fmt.Errorf("brevo: tạo request thất bại: %w", err)
	}
	req.Header.Set("api-key", apiKey)
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("Accept", "application/json")

	client := &http.Client{Timeout: 10 * time.Second}
	resp, err := client.Do(req)
	if err != nil {
		return fmt.Errorf("brevo: request thất bại: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 300 {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("brevo: API lỗi %d: %s", resp.StatusCode, string(body))
	}

	log.Printf("✅ [Auth] Email OTP đã gửi qua Brevo tới %s", toEmail)
	return nil
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

func generateSessionToken(email string) (string, error) {
	// Use JWT — self-validating, survives server restarts
	token, err := generateJWT(email)
	if err != nil {
		return "", fmt.Errorf("failed to generate JWT: %w", err)
	}
	// Keep sessionStore for CheckSession endpoint backwards-compat
	sessionMutex.Lock()
	sessionStore[token] = &SessionEntry{
		Email:     email,
		Token:     token,
		CreatedAt: time.Now(),
		ExpiresAt: time.Now().Add(sessionExpiry),
	}
	sessionMutex.Unlock()
	log.Printf("🔑 [Auth] JWT issued for %s (expires %s)", email, time.Now().Add(sessionExpiry).Format(time.RFC3339))
	return token, nil
}

func generateUserID(_ string) string {
	b := make([]byte, 8)
	if _, err := rand.Read(b); err != nil {
		// Fallback to timestamp-based ID
		return fmt.Sprintf("user_%d", time.Now().UnixNano())
	}
	return fmt.Sprintf("user_%x", b)
}

// ValidateToken checks whether a bearer token is active and not expired.
// Used by auth middleware and WebSocket handler.
// Priority: JWT validation first (stateless — works after restart),
// then session store fallback (for any legacy non-JWT tokens).
func ValidateToken(token string) bool {
	if token == "" {
		return false
	}
	// JWT path — no server state needed, works across restarts
	if validateJWT(token) {
		return true
	}
	// Fallback: legacy random-hex session tokens (migration period)
	sessionMutex.RLock()
	session, exists := sessionStore[token]
	sessionMutex.RUnlock()
	return exists && time.Now().Before(session.ExpiresAt)
}

// CleanupExpiredOTPs removes expired OTPs and sessions periodically
func CleanupExpiredOTPs(ctx context.Context) {
	ticker := time.NewTicker(1 * time.Minute)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			return
		case <-ticker.C:
			now := time.Now()

			// Cleanup OTPs
			otpMutex.Lock()
			for email, entry := range otpStore {
				if now.After(entry.ExpiresAt) {
					delete(otpStore, email)
				}
			}
			otpMutex.Unlock()

			// Cleanup expired sessions
			sessionMutex.Lock()
			for token, session := range sessionStore {
				if now.After(session.ExpiresAt) {
					delete(sessionStore, token)
				}
			}
			sessionMutex.Unlock()

			// Cleanup send rate limiter
			rateMutex.Lock()
			for email, entry := range rateLimiter {
				if time.Since(entry.FirstSent) > rateLimitWindow {
					delete(rateLimiter, email)
				}
			}
			// Cleanup verify rate limiter
			for email, entry := range verifyRateLimiter {
				if time.Since(entry.FirstSent) > verifyRateLimitWindow {
					delete(verifyRateLimiter, email)
				}
			}
			rateMutex.Unlock()
		}
	}
}
