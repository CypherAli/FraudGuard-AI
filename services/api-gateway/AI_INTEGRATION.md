# Tích hợp Deepgram và Google Gemini AI

## Tổng quan

Dự án FraudGuard-AI đã được cập nhật để sử dụng:
- **Deepgram API**: Chuyển đổi giọng nói thành văn bản (Speech-to-Text)
- **Google Gemini AI**: Phân tích và phát hiện lừa đảo từ nội dung cuộc gọi

## Cấu hình

### 1. Biến môi trường (.env)

File `.env` đã được cập nhật với các API keys:

```env
# AI Services
DEEPGRAM_API_KEY=41b6d70eb5a731165dde1eee393277fc9563a128
GEMINI_API_KEY=AIzaSyAojwrcRjM9zC92IabOR_invjR3ZpPrfmE
```

### 2. Cấu trúc dự án

Các file mới được tạo:

```
services/api-gateway/
├── internal/services/
│   ├── ai_clients.go          # Khai báo biến global cho AI clients
│   ├── deepgram_client.go     # Client tích hợp Deepgram API
│   ├── gemini_client.go       # Client tích hợp Google Gemini API
│   └── audio_processor.go     # Xử lý audio và phát hiện lừa đảo (đã cập nhật)
├── pkg/config/
│   └── config.go              # Cấu hình (đã cập nhật để sử dụng Gemini)
└── cmd/api/
    └── main.go                # Khởi tạo AI clients (đã cập nhật)
```

## Luồng hoạt động

### 1. Khởi tạo (main.go)

Khi server khởi động:
1. Load cấu hình từ file `.env`
2. Kết nối database
3. **Khởi tạo Deepgram client** với API key
4. **Khởi tạo Gemini client** với API key
5. Khởi động WebSocket hub

### 2. Xử lý Audio (audio_processor.go)

Khi nhận được audio từ client qua WebSocket:

```
Audio Data → ProcessAudioStream()
    ↓
    1. Deepgram API: Chuyển audio thành text (tiếng Việt)
    ↓
    2. Gemini AI: Phân tích text để phát hiện lừa đảo
    ↓
    3. Nếu phát hiện lừa đảo → Gửi cảnh báo về client
```

### 3. Phát hiện lừa đảo (gemini_client.go)

Gemini AI phân tích transcript dựa trên các dấu hiệu:
- ✅ Yêu cầu chuyển tiền gấp
- ✅ Giả mạo cơ quan chức năng (công an, tòa án, ngân hàng)
- ✅ Đe dọa, gây áp lực tâm lý
- ✅ Yêu cầu cung cấp thông tin cá nhân, mã OTP
- ✅ Hứa hẹn lợi nhuận cao bất thường
- ✅ Yêu cầu cài đặt ứng dụng lạ
- ✅ Sử dụng ngôn ngữ tạo sự hoảng loạn

Kết quả trả về:
```json
{
  "is_fraud": true/false,
  "risk_score": 0-100,
  "reason": "Lý do chi tiết"
}
```

## API Endpoints

### Deepgram API

**Endpoint**: `https://api.deepgram.com/v1/listen`

**Tham số**:
- `model=nova-2`: Model mới nhất của Deepgram
- `language=vi`: Ngôn ngữ tiếng Việt
- `punctuate=true`: Tự động thêm dấu câu
- `smart_format=true`: Định dạng thông minh

### Google Gemini API

**Endpoint**: `https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent`

**Model**: `gemini-pro` - Model phù hợp cho phân tích văn bản

## Chạy ứng dụng

### 1. Cài đặt dependencies

```bash
cd services/api-gateway
go mod tidy
```

### 2. Chạy server

```bash
go run cmd/api/main.go
```

### 3. Kiểm tra logs

Khi server khởi động, bạn sẽ thấy:

```
🚀 Starting FraudGuard AI API Gateway...
✅ Database connected
✅ Deepgram client initialized
✅ Gemini AI client initialized
✅ WebSocket hub started
✅ Server listening on 0.0.0.0:8080
```

### 4. Test với WebSocket

Kết nối WebSocket tại: `ws://localhost:8080/ws?device_id=YOUR_DEVICE_ID`

Gửi audio data qua WebSocket, server sẽ:
1. Transcribe audio bằng Deepgram
2. Phân tích bằng Gemini AI
3. Gửi cảnh báo nếu phát hiện lừa đảo

## Xử lý lỗi

### Nếu API key không được cấu hình

Server vẫn chạy nhưng sẽ bỏ qua các bước xử lý AI:

```
⚠️ Deepgram API key not configured
⚠️ Gemini API key not configured
```

### Nếu API call thất bại

Lỗi sẽ được log và không làm crash server:

```
❌ Deepgram transcription error: ...
❌ Gemini analysis error: ...
```

## Bảo mật

⚠️ **QUAN TRỌNG**: 
- File `.env` chứa API keys nhạy cảm
- Đảm bảo `.env` đã được thêm vào `.gitignore`
- Không commit API keys lên Git
- Trong production, sử dụng secret management service

## Giới hạn API

### Deepgram
- Kiểm tra quota tại: https://console.deepgram.com/
- API key hiện tại: `41b6d70eb5a731165dde1eee393277fc9563a128`

### Google Gemini
- Kiểm tra quota tại: https://aistudio.google.com/
- API key hiện tại: `AIzaSyAojwrcRjM9zC92IabOR_invjR3ZpPrfmE`
- Free tier: 60 requests/minute

## Tối ưu hóa

### 1. Audio Buffering
Hiện tại mỗi audio chunk được gửi trực tiếp đến Deepgram. Có thể tối ưu bằng cách:
- Buffer nhiều chunks trước khi gửi
- Sử dụng Deepgram streaming API

### 2. Caching
Có thể cache kết quả phân tích cho các transcript giống nhau để tiết kiệm API calls.

### 3. Rate Limiting
Thêm rate limiting để tránh vượt quá giới hạn API.

## Troubleshooting

### Lỗi "invalid JSON format in Gemini response"
- Gemini đôi khi trả về JSON wrapped trong markdown
- Code đã xử lý bằng cách extract JSON từ response

### Lỗi "no transcription found in response"
- Audio có thể quá ngắn hoặc không có tiếng nói
- Kiểm tra format audio (nên dùng WAV)

### Lỗi connection timeout
- Kiểm tra kết nối internet
- Tăng timeout trong HTTP client nếu cần

## Liên hệ

Nếu có vấn đề, kiểm tra:
1. API keys có đúng không
2. Quota API còn không
3. Logs server để xem lỗi chi tiết
