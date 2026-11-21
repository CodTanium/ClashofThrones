# Project status
**What:** where Clash of Thrones stands. **Updated:** 2026-11-21 · version 0.3.0 (pre-alpha) · roadmap: [ROADMAP.md](ROADMAP.md)
✅ = verified with test output or a screenshot.

## Milestone 3: menus, saves, mechanics, full level (v0.3.0)
| Item | Status | Evidence |
|---|---|---|
| Title menu: Continue, New Game, Load Game (3 slots + delete), Options, Quit | ✅ | screenshot 10 |
| Save system: 3 slots, autosave at altars and after death, manual save in pause | ✅ | save → menu → continue keeps health 8, ashes 300, flasks 4, items, relics |
| Options: graphics, audio, difficulty, shake/hit-stop toggles, controls page | ✅ builds and opens | — |
| Pause menu, altar menu (rest, level up, travel, relics) | ✅ | screenshot 18 |
| Movement: sprint, double jump, wall slide + jump, ledge grab + climb, ladders, drop-through, roll, air dash, plunge | ✅ | live test: states WallSlide, Ladder, Heal, Block, Bash, Heavy, Plunge, Roll; sprint 215 px/s |
| Combat: combo, heavy + charge, run/air attacks, block, parry, bash, executions, Wrath, flasks, stamina | ✅ | live test (heal 3→6 hp, bash -14 stamina, heavy -20) |
| Enemies: Warden, Ghoul, Archer (+ arrows), Wisp; detection, alert, search, patrol | ✅ | screenshots 13, 14 |
| Boss: Drowned Abbot, 2 phases, shockwaves, sweep, summons, fire pillars, arena gate, exit shrine | ✅ | live test: wake → phase 2 → defeat → 1500 ashes → "[E] Ring the bell"; screenshot 17 |
| Full level: 7 areas, 3 altars, chests, secrets, lever gate, key door, hazards | ✅ | screenshots 11-16 |
| AI art pass: enemies, boss, new hero animations | see [CHANGELOG](../CHANGELOG.md) | — |
| Smoke test (menu → new game → inputs) | ✅ | `scripts\smoke.ps1` PASS |

## Earlier milestones
- **v0.2.0**: local AI art pipeline, hero repainted. **v0.1.0**: first playable (knight, Warden, altars, dungeon scene).

## Next
- Owner playtest: speeds, timings, difficulty, level flow. Then hand polish on the AI-painted sheets.
- Phase 2 items in [ROADMAP.md](ROADMAP.md): real audio, merchant, key rebinding, more enemy behaviours.
