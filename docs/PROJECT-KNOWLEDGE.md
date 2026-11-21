# Project knowledge
Facts learned while working, newest first.
- 2026-11-21: `--headless` cannot take screenshots (dummy renderer); use `scripts\shot.ps1` (small window) instead.
- 2026-11-21: arguments with spaces to Godot via `Start-Process` must be quoted, or `--script=...` is split.
- 2026-11-21: only the .NET 10 runtime is installed on the dev PC, so the project targets `net10.0`.