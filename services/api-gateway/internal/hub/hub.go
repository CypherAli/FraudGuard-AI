package hub

import (
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
	// Using RWMutex for better performance:
	// - Lock() for write operations (register/unregister)
	// - RLock() for read operations (broadcast)
	mu sync.RWMutex
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

// Run starts the hub's main loop
// This method should be run in a separate goroutine
func (h *Hub) Run() {
	for {
		select {
		case client := <-h.Register:
			// WRITE OPERATION: Use Lock() to modify the map
			h.mu.Lock()
			h.clients[client] = true
			log.Printf(" Client registered: %s (Total: %d)", client.deviceID, len(h.clients))
			h.mu.Unlock()

		case client := <-h.Unregister:
			// WRITE OPERATION: Use Lock() to modify the map
			h.mu.Lock()
			if _, ok := h.clients[client]; ok {
				delete(h.clients, client)
				close(client.send)
				log.Printf(" Client unregistered: %s (Total: %d)", client.deviceID, len(h.clients))
			}
			h.mu.Unlock()

		case message := <-h.Broadcast:
			// READ OPERATION: Use RLock() to iterate over the map
			// This allows multiple concurrent reads while preventing writes
			h.mu.RLock()
			for client := range h.clients {
				select {
				case client.send <- message:
					// Message sent successfully
				default:
					// Client's send buffer is full, close the connection
					close(client.send)
					delete(h.clients, client)
					log.Printf(" Client send buffer full, closing: %s", client.deviceID)
				}
			}
			h.mu.RUnlock()
		}
	}
}

// GetClientCount returns the current number of connected clients (thread-safe)
func (h *Hub) GetClientCount() int {
	h.mu.RLock()
	defer h.mu.RUnlock()
	return len(h.clients)
}
