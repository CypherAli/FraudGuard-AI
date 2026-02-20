package audio

import (
	"container/ring"
	"encoding/binary"
	"fmt"
	"log"
	"math"
)

// VADProcessor handles Voice Activity Detection with padding support
// Uses energy-based detection (pure Go, no CGO required)
// Padding is critical to prevent "cold start" - Deepgram needs context before/after speech
type VADProcessor struct {
	sampleRate int
	frameSize  int // Frame size in bytes (20ms worth of audio recommended)

	// Energy-based VAD parameters
	energyThreshold float64 // Energy threshold for speech detection
	aggressiveness  int     // 0-3, higher = more aggressive filtering

	// Padding configuration (in milliseconds)
	prePaddingMs  int // Audio to include before speech starts (default: 300ms)
	postPaddingMs int // Audio to include after speech ends (default: 300ms)

	// Ring buffer for pre-padding
	// Stores recent audio frames so we can include them when speech is detected
	preBuffer *ring.Ring

	// Post-padding state
	postPaddingFramesRemaining int
	lastSpeechDetected         bool

	// Adaptive threshold (for noise floor estimation)
	noiseFloor        float64
	noiseFloorSamples int
	adaptiveEnabled   bool

	// Statistics
	totalFrames  int64
	speechFrames int64
}

// NewVADProcessor creates a new VAD processor with padding support
// sampleRate: Audio sample rate (8000, 16000, 32000, or 48000 Hz)
// aggressiveness: VAD sensitivity (0-3, higher = more aggressive filtering)
//
//	0 = Least aggressive (keeps more audio, good for quiet speech)
//	1 = Quality mode
//	2 = Low bitrate mode (recommended for telephony)
//	3 = Very aggressive (maximum filtering, may cut some speech)
//
// frameDurationMs: Frame duration in milliseconds (recommended: 20ms)
func NewVADProcessor(sampleRate int, aggressiveness int, frameDurationMs int) (*VADProcessor, error) {
	// Validate parameters
	if sampleRate != 8000 && sampleRate != 16000 && sampleRate != 32000 && sampleRate != 48000 {
		return nil, fmt.Errorf("invalid sample rate: %d (must be 8000, 16000, 32000, or 48000)", sampleRate)
	}

	if aggressiveness < 0 || aggressiveness > 3 {
		return nil, fmt.Errorf("invalid aggressiveness: %d (must be 0-3)", aggressiveness)
	}

	// Validate frame duration — WebRTC VAD supports only 10, 20, or 30ms
	if frameDurationMs != 10 && frameDurationMs != 20 && frameDurationMs != 30 {
		return nil, fmt.Errorf("invalid frame duration: %dms (must be 10, 20, or 30ms)", frameDurationMs)
	}

	// Calculate frame size in bytes
	// PCM16 = 2 bytes per sample
	samplesPerFrame := (sampleRate * frameDurationMs) / 1000
	frameSize := samplesPerFrame * 2

	// Set energy threshold based on aggressiveness
	// Higher aggressiveness = higher threshold = more filtering
	var energyThreshold float64
	switch aggressiveness {
	case 0:
		energyThreshold = 100.0 // Very sensitive
	case 1:
		energyThreshold = 300.0 // Balanced
	case 2:
		energyThreshold = 500.0 // Telephony (recommended)
	case 3:
		energyThreshold = 800.0 // Very aggressive
	}

	// Calculate ring buffer size for pre-padding
	// We want to store 300ms of audio
	prePaddingMs := 300
	postPaddingMs := 300
	framesForPrePadding := (prePaddingMs + frameDurationMs - 1) / frameDurationMs // Round up

	// Create ring buffer
	preBuffer := ring.New(framesForPrePadding)

	log.Printf("🎙️ [VAD] Initialized (Energy-based): rate=%dHz, aggr=%d, frame=%dms (%d bytes), threshold=%.0f, pre-pad=%dms (%d frames), post-pad=%dms",
		sampleRate, aggressiveness, frameDurationMs, frameSize, energyThreshold, prePaddingMs, framesForPrePadding, postPaddingMs)

	return &VADProcessor{
		sampleRate:                 sampleRate,
		frameSize:                  frameSize,
		energyThreshold:            energyThreshold,
		aggressiveness:             aggressiveness,
		prePaddingMs:               prePaddingMs,
		postPaddingMs:              postPaddingMs,
		preBuffer:                  preBuffer,
		postPaddingFramesRemaining: 0,
		lastSpeechDetected:         false,
		noiseFloor:                 0,
		noiseFloorSamples:          0,
		adaptiveEnabled:            true,
		totalFrames:                0,
		speechFrames:               0,
	}, nil
}

// calculateEnergy calculates the energy (RMS) of an audio frame
func (v *VADProcessor) calculateEnergy(frame []byte) float64 {
	if len(frame) < 2 || len(frame)%2 != 0 {
		return 0
	}

	var sumSquares float64
	sampleCount := len(frame) / 2

	for i := 0; i < len(frame); i += 2 {
		// Read PCM16 sample (little-endian)
		sample := int16(binary.LittleEndian.Uint16(frame[i : i+2]))
		sampleFloat := float64(sample)
		sumSquares += sampleFloat * sampleFloat
	}

	// Calculate RMS (Root Mean Square)
	rms := math.Sqrt(sumSquares / float64(sampleCount))
	return rms
}

