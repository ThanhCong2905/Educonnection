-- SQL Server: chay sau 01, 02, 03 tren database dich dang duoc chon.
-- Migration bo sung, khong xoa bang, nhu cau hay tin nhan cu. Co the chay lai.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.HoSoGiaSu', 'TenHienThi') IS NULL
BEGIN
    ALTER TABLE dbo.HoSoGiaSu ADD TenHienThi nvarchar(150) NOT NULL CONSTRAINT DF_HoSoGiaSu_TenHienThi DEFAULT N'',
        AnhDaiDien nvarchar(500) NULL,
        TruongDaoTao nvarchar(200) NOT NULL CONSTRAINT DF_HoSoGiaSu_Truong DEFAULT N'',
        ChuyenNganh nvarchar(200) NOT NULL CONSTRAINT DF_HoSoGiaSu_Nganh DEFAULT N'',
        TinhTrangHocTap nvarchar(100) NOT NULL CONSTRAINT DF_HoSoGiaSu_HocTap DEFAULT N'';
END;
IF OBJECT_ID('dbo.DieuKienNhanLopGiaSu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DieuKienNhanLopGiaSu (
        HoSoGiaSuId uniqueidentifier NOT NULL CONSTRAINT PK_DieuKienNhanLopGiaSu PRIMARY KEY REFERENCES dbo.HoSoGiaSu(Id),
        DayTrucTuyen bit NOT NULL,
        DayTrucTiep bit NOT NULL,
        KhuVuc nvarchar(200) NULL,
        HocPhi decimal(18,0) NULL,
        DonViHocPhi int NULL,
        DangNhanHocVien bit NOT NULL,
        CapNhatLuc datetimeoffset(7) NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_DieuKienNhanLop_HinhThuc CHECK (DayTrucTuyen = 1 OR DayTrucTiep = 1),
        CONSTRAINT CK_DieuKienNhanLop_KhuVuc CHECK (DayTrucTiep = 0 OR (KhuVuc IS NOT NULL AND LEN(LTRIM(RTRIM(KhuVuc))) > 0)),
        CONSTRAINT CK_DieuKienNhanLop_HocPhi CHECK ((HocPhi IS NULL AND DonViHocPhi IS NULL) OR (HocPhi IS NOT NULL AND HocPhi >= 0 AND DonViHocPhi IS NOT NULL AND DonViHocPhi IN (0,1,2)))
    );
    INSERT dbo.DieuKienNhanLopGiaSu (HoSoGiaSuId,DayTrucTuyen,DayTrucTiep,KhuVuc,HocPhi,DonViHocPhi,DangNhanHocVien,CapNhatLuc)
    SELECT Id,1,0,KhuVuc,HocPhiCongKhai,DonViHocPhi,0,SYSDATETIMEOFFSET() FROM dbo.HoSoGiaSu;
END;
IF OBJECT_ID('dbo.KhungGioRanhGiaSu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KhungGioRanhGiaSu (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_KhungGioRanhGiaSu PRIMARY KEY,
        HoSoGiaSuId uniqueidentifier NOT NULL REFERENCES dbo.DieuKienNhanLopGiaSu(HoSoGiaSuId),
        ThuTrongTuan int NOT NULL CHECK (ThuTrongTuan BETWEEN 0 AND 6),
        GioBatDau time(0) NOT NULL,
        GioKetThuc time(0) NOT NULL,
        CONSTRAINT CK_KhungGioRanhGiaSu_Gio CHECK (GioBatDau < GioKetThuc),
        CONSTRAINT UQ_KhungGioRanhGiaSu UNIQUE (HoSoGiaSuId,ThuTrongTuan,GioBatDau)
    );
END;
IF OBJECT_ID('dbo.YeuCauCapNhatHoSoGiaSu', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.YeuCauCapNhatHoSoGiaSu (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_YeuCauCapNhatHoSoGiaSu PRIMARY KEY,
        HoSoGiaSuId uniqueidentifier NOT NULL REFERENCES dbo.HoSoGiaSu(Id),
        TenHienThi nvarchar(150) NOT NULL,
        AnhDaiDien nvarchar(500) NULL,
        TrinhDo nvarchar(200) NOT NULL,
        TruongDaoTao nvarchar(200) NOT NULL,
        ChuyenNganh nvarchar(200) NOT NULL,
        TinhTrangHocTap nvarchar(100) NOT NULL,
        ChuyenMon nvarchar(500) NOT NULL,
        KinhNghiem nvarchar(2000) NOT NULL,
        MoTa nvarchar(4000) NOT NULL,
        MonDayJson nvarchar(max) NOT NULL CHECK (ISJSON(MonDayJson) = 1),
        MinhChungJson nvarchar(max) NOT NULL CHECK (ISJSON(MinhChungJson) = 1),
        TrangThai int NOT NULL CHECK (TrangThai IN (0,1,2)),
        LyDoTuChoi nvarchar(1000) NULL,
        NguoiDuyetId uniqueidentifier NULL REFERENCES dbo.AspNetUsers(Id),
        DuyetLuc datetimeoffset(7) NULL,
        TaoLuc datetimeoffset(7) NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT CK_YeuCauHoSo_Duyet CHECK ((TrangThai=0 AND NguoiDuyetId IS NULL AND DuyetLuc IS NULL) OR (TrangThai IN (1,2) AND NguoiDuyetId IS NOT NULL AND DuyetLuc IS NOT NULL))
    );
    CREATE UNIQUE INDEX UX_YeuCauHoSo_ChoDuyet ON dbo.YeuCauCapNhatHoSoGiaSu(HoSoGiaSuId) WHERE TrangThai = 0;
END;
IF OBJECT_ID('dbo.YeuCauLienHe', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.YeuCauLienHe (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_YeuCauLienHe PRIMARY KEY,
        GiaSuId uniqueidentifier NOT NULL REFERENCES dbo.AspNetUsers(Id),
        BenHocId uniqueidentifier NOT NULL REFERENCES dbo.AspNetUsers(Id),
        NguoiGuiId uniqueidentifier NOT NULL REFERENCES dbo.AspNetUsers(Id),
        LoiNhan nvarchar(2000) NOT NULL CHECK (LEN(LTRIM(RTRIM(LoiNhan))) > 0),
        TrangThai int NOT NULL CHECK (TrangThai IN (0,1,2,3)),
        GuiLuc datetimeoffset(7) NOT NULL,
        PhanHoiLuc datetimeoffset(7) NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_YeuCauLienHe_Cap UNIQUE (GiaSuId,BenHocId),
        CONSTRAINT CK_YeuCauLienHe_HaiBen CHECK (GiaSuId <> BenHocId AND NguoiGuiId IN (GiaSuId,BenHocId))
    );
END;
COMMIT;
GO
-- Batch rieng: cac cot moi da duoc SQL Server nhan dien.
UPDATE p SET TenHienThi=u.HoTen, AnhDaiDien=u.AnhDaiDien
FROM dbo.HoSoGiaSu p JOIN dbo.AspNetUsers u ON p.GiaSuId=u.Id WHERE p.TenHienThi=N'';
GO
