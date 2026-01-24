# Deepgram Optimization Guide

## 🎯 Current Implementation vs Future Optimization

### Current: HTTP API (Batch Processing)

**What we're using now:**
```go
// Current implementation in deepgram_client.go
func (d *DeepgramClient) TranscribeAudio(audioData []byte) (string, error) {
    url := "https://api.deepgram.com/v1/listen?model=nova-2&language=vi"
    req, _ := http.NewRequest("POST", url, bytes.NewReader(audioData))
    // ... send request and get response
}
```

**Pros:**
- ✅ Simple to implement
- ✅ Easy to debug
- ✅ Reliable for demos
- ✅ Works with any audio chunk size

**Cons:**
- ⚠️ Higher latency (~500ms-2s per chunk)
- ⚠️ Each chunk is a separate HTTP request
- ⚠️ No real-time streaming

**Recommended for:**
- ✅ **Hackathon/Demo** (current phase)
- ✅ MVP testing
- ✅ Low-volume usage

---

### Future: WebSocket Streaming API

**What it would look like:**
```go
// Future implementation (TODO)
type DeepgramStreamingClient struct {
    conn      *websocket.Conn
    apiKey    string
    callbacks *TranscriptionCallbacks
}

func (d *DeepgramStreamingClient) StreamAudio(audioChunk []byte) error {
    // Send audio chunk immediately via WebSocket
    return d.conn.WriteMessage(websocket.BinaryMessage, audioChunk)
}

// Receive transcripts in real-time via callback
func (d *DeepgramStreamingClient) onTranscript(transcript string) {
    // Process immediately as audio is being spoken
}
```

**Pros:**
- ✅ **Much lower latency** (~100-300ms)
- ✅ True real-time transcription
- ✅ Single persistent connection
- ✅ Interim results (partial transcripts)

**Cons:**
- ⚠️ More complex to implement
- ⚠️ Need to handle connection lifecycle
- ⚠️ Requires reconnection logic

**Recommended for:**
- 🔮 Production deployment
- 🔮 High-volume usage
- 🔮 Real-time critical applications

---

## 📊 Performance Comparison

| Metric | HTTP API (Current) | WebSocket Streaming (Future) |
|--------|-------------------|------------------------------|
| **Latency** | 500ms - 2s | 100ms - 300ms |
| **Connection** | New per chunk | Persistent |
| **Real-time** | No | Yes |
| **Complexity** | Low | Medium |
| **Reliability** | High | Medium (need reconnect) |
| **Best for** | Demo/MVP | Production |

---

## 🚀 Migration Path (When Ready)

### Phase 1: Current (Hackathon) ✅
- Use HTTP API
- Focus on functionality
- Get feedback on accuracy

### Phase 2: Optimization (Post-Hackathon)
1. Implement WebSocket client
2. Add connection pooling
3. Handle reconnection
4. A/B test performance

### Phase 3: Production
1. Switch to WebSocket by default
2. Keep HTTP as fallback
3. Monitor latency metrics
4. Optimize based on data

---

## 💡 Implementation Notes for Future

### WebSocket Client Structure

```go
// TODO: Implement this after hackathon
type DeepgramStreamingClient struct {
    conn          *websocket.Conn
    apiKey        string
    isConnected   bool
    mu            sync.Mutex
    
    // Callbacks
    onTranscript  func(string)
    onError       func(error)
    onClose       func()
    
    // Reconnection
    reconnectChan chan struct{}
    maxRetries    int
}

func (d *DeepgramStreamingClient) Connect() error {
    url := "wss://api.deepgram.com/v1/listen?model=nova-2&language=vi"
    headers := http.Header{
        "Authorization": []string{"Token " + d.apiKey},
    }
    
    conn, _, err := websocket.DefaultDialer.Dial(url, headers)
    if err != nil {
        return err
    }
    
    d.conn = conn
    d.isConnected = true
    
    // Start listening for responses
    go d.listenForTranscripts()
    
    return nil
}

func (d *DeepgramStreamingClient) StreamAudio(chunk []byte) error {
    d.mu.Lock()
    defer d.mu.Unlock()
    
    if !d.isConnected {
        return fmt.Errorf("not connected")
    }
    
    return d.conn.WriteMessage(websocket.BinaryMessage, chunk)
}

func (d *DeepgramStreamingClient) listenForTranscripts() {
    for {
        var response DeepgramStreamingResponse
        err := d.conn.ReadJSON(&response)
        
        if err != nil {
            d.onError(err)
            d.triggerReconnect()
            return
        }
        
        if response.IsFinal {
            d.onTranscript(response.Channel.Alternatives[0].Transcript)
        }
    }
}
```

