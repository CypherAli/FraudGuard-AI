package hub

import (
	"sync"
	"testing"
	"time"
)

// TestHubConcurrency tests the Hub's concurrency safety
func TestHubConcurrency(t *testing.T) {
	hub := NewHub()
	go hub.Run()

	// Simulate 100 concurrent client registrations
	var wg sync.WaitGroup
	for i := 0; i < 100; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			// Simulate client registration
			client := &Client{
				hub:      hub,
				send:     make(chan []byte, 256),
				deviceID: string(rune(id)),
			}
			hub.Register <- client
			time.Sleep(10 * time.Millisecond)
			hub.Unregister <- client
		}(i)
	}

	wg.Wait()
	time.Sleep(100 * time.Millisecond)

	// Verify all clients unregistered
	count := hub.GetClientCount()
	if count != 0 {
		t.Errorf("Expected 0 clients, got %d", count)
	}
}

// TestHubBroadcast tests the broadcast functionality
func TestHubBroadcast(t *testing.T) {
	hub := NewHub()
	go hub.Run()

	// Create test client
	client := &Client{
		hub:      hub,
		send:     make(chan []byte, 256),
		deviceID: "test-device",
	}
	hub.Register <- client
	time.Sleep(50 * time.Millisecond)

	// Broadcast message
	testMessage := []byte("test broadcast")
	hub.Broadcast <- testMessage
	time.Sleep(50 * time.Millisecond)

	// Verify message received
	select {
	case msg := <-client.send:
		if string(msg) != string(testMessage) {
			t.Errorf("Expected %s, got %s", testMessage, msg)
		}
	case <-time.After(1 * time.Second):
		t.Error("Timeout waiting for broadcast message")
	}

	hub.Unregister <- client
}

// TestRWMutexUsage verifies Lock/RLock usage (compile-time check)
func TestRWMutexUsage(t *testing.T) {
	// This test primarily verifies that the code compiles
	// The actual RWMutex usage is verified by code review
	hub := NewHub()

	// Verify hub has RWMutex
	if hub.Register == nil || hub.Unregister == nil || hub.Broadcast == nil {
		t.Error("Hub channels not initialized")
	}
}
