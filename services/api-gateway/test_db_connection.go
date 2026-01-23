package main

import (
	"context"
	"fmt"
	"log"
	"time"

	"github.com/fraudguard/api-gateway/pkg/config"
	"github.com/jackc/pgx/v5/pgxpool"
)

func main() {
	fmt.Println("🔍 Testing Database Connection...")

	// Load config
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("❌ Config load failed: %v", err)
	}

	// Build connection config
	poolConfig, err := pgxpool.ParseConfig(cfg.Database.GetDSN())
	if err != nil {
		log.Fatalf("❌ Parse config failed: %v", err)
	}

	poolConfig.MaxConns = int32(cfg.Database.MaxConns)
	poolConfig.MinConns = int32(cfg.Database.MinConns)

	// Create connection pool
	ctx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()

	pool, err := pgxpool.NewWithConfig(ctx, poolConfig)
	if err != nil {
		log.Fatalf("❌ Connection pool creation failed: %v", err)
	}
	defer pool.Close()

	// Test connection
	if err := pool.Ping(ctx); err != nil {
		log.Fatalf("❌ Ping failed: %v", err)
	}

	fmt.Println("✅ Database connection successful!")

	// Query sample data
	var count int
	err = pool.QueryRow(ctx, "SELECT COUNT(*) FROM blacklists").Scan(&count)
	if err != nil {
		log.Fatalf("❌ Query failed: %v", err)
	}

	fmt.Printf("✅ Blacklist entries: %d\n", count)

	// Query users
	err = pool.QueryRow(ctx, "SELECT COUNT(*) FROM users").Scan(&count)
	if err != nil {
		log.Fatalf("❌ Query failed: %v", err)
	}

	fmt.Printf("✅ User entries: %d\n", count)

	// Test JSONB column
	var hasJSONB bool
	err = pool.QueryRow(ctx, `
		SELECT EXISTS (
			SELECT 1 FROM information_schema.columns 
			WHERE table_name='call_logs' AND column_name='metadata' AND data_type='jsonb'
		)
	`).Scan(&hasJSONB)
	if err != nil {
		log.Fatalf("❌ JSONB check failed: %v", err)
	}

	if hasJSONB {
		fmt.Println("✅ JSONB metadata column verified!")
	}

	fmt.Println("\n🎉 All database tests passed!")
}
