package services

import (
	"fmt"
	"log"
	"sync"
	"time"

	"github.com/fraudguard/api-gateway/internal/models"
)

// AudioProcessor handles real-time audio streaming and transcription
type AudioProcessor struct {
	deviceID      string
	fraudDetector *FraudDetector
	sendAlert     func(models.AlertMessage)
	mu            sync.Mutex
}

// NewAudioProcessor creates a new audio processor for a client session
func NewAudioProcessor(deviceID string, sendAlert func(models.AlertMessage)) *AudioProcessor {
	return &AudioProcessor{
		deviceID:      deviceID,
		fraudDetector: NewFraudDetector(deviceID),
		sendAlert:     sendAlert,
	}
}

// ProcessAudioStream handles real-time audio streaming to Deepgram
// This is the main entry point called from WebSocket handler
func ProcessAudioStream(deviceID string, audioData []byte, sendAlert func(models.AlertMessage)) {
	log.Printf("🎤 [%s] Processing audio stream (size: %d bytes)", deviceID, len(audioData))

	// Check if Deepgram client is initialized
	if GlobalDeepgramClient == nil {
		log.Printf("⚠️ [%s] Deepgram client not initialized, skipping transcription", deviceID)
		return
	}

	// Process asynchronously to not block WebSocket
	go func() {
		// Step 1: Transcribe audio using Deepgram
		transcript, err := GlobalDeepgramClient.TranscribeAudio(audioData)
		if err != nil {
			log.Printf("❌ [%s] Deepgram transcription error: %v", deviceID, err)
			return
		}

		if transcript == "" {
			log.Printf("ℹ️ [%s] Empty transcript, skipping fraud detection", deviceID)
			return
		}

		log.Printf("📝 [%s] Transcript: %s", deviceID, transcript)

		// Step 2: Analyze transcript for fraud
		detector := NewFraudDetector(deviceID)
		result := detector.AnalyzeText(transcript)

		// Step 3: Send alert if fraud detected
		if result.IsAlert {
			alert := models.AlertMessage{
				RiskScore: result.RiskScore,
				Message:   result.Message,
				Action:    result.Action,
				Timestamp: time.Now().Unix(),
			}
			sendAlert(alert)
			log.Printf("🚨 [%s] FRAUD ALERT: %s (Risk: %d%%)",
				deviceID, result.Message, result.RiskScore)
		} else {
			log.Printf("✅ [%s] No fraud detected (Risk: %d%%)",
				deviceID, result.RiskScore)
		}
	}()
}

// ==================== Advanced Audio Processor (For Future Use) ====================

// StreamingAudioProcessor handles continuous audio streaming with session management
// This is for future implementation when you want to maintain persistent connections
type StreamingAudioProcessor struct {
	deviceID        string
	fraudDetector   *FraudDetector
	sendAlert       func(models.AlertMessage)
	mu              sync.Mutex
	isActive        bool
	audioBuffer     []byte
	bufferSize      int
	maxBufferSize   int
	lastProcessed   time.Time
	processInterval time.Duration
}

// NewStreamingAudioProcessor creates a new streaming processor
func NewStreamingAudioProcessor(deviceID string, sendAlert func(models.AlertMessage)) *StreamingAudioProcessor {
	return &StreamingAudioProcessor{
		deviceID:        deviceID,
		fraudDetector:   NewFraudDetector(deviceID),
		sendAlert:       sendAlert,
		audioBuffer:     make([]byte, 0),
		maxBufferSize:   1024 * 1024, // 1MB buffer
		processInterval: 2 * time.Second,
		lastProcessed:   time.Now(),
	}
}

// AddAudioChunk adds audio chunk to buffer and processes when ready
func (sap *StreamingAudioProcessor) AddAudioChunk(chunk []byte) error {
	sap.mu.Lock()
	defer sap.mu.Unlock()

	// Add to buffer
	sap.audioBuffer = append(sap.audioBuffer, chunk...)
	sap.bufferSize += len(chunk)

	log.Printf("🎤 [%s] Audio chunk added: %d bytes (buffer: %d bytes)",
		sap.deviceID, len(chunk), sap.bufferSize)

	// Check if we should process
	shouldProcess := false

	// Process if buffer is large enough
	if sap.bufferSize >= 32*1024 { // 32KB threshold
		shouldProcess = true
	}

	// Or if enough time has passed
	if time.Since(sap.lastProcessed) >= sap.processInterval {
		shouldProcess = true
	}

	if shouldProcess && sap.bufferSize > 0 {
		// Process current buffer
		bufferCopy := make([]byte, len(sap.audioBuffer))
		copy(bufferCopy, sap.audioBuffer)

		// Clear buffer
		sap.audioBuffer = make([]byte, 0)
		sap.bufferSize = 0
		sap.lastProcessed = time.Now()

		// Process asynchronously
		go sap.processBuffer(bufferCopy)
	}

	return nil
}

// processBuffer processes accumulated audio buffer
func (sap *StreamingAudioProcessor) processBuffer(buffer []byte) {
	if GlobalDeepgramClient == nil {
		log.Printf("⚠️ [%s] Deepgram client not initialized", sap.deviceID)
		return
	}

	log.Printf("🔄 [%s] Processing audio buffer: %d bytes", sap.deviceID, len(buffer))

	// Transcribe
	transcript, err := GlobalDeepgramClient.TranscribeAudio(buffer)
	if err != nil {
		log.Printf("❌ [%s] Transcription error: %v", sap.deviceID, err)
		return
	}

	if transcript == "" {
		return
	}

	log.Printf("📝 [%s] Transcript: %s", sap.deviceID, transcript)

	// Analyze for fraud
	result := sap.fraudDetector.AnalyzeText(transcript)

	if result.IsAlert {
		alert := models.AlertMessage{
			RiskScore: result.RiskScore,
			Message:   result.Message,
			Action:    result.Action,
			Timestamp: time.Now().Unix(),
		}
		sap.sendAlert(alert)
		log.Printf("🚨 [%s] FRAUD ALERT: %s (Risk: %d%%)",
			sap.deviceID, result.Message, result.RiskScore)
	}
}

// Start activates the streaming processor
func (sap *StreamingAudioProcessor) Start() error {
	sap.mu.Lock()
	defer sap.mu.Unlock()

	if sap.isActive {
		return fmt.Errorf("processor already active")
	}

	sap.isActive = true
	log.Printf("▶️ [%s] Streaming audio processor started", sap.deviceID)
	return nil
}

// Stop deactivates the streaming processor
func (sap *StreamingAudioProcessor) Stop() {
	sap.mu.Lock()
	defer sap.mu.Unlock()

	if !sap.isActive {
		return
	}

	// Process any remaining buffer
	if sap.bufferSize > 0 {
		bufferCopy := make([]byte, len(sap.audioBuffer))
		copy(bufferCopy, sap.audioBuffer)
		go sap.processBuffer(bufferCopy)
	}

	sap.isActive = false
	log.Printf("⏹️ [%s] Streaming audio processor stopped", sap.deviceID)
}

// GetStats returns current processor statistics
func (sap *StreamingAudioProcessor) GetStats() map[string]interface{} {
	sap.mu.Lock()
	defer sap.mu.Unlock()

	return map[string]interface{}{
		"device_id":      sap.deviceID,
		"is_active":      sap.isActive,
		"buffer_size":    sap.bufferSize,
		"risk_score":     sap.fraudDetector.GetCurrentRiskScore(),
		"alert_count":    sap.fraudDetector.GetAlertCount(),
		"last_processed": sap.lastProcessed,
	}
}
