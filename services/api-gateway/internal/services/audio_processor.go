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

	// Per-channel high-pass filter registry.
	// Key: "deviceID|mic" or "deviceID|voip"  (same as audioBufferRegistry).
	// The filter is applied to incoming PCM chunks inside audioBufferMutex to
	// guarantee sequential, race-free application of the stateful IIR filter.
	hpfRegistry = make(map[string]*audio.HighPassFilter)

	// Per-channel Voice Activity Detection registry.
	// Key: "deviceID|mic" or "deviceID|voip" — same convention as hpfRegistry.
	// VAD is invoked synchronously in ProcessAudioStream (before spawning the STT
	// goroutine), which is itself called sequentially from the WebSocket read-loop
	// goroutine — one goroutine per connected device.  Because there is therefore
	// at most one concurrent caller per VADProcessor, no per-processor mutex is
	// needed; vadMutex only protects map reads/writes.
	vadRegistry = make(map[string]*audio.VADProcessor)
	vadMutex    sync.Mutex
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
		delete(hpfRegistry, deviceID+"|mic")
		delete(hpfRegistry, deviceID+"|voip")
	}
	audioBufferMutex.Unlock()

	// Clean up VAD processors for evicted devices
	vadMutex.Lock()
	for _, deviceID := range stale {
		delete(vadRegistry, deviceID+"|mic")
		delete(vadRegistry, deviceID+"|voip")
	}
	vadMutex.Unlock()

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
	delete(audioBufferRegistry, deviceID+"|mic")
	delete(audioBufferRegistry, deviceID+"|voip")
	delete(audioBufferRegistry, deviceID) // legacy key (pre-dual-stream clients)
	// Clean up HPF state for this device (same mutex, same keys)
	delete(hpfRegistry, deviceID+"|mic")
	delete(hpfRegistry, deviceID+"|voip")
	audioBufferMutex.Unlock()

	// Clean up VAD state — separate mutex, must NOT be held together with audioBufferMutex
	vadMutex.Lock()
	delete(vadRegistry, deviceID+"|mic")
	delete(vadRegistry, deviceID+"|voip")
	vadMutex.Unlock()
}

