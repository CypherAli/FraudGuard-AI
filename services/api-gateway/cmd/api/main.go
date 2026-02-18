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
	"github.com/fraudguard/api-gateway/internal/metrics"
	customMiddleware "github.com/fraudguard/api-gateway/internal/middleware"
	"github.com/fraudguard/api-gateway/internal/repository"
	"github.com/fraudguard/api-gateway/internal/services"
	"github.com/fraudguard/api-gateway/pkg/cache"
	"github.com/fraudguard/api-gateway/pkg/config"
	"github.com/go-chi/chi/v5"
	"github.com/go-chi/chi/v5/middleware"
)

func main() {
	// Load configuration
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("❌ Failed to load configuration: %v", err)
	}

	// Validate configuration (logs warnings for missing optional values,
	// returns an error only for settings that would cause a runtime failure)
	if err := cfg.Validate(); err != nil {
		log.Fatalf("❌ Invalid configuration: %v", err)
	}

	log.Println("🚀 Starting FraudGuard AI API Gateway...")
	log.Printf("📍 Environment: %s", os.Getenv("GO_ENV"))
	log.Printf("🌐 Host: %s", cfg.Server.Host)
	log.Printf("🔌 Port: %d", cfg.Server.Port)

	// Initialize PostgreSQL database connection (optional - blacklist features)
	if err := db.Connect(&cfg.Database); err != nil {
		log.Printf("⚠️  Warning: PostgreSQL unavailable: %v", err)
		log.Println("⚠️  Blacklist and database features will be disabled")
	} else {
		defer db.Close()
		if err := db.AutoMigrate(); err != nil {
			log.Printf("⚠️  Warning: Failed to run migrations: %v", err)
		}
	}

	// Initialize SQLite database (for call history logs)
	if err := repository.InitSQLite(); err != nil {
		log.Printf("⚠️  Warning: SQLite initialization failed: %v", err)
		log.Println("⚠️  Call history logging will be disabled")
	}

	// Initialize AI clients
	if cfg.AI.DeepgramAPIKey != "" {
		services.GlobalDeepgramClient = services.NewDeepgramClient(cfg.AI.DeepgramAPIKey)
		log.Println("✅ Deepgram client initialized")
	} else {
		log.Println("⚠️  Deepgram API key not configured")
	}

	// Initialize Gemini client for contextual AI fraud detection
	if cfg.AI.GeminiAPIKey != "" {
		services.GlobalGeminiClient = services.NewGeminiClient(cfg.AI.GeminiAPIKey)
		log.Println("🤖 Gemini AI client initialized - contextual fraud analysis ACTIVE")
	} else {
		log.Println("⚠️  Gemini API key not configured - using keyword-only detection")
	}

	// Initialize cache (in-memory, Redis-compatible interface)
	if err := cache.Init(cfg.Redis.URL); err != nil {
		log.Printf("⚠️  Cache initialization failed: %v", err)
	} else {
		defer cache.Close()
		log.Println("📦 Cache system initialized")
	}

	// Log metrics status
	if metrics.IsEnabled() {
		log.Println("📊 Prometheus metrics enabled (FEATURE_METRICS=true)")
	}

	// Create WebSocket hub with cancellable context
	wsHub := hub.NewHub()
	hubCtx, hubCancel := context.WithCancel(context.Background())
	defer hubCancel()
	go wsHub.Run(hubCtx)
	log.Println("✅ WebSocket hub started")

	// Setup HTTP router
	r := chi.NewRouter()

	// Middleware
	r.Use(middleware.Logger)
	r.Use(middleware.Recoverer)
	r.Use(middleware.RequestID)
	r.Use(middleware.RealIP)
	r.Use(middleware.Timeout(60 * time.Second))

	// CORS middleware
	allowedOrigin := os.Getenv("CORS_ALLOWED_ORIGIN")
	if allowedOrigin == "" {
		allowedOrigin = "*" // Default to * for development
	}
	r.Use(func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			w.Header().Set("Access-Control-Allow-Origin", allowedOrigin)
			w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS")
			w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
			if r.Method == "OPTIONS" {
				w.WriteHeader(http.StatusOK)
				return
			}
			next.ServeHTTP(w, r)
		})
	})

	// API rate limiting
	r.Use(customMiddleware.APILimiter.HTTPMiddleware)

	// Routes
	r.Get("/health", handlers.HealthCheck)
	r.Get("/ws", func(w http.ResponseWriter, r *http.Request) {
		handlers.ServeWs(wsHub, w, r)
	})

	// API routes
	r.Route("/api", func(r chi.Router) {
		r.Get("/blacklist", handlers.GetBlacklist)
		r.Get("/check", handlers.CheckNumber)
		r.Get("/history", handlers.GetHistory)
		r.Post("/call-summary", handlers.GenerateCallSummary)
		r.Post("/report", handlers.ReportFraud)
		r.Post("/blacklist/import", handlers.ImportBlacklist)
	})

	// Auth routes (OTP Email authentication)
	r.Route("/auth", func(r chi.Router) {
		r.Post("/send-otp", handlers.SendOTP)
		r.Post("/verify-otp", handlers.VerifyOTP)
		r.Get("/check-session", handlers.CheckSession)
	})

	// Prometheus metrics endpoint
	r.Get("/metrics", metrics.MetricsHandler())

	// Start OTP cleanup goroutine with cancellable context
	otpCtx, otpCancel := context.WithCancel(context.Background())
	defer otpCancel()
	go handlers.CleanupExpiredOTPs(otpCtx)

	// Welcome route
	r.Get("/", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		fmt.Fprintf(w, `{
			"service": "FraudGuard AI",
			"version": "2.0.0",
			"status": "running",
			"endpoints": {
				"health": "/health",
				"websocket": "/ws?device_id=YOUR_DEVICE_ID",
				"blacklist": "/api/blacklist",
				"check": "/api/check?phone=PHONE_NUMBER",
				"history": "/api/history?device_id=YOUR_DEVICE_ID&limit=20",
				"call_summary": "POST /api/call-summary",
				"report": "POST /api/report",
				"blacklist_import": "POST /api/blacklist/import",
				"metrics": "/metrics"
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
	serverErr := make(chan error, 1)
	go func() {
		log.Printf("✅ Server listening on %s", serverAddr)
		log.Printf("🔌 WebSocket endpoint: ws://%s/ws?device_id=YOUR_DEVICE_ID", serverAddr)
		if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			serverErr <- err
		}
	}()

	// Wait for shutdown signal or server error
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	select {
	case <-quit:
	case err := <-serverErr:
		log.Printf("❌ Server failed to start: %v", err)
	}

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
