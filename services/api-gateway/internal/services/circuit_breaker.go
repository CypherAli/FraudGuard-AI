package services

import (
	"log"
	"sync"
	"time"
)

// CircuitBreaker implements the circuit breaker pattern for external API calls
// Prevents cascading failures when Deepgram API is unavailable
type CircuitBreaker struct {
	name         string
	failures     int
	threshold    int
	resetTimeout time.Duration
	lastFailure  time.Time
	state        CircuitState
	mu           sync.RWMutex
}

type CircuitState int

const (
	StateClosed   CircuitState = iota // Normal operation
	StateOpen                         // Rejecting requests
	StateHalfOpen                     // Testing if service recovered
)

// NewCircuitBreaker creates a new circuit breaker
// threshold: number of failures before opening circuit
// resetTimeout: how long to wait before trying again
func NewCircuitBreaker(name string, threshold int, resetTimeout time.Duration) *CircuitBreaker {
	return &CircuitBreaker{
		name:         name,
		threshold:    threshold,
		resetTimeout: resetTimeout,
		state:        StateClosed,
	}
}

// Allow checks if a request should be allowed through
func (cb *CircuitBreaker) Allow() bool {
	cb.mu.Lock()
	defer cb.mu.Unlock()

	switch cb.state {
	case StateClosed:
		return true

	case StateOpen:
		// Check if reset timeout has passed
		if time.Since(cb.lastFailure) > cb.resetTimeout {
			cb.state = StateHalfOpen
			log.Printf("🔄 [CircuitBreaker:%s] Transitioning to HALF-OPEN state", cb.name)
			return true
		}
		return false

	case StateHalfOpen:
		// Allow one request to test
		return true
	}

	return false
}

// RecordSuccess records a successful request
func (cb *CircuitBreaker) RecordSuccess() {
	cb.mu.Lock()
	defer cb.mu.Unlock()

	if cb.state == StateHalfOpen {
		log.Printf("✅ [CircuitBreaker:%s] Service recovered, closing circuit", cb.name)
	}

	cb.failures = 0
	cb.state = StateClosed
}

// RecordFailure records a failed request
func (cb *CircuitBreaker) RecordFailure() {
	cb.mu.Lock()
	defer cb.mu.Unlock()

	cb.failures++
	cb.lastFailure = time.Now()

	if cb.state == StateHalfOpen {
		// Failed during half-open, go back to open
		cb.state = StateOpen
		log.Printf("❌ [CircuitBreaker:%s] Test request failed, reopening circuit", cb.name)
		return
	}

	if cb.failures >= cb.threshold {
		cb.state = StateOpen
		log.Printf("🔴 [CircuitBreaker:%s] Threshold reached (%d failures), opening circuit for %v",
			cb.name, cb.threshold, cb.resetTimeout)
	}
}

// GetState returns the current state of the circuit breaker
func (cb *CircuitBreaker) GetState() string {
	cb.mu.RLock()
	defer cb.mu.RUnlock()

	switch cb.state {
	case StateClosed:
		return "CLOSED"
	case StateOpen:
		return "OPEN"
	case StateHalfOpen:
		return "HALF-OPEN"
	default:
		return "UNKNOWN"
	}
}

// IsOpen returns true if the circuit is open (not accepting requests)
func (cb *CircuitBreaker) IsOpen() bool {
	cb.mu.RLock()
	defer cb.mu.RUnlock()
	return cb.state == StateOpen
}

// Global circuit breaker for Deepgram API
var DeepgramCircuitBreaker = NewCircuitBreaker("Deepgram", 5, 30*time.Second)
