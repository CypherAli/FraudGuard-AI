# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM golang:1.24-alpine AS builder

# Install git (needed for private modules), ca-certificates (HTTPS)
RUN apk add --no-cache git ca-certificates tzdata

WORKDIR /app

# Copy dependency files first — cache-friendly layer (only re-runs on go.mod/sum change)
COPY go.mod go.sum ./
RUN go mod download

# Copy source and build static binary (no CGO = smaller, no libc dependency)
COPY . .
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
    go build -ldflags="-w -s" -o fraudguard-api ./cmd/api

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM gcr.io/distroless/static-debian12:nonroot

# Copy binary and certificates from builder
COPY --from=builder /app/fraudguard-api /fraudguard-api
COPY --from=builder /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/
COPY --from=builder /usr/share/zoneinfo /usr/share/zoneinfo

# Non-root user (distroless default: uid=65532 "nonroot")
USER nonroot:nonroot

EXPOSE 8080

ENTRYPOINT ["/fraudguard-api"]
