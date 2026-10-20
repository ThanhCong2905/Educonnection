param(
    [ValidateSet('Expire','Lock','Unlock','Roles','Cleanup')][string]$Action,
    [Parameter(Mandatory=$true)][string]$Email
)
$ErrorActionPreference = 'Stop'
if ($Email -notmatch '^auth-test-[a-z0-9-]+@example\.invalid$') { throw 'Only generated auth test accounts are allowed.' }
$connection = New-Object System.Data.SqlClient.SqlConnection 'Server=localhost;Database=KetNoiGiaSu;Integrated Security=True;TrustServerCertificate=True'
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $null = $command.Parameters.AddWithValue('@email',$Email)
    $command.CommandText = switch ($Action) {
        'Expire' { 'UPDATE dbo.PhienDangNhap SET HoatDongCuoiLuc=DATEADD(minute,-31,SYSDATETIMEOFFSET()) WHERE TaiKhoanId=(SELECT Id FROM dbo.AspNetUsers WHERE Email=@email)' }
        'Lock' { 'UPDATE dbo.AspNetUsers SET TrangThai=1 WHERE Email=@email' }
        'Unlock' { 'UPDATE dbo.AspNetUsers SET TrangThai=0 WHERE Email=@email' }
        'Roles' { 'SELECT r.Name FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId JOIN dbo.AspNetUsers u ON u.Id=ur.UserId WHERE u.Email=@email' }
        'Cleanup' { @'
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @id uniqueidentifier=(SELECT Id FROM dbo.AspNetUsers WHERE Email=@email);
DELETE FROM dbo.PhienDangNhap WHERE TaiKhoanId=@id;
DELETE FROM dbo.TrangThaiThuDangNhap WHERE LoaiKhoa=0 AND GiaTriKhoa=CONVERT(nvarchar(36),@id);
DELETE FROM dbo.AspNetUserRoles WHERE UserId=@id;
DELETE FROM dbo.AspNetUserTokens WHERE UserId=@id;
DELETE FROM dbo.AspNetUserClaims WHERE UserId=@id;
DELETE FROM dbo.AspNetUserLogins WHERE UserId=@id;
DELETE FROM dbo.AspNetUsers WHERE Id=@id;
COMMIT;
'@ }
    }
    if ($Action -eq 'Roles') { Write-Output $command.ExecuteScalar() }
    else { $null = $command.ExecuteNonQuery(); Write-Output 'OK' }
} finally { $connection.Dispose() }
