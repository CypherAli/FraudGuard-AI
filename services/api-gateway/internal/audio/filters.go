package audio

import (
	"encoding/binary"
	"math"
)

// HighPassFilter removes low-frequency noise (< cutoff frequency)
// This is critical for removing speaker bass leakage when hardware AEC fails
// Uses a simple first-order IIR (Infinite Impulse Response) filter
type HighPassFilter struct {
	cutoffFreq float64 // Cutoff frequency in Hz (recommended: 300Hz)
	sampleRate int     // Sample rate in Hz (e.g., 16000)
	alpha      float64 // Filter coefficient
	prevInput  float64 // Previous input sample (for IIR)
	prevOutput float64 // Previous output sample (for IIR)
}

// NewHighPassFilter creates a new high-pass filter
// cutoffFreq: Frequency below which signals are attenuated (e.g., 300Hz)
// sampleRate: Audio sample rate (e.g., 16000Hz)
func NewHighPassFilter(sampleRate int, cutoffFreq float64) *HighPassFilter {
	// Calculate RC time constant
	rc := 1.0 / (2.0 * math.Pi * cutoffFreq)
	dt := 1.0 / float64(sampleRate)

	// Calculate filter coefficient (alpha)
	// alpha = RC / (RC + dt)
	alpha := rc / (rc + dt)

	return &HighPassFilter{
		cutoffFreq: cutoffFreq,
		sampleRate: sampleRate,
		alpha:      alpha,
		prevInput:  0,
		prevOutput: 0,
	}
}

// Process applies high-pass filter to PCM16 audio data
// audioData: Raw PCM16 audio bytes (16-bit signed integers, little-endian)
// Returns: Filtered audio data in the same format
func (f *HighPassFilter) Process(audioData []byte) []byte {
	if len(audioData) == 0 || len(audioData)%2 != 0 {
		return audioData // Return unchanged if invalid
	}

	// Create output buffer
	output := make([]byte, len(audioData))

	// Process each 16-bit sample
	for i := 0; i < len(audioData); i += 2 {
		// Read PCM16 sample (little-endian)
		sample := int16(binary.LittleEndian.Uint16(audioData[i : i+2]))
		inputFloat := float64(sample)

		// Apply first-order IIR high-pass filter
		// y[n] = alpha * (y[n-1] + x[n] - x[n-1])
		outputFloat := f.alpha * (f.prevOutput + inputFloat - f.prevInput)

		// Update state
		f.prevInput = inputFloat
		f.prevOutput = outputFloat

		// Clamp to int16 range to prevent overflow
		if outputFloat > 32767 {
			outputFloat = 32767
		} else if outputFloat < -32768 {
			outputFloat = -32768
		}

		// Write filtered sample (little-endian)
		outputSample := int16(outputFloat)
		binary.LittleEndian.PutUint16(output[i:i+2], uint16(outputSample))
	}

	return output
}

// Reset resets the filter state (useful when starting a new audio stream)
func (f *HighPassFilter) Reset() {
	f.prevInput = 0
	f.prevOutput = 0
}

// GetCutoffFreq returns the cutoff frequency
func (f *HighPassFilter) GetCutoffFreq() float64 {
	return f.cutoffFreq
}
