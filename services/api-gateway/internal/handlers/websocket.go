package handlers

import (
	"log"
	"net/http"

	"github.com/fraudguard/api-gateway/internal/hub"
	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	// Allow all origins for development (restrict in production!)
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

// ServeWs handles websocket requests from the peer
func ServeWs(h *hub.Hub, w http.ResponseWriter, r *http.Request) {
	// Extract device_id from query parameters
	deviceID := r.URL.Query().Get("device_id")
	if deviceID == "" {
		http.Error(w, "device_id is required", http.StatusBadRequest)
		log.Println("❌ WebSocket connection rejected: missing device_id")
		return
	}

	// Upgrade HTTP connection to WebSocket
	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Printf("❌ WebSocket upgrade failed: %v", err)
		return
	}

	// Create new client
	client := hub.NewClient(h, conn, deviceID)
	h.Register <- client

	// Start client's read and write pumps in separate goroutines
	// These will run until the connection is closed
	go client.WritePump()
	go client.ReadPump()

	log.Printf("✅ WebSocket connection established for device: %s", deviceID)
}
