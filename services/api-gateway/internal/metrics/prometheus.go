package metrics

import (
	"net/http"
	"os"
	"sync"
	"sync/atomic"
	"time"
)

var metricsEnabled = os.Getenv("FEATURE_METRICS") == "true"

// Counters
var (
	FraudDetectionsTotal   atomic.Int64
	AlertsSentTotal        atomic.Int64
	DeepfakeDetectionsTotal atomic.Int64
	WebSocketConnections   atomic.Int64 // gauge (inc/dec)
	GeminiRequestsTotal    atomic.Int64
	DeepgramRequestsTotal  atomic.Int64
)

// Severity counters
var (
	fraudBySeverity   = make(map[string]*atomic.Int64)
	fraudSeverityMu   sync.RWMutex
)

// Latency tracking
type latencyTracker struct {
	mu      sync.Mutex
	buckets []float64 // in seconds
	count   int64
	sum     float64
}

var (
	DeepgramLatency = &latencyTracker{}
	GeminiLatency   = &latencyTracker{}
)

func init() {
	// Pre-initialize severity counters
	for _, sev := range []string{"CRITICAL", "HIGH", "MEDIUM", "LOW"} {
		fraudBySeverity[sev] = &atomic.Int64{}
	}
}

// IsEnabled returns whether metrics collection is active
func IsEnabled() bool {
	return metricsEnabled
}

// RecordFraudDetection increments fraud detection counter by severity
func RecordFraudDetection(severity string) {
	if !metricsEnabled {
		return
	}
	FraudDetectionsTotal.Add(1)

	fraudSeverityMu.RLock()
	if counter, ok := fraudBySeverity[severity]; ok {
		counter.Add(1)
	}
	fraudSeverityMu.RUnlock()
}

// RecordAlertSent increments alert counter
func RecordAlertSent() {
	if !metricsEnabled {
		return
	}
	AlertsSentTotal.Add(1)
}

// RecordDeepfakeDetection increments deepfake counter
func RecordDeepfakeDetection() {
	if !metricsEnabled {
		return
	}
	DeepfakeDetectionsTotal.Add(1)
}

// RecordWebSocketConnect increments active connections gauge
func RecordWebSocketConnect() {
	WebSocketConnections.Add(1)
}

// RecordWebSocketDisconnect decrements active connections gauge
func RecordWebSocketDisconnect() {
	WebSocketConnections.Add(-1)
}

// RecordLatency records a latency observation
func (lt *latencyTracker) RecordLatency(d time.Duration) {
	if !metricsEnabled {
		return
	}
	lt.mu.Lock()
	defer lt.mu.Unlock()
	seconds := d.Seconds()
	lt.buckets = append(lt.buckets, seconds)
	lt.count++
	lt.sum += seconds

	// Keep last 1000 observations
	if len(lt.buckets) > 1000 {
		lt.buckets = lt.buckets[len(lt.buckets)-1000:]
	}
}

// GetStats returns latency statistics
func (lt *latencyTracker) GetStats() (count int64, avgMs float64) {
	lt.mu.Lock()
	defer lt.mu.Unlock()
	if lt.count == 0 {
		return 0, 0
	}
	return lt.count, (lt.sum / float64(lt.count)) * 1000
}

// MetricsHandler returns an HTTP handler that serves Prometheus-compatible metrics
func MetricsHandler() http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/plain; version=0.0.4; charset=utf-8")

		// Counters
		writeMetric(w, "fraud_detections_total", "counter", FraudDetectionsTotal.Load())
		writeMetric(w, "alerts_sent_total", "counter", AlertsSentTotal.Load())
		writeMetric(w, "deepfake_detections_total", "counter", DeepfakeDetectionsTotal.Load())
		writeMetric(w, "gemini_requests_total", "counter", GeminiRequestsTotal.Load())
		writeMetric(w, "deepgram_requests_total", "counter", DeepgramRequestsTotal.Load())

		// Gauges
		writeMetric(w, "active_websocket_connections", "gauge", WebSocketConnections.Load())

		// Severity breakdown
		fraudSeverityMu.RLock()
		for sev, counter := range fraudBySeverity {
			writeMetricWithLabel(w, "fraud_detections_by_severity", "counter", sev, counter.Load())
		}
		fraudSeverityMu.RUnlock()

		// Latency summaries
		dgCount, dgAvg := DeepgramLatency.GetStats()
		writeMetric(w, "deepgram_requests_count", "counter", dgCount)
		writeMetricFloat(w, "deepgram_avg_latency_ms", "gauge", dgAvg)

		gmCount, gmAvg := GeminiLatency.GetStats()
		writeMetric(w, "gemini_requests_count", "counter", gmCount)
		writeMetricFloat(w, "gemini_avg_latency_ms", "gauge", gmAvg)
	}
}

func writeMetric(w http.ResponseWriter, name, mtype string, value int64) {
	w.Write([]byte("# TYPE " + name + " " + mtype + "\n"))
	w.Write([]byte(name + " " + itoa(value) + "\n"))
}

func writeMetricFloat(w http.ResponseWriter, name, mtype string, value float64) {
	w.Write([]byte("# TYPE " + name + " " + mtype + "\n"))
	w.Write([]byte(name + " " + ftoa(value) + "\n"))
}

func writeMetricWithLabel(w http.ResponseWriter, name, mtype, label string, value int64) {
	w.Write([]byte(name + "{severity=\"" + label + "\"} " + itoa(value) + "\n"))
}

func itoa(v int64) string {
	return string(appendInt(nil, v))
}

func ftoa(v float64) string {
	return string(appendFloat(nil, v))
}

func appendInt(buf []byte, v int64) []byte {
	return append(buf, []byte(intToStr(v))...)
}

func appendFloat(buf []byte, v float64) []byte {
	return append(buf, []byte(floatToStr(v))...)
}

func intToStr(v int64) string {
	if v == 0 {
		return "0"
	}
	neg := false
	if v < 0 {
		neg = true
		v = -v
	}
	buf := make([]byte, 0, 20)
	for v > 0 {
		buf = append(buf, byte('0'+v%10))
		v /= 10
	}
	if neg {
		buf = append(buf, '-')
	}
	// Reverse
	for i, j := 0, len(buf)-1; i < j; i, j = i+1, j-1 {
		buf[i], buf[j] = buf[j], buf[i]
	}
	return string(buf)
}

func floatToStr(v float64) string {
	// Simple float formatting to 2 decimal places
	intPart := int64(v)
	fracPart := int64((v - float64(intPart)) * 100)
	if fracPart < 0 {
		fracPart = -fracPart
	}
	return intToStr(intPart) + "." + padLeft(intToStr(fracPart), 2)
}

func padLeft(s string, n int) string {
	for len(s) < n {
		s = "0" + s
	}
	return s
}
