# Design overview
**Red line:** `Main` builds the level in code; actors get their art from `ArtLibrary`; `Game` adds feel; `DevBridge` exposes it all to tools.

| Part | Job | Talks to |
|---|---|---|
| Game (autoload) | input map, audio buses, hit-stop, shake | everyone |
| Main | builds layers, geometry, props, actors, camera, HUD; death/respawn loop | Player, Warden, Hud, Checkpoint |
| Player / Warden | movement, state machines, hitboxes | each other via `IDamageable` / `IParryable` |
| ArtLibrary | textures + sprite frames (generated or PNG override) | actors, props |
| DevBridge | TCP + startup script automation | whole scene tree |
Details: [TECHNICAL.md](TECHNICAL.md).