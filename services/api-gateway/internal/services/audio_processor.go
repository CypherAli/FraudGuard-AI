package services

import (
	"fmt"
	"log"
	"sync"
	"time"

	"github.com/fraudguard/api-gateway/internal/models"
)

// Global detector registry to track fraud detectors per device
var (
	detectorRegistry = make(map[string]*FraudDetector)
	detectorMutex    sync.RWMutex
)

// Audio buffer registry - accumulates audio chunks before sending to Deepgram
// This prevents sending tiny 8KB chunks that produce meaningless transcripts
var (
	audioBufferRegistry = make(map[string]*AudioBuffer)
	audioBufferMutex    sync.Mutex
)

// AudioBuffer accumulates audio chunks before transcription
type AudioBuffer struct {
	data          []byte
	lastFlush     time.Time
	flushInterval time.Duration // How often to send accumulated audio to Deepgram
	minSize       int           // Minimum bytes before sending
}

// getAudioBuffer retrieves or creates an audio buffer for a device
func getAudioBuffer(deviceID string) *AudioBuffer {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()

	if buf, exists := audioBufferRegistry[deviceID]; exists {
		return buf
	}

	buf := &AudioBuffer{
		data:          make([]byte, 0, 64*1024), // Pre-allocate 64KB
		lastFlush:     time.Now(),
		flushInterval: 2 * time.Second, // Send every 2 seconds
		minSize:       16000 * 2 * 1,   // ~1 second of 16kHz mono 16-bit audio (32KB)
	}
	audioBufferRegistry[deviceID] = buf
	return buf
}

// removeAudioBuffer cleans up audio buffer when device disconnects
func removeAudioBuffer(deviceID string) {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()
	delete(audioBufferRegistry, deviceID)
}

// Note: Buffer pooling for audio chunks should be implemented in the WebSocket handler
// (hub/client.go) where raw audio data is initially received, not here where we only
// process the data. This would reduce GC pressure from frequent 8KB allocations.

// GetOrCreateFraudDetector retrieves or creates a fraud detector for a device.
// If creating new, sets the alert callback so Gemini AI results can send alerts.
func GetOrCreateFraudDetector(deviceID string, sendAlert func(models.AlertMessage)) *FraudDetector {
	detectorMutex.Lock()
	defer detectorMutex.Unlock()

	if detector, exists := detectorRegistry[deviceID]; exists {
		// Update alert callback if provided (in case of reconnect)
		if sendAlert != nil {
			detector.SetAlertCallback(sendAlert)
		}
		return detector
	}

	detector := NewFraudDetector(deviceID)
	if sendAlert != nil {
		detector.SetAlertCallback(sendAlert)
	}
	detectorRegistry[deviceID] = detector
	log.Printf("🆕 [%s] Created new fraud detector (with alert callback: %v)", deviceID, sendAlert != nil)
	return detector
}

// GetFraudDetector retrieves or creates a fraud detector for a device (backward compat).
func GetFraudDetector(deviceID string) *FraudDetector {
	return GetOrCreateFraudDetector(deviceID, nil)
}

// RemoveFraudDetector removes a detector and audio buffer from registry (called after session ends)
func RemoveFraudDetector(deviceID string) {
	detectorMutex.Lock()
	delete(detectorRegistry, deviceID)
	detectorMutex.Unlock()

	removeAudioBuffer(deviceID)
	log.Printf("🗑️ [%s] Removed fraud detector and audio buffer from registry", deviceID)
}

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

// ProcessAudioStream handles real-time audio streaming with buffering
// Audio chunks are accumulated before sending to Deepgram for better transcription quality
// This is the main entry point called from WebSocket handler
func ProcessAudioStream(deviceID string, audioData []byte, sendAlert func(models.AlertMessage)) {
	// Check if Deepgram client is initialized
	if GlobalDeepgramClient == nil {
		log.Printf("⚠️ [%s] Deepgram client not initialized, skipping transcription", deviceID)
		return
	}

	// Accumulate audio in buffer
	buf := getAudioBuffer(deviceID)

	audioBufferMutex.Lock()
	buf.data = append(buf.data, audioData...)
	bufferSize := len(buf.data)
	timeSinceFlush := time.Since(buf.lastFlush)
	shouldFlush := bufferSize >= buf.minSize || timeSinceFlush >= buf.flushInterval

	var flushData []byte
	if shouldFlush && bufferSize > 0 {
		flushData = make([]byte, bufferSize)
		copy(flushData, buf.data)
		buf.data = buf.data[:0] // Reset buffer, keep capacity
		buf.lastFlush = time.Now()
	}
	audioBufferMutex.Unlock()

	if flushData == nil {
		return // Not ready to flush yet
	}

	log.Printf("🎤 [%s] Flushing audio buffer: %d bytes (%.1fs accumulated)", deviceID, len(flushData), timeSinceFlush.Seconds())

	// Check circuit breaker before making request
	if !DeepgramCircuitBreaker.Allow() {
		log.Printf("🔴 [%s] Circuit breaker OPEN - skipping Deepgram request", deviceID)
		return
	}

	// Process asynchronously to not block WebSocket
	go func() {
		defer func() {
			if r := recover(); r != nil {
				log.Printf("🔥 [%s] Recovered panic in audio processing: %v", deviceID, r)
			}
		}()

		// Step 1: Transcribe audio using Deepgram
		transcript, err := GlobalDeepgramClient.TranscribeAudio(flushData)
		if err != nil {
			log.Printf("❌ [%s] Deepgram transcription error: %v", deviceID, err)
			DeepgramCircuitBreaker.RecordFailure()
			return
		}

		DeepgramCircuitBreaker.RecordSuccess()

		if transcript == "" {
			return
		}

		log.Printf("📝 [%s] Transcript: '%s'", deviceID, transcript)

		// Step 2: Get or create fraud detector WITH sendAlert callback
		// This ensures Gemini AI async results can also trigger mobile alerts
		detector := GetOrCreateFraudDetector(deviceID, sendAlert)

		result := detector.AnalyzeText(transcript)

		log.Printf("📊 [%s] Analysis: IsAlert=%v, Action=%s, Score=%d",
			deviceID, result.IsAlert, result.Action, result.RiskScore)

		// Step 3: Send alert if fraud detected by keyword analysis
		if result.IsAlert {
			alert := models.AlertMessage{
				Type:       "alert",
				AlertType:  result.Action,
				Confidence: float64(result.RiskScore) / 100.0,
				Transcript: transcript,
				Keywords:   result.Patterns,
				Timestamp:  time.Now().Unix(),
				Message:    result.Message,
			}

			log.Printf("🚨 [%s] FRAUD ALERT: %s (Risk: %d%%)", deviceID, result.Message, result.RiskScore)
			sendAlert(alert)
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

	// Stale data handling: Drop old audio to avoid processing delays
	// In real-time fraud detection, old data is worthless - better to process fresh audio
	const maxAudioAge = 5 * time.Second
	if sap.bufferSize > 0 && time.Since(sap.lastProcessed) > maxAudioAge {
		log.Printf("⚠️ [%s] Dropping stale audio buffer (%v old, %d bytes) to maintain real-time processing",
			sap.deviceID, time.Since(sap.lastProcessed), sap.bufferSize)
		sap.audioBuffer = make([]byte, 0)
		sap.bufferSize = 0
	}

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
			Type:       "alert",
			AlertType:  result.Action,
			Confidence: float64(result.RiskScore) / 100.0,
			Transcript: transcript,
			Keywords:   result.Patterns,
			Timestamp:  time.Now().Unix(),
			Message:    result.Message,
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
