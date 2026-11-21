# Game design document
**What:** the design of Clash of Thrones (working title). **Updated:** 2026-11-21

## Pitch
A strictly 2D side-scrolling dark-fantasy action platformer. A lone knight descends through a drowned cathedral.
Combat is deliberate: read the tell, dodge or parry, punish. Every hit lands with weight (hit-stop, shake, sparks).

## Pillars
1. **Weighty, readable combat**: clear windups, short punishing windows, no button mashing.
2. **Gothic atmosphere**: candlelight in darkness, layered architecture, fog and dust.
3. **Exploration** (later): interconnected areas, shortcuts, abilities that open new paths.

## The knight
| Move | How it works |
|---|---|
| Run | accelerates to 150 px/s, stops quickly |
| Jump | 400 px/s; release early for a short hop; 0.1 s coyote time, 0.13 s buffer |
| Attack combo | 3 hits; press again during a swing to chain. Hit 3 is heavy (2 damage, big knockback) |
| Dodge | low dash, 0.34 s, invulnerable from 0.02 to 0.28 s; can cancel the end of an attack |
| Parry | raise the shield; a hit from the front within 0.2 s is parried: the attacker staggers 1.5 s and takes double damage |
| Hurt | knockback + 0.9 s invulnerability. 6 health |

## Enemy: Grave Warden
Hooded, hulking grave-keeper with a cleaver. 7 health. Patrols ±70 px, sees 210 px, attacks within 62 px.
Windup 0.67 s (last frame flashes a bright rim = the tell), swing 0.29 s, recovery 0.6 s (the punish window).
Keeps its poise while swinging (normal hits don't interrupt); a heavy hit breaks the windup.

## Level: The Sunken Nave (first playable)
2560 px long hall: start altar (lit), ledges, a raised section, a second altar at x=1500, two Wardens.
Death → "YOU HAVE FALLEN" → fade → respawn at the last lit altar, enemies reset.

## Tuning
All numbers live as constants at the top of `src/scripts/actors/Player.cs` and `Warden.cs`.

## Roadmap
| Stage | Content |
|---|---|
| First playable ✅ | this document |
| Vertical slice | 3 enemy types, 1 boss, main menu, save at altars, music + ambience, final hero art |
| Alpha | 3 connected areas, 2 traversal abilities, map |