# Game design document
**What:** the design of Clash of Thrones (working title). **Updated:** 2026-11-21 · v0.3.0
Every mechanic and its status: [MECHANICS_BACKLOG.md](MECHANICS_BACKLOG.md) · phases: [ROADMAP.md](ROADMAP.md)

## Pitch
A strictly 2D side-scrolling dark-fantasy action platformer. A lone knight descends through a drowned cathedral.
Combat is deliberate: read the tell, dodge, block or parry, punish. Every hit lands with weight.

## Pillars
1. **Weighty, readable combat**: clear windups, short punishing windows, stamina, no button mashing.
2. **Gothic atmosphere**: candlelight in darkness, layered architecture, fog and dust.
3. **Exploration**: abilities open paths (wall-jump shaft, double-jump gaps), secrets reward curiosity.

## The knight
| Move | How it works |
|---|---|
| Walk / run / sprint | analog stick < 50% walks (70 px/s), run 150 px/s, **hold dodge** to sprint 215 px/s (18 stamina/s) |
| Jump | 400 px/s; release early = short hop; 0.1 s coyote time, 0.13 s buffer |
| Double jump | after finding **Wings of Ash** (top of the shaft) |
| Wall slide / wall jump | hold toward a wall while falling: slide at 70 px/s; jump pushes off up and away |
| Ledge grab / climb | falling next to a ledge top catches it; up or jump climbs, down lets go |
| Ladders | up/down on a ladder; jump to let go |
| Drop through | down + jump on a one-way ledge |
| Roll | tap dodge on the ground: 0.38 s, invulnerable 0.04-0.30 s, 22 stamina |
| Air dash | dodge in the air, once per jump, invulnerable, 16 stamina |
| Light combo | 3 hits (10/10/18 dmg), 10 stamina each; can cancel the end into a roll |
| Running attack | attack while sprinting: lunge, 16 dmg |
| Aerial attack | attack in the air (2 per jump), 10 dmg |
| Plunge | down + attack in the air: fast fall, shockwave 20 dmg in 72 px |
| Heavy / charged | heavy 24 dmg; hold 0.6 s+ for a charged 40 dmg hit that breaks guards and windups |
| Block | hold block: hits from the front cost stamina (18 per damage point); empty stamina = guard broken |
| Parry | the first 0.2 s of a block deflects a parryable hit: the enemy staggers (double damage) and arrows fly back |
| Shield bash | attack while blocking: short shove, interrupts, breaks guards |
| Execution | attack next to a staggered enemy (gold ring above it): 60 dmg, invulnerable |
| Heal | flask: restores 45% health, 0.8 s, can walk slowly; flasks refill at altars |
| Wrath | bar fills from hits dealt and taken; activate when full: burst, +50% damage, 1 health per 3 hits, 10 s |
Damage is scaled by Strength (+12% per level) and the Ember Sigil relic.

## Enemies
| Enemy | Role | Health | Behaviour |
|---|---|---|---|
| Grave Warden | heavy | 80 | slow, 0.67 s windup with a bright tell, keeps poise while swinging, 2 damage |
| Ghoul | fast | 35 | rushes at 115 px/s, quick claw lunge, then leaps back out of reach |
| Bone Archer | ranged | 30 | keeps 100-280 px away, aimed arrows (block them, or parry them back for 25 dmg) |
| Wisp | flying | 20 | hovers, then swoops through the player; summoned by the boss |
All enemies patrol (turning at walls and ledge edges), spot the player with line of sight, call allies within 170 px,
chase, search the last known position for 3.5 s, then return to patrol. Health scales with difficulty.

## Boss: The Drowned Abbot (700 health)
| Phase | Attacks |
|---|---|
| 1 | staff slam (parryable; sends shockwaves both ways along the floor - jump them), wide sweep (red tint = unblockable, roll through it), summons 2 wisps |
| 2 (below 50%) | roar (pushes the player back), 25% faster, fire pillars erupt under the player every 3.2 s (0.9 s warning glow) |
The arena gate closes when the fight starts and opens when he falls; the exit shrine then lets you ring the bell (level complete).

## Progression (souls-like)
- **Ashes** drop from enemies (Warden 60, Ghoul 25, Archer 30, Wisp 15, Abbot 1500) and chests.
- At an **altar**: Rest (heal, refill flasks, enemies return, autosave), Level Up, Travel (between lit altars), Relics & Items.
- Level up costs 60 ashes, +18% per level: **Vitality** +1 health, **Strength** +12% damage, **Endurance** +20 stamina.
- **Death**: your ashes stay where you fell as a glowing remnant; touch it to take them back; die first and they're lost.
- **Relics** (2 slots): Ember Sigil (+15% damage), Tallow Heart (+2 health), Iron Vow (cheaper blocking), Thirsting Blade (heal every 8th hit), Pilgrim's Sandal (cheaper rolls/sprint).
- **Key items**: Wings of Ash (double jump), Rusted Key (reliquary door). **Flask Shard**: +1 flask.

## Level: The Sunken Nave
| # | Area | Teaches / contains |
|---|---|---|
| 1 | Entrance hall | first altar, a step (jump) and a high block (ledge grab), one Ghoul, a chest |
| 2 | Crumbling pit | crumbling ledges over spikes, an Archer above (drop onto it from the side room or parry its arrows), ladder to a side room with the **Ember Sigil**; a cracked wall hides a **Flask Shard** |
| 3 | Warden hall | two Wardens, swinging pendulum blades, a lever that opens the gate |
| 4 | Hollow shaft | wall-jump climb with rest ledges and a Wisp; **Wings of Ash** and the second altar at the top |
| 5 | Upper galleries | moving platform, double-jump gaps over spikes, fire jets, Archer, Ghoul, a high secret (**Pilgrim's Sandal**), chest with the **Rusted Key**, ladder down |
| 6 | Crypt | Warden, Ghoul, Archer in a low corridor; the locked reliquary (**Tallow Heart**, **Iron Vow**, third altar) |
| 7 | Boss arena | the Drowned Abbot, then the exit shrine |
The map is a text file ([src/levels/sunken_nave.txt](../src/levels/sunken_nave.txt)); one character = one 32 px tile.

## Options
Graphics (window mode, scale 2-4x, V-Sync, max FPS, FPS counter) · Audio (master, music, effects) · Gameplay (difficulty Easy/Normal/Hard, screen shake, hit-stop) · Controls (reference).

## Tuning
All numbers are constants at the top of `src/scripts/actors/Player.cs`, `EnemyBase.cs`, `Enemies.cs`, `Abbot.cs`, and in `SaveData` (`src/scripts/core/SaveSystem.cs`).
