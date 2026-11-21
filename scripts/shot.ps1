# shot.ps1 - run the game in a window, play an input script, save a screenshot, quit.
#   .\scripts\shot.ps1                                  -> reports\screenshots\shot.png
#   .\scripts\shot.ps1 -Out reports\screenshots\x.png -Script 'wait 1; press move_right; wait 2'
param([string]$Out = 'reports\screenshots\shot.png', [string]$Script = 'wait 2.5', [switch]$Menu)   # -Menu: shoot the title screen instead of starting a game
. "$PSScriptRoot\_common.ps1"
Invoke-Build; Invoke-Import
$abs = if ([IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $Root $Out }
$full = if ($Menu) { "$Script; shot $abs; quit" } else { "newgame; wait 1.5; $Script; shot $abs; quit" }
$r = Invoke-Game -Script $full -TimeoutSec 120
if (Test-Path $abs) { Write-Host "shot     $abs" } else { Write-Host 'no screenshot written' -ForegroundColor Red; exit 1 }