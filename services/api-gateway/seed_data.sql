-- ==========================================
-- FRAUDGUARD AI - BLACKLIST SEED DATA
-- 50+ Fraud Phone Numbers with Real Scenarios
-- Schema: phone_number, reason, confidence_score, reported_count
-- ==========================================

-- Clean existing data
TRUNCATE TABLE blacklist CASCADE;

-- ==========================================
-- NHOM 1: GIA DANH CO QUAN QUYEN LUC (CRITICAL - Score 0.9-1.0)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0911333444', 'Gia danh Dai uy cong an: Bao lien quan duong day rua tien xuyen quoc gia', 0.95, 45, 'active'),
('0988111222', 'Mao danh VKSND Toi cao: Gui lenh bat tam giam gia qua Zalo', 0.98, 67, 'active'),
('0909555666', 'Gia danh Cuc Canh sat ma tuy: Yeu cau chuyen tien vao tai khoan tam giu', 0.92, 38, 'active'),
('0912999888', 'Lua dao: Yeu cau cai ung dung "Bo Cong an" gia mao chua ma doc', 0.96, 51, 'active'),
('02477778888', 'Robocall: Bo TT&TT thong bao khoa SIM sau 2 gio do chua chuan hoa', 0.91, 89, 'active'),
('02899991111', 'Gia danh nhan vien Viettel: Yeu cau gui anh CCCD de mo khoa SIM', 0.93, 72, 'active'),
('0866111222', 'Gia danh can bo thue: Moi tai app Tong cuc Thue gia de hoan thue TNCN', 0.94, 56, 'active'),
('0933444555', 'Lua dao: Cai ma doc chiem quyen dieu khien dien thoai qua link la', 0.97, 43, 'active'),
('0944777888', 'Gia danh CSGT: Thong bao phat nguoi qua han, doa chuyen ho so sang toa', 0.90, 34, 'active'),
('0899666777', 'Lua dao: Yeu cau chuyen tien phat nguoi vao tai khoan ca nhan', 0.92, 41, 'active');

-- ==========================================
-- NHOM 2: LUA DAO TAI CHINH & NGAN HANG (CRITICAL - Score 0.85-0.95)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02873009999', 'Gia danh VPBank/Techcombank: Moi nang han muc the tin dung online', 0.89, 78, 'active'),
('0901234567', 'Lua dao: Yeu cau doc ma OTP de kich hoat ho so nang han muc', 0.94, 92, 'active'),
('0915666777', 'Gia danh nhan vien ngan hang: Ho tro cap nhat sinh trac hoc khuon mat', 0.91, 65, 'active'),
('0868123123', 'Lua dao Deepfake: Video call thu thap khuon mat de pha xac thuc FaceID', 0.96, 48, 'active'),
('0922111222', 'Gia danh MoMo: Thong bao tai khoan bi tam khoa, yeu cau xac thuc CCCD', 0.88, 71, 'active'),
('0877333444', 'Lua dao: Gui link gia mao trang MoMo/ZaloPay de danh cap dang nhap', 0.93, 59, 'active'),
('0933666888', 'Gia danh VietinBank: Yeu cau cap nhat thong tin eKYC qua link gia', 0.90, 53, 'active'),
('0866444777', 'Lua dao: Thong bao trung thuong 500 trieu tu ngan hang, yeu cau nop thue', 0.87, 44, 'active');

-- ==========================================
-- NHOM 3: KHAI THAC TAM LY GIA DINH (CRITICAL - Score 0.85-0.95)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0977888999', 'Gia danh bac si: Bao con bi tai nan chan thuong so nao can tien mo gap', 0.93, 87, 'active'),
('0333999111', 'Lua dao cap cuu: Tao hien truong gia (tieng coi xe) ep chuyen tien', 0.91, 76, 'active'),
('0966111333', 'Lua dao Deepfake: Hack Zalo goi video gia giong/mat nguoi than vay tien', 0.95, 54, 'active'),
('0844555666', 'Gia danh ban than: Goi video bi mat dien thoai yeu cau chuyen tien', 0.89, 62, 'active'),
('02455558888', 'Gia danh EVN: Doa cat dien sau 2 gio do no cuoc qua app la', 0.88, 83, 'active'),
('0933777888', 'Lua dao: Gui SMS gia mao EVN voi link QR thanh toan tro ve tai khoan lua', 0.90, 69, 'active'),
('0955222333', 'Gia danh thay giao: Bao hoc sinh gap su co can hop phu huynh gap', 0.86, 41, 'active'),
('0811444555', 'Lua dao: Gia danh con gai khoc loc bao mat vi bi cuop yeu cau chuyen tien', 0.92, 58, 'active');

