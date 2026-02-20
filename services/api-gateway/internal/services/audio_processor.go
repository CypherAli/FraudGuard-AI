package services

import (
	"context"
	"fmt"
	"log"
	"sync"
	"time"

	"github.com/fraudguard/api-gateway/internal/audio"
	"github.com/fraudguard/api-gateway/internal/models"
)

// Global detector registry to track fraud detectors per device
var (
	detectorRegistry         = make(map[string]*FraudDetector)
	detectorMutex            sync.RWMutex
	deepfakeRegistry         = make(map[string]*audio.DeepfakeDetector)
	deepfakeMutex            sync.RWMutex
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

	deepfakeMutex.Lock()
	delete(deepfakeRegistry, deviceID)
	deepfakeMutex.Unlock()

	removeAudioBuffer(deviceID)
	log.Printf("🗑️ [%s] Removed fraud detector and audio buffer from registry", deviceID)
}

// GetDeepfakeDetector retrieves or creates a deepfake detector for a device
func GetDeepfakeDetector(deviceID string) *audio.DeepfakeDetector {
	deepfakeMutex.RLock()
	dd, exists := deepfakeRegistry[deviceID]
	deepfakeMutex.RUnlock()
	if exists {
		return dd
	}

	deepfakeMutex.Lock()
	defer deepfakeMutex.Unlock()
	if dd, exists := deepfakeRegistry[deviceID]; exists {
		return dd
	}
	dd = audio.NewDeepfakeDetector()
	deepfakeRegistry[deviceID] = dd
	return dd
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

	// Fast-path: nếu cả 2 STT đều OPEN thì skip ngay (dùng IsOpen để không consume HalfOpen slot)
	if DeepgramCircuitBreaker.IsOpen() && (GlobalTranscribeClient == nil || !GlobalTranscribeClient.IsEnabled() || TranscribeCircuitBreaker.IsOpen()) {
		log.Printf("[%s] Both STT circuit breakers OPEN - skipping request", deviceID)
		return
	}

	// Acquire global semaphore slot to limit total concurrent goroutines
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

		// Timeout must cover worst-case pipeline:
		//   STT (Deepgram/Transcribe): ~10s
		//   Gemini agent (4 iters × 8s): ~32s
		//   DB calls + overhead: ~5s
		// Total worst-case: ~47s → use 50s to be safe
		ctx, cancel := context.WithTimeout(context.Background(), 50*time.Second)
		defer cancel()

		log.Printf("🔄 [%s] Starting async transcription...", deviceID)

		// Run deepfake analysis in parallel with transcription
		// Use flushData (the safely copied buffer) instead of audioData (the raw parameter)
		// to avoid potential aliasing issues if the caller reuses the underlying array.
		var deepfakeScore int
		var deepfakeDone chan struct{}
		if audio.IsEnabled() {
			deepfakeDone = make(chan struct{})
			go func() {
				defer close(deepfakeDone)
				dd := GetDeepfakeDetector(deviceID)
				analysis := dd.AnalyzeChunk(flushData)
				deepfakeScore = dd.GetRollingScore() // Use rolling average for stability
				if analysis.IsLikelyFake {
					log.Printf("🎭 [%s] Deepfake detected! Score=%d (chunk=%d)",
						deviceID, deepfakeScore, analysis.Score)
				}
			}()
		}

		// Step 1: Transcribe audio — Deepgram primary, Amazon Transcribe fallback
		var transcript string
		var transcribeErr error

		if DeepgramCircuitBreaker.Allow() {
			transcript, transcribeErr = GlobalDeepgramClient.TranscribeAudio(flushData)
			if transcribeErr != nil || ctx.Err() != nil {
				log.Printf("❌ [%s] Deepgram error: %v — trying Amazon Transcribe fallback", deviceID, transcribeErr)
				DeepgramCircuitBreaker.RecordFailure()
				transcribeErr = nil // reset để thử fallback

				if GlobalTranscribeClient != nil && GlobalTranscribeClient.IsEnabled() && TranscribeCircuitBreaker.Allow() {
					log.Printf("🔄 [%s] Switching to Amazon Transcribe...", deviceID)
					transcript, transcribeErr = GlobalTranscribeClient.TranscribeAudio(flushData)
					if transcribeErr != nil {
						log.Printf("❌ [%s] Amazon Transcribe also failed: %v", deviceID, transcribeErr)
						TranscribeCircuitBreaker.RecordFailure()
						return
					}
					TranscribeCircuitBreaker.RecordSuccess()
					log.Printf("✅ [%s] Amazon Transcribe fallback succeeded", deviceID)
				} else {
					return
				}
			} else {
				DeepgramCircuitBreaker.RecordSuccess()
			}
		} else {
			// Deepgram OPEN — nhảy thẳng sang Amazon Transcribe
			if GlobalTranscribeClient != nil && GlobalTranscribeClient.IsEnabled() && TranscribeCircuitBreaker.Allow() {
				log.Printf("🔄 [%s] Deepgram OPEN, using Amazon Transcribe directly", deviceID)
				transcript, transcribeErr = GlobalTranscribeClient.TranscribeAudio(flushData)
				if transcribeErr != nil {
					log.Printf("❌ [%s] Amazon Transcribe failed: %v", deviceID, transcribeErr)
					TranscribeCircuitBreaker.RecordFailure()
					return
				}
				TranscribeCircuitBreaker.RecordSuccess()
			} else {
				return
			}
		}

		// Wait for deepfake analysis to complete
		if deepfakeDone != nil {
			<-deepfakeDone
		}

		if transcript == "" {
			return
		}

		log.Printf("[%s] Transcript: '%s'", deviceID, transcript)

		detector := GetOrCreateFraudDetector(deviceID, sendAlert)
		result := detector.AnalyzeText(transcript)

		// Boost risk score if deepfake detected
		// Bug #4 fix: also update the session's AccumulatedScore so it persists across calls
		if deepfakeScore > 70 {
			const deepfakeBoost = 15
			result.RiskScore += deepfakeBoost
			if result.RiskScore > 100 {
				result.RiskScore = 100
			}
			result.Patterns = append(result.Patterns, fmt.Sprintf("DEEPFAKE: score=%d", deepfakeScore))
			// Persist boost to session so future AnalyzeText calls reflect deepfake impact
			detector.AddDeepfakeBoost(deepfakeBoost)
			if !result.IsAlert && result.RiskScore >= 40 {
				result.IsAlert = true
				result.Action = "MEDIUM"
				result.Message = fmt.Sprintf("CANH BAO: Phat hien giong noi co the la deepfake! (Diem rui ro: %d/100)", result.RiskScore)
			}
		}

		log.Printf("📊 [%s] Analysis complete - IsAlert: %v, Action: %s, RiskScore: %d, DeepfakeScore: %d",
			deviceID, result.IsAlert, result.Action, result.RiskScore, deepfakeScore)

		// Bug #3 fix: send LOW-level informational alert to mobile so it can display subtle UI cue
		// result.IsAlert is false for LOW (no spam guard check needed), so we send unconditionally
		if result.IsAlert {
			alert := models.AlertMessage{
				Type:          "alert",
				AlertType:     result.Action,
				Confidence:    float64(result.RiskScore) / 100.0,
				Transcript:    transcript,
				Keywords:      result.Patterns,
				Timestamp:     time.Now().Unix(),
				Message:       result.Message,
				DeepfakeScore: deepfakeScore,
			}
			log.Printf("[%s] FRAUD ALERT: %s (Risk: %d%%)", deviceID, result.Action, result.RiskScore)
			sendAlert(alert)
		} else if result.Action == "LOW" {
			// LOW risk: send as informational alert (IsAlert=false in result, but mobile still notified)
			alert := models.AlertMessage{
				Type:          "alert",
				AlertType:     "LOW",
				Confidence:    float64(result.RiskScore) / 100.0,
				Transcript:    transcript,
				Keywords:      result.Patterns,
				Timestamp:     time.Now().Unix(),
				Message:       result.Message,
				DeepfakeScore: deepfakeScore,
			}
			log.Printf("[%s] LOW RISK INFO: sending informational alert (Risk: %d%%)", deviceID, result.RiskScore)
			sendAlert(alert)
		}
	}()
}
