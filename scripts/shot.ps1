# shot.ps1 - run the game in a window, play an input script, save a screenshot, quit.
#   .\scripts\shot.ps1                                  -> reports\screenshots\shot.png
#   .\scripts\shot.ps1 -Out reports\screenshots\x.png -Script 'wait 1; press move_right; wait 2'
param([string]$Out = 'reports\screenshots\shot.png', [string]$Script = 'wait 2.5')
. "$PSScriptRoot\_common.ps1"
Invoke-Build; Invoke-Import
$abs = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Root $Out }
$r = Invoke-Game -Script "$Script; shot $abs; quit" -TimeoutSec 90
if (Test-Path $abs) { Write-Host "shot     $abs" } else { Write-Host 'no screenshot written' -ForegroundColor Red; exit 1 }