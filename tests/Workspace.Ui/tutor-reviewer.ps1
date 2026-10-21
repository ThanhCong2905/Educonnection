param([Parameter(Mandatory=$true)][string]$Email)
$ErrorActionPreference='Stop'
$meta=Get-Content 'tmp/workspace-ui-db.json' -Raw|ConvertFrom-Json
if($meta.Database -notmatch '^KetNoiGiaSu_UiTests_[0-9a-f]{32}$' -or $Email -notmatch '^workspace-test-[a-z0-9-]+@example\.invalid$'){throw 'Only isolated fixture accounts are allowed'}
$connection=New-Object System.Data.SqlClient.SqlConnection $meta.ConnectionString
$connection.Open()
try {
    $command=$connection.CreateCommand();$command.CommandText=@'
DECLARE @user uniqueidentifier=(SELECT Id FROM dbo.AspNetUsers WHERE Email=@email);
IF @user IS NULL THROW 50001, 'Test user not found', 1;
DECLARE @role uniqueidentifier=(SELECT Id FROM dbo.AspNetRoles WHERE Name=N'QuanTriVien');
IF @role IS NULL BEGIN SET @role=NEWID(); INSERT dbo.AspNetRoles (Id,Name,NormalizedName) VALUES (@role,N'QuanTriVien',N'QUANTRIVIEN'); END;
UPDATE dbo.AspNetUserRoles SET RoleId=@role WHERE UserId=@user;
'@
    $null=$command.Parameters.AddWithValue('@email',$Email);$null=$command.ExecuteNonQuery()
} finally {$connection.Dispose()}
