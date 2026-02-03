package services

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
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
		return "", fmt.Errorf("empty audio data")
	}

	if len(audioData) < 100 {
		return "", fmt.Errorf("audio data too short (%d bytes)", len(audioData))
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

	req, err := http.NewRequest("POST", url, bytes.NewReader(audioData))
	if err != nil {
		return "", fmt.Errorf("failed to create request: %w", err)
	}

	req.Header.Set("Authorization", "Token "+d.APIKey)
	// Changed from audio/wav to application/octet-stream for raw PCM
	req.Header.Set("Content-Type", "application/octet-stream")

	resp, err := d.HTTPClient.Do(req)
	if err != nil {
		return "", fmt.Errorf("failed to send request: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return "", fmt.Errorf("deepgram API error (status %d): %s", resp.StatusCode, string(body))
	}

	var result DeepgramResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return "", fmt.Errorf("failed to decode response: %w", err)
	}

	if len(result.Results.Channels) > 0 && len(result.Results.Channels[0].Alternatives) > 0 {
		return result.Results.Channels[0].Alternatives[0].Transcript, nil
	}

	return "", fmt.Errorf("no transcription found in response")
}
