# Shared helpers for the dev scripts. Finds Godot (.NET build) and the project folder.
$ErrorActionPreference = 'Continue'
$Root = Split-Path $PSScriptRoot -Parent
$Src  = Join-Path $Root 'src'
function Find-Godot {
    $g = [Environment]::GetEnvironmentVariable('GODOT', 'User'); if (-not $g) { $g = $env:GODOT }
    if ($g -and (Test-Path $g)) { return $g }
    $hit = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages", "$env:LOCALAPPDATA\Programs", "$HOME\Downloads", 'C:\Program Files' -Recurse -Depth 3 -Filter 'Godot*mono*win64.exe' -ErrorAction SilentlyContinue |
        Where-Object Name -notmatch 'console' | Select-Object -First 1
    if (-not $hit) { throw 'Godot 4.7 .NET not found: winget install GodotEngine.GodotEngine.Mono, then set the GODOT user variable.' }
    return $hit.FullName
}
function Get-GodotConsole { (Find-Godot) -replace '\.exe$', '_console.exe' }
function Invoke-Build {
    $out = dotnet build (Join-Path $Src 'ClashOfThrones.csproj') -nologo -v q 2>&1
    $errs = $out | Select-String ': error ' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique
    if ($LASTEXITCODE -ne 0) { $errs | ForEach-Object { Write-Host $_ -ForegroundColor Red }; throw 'build failed' }
    Write-Host 'build    ok'
}
function Invoke-Import {
    if (Test-Path (Join-Path $Src '.godot\imported')) { return }
    Write-Host 'import   first run: importing assets (headless)...'
    & (Get-GodotConsole) --headless --path $Src --import 2>&1 | Out-Null
}
# Runs the game with a DevBridge script; prints only errors and DevBridge lines. Returns the exit code.
function Invoke-Game([string]$Script, [switch]$Headless, [int]$TimeoutSec = 60) {
    $args = @('--path', $Src)
    if ($Headless) { $args = @('--headless') + $args } else { $args += @('--windowed', '--resolution', '1280x720') }
    $args = @($args | ForEach-Object { if ($_ -match ' ') { "`"$_`"" } else { $_ } })
    $args += @('--', "`"--script=$Script`"")
    $log = Join-Path $env:TEMP "cot-run-$PID.log"
    $p = Start-Process -FilePath (Get-GodotConsole) -ArgumentList $args -NoNewWindow -PassThru -RedirectStandardOutput $log -RedirectStandardError "$log.err"
    if (-not $p.WaitForExit($TimeoutSec * 1000)) { $p.Kill(); Write-Host "timeout after $TimeoutSec s" -ForegroundColor Red }
    $lines = @(Get-Content $log, "$log.err" -ErrorAction SilentlyContinue)
    $bad = $lines | Where-Object { $_ -match 'ERROR|Exception|SCRIPT ERROR|at ClashOfThrones' }
    $lines | Where-Object { $_ -match '\[DevBridge\]' } | ForEach-Object { Write-Host "  $_" }
    $bad | Select-Object -First 25 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Remove-Item $log, "$log.err" -ErrorAction SilentlyContinue
    return [pscustomobject]@{ Exit = $p.ExitCode; Errors = @($bad).Count }
}