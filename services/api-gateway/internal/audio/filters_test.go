package audio

import (
	"testing"
)

func TestHighPassFilter_Basic(t *testing.T) {
	// Create filter: 16kHz sample rate, 300Hz cutoff
	filter := NewHighPassFilter(16000, 300.0)

	if filter == nil {
		t.Fatal("Failed to create filter")
	}

	if filter.GetCutoffFreq() != 300.0 {
		t.Errorf("Expected cutoff 300Hz, got %.2f", filter.GetCutoffFreq())
	}
}

func TestHighPassFilter_ProcessEmptyData(t *testing.T) {
	filter := NewHighPassFilter(16000, 300.0)

	// Empty data should return unchanged
	result := filter.Process([]byte{})
	if len(result) != 0 {
		t.Errorf("Expected empty result, got %d bytes", len(result))
	}
}

func TestHighPassFilter_ProcessInvalidSize(t *testing.T) {
	filter := NewHighPassFilter(16000, 300.0)

	// Odd number of bytes (invalid for PCM16)
	result := filter.Process([]byte{0x00, 0x01, 0x02})
	if len(result) != 3 {
		t.Errorf("Expected unchanged data, got %d bytes", len(result))
	}
}

func TestHighPassFilter_ProcessSilence(t *testing.T) {
	filter := NewHighPassFilter(16000, 300.0)

	// Create 1 second of silence (16000 samples * 2 bytes = 32000 bytes)
	silence := make([]byte, 32000)

	result := filter.Process(silence)

	if len(result) != len(silence) {
		t.Errorf("Expected %d bytes, got %d", len(silence), len(result))
	}

	// Filtered silence should still be mostly zeros
	nonZeroCount := 0
	for _, b := range result {
		if b != 0 {
			nonZeroCount++
		}
	}

	// Allow some numerical noise, but should be mostly zero
	if float64(nonZeroCount)/float64(len(result)) > 0.01 {
		t.Errorf("Filtered silence has too many non-zero bytes: %d/%d", nonZeroCount, len(result))
	}
}

func TestHighPassFilter_Reset(t *testing.T) {
	filter := NewHighPassFilter(16000, 300.0)

	// Process some data
	testData := make([]byte, 1000)
	for i := range testData {
		testData[i] = byte(i % 256)
	}
	filter.Process(testData)

	// Reset should clear state
	filter.Reset()

	// After reset, processing same data should give same result as fresh filter
	filter2 := NewHighPassFilter(16000, 300.0)
	result1 := filter.Process(testData)
	result2 := filter2.Process(testData)

	if len(result1) != len(result2) {
		t.Errorf("Results differ in length after reset: %d vs %d", len(result1), len(result2))
	}
}

func BenchmarkHighPassFilter_Process(b *testing.B) {
	filter := NewHighPassFilter(16000, 300.0)

	// Create 100ms of audio data (1600 samples * 2 bytes = 3200 bytes)
	audioData := make([]byte, 3200)
	for i := range audioData {
		audioData[i] = byte(i % 256)
	}

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		filter.Process(audioData)
	}
}
