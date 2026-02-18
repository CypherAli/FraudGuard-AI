-- Don dep du lieu cu
TRUNCATE TABLE blacklist;

-- ==========================================
-- 1. NHOM DAU SO QUOC TE (LUA DAO WANGIRI / NHA MAY)
-- Dau hieu: Co dau (+) hoac (00) o dau, goi nho de du goi lai tinh cuoc phi cao
-- ==========================================
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at, created_at, updated_at) VALUES
-- Cac so cu the da duoc bao cao
('+22375260052', 'Lua dao quoc te (Mali): Nha may tru tien cuoc ve tinh', 0.95, 100, NOW(), NOW(), NOW()),
('+22382271520', 'Lua dao quoc te (Mali): Goi nho tao cuoc goi di quoc te', 0.95, 100, NOW(), NOW(), NOW()),
('+22379262886', 'Lua dao quoc te: Gia mao tong dai nuoc ngoai', 0.95, 100, NOW(), NOW(), NOW()),
('+8919008198', 'Lua dao quoc te: Gia dang dau so 1900 (tranh nham lan)', 0.95, 100, NOW(), NOW(), NOW()),
('+4422222202', 'Lua dao tu Anh (UK): Moi dau tu tai chinh lua dao', 0.95, 100, NOW(), NOW(), NOW()),
-- Cac dau so quoc gia rui ro cao (chan theo prefix neu he thong ho tro, o day luu mau dai dien)
('+2240000000', 'Dau so Guinea (+224): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW()),
('+2310000000', 'Dau so Liberia (+231): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW()),
('+2320000000', 'Dau so Sierra Leone (+232): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW()),
('+2520000000', 'Dau so Somalia (+252): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW()),
('+2470000000', 'Dau so Ascension Island (+247): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW()),
('+3710000000', 'Dau so Latvia (+371): Nguy co lua dao Wangiri cao', 0.75, 50, NOW(), NOW(), NOW());

-- ==========================================
-- 2. NHOM DAU SO CO DINH GIA MAO (024, 028)
-- Dau hieu: Dang so ban nhung goi moi chao chung khoan, gia danh cong an/toa an
-- ==========================================
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at, created_at, updated_at) VALUES
-- Khu vuc Ha Noi (024)
('02499950060', 'Gia danh Cong an Ha Noi: Thong bao phat nguoi/an tich', 0.95, 100, NOW(), NOW(), NOW()),
('02499954266', 'Gia danh nhan vien vien thong: Doa khoa SIM sau 2 gio', 0.95, 100, NOW(), NOW(), NOW()),
('0249997041', 'Spam: Quang cao chung khoan quoc te', 0.75, 50, NOW(), NOW(), NOW()),
('02444508888', 'Robocall: Cuoc goi tu dong moi chao dau tu', 0.75, 50, NOW(), NOW(), NOW()),
('02499950412', 'Lua dao: Mao danh Cuc Dang kiem/Cuc Thue', 0.95, 100, NOW(), NOW(), NOW()),
('02439446395', 'Gia danh Toa an nhan dan: Gui giay trieu tap gia', 0.95, 100, NOW(), NOW(), NOW()),

-- Khu vuc TP.HCM (028)
('02899964439', 'Gia danh Cong an TP.HCM: Bao lien quan duong day rua tien', 0.95, 100, NOW(), NOW(), NOW()),
('02856786501', 'Gia danh Dien luc EVN: Bao cat dien do no cuoc', 0.95, 100, NOW(), NOW(), NOW()),
('02899964438', 'Lua dao: Tuyen cong tac vien lam nhiem vu online', 0.95, 100, NOW(), NOW(), NOW()),
('02899964437', 'Gia danh Buu cuc: Bao co buu pham no cuoc hai quan', 0.95, 100, NOW(), NOW(), NOW()),
('02873034653', 'Spam: Moi vay tin chap lai suat cao', 0.75, 50, NOW(), NOW(), NOW()),
('02899950012', 'Spam: Moi mua ban dat nen du an ma', 0.75, 50, NOW(), NOW(), NOW()),
('02873065555', 'Telesale: Spam cuoc goi moi so huu ky nghi duong', 0.75, 50, NOW(), NOW(), NOW()),
('02899964448', 'Lua dao: Mao danh nhan vien Shopee tang qua tri an', 0.95, 100, NOW(), NOW(), NOW()),
('02822000266', 'Spam: Quang cao cac khoa hoc online lua dao', 0.75, 50, NOW(), NOW(), NOW()),
('0287108690', 'Gia danh ngan hang: Canh bao tai khoan bi khoa', 0.95, 100, NOW(), NOW(), NOW()),
('02899950015', 'Spam: Goi dien lam phien lien tuc', 0.75, 50, NOW(), NOW(), NOW()),
('02899958588', 'Lua dao: Thong bao trung thuong yeu cau phi van chuyen', 0.95, 100, NOW(), NOW(), NOW()),
('02871099082', 'Spam: Moi tham gia san Forex trai phep', 0.75, 50, NOW(), NOW(), NOW()),
('02899996142', 'Gia danh Bo Y te: Lua dao ban thuoc/thuc pham chuc nang', 0.95, 100, NOW(), NOW(), NOW());

-- ==========================================
-- 3. NHOM DAU SO DICH VU (1900) & DI DONG RAC
-- Dau hieu: Cuoc phi cao, du goi lai hoac spam tin nhan
-- ==========================================
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at, created_at, updated_at) VALUES
-- Dau so 1900 (Luu y: 1900 cung co so sach, chi chan so cu the)
('19002191', 'Tong dai spam: Kich hoat bao hanh gia mao', 0.75, 50, NOW(), NOW(), NOW()),
('19003441', 'Tong dai lua cuoc: Nhay may du goi lai (5.000d-15.000d/phut)', 0.75, 50, NOW(), NOW(), NOW()),
('19002170', 'Spam: Tu van xo so/lo de lua dao', 0.75, 50, NOW(), NOW(), NOW()),
('19002446', 'Tong dai ma: Quang cao game bai doi thuong', 0.75, 50, NOW(), NOW(), NOW()),
('19003439', 'Spam: Dich vu ket ban/hen ho lua dao', 0.75, 50, NOW(), NOW(), NOW()),
('19004510', 'Lua dao: Thong bao trung thuong ao', 0.75, 50, NOW(), NOW(), NOW()),
('19001095', 'Spam: Moi chao dich vu phong thuy me tin', 0.75, 50, NOW(), NOW(), NOW()),
('19002190', 'Tong dai spam cuoc phi cao', 0.75, 50, NOW(), NOW(), NOW()),
('19002196', 'Lua dao: Gia mao tong dai ho tro Facebook/Zalo', 0.75, 50, NOW(), NOW(), NOW()),
('19004562', 'Spam: Quang cao ban SIM so dep', 0.75, 50, NOW(), NOW(), NOW()),

-- So di dong rac / So la
('0565412664', 'Lua dao: Gia danh nhan vien nha mang nang cap SIM 4G', 0.95, 100, NOW(), NOW(), NOW());

-- ==========================================
-- GHI CHU QUAN TRONG CHO DEMO:
-- 1. Cac so 024/028/1900 co the la so ao (VoIP), ke lua dao thay doi lien tuc.
-- 2. Trong Demo, hay dung so cua ban de goi, nhung 'gia vo' hien thi la mot trong cac so tren
--    (neu App co chuc nang mo phong), hoac add so dien thoai that cua ban dien vao blacklist nay
--    de khi ho goi den, App se bao DO ngay lap tuc.
-- ==========================================
