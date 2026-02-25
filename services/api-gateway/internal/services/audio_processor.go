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
	detectorLastSeen         = make(map[string]time.Time)
	deepfakeRegistry         = make(map[string]*audio.DeepfakeDetector)
	deepfakeMutex            sync.RWMutex
	audioProcessingSemaphore = make(chan struct{}, 50) // Max 50 concurrent goroutines
)

const detectorTTL = 30 * time.Minute

// Channel byte prefix — matches VoipPlaybackCaptureService constants on the mobile side.
// Mobile prepends exactly 1 byte before each PCM chunk over the WebSocket.
const (
	ChannelMic  = byte(0x00) // Uplink:   user's microphone  → label [USER]
	ChannelVoIP = byte(0x01) // Downlink: VoIP playback      → label [SCAMMER]
)

func init() {
	go func() {
		ticker := time.NewTicker(10 * time.Minute)
		defer ticker.Stop()
		for range ticker.C {
			evictStaleDetectors()
		}
	}()
}

// evictStaleDetectors removes detectors that haven't been used in detectorTTL.
// Prevents unbounded memory growth when many unique deviceIDs connect over time.
func evictStaleDetectors() {
	cutoff := time.Now().Add(-detectorTTL)

	// Collect stale device IDs under the detector lock
	detectorMutex.Lock()
	var stale []string
	for deviceID, lastSeen := range detectorLastSeen {
		if lastSeen.Before(cutoff) {
			stale = append(stale, deviceID)
			delete(detectorRegistry, deviceID)
			delete(detectorLastSeen, deviceID)
		}
	}
	detectorMutex.Unlock()

	if len(stale) == 0 {
		return
	}

	// Clean up associated registries for evicted devices
	deepfakeMutex.Lock()
	for _, deviceID := range stale {
		delete(deepfakeRegistry, deviceID)
	}
	deepfakeMutex.Unlock()

	audioBufferMutex.Lock()
	for _, deviceID := range stale {
		delete(audioBufferRegistry, deviceID+"|mic")
		delete(audioBufferRegistry, deviceID+"|voip")
		delete(audioBufferRegistry, deviceID) // legacy key
	}
	audioBufferMutex.Unlock()

	log.Printf("🗑️ Evicted %d stale detector(s) (TTL=%v): %v", len(stale), detectorTTL, stale)
}

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

// getAudioBuffer retrieves or creates an AudioBuffer for a given key.
// Use compound keys like "deviceID|mic" or "deviceID|voip" for dual-stream support.
func getAudioBuffer(key string) *AudioBuffer {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()

	if buf, exists := audioBufferRegistry[key]; exists {
		return buf
	}

	buf := &AudioBuffer{
		data:          make([]byte, 0, 64*1024),
		lastFlush:     time.Now(),
		flushInterval: 2 * time.Second,
		minSize:       16000 * 2 * 1, // ~1 second of 16kHz mono 16-bit audio (32KB)
	}
	audioBufferRegistry[key] = buf
	return buf
}

func removeAudioBuffer(deviceID string) {
	audioBufferMutex.Lock()
	defer audioBufferMutex.Unlock()
	delete(audioBufferRegistry, deviceID+"|mic")
	delete(audioBufferRegistry, deviceID+"|voip")
	delete(audioBufferRegistry, deviceID) // legacy key (pre-dual-stream clients)
}

// GetOrCreateFraudDetector retrieves or creates a fraud detector for a device.
// Sets the alert callback so Gemini AI async results can send alerts.
func GetOrCreateFraudDetector(deviceID string, sendAlert func(models.AlertMessage)) *FraudDetector {
	detectorMutex.Lock()
	defer detectorMutex.Unlock()

	if detector, exists := detectorRegistry[deviceID]; exists {
		detectorLastSeen[deviceID] = time.Now()
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
	detectorLastSeen[deviceID] = time.Now()
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
	delete(detectorLastSeen, deviceID)
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
//
// Protocol: each chunk begins with a 1-byte channel prefix:
//
//	0x00 (ChannelMic)  → user's microphone uplink   → labeled [USER]
//	0x01 (ChannelVoIP) → VoIP playback downlink      → labeled [SCAMMER]
//
// Legacy clients (no prefix) are treated as Mic for backward compatibility.
func ProcessAudioStream(deviceID string, audioData []byte, sendAlert func(models.AlertMessage)) {
	if GlobalDeepgramClient == nil || len(audioData) == 0 {
		return
	}

	// Decode channel prefix. If the first byte is a known channel byte, strip it.
	// Otherwise fall back to treating the whole payload as Mic audio (legacy).
	channel := ChannelMic
	speakerLabel := "[USER]"
	pcmData := audioData

	if audioData[0] == ChannelMic || audioData[0] == ChannelVoIP {
		channel = audioData[0]
		pcmData = audioData[1:]
		if channel == ChannelVoIP {
			speakerLabel = "[SCAMMER]"
		}
	}

	if len(pcmData) == 0 {
		return
	}

	// Per-channel buffer key: "deviceID|0" (mic) or "deviceID|1" (voip)
	bufKey := fmt.Sprintf("%s|%d", deviceID, channel)
	buf := getAudioBuffer(bufKey)

	audioBufferMutex.Lock()
	buf.data = append(buf.data, pcmData...)
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

		// Run deepfake analysis in parallel with transcription.
		// Only meaningful for the VoIP (downlink) stream — we want to detect
		// AI-synthesized scammer voices, not the victim's microphone.
		// Use a buffered channel (cap=1) to pass the score back safely.
		deepfakeChan := make(chan int, 1)
		if audio.IsEnabled() && channel == ChannelVoIP {
			go func() {
				dd := GetDeepfakeDetector(deviceID)
				analysis := dd.AnalyzeChunk(flushData)
				score := dd.GetRollingScore() // Use rolling average for stability
				if analysis.IsLikelyFake {
					log.Printf("🎭 [%s] Deepfake detected! Score=%d (chunk=%d)",
						deviceID, score, analysis.Score)
				}
				deepfakeChan <- score
			}()
		} else {
			deepfakeChan <- 0 // no analysis — send sentinel so receive below never blocks
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

		// Receive deepfake score (blocks until goroutine sends, or immediately if disabled)
		deepfakeScore := <-deepfakeChan

		if transcript == "" {
			return
		}

		// Prefix transcript with speaker label so FraudDetector and Gemini Agent
		// receive a labeled 2-sided conversation: "[USER] ..." vs "[SCAMMER] ..."
		labeledTranscript := speakerLabel + " " + transcript
		log.Printf("[%s] %s Transcript: '%s'", deviceID, speakerLabel, transcript)

		detector := GetOrCreateFraudDetector(deviceID, sendAlert)
		result := detector.AnalyzeText(labeledTranscript)

		// Always persist the deepfake score to the session so EndSession can save it to DB
		detector.UpdateDeepfakeScore(deepfakeScore)

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
				Transcript:    labeledTranscript,
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
				Transcript:    labeledTranscript,
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
