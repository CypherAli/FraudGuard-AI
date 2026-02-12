package services

import "time"

var (
	// Global AI clients - initialized in main.go
	GlobalDeepgramClient *DeepgramClient
	GlobalGeminiClient   *GeminiClient

	// Circuit breakers for external AI APIs
	GeminiCircuitBreaker   = NewCircuitBreaker("Gemini", 3, 60*time.Second)
	DeepgramCircuitBreaker = NewCircuitBreaker("Deepgram", 5, 30*time.Second)
)
