# Guide
How to build, test and run Clash of Thrones.
| Task | Command |
|---|---|
| Build | `.\scripts\build.ps1` |
| Smoke test (headless, simulated input) | `.\scripts\smoke.ps1` |
| Screenshot | `.\scripts\shot.ps1 [-Out file] [-Script "..."]` |
| Play / editor | `.\scripts\run.ps1` / `.\scripts\run.ps1 -Editor` |
| Talk to the running game | `.\scripts\cot.ps1 help` |
| Bake art to PNG | `.\scripts\bake-art.ps1 [-Use]` |
| AI art tools (once) / generate | `.\scripts\setup-art-tools.ps1` / `.\scripts\artgen.ps1 -Prompt "..."` |
| Hero sheet in the AI style | `.\scripts\hero-restyle.ps1` |

## End of session
1. `.\scripts\smoke.ps1` passes.
2. Commit + push this repo (`pre-alpha`).
3. Push the private project notes too (see issue #3).