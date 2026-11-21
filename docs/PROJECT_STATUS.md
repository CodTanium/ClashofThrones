# Project status
**What:** where Clash of Thrones stands. **Updated:** 2026-11-21 · version 0.1.0 (pre-alpha)

## Milestone 1: first playable (✅ = verified with output or a screenshot)
| # | Item | Status |
|---|---|---|
| 1 | Godot 4.7.2 .NET + C# (.NET 10) project, 640x360, nearest filter, integer scale, pixel snap | ✅ builds, runs |
| 2 | Input map (keyboard + gamepad), audio buses (Music, SFX, Ambience) | ✅ |
| 3 | DevBridge autoload + `cot` CLI (screenshot, tree, get/set, call, spawn, free, GDScript, input, reload, log) | ✅ tested live |
| 4 | Scripts: build, smoke (headless), shot, run, bake-art | ✅ smoke PASS |
| 5 | Docs: status, art direction, GDD, licences, bugs, technical | ✅ |
| 6 | Art pipeline: procedural placeholder art (hero, enemy, environment); bake to PNG for painting over | ✅ placeholder |
| 7 | Hero animations: idle, run, jump, fall, land, attack 1-3, dodge, parry, hurt, death | ✅ placeholder |
| 8 | First scene: gothic brick dungeon, 5 parallax layers, 2D lights, candles, fog, dust, pixel-exact camera | ✅ |
| 9 | Movement + combat: acceleration, variable jump, coyote, buffer, combo, hitboxes, i-frames, knockback, hit-stop, shake | ✅ |
| 10 | Enemy (slow melee), checkpoint, death + respawn | ✅ tested (respawn at 2nd altar x=1536) |

## Milestone 2 (Phase 1): art pipeline
| Item | Status |
|---|---|
| ComfyUI + SDXL + Pixel Art XL installed locally (RTX 3070, CUDA ok) | ✅ |
| artgen.ps1 (txt2img / img2img via API), hero-restyle.ps1, in-engine cleanup | ✅ |
| Hero sheet restyled (54 frames) and used in game | ✅ screenshot 05 |
| Owner approval of the hero look | waiting |

## Next
- Final-quality hero art (local Stable Diffusion pixel-art model + cleanup tool, or hand-painted over the baked PNGs).
- Owner review of the feel (speeds, timings) in [GAME_DESIGN_DOCUMENT.md](GAME_DESIGN_DOCUMENT.md#tuning).
- Vertical slice: 3 enemy types, 1 boss, main menu, save, music and ambience.
