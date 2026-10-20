$ErrorActionPreference = 'Stop'
$identity = Get-Content 'tmp/auth-test-identity.json' -Raw | ConvertFrom-Json
$emails = @($identity.email, "auth-test-$($identity.suffix)-phuhuynh@example.invalid", "auth-test-$($identity.suffix)-giasu@example.invalid")
foreach ($email in $emails) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File tests/auth/auth-db-test.ps1 -Action Cleanup -Email $email
    if ($LASTEXITCODE -ne 0) { throw 'Test account cleanup failed.' }
}
if ($identity.network -match '^127\.\d{1,3}\.\d{1,3}\.$') {
    $connection = New-Object System.Data.SqlClient.SqlConnection 'Server=localhost;Database=KetNoiGiaSu;Integrated Security=True;TrustServerCertificate=True'
    $connection.Open()
    try {
        foreach ($lastOctet in 77..84) {
            $command = $connection.CreateCommand()
            $command.CommandText = 'DELETE FROM dbo.TrangThaiThuDangNhap WHERE LoaiKhoa=1 AND GiaTriKhoa=@ip'
            $null = $command.Parameters.AddWithValue('@ip', "$($identity.network)$lastOctet")
            $null = $command.ExecuteNonQuery()
        }
    } finally { $connection.Dispose() }
}
if (Test-Path -LiteralPath '.maildrop') {
    Get-ChildItem -LiteralPath '.maildrop' -Filter '*.eml' | ForEach-Object {
        $mail = Get-Content -LiteralPath $_.FullName -Raw
        if ($emails | Where-Object { $mail -match [regex]::Escape($_) }) { Remove-Item -LiteralPath $_.FullName }
    }
}
