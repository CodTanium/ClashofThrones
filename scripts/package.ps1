# package.ps1 - export a release build and zip it for testers (itch.io, a USB stick, a friend).
#   .\scripts\package.ps1                 -> build\ClashOfThrones-v<VERSION>-windows.zip
# Needs the Godot 4.7.2 mono export templates (Godot editor: Editor > Manage Export Templates, or see docs/TECHNICAL.md).
# Release builds have the DevBridge switched off (it only runs in debug builds).
. "$PSScriptRoot\_common.ps1"
$ver = (Get-Content (Join-Path $Root 'VERSION') -Raw).Trim()
$out = Join-Path $Root 'build\windows'
Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $out | Out-Null
Invoke-Build; Invoke-Import
Write-Host 'export   Windows Desktop (release)...'
$log = & (Get-GodotConsole) --headless --path $Src --export-release 'Windows Desktop' (Join-Path $out 'ClashOfThrones.exe') 2>&1
$log | Select-String 'ERROR|error' | Select-Object -First 15 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
if (-not (Test-Path (Join-Path $out 'ClashOfThrones.exe'))) { throw 'export failed (see errors above)' }
# self-test of the packed release: level file + art must load
$st = & (Join-Path $out 'ClashOfThrones.exe') --headless -- --selftest 2>&1 | Select-String 'selftest'
Write-Host "selftest $st"
if ("$st" -notmatch 'PASS') { throw 'release self-test failed' }
@"
CLASH OF THRONES  v$ver  (pre-alpha test build)
A 2D dark-fantasy action platformer.

Start: double-click ClashOfThrones.exe (keep the data_ folder next to it).
Windows may warn about an unknown publisher: More info > Run anyway.
Saves and settings: %APPDATA%\Godot\app_userdata\Clash of Thrones\

Controls (keyboard / gamepad)
  Move A/D, left stick          Jump Space, A (again in the air = double jump once found)
  Attack J, X                   Heavy I, Y (hold = charge)
  Roll/Sprint/Air dash K, B     Block/Parry L, LB (+ J = shield bash)
  Flask R, RB                   Wrath F, RT       Interact E, LT      Pause Esc, Start
Full list in the game: Esc > Options > Controls.

Please report: what felt too hard/easy, any place you got stuck, any crash.
"@ | Set-Content (Join-Path $out 'README.txt') -Encoding UTF8
$zip = Join-Path $Root "build\ClashOfThrones-v$ver-windows.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
$mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "package  $zip  ($mb MB)"
Get-ChildItem $out | ForEach-Object { Write-Host "         $($_.Name)" }