package handlers

import (
	"log"
	"net/http"
	"os"
	"regexp"
	"strings"
	"sync"

	"github.com/fraudguard/api-gateway/internal/hub"
	"github.com/gorilla/websocket"
)

// deviceIDRegexp enforces safe device_id format: alphanumeric + hyphens/underscores, 8-64 chars.
// Prevents log injection, path traversal, and ensures per-device map keys are bounded.
var deviceIDRegexp = regexp.MustCompile(`^[a-zA-Z0-9_-]{8,64}$`)

// checkWebSocketOrigin validates the WebSocket origin against CORS_ALLOWED_ORIGIN env var.
// If CORS_ALLOWED_ORIGIN is "*" or unset, all origins are allowed (development mode).
// In production, set CORS_ALLOWED_ORIGIN to the exact allowed origin.
func checkWebSocketOrigin(r *http.Request) bool {
	allowedOrigin := os.Getenv("CORS_ALLOWED_ORIGIN")
	if allowedOrigin == "" || allowedOrigin == "*" {
		return true // Development: allow all
	}
	origin := r.Header.Get("Origin")
	if origin == "" {
		return true // No origin header: direct connection (non-browser)
	}
	// Support comma-separated list of allowed origins
	for _, allowed := range strings.Split(allowedOrigin, ",") {
		if strings.EqualFold(strings.TrimSpace(allowed), origin) {
			return true
		}
	}
	log.Printf("⚠️  WebSocket origin rejected: %s (allowed: %s)", origin, allowedOrigin)
	return false
}

// maxConnectionsPerDevice limits how many concurrent WebSocket connections a
// single device_id may maintain.  A legitimate mobile client needs at most one
// connection (two channels are multiplexed inside that connection via the
// 0x00/0x01 channel prefix).  Cap at 3 to tolerate brief reconnect races.
const maxConnectionsPerDevice = 3

var (
	deviceConnCount = make(map[string]int)
	deviceConnMutex sync.Mutex
)

// incrementDeviceConnections atomically increments and returns the new count.
func incrementDeviceConnections(deviceID string) int {
	deviceConnMutex.Lock()
	defer deviceConnMutex.Unlock()
	deviceConnCount[deviceID]++
	return deviceConnCount[deviceID]
}

// decrementDeviceConnections atomically decrements the count.
// Deletes the map entry when the count reaches zero to prevent unbounded growth.
func decrementDeviceConnections(deviceID string) {
	deviceConnMutex.Lock()
	defer deviceConnMutex.Unlock()
	if deviceConnCount[deviceID] > 0 {
		deviceConnCount[deviceID]--
	}
	if deviceConnCount[deviceID] == 0 {
		delete(deviceConnCount, deviceID)
	}
}

var upgrader = websocket.Upgrader{
	ReadBufferSize:  65536, // 64 KB — audio chunks are 8 KB, need headroom for framing overhead
	WriteBufferSize: 65536, // 64 KB — alerts can be large JSON; avoid fragmented writes
	CheckOrigin:     checkWebSocketOrigin,
}

// ServeWs handles websocket requests from the peer
func ServeWs(h *hub.Hub, w http.ResponseWriter, r *http.Request) {
	// Extract device_id first — needed for token-device binding check below
	deviceID := r.URL.Query().Get("device_id")
	if deviceID == "" {
		http.Error(w, `{"success":false,"error":"device_id is required"}`, http.StatusBadRequest)
		log.Println("⛔ WebSocket rejected: missing device_id")
		return
	}
	// Validate format: prevent log injection, path traversal, unbounded map keys
	if !deviceIDRegexp.MatchString(deviceID) {
		http.Error(w, `{"success":false,"error":"invalid device_id format (8-64 alphanumeric/_/-)"}`, http.StatusBadRequest)
		log.Printf("⛔ WebSocket rejected: invalid device_id format: %q", deviceID[:min(len(deviceID), 16)])
		return
	}

	// Validate bearer token AND enforce token-device binding
	// Accept from Authorization header or ?token= query param
	// (WebSocket clients cannot set custom headers in some implementations)
	token := strings.TrimPrefix(r.Header.Get("Authorization"), "Bearer ")
	if token == "" {
		token = r.URL.Query().Get("token")
	}
	if !ValidateTokenForDevice(token, deviceID) {
		http.Error(w, `{"success":false,"error":"Unauthorized — invalid token or device mismatch"}`, http.StatusUnauthorized)
		log.Printf("⛔ WebSocket rejected: invalid token or device mismatch for device=%s", deviceID)
		return
	}

	// Per-device connection limit — reject before upgrade to save resources.
	// Snapshot the count from the atomic increment so we never read the map without the lock.
	activeCount := incrementDeviceConnections(deviceID)
	if activeCount > maxConnectionsPerDevice {
		decrementDeviceConnections(deviceID)
		http.Error(w, `{"success":false,"error":"Too many concurrent connections for this device"}`,
			http.StatusTooManyRequests)
		log.Printf("⛔ [%s] Connection rejected: per-device limit %d exceeded (active=%d)",
			deviceID, maxConnectionsPerDevice, activeCount)
		return
	}

	// Upgrade HTTP connection to WebSocket
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		decrementDeviceConnections(deviceID) // upgrade failed — release the slot
		log.Printf("WebSocket upgrade failed: %v", err)
		return
	}

	// Create new client
	client := hub.NewClient(h, conn, deviceID)
	h.Register <- client

	// Start write pump — runs until the connection is closed
	go client.WritePump()

	// Start read pump — decrement the per-device counter when it exits
	// (ReadPump is the authoritative "connection alive" goroutine)
	go func() {
		defer decrementDeviceConnections(deviceID)
		client.ReadPump()
	}()

	log.Printf("✅ WebSocket connection established for device: %s (active=%d)",
		deviceID, activeCount)
}
