package services

// keyword_config.go — Hot-reloadable keyword configuration for FraudGuard
//
// PDF spec: "Keyword lists are config-file driven and hot-reloadable as scam tactics evolve."
//
// Implementation notes:
//   - Dùng sync/atomic.Value thay vì unsafe.Pointer — type-safe, không cần unsafe package
//   - globalKeywordWatcher được set trong initKeywordWatcher (called via sync.Once)
//   - GetKeywordMatcher() luôn trả về non-nil matcher (fallback sang builtin nếu cần)

import (
	"encoding/json"
	"log"
	"os"
	"path/filepath"
	"regexp"
	"runtime"
	"sync"
	"sync/atomic"
	"time"
)

// KeywordConfig là cấu trúc JSON của keywords.json
type KeywordConfig struct {
	CriticalKeywords  map[string]int `json:"criticalKeywords"`
	WarningKeywords   map[string]int `json:"warningKeywords"`
	SuspiciousPhrases map[string]int `json:"suspiciousPhrases"`
	NegativeKeywords  map[string]int `json:"negativeKeywords"`
}

// keywordWatcher quản lý việc load và hot-reload keyword config từ file
type keywordWatcher struct {
	filePath    string
	lastModTime time.Time
	matcher     atomic.Value // stores *KeywordMatcher, type-safe thay vì unsafe.Pointer
	mu          sync.Mutex   // chỉ dùng khi cập nhật lastModTime
	stopCh      chan struct{}
}

// globalKeywordWatcher là singleton, được set đúng 1 lần trong initKeywordWatcher
// Dùng atomic.Pointer thay vì bare pointer để đảm bảo visibility across goroutines
var globalKeywordWatcherPtr atomic.Pointer[keywordWatcher]

// initKeywordWatcher khởi tạo watcher; gọi đúng 1 lần qua sync.Once trong ensureKeywordWatcher
func initKeywordWatcher(configPath string) {
	watcher := &keywordWatcher{
		filePath: configPath,
		stopCh:   make(chan struct{}),
	}

	// Load lần đầu — fallback sang builtin nếu file không tồn tại hoặc parse lỗi
	km := watcher.loadFromFile()
	if km == nil {
		log.Printf("⚠️ [KeywordWatcher] keywords.json not found at %s, using built-in defaults", configPath)
		km = builtinKeywordMatcher()
	} else {
		log.Printf("✅ [KeywordWatcher] Loaded keywords from %s", configPath)
	}
	watcher.matcher.Store(km)

	// Publish watcher trước khi start goroutine để GetKeywordMatcher thấy ngay
	globalKeywordWatcherPtr.Store(watcher)

	// Bắt đầu goroutine watch file changes
	go watcher.watch()
}

// GetKeywordMatcher trả về KeywordMatcher hiện tại (thread-safe, lock-free read).
// Luôn trả về non-nil — worst case là builtin matcher.
func GetKeywordMatcher() *KeywordMatcher {
	w := globalKeywordWatcherPtr.Load()
	if w == nil {
		// Watcher chưa init (hiếm — chỉ xảy ra nếu gọi trước ensureKeywordWatcher)
		return builtinKeywordMatcher()
	}
	km, _ := w.matcher.Load().(*KeywordMatcher)
	if km == nil {
		return builtinKeywordMatcher()
	}
	return km
}

// watch kiểm tra mtime của file mỗi 30s, reload nếu thay đổi
func (w *keywordWatcher) watch() {
	ticker := time.NewTicker(30 * time.Second)
	defer ticker.Stop()
	for {
		select {
		case <-ticker.C:
			w.reloadIfChanged()
		case <-w.stopCh:
			log.Printf("🛑 [KeywordWatcher] Stopped")
			return
		}
	}
}

// reloadIfChanged kiểm tra mtime và reload nếu file thay đổi.
// Giữ nguyên matcher cũ nếu parse lỗi — không crash service.
func (w *keywordWatcher) reloadIfChanged() {
	info, err := os.Stat(w.filePath)
	if err != nil {
		return // file bị xoá / không đọc được — giữ nguyên matcher cũ
	}

	w.mu.Lock()
	changed := info.ModTime().After(w.lastModTime)
	w.mu.Unlock()

	if !changed {
		return
	}

	km := w.loadFromFile()
	if km == nil {
		return // parse lỗi — giữ nguyên matcher cũ
	}

	w.mu.Lock()
	w.lastModTime = info.ModTime()
	w.mu.Unlock()

	w.matcher.Store(km)
	log.Printf("🔄 [KeywordWatcher] Keywords reloaded from %s (mtime: %s)", w.filePath, info.ModTime().Format(time.RFC3339))
}

