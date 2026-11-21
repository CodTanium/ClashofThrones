# Project knowledge
Facts learned while working, newest first.
- 2026-11-21: Windows PowerShell 5.1 `Get-Content -Raw` reads UTF-8 without BOM as ANSI and garbles ✅, · and emoji. Edit docs with a UTF-8-aware tool, or pass `-Encoding UTF8`.
- 2026-11-21: AI restyle denoise: 0.42 only smooths the rig; 0.58-0.68 adds real detail and keeps the pose; lying-down death frames need ~0.5.
- 2026-11-21: `--headless` cannot take screenshots (dummy renderer); use `scripts\shot.ps1` (small window) instead.
- 2026-11-21: arguments with spaces to Godot via `Start-Process` must be quoted, or `--script=...` is split.
- 2026-11-21: only the .NET 10 runtime is installed on the dev PC, so the project targets `net10.0`.