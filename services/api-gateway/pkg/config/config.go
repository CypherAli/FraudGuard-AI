package config

import (
	"fmt"
	"log"
	"os"
	"strconv"
	"strings"
	"time"

	"github.com/joho/godotenv"
)

// Config holds all application configuration
type Config struct {
	Database  DatabaseConfig
	Server    ServerConfig
	WebSocket WebSocketConfig
	AI        AIConfig
	Redis     RedisConfig
}

// DatabaseConfig holds database connection settings
type DatabaseConfig struct {
	Host     string
	Port     int
	User     string
	Password string
	DBName   string
	SSLMode  string
	MaxConns int
	MinConns int
}

// ServerConfig holds HTTP server settings
type ServerConfig struct {
	Host         string
	Port         int
	ReadTimeout  time.Duration
	WriteTimeout time.Duration
}

// WebSocketConfig holds WebSocket settings
type WebSocketConfig struct {
	ReadBufferSize  int
	WriteBufferSize int
	PingInterval    time.Duration
	PongTimeout     time.Duration
}

// AIConfig holds AI service API keys
type AIConfig struct {
	DeepgramAPIKey string
	GeminiAPIKey   string
	// AWS Amazon Transcribe — fallback STT khi Deepgram circuit breaker open
	AWSAccessKeyID     string
	AWSSecretAccessKey string
	AWSRegion          string
}

// RedisConfig holds Redis connection settings
type RedisConfig struct {
	URL string
}

// Load reads configuration from environment variables
func Load() (*Config, error) {
	// Load .env file if it exists (ignore error if not found)
	_ = godotenv.Load()

	// Check if DATABASE_URL is set (Render/Heroku style)
	databaseURL := os.Getenv("DATABASE_URL")

	var dbConfig DatabaseConfig
	if databaseURL != "" {
		// Parse DATABASE_URL (Render provides this)
		dbConfig = DatabaseConfig{
			MaxConns: getEnvAsInt("DB_MAX_CONNS", 25),
			MinConns: getEnvAsInt("DB_MIN_CONNS", 5),
		}
		// We'll use DATABASE_URL directly in GetDSN()
	} else {
		// Use individual env vars (local development)
		dbConfig = DatabaseConfig{
			Host:     getEnv("DB_HOST", "localhost"),
			Port:     getEnvAsInt("DB_PORT", 5432),
			User:     getEnv("DB_USER", "fraudguard"),
			Password: getEnv("DB_PASSWORD", ""),
			DBName:   getEnv("DB_NAME", "fraudguard_db"),
			SSLMode:  getEnv("DB_SSLMODE", "disable"),
			MaxConns: getEnvAsInt("DB_MAX_CONNS", 25),
			MinConns: getEnvAsInt("DB_MIN_CONNS", 5),
		}
	}

	// Render uses PORT env var, not SERVER_PORT
	port := getEnvAsInt("PORT", 0)
	if port == 0 {
		port = getEnvAsInt("SERVER_PORT", 8080)
	}

	cfg := &Config{
		Database: dbConfig,
		Server: ServerConfig{
			Host:         getEnv("SERVER_HOST", "0.0.0.0"),
			Port:         port,
			ReadTimeout:  getEnvAsDuration("SERVER_READ_TIMEOUT", 15*time.Second),
			WriteTimeout: getEnvAsDuration("SERVER_WRITE_TIMEOUT", 15*time.Second),
		},
		WebSocket: WebSocketConfig{
			ReadBufferSize:  getEnvAsInt("WS_READ_BUFFER_SIZE", 1024),
			WriteBufferSize: getEnvAsInt("WS_WRITE_BUFFER_SIZE", 1024),
			PingInterval:    getEnvAsDuration("WS_PING_INTERVAL", 60*time.Second),
			PongTimeout:     getEnvAsDuration("WS_PONG_TIMEOUT", 10*time.Second),
		},
		AI: AIConfig{
			DeepgramAPIKey:     getEnv("DEEPGRAM_API_KEY", ""),
			GeminiAPIKey:       getEnv("GEMINI_API_KEY", ""),
			AWSAccessKeyID:     getEnv("AWS_ACCESS_KEY_ID", ""),
			AWSSecretAccessKey: getEnv("AWS_SECRET_ACCESS_KEY", ""),
			AWSRegion:          getEnv("AWS_REGION", "ap-southeast-1"),
		},
		Redis: RedisConfig{
			URL: getEnv("REDIS_URL", ""),
		},
	}

	return cfg, nil
}

