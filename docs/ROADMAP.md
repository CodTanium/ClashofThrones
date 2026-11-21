# Roadmap
**What:** the road from first playable to release, phase by phase. **Updated:** 2026-11-21 · current: **Phase 1 (art pipeline)**
Status of the current milestone: [PROJECT_STATUS.md](PROJECT_STATUS.md) · design: [GAME_DESIGN_DOCUMENT.md](GAME_DESIGN_DOCUMENT.md)

```mermaid
flowchart LR
    P0[0 First playable ✅] --> P1[1 Art pipeline + hero] --> P2[2 Vertical slice] --> P3[3 Alpha] --> P4[4 Beta] --> P5[5 Release]
```

## Phase 0: First playable ✅ (2026-11-21)
- [x] Godot 4.7.2 .NET project, pixel-perfect 640x360
- [x] Knight: move, variable jump, combo, dodge, parry, hurt, death
- [x] Grave Warden, two altars, death/respawn loop
- [x] Gothic dungeon scene: parallax, candle lights, fog, dust
- [x] DevBridge + `cot` CLI, smoke/screenshot scripts

## Phase 1: Art pipeline + hero (now)
- [x] Local AI art tools: ComfyUI + SDXL + Pixel Art XL LoRA (`scripts/setup-art-tools.ps1`, outside the repo)
- [x] `scripts/artgen.ps1`: generate from a prompt or restyle the rig frames (img2img), headless via the ComfyUI API
- [x] Cleanup step in the game (`--art-clean`): background removal, downscale to the pixel grid, palette quantize, sheet packing
- [ ] Hero concept at or above the target quality; owner approves (4 concepts in `reports/art/`)
- [x] Hero sprite sheet (all 12 animations) in the new style: first pass 2026-11-21 (`scripts/hero-restyle.ps1`)
- [ ] Hero polish: hand fixes in Pixelorama, frame-to-frame consistency (owner review)
- [ ] Environment pass: bricks, pillars, altar, candles in the same style
- [ ] Licence decision ([#4](https://github.com/CodTanium/ClashofThrones/issues/4))

## Phase 2: Vertical slice (one polished 10-15 minute area)
- [ ] 3 enemy types (slow melee ✅, fast flyer, ranged caster) + 1 boss with 2 phases
- [ ] Healing flasks, altar rest = save + refill + enemy reset
- [ ] Main menu, pause menu, settings (volume, keybinds, window scale)
- [ ] Save/load (altar + collected items)
- [ ] Music (1 area theme, 1 boss theme) + ambience + final SFX
- [ ] Executions on staggered enemies, hit sparks and blood polish
- [ ] Real level authored as scenes (TileMapLayer), 4-6 rooms with one shortcut

## Phase 3: Alpha (content complete for 3 areas)
- [ ] 3 connected areas, map screen
- [ ] 2 traversal abilities (e.g. wall climb, air dash) that open old paths
- [ ] 8+ enemy types, 3 bosses
- [ ] NPCs + short lore texts; final title and studio name decided (name check before any store page)

## Phase 4: Beta
- [ ] Full playthrough, difficulty curve, playtests with 5+ people
- [ ] Performance pass, gamepad polish, rebinding, accessibility (screen shake toggle, hit-stop toggle)
- [ ] Bug bash ([BUG_TRACKER.md](BUG_TRACKER.md) empty of high/critical)

## Phase 5: Release
- [ ] Export presets (Windows first), installer/zip, store page assets
- [ ] Credits + licences complete ([ASSET_LICENCES.md](ASSET_LICENCES.md))
- [ ] 1.0 tag, changelog, trailer

## Open issues
| # | Title |
|---|---|
| [#3](https://github.com/CodTanium/ClashofThrones/issues/3) | Sync the private project notes after each session |
| [#4](https://github.com/CodTanium/ClashofThrones/issues/4) | Choose a licence for the public repository |

