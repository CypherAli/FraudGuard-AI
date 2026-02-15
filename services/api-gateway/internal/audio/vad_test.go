package audio

import (
	"testing"
)

func TestNewVADProcessor_ValidParams(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	if vad == nil {
		t.Fatal("VAD is nil")
	}

	if vad.sampleRate != 16000 {
		t.Errorf("Expected sample rate 16000, got %d", vad.sampleRate)
	}
}

func TestNewVADProcessor_InvalidSampleRate(t *testing.T) {
	_, err := NewVADProcessor(12000, 2, 20) // Invalid sample rate
	if err == nil {
		t.Error("Expected error for invalid sample rate, got nil")
	}
}

func TestNewVADProcessor_InvalidAggressiveness(t *testing.T) {
	_, err := NewVADProcessor(16000, 5, 20) // Invalid aggressiveness
	if err == nil {
		t.Error("Expected error for invalid aggressiveness, got nil")
	}
}

func TestNewVADProcessor_InvalidFrameDuration(t *testing.T) {
	_, err := NewVADProcessor(16000, 2, 15) // Invalid frame duration
	if err == nil {
		t.Error("Expected error for invalid frame duration, got nil")
	}
}

func TestVADProcessor_ProcessFrame_Silence(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	// Create 20ms of silence (16000 Hz * 0.02s * 2 bytes = 640 bytes)
	silenceFrame := make([]byte, 640)

	isSpeech, err := vad.ProcessFrame(silenceFrame)
	if err != nil {
		t.Errorf("ProcessFrame failed: %v", err)
	}

	// Silence should not be detected as speech
	if isSpeech {
		t.Error("Silence was incorrectly detected as speech")
	}
}

func TestVADProcessor_ProcessFrame_InvalidSize(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	// Wrong size frame
	wrongFrame := make([]byte, 100)

	_, err = vad.ProcessFrame(wrongFrame)
	if err == nil {
		t.Error("Expected error for invalid frame size, got nil")
	}
}

func TestVADProcessor_GetStatistics(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	// Initially should have zero stats
	total, speech, ratio := vad.GetStatistics()
	if total != 0 || speech != 0 || ratio != 0.0 {
		t.Errorf("Expected zero stats, got total=%d, speech=%d, ratio=%.2f", total, speech, ratio)
	}

	// Process some frames
	silenceFrame := make([]byte, 640)
	for i := 0; i < 10; i++ {
		vad.ProcessFrame(silenceFrame)
	}

	total, speech, ratio = vad.GetStatistics()
	if total != 10 {
		t.Errorf("Expected 10 total frames, got %d", total)
	}

	// Silence frames should have 0% speech ratio
	if ratio > 0.1 {
		t.Errorf("Expected low speech ratio for silence, got %.2f", ratio)
	}
}

func TestVADProcessor_Reset(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	// Process some frames
	silenceFrame := make([]byte, 640)
	for i := 0; i < 5; i++ {
		vad.ProcessFrame(silenceFrame)
	}

	// Reset
	vad.Reset()

	// Stats should be zero after reset
	total, speech, ratio := vad.GetStatistics()
	if total != 0 || speech != 0 || ratio != 0.0 {
		t.Errorf("Expected zero stats after reset, got total=%d, speech=%d, ratio=%.2f", total, speech, ratio)
	}
}

func TestVADProcessor_ProcessBufferWithPadding_SmallBuffer(t *testing.T) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		t.Fatalf("Failed to create VAD: %v", err)
	}

	// Buffer smaller than frame size
	smallBuffer := make([]byte, 100)
	timestamp := int64(1000000)

	segment, isSpeech, _, _ := vad.ProcessBufferWithPadding(smallBuffer, timestamp)

	// Should return nil for small buffer
	if segment != nil {
		t.Error("Expected nil segment for small buffer")
	}

	if isSpeech {
		t.Error("Expected no speech for small buffer")
	}
}

func BenchmarkVADProcessor_ProcessFrame(b *testing.B) {
	vad, err := NewVADProcessor(16000, 2, 20)
	if err != nil {
		b.Fatalf("Failed to create VAD: %v", err)
	}

	// Create 20ms frame
	frame := make([]byte, 640)

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		vad.ProcessFrame(frame)
	}
}
