package config

import (
	"fmt"
	"os"
	"strconv"
	"time"

	"github.com/joho/godotenv"
)

// Config holds all application configuration
type Config struct {
	Database  DatabaseConfig
	Server    ServerConfig
	WebSocket WebSocketConfig
	AI        AIConfig
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
	VectorDBURL    string
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
			Password: getEnv("DB_PASSWORD", "fraudguard_secure_2024"),
			DBName:   getEnv("DB_NAME", "fraudguard_db"),
			SSLMode:  getEnv("DB_SSLMODE", "disable"),
			MaxConns: getEnvAsInt("DB_MAX_CONNS", 25),
			MinConns: getEnvAsInt("DB_MIN_CONNS", 5),
		}
	}

	cfg := &Config{
		Database: dbConfig,
		Server: ServerConfig{
			Host:         getEnv("SERVER_HOST", "0.0.0.0"),
			Port:         getEnvAsInt("SERVER_PORT", 8080),
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
			DeepgramAPIKey: getEnv("DEEPGRAM_API_KEY", ""),
			GeminiAPIKey:   getEnv("GEMINI_API_KEY", ""),
			VectorDBURL:    getEnv("VECTOR_DB_URL", ""),
		},
	}

	// Validate required fields
	databaseURL = os.Getenv("DATABASE_URL")
	if databaseURL == "" && cfg.Database.Password == "" {
		return nil, fmt.Errorf("DATABASE_URL or DB_PASSWORD is required")
	}

	return cfg, nil
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
