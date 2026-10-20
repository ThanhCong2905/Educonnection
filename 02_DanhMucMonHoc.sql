-- Danh muc co ban, khong tao tai khoan, ho so hay nhu cau mau.
-- Chay tren KetNoiGiaSu. Chi them mon chua ton tai, giu nguyen du lieu hien co.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT INTO dbo.MonHoc (TenMon, DangSuDung)
SELECT source.TenMon, 1
FROM (VALUES (N'Toán học'), (N'Ngữ văn'), (N'Tiếng Anh'), (N'Vật lý'),
             (N'Hóa học'), (N'Sinh học'), (N'Lịch sử'), (N'Địa lý'), (N'Tin học')) source(TenMon)
WHERE NOT EXISTS (SELECT 1 FROM dbo.MonHoc AS currentSubject WITH (UPDLOCK,HOLDLOCK) WHERE currentSubject.TenMon = source.TenMon);
COMMIT;
