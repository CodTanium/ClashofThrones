# Decision log
| Date | Decision | Why | Alternatives |
|---|---|---|---|
| 2026-11-21 | Godot 4.7.2 .NET, C# on .NET 10 | strong 2D, free, C# tooling | Unity, GameMaker |
| 2026-11-21 | 640x360 internal, integer scale, hero ~90 px | crisp pixels at 1080p/1440p, detailed characters | 480x270 |
| 2026-11-21 | Placeholder art generated in code | original, consistent frames, zero licence risk, replaceable by PNG | CC0 packs |
| 2026-11-21 | Level built in code, not .tscn | easy to diff, review and change by text | hand-made scenes (later, for real levels) |
| 2026-11-21 | Own DevBridge + CLI instead of a third-party editor plugin | small, localhost-only, debug-only, no dependency | Godot MCP plugins |