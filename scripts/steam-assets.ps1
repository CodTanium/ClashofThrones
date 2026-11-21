# steam-assets.ps1 - make the Steam store images from in-game screenshots: capsules, library art, logo, 1920x1080 screenshots.
#   .\scripts\steam-assets.ps1        -> marketing\steam\*.png
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$shots = Join-Path $root 'reports\screenshots'
$out = Join-Path $root 'marketing\steam'
New-Item -ItemType Directory -Force (Join-Path $out 'screenshots') | Out-Null
$cream = [Drawing.ColorTranslator]::FromHtml('#efe4c8'); $gold = [Drawing.ColorTranslator]::FromHtml('#f0c55a')

function Cover($src, $w, $h, [double]$focusX = 0.5, [double]$zoom = 1.0) {
    $img = [Drawing.Image]::FromFile((Join-Path $shots $src))
    $bmp = New-Object Drawing.Bitmap $w, $h
    $g = [Drawing.Graphics]::FromImage($bmp); $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $s = [Math]::Max($w / $img.Width, $h / $img.Height) * $zoom
    $dw = $img.Width * $s; $dh = $img.Height * $s
    $g.DrawImage($img, [float](($w - $dw) * $focusX), [float](($h - $dh) / 2), [float]$dw, [float]$dh)
    $img.Dispose(); return @($bmp, $g)
}
function Shade($g, $w, $h, [int]$alpha = 200) {
    $r = New-Object Drawing.Rectangle 0, ([int]($h * 0.45)), $w, ([int]($h * 0.55) + 1)
    $br = New-Object Drawing.Drawing2D.LinearGradientBrush $r, ([Drawing.Color]::FromArgb(0, 5, 4, 8)), ([Drawing.Color]::FromArgb($alpha, 5, 4, 8)), 90
    $g.FillRectangle($br, $r)
}
function Title($g, $w, $h, [double]$size, [double]$yFrac, [bool]$sub = $true) {
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $fmt = New-Object Drawing.StringFormat; $fmt.Alignment = 'Center'; $fmt.FormatFlags = 'NoWrap'
    do {   # shrink until the title fits in 90% of the width
        $f = New-Object Drawing.Font 'Georgia', ([float]$size), ([Drawing.FontStyle]::Bold), ([Drawing.GraphicsUnit]::Pixel)
        $fits = $g.MeasureString('CLASH OF THRONES', $f).Width -le $w * 0.9
        if (-not $fits) { $size *= 0.94 }
    } until ($fits)
    $rect = New-Object Drawing.RectangleF 0, ([float]($h * $yFrac)), $w, ($size * 1.4)
    $sh = New-Object Drawing.RectangleF 3, ([float]($h * $yFrac + 3)), $w, ($size * 1.4)
    $g.DrawString('CLASH OF THRONES', $f, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(220, 0, 0, 0))), $sh, $fmt)
    $g.DrawString('CLASH OF THRONES', $f, (New-Object Drawing.SolidBrush $cream), $rect, $fmt)
    if ($sub) {
        $f2 = New-Object Drawing.Font 'Georgia', ([float]($size * 0.36)), ([Drawing.FontStyle]::Italic), ([Drawing.GraphicsUnit]::Pixel)
        $r2 = New-Object Drawing.RectangleF 0, ([float]($h * $yFrac + $size * 1.25)), $w, ($size * 0.6)
        $g.DrawString('a dark-fantasy action platformer', $f2, (New-Object Drawing.SolidBrush $gold), $r2, $fmt)
    }
}
function Save($pair, $name) { $pair[1].Dispose(); $pair[0].Save((Join-Path $out $name), [Drawing.Imaging.ImageFormat]::Png); $pair[0].Dispose(); Write-Host "  $name" }

$hero = 'key-boss.png'   # clean shot (HUD hidden): .\scripts\shot.ps1 -Out reports\screenshots\key-boss.png -Script '... set /root/Main/Hud visible false'
foreach ($a in @(
    @{ n = 'header_capsule_920x430.png'; w = 920; h = 430; ts = 64; ty = 0.62; f = 0.5 },
    @{ n = 'small_capsule_462x174.png'; w = 462; h = 174; ts = 40; ty = 0.5; f = 0.5; nosub = $true },
    @{ n = 'main_capsule_1232x706.png'; w = 1232; h = 706; ts = 88; ty = 0.66; f = 0.5 },
    @{ n = 'vertical_capsule_748x896.png'; w = 748; h = 896; ts = 62; ty = 0.74; f = 0.62 },
    @{ n = 'library_capsule_600x900.png'; w = 600; h = 900; ts = 50; ty = 0.76; f = 0.62 })) {
    $p = Cover $hero $a.w $a.h $a.f; Shade $p[1] $a.w $a.h; Title $p[1] $a.w $a.h $a.ts $a.ty (-not $a.nosub); Save $p $a.n
}
$p = Cover 'key-gallery.png' 3840 1240 0.5; Save $p 'library_hero_3840x1240.png'          # no text allowed on the library hero
$logo = New-Object Drawing.Bitmap 1280, 720; $lg = [Drawing.Graphics]::FromImage($logo); Title $lg 1280 720 120 0.36; $lg.Dispose()
$logo.Save((Join-Path $out 'library_logo_1280x720.png'), [Drawing.Imaging.ImageFormat]::Png); $logo.Dispose(); Write-Host '  library_logo_1280x720.png'
$i = 1
foreach ($s in 'key-boss', 'key-hall', 'key-gallery', '17-boss-phase2', '19-new-art', '13-gallery', '14-pit', '15-arena', '18-altar-menu', '10-menu') {
    if (-not (Test-Path (Join-Path $shots "$s.png"))) { continue }
    $p = Cover "$s.png" 1920 1080 0.5; Save $p ("screenshots\{0:00}_{1}.png" -f $i, $s); $i++
}
Write-Host "steam assets -> $out"
