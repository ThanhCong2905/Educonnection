$ErrorActionPreference = "Stop"
$dotnet = Join-Path $PSScriptRoot ".dotnet\dotnet.exe"
$project = Join-Path $PSScriptRoot "KetNoiGiaSu.csproj"
$application = Join-Path $PSScriptRoot "bin\Debug\net10.0\KetNoiGiaSu.dll"

if (-not (Test-Path $dotnet)) {
    $dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
}
if (-not (Test-Path $dotnet)) {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { $dotnet = $command.Source }
    else { throw "Khong tim thay .NET SDK. Can cai SDK theo global.json." }
}

$env:PATH = ((Split-Path $dotnet) + ";" + $env:PATH)
$env:APPDATA = Join-Path $PSScriptRoot ".appdata"
$env:LOCALAPPDATA = Join-Path $PSScriptRoot ".localappdata"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5187"
New-Item -ItemType Directory -Force -Path $env:APPDATA, $env:LOCALAPPDATA | Out-Null

& $dotnet build $project --configfile (Join-Path $PSScriptRoot "NuGet.Config") "-p:UseSharedCompilation=false" "-nodeReuse:false" --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

& $dotnet $application
