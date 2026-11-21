# Art direction
**What:** the rules every asset follows. **Updated:** 2026-11-21

| Rule | Value |
|---|---|
| Internal resolution | 640x360, integer scaling only (3x = 1080p, 4x = 1440p), nearest filtering, pixel snap |
| Hero size | about 90 px tall; frame 128x112, feet at (64,108) |
| Enemy frame | 160x128, feet at (80,124) |
| Light | always from the **top left**: highlights top-left, shadows bottom-right; warm candle light (#ff8a2a) against cold blue-violet stone |
| Outline | 1 px, very dark violet (#0b0a0f), never pure black |
| Shading | 4-step ramps (highlight, base, shadow, deep); no gradients or blur on sprites |
| Mood | dark, gothic, sacred-and-decayed; the darkness is a colour (CanvasModulate 0.44, 0.41, 0.52) |

## Palette (core)
| Use | Colours |
|---|---|
| Steel | #d2d6df #8f95a3 #565b69 #2d303b |
| Blood red cloth | #c23a33 #8a2420 #561718 #2e0d10 |
| Gold | #f0c55a #c38d2c #8a5d1c #4a300e |
| Leather | #83603f #5c412b #3c2a1c #22170f |
| Stone | #5b5866 #3e3c48 #2a2832 #17161c |
| Flame | #fff4d6 #ffd27a #ff8a2a |

## Parallax layers (back to front)
far sky + cathedral silhouettes (0.12) · colonnade (0.35) · brick wall with arches (0.62) · fog (0.8) · **world (1.0)** · front fog (1.1) · chains + column (1.3)

## Replacing placeholder art
`scripts\bake-art.ps1 -Use` writes every generated texture to `src/art/<name>.png`. Paint over it (Pixelorama, Krita),
keep the size and frame grid; the game uses the PNG instead of the generated one. Record the source in ASSET_LICENCES.md.