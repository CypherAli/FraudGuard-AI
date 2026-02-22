package services

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"time"

	"crypto/hmac"
	"crypto/sha256"
	"encoding/hex"
	"sort"
	"strings"

	"github.com/fraudguard/api-gateway/internal/metrics"
)

// TranscribeClient handles communication with Amazon Transcribe as STT fallback
// Used when Deepgram circuit breaker is open
type TranscribeClient struct {
	AccessKeyID     string
	SecretAccessKey string
	Region          string
	HTTPClient      *http.Client
}

// transcribeStreamResponse dùng cho Transcribe Streaming (real-time, không cần S3)
// Gọi qua HTTP/1.1 chunked streaming endpoint
type transcribeStreamResponse struct {
	Transcript struct {
		Results []struct {
			Alternatives []struct {
				Transcript string `json:"Transcript"`
				Items      []struct {
					Content    string  `json:"Content"`
					Confidence float64 `json:"Confidence,string"`
				} `json:"Items"`
			} `json:"Alternatives"`
			IsPartial bool `json:"IsPartial"`
		} `json:"Results"`
	} `json:"Transcript"`
}

// NewTranscribeClient tạo Amazon Transcribe client mới
// Đọc credentials từ environment variables
func NewTranscribeClient() *TranscribeClient {
	return &TranscribeClient{
		AccessKeyID:     os.Getenv("AWS_ACCESS_KEY_ID"),
		SecretAccessKey: os.Getenv("AWS_SECRET_ACCESS_KEY"),
		Region:          getEnvWithDefault("AWS_REGION", "ap-southeast-1"),
		HTTPClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

// IsEnabled kiểm tra client đã được cấu hình chưa
func (t *TranscribeClient) IsEnabled() bool {
	return t != nil && t.AccessKeyID != "" && t.SecretAccessKey != ""
}

// TranscribeAudio gửi audio PCM16 lên Amazon Transcribe Streaming
// Đây là fallback khi Deepgram circuit breaker open
// Sử dụng Amazon Transcribe Streaming HTTP/2 API
func (t *TranscribeClient) TranscribeAudio(audioData []byte) (string, error) {
	if !t.IsEnabled() {
		return "", fmt.Errorf("Amazon Transcribe not configured (missing AWS credentials)")
	}
	if len(audioData) == 0 {
		return "", fmt.Errorf("empty audio data")
	}

	log.Printf("🔄 [Transcribe] Fallback STT: %d bytes via Amazon Transcribe Streaming", len(audioData))

	// Track metrics
	metrics.RecordTranscribeRequest()
	start := time.Now()

	// Amazon Transcribe Streaming endpoint
	endpoint := fmt.Sprintf("https://transcribestreaming.%s.amazonaws.com/stream-transcription", t.Region)

	ctx, cancel := context.WithTimeout(context.Background(), 25*time.Second)
	defer cancel()

	req, err := http.NewRequestWithContext(ctx, "POST", endpoint, bytes.NewReader(audioData))
	if err != nil {
		return "", fmt.Errorf("failed to create transcribe request: %w", err)
	}

	// Headers bắt buộc cho Transcribe Streaming
	req.Header.Set("Content-Type", "application/octet-stream")
	req.Header.Set("x-amzn-transcribe-language-code", "vi-VN")
	req.Header.Set("x-amzn-transcribe-media-encoding", "pcm")
	req.Header.Set("x-amzn-transcribe-sample-rate", "16000")
	req.Header.Set("x-amzn-transcribe-enable-partial-results-stabilization", "true")
	req.Header.Set("x-amzn-transcribe-partial-results-stability", "high")

	// Ký request bằng AWS Signature Version 4
	if err := t.signRequest(req, audioData); err != nil {
		return "", fmt.Errorf("failed to sign request: %w", err)
	}

	resp, err := t.HTTPClient.Do(req)
	if err != nil {
		return "", fmt.Errorf("transcribe request failed: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		log.Printf("❌ [Transcribe] API error (status %d): %s", resp.StatusCode, string(body))
		return "", fmt.Errorf("transcribe API error (status %d)", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return "", fmt.Errorf("failed to read transcribe response: %w", err)
	}

	// Parse streaming response
	var streamResp transcribeStreamResponse
	if err := json.Unmarshal(body, &streamResp); err != nil {
		return "", fmt.Errorf("failed to parse transcribe response: %w", err)
	}

	// Lấy transcript cuối cùng (không phải partial)
	var finalTranscript string
	hasFinalResult := false
	for _, result := range streamResp.Transcript.Results {
		if !result.IsPartial && len(result.Alternatives) > 0 {
			finalTranscript = result.Alternatives[0].Transcript
			hasFinalResult = true
		}
	}

	// Record latency
	metrics.TranscribeLatency.RecordLatency(time.Since(start))

	// If we got results but none were final (all partial), log a warning
	// This can happen when audio is too short or speech is cut off mid-sentence
	if !hasFinalResult && len(streamResp.Transcript.Results) > 0 {
		log.Printf("⚠️ [Transcribe] Only partial results received — audio may be too short or cut off")
		return "", fmt.Errorf("transcribe returned only partial results, no final transcript")
	}

	log.Printf("✅ [Transcribe] Transcript: '%s'", finalTranscript)
	return finalTranscript, nil
}

// signRequest ký HTTP request bằng AWS Signature Version 4
func (t *TranscribeClient) signRequest(req *http.Request, body []byte) error {
	now := time.Now().UTC()
	dateStamp := now.Format("20060102")
	amzDate := now.Format("20060102T150405Z")

	req.Header.Set("x-amz-date", amzDate)
	req.Header.Set("host", req.Host)

	// Bước 1: Canonical Request
	canonicalHeaders, signedHeaders := buildCanonicalHeaders(req)
	payloadHash := hashSHA256(body)

	canonicalRequest := strings.Join([]string{
		req.Method,
		req.URL.Path,
		req.URL.RawQuery,
		canonicalHeaders,
		signedHeaders,
		payloadHash,
	}, "\n")

	// Bước 2: String to Sign
	credentialScope := fmt.Sprintf("%s/%s/transcribe/aws4_request", dateStamp, t.Region)
	stringToSign := strings.Join([]string{
		"AWS4-HMAC-SHA256",
		amzDate,
		credentialScope,
		hashSHA256([]byte(canonicalRequest)),
	}, "\n")

	// Bước 3: Signing Key
	signingKey := getSigningKey(t.SecretAccessKey, dateStamp, t.Region, "transcribe")

	// Bước 4: Signature
	signature := hex.EncodeToString(hmacSHA256(signingKey, []byte(stringToSign)))

	// Bước 5: Authorization header
	authHeader := fmt.Sprintf(
		"AWS4-HMAC-SHA256 Credential=%s/%s, SignedHeaders=%s, Signature=%s",
		t.AccessKeyID, credentialScope, signedHeaders, signature,
	)
	req.Header.Set("Authorization", authHeader)

	return nil
}

// --- AWS Signature V4 helpers ---

func buildCanonicalHeaders(req *http.Request) (canonical string, signed string) {
	headers := make(map[string]string)
	for key, vals := range req.Header {
		lowerKey := strings.ToLower(key)
		headers[lowerKey] = strings.TrimSpace(vals[0])
	}
	// host harus ada
	headers["host"] = req.Host

	keys := make([]string, 0, len(headers))
	for k := range headers {
		keys = append(keys, k)
	}
	sort.Strings(keys)

	var canonicalLines []string
	for _, k := range keys {
		canonicalLines = append(canonicalLines, k+":"+headers[k])
	}

	canonical = strings.Join(canonicalLines, "\n") + "\n"
	signed = strings.Join(keys, ";")
	return
}

func hashSHA256(data []byte) string {
	h := sha256.Sum256(data)
	return hex.EncodeToString(h[:])
}

func hmacSHA256(key, data []byte) []byte {
	h := hmac.New(sha256.New, key)
	h.Write(data)
	return h.Sum(nil)
}

func getSigningKey(secretKey, date, region, service string) []byte {
	kDate := hmacSHA256([]byte("AWS4"+secretKey), []byte(date))
	kRegion := hmacSHA256(kDate, []byte(region))
	kService := hmacSHA256(kRegion, []byte(service))
	kSigning := hmacSHA256(kService, []byte("aws4_request"))
	return kSigning
}

func getEnvWithDefault(key, defaultVal string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return defaultVal
}
