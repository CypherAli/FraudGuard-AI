package middleware

import (
	"log"
	"net/http"
	"sync"
	"time"
)

// RateLimiter provides per-IP rate limiting
type RateLimiter struct {
	mu       sync.RWMutex
	clients  map[string]*clientBucket
	rate     int           // max requests per window
	window   time.Duration // time window
	stopGC   chan struct{}
}

type clientBucket struct {
	count     int
	windowEnd time.Time
}

// NewRateLimiter creates a new rate limiter
// rate: max requests allowed, window: time window for rate limiting
func NewRateLimiter(rate int, window time.Duration) *RateLimiter {
	rl := &RateLimiter{
		clients: make(map[string]*clientBucket),
		rate:    rate,
		window:  window,
		stopGC:  make(chan struct{}),
	}

	// Cleanup expired entries every minute
	go rl.cleanup()

	return rl
}

// Allow checks if a client is within rate limits
func (rl *RateLimiter) Allow(clientIP string) bool {
	rl.mu.Lock()
	defer rl.mu.Unlock()

	now := time.Now()
	bucket, exists := rl.clients[clientIP]

	if !exists || now.After(bucket.windowEnd) {
		// New window
		rl.clients[clientIP] = &clientBucket{
			count:     1,
			windowEnd: now.Add(rl.window),
		}
		return true
	}

	bucket.count++
	if bucket.count > rl.rate {
		return false
	}

	return true
}

// HTTPMiddleware returns an HTTP middleware that applies rate limiting
func (rl *RateLimiter) HTTPMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		clientIP := r.RemoteAddr
		if xForward := r.Header.Get("X-Forwarded-For"); xForward != "" {
			clientIP = xForward
		}

		if !rl.Allow(clientIP) {
			log.Printf("🚫 Rate limit exceeded for %s", clientIP)
			http.Error(w, `{"error": "Quá nhiều yêu cầu, vui lòng thử lại sau"}`, http.StatusTooManyRequests)
			return
		}

		next.ServeHTTP(w, r)
	})
}

// WSRateLimit checks WebSocket message rate for a device
// Returns true if allowed, false if rate limited
func (rl *RateLimiter) WSRateLimit(deviceID string) bool {
	return rl.Allow("ws:" + deviceID)
}

// cleanup periodically removes expired entries
func (rl *RateLimiter) cleanup() {
	ticker := time.NewTicker(1 * time.Minute)
	defer ticker.Stop()

	for {
		select {
		case <-rl.stopGC:
			return
		case <-ticker.C:
			rl.mu.Lock()
			now := time.Now()
			for key, bucket := range rl.clients {
				if now.After(bucket.windowEnd) {
					delete(rl.clients, key)
				}
			}
			rl.mu.Unlock()
		}
	}
}

// Stop stops the rate limiter cleanup goroutine
func (rl *RateLimiter) Stop() {
	close(rl.stopGC)
}

// Global rate limiters
var (
	// API rate limiter: 100 requests per minute per IP
	APILimiter = NewRateLimiter(100, 1*time.Minute)

	// WebSocket rate limiter: 60 messages per second per device (audio chunks)
	WSLimiter = NewRateLimiter(60, 1*time.Second)
)
