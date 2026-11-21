# restyle.ps1 - repaint a character sprite sheet in the AI pixel-art style, keeping the rig's exact poses.
#   1. export every rig frame upscaled (to ~1024 px)    2. img2img each frame with one seed (artgen.ps1)
#   3. downscale, cut with the rig silhouette, one shared palette -> src\art\<rig>.png (the game picks it up)
#   .\scripts\restyle.ps1 -Rig warden                 all frames of one character
#   .\scripts\restyle.ps1 -Rig hero -Only heavy,block  only some animations (others keep their current look)
#   .\scripts\restyle.ps1 -Rig ghoul -Revert          back to the code-drawn sheet
# Rigs: hero, warden, ghoul, archer, abbot. Prompts and settings per rig are below.
param(
    [Parameter(Mandatory)][ValidateSet('hero', 'warden', 'ghoul', 'archer', 'abbot')][string]$Rig,
    [string]$Prompt, [double]$Denoise = -1, [long]$Seed = 4242, [string[]]$Only, [int]$Colors = 32, [switch]$Revert, [switch]$KeepComfy
)
. "$PSScriptRoot\_common.ps1"
$tail = 'dark fantasy, gothic, dramatic rim light from top left, highly detailed, crisp pixels, plain grey background'
$cfg = @{
    hero   = @{ scale = 8; denoise = 0.68; prompt = "pixel art, side view game sprite of a medieval knight in battered steel plate armor with engraved details and rivets, great helm with narrow visor, chainmail, tattered crimson cape and tabard, leather belt, longsword, kite shield with gold boss, $tail" }
    warden = @{ scale = 6; denoise = 0.62; prompt = "pixel art, side view game sprite of a hulking hooded grave keeper monster, tattered brown robe, pale grey skin, glowing orange eyes in the hood, huge rusty cleaver, $tail" }
    ghoul  = @{ scale = 8; denoise = 0.62; prompt = "pixel art, side view game sprite of an emaciated pale ghoul, hunched, bald skull-like head, long bony claws, ragged dark cloth, one red glowing eye, $tail" }
    archer = @{ scale = 8; denoise = 0.6;  prompt = "pixel art, side view game sprite of a skeleton archer in a hooded dark purple cloak, bone arms, wooden recurve bow, glowing cyan eye, $tail" }
    abbot  = @{ scale = 4; denoise = 0.6;  prompt = "pixel art, side view game sprite of a giant undead abbot boss, teal robes, crimson tabard, tall golden mitre, glowing cyan eyes, ornate golden crozier staff, $tail" }
}[$Rig]
if (-not $Prompt) { $Prompt = $cfg.prompt }
if ($Denoise -lt 0) { $Denoise = $cfg.denoise }
$target = Join-Path $Src "art\$Rig.png"
if ($Revert) { Remove-Item $target, "$target.import" -ErrorAction SilentlyContinue; Write-Host "reverted: $Rig uses the generated sheet again"; return }
$work = Join-Path $env:TEMP ($(if ($Rig -eq 'hero') { 'cot-hero-restyle' } else { "cot-restyle-$Rig" }))
Remove-Item -Recurse -Force "$work\in" -ErrorAction SilentlyContinue   # out\ is kept: -Only re-renders some animations, the rest stay
Invoke-Build
& (Get-GodotConsole) --headless --path $Src -- "--export-rig=$work\in" "--rig=$Rig" "--scale=$($cfg.scale)" 2>&1 | Select-String 'DevBridge' | ForEach-Object { $_.Line }
if ($Only) { Get-ChildItem "$work\in" -Filter *.png | Where-Object { ($_.BaseName -replace '_\d+$', '') -notin $Only } | Remove-Item } else { Remove-Item -Recurse -Force "$work\out" -ErrorAction SilentlyContinue }
& "$PSScriptRoot\artgen.ps1" -Prompt $Prompt -Init "$work\in" -Out "$work\out" -Denoise $Denoise -Seed $Seed -Stop:(-not $KeepComfy)
New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
& (Get-GodotConsole) --headless --path $Src -- "--art-restyle=$work\out" "--rig=$Rig" "--out=$target" "--colors=$Colors" 2>&1 | Select-String 'DevBridge|ERROR' | ForEach-Object { $_.Line }
& (Get-GodotConsole) --headless --path $Src --import 2>&1 | Out-Null
Write-Host "sheet    $target  (frames kept for review in $work\out)"