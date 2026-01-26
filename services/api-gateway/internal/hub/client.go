package hub

import (
	"encoding/json"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/fraudguard/api-gateway/internal/services"
	"github.com/gorilla/websocket"
)

const (
	// Time allowed to write a message to the peer
	writeWait = 10 * time.Second

	// Time allowed to read the next pong message from the peer
	pongWait = 60 * time.Second

	// Send pings to peer with this period (must be less than pongWait)
	pingPeriod = (pongWait * 9) / 10

	// Maximum message size allowed from peer
	maxMessageSize = 512 * 1024 // 512 KB (for audio chunks)
)

// Client is a middleman between the websocket connection and the hub
type Client struct {
	hub *Hub

	// The websocket connection
	conn *websocket.Conn

	// Buffered channel of outbound messages
	send chan []byte

	// Device ID for this client
	deviceID string
}

// NewClient creates a new Client instance
func NewClient(hub *Hub, conn *websocket.Conn, deviceID string) *Client {
	return &Client{
		hub:      hub,
		conn:     conn,
		send:     make(chan []byte, 256),
		deviceID: deviceID,
	}
}

// ReadPump pumps messages from the websocket connection to the hub
// CRITICAL: This does NOT broadcast audio to all clients!
// Audio is processed PRIVATELY for each client
func (c *Client) ReadPump() {
	defer func() {
		// Save call log to database before unregistering
		// This ensures we capture the session data even if connection drops
		if detector := services.GetFraudDetector(c.deviceID); detector != nil {
			detector.EndSession()
		}

		c.hub.Unregister <- c
		c.conn.Close()
	}()

	c.conn.SetReadDeadline(time.Now().Add(pongWait))
	c.conn.SetReadLimit(maxMessageSize)
	c.conn.SetPongHandler(func(string) error {
		c.conn.SetReadDeadline(time.Now().Add(pongWait))
		return nil
	})

	for {
		messageType, message, err := c.conn.ReadMessage()
		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				log.Printf("WebSocket error: %v", err)
			}
			break
		}

		// Handle different message types
		switch messageType {
		case websocket.BinaryMessage:
			// ✅ CORRECT: Process audio PRIVATELY for this client only
			// DO NOT broadcast to other clients!
			go services.ProcessAudioStream(c.deviceID, message, c.sendAlert)

		case websocket.TextMessage:
			// Handle JSON commands (e.g., report fraud)
			c.handleTextMessage(message)

		default:
			log.Printf("Unknown message type: %d", messageType)
		}
	}
}

// handleTextMessage processes JSON commands from the client
func (c *Client) handleTextMessage(message []byte) {
	var report models.ReportRequest
	if err := json.Unmarshal(message, &report); err != nil {
		log.Printf("Error parsing JSON message: %v", err)
		return
	}

	// Process the report (add to blacklist, etc.)
	go services.ProcessFraudReport(report)
}

// sendAlert sends an alert message to this specific client
func (c *Client) sendAlert(alert models.AlertMessage) {
	alertJSON, err := json.Marshal(alert)
	if err != nil {
		log.Printf("Error marshaling alert: %v", err)
		return
	}

	select {
	case c.send <- alertJSON:
		log.Printf("📢 Alert sent to client %s: %s", c.deviceID, alert.Message)
	default:
		log.Printf("⚠️ Failed to send alert to client %s (buffer full)", c.deviceID)
	}
}

// WritePump pumps messages from the hub to the websocket connection
func (c *Client) WritePump() {
	ticker := time.NewTicker(pingPeriod)
	defer func() {
		ticker.Stop()
		c.conn.Close()
	}()

	for {
		select {
		case message, ok := <-c.send:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if !ok {
				// The hub closed the channel
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
				return
			}

			w, err := c.conn.NextWriter(websocket.TextMessage)
			if err != nil {
				return
			}
			w.Write(message)

			// Add queued messages to the current websocket message
			n := len(c.send)
			for i := 0; i < n; i++ {
				w.Write([]byte{'\n'})
				w.Write(<-c.send)
			}

			if err := w.Close(); err != nil {
				return
			}

		case <-ticker.C:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if err := c.conn.WriteMessage(websocket.PingMessage, nil); err != nil {
				return
			}
		}
	}
}
