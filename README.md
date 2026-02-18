# FraudGuard AI

> Hệ thống AI phát hiện và ngăn chặn lừa đảo viễn thông theo thời gian thực.

---

## Tổng quan

FraudGuard AI phân tích cuộc gọi điện thoại trực tiếp bằng AI để cảnh báo người dùng khi phát hiện dấu hiệu lừa đảo — deepfake giọng nói, kịch bản gian lận, số điện thoại trong blacklist.

```
┌─────────────────────┐        WebSocket / REST        ┌──────────────────────┐
│  Android Mobile App │  ──────────────────────────►  │  Go API Gateway      │
│  (.NET MAUI)        │  ◄──────────────────────────  │  services/           │
│                     │      Real-time fraud alerts    │  PostgreSQL + AI     │
└─────────────────────┘                                └──────────────────────┘
```

## Cấu trúc project

```
FraudGuard-AI/
├── services/api-gateway/   # Backend Go — fraud detection engine
├── mobile/FraudGuard-AI/   # Android app — .NET MAUI
├── scripts/                # PowerShell scripts (build, deploy, server)
├── docs/                   # Tài liệu kỹ thuật
└── tests/                  # Integration tests
```

## Khởi động nhanh

### 1. Backend

```powershell
# Sao chép cấu hình
cd services/api-gateway
cp .env.example .env
# Điền API keys vào .env

# Khởi động database + server
.\scripts\start-server.ps1
```

### 2. Mobile App

```powershell
# Build APK
.\scripts\build-apk.ps1

# Build + install thẳng lên device (yêu cầu ADB)
.\scripts\build-and-install.ps1
```

## Tech Stack

| Layer | Công nghệ |
|---|---|
| Backend | Go 1.22+, Chi router, WebSocket (Gorilla) |
| Database | PostgreSQL 16, pgx/v5 |
| AI | Google Gemini, Deepgram STT |
| Mobile | .NET MAUI 8, Android |
| Auth | Firebase, Email OTP (Brevo) |
| Infra | Docker, Railway, Render, ngrok |

## Scripts

| Script | Tác dụng |
|---|---|
| `scripts/start-server.ps1` | Khởi động Docker + DB + API server |
| `scripts/build-apk.ps1` | Build Release APK |
| `scripts/build-and-install.ps1` | Build + install qua ADB |
| `scripts/deploy-railway.ps1` | Deploy lên Railway |

## Tài liệu

- [`docs/mobile/DEPLOY_GUIDE.md`](docs/mobile/DEPLOY_GUIDE.md) — Hướng dẫn deploy mobile
- [`docs/mobile/FIREBASE_SETUP.md`](docs/mobile/FIREBASE_SETUP.md) — Cấu hình Firebase
- [`docs/mobile/INSTALL_GUIDE.md`](docs/mobile/INSTALL_GUIDE.md) — Cài đặt APK
- [`docs/GMAIL_OTP_SETUP.md`](docs/GMAIL_OTP_SETUP.md) — Cấu hình Gmail OTP
- [`services/api-gateway/README.md`](services/api-gateway/README.md) — API docs chi tiết

## Yêu cầu

- Go 1.22+
- .NET 8 SDK + Android workload
- Docker Desktop
- PostgreSQL 17 (local) hoặc Railway/Render (cloud)
- Android SDK (để build APK)
