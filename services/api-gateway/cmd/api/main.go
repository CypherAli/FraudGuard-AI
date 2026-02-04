package main

import (
	"context"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/fraudguard/api-gateway/internal/db"
	"github.com/fraudguard/api-gateway/internal/handlers"
	"github.com/fraudguard/api-gateway/internal/hub"
	"github.com/fraudguard/api-gateway/internal/repository"
	"github.com/fraudguard/api-gateway/internal/services"
	"github.com/fraudguard/api-gateway/pkg/config"
	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
)

func main() {
	// Load configuration
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf(" Failed to load configuration: %v", err)
	}

	log.Println("🚀 Starting FraudGuard AI API Gateway...")
	log.Printf("📍 Environment: %s", os.Getenv("GO_ENV"))
	log.Printf("🌐 Host: %s", cfg.Server.Host)
	log.Printf("🔌 Port: %d", cfg.Server.Port)

	// Initialize PostgreSQL database connection (for blacklist)
	if err := db.Connect(&cfg.Database); err != nil {
		log.Fatalf(" Failed to connect to PostgreSQL database: %v", err)
	}
	defer db.Close()

	// Auto-migrate: Create tables and seed data
	if err := db.AutoMigrate(); err != nil {
		log.Fatalf(" Failed to run migrations: %v", err)
	}

	// Initialize SQLite database (for call history logs)
	if err := repository.InitSQLite(); err != nil {
		log.Printf(" Warning: SQLite initialization failed: %v", err)
		log.Println(" Call history logging will be disabled")
	}

	// Initialize AI clients
	if cfg.AI.DeepgramAPIKey != "" {
		services.GlobalDeepgramClient = services.NewDeepgramClient(cfg.AI.DeepgramAPIKey)
		log.Println(" Deepgram client initialized")
	} else {
		log.Println(" Deepgram API key not configured")
	}

	// TODO: Initialize Gemini client for advanced AI fraud detection
	// For now, using keyword-based detection (hard rules)
	if cfg.AI.GeminiAPIKey != "" {
		log.Println("ℹ Gemini API key configured (not yet integrated)")
	}

	// Create WebSocket hub
	wsHub := hub.NewHub()
	go wsHub.Run()
	log.Println(" WebSocket hub started")

	// Setup HTTP router
	r := chi.NewRouter()

	// Middleware
	r.Use(middleware.Logger)
	r.Use(middleware.Recoverer)
	r.Use(middleware.RequestID)
	r.Use(middleware.RealIP)
	r.Use(middleware.Timeout(60 * time.Second))

	// CORS middleware (allow all origins for development)
	r.Use(func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Access-Control-Allow-Origin", "*")
			w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
			if r.Method == "OPTIONS" {
				w.WriteHeader(http.StatusOK)
				return
			}
			next.ServeHTTP(w, r)
		})
	})

	// Routes
	r.Get("/health", handlers.HealthCheck)
	r.Get("/ws", func(w http.ResponseWriter, r *http.Request) {
		handlers.ServeWs(wsHub, w, r)
	})

	// API routes
	r.Route("/api", func(r chi.Router) {
		r.Get("/blacklist", handlers.GetBlacklist)
		r.Get("/check", handlers.CheckNumber)
		r.Get("/history", handlers.GetHistory) // NEW: Call history endpoint
	})

	// Welcome route
	r.Get("/", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		fmt.Fprintf(w, `{
			"service": "FraudGuard AI",
			"version": "1.0.0",
			"status": "running",
			"endpoints": {
				"health": "/health",
				"websocket": "/ws?device_id=YOUR_DEVICE_ID",
				"blacklist": "/api/blacklist",
				"check": "/api/check?phone=PHONE_NUMBER",
				"history": "/api/history?device_id=YOUR_DEVICE_ID&limit=20"
			}
		}`)
	})

	// Create HTTP server
	serverAddr := fmt.Sprintf("%s:%d", cfg.Server.Host, cfg.Server.Port)
	srv := &http.Server{
		Addr:         serverAddr,
		Handler:      r,
		ReadTimeout:  cfg.Server.ReadTimeout,
		WriteTimeout: cfg.Server.WriteTimeout,
	}

	// Start server in a goroutine
	go func() {
		log.Printf(" Server listening on %s", serverAddr)
		log.Printf(" WebSocket endpoint: ws://%s/ws?device_id=YOUR_DEVICE_ID", serverAddr)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf(" Server failed to start: %v", err)
		}
	}()

	// Graceful shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	log.Println("🛑 Shutting down server...")

	// Graceful shutdown WebSocket connections first
	wsHub.GracefulShutdown()

	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	if err := srv.Shutdown(ctx); err != nil {
		log.Printf("❌ Server forced to shutdown: %v", err)
	}

	log.Println("✅ Server stopped gracefully")
}
