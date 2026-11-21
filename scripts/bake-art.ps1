# bake-art.ps1 - write every generated placeholder texture as PNG so it can be painted over in Pixelorama/Krita.
# PNGs in src/art/ replace the generated version automatically (same name).
#   .\scripts\bake-art.ps1               -> src\art\baked\ (review copies, not used by the game)
#   .\scripts\bake-art.ps1 -Use          -> src\art\        (the game uses them from now on)
param([switch]$Use)
. "$PSScriptRoot\_common.ps1"
Invoke-Build
$dir = if ($Use) { 'res://art/' } else { 'res://art/baked/' }
& (Get-GodotConsole) --headless --path $Src -- "--bake-art=$dir" 2>&1 | Select-String 'DevBridge|ERROR' | ForEach-Object { $_.Line }