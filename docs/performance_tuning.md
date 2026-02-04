# Performance Tuning & Optimization Guide

## Overview

This document details all performance optimizations implemented in FraudGuard-AI to ensure reliable, real-time fraud detection during phone calls.

## Critical Improvements Implemented

### 1. Alert System Debugging & Enhancement

#### Problem
Fraud alerts were not triggering during actual fraud calls, despite keyword detection working.

#### Solution
- **Comprehensive Logging**: Added detailed logging at every step of the alert pipeline
  - Fraud detector: Input text, keyword matches, score calculation, threshold comparison
  - Audio processor: Transcription status, analysis results, alert creation
  - WebSocket client: JSON marshaling, channel delivery, buffer status
  - Mobile app: Message reception, JSON parsing, event triggering, UI updates

- **Lowered Thresholds**: Adjusted for Vietnamese fraud patterns
  ```
  OLD: LOW=30, MEDIUM=50, HIGH=70, CRITICAL=90
  NEW: LOW=20, MEDIUM=40, HIGH=60, CRITICAL=80
  ```

- **Configuration System**: Environment variable support for production tuning
  ```bash
  FRAUD_THRESHOLD_MEDIUM=40
  FRAUD_THRESHOLD_HIGH=60
  FRAUD_THRESHOLD_CRITICAL=80
  ```

#### Verification
Check logs for the complete alert flow:
```
🔍 [device_id] ===== FRAUD ANALYSIS START =====
🔍 [device_id] Input text: 'chuyển tiền ngay'
🔴 [device_id] Critical keyword detected: 'chuyển tiền' (+50 points)
🚨🚨🚨 [device_id] CRITICAL ALERT TRIGGERED! Score=50 (threshold=40)
📦 [device_id] Alert message created: Type=alert, AlertType=CRITICAL
✅✅✅ [device_id] Alert successfully queued to WebSocket channel
[AudioService] ✅ Alert message detected!
[MainPage] 🚨 HIGH RISK ALERT - Triggering high risk handler
```

---

### 2. Circuit Breaker Pattern

#### Purpose
Protect system from cascading failures when Deepgram API is down or slow.

#### Implementation
- **Three States**:
  - `CLOSED`: Normal operation, all requests allowed
  - `OPEN`: Too many failures, reject requests to prevent overload
  - `HALF_OPEN`: Testing if service recovered

- **Configuration**:
  ```go
  FailureThreshold: 5      // Open after 5 consecutive failures
  Timeout: 30 seconds      // Wait 30s before testing recovery
  ```

#### Behavior
```
Normal → 5 failures → OPEN (reject for 30s) → HALF_OPEN (test) → Success → CLOSED
                                                                 → Failure → OPEN
```

#### Monitoring
```go
log.Printf("🔴 [CircuitBreaker:Deepgram] OPENED (too many failures: 5)")
log.Printf("🔄 [CircuitBreaker:Deepgram] Attempting recovery (OPEN → HALF_OPEN)")
log.Printf("🟢 [CircuitBreaker:Deepgram] RECOVERED (HALF_OPEN → CLOSED)")
```

---

### 3. Buffer Pooling (Reduce GC Pressure)

#### Problem
Audio processing creates many short-lived byte arrays, causing frequent garbage collection.

#### Solution
```go
var audioBufferPool = sync.Pool{
    New: func() interface{} {
        buf := make([]byte, 8192)
        return &buf
    },
}

// Usage
buffer := audioBufferPool.Get().(*[]byte)
defer audioBufferPool.Put(buffer)
```

#### Benefits
- **Reduced Allocations**: Reuse buffers instead of allocating new ones
- **Lower GC Pressure**: Fewer objects for garbage collector to track
- **Better Performance**: Less CPU time spent in GC pauses

---

### 4. Stale Data Handling

#### Problem
In real-time fraud detection, old audio data is worthless. If network lags and buffer fills up, processing delayed audio means alerts arrive too late.

#### Solution
```csharp
const int maxAudioAge = 5 * time.Second;
if (sap.bufferSize > 0 && time.Since(sap.lastProcessed) > maxAudioAge) {
    log.Printf("⚠️ Dropping stale audio buffer (%v old, %d bytes)",
        time.Since(sap.lastProcessed), sap.bufferSize);
    sap.audioBuffer = make([]byte, 0);
    sap.bufferSize = 0;
}
```

#### Rationale
Better to drop 1-2 seconds of old audio and process fresh data than to process a 10-second backlog where fraud alerts arrive too late to be useful.

---

### 5. Exponential Backoff with Jitter (Mobile Reconnection)

#### Problem
When server restarts, all clients reconnect simultaneously → "Thundering Herd" → server overload.

#### Solution
```csharp
// Exponential backoff: 1s, 2s, 4s, 8s, 16s
int baseDelay = (int)Math.Pow(2, _reconnectAttempts - 1) * 1000;

// Add jitter (0-1000ms) to prevent synchronized reconnects
int jitter = _random.Next(0, 1000);
int totalDelay = baseDelay + jitter;

await Task.Delay(totalDelay);
```

#### Example Timeline
```
Attempt 1: 1000ms + 234ms = 1.234s
Attempt 2: 2000ms + 789ms = 2.789s
Attempt 3: 4000ms + 456ms = 4.456s
Attempt 4: 8000ms + 123ms = 8.123s
Attempt 5: 16000ms + 890ms = 16.890s
```

---

### 6. WebSocket Buffer Optimization

