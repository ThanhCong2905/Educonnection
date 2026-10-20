param([ValidateSet('Create','Verify','Approve','Cleanup')][string]$Action='Create', [string]$Email)
$ErrorActionPreference='Stop'
$metaPath=Join-Path (Get-Location).Path 'tmp\workspace-ui-db.json'
$master='Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True'
if($Action -eq 'Create'){
    $name='KetNoiGiaSu_UiTests_'+[Guid]::NewGuid().ToString('N')
    $connection=New-Object System.Data.SqlClient.SqlConnection $master;$connection.Open()
    try{
        $sql=(Get-Content '01_TaoCoSoDuLieu.sql' -Raw -Encoding UTF8).Replace('KetNoiGiaSu',$name)
        foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*$')){if($batch.Trim()){ $command=$connection.CreateCommand();$command.CommandText=$batch;$null=$command.ExecuteNonQuery() }}
        $sql=(Get-Content '03_HoSoCaNhan.sql' -Raw -Encoding UTF8).Replace('KetNoiGiaSu',$name)
        foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*$')){if($batch.Trim()){ $command=$connection.CreateCommand();$command.CommandText=$batch;$null=$command.ExecuteNonQuery() }}
        $command=$connection.CreateCommand();$command.CommandText=Get-Content '02_DanhMucMonHoc.sql' -Raw -Encoding UTF8;$null=$command.ExecuteNonQuery()
        @{Database=$name;ConnectionString="Server=localhost;Database=$name;Integrated Security=True;TrustServerCertificate=True"}|ConvertTo-Json|Set-Content $metaPath -Encoding UTF8
        Write-Output "Created isolated UI test database: $name"
    }finally{$connection.Dispose()}
}else{
    $meta=Get-Content $metaPath -Raw|ConvertFrom-Json
    if($meta.Database -notmatch '^KetNoiGiaSu_UiTests_[0-9a-f]{32}$'){throw 'Unsafe test database name'}
    if($Action -eq 'Verify'){
        $null=Invoke-WebRequest 'http://localhost:5198/health/database' -UseBasicParsing
        $listener=Get-NetTCPConnection -LocalPort 5198 -State Listen | Select-Object -First 1
        if(-not $listener){throw 'No test server at port 5198'}
        $connection=New-Object System.Data.SqlClient.SqlConnection $master;$connection.Open()
        try{
            $command=$connection.CreateCommand()
            $command.CommandText='SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE host_process_id=@process AND DB_NAME(database_id)=@database'
            $null=$command.Parameters.AddWithValue('@process',[int]$listener.OwningProcess)
            $null=$command.Parameters.AddWithValue('@database',[string]$meta.Database)
            if([int]$command.ExecuteScalar() -lt 1){throw 'The server is not connected to the isolated UI test database. Test cancelled.'}
            Write-Output 'Verified isolated test server'
        }finally{$connection.Dispose()}
    }elseif($Action -eq 'Cleanup'){
        $connection=New-Object System.Data.SqlClient.SqlConnection $master;$connection.Open()
        try{$command=$connection.CreateCommand();$command.CommandText="ALTER DATABASE [$($meta.Database)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($meta.Database)];";$null=$command.ExecuteNonQuery();Write-Output 'Removed isolated UI test database'}finally{$connection.Dispose()}
    }else{
        if($Email -notmatch '^workspace-test-[a-z0-9-]+@example\.invalid$'){throw 'Only test fixture accounts are allowed'}
        $connection=New-Object System.Data.SqlClient.SqlConnection $meta.ConnectionString;$connection.Open()
        try{$command=$connection.CreateCommand();$command.CommandText=@'
DECLARE @reviewer uniqueidentifier=NEWID();
INSERT INTO dbo.AspNetUsers (Id,Email,NormalizedEmail,EmailConfirmed,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount,HoTen,TrangThai)
VALUES (@reviewer,CONVERT(nvarchar(36),@reviewer)+N'@example.invalid',UPPER(CONVERT(nvarchar(36),@reviewer)+N'@example.invalid'),0,0,0,0,0,N'Quản trị viên kiểm thử',0);
UPDATE dbo.HoSoGiaSu SET TrangThaiDuyet=1,NguoiDuyetId=@reviewer,DuyetLuc=SYSDATETIMEOFFSET()
WHERE GiaSuId=(SELECT Id FROM dbo.AspNetUsers WHERE Email=@email);
'@;$null=$command.Parameters.AddWithValue('@email',$Email);$null=$command.ExecuteNonQuery();Write-Output 'Approved test tutor'}finally{$connection.Dispose()}
    }
}
