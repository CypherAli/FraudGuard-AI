# Setup appsettings.json (local only)

This project uses a local config file for email OTP.
Do NOT commit it to Git.

## File path
Create this file on your machine:

mobile/FraudGuard-AI/Resources/Raw/appsettings.json

## Nội dung file appsettings.json

{
  "Brevo": {
    "ApiKey": "xkeysib-...",
    "FromEmail": "your_verified_sender@email.com",
    "FromName": "FraudGuard AI"
  }
}

## Notes
1️⃣ Tạo tài khoản Brevo

Truy cập: https://www.brevo.com

Chọn Sign up hoặc Log in

Hoàn tất các bước xác minh tài khoản theo yêu cầu của Brevo

2️⃣ Xác thực người gửi (Sender)

Vào Settings → Senders & IP

Chọn Add a sender

Nhập email bạn muốn dùng để gửi OTP

Brevo sẽ gửi email xác thực đến địa chỉ đó

Mở email và bấm Verify để hoàn tất xác thực

👉 Chỉ những sender đã verify mới gửi mail được.

3️⃣ Tạo API Key

Vào SMTP & API → API keys

Chọn Generate a new API key

Đặt tên cho API key

Copy API key và lưu lại


4️⃣ Điền cấu hình vào ứng dụng

Điền config vào appsettings.json
 
