package services

import (
	"log"
	"os"
	"strconv"
)

// FraudDetectionConfig holds configurable thresholds for fraud detection
type FraudDetectionConfig struct {
	// Risk score thresholds (0-100 scale)
	LowThreshold      int
	MediumThreshold   int
	HighThreshold     int
	CriticalThreshold int

	// Alert configuration
	MaxAlertsPerSession int
	AlertCooldownMs     int

	// Performance tuning
	MaxAudioAgeSeconds int // Drop audio older than this
	MaxBufferSize      int // Maximum audio buffer size
	ProcessIntervalMs  int // How often to process buffered audio
}

// DefaultFraudDetectionConfig returns the default configuration
// Optimized for Vietnamese fraud patterns based on testing
func DefaultFraudDetectionConfig() *FraudDetectionConfig {
	return &FraudDetectionConfig{
		// Adjusted thresholds - more sensitive to fraud patterns
		LowThreshold:      20, // Just informational
		MediumThreshold:   40, // Show warning (reduced from 50)
		HighThreshold:     60, // Alert user (reduced from 70)
		CriticalThreshold: 80, // Critical alert (reduced from 90)

		// Alert limits to prevent spam
		MaxAlertsPerSession: 10,
		AlertCooldownMs:     2000, // 2 seconds between alerts

		// Performance settings
		MaxAudioAgeSeconds: 5,     // Drop audio older than 5s
		MaxBufferSize:      32768, // 32KB buffer
		ProcessIntervalMs:  2000,  // Process every 2s
	}
}

// LoadFromEnvironment loads configuration from environment variables
// This allows production tuning without code changes
func LoadFromEnvironment() *FraudDetectionConfig {
	config := DefaultFraudDetectionConfig()

	// Load thresholds from environment
	if val := os.Getenv("FRAUD_THRESHOLD_LOW"); val != "" {
		if threshold, err := strconv.Atoi(val); err == nil {
			config.LowThreshold = threshold
			log.Printf("📊 [Config] LOW threshold set to %d from environment", threshold)
		}
	}

	if val := os.Getenv("FRAUD_THRESHOLD_MEDIUM"); val != "" {
		if threshold, err := strconv.Atoi(val); err == nil {
			config.MediumThreshold = threshold
			log.Printf("📊 [Config] MEDIUM threshold set to %d from environment", threshold)
		}
	}

	if val := os.Getenv("FRAUD_THRESHOLD_HIGH"); val != "" {
		if threshold, err := strconv.Atoi(val); err == nil {
			config.HighThreshold = threshold
			log.Printf("📊 [Config] HIGH threshold set to %d from environment", threshold)
		}
	}

	if val := os.Getenv("FRAUD_THRESHOLD_CRITICAL"); val != "" {
		if threshold, err := strconv.Atoi(val); err == nil {
			config.CriticalThreshold = threshold
			log.Printf("📊 [Config] CRITICAL threshold set to %d from environment", threshold)
		}
	}

	// Load performance settings
	if val := os.Getenv("FRAUD_MAX_AUDIO_AGE_SECONDS"); val != "" {
		if age, err := strconv.Atoi(val); err == nil {
			config.MaxAudioAgeSeconds = age
			log.Printf("📊 [Config] Max audio age set to %ds from environment", age)
		}
	}

	log.Printf("✅ [Config] Fraud detection config loaded - Thresholds: LOW=%d, MEDIUM=%d, HIGH=%d, CRITICAL=%d",
		config.LowThreshold, config.MediumThreshold, config.HighThreshold, config.CriticalThreshold)

	return config
}

// Validate checks if the configuration is valid
func (c *FraudDetectionConfig) Validate() error {
	// Ensure thresholds are in ascending order
	if c.LowThreshold >= c.MediumThreshold ||
		c.MediumThreshold >= c.HighThreshold ||
		c.HighThreshold >= c.CriticalThreshold {
		log.Printf("⚠️ [Config] WARNING: Thresholds not in ascending order!")
	}

	if c.MaxAudioAgeSeconds <= 0 {
		log.Printf("⚠️ [Config] WARNING: MaxAudioAgeSeconds should be positive")
	}

	return nil
}
