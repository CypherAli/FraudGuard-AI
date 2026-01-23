# FraudGuard AI - API Gateway

## Overview
Real-time fraud detection system backend built with Go, PostgreSQL, and WebSocket.

## Architecture
- **Clean Architecture** following Standard Go Project Layout
- **WebSocket Hub** with concurrency-safe client management (sync.RWMutex)
- **Stream Processing** - Audio processed privately per client (NOT broadcast)
- **PostgreSQL 16** with JSONB for flexible AI metadata storage

## Tech Stack
- Go 1.22+
- PostgreSQL 16
- WebSocket (gorilla/websocket)
- pgx/v5 (PostgreSQL driver)
- chi/v5 (HTTP router)

## Quick Start

### 1. Setup Environment
```bash
cd services/api-gateway
cp .env.example .env
# Edit .env with your configuration
```

### 2. Start Database
```bash
docker-compose up -d
```

Wait for PostgreSQL to be ready (check with `docker-compose logs -f postgres`)

### 3. Install Dependencies
```bash
go mod download
```

### 4. Run Application
```bash
go run cmd/api/main.go
```

Server will start on `http://localhost:8080`

## API Endpoints

### REST API
- `GET /health` - Health check
- `GET /api/blacklist` - Get all blacklisted numbers
- `GET /api/check?phone=+84123456789` - Check if number is blacklisted

### WebSocket
- `ws://localhost:8080/ws?device_id=YOUR_DEVICE_ID`

**Message Types:**
- **Binary** - Audio chunks (processed privately per client)
- **Text (JSON)** - Commands (e.g., fraud reports)

**Alert Format (Server → Client):**
```json
{
  "risk_score": 85,
  "message": "⚠️ Potential fraud detected!",
  "action": "SHOW_WARNING",
  "timestamp": 1706000000
}
```

**Report Format (Client → Server):**
```json
{
  "phone_number": "+84123456789",
  "reason": "Scam call asking for bank details",
  "device_id": "test-device-001"
}
```

## Database Schema

### Tables
- `users` - Device registrations
- `blacklists` - Reported fraudulent numbers with risk levels
- `call_logs` - Call records with AI analysis (JSONB metadata)

### Risk Levels
- `LOW` - 1 report
- `MEDIUM` - 2-4 reports
- `HIGH` - 5-9 reports
- `CRITICAL` - 10+ reports

## Development

### Run with Race Detector
```bash
go run -race cmd/api/main.go
```

### Build for Production
```bash
go build -o bin/fraudguard-api cmd/api/main.go
```

## Critical Implementation Notes

### ⚠️ Concurrency Safety
The WebSocket hub uses `sync.RWMutex`:
- `Lock()` for register/unregister (write operations)
- `RLock()` for broadcast (read operations)

### ⚠️ Stream Processing vs Broadcast
**DO NOT broadcast audio streams!** Each client's audio is processed privately:
- ✅ CORRECT: `go services.ProcessAudioStream(client, data)`
- ❌ WRONG: `hub.broadcast <- audioData` (privacy violation!)

The `broadcast` channel is ONLY for server-wide notifications (e.g., maintenance alerts).

## TODO
- [ ] Integrate Deepgram API for real-time transcription
- [ ] Integrate OpenAI for semantic fraud analysis
- [ ] Add Vector DB (Pinecone/Weaviate) for pattern matching
- [ ] Implement user authentication
- [ ] Add rate limiting
- [ ] Setup monitoring and logging
- [ ] Write unit tests
- [ ] Add Docker build for production

## License
Proprietary - FraudGuard AI
