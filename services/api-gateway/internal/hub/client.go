package hub

import (
	"encoding/json"
	"log"
	"sync"
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

	// Ensures send channel is closed exactly once (prevents double-close panic)
	sendOnce sync.Once
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

// closeSend safely closes the send channel exactly once
func (c *Client) closeSend() {
	c.sendOnce.Do(func() {
		close(c.send)
	})
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
		// Clean up detector from registry to prevent memory leak
		services.RemoveFraudDetector(c.deviceID)

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
	log.Printf("📨 [%s] ===== SENDING ALERT TO CLIENT =====", c.deviceID)
	log.Printf("📨 [%s] Alert: Type=%s, AlertType=%s, Confidence=%.2f",
		c.deviceID, alert.Type, alert.AlertType, alert.Confidence)

	alertJSON, err := json.Marshal(alert)
	if err != nil {
		log.Printf("❌ [%s] Error marshaling alert to JSON: %v", c.deviceID, err)
		return
	}

	log.Printf("📝 [%s] Alert JSON created (%d bytes): %s",
		c.deviceID, len(alertJSON), string(alertJSON))

	// Try to send to channel
	log.Printf("📤 [%s] Attempting to send to WebSocket channel (buffer: %d/%d)...",
		c.deviceID, len(c.send), cap(c.send))

	select {
	case c.send <- alertJSON:
		log.Printf("✅✅✅ [%s] Alert successfully queued to WebSocket channel", c.deviceID)
		log.Printf("📢 [%s] Alert message: %s", c.deviceID, alert.Message)
	default:
		log.Printf("❌❌❌ [%s] FAILED to send alert - WebSocket buffer FULL (%d/%d)",
			c.deviceID, len(c.send), cap(c.send))
		log.Printf("⚠️ [%s] Alert dropped due to full buffer. Consider increasing buffer size.", c.deviceID)
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

			// ✅ FIX: Send each alert as a SEPARATE WebSocket message
			// This prevents invalid JSON like {"alert1"}\n{"alert2"}
			if err := c.conn.WriteMessage(websocket.TextMessage, message); err != nil {
				return
			}

			// Send any additional queued messages as SEPARATE frames
			// Mobile app can now parse each message individually
		drainLoop:
			for {
				select {
				case queuedMsg := <-c.send:
					c.conn.SetWriteDeadline(time.Now().Add(writeWait))
					if err := c.conn.WriteMessage(websocket.TextMessage, queuedMsg); err != nil {
						return
					}
				default:
					// No more queued messages
					break drainLoop
				}
			}

		case <-ticker.C:
			c.conn.SetWriteDeadline(time.Now().Add(writeWait))
			if err := c.conn.WriteMessage(websocket.PingMessage, nil); err != nil {
				return
			}
		}
	}
}
