# cot.ps1 - run the cot CLI (talks to the running game). Same arguments as cot.   .\scripts\cot.ps1 status
$exe = Join-Path (Split-Path $PSScriptRoot -Parent) 'tools\Cot\bin\Release\net10.0\cot.exe'
if (-not (Test-Path $exe)) { dotnet build (Join-Path (Split-Path $PSScriptRoot -Parent) 'tools\Cot\Cot.csproj') -c Release -nologo -v q | Out-Null }
& $exe @args
exit $LASTEXITCODE