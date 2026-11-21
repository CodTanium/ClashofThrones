# Test matrix
| Area | Smoke | Full | Command | Last result |
|---|---|---|---|---|
| Build | ✅ | - | `scripts\build.ps1` | 2026-11-21 ok, 0 errors |
| Menu → new game → movement + combat inputs | ✅ | - | `scripts\smoke.ps1` | 2026-11-21 PASS |
| Rendering of each level area | ✅ | - | `scripts\shot.ps1 -Script "tp x y; wait 1"` | 2026-11-21 screenshots 10-18 |
| Moves + combat states (ledge, ladder, wall slide, heal, block, bash, heavy, plunge, roll, sprint) | - | live via cot | see TECHNICAL.md | 2026-11-21 ok |
| Boss: trigger, phase 2, defeat, loot, exit shrine | - | live via cot | `cot call Actors/Boss DebugDamage N` | 2026-11-21 ok |
| Save → quit to menu → continue | - | live via cot | `cot give ...`, `Game.SaveRun()`, `Game.Continue(9)` | 2026-11-21 ok (hp 8, ashes 300, flasks 4, items kept) |
