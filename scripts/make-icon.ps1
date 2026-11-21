# make-icon.ps1 - draws the game icon (pixel sword on a dark crest) and writes src\icon.ico (16-256 px, PNG entries).
#   .\scripts\make-icon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
# 32x32 design, one char per pixel:  . empty  d dark bg  e edge  s steel light  m steel mid  g gold  l leather  r red
$art = @(
'................................',
'.....eeeeeeeeeeeeeeeeeeeeee.....',
'....edddddddddddddddddddddde....',
'....eddddddddddsmdddddddddde....',
'....eddddddddddsmdddddddddde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edrrddddddsmmddddddrrdde....',
'....edddddddddsmmdddddddddde....',
'....edddddddddsmmdddddddddde....',
'....edddddddddsmmdddddddddde....',
'....edddddddddsmmdddddddddde....',
'....edddddddddsmmdddddddddde....',
'....edddddggggggggggddddddde....',
'....edddddggggggggggddddddde....',
'.....edddddddddllddddddddde.....',
'.....edddddddddllddddddddde.....',
'......eddddddddllddddddde.......',
'......eddddddddllddddddde.......',
'.......edddddddllddddde.........',
'........edddddggggddde..........',
'.........edddggggdde............',
'..........eddddddde.............',
'............eddde...............',
'.............eee................',
'................................',
'................................')
$col = @{ 'd' = '#14121c'; 'e' = '#4a3c30'; 's' = '#e6e9f0'; 'm' = '#8f95a3'; 'g' = '#d9a441'; 'l' = '#5c412b'; 'r' = '#8a2420' }
$base = New-Object Drawing.Bitmap 32, 32
for ($y = 0; $y -lt 32; $y++) { for ($x = 0; $x -lt 32; $x++) {
    $c = $art[$y][$x]; if ($col.ContainsKey([string]$c)) { $base.SetPixel($x, $y, [Drawing.ColorTranslator]::FromHtml($col[[string]$c])) } } }
$entries = @()
foreach ($size in 16, 32, 48, 64, 128, 256) {
    $bmp = New-Object Drawing.Bitmap $size, $size
    $g = [Drawing.Graphics]::FromImage($bmp); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $g.DrawImage($base, 0, 0, $size, $size); $g.Dispose()
    $ms = New-Object IO.MemoryStream; $bmp.Save($ms, [Drawing.Imaging.ImageFormat]::Png)
    $entries += , @($size, $ms.ToArray())
    if ($size -eq 256) { $bmp.Save((Join-Path $root 'src\icon.png'), [Drawing.Imaging.ImageFormat]::Png) }
}
$out = New-Object IO.MemoryStream; $w = New-Object IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$entries.Count)
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    $s = $e[0]; $len = $e[1].Length
    $w.Write([byte]($(if ($s -ge 256) { 0 } else { $s }))); $w.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $w.Write([byte]0); $w.Write([byte]0); $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$len); $w.Write([uint32]$offset)
    $offset += $len
}
foreach ($e in $entries) { $w.Write($e[1]) }
[IO.File]::WriteAllBytes((Join-Path $root 'src\icon.ico'), $out.ToArray())
Write-Host "icon     $(Join-Path $root 'src\icon.ico')  ($($entries.Count) sizes)"
