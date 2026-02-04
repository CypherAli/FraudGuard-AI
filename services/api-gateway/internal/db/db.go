package db

import (
	"context"
	"fmt"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/pkg/config"
	"github.com/jackc/pgx/v5/pgxpool"
)

// Pool is the global database connection pool
var Pool *pgxpool.Pool

// Connect initializes the database connection pool
func Connect(cfg *config.DatabaseConfig) error {
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	// Build connection config
	poolConfig, err := pgxpool.ParseConfig(cfg.GetDSN())
	if err != nil {
		return fmt.Errorf("unable to parse database config: %w", err)
	}

	// Set pool size
	poolConfig.MaxConns = int32(cfg.MaxConns)
	poolConfig.MinConns = int32(cfg.MinConns)

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
	log.Printf(" Database connection established (Max: %d, Min: %d)", cfg.MaxConns, cfg.MinConns)
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