#### Configuration
```go
const (
    writeWait      = 10 * time.Second
    pongWait       = 60 * time.Second
    pingPeriod     = 54 * time.Second  // (pongWait * 9) / 10
    maxMessageSize = 512 * 1024        // 512 KB for audio chunks
)

// Client send buffer
send: make(chan []byte, 256)  // 256 message buffer
```

#### Buffer Size Selection
- **8192 bytes** for audio chunks: Balances latency and throughput
  - Too small (1024): Excessive TCP fragmentation, high CPU overhead
  - Too large (32KB): Increased latency, delayed alerts
  - 8192: Sweet spot for 16kHz audio (0.5s of audio)

---

## Performance Benchmarks

### Expected Metrics

| Metric | Target | Notes |
|--------|--------|-------|
| Alert Latency | < 3 seconds | From keyword spoken to mobile alert |
| Concurrent Connections | 50-100 | Without degradation |
| Memory Usage | < 500MB | For 100 concurrent connections |
| CPU Usage | < 50% | On 4-core server |
| Transcription Accuracy | > 90% | Vietnamese language |

### Load Testing

#### Setup
```bash
# Install k6
choco install k6  # Windows
brew install k6   # macOS

# Run load test
k6 run --vus 50 --duration 2m tests/load_test.js
```

#### Test Scenarios
1. **Baseline** (10 connections): Establish performance baseline
2. **Target** (50-100 connections): Verify production capacity
3. **Stress** (200+ connections): Find breaking point

---

## Monitoring & Debugging

### Backend Logs to Watch

```bash
# Successful fraud detection flow
grep "FRAUD ANALYSIS START" logs.txt
grep "CRITICAL ALERT TRIGGERED" logs.txt
grep "Alert successfully queued" logs.txt

# Circuit breaker status
grep "CircuitBreaker" logs.txt

# Deepgram API health
grep "Deepgram transcription error" logs.txt
```

### Mobile Logs to Watch

```bash
# Android logcat
adb logcat | grep "AudioService\|MainPage"

# Look for
[AudioService] ===== PROCESSING SERVER MESSAGE =====
[AudioService] ✅ Alert message detected!
[MainPage] 🚨 HIGH RISK ALERT - Triggering high risk handler
```

### Common Issues

#### Issue 1: Alerts Not Triggering
**Symptoms**: Keywords detected but no mobile alert

**Debug Steps**:
1. Check backend logs for "CRITICAL ALERT TRIGGERED"
2. Check "Alert successfully queued to WebSocket channel"
3. Check mobile logs for "PROCESSING SERVER MESSAGE"
4. Verify JSON format matches expected structure

**Common Causes**:
- Thresholds too high (adjust in environment variables)
- WebSocket buffer full (increase buffer size)
- JSON format mismatch (check alert_type vs alertType)

#### Issue 2: High Latency
**Symptoms**: Alerts arrive 10+ seconds after keywords spoken

**Debug Steps**:
1. Check for "Dropping stale audio buffer" warnings
2. Monitor Deepgram API response times
3. Check network latency between mobile and server

**Solutions**:
- Reduce audio buffer size
- Increase processing interval
- Check Deepgram API status

#### Issue 3: Connection Drops
**Symptoms**: Frequent reconnections

**Debug Steps**:
1. Check circuit breaker state
2. Monitor ping/pong messages
3. Check network stability

**Solutions**:
- Increase pongWait timeout
- Implement better error handling
- Check firewall/NAT settings

---

## Production Deployment Checklist

### Environment Variables
```bash
# Fraud detection thresholds
export FRAUD_THRESHOLD_LOW=20
export FRAUD_THRESHOLD_MEDIUM=40
export FRAUD_THRESHOLD_HIGH=60
export FRAUD_THRESHOLD_CRITICAL=80

# Performance tuning
export FRAUD_MAX_AUDIO_AGE_SECONDS=5
export MAX_CONCURRENT_TRANSCRIPTIONS=10

# Deepgram API
export DEEPGRAM_API_KEY=your_key_here
```

### Server Configuration
- **CPU**: Minimum 2 cores, recommended 4 cores
- **RAM**: Minimum 2GB, recommended 4GB
- **Network**: Low latency (<50ms) to Deepgram API
- **Storage**: 10GB for logs and database

### Monitoring Setup
1. **Application Logs**: Centralized logging (e.g., ELK stack)
2. **Metrics**: Prometheus + Grafana for real-time monitoring
3. **Alerts**: PagerDuty/Slack for critical issues
4. **Health Checks**: `/health` endpoint monitoring

---

## Future Optimizations

### Planned Improvements
1. **AI-Powered Detection**: Integrate GPT/Gemini for semantic analysis
2. **Adaptive Thresholds**: Machine learning to adjust based on patterns
3. **Multi-Language Support**: Expand beyond Vietnamese
4. **Edge Processing**: On-device fraud detection for offline scenarios
5. **Distributed System**: Scale horizontally with Redis pub/sub

### Research Areas
- WebRTC for lower latency audio streaming
- On-device speech recognition (Whisper.cpp)
- Federated learning for privacy-preserving model updates
- Real-time speaker diarization (detect multiple speakers)

---

## References

- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
- [Exponential Backoff](https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/)
- [WebSocket Best Practices](https://www.ably.io/topic/websockets)
- [Go sync.Pool](https://golang.org/pkg/sync/#Pool)
- [Deepgram API Docs](https://developers.deepgram.com/)
