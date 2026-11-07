param([switch]$WithDriverTests)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $PSScriptRoot
try {
    $localSdk = Join-Path (Split-Path $PSScriptRoot -Parent) '.tools\dotnet\dotnet.exe'
    $dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
    if ($WithDriverTests) { $env:UGA_DRIVER_TESTS = '1' } else { $env:UGA_DRIVER_TESTS = '0' }
    & $dotnet restore
    if ($LASTEXITCODE) { throw 'Falha no restore.' }
    & $dotnet build -c Release --no-restore
    if ($LASTEXITCODE) { throw 'Falha na compilação.' }
    & $dotnet test -c Release --no-build --logger 'trx;LogFileName=UGA-tests.trx' --results-directory artifacts/test-results
    if ($LASTEXITCODE) { throw 'Testes falharam. Consulte artifacts/test-results.' }
} finally { Pop-Location }