// loadFromFile đọc và parse keywords.json, trả về nil nếu lỗi
func (w *keywordWatcher) loadFromFile() *KeywordMatcher {
	data, err := os.ReadFile(w.filePath)
	if err != nil {
		return nil
	}

	var cfg KeywordConfig
	if err := json.Unmarshal(data, &cfg); err != nil {
		log.Printf("❌ [KeywordWatcher] Failed to parse %s: %v", w.filePath, err)
		return nil
	}

	// Đảm bảo các map không nil để tránh panic khi range
	if cfg.CriticalKeywords == nil {
		cfg.CriticalKeywords = map[string]int{}
	}
	if cfg.WarningKeywords == nil {
		cfg.WarningKeywords = map[string]int{}
	}
	if cfg.SuspiciousPhrases == nil {
		cfg.SuspiciousPhrases = map[string]int{}
	}
	if cfg.NegativeKeywords == nil {
		cfg.NegativeKeywords = map[string]int{}
	}

	// Warn nếu keyword categories bị thiếu — silent empty map = fraud detection bị vô hiệu hóa một phần
	if len(cfg.CriticalKeywords) == 0 {
		log.Printf("⚠️ [KeywordWatcher] criticalKeywords is empty in %s — critical fraud detection disabled!", w.filePath)
	}
	if len(cfg.WarningKeywords) == 0 {
		log.Printf("⚠️ [KeywordWatcher] warningKeywords is empty in %s", w.filePath)
	}
	if len(cfg.SuspiciousPhrases) == 0 {
		log.Printf("⚠️ [KeywordWatcher] suspiciousPhrases is empty in %s", w.filePath)
	}

	km := &KeywordMatcher{
		criticalKeywords:  cfg.CriticalKeywords,
		warningKeywords:   cfg.WarningKeywords,
		suspiciousPhrases: cfg.SuspiciousPhrases,
		negativeKeywords:  cfg.NegativeKeywords,
	}

	// Compile các negative regex patterns (hardcoded — ít thay đổi hơn keywords)
	km.negativePatterns = []*regexp.Regexp{
		regexp.MustCompile(`(?i)p[h\-\. _]*[h1i!][\-\. _]*[i1!][\-\. _]*m`),
		regexp.MustCompile(`(?i)t[r\-\. _]*[u\-\. _]*[y\-\. _]*[e3ê\-\. _]*[n\-\. _]`),
		regexp.MustCompile(`(?i)t[i1!\-\. _]*n[\-\. _]+t[u\-\. _]*[c\-\. _]`),
		regexp.MustCompile(`(?i)s[a@4\-\. _]*[c\-\. _]*h`),
		regexp.MustCompile(`(?i)d[i1!\-\. _]*[e3ê\-\. _]*n[\-\. _]+v[i1!\-\. _]*[e3ê\-\. _]*n`),
		regexp.MustCompile(`(?i)k[i1!\-\. _]*[c\-\. _]*h[\-\. _]+b[a@4\-\. _]*n`),
	}
	km.negativeScores = []int{100, 100, 80, 90, 70, 70}

	return km
}

// defaultKeywordConfigPath trả về đường dẫn mặc định của keywords.json.
// Ưu tiên: env KEYWORD_CONFIG_PATH → cùng thư mục binary → thư mục source (dev)
func defaultKeywordConfigPath() string {
	if p := os.Getenv("KEYWORD_CONFIG_PATH"); p != "" {
		return p
	}

	// Thử cùng thư mục với binary
	exePath, err := os.Executable()
	if err == nil {
		candidate := filepath.Join(filepath.Dir(exePath), "keywords.json")
		if _, err := os.Stat(candidate); err == nil {
			return candidate
		}
	}

	// Fallback: thư mục source file (development mode)
	_, filename, _, ok := runtime.Caller(0)
	if ok {
		return filepath.Join(filepath.Dir(filename), "keywords.json")
	}

	return "keywords.json"
}

// builtinKeywordMatcher là fallback khi file không tồn tại
func builtinKeywordMatcher() *KeywordMatcher {
	return initializeKeywordMatcher()
}
