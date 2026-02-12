package services

import "time"

var (
	// Global AI clients - initialized in main.go
	GlobalDeepgramClient *DeepgramClient
	GlobalGeminiClient   *GeminiClient

	// Circuit breaker for Gemini API
	GeminiCircuitBreaker = NewCircuitBreaker("Gemini", 3, 60*time.Second)
)
