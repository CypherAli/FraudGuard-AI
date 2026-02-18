package hub

import (
	"context"
	"log"
	"sync"
)

// Hub maintains the set of active clients and broadcasts messages to the clients
// IMPORTANT: This Hub uses broadcast ONLY for server-wide notifications,
// NOT for audio streams (which are processed privately per client)
type Hub struct {
	// Registered clients (protected by mutex)
	clients map[*Client]bool

	// Inbound messages from the clients (for server-wide broadcast only)
	Broadcast chan []byte

	// Register requests from the clients
	Register chan *Client

	// Unregister requests from clients
	Unregister chan *Client

	// Mutex to protect the clients map
	mu sync.RWMutex

	// Context for graceful shutdown of Run loop
	cancel context.CancelFunc
}

// NewHub creates a new Hub instance
func NewHub() *Hub {
	return &Hub{
		clients:    make(map[*Client]bool),
		Broadcast:  make(chan []byte, 256),
		Register:   make(chan *Client),
		Unregister: make(chan *Client),
	}
}

// Run starts the hub's main loop with context for graceful shutdown.
// This method should be run in a separate goroutine.
func (h *Hub) Run(ctx context.Context) {
	childCtx, cancel := context.WithCancel(ctx)
	h.cancel = cancel
	defer cancel()

	for {
		select {
		case <-childCtx.Done():
			log.Println("Hub Run loop stopped (context cancelled)")
			return

		case client := <-h.Register:
			h.mu.Lock()
			h.clients[client] = true
			log.Printf(" Client registered: %s (Total: %d)", client.deviceID, len(h.clients))
			h.mu.Unlock()

		case client := <-h.Unregister:
			h.mu.Lock()
			if _, ok := h.clients[client]; ok {
				delete(h.clients, client)
				client.closeSend() // safe close via sync.Once
				log.Printf(" Client unregistered: %s (Total: %d)", client.deviceID, len(h.clients))
			}
			h.mu.Unlock()

		case message := <-h.Broadcast:
			var failedClients []*Client

			h.mu.RLock()
			for client := range h.clients {
				select {
				case client.send <- message:
				default:
					failedClients = append(failedClients, client)
				}
			}
			h.mu.RUnlock()

			if len(failedClients) > 0 {
				h.mu.Lock()
				for _, client := range failedClients {
					if _, ok := h.clients[client]; ok {
						client.closeSend() // safe close via sync.Once
						delete(h.clients, client)
						log.Printf(" Client send buffer full, closing: %s", client.deviceID)
					}
				}
				h.mu.Unlock()
			}
		}
	}
}

// GetClientCount returns the current number of connected clients (thread-safe)
func (h *Hub) GetClientCount() int {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return len(h.clients)
}

// GracefulShutdown closes all client connections gracefully.
// Stops the Run loop, then closes all client send channels and connections.
func (h *Hub) GracefulShutdown() {
	// Stop Run loop first so it doesn't race with us
	if h.cancel != nil {
		h.cancel()
	}

	h.mu.Lock()
	defer h.mu.Unlock()

	clientCount := len(h.clients)
	if clientCount == 0 {
		log.Println("🛑 No active WebSocket connections to close")
		return
	}

	log.Printf("🛑 Graceful shutdown: Closing %d WebSocket connections...", clientCount)

	for client := range h.clients {
		// closeSend will cause WritePump to exit and send CloseMessage itself
		client.closeSend()
		delete(h.clients, client)
	}

	log.Println("✅ All WebSocket connections closed gracefully")
}
