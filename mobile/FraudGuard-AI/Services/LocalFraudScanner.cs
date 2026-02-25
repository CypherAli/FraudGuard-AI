using System;
using System.Collections.Generic;
using System.Linq;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Lớp 1 (On-device) — Local keyword scanner chạy NGAY TRÊN THIẾT BỊ.
    ///
    /// Nhận transcript text từ backend "transcript_preview" message và cảnh báo
    /// NGAY LẬP TỨC khi phát hiện từ khóa cực kỳ nguy hiểm — không cần chờ
    /// FraudDetector + Gemini Agent hoàn thành phân tích (có thể mất 1-5 giây).
    ///
    /// Kiến trúc Phòng thủ 3 lớp:
    ///   Lớp 1 (đây) — On-device instant scan: cảnh báo trong &lt;100ms
    ///   Lớp 2 (Backend) — Pattern scoring + time-based decay
    ///   Lớp 3 (AI Cloud) — Gemini Agent, chỉ khi score ≥ MediumThreshold
    /// </summary>
    public static class LocalFraudScanner
    {
        // ── Emergency keywords (1 hit = CRITICAL cảnh báo ngay) ──────────────
        // Các từ này GẦN NHƯ KHÔNG BAO GIỜ xuất hiện trong hội thoại bình thường.
        // Bất kỳ 1 từ nào trong danh sách này = nguy hiểm tức thì.
        private static readonly string[] EmergencyKeywords =
        {
            // Mã xác thực / OTP
            "mã otp", "nhập otp", "đọc mã otp", "gửi otp", "otp của bạn",
            "mã xác nhận", "mã bảo mật", "mã giao dịch",

            // Phần mềm kiểm soát từ xa — red flag tuyệt đối
            "anydesk", "teamviewer", "ultraviewer", "airdroid", "remote desktop",

            // Thông tin tài chính nhạy cảm
            "số tài khoản ngân hàng", "số thẻ tín dụng", "số thẻ atm",
            "cvv", "mã cvv", "pin atm", "mã pin",

            // Lệnh bắt / truy nã (giả mạo công an)
            "lệnh bắt", "lệnh tạm giữ", "bắt giam ngay",
            "bị truy nã", "lệnh truy nã",

            // Yêu cầu tuyệt mật
            "không được nói với ai", "tuyệt đối giữ bí mật",
            "đừng nói với gia đình",
        };

        // ── Warning keywords (2+ hits = hiển thị banner cảnh báo nhẹ) ────────
        // Có thể xuất hiện trong hội thoại bình thường (phim, tin tức),
        // nhưng khi xuất hiện cùng nhau = đáng ngờ.
        private static readonly string[] WarningKeywords =
        {
            "công an", "cảnh sát", "viện kiểm sát", "tòa án",
            "chuyển tiền", "chuyển khoản",
            "rửa tiền", "ma túy", "buôn người",
            "tài khoản bị khóa", "tài khoản bị đóng băng",
            "bí mật", "đừng nói ai",
            "trong 5 phút", "trong 10 phút", "ngay lập tức", "gấp lắm",
        };

        // ── Whitelist — suppress warnings nếu context rõ ràng là bình thường ─
        private static readonly string[] ContextWhitelist =
        {
            "phim", "bộ phim", "kịch bản", "diễn viên", "đạo diễn",
            "truyện", "tiểu thuyết", "tin tức", "bản tin",
            "drama", "series", "tập phim",
        };

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Quét transcript cho emergency/warning keywords.
        /// Trả về LocalScanResult với Level và danh sách từ khóa tìm thấy.
        /// </summary>
        public static LocalScanResult Scan(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
                return LocalScanResult.Safe;

            var lower = transcript.ToLowerInvariant();

            // Nếu context whitelist rõ ràng (đang nói về phim/tin tức),
            // chỉ emergency keywords mới trigger cảnh báo.
            bool hasWhitelistContext = ContextWhitelist.Any(w => lower.Contains(w));

            // 1. Kiểm tra Emergency keywords (LUÔN cảnh báo, dù có whitelist)
            var foundEmergency = EmergencyKeywords
                .Where(kw => lower.Contains(kw))
                .ToList();

            if (foundEmergency.Count > 0)
            {
                return new LocalScanResult(ScanLevel.Emergency, foundEmergency);
            }

            // 2. Kiểm tra Warning keywords (chỉ cảnh báo khi không có whitelist)
            if (!hasWhitelistContext)
            {
                var foundWarning = WarningKeywords
                    .Where(kw => lower.Contains(kw))
                    .ToList();

                // Cần ≥2 warning keyword để tránh false positive
                if (foundWarning.Count >= 2)
                {
                    return new LocalScanResult(ScanLevel.Warning, foundWarning);
                }
            }

            return LocalScanResult.Safe;
        }
    }

    // ── Result types ──────────────────────────────────────────────────────────

    public enum ScanLevel
    {
        Safe      = 0,
        Warning   = 1,
        Emergency = 2,
    }

    public sealed class LocalScanResult
    {
        public static readonly LocalScanResult Safe =
            new LocalScanResult(ScanLevel.Safe, new List<string>());

        public ScanLevel    Level    { get; }
        public List<string> Keywords { get; }

        public LocalScanResult(ScanLevel level, List<string> keywords)
        {
            Level    = level;
            Keywords = keywords ?? new List<string>();
        }
    }
}
