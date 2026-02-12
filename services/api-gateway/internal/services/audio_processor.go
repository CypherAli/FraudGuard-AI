package services

import (
	"log"
	"sync"
	"time"

	"github.com/fraudguard/api-gateway/internal/models"
)

// Global detector registry to track fraud detectors per device
var (
	detectorRegistry         = make(map[string]*FraudDetector)
	detectorMutex            sync.RWMutex
	audioProcessingSemaphore = make(chan struct{}, 50) // Max 50 concurrent goroutines
)

// Audio buffer registry - accumulates audio chunks before sending to Deepgram
var (
	audioBufferRegistry = make(map[string]*AudioBuffer)
	audioBufferMutex    sync.Mutex
)

// AudioBuffer accumulates audio chunks before transcription
type AudioBuffer struct {
	data          []byte
	lastFlush     time.Time
	flushInterval time.Duration
	minSize       int
}

func getAudioBuffer(deviceID string) *AudioBuffer {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()

	if buf, exists := audioBufferRegistry[deviceID]; exists {
		return buf
	}

	buf := &AudioBuffer{
		data:          make([]byte, 0, 64*1024),
		lastFlush:     time.Now(),
		flushInterval: 2 * time.Second,
		minSize:       16000 * 2 * 1, // ~1 second of 16kHz mono 16-bit audio (32KB)
	}
	audioBufferRegistry[deviceID] = buf
	return buf
}

func removeAudioBuffer(deviceID string) {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()
	delete(audioBufferRegistry, deviceID)
}

// GetOrCreateFraudDetector retrieves or creates a fraud detector for a device.
// Sets the alert callback so Gemini AI async results can send alerts.
func GetOrCreateFraudDetector(deviceID string, sendAlert func(models.AlertMessage)) *FraudDetector {
	detectorMutex.Lock()
	defer detectorMutex.Unlock()

	if detector, exists := detectorRegistry[deviceID]; exists {
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
	log.Printf("[%s] Created new fraud detector", deviceID)
	return detector
}

// GetFraudDetector retrieves or creates a fraud detector for a device.
func GetFraudDetector(deviceID string) *FraudDetector {
	return GetOrCreateFraudDetector(deviceID, nil)
}

// RemoveFraudDetector removes a detector and audio buffer from registry
func RemoveFraudDetector(deviceID string) {
	detectorMutex.Lock()
	delete(detectorRegistry, deviceID)
	detectorMutex.Unlock()

	removeAudioBuffer(deviceID)
	log.Printf("[%s] Removed fraud detector and audio buffer", deviceID)
}

// ProcessAudioStream handles real-time audio streaming with buffering.
// Audio chunks are accumulated before sending to Deepgram for better transcription quality.
func ProcessAudioStream(deviceID string, audioData []byte, sendAlert func(models.AlertMessage)) {
	if GlobalDeepgramClient == nil {
		return
	}

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
		buf.data = buf.data[:0]
		buf.lastFlush = time.Now()
	}
	audioBufferMutex.Unlock()

	if flushData == nil {
		return
	}

	log.Printf("[%s] Flushing audio buffer: %d bytes (%.1fs accumulated)", deviceID, len(flushData), timeSinceFlush.Seconds())

	if !DeepgramCircuitBreaker.Allow() {
		log.Printf("[%s] Deepgram circuit breaker OPEN - skipping request", deviceID)
		return
	}

	// Acquire semaphore slot
	select {
	case audioProcessingSemaphore <- struct{}{}:
	default:
		log.Printf("[%s] Audio processing pool full, dropping chunk", deviceID)
		return
	}

	go func() {
		defer func() {
			<-audioProcessingSemaphore
			if r := recover(); r != nil {
				log.Printf("[%s] Recovered panic in audio processing: %v", deviceID, r)
			}
		}()

		transcript, err := GlobalDeepgramClient.TranscribeAudio(flushData)
		if err != nil {
			log.Printf("[%s] Deepgram transcription error: %v", deviceID, err)
			DeepgramCircuitBreaker.RecordFailure()
			return
		}
		DeepgramCircuitBreaker.RecordSuccess()

		if transcript == "" {
			return
		}

		log.Printf("[%s] Transcript: '%s'", deviceID, transcript)

		detector := GetOrCreateFraudDetector(deviceID, sendAlert)
		result := detector.AnalyzeText(transcript)

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
			log.Printf("[%s] FRAUD ALERT: %s (Risk: %d%%)", deviceID, result.Action, result.RiskScore)
			sendAlert(alert)
		}
	}()
}
