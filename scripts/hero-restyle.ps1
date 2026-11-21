# hero-restyle.ps1 - repaint the hero sprite sheet in the AI pixel-art style, keeping the rig's exact poses.
#   1. export every rig frame upscaled x8 (1024x896)      2. img2img each frame with the same seed (artgen.ps1)
#   3. downscale, cut with the rig silhouette, one shared palette -> src\art\hero.png (the game picks it up)
#   .\scripts\hero-restyle.ps1                      all 54 frames (~15 min on an 8 GB GPU)
#   .\scripts\hero-restyle.ps1 -Only idle,attack1   only some animations (quick test; others keep the rig look)
#   .\scripts\hero-restyle.ps1 -Revert              delete src\art\hero.png and go back to the generated sheet
param(
    [string]$Prompt = 'pixel art, side view game sprite of a medieval knight in battered steel plate armor with engraved details and rivets, great helm with narrow visor, chainmail, tattered crimson cape and tabard, leather belt, longsword, kite shield with gold boss, dark fantasy, gothic, dramatic rim light from top left, highly detailed, crisp pixels, plain grey background',
    [double]$Denoise = 0.68, [long]$Seed = 4242, [string[]]$Only, [int]$Colors = 32, [switch]$Revert, [switch]$KeepComfy
)
. "$PSScriptRoot\_common.ps1"
$target = Join-Path $Src 'art\hero.png'
if ($Revert) { Remove-Item $target -ErrorAction SilentlyContinue; Write-Host 'reverted: the game uses the generated hero sheet again'; return }
$work = Join-Path $env:TEMP 'cot-hero-restyle'
Remove-Item -Recurse -Force "$work\in" -ErrorAction SilentlyContinue   # out\ is kept: -Only re-renders some animations, the rest stay
Invoke-Build
& (Get-GodotConsole) --headless --path $Src -- "--export-rig=$work\in" --scale=8 2>&1 | Select-String 'DevBridge' | ForEach-Object { $_.Line }
if ($Only) { Get-ChildItem "$work\in" -Filter *.png | Where-Object { ($_.BaseName -replace '_\d+$', '') -notin $Only } | Remove-Item } else { Remove-Item -Recurse -Force "$work\out" -ErrorAction SilentlyContinue }
& "$PSScriptRoot\artgen.ps1" -Prompt $Prompt -Init "$work\in" -Out "$work\out" -Denoise $Denoise -Seed $Seed -Stop:(-not $KeepComfy)
New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
& (Get-GodotConsole) --headless --path $Src -- "--art-restyle=$work\out" "--out=$target" "--colors=$Colors" 2>&1 | Select-String 'DevBridge|ERROR' | ForEach-Object { $_.Line }
& (Get-GodotConsole) --headless --path $Src --import 2>&1 | Out-Null
Write-Host "sheet    $target  (frames kept for review in $work\out)"



