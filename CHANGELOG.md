# Changelog
Newest first.

## 0.3.1 (2026-11-21)
- Windows export preset and `scripts/package.ps1` (release build + zip with a tester README) for an itch.io test page.
- Demo installer: `installer/ClashOfThrones.iss` + `scripts/installer.ps1` (Inno Setup 6): per-user install, Start-menu/desktop shortcuts, uninstaller; game icon (`scripts/make-icon.ps1` → `src/icon.ico`, also used for the exe).
- GitHub Release `v0.3.1-demo` with the installer and the zip.
- Steam prep: `steam/app_build.template.vdf`, `scripts/steam-upload.ps1` (SteamPipe via steamcmd), `docs/STEAM.md` checklist.
- Issues #5-#14 for every open item (playtest, audio, rebinding, merchant, art polish, self-pause bug, phase 3, itch.io, Steam, Apple/mobile); roadmap distribution table.

## 0.3.0 (2026-11-21)
- Title menu (continue, new game, 3 save slots, load, delete, options, quit); pause menu; altar menu (rest, level up, travel, relics).
- Save system: JSON slots, autosave at altars and after death, manual save. Options: graphics, audio, difficulty, accessibility, controls.
- Movement: sprint, double jump (Wings of Ash), wall slide + wall jump, ledge grab + climb, ladders, drop-through, air dash, plunge.
- Combat: heavy + charged attack, running and aerial attacks, block + parry, shield bash, guard break, executions, Wrath, flasks, stamina.
- Enemies on a shared AI base (detect, alert allies, chase, search, patrol): Grave Warden, Ghoul, Bone Archer (arrows can be parried back), Wisp.
- Boss: the Drowned Abbot (2 phases, shockwaves, unblockable sweep, summons, fire pillars).
- Souls-like progression: ashes, levels (Vitality, Strength, Endurance), death remnant, 5 relics, key items, flask shards.
- Full level "The Sunken Nave" from a text map (7 areas, hazards: spikes, pits, crumbling ledges, pendulums, fire jets; secrets; gate; key door).
- Synthesised music and ~25 new sound effects; damage numbers, blood, shockwaves.
- AI art pass for the enemies, the boss and the 13 new hero animations (`scripts/restyle.ps1 -Rig <name>`).
- docs/MECHANICS_BACKLOG.md: the owner's full mechanics list sorted into built / later / not for 2D.

## 0.2.0 (2026-11-21)
- AI art pipeline: setup-art-tools.ps1 (ComfyUI + SDXL + Pixel Art XL), artgen.ps1, hero-restyle.ps1, in-engine cleanup (--art-clean, --art-restyle, --export-rig).
- Hero sprite sheet repainted in the new style (src/art/hero.png); 4 hero concepts.
- docs/ROADMAP.md; issue drafts in work/github/issues.

## 0.1.0 (2026-11-21)
- First playable: knight (move, jump, combo, dodge, parry), Grave Warden, two altars, death/respawn.
- Gothic dungeon scene: parallax, candle lights, light shafts, fog, dust.
- Procedural placeholder art and sound; DevBridge + cot CLI; build/smoke/shot/run/bake-art scripts.

## 0.0.1 (2026-11-21)
- Project created.
