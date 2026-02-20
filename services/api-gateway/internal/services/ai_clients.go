package services

import "time"

var (
	// Global AI clients - initialized in main.go
	GlobalDeepgramClient   *DeepgramClient
	GlobalGeminiClient     *GeminiClient
	GlobalTranscribeClient *TranscribeClient // AWS Amazon Transcribe (fallback STT)

	// Circuit breakers for external AI APIs
	GeminiCircuitBreaker     = NewCircuitBreaker("Gemini", 3, 60*time.Second)
	DeepgramCircuitBreaker   = NewCircuitBreaker("Deepgram", 5, 30*time.Second)
	TranscribeCircuitBreaker = NewCircuitBreaker("AmazonTranscribe", 3, 60*time.Second)
)
