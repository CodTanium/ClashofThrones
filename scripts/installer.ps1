# installer.ps1 - build the Windows setup installer for the demo (Inno Setup 6).
#   .\scripts\installer.ps1            -> build\ClashOfThrones-v<VERSION>-Demo-Setup.exe (runs package.ps1 first)
#   .\scripts\installer.ps1 -NoExport  reuse the existing build\windows export
param([switch]$NoExport)
. "$PSScriptRoot\_common.ps1"
$iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found: winget install JRSoftware.InnoSetup' }
if (-not $NoExport) { & "$PSScriptRoot\package.ps1" }
$ver = (Get-Content (Join-Path $Root 'VERSION') -Raw).Trim()
$out = & $iscc "/DAppVersion=$ver" (Join-Path $Root 'installer\ClashOfThrones.iss') 2>&1
if ($LASTEXITCODE -ne 0) { $out | Select-Object -Last 15 | ForEach-Object { Write-Host $_ -ForegroundColor Red }; throw 'installer build failed' }
$exe = Join-Path $Root "build\ClashOfThrones-v$ver-Demo-Setup.exe"
Write-Host ("setup    {0}  ({1:n1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))