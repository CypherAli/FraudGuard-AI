# FraudGuard AI - API Gateway

> Hệ thống Backend phát hiện lừa đảo thời gian thực (Real-time Fraud Detection)
> Được xây dựng bởi Team ABSOLUTEGW - Swin Hackathon 2026

## Giới thiệu
Đây là API Gateway trung tâm của hệ thống FraudGuard AI. Hệ thống đóng vai trò tiếp nhận luồng âm thanh từ ứng dụng di động (Mobile App), chuyển tiếp sang AI Engine để phân tích và trả về cảnh báo lừa đảo ngay lập tức cho người dùng.

Hệ thống được thiết kế tối ưu cho hiệu năng cao (High Performance) và ưu tiên bảo mật quyền riêng tư (Privacy-first).

---

## Kiến trúc và Công nghệ

### Tech Stack
| Thành phần | Công nghệ | Mục đích sử dụng |
| :--- | :--- | :--- |
| **Ngôn ngữ** | Go (Golang) 1.22+ | Xử lý đồng thời (concurrency) tốc độ cao, backend core. |
| **Cơ sở dữ liệu** | PostgreSQL 16 | Lưu trữ User, Log và Blacklist. Sử dụng JSONB cho dữ liệu động. |
| **Giao thức** | WebSocket (Gorilla) | Kết nối thời gian thực 2 chiều (Full-duplex). |
| **Driver** | pgx/v5 | Driver PostgreSQL hiệu năng cao cho Go. |
| **Router** | Chi/v5 | HTTP Router nhẹ và tốc độ nhanh. |

### Điểm nhấn Kiến trúc
* **Clean Architecture:** Tuân thủ cấu trúc dự án chuẩn của Go (Standard Layout), dễ dàng mở rộng và bảo trì.
* **An toàn luồng (Concurrency Safe):** Quản lý hàng ngàn kết nối WebSocket an toàn tuyệt đối nhờ cơ chế khóa `sync.RWMutex`.
* **Ưu tiên riêng tư (Privacy First):** Luồng âm thanh (Audio Stream) được xử lý riêng biệt (Private) cho từng thiết bị, tuyệt đối không phát tán (broadcast) sang người dùng khác.

---

## Cài đặt và Chạy dự án (Quick Start)

### 1. Chuẩn bị môi trường
* Cài đặt Go phiên bản 1.22 trở lên.
* Cài đặt Docker Desktop (để chạy cơ sở dữ liệu).

### 2. Thiết lập cấu hình
Di chuyển vào thư mục dự án và tạo file biến môi trường:
```bash
cd services/api-gateway
cp .env.example .env
# Mở file .env và chỉnh sửa thông tin kết nối nếu cần thiết