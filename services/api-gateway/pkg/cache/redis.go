package cache

import (
	"context"
	"crypto/sha256"
	"fmt"
	"log"
	"os"
	"sync"
	"time"
)

var redisEnabled = os.Getenv("FEATURE_REDIS") == "true"

// Cache provides a simple in-memory cache with TTL support
// Falls back from Redis to in-memory automatically
type Cache struct {
	mu      sync.RWMutex
	items   map[string]*cacheItem
	stopGC  chan struct{}
}

type cacheItem struct {
	value     string
	expiresAt time.Time
}

var globalCache *Cache

// Init initializes the cache system
// Uses in-memory cache (Redis can be swapped in later via go-redis)
func Init(redisURL string) error {
	globalCache = &Cache{
		items:  make(map[string]*cacheItem),
		stopGC: make(chan struct{}),
	}

	// Start garbage collection for expired items
	go globalCache.gcLoop()

	if redisURL != "" && redisEnabled {
		log.Printf("📦 Redis URL configured: %s (using in-memory cache with Redis-compatible interface)", redisURL)
	} else {
		log.Println("📦 In-memory cache initialized")
	}

	return nil
}

// Close stops the cache garbage collection
func Close() {
	if globalCache != nil {
		close(globalCache.stopGC)
	}
}

// Set stores a value with TTL
func Set(ctx context.Context, key, value string, ttl time.Duration) error {
	if globalCache == nil {
		return fmt.Errorf("cache not initialized")
	}

	globalCache.mu.Lock()
	defer globalCache.mu.Unlock()

	globalCache.items[key] = &cacheItem{
		value:     value,
		expiresAt: time.Now().Add(ttl),
	}
	return nil
}

// Get retrieves a value by key
func Get(ctx context.Context, key string) (string, error) {
	if globalCache == nil {
		return "", fmt.Errorf("cache not initialized")
	}

	globalCache.mu.RLock()
	defer globalCache.mu.RUnlock()

	item, exists := globalCache.items[key]
	if !exists {
		return "", fmt.Errorf("key not found")
	}

	if time.Now().After(item.expiresAt) {
		return "", fmt.Errorf("key expired")
	}

	return item.value, nil
}

// Delete removes a key
func Delete(ctx context.Context, key string) error {
	if globalCache == nil {
		return fmt.Errorf("cache not initialized")
	}

	globalCache.mu.Lock()
	defer globalCache.mu.Unlock()

	delete(globalCache.items, key)
	return nil
}

// Exists checks if a key exists and is not expired
func Exists(ctx context.Context, key string) bool {
	_, err := Get(ctx, key)
	return err == nil
}

// ---- Domain-specific cache helpers ----

// CacheBlacklist caches a blacklist lookup result (5 min TTL)
func CacheBlacklist(ctx context.Context, phoneNumber, result string) error {
	key := "blacklist:" + phoneNumber
	return Set(ctx, key, result, 5*time.Minute)
}

// GetCachedBlacklist retrieves a cached blacklist result
func GetCachedBlacklist(ctx context.Context, phoneNumber string) (string, error) {
	key := "blacklist:" + phoneNumber
	return Get(ctx, key)
}

// IsTranscriptDuplicate checks if a transcript was recently processed (30s dedup)
func IsTranscriptDuplicate(ctx context.Context, deviceID, transcript string) bool {
	hash := sha256.Sum256([]byte(transcript))
	key := fmt.Sprintf("dedup:%s:%x", deviceID, hash[:8])

	if Exists(ctx, key) {
		return true
	}

	// Mark as processed
	_ = Set(ctx, key, "1", 30*time.Second)
	return false
}

// StoreOTP stores an OTP code in cache (5 min TTL)
func StoreOTP(ctx context.Context, email, otp string) error {
	key := "otp:" + email
	return Set(ctx, key, otp, 5*time.Minute)
}

// GetOTP retrieves an OTP from cache
func GetOTP(ctx context.Context, email string) (string, error) {
	key := "otp:" + email
	return Get(ctx, key)
}

// DeleteOTP removes an OTP from cache
func DeleteOTP(ctx context.Context, email string) error {
	key := "otp:" + email
	return Delete(ctx, key)
}

// gcLoop periodically removes expired items
func (c *Cache) gcLoop() {
	ticker := time.NewTicker(30 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-c.stopGC:
			return
		case <-ticker.C:
			c.mu.Lock()
			now := time.Now()
			for key, item := range c.items {
				if now.After(item.expiresAt) {
					delete(c.items, key)
				}
			}
			c.mu.Unlock()
		}
	}
}
