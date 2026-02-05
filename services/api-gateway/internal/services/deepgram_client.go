package services

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"time"
)

// DeepgramClient handles communication with Deepgram API
type DeepgramClient struct {
	APIKey     string
	HTTPClient *http.Client
}

// DeepgramResponse represents the response from Deepgram API
type DeepgramResponse struct {
	Results struct {
		Channels []struct {
			Alternatives []struct {
				Transcript string  `json:"transcript"`
				Confidence float64 `json:"confidence"`
			} `json:"alternatives"`
		} `json:"channels"`
	} `json:"results"`
}

// NewDeepgramClient creates a new Deepgram client
func NewDeepgramClient(apiKey string) *DeepgramClient {
	return &DeepgramClient{
		APIKey: apiKey,
		HTTPClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

// TranscribeAudio sends audio data to Deepgram for transcription
func (d *DeepgramClient) TranscribeAudio(audioData []byte) (string, error) {
	// Validate audio data
	if len(audioData) == 0 {
		log.Printf("❌ [Deepgram] Empty audio data")
		return "", fmt.Errorf("empty audio data")
	}

	if len(audioData) < 100 {
		log.Printf("❌ [Deepgram] Audio data too short: %d bytes", len(audioData))
		return "", fmt.Errorf("audio data too short (%d bytes)", len(audioData))
	}

	log.Printf("🔊 [Deepgram] Transcribing audio: %d bytes", len(audioData))

	// Log first bytes for debugging audio format
	if len(audioData) >= 16 {
		log.Printf("🔊 [Deepgram] Audio header (first 16 bytes): %v", audioData[:16])
	}

	// Configure Deepgram for raw PCM linear16
	// encoding=linear16: Raw PCM 16-bit signed integer
	// sample_rate=16000: 16kHz sampling rate
	// channels=1: Mono audio
	url := "https://api.deepgram.com/v1/listen?" +
		"model=nova-2&" +
		"language=vi&" +
		"punctuate=true&" +
		"smart_format=true&" +
		"encoding=linear16&" +
		"sample_rate=16000&" +
		"channels=1"

	log.Printf("🔊 [Deepgram] API URL: %s", url)

	req, err := http.NewRequest("POST", url, bytes.NewReader(audioData))
	if err != nil {
		log.Printf("❌ [Deepgram] Failed to create request: %v", err)
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Authorization", "Token "+d.APIKey)
	// Changed from audio/wav to application/octet-stream for raw PCM
	req.Header.Set("Content-Type", "application/octet-stream")

	log.Printf("🔊 [Deepgram] Sending request...")
	resp, err := d.HTTPClient.Do(req)
	if err != nil {
		log.Printf("❌ [Deepgram] Request failed: %v", err)
		return "", fmt.Errorf("failed to send request: %w", err)
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(resp.Body)
	log.Printf("🔊 [Deepgram] Response status: %d", resp.StatusCode)
	log.Printf("🔊 [Deepgram] Response body (%d bytes): %s", len(body), string(body))

	if resp.StatusCode != http.StatusOK {
		log.Printf("❌ [Deepgram] API error (status %d): %s", resp.StatusCode, string(body))
		return "", fmt.Errorf("deepgram API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result DeepgramResponse
	if err := json.Unmarshal(body, &result); err != nil {
		log.Printf("❌ [Deepgram] Failed to decode response: %v", err)
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Results.Channels) > 0 && len(result.Results.Channels[0].Alternatives) > 0 {
		transcript := result.Results.Channels[0].Alternatives[0].Transcript
		confidence := result.Results.Channels[0].Alternatives[0].Confidence
		log.Printf("✅ [Deepgram] Transcript: '%s' (confidence: %.2f)", transcript, confidence)
		return transcript, nil
	}

	log.Printf("⚠️ [Deepgram] No transcription found in response")
	return "", nil // Return empty string instead of error - silence is valid
}