### Integration with AudioProcessor

```go
// TODO: Update audio_processor.go
type StreamingAudioProcessor struct {
    deepgramStream *DeepgramStreamingClient
    fraudDetector  *FraudDetector
    // ...
}

func (sap *StreamingAudioProcessor) Start() error {
    // Connect to Deepgram WebSocket
    sap.deepgramStream.onTranscript = func(transcript string) {
        // Analyze immediately
        result := sap.fraudDetector.AnalyzeText(transcript)
        if result.IsAlert {
            sap.sendAlert(result)
        }
    }
    
    return sap.deepgramStream.Connect()
}

func (sap *StreamingAudioProcessor) AddAudioChunk(chunk []byte) error {
    // Stream directly to Deepgram (no buffering needed)
    return sap.deepgramStream.StreamAudio(chunk)
}
```

---

## 🎯 Recommendation for Hackathon

### ✅ Keep Current Implementation

**Why:**
1. **Stability**: HTTP API is battle-tested
2. **Simplicity**: Less code = fewer bugs
3. **Time**: Focus on features, not optimization
4. **Demo**: Latency is acceptable for demo

**What to do:**
- ✅ Use current HTTP implementation
- ✅ Focus on fraud detection accuracy
- ✅ Collect feedback on UX
- ✅ Document performance metrics

### 📝 Document for Future

Add TODO comments in code:
```go
// TODO: Optimize with WebSocket streaming for production
// Current HTTP API has ~500ms-2s latency
// WebSocket would reduce to ~100-300ms
// See DEEPGRAM_OPTIMIZATION.md for migration plan
```

---

## 📈 When to Optimize

**Optimize when:**
- ✅ After hackathon success
- ✅ User feedback indicates latency is an issue
- ✅ Scaling to production
- ✅ Processing >1000 calls/day

**Don't optimize if:**
- ❌ Still in MVP/demo phase
- ❌ No user complaints about speed
- ❌ Other features are higher priority
- ❌ Team bandwidth is limited

---

## 🔧 Quick Wins (No Major Refactor)

While keeping HTTP API, you can still optimize:

### 1. Connection Pooling
```go
// Reuse HTTP connections
var httpClient = &http.Client{
    Timeout: 30 * time.Second,
    Transport: &http.Transport{
        MaxIdleConns:        100,
        MaxIdleConnsPerHost: 10,
        IdleConnTimeout:     90 * time.Second,
    },
}
```

### 2. Concurrent Processing
```go
// Process multiple chunks in parallel
var wg sync.WaitGroup
for _, chunk := range audioChunks {
    wg.Add(1)
    go func(c []byte) {
        defer wg.Done()
        transcript, _ := deepgram.TranscribeAudio(c)
        // ...
    }(chunk)
}
wg.Wait()
```

### 3. Caching
```go
// Cache identical audio chunks
var transcriptCache = make(map[string]string)

func TranscribeWithCache(audioData []byte) (string, error) {
    hash := sha256.Sum256(audioData)
    key := hex.EncodeToString(hash[:])
    
    if cached, ok := transcriptCache[key]; ok {
        return cached, nil
    }
    
    transcript, err := TranscribeAudio(audioData)
    if err == nil {
        transcriptCache[key] = transcript
    }
    return transcript, err
}
```

---

## 📊 Monitoring Metrics

Track these metrics to decide when to optimize:

```go
type PerformanceMetrics struct {
    TotalRequests      int
    AverageLatency     time.Duration
    P95Latency         time.Duration
    P99Latency         time.Duration
    ErrorRate          float64
    TranscriptsPerSec  float64
}

// Log metrics
log.Printf("📊 Deepgram Performance: Avg=%dms, P95=%dms, Errors=%.2f%%",
    metrics.AverageLatency.Milliseconds(),
    metrics.P95Latency.Milliseconds(),
    metrics.ErrorRate * 100)
```

**Decision criteria:**
- If P95 > 3s → Consider WebSocket
- If error rate > 5% → Investigate issues
- If throughput < 10 req/s → Optimize

---

## ✅ Summary

**For Hackathon (Now):**
- ✅ Keep HTTP API
- ✅ Focus on features
- ✅ Document performance
- ✅ Collect user feedback

**For Production (Later):**
- 🔮 Migrate to WebSocket
- 🔮 Add connection pooling
- 🔮 Implement caching
- 🔮 Monitor metrics

**Remember:**
> "Premature optimization is the root of all evil" - Donald Knuth

Focus on making it **work** first, then make it **fast**! 🚀
