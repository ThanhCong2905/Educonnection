-- SQL Server 2022. Khoi tao tren database MOI; khong xoa database dang co.
-- PHIEN BAN v3 - phuong an chot 20/10/2026.
-- LichHoc: khung lich theo tuan; BuoiHoc: tung buoi cu the.
-- Script khoi tao database MOI, khong phai migration.

USE master;

GO

IF DB_ID(N'KetNoiGiaSu') IS NOT NULL BEGIN RAISERROR(N'Database da ton tai. Script bi vo hieu hoa; dung migration rieng.', 16, 1); SET NOEXEC ON; END;

GO

CREATE DATABASE KetNoiGiaSu;

GO

USE KetNoiGiaSu;

GO

SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET QUOTED_IDENTIFIER ON;

SET ANSI_NULLS ON;

SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

CREATE TABLE dbo.HoSoGiaSu (
    [Id] uniqueidentifier NOT NULL,
    [GiaSuId] uniqueidentifier NOT NULL,
    [TrinhDo] nvarchar(200) NOT NULL,
    [ChuyenMon] nvarchar(500) NOT NULL,
    [KinhNghiem] nvarchar(2000) NOT NULL,
    [MoTa] nvarchar(4000) NOT NULL,
    [KhuVuc] nvarchar(200) NOT NULL,
    -- Hoc phi cong khai bang VND; 0 = moi gio, 1 = moi buoi, 2 = moi thang.
    [HocPhiCongKhai] decimal(18,0) NOT NULL,
    [DonViHocPhi] int NOT NULL,
    [TrangThaiDuyet] int NOT NULL,
    [LyDoTuChoi] nvarchar(1000) NULL,
    [NguoiDuyetId] uniqueidentifier NULL,
    [DuyetLuc] datetimeoffset(7) NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL CONSTRAINT DF_HoSoGiaSu_TaoLuc DEFAULT TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_HoSoGiaSu PRIMARY KEY ([Id])
);

