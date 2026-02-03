-- Copy paste đoạn này vào Railway Query Editor

TRUNCATE TABLE blacklist;

-- QUOC TE (11 số)
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at) VALUES
('+22375260052', 'Lua dao quoc te (Mali)', 0.95, 100, NOW()),
('+22382271520', 'Lua dao quoc te (Mali)', 0.95, 100, NOW()),
('+22379262886', 'Lua dao quoc te', 0.95, 100, NOW()),
('+8919008198', 'Lua dao quoc te', 0.95, 100, NOW()),
('+4422222202', 'Lua dao tu Anh', 0.95, 100, NOW()),
('+2240000000', 'Guinea - Wangiri', 0.75, 50, NOW()),
('+2310000000', 'Liberia - Wangiri', 0.75, 50, NOW()),
('+2320000000', 'Sierra Leone - Wangiri', 0.75, 50, NOW()),
('+2520000000', 'Somalia - Wangiri', 0.75, 50, NOW()),
('+2470000000', 'Ascension - Wangiri', 0.75, 50, NOW()),
('+3710000000', 'Latvia - Wangiri', 0.75, 50, NOW());

-- HA NOI 024 (6 số)  
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at) VALUES
('02499950060', 'Gia danh Cong an HN', 0.95, 100, NOW()),
('02499954266', 'Gia danh vien thong', 0.95, 100, NOW()),
('0249997041', 'Spam chung khoan', 0.75, 50, NOW()),
('02444508888', 'Robocall dau tu', 0.75, 50, NOW()),
('02499950412', 'Gia danh Cuc Thue', 0.95, 100, NOW()),
('02439446395', 'Gia danh Toa an', 0.95, 100, NOW());

-- HCM 028 (14 số)
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at) VALUES
('02899964439', 'Gia danh Cong an HCM', 0.95, 100, NOW()),
('02856786501', 'Gia danh EVN', 0.95, 100, NOW()),
('02899964438', 'Lua dao tuyen dung', 0.95, 100, NOW()),
('02899964437', 'Gia danh buu cuc', 0.95, 100, NOW()),
('02873034653', 'Spam vay tin chap', 0.75, 50, NOW()),
('02899950012', 'Spam ban dat', 0.75, 50, NOW()),
('02873065555', 'Telesale ky nghi', 0.75, 50, NOW()),
('02899964448', 'Gia danh Shopee', 0.95, 100, NOW()),
('02822000266', 'Spam khoa hoc lua dao', 0.75, 50, NOW()),
('0287108690', 'Gia danh ngan hang', 0.95, 100, NOW()),
('02899950015', 'Spam lien tuc', 0.75, 50, NOW()),
('02899958588', 'Lua dao trung thuong', 0.95, 100, NOW()),
('02871099082', 'Spam Forex', 0.75, 50, NOW()),
('02899996142', 'Gia danh Bo Y te', 0.95, 100, NOW());

-- DAU SO 1900 (11 số)
INSERT INTO blacklist (phone_number, reason, confidence_score, reported_count, last_reported_at) VALUES
('19006600', 'Spam dich vu chuyen tien', 0.75, 50, NOW()),
('19001559', 'Spam bao hiem', 0.75, 50, NOW()),
('19002008', 'Telesale bat dong san', 0.75, 50, NOW()),
('19009999', 'Spam quang cao', 0.75, 50, NOW()),
('19008198', 'Spam tong dai gia mao', 0.75, 50, NOW()),
('19001900', 'Spam khao sat lua dao', 0.75, 50, NOW()),
('19003000', 'Spam the tin dung', 0.75, 50, NOW()),
('19005555', 'Robocall quang cao', 0.75, 50, NOW()),
('19007777', 'Spam dau tu chung khoan', 0.75, 50, NOW()),
('19008888', 'Telesale bat dong san', 0.75, 50, NOW()),
('19001234', 'Spam tong hop', 0.75, 50, NOW());

-- Verify
SELECT COUNT(*) as total_fraud_numbers FROM blacklist;
