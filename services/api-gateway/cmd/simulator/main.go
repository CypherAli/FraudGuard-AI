package main

import (
	"encoding/json"
	"log"
	"net/url"
	"os"
	"os/signal"
	"time"

	"github.com/gorilla/websocket"
)

// AlertMessage represents the alert received from server
type AlertMessage struct {
	RiskScore int    `json:"risk_score"`
	Message   string `json:"message"`
	Action    string `json:"action"`
	Timestamp int64  `json:"timestamp"`
}

func main() {
	log.Println("=== FraudGuard AI - WebSocket Simulator ===")
	log.Println("Mô phỏng Mobile App kết nối đến Server")

	// 1. Kết nối đến Server
	u := url.URL{Scheme: "ws", Host: "localhost:8080", Path: "/ws", RawQuery: "device_id=SIMULATOR_01"}
	log.Printf("🔌 Đang kết nối đến: %s", u.String())

	c, _, err := websocket.DefaultDialer.Dial(u.String(), nil)
	if err != nil {
		log.Fatal(" Kết nối thất bại:", err)
	}
	defer c.Close()
	log.Println(" Kết nối thành công!")

	done := make(chan struct{})
	interrupt := make(chan os.Signal, 1)
	signal.Notify(interrupt, os.Interrupt)

	// 2. Goroutine để NHẬN cảnh báo từ Server
	go func() {
		defer close(done)
		for {
			_, message, err := c.ReadMessage()
			if err != nil {
				log.Println(" Ngắt kết nối:", err)
				return
			}

			// Parse alert message
			var alert AlertMessage
			if err := json.Unmarshal(message, &alert); err != nil {
				log.Printf(" Nhận được: %s", string(message))
			} else {
				log.Printf("\n === CẢNH BÁO TỪ SERVER ===")
				log.Printf("   Risk Score: %d/100", alert.RiskScore)
				log.Printf("   Action: %s", alert.Action)
				log.Printf("   Message: %s", alert.Message)
				log.Printf("   Timestamp: %d", alert.Timestamp)
				log.Printf("================================\n")
			}
		}
	}()

	// 3. Giả lập gửi Audio
	log.Println(" Bắt đầu giả lập gửi Audio...")
	log.Println("⚠️ Lưu ý: Server đang dùng mock data, không cần Deepgram thật")

	// Scenario 1: Gửi audio chunk bình thường
	log.Println(" Scenario 1: Gửi audio chunk bình thường...")
	dummyAudio1 := []byte("AUDIO_CHUNK_NORMAL") // Giả vờ là audio
	err = c.WriteMessage(websocket.BinaryMessage, dummyAudio1)
	if err != nil {
		log.Println(" Lỗi gửi:", err)
		return
	}
	log.Println(" Đã gửi Audio Chunk 1")
	time.Sleep(2 * time.Second)

	// Scenario 2: Gửi audio chunk có nội dung lừa đảo (mock)
	log.Println("\n Scenario 2: Gửi audio chunk lừa đảo...")
	dummyAudio2 := []byte("AUDIO_CHUNK_FRAUD") // Giả vờ là audio lừa đảo
	err = c.WriteMessage(websocket.BinaryMessage, dummyAudio2)
	if err != nil {
		log.Println(" Lỗi gửi:", err)
		return
	}
	log.Println(" Đã gửi Audio Chunk 2")
	time.Sleep(2 * time.Second)

	// Scenario 3: Gửi nhiều chunks để test accumulated scoring
	log.Println("\n Scenario 3: Gửi nhiều chunks (test accumulated scoring)...")
	for i := 3; i <= 5; i++ {
		dummyAudio := []byte("AUDIO_CHUNK_" + string(rune(i)))
		err = c.WriteMessage(websocket.BinaryMessage, dummyAudio)
		if err != nil {
			log.Println(" Lỗi gửi:", err)
			return
		}
		log.Printf(" Đã gửi Audio Chunk %d", i)
		time.Sleep(1 * time.Second)
	}

	// Scenario 4: Test JSON message (report fraud)
	log.Println("\n Scenario 4: Gửi báo cáo số lừa đảo (JSON)...")
	reportJSON := `{
		"device_id": "SIMULATOR_01",
		"phone_number": "+84987654321",
		"reason": "Giả mạo công an, đòi chuyển tiền"
	}`
	err = c.WriteMessage(websocket.TextMessage, []byte(reportJSON))
	if err != nil {
		log.Println(" Lỗi gửi:", err)
		return
	}
	log.Println(" Đã gửi báo cáo fraud")

	// Giữ kết nối để chờ Server phản hồi
	log.Println(" Chờ phản hồi từ Server (5 giây)...")
	log.Println("💡 Nhấn Ctrl+C để thoát sớm")

	select {
	case <-done:
		log.Println(" Kết nối đã đóng")
	case <-interrupt:
		log.Println("\n Nhận tín hiệu interrupt, đang đóng kết nối...")
		err := c.WriteMessage(websocket.CloseMessage, websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
		if err != nil {
			log.Println(" Lỗi khi đóng:", err)
			return
		}
		select {
		case <-done:
		case <-time.After(time.Second):
		}
	case <-time.After(5 * time.Second):
		log.Println(" Hết giờ test, đóng kết nối")
	}

	log.Println("\n=== Test hoàn tất ===")
}