// getVADProcessor retrieves or lazily creates a VADProcessor for the given buffer key.
// aggressiveness=2 is WebRTC "Low bitrate / telephony" mode — ideal for Vietnamese call audio.
// frameDurationMs=20 is the standard WebRTC frame size.
// The processor must only be called from one goroutine at a time (enforced by the
// WebSocket read-loop caller; see vadRegistry comment above).
func getVADProcessor(bufKey string) *audio.VADProcessor {
	vadMutex.Lock()
	defer vadMutex.Unlock()
	if vad, ok := vadRegistry[bufKey]; ok {
		return vad
	}
	vad, err := audio.NewVADProcessor(16000, 2, 20)
	if err != nil {
		log.Printf("⚠️ [VAD] Failed to create processor for %q: %v", bufKey, err)
		return nil
	}
	vadRegistry[bufKey] = vad
	return vad
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

	// Per-channel buffer key: "deviceID|mic" or "deviceID|voip"
	// Must match the keys used in removeAudioBuffer and evictStaleDetectors.
	channelSuffix := "mic"
	if channel == ChannelVoIP {
		channelSuffix = "voip"
	}
	bufKey := deviceID + "|" + channelSuffix
	buf := getAudioBuffer(bufKey)

	audioBufferMutex.Lock()
	// Apply high-pass filter to remove low-frequency noise (wind, handling, HVAC)
	// before buffering.  The HPF is stateful (IIR), so it must run sequentially
	// inside the same mutex that guards the audio buffer — this guarantees that
	// chunks are filtered in the exact order they are buffered, preserving the
	// filter's prevInput/prevOutput state without any additional locking.
	if hpf, ok := hpfRegistry[bufKey]; ok {
		pcmData = hpf.Process(pcmData)
	} else {
		// First chunk for this device+channel — create a 300 Hz HPF.
		// 300 Hz is the standard lower bound of telephone speech bandwidth.
		newHPF := audio.NewHighPassFilter(16000, 300.0)
		hpfRegistry[bufKey] = newHPF
		pcmData = newHPF.Process(pcmData)
	}
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

	// ── VAD: discard silence-only chunks before hitting Deepgram ─────────────
	// ProcessAudioStream is driven by the WebSocket read-loop (one goroutine per
	// device), so getVADProcessor / ProcessBufferWithPadding are always called
	// sequentially for a given bufKey — no extra locking needed on the processor.
	//
	// Edge-case: if speech straddles a flush boundary (v.lastSpeechDetected==true
	// at the start of the next call) the VAD may return speechData with hasSpeech==false
	// (speech start was signalled in the previous call).  We therefore skip STT
	// only when BOTH hasSpeech==false AND speechData is empty — i.e. pure silence.
	if vad := getVADProcessor(bufKey); vad != nil {
		speechData, hasSpeech, _, silenceMs := vad.ProcessBufferWithPadding(flushData, time.Now().UnixMilli())
		if !hasSpeech && len(speechData) == 0 {
			log.Printf("🔇 [%s] VAD: silence (%dms) — STT call skipped", deviceID, silenceMs)
			return
		}
		if len(speechData) > 0 && len(speechData) < len(flushData) {
			log.Printf("🗣️ [%s] VAD: speech %d→%d bytes (saved %.0f%%)",
				deviceID, len(flushData), len(speechData),
				float64(len(flushData)-len(speechData))/float64(len(flushData))*100)
			flushData = speechData
		}
	}

	log.Printf("[%s] Flushing audio buffer: %d bytes (%.1fs accumulated)", deviceID, len(flushData), timeSinceFlush.Seconds())

	// Fast-path: skip only when BOTH STT providers are unavailable.
	// Use IsOpen() (not Allow()) here to avoid consuming a HalfOpen probe slot.
	deepgramDown := DeepgramCircuitBreaker.IsOpen()
	transcribeDown := GlobalTranscribeClient == nil ||
		!GlobalTranscribeClient.IsEnabled() ||
		TranscribeCircuitBreaker.IsOpen()
	if deepgramDown && transcribeDown {
		log.Printf("[%s] Both STT circuit breakers OPEN — skipping chunk", deviceID)
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
				// Fix 37: recover from any panic inside deepfake analysis so the buffered
				// channel always receives a value. Without this, a panic leaves deepfakeChan
				// empty and the caller blocks forever at `<-deepfakeChan`, hanging the
				// entire audio-processing goroutine (context timeout does NOT rescue it
				// because that receive is not select-ed against ctx.Done()).
				defer func() {
					if r := recover(); r != nil {
						log.Printf("⚠️ [%s] Deepfake goroutine panic: %v — using score=0", deviceID, r)
						deepfakeChan <- 0
					}
				}()
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

		// Fix 45: select with ctx.Done() as an extra safety net alongside Fix 37's recover().
		// If the goroutine somehow panics before recover() fires (e.g. runtime.Goexit()),
		// ctx timeout guarantees the pipeline is never permanently blocked.
		var deepfakeScore int
		select {
		case deepfakeScore = <-deepfakeChan:
		case <-ctx.Done():
			deepfakeScore = 0
			log.Printf("⚠️ [%s] Deepfake analysis timed out — using score=0", deviceID)
		}

		if transcript == "" {
			return
		}

		// Prefix transcript with speaker label so FraudDetector and Gemini Agent
		// receive a labeled 2-sided conversation: "[USER] ..." vs "[SCAMMER] ..."
		labeledTranscript := speakerLabel + " " + transcript
		log.Printf("[%s] %s Transcript: '%s'", deviceID, speakerLabel, transcript)

		// ── Lớp 1 Fast Path — gửi transcript ngay lập tức về mobile ─────────
		// Mobile nhận được trong <100ms và chạy LocalFraudScanner.CheckEmergency()
		// TRƯỚC KHI FraudDetector + Gemini Agent hoàn thành (có thể mất 1-5 giây).
		// Mục tiêu: từ lúc kẻ lừa đảo nói xong → điện thoại nạn nhân nhận
		// được transcript preview trong dưới 1 giây.
		sendAlert(models.AlertMessage{
			Type:      "transcript_preview",
			Transcript: labeledTranscript,
			Timestamp: time.Now().Unix(),
		})

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
