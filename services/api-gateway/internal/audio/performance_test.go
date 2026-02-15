package audio

import (
	"math/rand"
	"sync"
	"testing"
	"time"
)

// BenchmarkHighPassFilter_SingleStream tests HPF performance for a single audio stream
func BenchmarkHighPassFilter_SingleStream(b *testing.B) {
	filter := NewHighPassFilter(16000, 300.0)

	// Create 100ms of audio data (1600 samples * 2 bytes = 3200 bytes)
	audioData := make([]byte, 3200)
	for i := range audioData {
		audioData[i] = byte(rand.Intn(256))
	}

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		filter.Process(audioData)
	}

	// Report throughput
	b.SetBytes(int64(len(audioData)))
}

// BenchmarkVADProcessor_SingleStream tests VAD performance for a single audio stream
func BenchmarkVADProcessor_SingleStream(b *testing.B) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		b.Fatalf("Failed to create VAD: %v", err)
	}

	// Create 20ms frame (320 samples * 2 bytes = 640 bytes)
	frame := make([]byte, 640)
	for i := range frame {
		frame[i] = byte(rand.Intn(256))
	}

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		vad.ProcessFrame(frame)
	}

	b.SetBytes(int64(len(frame)))
}

// BenchmarkVADProcessor_WithPadding tests VAD with padding for realistic scenario
func BenchmarkVADProcessor_WithPadding(b *testing.B) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		b.Fatalf("Failed to create VAD: %v", err)
	}

	// Create 1 second of audio data (16000 samples * 2 bytes = 32000 bytes)
	audioData := make([]byte, 32000)
	for i := range audioData {
		audioData[i] = byte(rand.Intn(256))
	}

	timestamp := time.Now().UnixMilli()

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		vad.ProcessBufferWithPadding(audioData, timestamp)
	}

	b.SetBytes(int64(len(audioData)))
}

// BenchmarkCombinedPipeline_SingleStream tests HPF + VAD pipeline
func BenchmarkCombinedPipeline_SingleStream(b *testing.B) {
	hpf := NewHighPassFilter(16000, 300.0)
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		b.Fatalf("Failed to create VAD: %v", err)
	}

	// Create 1 second of audio data
	audioData := make([]byte, 32000)
	for i := range audioData {
		audioData[i] = byte(rand.Intn(256))
	}

	timestamp := time.Now().UnixMilli()

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		// Simulate complete pipeline
		filtered := hpf.Process(audioData)
		vad.ProcessBufferWithPadding(filtered, timestamp)
	}

	b.SetBytes(int64(len(audioData)))
}

// BenchmarkConcurrentStreams_10Users simulates 10 concurrent users
func BenchmarkConcurrentStreams_10Users(b *testing.B) {
	benchmarkConcurrentStreams(b, 10)
}

// BenchmarkConcurrentStreams_20Users simulates 20 concurrent users
func BenchmarkConcurrentStreams_20Users(b *testing.B) {
	benchmarkConcurrentStreams(b, 20)
}

// BenchmarkConcurrentStreams_50Users simulates 50 concurrent users (stress test)
func BenchmarkConcurrentStreams_50Users(b *testing.B) {
	benchmarkConcurrentStreams(b, 50)
}

// benchmarkConcurrentStreams is a helper function to test concurrent audio processing
func benchmarkConcurrentStreams(b *testing.B, numUsers int) {
	// Create audio processors for each user
	processors := make([]*userProcessor, numUsers)
	for i := 0; i < numUsers; i++ {
		hpf := NewHighPassFilter(16000, 300.0)
		vad, err := NewVADProcessor(16000, 2, 20)
		if err != nil {
			b.Fatalf("Failed to create VAD for user %d: %v", i, err)
		}
		processors[i] = &userProcessor{
			hpf: hpf,
			vad: vad,
		}
	}

	// Create audio data (100ms chunks, typical WebSocket chunk size)
	audioData := make([]byte, 3200)
	for i := range audioData {
		audioData[i] = byte(rand.Intn(256))
	}

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		var wg sync.WaitGroup
		wg.Add(numUsers)

		// Simulate concurrent processing
		for j := 0; j < numUsers; j++ {
			go func(processor *userProcessor) {
				defer wg.Done()

				timestamp := time.Now().UnixMilli()

				// Process audio chunk (HPF + VAD)
				filtered := processor.hpf.Process(audioData)
				processor.vad.ProcessBufferWithPadding(filtered, timestamp)
			}(processors[j])
		}

		wg.Wait()
	}

	// Report total throughput (all users combined)
	b.SetBytes(int64(len(audioData) * numUsers))
}

// userProcessor represents audio processing state for a single user
type userProcessor struct {
	hpf *HighPassFilter
	vad *VADProcessor
}

// BenchmarkMemoryAllocation tests memory allocation patterns
func BenchmarkMemoryAllocation_Pipeline(b *testing.B) {
	hpf := NewHighPassFilter(16000, 300.0)
	vad, _ := NewVADProcessor(16000, 2, 20)

	audioData := make([]byte, 32000)
	timestamp := time.Now().UnixMilli()

	b.ResetTimer()
	b.ReportAllocs()

	for i := 0; i < b.N; i++ {
		filtered := hpf.Process(audioData)
		speechSegment, _, _, _ := vad.ProcessBufferWithPadding(filtered, timestamp)

		// Simulate using the output
		if speechSegment != nil {
			_ = len(speechSegment)
		}
	}
}

// TestConcurrentStressTest verifies correctness under concurrent load
func TestConcurrentStressTest(t *testing.T) {
	const numUsers = 20
	const chunksPerUser = 100

	var wg sync.WaitGroup
	wg.Add(numUsers)

	errors := make(chan error, numUsers)

	for i := 0; i < numUsers; i++ {
		go func(userID int) {
			defer wg.Done()

			hpf := NewHighPassFilter(16000, 300.0)
			vad, err := NewVADProcessor(16000, 2, 20)
			if err != nil {
				errors <- err
				return
			}

			audioData := make([]byte, 3200)
			for j := range audioData {
				audioData[j] = byte(rand.Intn(256))
			}

			// Process multiple chunks
			for chunk := 0; chunk < chunksPerUser; chunk++ {
				timestamp := time.Now().UnixMilli()

				filtered := hpf.Process(audioData)
				_, _, _, _ = vad.ProcessBufferWithPadding(filtered, timestamp)
			}
		}(i)
	}

	wg.Wait()
	close(errors)

	// Check for errors
	for err := range errors {
		t.Errorf("Concurrent processing error: %v", err)
	}
}

// BenchmarkLatency_SingleChunk measures end-to-end latency for a single chunk
func BenchmarkLatency_SingleChunk(b *testing.B) {
	hpf := NewHighPassFilter(16000, 300.0)
	vad, _ := NewVADProcessor(16000, 2, 20)

	// 100ms chunk (typical WebSocket chunk)
	audioData := make([]byte, 3200)
	for i := range audioData {
		audioData[i] = byte(rand.Intn(256))
	}

	b.ResetTimer()

	for i := 0; i < b.N; i++ {
		start := time.Now()

		timestamp := time.Now().UnixMilli()
		filtered := hpf.Process(audioData)
		vad.ProcessBufferWithPadding(filtered, timestamp)

		latency := time.Since(start)

		// Report if latency is too high (> 10ms is concerning for real-time)
		if latency > 10*time.Millisecond {
			b.Logf("Warning: High latency detected: %v", latency)
		}
	}
}