CREATE TABLE dbo.MonHoc (
    [Id] int IDENTITY(1,1) NOT NULL,
    [TenMon] nvarchar(100) NOT NULL,
    [DangSuDung] bit NOT NULL,
    CONSTRAINT PK_MonHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.HoSoGiaSuMonDay (
    [HoSoGiaSuId] uniqueidentifier NOT NULL,
    [MonHocId] int NOT NULL,
    [PhamViDay] nvarchar(500) NOT NULL,
    CONSTRAINT PK_HoSoGiaSuMonDay PRIMARY KEY ([HoSoGiaSuId], [MonHocId])
);

CREATE TABLE dbo.NhuCauHoc (
    [Id] uniqueidentifier NOT NULL,
    [ChuSoHuuId] uniqueidentifier NOT NULL,
    [LoaiNhuCau] int NOT NULL,
    [MonHocId] int NOT NULL,
    [KhoiLop] nvarchar(100) NULL,
    [HocLuc] nvarchar(200) NULL,
    [MucTieu] nvarchar(2000) NULL,
    [SoBuoiTuan] int NULL,
    [HinhThucHoc] int NOT NULL,
    [KhuVuc] nvarchar(200) NULL,
    [DiaChiChiTiet] nvarchar(500) NULL,
    [HocPhiDuKien] decimal(18,0) NULL,
    [DonViHocPhi] int NULL,
    [TenNguoiHoc] nvarchar(150) NULL,
    [TuoiNguoiHoc] int NULL,
    [TrangThai] int NOT NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL CONSTRAINT DF_NhuCauHoc_TaoLuc DEFAULT TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_NhuCauHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.KhungGioRanh (
    [Id] uniqueidentifier NOT NULL,
    [NhuCauId] uniqueidentifier NOT NULL,
    [ThuTrongTuan] int NOT NULL,
    [GioBatDau] time(0) NOT NULL,
    [GioKetThuc] time(0) NOT NULL,
    CONSTRAINT PK_KhungGioRanh PRIMARY KEY ([Id])
);

CREATE TABLE dbo.YeuCauKetNoiLopHoc (
    [Id] uniqueidentifier NOT NULL,
    [GiaSuId] uniqueidentifier NOT NULL,
    [BenHocId] uniqueidentifier NOT NULL,
    [NguoiGuiId] uniqueidentifier NOT NULL,
    [NhuCauId] uniqueidentifier NULL,
    [LoiNhan] nvarchar(2000) NULL,
    [TrangThai] int NOT NULL,
    [GuiLuc] datetimeoffset(7) NOT NULL,
    [HetHanLuc] datetimeoffset(7) NOT NULL,
    [TenLopDuKien] nvarchar(200) NULL,
    [MonHocId] int NULL,
    [MucTieu] nvarchar(2000) NULL,
    [SoBuoiTuan] int NULL,
    [ThoiLuongPhut] int NULL,
    [LichDuKien] nvarchar(2000) NULL,
    [BatDauHocLuc] datetimeoffset(7) NULL,
    [HinhThucHoc] int NULL,
    [DiaDiem] nvarchar(500) NULL,
    [HocPhi] decimal(18,0) NULL,
    [DonViHocPhi] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_YeuCauKetNoiLopHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.LopHoc (
    [Id] uniqueidentifier NOT NULL,
    [YeuCauKetNoiId] uniqueidentifier NOT NULL,
    [GiaSuId] uniqueidentifier NOT NULL,
    [BenHocId] uniqueidentifier NOT NULL,
    [TenLop] nvarchar(200) NOT NULL,
    [MonHocId] int NOT NULL,
    [MucTieu] nvarchar(2000) NOT NULL,
    [SoBuoiTuan] int NOT NULL,
    [ThoiLuongPhut] int NOT NULL,
    [HinhThucHoc] int NOT NULL,
    [DiaDiem] nvarchar(500) NULL,
    [HocPhi] decimal(18,0) NOT NULL,
    [DonViHocPhi] int NOT NULL,
    [TrangThai] int NOT NULL,
    [GiaSuXacNhanKetThucLuc] datetimeoffset(7) NULL,
    [BenHocXacNhanKetThucLuc] datetimeoffset(7) NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL CONSTRAINT DF_LopHoc_TaoLuc DEFAULT TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_LopHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.YeuCauHuyKetNoi (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [NguoiGuiId] uniqueidentifier NOT NULL,
    [LyDo] nvarchar(1000) NOT NULL,
    [TrangThai] int NOT NULL,
    [GuiLuc] datetimeoffset(7) NOT NULL,
    [HetHanLuc] datetimeoffset(7) NOT NULL,
    [PhanHoiLuc] datetimeoffset(7) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_YeuCauHuyKetNoi PRIMARY KEY ([Id])
);

CREATE TABLE dbo.ThanhVienLopHoc (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [TaiKhoanId] uniqueidentifier NOT NULL,
    [NguoiDeNghiId] uniqueidentifier NOT NULL,
    [TrangThai] int NOT NULL,
    [MucQuyen] int NULL,
    [DeNghiLuc] datetimeoffset(7) NOT NULL,
    [DuyetLuc] datetimeoffset(7) NULL,
    [RoiLopLuc] datetimeoffset(7) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_ThanhVienLopHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.LichHoc (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [TieuDe] nvarchar(200) NOT NULL,
    [ThuTrongTuan] int NOT NULL,
    [GioBatDau] time(0) NOT NULL,
    [GioKetThuc] time(0) NOT NULL,
    [TuNgay] date NOT NULL,
    [DenNgay] date NULL,
    [TrangThai] int NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_LichHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.BuoiHoc (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [LichHocId] uniqueidentifier NULL,
    [BuoiHocGocId] uniqueidentifier NULL,
    [NgayTheoLich] date NULL,
    [TieuDe] nvarchar(200) NOT NULL,
    [BatDauLuc] datetimeoffset(7) NOT NULL,
    [KetThucLuc] datetimeoffset(7) NOT NULL,
    [LaBuoiBu] bit NOT NULL,
    [TrangThai] int NOT NULL,
    [NoiDungGiangDay] nvarchar(4000) NULL,
    [NhanXet] nvarchar(2000) NULL,
    [TrangThaiThamDu] int NULL,
    [GhiNhanLuc] datetimeoffset(7) NULL,
    [NguoiGhiNhanId] uniqueidentifier NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_BuoiHoc PRIMARY KEY ([Id])
);

CREATE TABLE dbo.BaiTapGiao (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [BuoiHocId] uniqueidentifier NULL,
    [NguoiGiaoId] uniqueidentifier NOT NULL,
    [TieuDe] nvarchar(200) NOT NULL,
    [NoiDung] nvarchar(4000) NOT NULL,
    [HanNopLuc] datetimeoffset(7) NOT NULL,
    [TieuChiCham] nvarchar(2000) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_BaiTapGiao PRIMARY KEY ([Id])
);

CREATE TABLE dbo.BaiTapNop (
    [Id] uniqueidentifier NOT NULL,
    [BaiTapGiaoId] uniqueidentifier NOT NULL,
    [NguoiNopId] uniqueidentifier NOT NULL,
    [NoiDung] nvarchar(4000) NULL,
    [NopLuc] datetimeoffset(7) NOT NULL,
    [MaThaoTac] uniqueidentifier NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [SoLanNop] int NOT NULL,
    CONSTRAINT PK_BaiTapNop PRIMARY KEY ([Id])
);

CREATE TABLE dbo.KetQuaChamBai (
    [Id] uniqueidentifier NOT NULL,
    [BaiTapNopId] uniqueidentifier NOT NULL,
    [NguoiChamId] uniqueidentifier NOT NULL,
    [Diem] decimal(4,2) NULL,
    [NhanXet] nvarchar(2000) NOT NULL,
    [ChamLuc] datetimeoffset(7) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_KetQuaChamBai PRIMARY KEY ([Id])
);

CREATE TABLE dbo.KetQuaKiemTra (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [TenBaiKiemTra] nvarchar(200) NOT NULL,
    [NgayKiemTra] date NOT NULL,
    [Diem] decimal(4,2) NOT NULL,
    [NhanXet] nvarchar(2000) NULL,
    [NguoiGhiId] uniqueidentifier NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_KetQuaKiemTra PRIMARY KEY ([Id])
);

CREATE TABLE dbo.CuocHoiThoai (
    [Id] uniqueidentifier NOT NULL,
    [LoaiHoiThoai] int NOT NULL,
    [GiaSuId] uniqueidentifier NULL,
    [BenHocId] uniqueidentifier NULL,
    [LopHocId] uniqueidentifier NULL,
    [HoiThoaiTruocKetNoiId] uniqueidentifier NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL,
    CONSTRAINT PK_CuocHoiThoai PRIMARY KEY ([Id])
);

CREATE TABLE dbo.TinNhan (
    [Id] uniqueidentifier NOT NULL,
    [CuocHoiThoaiId] uniqueidentifier NOT NULL,
    [NguoiGuiId] uniqueidentifier NOT NULL,
    [NoiDung] nvarchar(4000) NOT NULL,
    [GuiLuc] datetimeoffset(7) NOT NULL,
    [MaThaoTac] uniqueidentifier NOT NULL,
    CONSTRAINT PK_TinNhan PRIMARY KEY NONCLUSTERED ([Id])
);

CREATE TABLE dbo.PhieuDanhGia (
    [Id] uniqueidentifier NOT NULL,
    [LopHocId] uniqueidentifier NOT NULL,
    [NguoiGuiId] uniqueidentifier NOT NULL,
    [MocDanhGia] nvarchar(200) NOT NULL,
    [NoiDung] nvarchar(4000) NOT NULL,
    [MucDoHaiLong] int NULL,
    [GuiLuc] datetimeoffset(7) NOT NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_PhieuDanhGia PRIMARY KEY ([Id])
);

CREATE TABLE dbo.BaoCaoViPham (
    [Id] uniqueidentifier NOT NULL,
    [NguoiBaoCaoId] uniqueidentifier NOT NULL,
    [LoaiDoiTuong] int NOT NULL,
    [LyDo] nvarchar(1000) NOT NULL,
    [MoTa] nvarchar(4000) NOT NULL,
    [TrangThai] int NOT NULL,
    [NguoiXuLyId] uniqueidentifier NULL,
    [KetLuan] nvarchar(2000) NULL,
    [XuLyLuc] datetimeoffset(7) NULL,
    [RowVersion] rowversion NOT NULL,
    [TaiKhoanId] uniqueidentifier NULL,
    [HoSoGiaSuId] uniqueidentifier NULL,
    [NhuCauHocId] uniqueidentifier NULL,
    [LopHocId] uniqueidentifier NULL,
    CONSTRAINT PK_BaoCaoViPham PRIMARY KEY ([Id])
);

CREATE TABLE dbo.ThongBao (
    [Id] uniqueidentifier NOT NULL,
    [NguoiNhanId] uniqueidentifier NOT NULL,
    [LoaiThongBao] int NOT NULL,
    [DoiTuongId] uniqueidentifier NOT NULL,
    [NoiDungTomTat] nvarchar(500) NOT NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL,
    [DaDocLuc] datetimeoffset(7) NULL,
    [MaSuKien] uniqueidentifier NOT NULL,
    CONSTRAINT PK_ThongBao PRIMARY KEY ([Id])
);

CREATE TABLE dbo.TepDinhKem (
    [Id] uniqueidentifier NOT NULL,
    [NguoiTaiLenId] uniqueidentifier NOT NULL,
    [TenGoc] nvarchar(255) NOT NULL,
    [TenLuuTru] nvarchar(450) NOT NULL,
    [LoaiNoiDung] nvarchar(100) NOT NULL,
    [KichThuoc] bigint NOT NULL,
    [Checksum] char(64) NOT NULL,
    [TrangThaiQuet] int NOT NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL,
    [HoSoGiaSuId] uniqueidentifier NULL,
    [BaiTapGiaoId] uniqueidentifier NULL,
    [BaiTapNopId] uniqueidentifier NULL,
    [TinNhanId] uniqueidentifier NULL,
    [BaoCaoViPhamId] uniqueidentifier NULL,
    CONSTRAINT PK_TepDinhKem PRIMARY KEY ([Id])
);

CREATE TABLE dbo.PhienDangNhap (
    [Id] uniqueidentifier NOT NULL,
    [TaiKhoanId] uniqueidentifier NOT NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL,
    [HoatDongCuoiLuc] datetimeoffset(7) NOT NULL,
    [ThuHoiLuc] datetimeoffset(7) NULL,
    [LyDoThuHoi] nvarchar(500) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_PhienDangNhap PRIMARY KEY ([Id])
);

CREATE TABLE dbo.TrangThaiThuDangNhap (
    [LoaiKhoa] int NOT NULL,
    [GiaTriKhoa] nvarchar(256) NOT NULL,
    [SoLanSai] int NOT NULL,
    [LanSaiCuoiLuc] datetimeoffset(7) NOT NULL,
    [ChoDenLuc] datetimeoffset(7) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_TrangThaiThuDangNhap PRIMARY KEY ([LoaiKhoa], [GiaTriKhoa])
);

CREATE TABLE dbo.SuKienChoXuLy (
    [Id] uniqueidentifier NOT NULL,
    [LoaiSuKien] nvarchar(150) NOT NULL,
    [DuLieuToiThieu] nvarchar(max) NOT NULL,
    [TaoLuc] datetimeoffset(7) NOT NULL,
    [TrangThai] int NOT NULL,
    [SoLanThu] int NOT NULL,
    [ThuLaiLuc] datetimeoffset(7) NULL,
    [XuLyXongLuc] datetimeoffset(7) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT PK_SuKienChoXuLy PRIMARY KEY ([Id])
);

CREATE TABLE dbo.NhatKyQuanTri (
    [Id] uniqueidentifier NOT NULL,
    [NguoiThucHienId] uniqueidentifier NOT NULL,
    [ThoiDiem] datetimeoffset(7) NOT NULL,
    [HanhDong] nvarchar(200) NOT NULL,
    [LoaiDoiTuong] nvarchar(100) NOT NULL,
    [DoiTuongId] nvarchar(100) NOT NULL,
    [KetQua] nvarchar(1000) NOT NULL,
    [MaLienKetYeuCau] nvarchar(100) NOT NULL,
    CONSTRAINT PK_NhatKyQuanTri PRIMARY KEY ([Id])
);

CREATE TABLE dbo.AspNetUsers (
    [Id] uniqueidentifier NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NOT NULL,
    [NormalizedEmail] nvarchar(256) NOT NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset(7) NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    [HoTen] nvarchar(150) NOT NULL,
    [AnhDaiDien] nvarchar(500) NULL,
    [TrangThai] int NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY ([Id])
);

CREATE TABLE dbo.AspNetRoles (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY ([Id])
);

CREATE TABLE dbo.AspNetUserClaims (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY ([Id])
);

CREATE TABLE dbo.AspNetRoleClaims (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY ([Id])
);

CREATE TABLE dbo.AspNetUserRoles (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY ([UserId], [RoleId])
);

CREATE TABLE dbo.AspNetUserLogins (
    [LoginProvider] nvarchar(128) NOT NULL,
    [ProviderKey] nvarchar(128) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY ([LoginProvider], [ProviderKey])
);

CREATE TABLE dbo.AspNetUserTokens (
    [UserId] uniqueidentifier NOT NULL,
    [LoginProvider] nvarchar(128) NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY ([UserId], [LoginProvider], [Name])
);

ALTER TABLE dbo.LichHoc ADD CONSTRAINT UQ_LichHoc_Id_LopHocId UNIQUE (Id, LopHocId);
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT UQ_BuoiHoc_Id_LopHocId UNIQUE (Id, LopHocId);

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT UQ_YeuCauKetNoiLopHoc_Id_GiaSuId_BenHocId UNIQUE (Id, GiaSuId, BenHocId);

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT FK_HoSoGiaSu_AspNetUsers_NguoiDuyetId FOREIGN KEY ([NguoiDuyetId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT FK_HoSoGiaSu_AspNetUsers_GiaSuId FOREIGN KEY ([GiaSuId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.HoSoGiaSuMonDay ADD CONSTRAINT FK_HoSoGiaSuMonDay_HoSoGiaSu_HoSoGiaSuId FOREIGN KEY ([HoSoGiaSuId]) REFERENCES dbo.HoSoGiaSu (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.HoSoGiaSuMonDay ADD CONSTRAINT FK_HoSoGiaSuMonDay_MonHoc_MonHocId FOREIGN KEY ([MonHocId]) REFERENCES dbo.MonHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT FK_NhuCauHoc_AspNetUsers_ChuSoHuuId FOREIGN KEY ([ChuSoHuuId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT FK_NhuCauHoc_MonHoc_MonHocId FOREIGN KEY ([MonHocId]) REFERENCES dbo.MonHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.KhungGioRanh ADD CONSTRAINT FK_KhungGioRanh_NhuCauHoc_NhuCauId FOREIGN KEY ([NhuCauId]) REFERENCES dbo.NhuCauHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT FK_YeuCauKetNoiLopHoc_AspNetUsers_GiaSuId FOREIGN KEY ([GiaSuId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT FK_YeuCauKetNoiLopHoc_AspNetUsers_BenHocId FOREIGN KEY ([BenHocId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT FK_YeuCauKetNoiLopHoc_AspNetUsers_NguoiGuiId FOREIGN KEY ([NguoiGuiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT FK_YeuCauKetNoiLopHoc_NhuCauHoc_NhuCauId FOREIGN KEY ([NhuCauId]) REFERENCES dbo.NhuCauHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT FK_YeuCauKetNoiLopHoc_MonHoc_MonHocId FOREIGN KEY ([MonHocId]) REFERENCES dbo.MonHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.LopHoc ADD CONSTRAINT FK_LopHoc_YeuCauKetNoiLopHoc_YeuCauKetNoiId FOREIGN KEY ([YeuCauKetNoiId], [GiaSuId], [BenHocId]) REFERENCES dbo.YeuCauKetNoiLopHoc (Id, GiaSuId, BenHocId) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.LopHoc ADD CONSTRAINT FK_LopHoc_AspNetUsers_GiaSuId FOREIGN KEY ([GiaSuId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.LopHoc ADD CONSTRAINT FK_LopHoc_AspNetUsers_BenHocId FOREIGN KEY ([BenHocId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.LopHoc ADD CONSTRAINT FK_LopHoc_MonHoc_MonHocId FOREIGN KEY ([MonHocId]) REFERENCES dbo.MonHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauHuyKetNoi ADD CONSTRAINT FK_YeuCauHuyKetNoi_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.YeuCauHuyKetNoi ADD CONSTRAINT FK_YeuCauHuyKetNoi_AspNetUsers_NguoiGuiId FOREIGN KEY ([NguoiGuiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT FK_ThanhVienLopHoc_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT FK_ThanhVienLopHoc_AspNetUsers_TaiKhoanId FOREIGN KEY ([TaiKhoanId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT FK_ThanhVienLopHoc_AspNetUsers_NguoiDeNghiId FOREIGN KEY ([NguoiDeNghiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.LichHoc ADD CONSTRAINT FK_LichHoc_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT FK_BuoiHoc_LichHoc_LichHocId FOREIGN KEY ([LichHocId], [LopHocId]) REFERENCES dbo.LichHoc (Id, LopHocId) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT FK_BuoiHoc_AspNetUsers_NguoiGhiNhanId FOREIGN KEY ([NguoiGhiNhanId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaiTapGiao ADD CONSTRAINT FK_BaiTapGiao_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaiTapGiao ADD CONSTRAINT FK_BaiTapGiao_BuoiHoc_BuoiHocId FOREIGN KEY ([BuoiHocId], [LopHocId]) REFERENCES dbo.BuoiHoc (Id, LopHocId) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaiTapGiao ADD CONSTRAINT FK_BaiTapGiao_AspNetUsers_NguoiGiaoId FOREIGN KEY ([NguoiGiaoId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaiTapNop ADD CONSTRAINT FK_BaiTapNop_BaiTapGiao_BaiTapGiaoId FOREIGN KEY ([BaiTapGiaoId]) REFERENCES dbo.BaiTapGiao (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaiTapNop ADD CONSTRAINT FK_BaiTapNop_AspNetUsers_NguoiNopId FOREIGN KEY ([NguoiNopId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.KetQuaChamBai ADD CONSTRAINT FK_KetQuaChamBai_BaiTapNop_BaiTapNopId FOREIGN KEY ([BaiTapNopId]) REFERENCES dbo.BaiTapNop (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.KetQuaChamBai ADD CONSTRAINT FK_KetQuaChamBai_AspNetUsers_NguoiChamId FOREIGN KEY ([NguoiChamId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.KetQuaKiemTra ADD CONSTRAINT FK_KetQuaKiemTra_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.KetQuaKiemTra ADD CONSTRAINT FK_KetQuaKiemTra_AspNetUsers_NguoiGhiId FOREIGN KEY ([NguoiGhiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT FK_CuocHoiThoai_AspNetUsers_GiaSuId FOREIGN KEY ([GiaSuId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT FK_CuocHoiThoai_AspNetUsers_BenHocId FOREIGN KEY ([BenHocId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT FK_CuocHoiThoai_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT FK_CuocHoiThoai_CuocHoiThoai_HoiThoaiTruocKetNoiId FOREIGN KEY ([HoiThoaiTruocKetNoiId]) REFERENCES dbo.CuocHoiThoai (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TinNhan ADD CONSTRAINT FK_TinNhan_CuocHoiThoai_CuocHoiThoaiId FOREIGN KEY ([CuocHoiThoaiId]) REFERENCES dbo.CuocHoiThoai (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TinNhan ADD CONSTRAINT FK_TinNhan_AspNetUsers_NguoiGuiId FOREIGN KEY ([NguoiGuiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.PhieuDanhGia ADD CONSTRAINT FK_PhieuDanhGia_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.PhieuDanhGia ADD CONSTRAINT FK_PhieuDanhGia_AspNetUsers_NguoiGuiId FOREIGN KEY ([NguoiGuiId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_AspNetUsers_NguoiBaoCaoId FOREIGN KEY ([NguoiBaoCaoId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_AspNetUsers_NguoiXuLyId FOREIGN KEY ([NguoiXuLyId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_AspNetUsers_TaiKhoanId FOREIGN KEY ([TaiKhoanId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_HoSoGiaSu_HoSoGiaSuId FOREIGN KEY ([HoSoGiaSuId]) REFERENCES dbo.HoSoGiaSu (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_NhuCauHoc_NhuCauHocId FOREIGN KEY ([NhuCauHocId]) REFERENCES dbo.NhuCauHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT FK_BaoCaoViPham_LopHoc_LopHocId FOREIGN KEY ([LopHocId]) REFERENCES dbo.LopHoc (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.ThongBao ADD CONSTRAINT FK_ThongBao_AspNetUsers_NguoiNhanId FOREIGN KEY ([NguoiNhanId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.ThongBao ADD CONSTRAINT FK_ThongBao_SuKienChoXuLy_MaSuKien FOREIGN KEY ([MaSuKien]) REFERENCES dbo.SuKienChoXuLy (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_AspNetUsers_NguoiTaiLenId FOREIGN KEY ([NguoiTaiLenId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_HoSoGiaSu_HoSoGiaSuId FOREIGN KEY ([HoSoGiaSuId]) REFERENCES dbo.HoSoGiaSu (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_BaiTapGiao_BaiTapGiaoId FOREIGN KEY ([BaiTapGiaoId]) REFERENCES dbo.BaiTapGiao (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_BaiTapNop_BaiTapNopId FOREIGN KEY ([BaiTapNopId]) REFERENCES dbo.BaiTapNop (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_TinNhan_TinNhanId FOREIGN KEY ([TinNhanId]) REFERENCES dbo.TinNhan (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT FK_TepDinhKem_BaoCaoViPham_BaoCaoViPhamId FOREIGN KEY ([BaoCaoViPhamId]) REFERENCES dbo.BaoCaoViPham (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.PhienDangNhap ADD CONSTRAINT FK_PhienDangNhap_AspNetUsers_TaiKhoanId FOREIGN KEY ([TaiKhoanId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.NhatKyQuanTri ADD CONSTRAINT FK_NhatKyQuanTri_AspNetUsers_NguoiThucHienId FOREIGN KEY ([NguoiThucHienId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetUserClaims ADD CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY ([UserId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetRoleClaims ADD CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY ([RoleId]) REFERENCES dbo.AspNetRoles (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY ([UserId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY ([RoleId]) REFERENCES dbo.AspNetRoles (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetUserLogins ADD CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY ([UserId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.AspNetUserTokens ADD CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY ([UserId]) REFERENCES dbo.AspNetUsers (Id) ON DELETE NO ACTION ON UPDATE NO ACTION;

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_TrangThaiDuyet CHECK (TrangThaiDuyet IN (0,1,2));

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_HocPhiCongKhaiKhongAm CHECK (HocPhiCongKhai >= 0);

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_DonViHocPhi CHECK (DonViHocPhi IN (0,1,2));

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_LoaiNhuCau CHECK (LoaiNhuCau IN (0,1));

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_SoBuoiTuanHopLe CHECK (SoBuoiTuan BETWEEN 1 AND 7);

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_HinhThucHoc CHECK (HinhThucHoc IN (0,1));

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_HocPhiDuKienKhongAm CHECK (HocPhiDuKien >= 0);

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_TuoiNguoiHocHopLe CHECK (TuoiNguoiHoc BETWEEN 1 AND 100);

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_TrangThai CHECK (TrangThai IN (0,1,2));

ALTER TABLE dbo.KhungGioRanh ADD CONSTRAINT CK_KhungGioRanh_ThuTrongTuan CHECK (ThuTrongTuan IN (0,1,2,3,4,5,6));

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_TrangThai CHECK (TrangThai IN (0,1,2,3,4,5));

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_SoBuoiTuanHopLe CHECK (SoBuoiTuan BETWEEN 1 AND 7);

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_ThoiLuongPhutHopLe CHECK (ThoiLuongPhut BETWEEN 1 AND 480);

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_HinhThucHoc CHECK (HinhThucHoc IN (0,1));

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_HocPhiKhongAm CHECK (HocPhi >= 0);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_SoBuoiTuanHopLe CHECK (SoBuoiTuan BETWEEN 1 AND 7);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_ThoiLuongPhutHopLe CHECK (ThoiLuongPhut BETWEEN 1 AND 480);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_HinhThucHoc CHECK (HinhThucHoc IN (0,1));

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_HocPhiKhongAm CHECK (HocPhi >= 0);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_TrangThai CHECK (TrangThai IN (0,1,2,3));

ALTER TABLE dbo.YeuCauHuyKetNoi ADD CONSTRAINT CK_YeuCauHuyKetNoi_TrangThai CHECK (TrangThai IN (0,1,2,3));

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT CK_ThanhVienLopHoc_TrangThai CHECK (TrangThai IN (0,1,2,3));

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT CK_ThanhVienLopHoc_MucQuyen CHECK (MucQuyen IN (0,1));

ALTER TABLE dbo.LichHoc ADD CONSTRAINT CK_LichHoc_TrangThai CHECK (TrangThai IN (0,1,2));

ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_TrangThaiThamDu CHECK (TrangThaiThamDu IN (0,1,2));

ALTER TABLE dbo.BaiTapNop ADD CONSTRAINT CK_BaiTapNop_SoLanNopDuong CHECK (SoLanNop > 0);

ALTER TABLE dbo.KetQuaChamBai ADD CONSTRAINT CK_KetQuaChamBai_DiemHopLe CHECK (Diem BETWEEN 0 AND 10);

ALTER TABLE dbo.KetQuaKiemTra ADD CONSTRAINT CK_KetQuaKiemTra_DiemHopLe CHECK (Diem BETWEEN 0 AND 10);

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT CK_CuocHoiThoai_LoaiHoiThoai CHECK (LoaiHoiThoai IN (0,1));

ALTER TABLE dbo.PhieuDanhGia ADD CONSTRAINT CK_PhieuDanhGia_MucDoHaiLongHopLe CHECK (MucDoHaiLong BETWEEN 1 AND 5);

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT CK_BaoCaoViPham_LoaiDoiTuong CHECK (LoaiDoiTuong IN (0,1,2,3));

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT CK_BaoCaoViPham_TrangThai CHECK (TrangThai IN (0,1,2));

ALTER TABLE dbo.ThongBao ADD CONSTRAINT CK_ThongBao_LoaiThongBao CHECK (LoaiThongBao IN (0,1,2,3,4,5));

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT CK_TepDinhKem_TrangThaiQuet CHECK (TrangThaiQuet IN (0,1,2,3));

ALTER TABLE dbo.TrangThaiThuDangNhap ADD CONSTRAINT CK_TrangThaiThuDangNhap_LoaiKhoa CHECK (LoaiKhoa IN (0,1));

ALTER TABLE dbo.TrangThaiThuDangNhap ADD CONSTRAINT CK_TrangThaiThuDangNhap_SoLanSaiKhongAm CHECK (SoLanSai >= 0);

ALTER TABLE dbo.SuKienChoXuLy ADD CONSTRAINT CK_SuKienChoXuLy_TrangThai CHECK (TrangThai IN (0,1,2,3));

ALTER TABLE dbo.SuKienChoXuLy ADD CONSTRAINT CK_SuKienChoXuLy_SoLanThuKhongAm CHECK (SoLanThu >= 0);

ALTER TABLE dbo.AspNetUsers ADD CONSTRAINT CK_AspNetUsers_TrangThai CHECK (TrangThai IN (0,1));

ALTER TABLE dbo.AspNetUsers ADD CONSTRAINT CK_AspNetUsers_AccessFailedCount CHECK (AccessFailedCount >= 0);

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT CK_BaoCaoViPham_DoiTuongHopLe CHECK ((LoaiDoiTuong = 0 AND TaiKhoanId IS NOT NULL AND HoSoGiaSuId IS NULL AND NhuCauHocId IS NULL AND LopHocId IS NULL) OR (LoaiDoiTuong = 1 AND TaiKhoanId IS NULL AND HoSoGiaSuId IS NOT NULL AND NhuCauHocId IS NULL AND LopHocId IS NULL) OR (LoaiDoiTuong = 2 AND TaiKhoanId IS NULL AND HoSoGiaSuId IS NULL AND NhuCauHocId IS NOT NULL AND LopHocId IS NULL) OR (LoaiDoiTuong = 3 AND TaiKhoanId IS NULL AND HoSoGiaSuId IS NULL AND NhuCauHocId IS NULL AND LopHocId IS NOT NULL));

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT CK_TepDinhKem_MotChuSoHuu CHECK (CASE WHEN HoSoGiaSuId IS NULL THEN 0 ELSE 1 END + CASE WHEN BaiTapGiaoId IS NULL THEN 0 ELSE 1 END + CASE WHEN BaiTapNopId IS NULL THEN 0 ELSE 1 END + CASE WHEN TinNhanId IS NULL THEN 0 ELSE 1 END + CASE WHEN BaoCaoViPhamId IS NULL THEN 0 ELSE 1 END = 1);

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT CK_TepDinhKem_KichThuoc CHECK (KichThuoc BETWEEN 1 AND 10000000);

ALTER TABLE dbo.KhungGioRanh ADD CONSTRAINT CK_KhungGioRanh_ThuTuGio CHECK (GioBatDau < GioKetThuc);



ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_HanPhanHoi CHECK (HetHanLuc > GuiLuc);

ALTER TABLE dbo.YeuCauHuyKetNoi ADD CONSTRAINT CK_YeuCauHuyKetNoi_HanPhanHoi CHECK (HetHanLuc > GuiLuc);

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_HaiBenKhacNhau CHECK (GiaSuId <> BenHocId);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_HaiBenKhacNhau CHECK (GiaSuId <> BenHocId);

ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_NguoiGuiHopLe CHECK (NguoiGuiId IN (GiaSuId, BenHocId));

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT CK_ThanhVienLopHoc_QuyenKhiDuyet CHECK (TrangThai <> 1 OR (MucQuyen IS NOT NULL AND DuyetLuc IS NOT NULL));

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT CK_CuocHoiThoai_CauTruc CHECK ((LoaiHoiThoai=0 AND GiaSuId IS NOT NULL AND BenHocId IS NOT NULL AND GiaSuId<>BenHocId AND LopHocId IS NULL AND HoiThoaiTruocKetNoiId IS NULL) OR (LoaiHoiThoai=1 AND LopHocId IS NOT NULL AND GiaSuId IS NULL AND BenHocId IS NULL));

ALTER TABLE dbo.CuocHoiThoai ADD CONSTRAINT CK_CuocHoiThoai_KhongTuThamChieu CHECK (HoiThoaiTruocKetNoiId IS NULL OR HoiThoaiTruocKetNoiId <> Id);

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_NguoiDuyetDayDu CHECK ((NguoiDuyetId IS NULL AND DuyetLuc IS NULL) OR (NguoiDuyetId IS NOT NULL AND DuyetLuc IS NOT NULL));

ALTER TABLE dbo.BaoCaoViPham ADD CONSTRAINT CK_BaoCaoViPham_XuLyDayDu CHECK (
 (TrangThai=0 AND NguoiXuLyId IS NULL AND XuLyLuc IS NULL)
 OR (TrangThai=1 AND NguoiXuLyId IS NOT NULL AND XuLyLuc IS NULL)
 OR (TrangThai=2 AND NguoiXuLyId IS NOT NULL AND XuLyLuc IS NOT NULL AND KetLuan IS NOT NULL AND LEN(LTRIM(RTRIM(KetLuan)))>0));

ALTER TABLE dbo.YeuCauHuyKetNoi ADD CONSTRAINT CK_YeuCauHuyKetNoi_PhanHoiSauGui CHECK (PhanHoiLuc IS NULL OR PhanHoiLuc >= GuiLuc);

ALTER TABLE dbo.ThanhVienLopHoc ADD CONSTRAINT CK_ThanhVienLopHoc_DuyetSauDeNghi CHECK (DuyetLuc IS NULL OR DuyetLuc >= DeNghiLuc);

ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT UQ_HoSoGiaSu_GiaSuId UNIQUE (GiaSuId);

ALTER TABLE dbo.LopHoc ADD CONSTRAINT UQ_LopHoc_YeuCauKetNoiId UNIQUE (YeuCauKetNoiId);

ALTER TABLE dbo.BaiTapNop ADD CONSTRAINT UQ_BaiTapNop_BaiTapGiaoId_SoLanNop UNIQUE (BaiTapGiaoId, SoLanNop);

ALTER TABLE dbo.BaiTapNop ADD CONSTRAINT UQ_BaiTapNop_NguoiNopId_MaThaoTac UNIQUE (NguoiNopId, MaThaoTac);

ALTER TABLE dbo.KetQuaChamBai ADD CONSTRAINT UQ_KetQuaChamBai_BaiTapNopId UNIQUE (BaiTapNopId);

ALTER TABLE dbo.TinNhan ADD CONSTRAINT UQ_TinNhan_NguoiGuiId_MaThaoTac UNIQUE (NguoiGuiId, MaThaoTac);

ALTER TABLE dbo.ThongBao ADD CONSTRAINT UQ_ThongBao_MaSuKien_NguoiNhanId UNIQUE (MaSuKien, NguoiNhanId);

ALTER TABLE dbo.TepDinhKem ADD CONSTRAINT UQ_TepDinhKem_TenLuuTru UNIQUE (TenLuuTru);

ALTER TABLE dbo.AspNetUserRoles ADD CONSTRAINT UQ_AspNetUserRoles_UserId UNIQUE (UserId);

ALTER TABLE dbo.AspNetUsers ADD CONSTRAINT UQ_AspNetUsers_NormalizedEmail UNIQUE (NormalizedEmail);

ALTER TABLE dbo.KhungGioRanh ADD CONSTRAINT UQ_KhungGioRanh_NhuCauId_Thu_GioBatDau UNIQUE (NhuCauId, ThuTrongTuan, GioBatDau);



ALTER TABLE dbo.PhieuDanhGia ADD CONSTRAINT UQ_PhieuDanhGia_LopHocId_NguoiGuiId_MocDanhGia UNIQUE (LopHocId, NguoiGuiId, MocDanhGia);

CREATE INDEX IX_HoSoGiaSuMonDay_MonHocId ON dbo.HoSoGiaSuMonDay (MonHocId);

CREATE INDEX IX_NhuCauHoc_ChuSoHuuId ON dbo.NhuCauHoc (ChuSoHuuId);

CREATE INDEX IX_KhungGioRanh_NhuCauId ON dbo.KhungGioRanh (NhuCauId);

CREATE INDEX IX_YeuCauKetNoiLopHoc_GiaSuId ON dbo.YeuCauKetNoiLopHoc (GiaSuId);

CREATE INDEX IX_YeuCauKetNoiLopHoc_BenHocId ON dbo.YeuCauKetNoiLopHoc (BenHocId);

CREATE INDEX IX_YeuCauKetNoiLopHoc_NguoiGuiId ON dbo.YeuCauKetNoiLopHoc (NguoiGuiId);

CREATE INDEX IX_YeuCauKetNoiLopHoc_NhuCauId ON dbo.YeuCauKetNoiLopHoc (NhuCauId);

CREATE INDEX IX_YeuCauKetNoiLopHoc_MonHocId ON dbo.YeuCauKetNoiLopHoc (MonHocId);

CREATE INDEX IX_LopHoc_GiaSuId ON dbo.LopHoc (GiaSuId);

CREATE INDEX IX_LopHoc_BenHocId ON dbo.LopHoc (BenHocId);

CREATE INDEX IX_LopHoc_MonHocId ON dbo.LopHoc (MonHocId);

CREATE INDEX IX_YeuCauHuyKetNoi_LopHocId ON dbo.YeuCauHuyKetNoi (LopHocId);

CREATE INDEX IX_YeuCauHuyKetNoi_NguoiGuiId ON dbo.YeuCauHuyKetNoi (NguoiGuiId);

CREATE INDEX IX_ThanhVienLopHoc_LopHocId ON dbo.ThanhVienLopHoc (LopHocId);

CREATE INDEX IX_ThanhVienLopHoc_TaiKhoanId ON dbo.ThanhVienLopHoc (TaiKhoanId);

CREATE INDEX IX_ThanhVienLopHoc_NguoiDeNghiId ON dbo.ThanhVienLopHoc (NguoiDeNghiId);

CREATE INDEX IX_BuoiHoc_LichHocId ON dbo.BuoiHoc (LichHocId);

CREATE INDEX IX_BuoiHoc_NguoiGhiNhanId ON dbo.BuoiHoc (NguoiGhiNhanId);

CREATE INDEX IX_BaiTapGiao_LopHocId ON dbo.BaiTapGiao (LopHocId);

CREATE INDEX IX_BaiTapGiao_BuoiHocId ON dbo.BaiTapGiao (BuoiHocId);

CREATE INDEX IX_BaiTapGiao_NguoiGiaoId ON dbo.BaiTapGiao (NguoiGiaoId);

CREATE INDEX IX_KetQuaChamBai_NguoiChamId ON dbo.KetQuaChamBai (NguoiChamId);

CREATE INDEX IX_KetQuaKiemTra_LopHocId ON dbo.KetQuaKiemTra (LopHocId);

CREATE INDEX IX_KetQuaKiemTra_NguoiGhiId ON dbo.KetQuaKiemTra (NguoiGhiId);

CREATE INDEX IX_CuocHoiThoai_GiaSuId ON dbo.CuocHoiThoai (GiaSuId);

CREATE INDEX IX_CuocHoiThoai_BenHocId ON dbo.CuocHoiThoai (BenHocId);

CREATE INDEX IX_CuocHoiThoai_HoiThoaiTruocKetNoiId ON dbo.CuocHoiThoai (HoiThoaiTruocKetNoiId);

CREATE INDEX IX_PhieuDanhGia_LopHocId ON dbo.PhieuDanhGia (LopHocId);

CREATE INDEX IX_PhieuDanhGia_NguoiGuiId ON dbo.PhieuDanhGia (NguoiGuiId);

CREATE INDEX IX_BaoCaoViPham_NguoiBaoCaoId ON dbo.BaoCaoViPham (NguoiBaoCaoId);

CREATE INDEX IX_BaoCaoViPham_NguoiXuLyId ON dbo.BaoCaoViPham (NguoiXuLyId) WHERE NguoiXuLyId IS NOT NULL;

CREATE INDEX IX_BaoCaoViPham_TaiKhoanId ON dbo.BaoCaoViPham (TaiKhoanId) WHERE TaiKhoanId IS NOT NULL;

CREATE INDEX IX_BaoCaoViPham_HoSoGiaSuId ON dbo.BaoCaoViPham (HoSoGiaSuId) WHERE HoSoGiaSuId IS NOT NULL;

CREATE INDEX IX_BaoCaoViPham_NhuCauHocId ON dbo.BaoCaoViPham (NhuCauHocId) WHERE NhuCauHocId IS NOT NULL;

CREATE INDEX IX_BaoCaoViPham_LopHocId ON dbo.BaoCaoViPham (LopHocId) WHERE LopHocId IS NOT NULL;

CREATE INDEX IX_TepDinhKem_NguoiTaiLenId ON dbo.TepDinhKem (NguoiTaiLenId);

CREATE INDEX IX_TepDinhKem_HoSoGiaSuId ON dbo.TepDinhKem (HoSoGiaSuId) WHERE HoSoGiaSuId IS NOT NULL;

CREATE INDEX IX_TepDinhKem_BaiTapGiaoId ON dbo.TepDinhKem (BaiTapGiaoId) WHERE BaiTapGiaoId IS NOT NULL;

CREATE INDEX IX_TepDinhKem_BaiTapNopId ON dbo.TepDinhKem (BaiTapNopId) WHERE BaiTapNopId IS NOT NULL;

CREATE INDEX IX_TepDinhKem_TinNhanId ON dbo.TepDinhKem (TinNhanId) WHERE TinNhanId IS NOT NULL;

CREATE INDEX IX_TepDinhKem_BaoCaoViPhamId ON dbo.TepDinhKem (BaoCaoViPhamId) WHERE BaoCaoViPhamId IS NOT NULL;

CREATE INDEX IX_PhienDangNhap_TaiKhoanId ON dbo.PhienDangNhap (TaiKhoanId);

CREATE INDEX IX_NhatKyQuanTri_NguoiThucHienId ON dbo.NhatKyQuanTri (NguoiThucHienId);

CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);

CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);

CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);

CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);

CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;

CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;

CREATE UNIQUE INDEX IX_ThanhVienLopHoc_LopHocId_TaiKhoanId ON dbo.ThanhVienLopHoc (LopHocId, TaiKhoanId) WHERE TrangThai IN (0,1);

CREATE UNIQUE INDEX IX_CuocHoiThoai_GiaSuId_BenHocId ON dbo.CuocHoiThoai (GiaSuId, BenHocId) WHERE LoaiHoiThoai=0;

CREATE INDEX IX_NhuCauHoc_MonHocId_TrangThai_HinhThucHoc ON dbo.NhuCauHoc (MonHocId, TrangThai, HinhThucHoc);

CREATE INDEX IX_LichHoc_LopHocId_TuNgay ON dbo.LichHoc (LopHocId, TuNgay);

CREATE INDEX IX_ThongBao_NguoiNhanId_DaDocLuc_TaoLuc ON dbo.ThongBao (NguoiNhanId, DaDocLuc, TaoLuc);

CREATE INDEX IX_SuKienChoXuLy_TrangThai_ThuLaiLuc ON dbo.SuKienChoXuLy (TrangThai, ThuLaiLuc);

CREATE INDEX IX_YeuCauKetNoiLopHoc_TrangThai_HetHanLuc ON dbo.YeuCauKetNoiLopHoc (TrangThai, HetHanLuc);

CREATE INDEX IX_YeuCauHuyKetNoi_TrangThai_HetHanLuc ON dbo.YeuCauHuyKetNoi (TrangThai, HetHanLuc);

CREATE UNIQUE CLUSTERED INDEX CIX_TinNhan_CuocHoiThoaiId_GuiLuc_Id ON dbo.TinNhan (CuocHoiThoaiId, GuiLuc, Id);

CREATE UNIQUE INDEX IX_CuocHoiThoai_LopHocId ON dbo.CuocHoiThoai (LopHocId) WHERE LopHocId IS NOT NULL;

CREATE UNIQUE INDEX IX_YeuCauKetNoiLopHoc_DangCho ON dbo.YeuCauKetNoiLopHoc (GiaSuId, BenHocId, NhuCauId) WHERE TrangThai IN (0,1);

CREATE UNIQUE INDEX IX_YeuCauHuyKetNoi_DangCho ON dbo.YeuCauHuyKetNoi (LopHocId) WHERE TrangThai = 0;

CREATE INDEX IX_HoSoGiaSu_NguoiDuyetId ON dbo.HoSoGiaSu (NguoiDuyetId) WHERE NguoiDuyetId IS NOT NULL;

CREATE INDEX IX_HoSoGiaSu_TrangThaiDuyet_KhuVuc ON dbo.HoSoGiaSu (TrangThaiDuyet, KhuVuc);

CREATE INDEX IX_BaoCaoViPham_TrangThai ON dbo.BaoCaoViPham (TrangThai);

CREATE INDEX IX_PhienDangNhap_TaiKhoanId_DangHoatDong ON dbo.PhienDangNhap (TaiKhoanId) WHERE ThuHoiLuc IS NULL;

CREATE INDEX IX_SuKienChoXuLy_TrangThai_XuLyXongLuc ON dbo.SuKienChoXuLy (TrangThai, XuLyXongLuc) WHERE XuLyXongLuc IS NOT NULL;

CREATE INDEX IX_NhatKyQuanTri_ThoiDiem ON dbo.NhatKyQuanTri (ThoiDiem);

ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_DonViHocPhi CHECK (DonViHocPhi IN (0,1,2));
ALTER TABLE dbo.NhuCauHoc ADD CONSTRAINT CK_NhuCauHoc_HocPhiVaDonVi CHECK ((HocPhiDuKien IS NULL AND DonViHocPhi IS NULL) OR (HocPhiDuKien IS NOT NULL AND DonViHocPhi IS NOT NULL));
ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_DonViHocPhi CHECK (DonViHocPhi IN (0,1,2));
ALTER TABLE dbo.YeuCauKetNoiLopHoc ADD CONSTRAINT CK_YeuCauKetNoiLopHoc_HocPhiVaDonVi CHECK ((HocPhi IS NULL AND DonViHocPhi IS NULL) OR (HocPhi IS NOT NULL AND DonViHocPhi IS NOT NULL));
ALTER TABLE dbo.LopHoc ADD CONSTRAINT CK_LopHoc_DonViHocPhi CHECK (DonViHocPhi IN (0,1,2));
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT FK_BuoiHoc_LopHoc_LopHocId FOREIGN KEY (LopHocId) REFERENCES dbo.LopHoc(Id);
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT FK_BuoiHoc_BuoiHoc_BuoiHocGocId FOREIGN KEY (BuoiHocGocId, LopHocId) REFERENCES dbo.BuoiHoc(Id, LopHocId);
ALTER TABLE dbo.LichHoc ADD CONSTRAINT CK_LichHoc_ThuTrongTuan CHECK (ThuTrongTuan BETWEEN 0 AND 6);
ALTER TABLE dbo.LichHoc ADD CONSTRAINT CK_LichHoc_ThuTuGio CHECK (GioBatDau < GioKetThuc);
ALTER TABLE dbo.LichHoc ADD CONSTRAINT CK_LichHoc_KhoangNgay CHECK (DenNgay IS NULL OR DenNgay >= TuNgay);
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_TrangThai CHECK (TrangThai IN (0,1,2));
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_ThuTuThoiGian CHECK (BatDauLuc < KetThucLuc);
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_KhongTuThamChieu CHECK (BuoiHocGocId IS NULL OR BuoiHocGocId <> Id);
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_LienKetBuoiBu CHECK ((LaBuoiBu=0 AND BuoiHocGocId IS NULL) OR (LaBuoiBu=1 AND BuoiHocGocId IS NOT NULL));
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_NgayTheoLich CHECK ((LichHocId IS NULL AND NgayTheoLich IS NULL) OR (LichHocId IS NOT NULL AND NgayTheoLich IS NOT NULL));
ALTER TABLE dbo.BuoiHoc ADD CONSTRAINT CK_BuoiHoc_GhiNhanDayDu CHECK ((TrangThai=1 AND GhiNhanLuc IS NOT NULL AND NguoiGhiNhanId IS NOT NULL AND TrangThaiThamDu IS NOT NULL) OR (TrangThai IN (0,2) AND GhiNhanLuc IS NULL AND NguoiGhiNhanId IS NULL AND TrangThaiThamDu IS NULL));
ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_DuyetTheoTrangThai CHECK ((TrangThaiDuyet=0 AND NguoiDuyetId IS NULL AND DuyetLuc IS NULL) OR (TrangThaiDuyet IN (1,2) AND NguoiDuyetId IS NOT NULL AND DuyetLuc IS NOT NULL));
ALTER TABLE dbo.HoSoGiaSu ADD CONSTRAINT CK_HoSoGiaSu_LyDoTuChoi CHECK (TrangThaiDuyet<>2 OR (LyDoTuChoi IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoTuChoi)))>0));

CREATE INDEX IX_BuoiHoc_LopHocId_BatDauLuc ON dbo.BuoiHoc(LopHocId, BatDauLuc);
CREATE INDEX IX_BuoiHoc_BuoiHocGocId ON dbo.BuoiHoc(BuoiHocGocId) WHERE BuoiHocGocId IS NOT NULL;
CREATE UNIQUE INDEX IX_BuoiHoc_LichHocId_NgayTheoLich ON dbo.BuoiHoc(LichHocId, NgayTheoLich) WHERE LichHocId IS NOT NULL;

COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

GO

SET NOEXEC OFF;

GO