-- ==========================================
-- FRAUDGUARD AI - BLACKLIST SEED DATA
-- 50+ Fraud Phone Numbers with Real Scenarios
-- Updated: January 26, 2026
-- ==========================================

-- Dọn dẹp dữ liệu cũ
TRUNCATE TABLE blacklist;

-- ==========================================
-- NHÓM 1: GIẢ DANH CƠ QUAN QUYỀN LỰC (AUTHORITY IMPERSONATION)
-- Mục tiêu: Đe dọa pháp lý, gây sợ hãi để nạn nhân chuyển tiền "phục vụ điều tra"
-- ==========================================

-- Kịch bản 1: Giả danh Bộ Công an / Viện Kiểm sát ("Chuyên án ma túy/Rửa tiền")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0911333444', 'Giả danh Đại úy công an: Báo liên quan đường dây rửa tiền xuyên quốc gia', 0.95, 45, 'active'),
('0988111222', 'Mạo danh VKSND Tối cao: Gửi lệnh bắt tạm giam giả qua Zalo', 0.98, 67, 'active'),
('0909555666', 'Giả danh Cục Cảnh sát ma túy: Yêu cầu chuyển tiền vào tài khoản tạm giữ để thẩm tra', 0.92, 38, 'active'),
('0912999888', 'Lừa đảo: Yêu cầu cài ứng dụng "Bộ Công an" giả mạo chứa mã độc', 0.96, 51, 'active');

-- Kịch bản 2: Giả danh Cục Viễn thông / Nhà mạng ("Khóa SIM sau 2 giờ")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02477778888', 'Robocall: "Bộ TT&TT thông báo khóa SIM sau 2 giờ do chưa chuẩn hóa"', 0.91, 89, 'active'),
('02899991111', 'Giả danh nhân viên Viettel/VinaPhone: Yêu cầu gửi ảnh CCCD để mở khóa SIM', 0.93, 72, 'active');

-- Kịch bản 3: Giả danh Cơ quan Thuế ("Ứng dụng Thuế độc hại")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0866111222', 'Giả danh cán bộ thuế: Mời tải app Tổng cục Thuế giả để hoàn thuế TNCN', 0.94, 56, 'active'),
('0933444555', 'Lừa đảo: Cài mã độc chiếm quyền điều khiển điện thoại (Accessibility) qua link lạ', 0.97, 43, 'active');

-- Kịch bản 4: Giả danh Cảnh sát Giao thông ("Phạt nguội")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0944777888', 'Giả danh CSGT: Thông báo biên lai phạt nguội quá hạn, dọa chuyển hồ sơ sang tòa án', 0.90, 34, 'active'),
('0899666777', 'Lừa đảo: Yêu cầu chuyển tiền phạt nguội vào tài khoản cá nhân để xóa hồ sơ', 0.92, 41, 'active');


-- ==========================================
-- NHÓM 2: LỪA ĐẢO TÀI CHÍNH & NGÂN HÀNG (FINANCIAL FRAUD)
-- Mục tiêu: Chiếm đoạt tài khoản, lừa chuyển tiền
-- ==========================================

-- Kịch bản 5: Giả danh Ngân hàng ("Nâng hạn mức thẻ tín dụng")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02873009999', 'Giả danh VPBank/Techcombank: Mời nâng hạn mức thẻ tín dụng online', 0.89, 78, 'active'),
('0901234567', 'Lừa đảo: Yêu cầu đọc mã OTP để "kích hoạt hồ sơ" nâng hạn mức', 0.94, 92, 'active');

-- Kịch bản 6: Cập nhật Sinh trắc học (Quyết định 2345)
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0915666777', 'Giả danh nhân viên ngân hàng: Hỗ trợ cập nhật sinh trắc học khuôn mặt online', 0.91, 65, 'active'),
('0868123123', 'Lừa đảo: Video call Deepfake thu thập khuôn mặt để phá xác thực FaceID', 0.96, 48, 'active');

-- Kịch bản 7: Giả danh Ví điện tử (MoMo/ZaloPay)
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0922111222', 'Giả danh MoMo: Thông báo tài khoản bị tạm khóa, yêu cầu xác thực CCCD', 0.88, 71, 'active'),
('0877333444', 'Lừa đảo: Gửi link giả mạo trang MoMo/ZaloPay để đánh cắp đăng nhập', 0.93, 59, 'active'),
('0933666888', 'Giả danh VietinBank: Yêu cầu cập nhật thông tin eKYC qua link giả', 0.90, 53, 'active'),
('0866444777', 'Lừa đảo: Thông báo trúng thưởng 500 triệu từ ngân hàng, yêu cầu nộp thuế', 0.87, 44, 'active');


-- ==========================================
-- NHÓM 3: KHAI THÁC TÂM LÝ GIA ĐÌNH & ĐỜI SỐNG
-- Mục tiêu: Đánh vào nỗi sợ hãi, lo lắng cho người thân
-- ==========================================

-- Kịch bản 8: "Con đang cấp cứu"
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0977888999', 'Giả danh giáo viên/bác sĩ: Báo con bị tai nạn chấn thương sọ não, cần tiền mổ gấp', 0.93, 87, 'active'),
('0333999111', 'Lừa đảo cấp cứu: Tạo hiện trường giả (tiếng còi xe) ép chuyển tiền viện phí', 0.91, 76, 'active');

