package middleware

import (
	"encoding/json"
	"net/http"
	"strings"
)

// TokenValidator is a function that checks if a bearer token is valid.
// Injected from handlers.ValidateToken to avoid import cycles.
type TokenValidator func(token string) bool

// RequireAuth returns a middleware that validates Bearer tokens.
// Endpoints protected by this middleware will return 401 if the token
// is missing, malformed, or unknown.
func RequireAuth(validate TokenValidator) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			authHeader := r.Header.Get("Authorization")
			if authHeader == "" {
				writeUnauthorized(w, "Authorization header required")
				return
			}

			if !strings.HasPrefix(authHeader, "Bearer ") {
				writeUnauthorized(w, "Invalid authorization format — expected 'Bearer <token>'")
				return
			}

			token := strings.TrimPrefix(authHeader, "Bearer ")
			if !validate(token) {
				writeUnauthorized(w, "Token không hợp lệ hoặc đã hết hạn")
				return
			}

			next.ServeHTTP(w, r)
		})
	}
}

func writeUnauthorized(w http.ResponseWriter, msg string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusUnauthorized)
	json.NewEncoder(w).Encode(map[string]interface{}{
		"success": false,
		"error":   msg,
	})
}
