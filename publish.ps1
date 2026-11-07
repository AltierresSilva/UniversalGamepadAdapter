param([switch]$SkipBuild, [switch]$WithDriverTests, [string]$IsccPath = '')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
Push-Location $PSScriptRoot
try {
    if (!$SkipBuild) { & "$PSScriptRoot\build.ps1" -WithDriverTests:$WithDriverTests }
    $toolsRoot = Join-Path (Split-Path $PSScriptRoot -Parent) '.tools'
    $localSdk = Join-Path $toolsRoot 'dotnet\dotnet.exe'
    $dotnet = if (Test-Path -LiteralPath $localSdk) { $localSdk } else { (Get-Command dotnet -ErrorAction Stop).Source }
    $output = Join-Path $PSScriptRoot 'artifacts\Release\win-x64'
    & $dotnet publish src/UGA.App/UGA.App.csproj -c Release -r win-x64 --self-contained true -o $output -p:PublishSingleFile=false -p:PublishTrimmed=false
    if ($LASTEXITCODE) { throw 'Falha na publicação.' }
    foreach ($required in @('UniversalGamepadAdapter.exe','SDL3.dll','SDL3-CS.dll','Nefarius.ViGEm.Client.dll','coreclr.dll','System.Windows.Forms.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $output $required))) { throw "Dependência ausente: $required" }
    }
    Copy-Item -LiteralPath "$PSScriptRoot\README.md","$PSScriptRoot\LICENSE","$PSScriptRoot\THIRD-PARTY-NOTICES.md" -Destination $output
    $docsOutput = Join-Path $output 'docs'
    New-Item -ItemType Directory -Path $docsOutput -Force | Out-Null
    Get-ChildItem -LiteralPath "$PSScriptRoot\docs" -Filter '*.md' -File | Copy-Item -Destination $docsOutput
    Get-ChildItem -LiteralPath $output -File -Recurse | Sort-Object FullName | ForEach-Object {
        [PSCustomObject]@{ File = [IO.Path]::GetRelativePath($output,$_.FullName); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    } | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath 'artifacts\publish-sha256.json' -Encoding utf8
    if (!$IsccPath) {
        $candidates = @((Join-Path $toolsRoot 'InnoSetup\ISCC.exe'), "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 7\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")
        $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
    if ($IsccPath) {
        & $IsccPath "$PSScriptRoot\Installer\UGA.iss"
        if ($LASTEXITCODE) { throw 'Falha ao gerar Setup.exe.' }
        Get-FileHash -LiteralPath 'artifacts\Installer\UGA-1.0.0-Setup.exe' -Algorithm SHA256 | Format-List
    } else { Write-Warning 'Inno Setup não encontrado. Compile Installer/UGA.iss com Inno Setup 6 ou superior.' }
} finally { Pop-Location }