-- Kịch bản 9: Video Call Deepfake ("Người bạn trong video")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0966111333', 'Lừa đảo Deepfake: Hack Zalo, gọi video giả giọng/mặt người thân để vay tiền gấp', 0.95, 54, 'active'),
('0844555666', 'Giả danh bạn thân: Gọi video bị mất điện thoại yêu cầu chuyển tiền', 0.89, 62, 'active');

-- Kịch bản 10: Giả danh Điện lực ("Cắt điện sinh hoạt")
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02455558888', 'Giả danh EVN: Dọa cắt điện sau 2 giờ do nợ cước, yêu cầu thanh toán qua app lạ', 0.88, 83, 'active'),
('0933777888', 'Lừa đảo: Gửi SMS giả mạo EVN với link QR thanh toán trỏ về tài khoản lừa đảo', 0.90, 69, 'active');

-- Kịch bản 11: Giả danh Nhà trường
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0955222333', 'Giả danh thầy giáo: Báo học sinh gặp sự cố cần họp phụ huynh gấp', 0.86, 41, 'active'),
('0811444555', 'Lừa đảo: Giả danh con gái khóc lóc báo mất ví bị cướp yêu cầu chuyển tiền', 0.92, 58, 'active');


-- ==========================================
-- NHÓM 4: ĐẦU TƯ & VIỆC LÀM (OPPORTUNITY FRAUD)
-- Mục tiêu: Đánh vào lòng tham, mong muốn kiếm tiền dễ dàng
-- ==========================================

-- Kịch bản 12: Đầu tư chứng khoán quốc tế / Tiền ảo
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0833444555', 'Mời vào nhóm Zalo "Thầy đọc lệnh": Đầu tư chứng khoán quốc tế bao lỗ', 0.82, 95, 'active'),
('0922888999', 'Lừa đảo sàn Forex/Tiền ảo: Dụ nạp tiền rồi khóa tài khoản không cho rút', 0.84, 112, 'active'),
('0788111222', 'Đầu tư Bitcoin/Ethereum giả: Hứa lãi 20%/tháng chiếm đoạt vốn gốc', 0.81, 78, 'active');

-- Kịch bản 13: Tuyển dụng "Việc nhẹ lương cao"
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0355666777', 'Mạo danh Shopee/Lazada: Tuyển CTV chốt đơn ảo, nạp tiền làm nhiệm vụ', 0.79, 134, 'active'),
('0811222333', 'Lừa đảo tuyển dụng: Yêu cầu nộp phí đồng phục/hồ sơ rồi cắt liên lạc', 0.76, 89, 'active'),
('0566888999', 'Việc làm online: Tập đơn hàng hàng ngày nạp tiền tạm ứng không hoàn', 0.78, 156, 'active');

-- Kịch bản 14: Lừa đảo trúng thưởng / Bưu kiện
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0588111222', 'Thông báo trúng thưởng xe SH: Yêu cầu chuyển trước phí vận chuyển/thuế', 0.75, 67, 'active'),
('0999888777', 'Giả danh Hải quan/Chuyển phát: Bưu kiện từ nước ngoài bị giữ, yêu cầu đóng phạt', 0.77, 72, 'active');


-- ==========================================
-- NHÓM 5: SPAM & QUẤY RỐI (RÁC VIỄN THÔNG)
-- Mục tiêu: Quảng cáo, làm phiền
-- ==========================================
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02473001234', 'Spam bất động sản: Mời mua dự án nghỉ dưỡng, đất nền', 0.65, 201, 'active'),
('02888889999', 'Telesale: Spam mời mở thẻ tín dụng/vay tiêu dùng liên tục', 0.68, 178, 'active'),
('0911000111', 'Quảng cáo rác: Dịch vụ hút bể phốt, thông tắc cống', 0.60, 145, 'active'),
('0566777888', 'Spam SIM số đẹp: Nhắn tin/gọi điện chào bán SIM', 0.62, 167, 'active'),
('0944111222', 'Cuộc gọi tự động (Robocall): Quảng cáo khóa học tiếng Anh/Kỹ năng mềm', 0.64, 189, 'active');


-- ==========================================
-- VERIFICATION & STATISTICS
-- ==========================================

-- Đếm tổng số records đã import
SELECT COUNT(*) as total_blacklist_records FROM blacklist;

-- Thống kê theo mức độ nguy hiểm (confidence score)
SELECT 
    CASE 
        WHEN confidence_score >= 0.9 THEN 'CRITICAL (≥0.9)'
        WHEN confidence_score >= 0.8 THEN 'HIGH (0.8-0.89)'
        WHEN confidence_score >= 0.7 THEN 'MEDIUM (0.7-0.79)'
        ELSE 'LOW (<0.7)'
    END as risk_level,
    COUNT(*) as count
FROM blacklist
GROUP BY risk_level
ORDER BY MIN(confidence_score) DESC;

-- Top 5 số điện thoại bị báo cáo nhiều nhất
SELECT phone_number, reason, confidence_score, reported_count
FROM blacklist
ORDER BY reported_count DESC
LIMIT 5;