// Validate checks the configuration for common issues and logs warnings.
// It does NOT return an error for missing optional fields (AI keys, Redis, DB)
// because the server degrades gracefully without them. It only errors on
// clearly invalid settings that would cause a runtime failure.
func (c *Config) Validate() error {
	var warnings []string

	// Server
	if c.Server.Port <= 0 || c.Server.Port > 65535 {
		return fmt.Errorf("invalid SERVER_PORT=%d: must be 1-65535", c.Server.Port)
	}
	if c.Server.ReadTimeout < time.Second {
		warnings = append(warnings, fmt.Sprintf("SERVER_READ_TIMEOUT=%v is very low (<1s)", c.Server.ReadTimeout))
	}
	if c.Server.WriteTimeout < time.Second {
		warnings = append(warnings, fmt.Sprintf("SERVER_WRITE_TIMEOUT=%v is very low (<1s)", c.Server.WriteTimeout))
	}

	// Database — warn if neither DATABASE_URL nor DB_PASSWORD is set
	hasDatabaseURL := os.Getenv("DATABASE_URL") != ""
	if !hasDatabaseURL && c.Database.Password == "" {
		warnings = append(warnings, "No DATABASE_URL or DB_PASSWORD set — blacklist features will be unavailable")
	}

	// AI keys — warn if missing (features degrade gracefully)
	if c.AI.DeepgramAPIKey == "" {
		warnings = append(warnings, "DEEPGRAM_API_KEY not set — speech-to-text transcription will be disabled")
	}
	if c.AI.GeminiAPIKey == "" {
		warnings = append(warnings, "GEMINI_API_KEY not set — AI fraud analysis and call summaries will be disabled")
	}

	// CORS — warn if wildcard in non-development
	goEnv := strings.ToLower(getEnv("GO_ENV", "development"))
	corsOrigin := getEnv("CORS_ALLOWED_ORIGIN", "*")
	if goEnv == "production" && corsOrigin == "*" {
		warnings = append(warnings, "CORS_ALLOWED_ORIGIN=* is insecure in production. Set it to your app's domain.")
	}

	// WebSocket buffers
	if c.WebSocket.ReadBufferSize < 512 {
		warnings = append(warnings, fmt.Sprintf("WS_READ_BUFFER_SIZE=%d is very small", c.WebSocket.ReadBufferSize))
	}
	if c.WebSocket.WriteBufferSize < 512 {
		warnings = append(warnings, fmt.Sprintf("WS_WRITE_BUFFER_SIZE=%d is very small", c.WebSocket.WriteBufferSize))
	}

	// Log all warnings
	for _, w := range warnings {
		log.Printf("⚠️  [Config] %s", w)
	}

	return nil
}

// Helper functions to read environment variables with defaults
func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func getEnvAsInt(key string, defaultValue int) int {
	valueStr := os.Getenv(key)
	if valueStr == "" {
		return defaultValue
	}
	value, err := strconv.Atoi(valueStr)
	if err != nil {
		return defaultValue
	}
	return value
}

func getEnvAsDuration(key string, defaultValue time.Duration) time.Duration {
	valueStr := os.Getenv(key)
	if valueStr == "" {
		return defaultValue
	}
	value, err := time.ParseDuration(valueStr)
	if err != nil {
		return defaultValue
	}
	return value
}

// GetDSN returns the PostgreSQL connection string
func (c *DatabaseConfig) GetDSN() string {
	// Check if DATABASE_URL is set (cloud deployment)
	if databaseURL := os.Getenv("DATABASE_URL"); databaseURL != "" {
		return databaseURL
	}

	// Build from individual components (local development)
	return fmt.Sprintf(
		"host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
		c.Host, c.Port, c.User, c.Password, c.DBName, c.SSLMode,
	)
}
