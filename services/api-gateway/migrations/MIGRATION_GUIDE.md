# Database Migration Guide

## Kiến trúc Database

FraudGuard AI dùng **2 database riêng biệt**:

| Database | Engine | Lưu gì | Tạo bởi |
|----------|--------|--------|---------|
| `fraudguard_db` | PostgreSQL (cloud) | `blacklist` table | `migrate.go` — tự động khi server start |
| `fraud_guard.db` | SQLite (local) | Call logs per-session | GORM AutoMigrate — tự động khi start |

> **Lưu ý**: Bảng `users` và `call_logs` trong `001_init.sql` là schema cũ, **hiện không dùng**.
> Code thực tế lưu call logs vào SQLite thông qua GORM.

---

## Các file migrations/

| File | Mục đích | Chạy khi nào |
|------|----------|-------------|
| `000_create_database.sql` | Tạo PostgreSQL user + database | 1 lần khi setup lần đầu |
| `001_init.sql` | Schema ban đầu (tham khảo) | Không cần chạy — `migrate.go` lo |
| `002_fix_blacklist_schema.sql` | Fix blacklist UUID → SERIAL | Đã áp dụng (v2) |
| `003_seed_data.sql` | Seed 42 số fraud VN | Tham khảo — `migrate.go` đã có seed |

---

## Setup lần đầu (Fresh install)

```bash
# 1. Tạo PostgreSQL database (chỉ cần 1 lần)
psql -U postgres -f migrations/000_create_database.sql

# 2. Khởi động server — tự động tạo bảng + seed data
cd services/api-gateway
go run cmd/api/main.go
# Server sẽ tự chạy AutoMigrate() khi start
```

Không cần chạy tay các file SQL còn lại.

---

## Thêm migration mới

Khi cần thay đổi schema PostgreSQL:

1. Tạo file: `00X_mo_ta.sql` (đánh số tiếp theo)
2. Cập nhật `internal/db/migrate.go` để áp dụng thay đổi
3. Document breaking changes trong file này
4. Test trên dev trước

```bash
# Backup trước khi migrate production
pg_dump $DATABASE_URL > backup_$(date +%Y%m%d_%H%M).sql

# Restore nếu có lỗi
psql $DATABASE_URL < backup_YYYYMMDD_HHMM.sql
```

---

## Schema History

### v2 — 002_fix_blacklist_schema.sql (2026-02-10)
**Breaking Change** ⚠️
- `blacklist.id`: UUID → SERIAL (auto-increment)
- Rename: `blacklists` → `blacklist`
- Rename: `report_count` → `reported_count`, `risk_level` → `confidence_score`
- Thêm: `first_reported_at`, `status`