-- ==========================================
-- NHOM 4: DAU TU & VIEC LAM (WARNING - Score 0.70-0.85)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0833444555', 'Moi vao nhom Zalo "Thay doc lenh": Dau tu chung khoan quoc te bao lo', 0.82, 95, 'active'),
('0922888999', 'Lua dao san Forex/Tien ao: Du nap tien roi khoa tai khoan khong cho rut', 0.84, 112, 'active'),
('0788111222', 'Dau tu Bitcoin/Ethereum gia: Hoa lai 20%/thang chiem doat von goc', 0.81, 78, 'active'),
('0355666777', 'Mao danh Shopee/Lazada: Tuyen CTV chot don ao nap tien lam nhiem vu', 0.79, 134, 'active'),
('0811222333', 'Lua dao tuyen dung: Yeu cau nop phi dong phuc/ho so roi cat lien lac', 0.76, 89, 'active'),
('0566888999', 'Viec lam online: Tap don hang hang ngay nap tien tam ung khong hoan', 0.78, 156, 'active'),
('0588111222', 'Thong bao trung thuong xe SH: Yeu cau chuyen truoc phi van chuyen/thue', 0.75, 67, 'active'),
('0999888777', 'Gia danh Hai quan: Buu kien tu nuoc ngoai bi giu yeu cau dong phat', 0.77, 72, 'active'),
('0377999111', 'Trung thuong VinMart gia: Nop phi truoc de nhan qua 100 trieu', 0.74, 54, 'active'),
('0944333555', 'Du an ma: Moi mua dat nen tang gia nhanh chiem doat tien dat coc', 0.80, 43, 'active'),
('0822666777', 'Can ho chung cu sieu re: Gia mao du an that lua ban giay to gia', 0.83, 38, 'active');

-- ==========================================
-- NHOM 5: SPAM & QUAY ROI (WARNING - Score 0.60-0.75)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('02473001234', 'Spam bat dong san: Moi mua du an nghi duong dat nen lien tuc', 0.68, 245, 'active'),
('02888889999', 'Telesale: Spam moi mo the tin dung/vay tieu dung khong ngung', 0.65, 312, 'active'),
('0911000111', 'Quang cao rac: Dich vu hut be phot thong tac cong', 0.62, 189, 'active'),
('0566777888', 'Spam SIM so dep: Nhan tin/goi dien chao ban SIM lien tuc', 0.64, 267, 'active'),
('0944111222', 'Robocall: Quang cao khoa hoc tieng Anh/Ky nang mem tu dong', 0.67, 298, 'active'),
('0355444666', 'Spam casino online: Moi tham gia choi game bai doi thuong', 0.71, 156, 'active'),
('0799888111', 'Quang cao vay tien nhanh: Duyet 5 phut lai 0% thuc chat lua dao', 0.73, 178, 'active'),
('0522333444', 'Telesale bao hiem: Goi dien chao ban bao hiem nhan tho lien tuc', 0.63, 234, 'active'),
('0866777999', 'Spam my pham: Chao ban kem tri mun tri nam than ki', 0.61, 145, 'active'),
('0977222111', 'Quang cao hoc luat xe: Goi dien spam moi hoc lai xe A1 A2 B2', 0.66, 201, 'active');

-- ==========================================
-- NHOM 6: CAC HINH THUC KHAC (MIXED - Score 0.70-0.90)
-- ==========================================

INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, status) VALUES
('0888777666', 'Gia danh Facebook: Thong bao vi pham ban quyen yeu cau xac thuc', 0.85, 67, 'active'),
('0966444555', 'Lua dao hen ho online: Tao tai khoan gia xinh vay tien sau khi quen', 0.79, 94, 'active'),
('02466669999', 'Gia danh trung tam tiem chung: Moi dang ky vaccine COVID gia thu phi', 0.82, 52, 'active'),
('0933222111', 'Quang cao thuoc chua benh: Ban san pham gia chat luong kem', 0.71, 118, 'active'),
('0877555444', 'Tour du lich gia re: Nhan tien dat coc roi huy tour khong hoan', 0.76, 85, 'active'),
('0522888999', 'Ve may bay gia: Gui thong tin dat cho roi bien mat chiem doat tien', 0.81, 73, 'active'),
('0944666888', 'Gia danh quy tu thien: Keu goi quyen gop cho benh nhi chiem doat', 0.84, 61, 'active'),
('0366777999', 'Lu lut mien Trung gia: Tao tai khoan ngan hang gia nhan tu thien', 0.87, 48, 'active');

-- ==========================================
-- STATISTICS & VERIFICATION
-- ==========================================

-- Count total records
SELECT 'Total blacklist entries' as metric, COUNT(*) as value FROM blacklist
UNION ALL
SELECT 'CRITICAL (>= 0.85)', COUNT(*) FROM blacklist WHERE confidence_score >= 0.85
UNION ALL
SELECT 'WARNING (0.70-0.84)', COUNT(*) FROM blacklist WHERE confidence_score >= 0.70 AND confidence_score < 0.85
UNION ALL
SELECT 'LOW (< 0.70)', COUNT(*) FROM blacklist WHERE confidence_score < 0.70
UNION ALL
SELECT 'Average confidence', ROUND(AVG(confidence_score)::numeric, 3)::text FROM blacklist
UNION ALL
SELECT 'Total reports', SUM(reported_count)::text FROM blacklist;

-- Show sample high-risk numbers
SELECT 
    phone_number,
    ROUND(confidence_score::numeric, 2) as score,
    reported_count as reports,
    LEFT(reason, 60) as description
FROM blacklist
WHERE confidence_score >= 0.90
ORDER BY confidence_score DESC, reported_count DESC
LIMIT 10;

-- Success message
SELECT 
    '=== DATA IMPORT SUCCESSFUL ===' as status,
    COUNT(*) as total_records,
    ROUND(AVG(confidence_score)::numeric, 3) as avg_score,
    SUM(reported_count) as total_reports
FROM blacklist;
