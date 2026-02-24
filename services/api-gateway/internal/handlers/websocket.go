package handlers

import (
	"log"
	"net/http"
	"os"
	"strings"

	"github.com/fraudguard/api-gateway/internal/hub"
	"github.com/gorilla/websocket"
)

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

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	CheckOrigin:     checkWebSocketOrigin,
}

// ServeWs handles websocket requests from the peer
func ServeWs(h *hub.Hub, w http.ResponseWriter, r *http.Request) {
	// Validate bearer token — accept from Authorization header or ?token= query param
	// (WebSocket clients cannot set custom headers in some implementations)
	token := strings.TrimPrefix(r.Header.Get("Authorization"), "Bearer ")
	if token == "" {
		token = r.URL.Query().Get("token")
	}
	if !ValidateToken(token) {
		http.Error(w, `{"success":false,"error":"Unauthorized — invalid or missing token"}`, http.StatusUnauthorized)
		log.Println("⛔ WebSocket rejected: invalid token")
		return
	}

	// Extract device_id from query parameters
	deviceID := r.URL.Query().Get("device_id")
	if deviceID == "" {
		http.Error(w, "device_id is required", http.StatusBadRequest)
		log.Println("⛔ WebSocket connection rejected: missing device_id")
		return
	}

	// Upgrade HTTP connection to WebSocket
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf(" WebSocket upgrade failed: %v", err)
		return
	}

	// Create new client
	client := hub.NewClient(h, conn, deviceID)
	h.Register <- client

	// Start client's read and write pumps in separate goroutines
	// These will run until the connection is closed
	go client.WritePump()
	go client.ReadPump()

	log.Printf(" WebSocket connection established for device: %s", deviceID)
}
