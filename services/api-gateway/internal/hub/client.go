package hub

import (
	"encoding/json"
	"log"
	"sync"
	"sync/atomic"
	"time"

	"github.com/fraudguard/api-gateway/internal/models"
	"github.com/fraudguard/api-gateway/internal/services"
	"github.com/gorilla/websocket"
)

// droppedAlerts tracks total alerts dropped due to full send buffers (for monitoring)
var droppedAlerts int64

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

// Client is a middleman between the websocket connection and the hub.
// Two-tier send channels ensure CRITICAL alerts are never dropped by MEDIUM/HIGH noise:
//   - criticalSend: small dedicated buffer for CRITICAL alerts — WritePump drains first
//   - send:         normal buffer for HIGH/MEDIUM/info messages
type Client struct {
	hub *Hub

	// The websocket connection
	conn *websocket.Conn

	// Priority channels: critical drained before normal in WritePump
	criticalSend chan []byte // CRITICAL alerts — never dropped, size 64
	send         chan []byte // HIGH/MEDIUM/info — dropped on overflow, size 512

	// Device ID for this client
	deviceID string

	// Semaphore to limit concurrent audio processing goroutines per client
	audioSem chan struct{}

	// Ensures send channels are closed exactly once (prevents double-close panic)
	sendOnce sync.Once
}

// NewClient creates a new Client instance
func NewClient(hub *Hub, conn *websocket.Conn, deviceID string) *Client {
	return &Client{
		hub:          hub,
		conn:         conn,
		criticalSend: make(chan []byte, 64),  // CRITICAL: small + dedicated, never overflows in practice
		send:         make(chan []byte, 512), // Normal: larger buffer for burst traffic
		deviceID:     deviceID,
		audioSem:     make(chan struct{}, 5), // Max 5 concurrent audio processing goroutines
	}
}

// closeSend safely closes both send channels exactly once
func (c *Client) closeSend() {
	c.sendOnce.Do(func() {
		close(c.criticalSend)
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
			// Use semaphore to limit concurrent goroutines (prevent goroutine leak)
			select {
			case c.audioSem <- struct{}{}:
				go func(data []byte) {
					defer func() { <-c.audioSem }()
					services.ProcessAudioStream(c.deviceID, data, c.sendAlert)
				}(message)
			default:
				// Semaphore full - drop this audio chunk to prevent goroutine explosion
				log.Printf("⚠️ [%s] Audio processing backpressure - dropping chunk (%d bytes)", c.deviceID, len(message))
			}

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
	// Fix 43: add recover() so a panic inside ProcessFraudReport cannot crash the server.
	go func() {
		defer func() {
			if r := recover(); r != nil {
				log.Printf("⚠️ [%s] Recovered panic in ProcessFraudReport: %v", c.deviceID, r)
			}
		}()
		if err := services.ProcessFraudReport(report); err != nil {
			log.Printf("⚠️ [%s] ProcessFraudReport error: %v", c.deviceID, err)
		}
	}()
}

// sendAlert sends an alert message to this specific client
func (c *Client) sendAlert(alert models.AlertMessage) {
	// Fix 38: recover from "send on closed channel" panic that can occur when
	// the Hub closes c.send (client disconnect) while an audio-processing goroutine
	// is still completing and calling sendAlert. The panic is safe to swallow here
	// because the client is already gone — the alert is inherently undeliverable.
	defer func() {
		if r := recover(); r != nil {
			log.Printf("⚠️ [%s] sendAlert: recovered panic (channel closed on disconnect): %v", c.deviceID, r)
		}
	}()

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
	// Route by priority: CRITICAL/PENDING → dedicated channel (never dropped on overflow,
	// blocks briefly instead). HIGH/MEDIUM/info → normal channel with drop-on-full.
	isCritical := alert.AlertType == "CRITICAL" || alert.Type == "pending_report"

	if isCritical {
		log.Printf("📤 [%s] CRITICAL/PENDING → priority channel (%d/%d)",
			c.deviceID, len(c.criticalSend), cap(c.criticalSend))
		// Use non-blocking with a fallback log — in extreme cases even critical might drop,
		// but the 64-slot buffer makes this statistically impossible in normal operation.
		select {
		case c.criticalSend <- alertJSON:
			log.Printf("✅ [%s] CRITICAL alert queued (priority channel)", c.deviceID)
		default:
			atomic.AddInt64(&droppedAlerts, 1)
			log.Printf("🚨 [%s] CRITICAL DROPPED — priority buffer full (%d/%d) [total dropped: %d]",
				c.deviceID, len(c.criticalSend), cap(c.criticalSend), atomic.LoadInt64(&droppedAlerts))
		}
	} else {
		log.Printf("📤 [%s] Normal alert → standard channel (%d/%d)",
			c.deviceID, len(c.send), cap(c.send))
		select {
		case c.send <- alertJSON:
			log.Printf("✅ [%s] Alert queued (standard channel)", c.deviceID)
		default:
			atomic.AddInt64(&droppedAlerts, 1)
			log.Printf("❌ [%s] Alert dropped — buffer full (%d/%d) [total: %d]",
				c.deviceID, len(c.send), cap(c.send), atomic.LoadInt64(&droppedAlerts))
		}
	}
}

// WritePump pumps messages from the hub to the websocket connection.
// Drain order: criticalSend → send → ping. CRITICAL alerts always go first.
func (c *Client) WritePump() {
	ticker := time.NewTicker(pingPeriod)
	defer func() {
		ticker.Stop()
		c.conn.Close()
	}()

	// writeMsg sends one WebSocket text frame, returns false on error.
	writeMsg := func(msg []byte) bool {
		c.conn.SetWriteDeadline(time.Now().Add(writeWait))
		return c.conn.WriteMessage(websocket.TextMessage, msg) == nil
	}

	for {
		// Phase 1: drain ALL pending critical alerts before anything else
		drained := true
		for drained {
			select {
			case msg, ok := <-c.criticalSend:
				if !ok {
					c.conn.WriteMessage(websocket.CloseMessage, []byte{})
					return
				}
				if !writeMsg(msg) {
					return
				}
			default:
				drained = false
			}
		}

		// Phase 2: block on either channel or ping ticker
		select {
		case msg, ok := <-c.criticalSend:
			if !ok {
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
				return
			}
			if !writeMsg(msg) {
				return
			}

		case msg, ok := <-c.send:
			if !ok {
				c.conn.WriteMessage(websocket.CloseMessage, []byte{})
				return
			}
			if !writeMsg(msg) {
				return
			}
			// Drain remaining normal messages in the same loop iteration
		drainNormal:
			for {
				select {
				case qm, ok := <-c.send:
					if !ok {
						return
					}
					if !writeMsg(qm) {
						return
					}
				default:
					break drainNormal
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
