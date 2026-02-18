package db

import (
	"context"
	"fmt"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/pkg/config"
	"github.com/jackc/pgx/v5/pgxpool"
)

const (
	defaultMaxConns = 25
	defaultMinConns = 2
	maxAllowedConns = 100
)

// Pool is the global database connection pool
var Pool *pgxpool.Pool

// Connect initializes the database connection pool
func Connect(cfg *config.DatabaseConfig) error {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	// Validate and clamp pool sizes to safe defaults
	maxConns := cfg.MaxConns
	if maxConns <= 0 {
		log.Printf("⚠️  DB MaxConns=%d is invalid, using default %d", maxConns, defaultMaxConns)
		maxConns = defaultMaxConns
	} else if maxConns > maxAllowedConns {
		log.Printf("⚠️  DB MaxConns=%d exceeds limit, clamping to %d", maxConns, maxAllowedConns)
		maxConns = maxAllowedConns
	}

	minConns := cfg.MinConns
	if minConns < 0 {
		log.Printf("⚠️  DB MinConns=%d is invalid, using default %d", minConns, defaultMinConns)
		minConns = defaultMinConns
	}
	if minConns > maxConns {
		log.Printf("⚠️  DB MinConns=%d > MaxConns=%d, clamping MinConns to %d", minConns, maxConns, maxConns/2)
		minConns = maxConns / 2
	}

	// Build connection config
	poolConfig, err := pgxpool.ParseConfig(cfg.GetDSN())
	if err != nil {
		return fmt.Errorf("unable to parse database config: %w", err)
	}

	// Set validated pool sizes
	poolConfig.MaxConns = int32(maxConns)
	poolConfig.MinConns = int32(minConns)

	// Set connection timeouts
	poolConfig.ConnConfig.ConnectTimeout = 5 * time.Second

	// Create connection pool
	pool, err := pgxpool.NewWithConfig(ctx, poolConfig)
	if err != nil {
		return fmt.Errorf("unable to create connection pool: %w", err)
	}

	// Test connection
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return fmt.Errorf("unable to ping database: %w", err)
	}

	Pool = pool
	log.Printf("✅ Database connection established (Max: %d, Min: %d)", maxConns, minConns)
	return nil
}

// Close gracefully closes the database connection pool
func Close() {
	if Pool != nil {
		Pool.Close()
		log.Println("Database connection closed")
	}
}

// HealthCheck verifies database connectivity
func HealthCheck(ctx context.Context) error {
	if Pool == nil {
		return fmt.Errorf("database pool is not initialized")
	}

	ctx, cancel := context.WithTimeout(ctx, 2*time.Second)
	defer cancel()

	if err := Pool.Ping(ctx); err != nil {
		return fmt.Errorf("database health check failed: %w", err)
	}

	return nil
}