// ProcessFrame processes a single audio frame and returns whether it contains speech
// frame: Audio data (must be exactly frameSize bytes)
// Returns: (isSpeech, error)
func (v *VADProcessor) ProcessFrame(frame []byte) (bool, error) {
	if len(frame) != v.frameSize {
		return false, fmt.Errorf("invalid frame size: got %d bytes, expected %d", len(frame), v.frameSize)
	}

	v.totalFrames++

	// Calculate energy of this frame
	energy := v.calculateEnergy(frame)

	// Adaptive noise floor estimation
	if v.adaptiveEnabled {
		if v.noiseFloorSamples < 10 {
			// Initial noise floor estimation (first 10 frames)
			v.noiseFloor = (v.noiseFloor*float64(v.noiseFloorSamples) + energy) / float64(v.noiseFloorSamples+1)
			v.noiseFloorSamples++
		} else {
			// Slowly adapt noise floor (only when no speech detected)
			if energy < v.energyThreshold {
				v.noiseFloor = 0.95*v.noiseFloor + 0.05*energy
			}
		}
	}

	// Speech detection: energy above threshold
	// Add noise floor margin for better detection
	effectiveThreshold := v.energyThreshold + v.noiseFloor
	isSpeech := energy > effectiveThreshold

	if isSpeech {
		v.speechFrames++
	}

	return isSpeech, nil
}

// ProcessBufferWithPadding processes audio buffer and returns speech segments with padding
// audioData: Raw PCM16 audio data
// timestamp: Unix timestamp in milliseconds when this audio was captured
// Returns: (speechSegment, isSpeech, segmentTimestamp, silenceDurationMs)
//   - speechSegment: Audio data with padding (nil if no speech)
//   - isSpeech: True if this segment contains speech
//   - segmentTimestamp: Adjusted timestamp accounting for pre-padding
//   - silenceDurationMs: Duration of silence if no speech detected
func (v *VADProcessor) ProcessBufferWithPadding(audioData []byte, timestamp int64) ([]byte, bool, int64, int64) {
	if len(audioData) < v.frameSize {
		// Not enough data for a frame, store in ring buffer and return
		v.preBuffer.Value = audioData
		v.preBuffer = v.preBuffer.Next()
		return nil, false, timestamp, 0
	}

	// Split audio into frames
	var speechSegment []byte
	hasSpeech := false

	for offset := 0; offset+v.frameSize <= len(audioData); offset += v.frameSize {
		frame := audioData[offset : offset+v.frameSize]

		isSpeech, err := v.ProcessFrame(frame)
		if err != nil {
			log.Printf("⚠️ [VAD] Frame processing error: %v", err)
			continue
		}

		if isSpeech {
			// Speech detected!
			if !v.lastSpeechDetected {
				// First speech frame after silence
				// Include pre-padding from ring buffer
				log.Printf("🗣️ [VAD] Speech START detected - including %dms pre-padding", v.prePaddingMs)

				// Collect frames from ring buffer
				v.preBuffer.Do(func(val interface{}) {
					if val != nil {
						if frameData, ok := val.([]byte); ok && len(frameData) > 0 {
							speechSegment = append(speechSegment, frameData...)
						}
					}
				})

				hasSpeech = true
			}

			// Add current frame
			speechSegment = append(speechSegment, frame...)

			// Reset post-padding counter
			frameDurationMs := (v.frameSize * 1000) / (v.sampleRate * 2)
			v.postPaddingFramesRemaining = v.postPaddingMs / frameDurationMs
			v.lastSpeechDetected = true

		} else {
			// No speech in this frame
			if v.lastSpeechDetected && v.postPaddingFramesRemaining > 0 {
				// We're in post-padding period after speech ended
				speechSegment = append(speechSegment, frame...)
				v.postPaddingFramesRemaining--

				if v.postPaddingFramesRemaining == 0 {
					// Post-padding complete
					log.Printf("🔇 [VAD] Speech END detected - included %dms post-padding", v.postPaddingMs)
					v.lastSpeechDetected = false
					hasSpeech = true
					break // Return this segment
				}
			} else {
				// Pure silence, store in ring buffer for potential future pre-padding
				v.preBuffer.Value = frame
				v.preBuffer = v.preBuffer.Next()
				v.lastSpeechDetected = false
			}
		}
	}

	// Calculate adjusted timestamp (accounting for pre-padding)
	adjustedTimestamp := timestamp
	if hasSpeech {
		adjustedTimestamp -= int64(v.prePaddingMs)
	}

	// Calculate silence duration if no speech
	var silenceDurationMs int64
	if !hasSpeech {
		frameDurationMs := int64((v.frameSize * 1000) / (v.sampleRate * 2))
		numFrames := int64(len(audioData)) / int64(v.frameSize)
		silenceDurationMs = numFrames * frameDurationMs
	}

	return speechSegment, hasSpeech, adjustedTimestamp, silenceDurationMs
}

// GetStatistics returns VAD statistics
func (v *VADProcessor) GetStatistics() (totalFrames int64, speechFrames int64, speechRatio float64) {
	ratio := 0.0
	if v.totalFrames > 0 {
		ratio = float64(v.speechFrames) / float64(v.totalFrames)
	}
	return v.totalFrames, v.speechFrames, ratio
}

// Reset resets VAD state (useful when starting a new session)
func (v *VADProcessor) Reset() {
	v.totalFrames = 0
	v.speechFrames = 0
	v.postPaddingFramesRemaining = 0
	v.lastSpeechDetected = false
	v.noiseFloor = 0
	v.noiseFloorSamples = 0

	// Clear ring buffer
	v.preBuffer.Do(func(val interface{}) {
		val = nil
	})

	log.Printf("🔄 [VAD] State reset")
}
