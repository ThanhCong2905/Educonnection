USE [KetNoiGiaSu];
GO
IF OBJECT_ID(N'dbo.HoSoCaNhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoSoCaNhan (
        TaiKhoanId uniqueidentifier NOT NULL CONSTRAINT PK_HoSoCaNhan PRIMARY KEY,
        ThanhPho nvarchar(100) NOT NULL,
        QuanHuyen nvarchar(100) NOT NULL,
        DiaChiChiTiet nvarchar(500) NULL,
        CONSTRAINT FK_HoSoCaNhan_TaiKhoan FOREIGN KEY (TaiKhoanId) REFERENCES dbo.AspNetUsers(Id)
    );
END;
GO
