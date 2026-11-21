# Mechanics backlog
**What:** every mechanic from the owner's wish list (2026-11-21), sorted into **built (v0.3)**, **later** (with the roadmap phase), or **not for 2D**. **Where:** docs/MECHANICS_BACKLOG.md
The list came from a third-person action game. Clash of Thrones is a 2D side-scroller, so camera/3D items are adapted or dropped, and nothing is copied by name: rage = our own **Wrath**, companions/runic powers = our own designs later.

Legend: ✅ built and tested · 🔜 later (phase) · ✖ not for 2D / not planned

## 1. Character movement
| ✅ Built | 🔜 Later | ✖ Not for 2D |
|---|---|---|
| walk (analog), run, sprint (hold dodge), jump (variable height), double jump (Wings of Ash), wall slide + wall jump, ledge grab + climb, drop down through ledges, falling, landing, dodge roll, air dash, ladders, moving platforms, jump across gaps, attack while falling (plunge), ground slam (plunge shockwave), crumbling ledges | wall climbing (P3 ability), vaulting/mantling small steps (P2 polish), quick turn animation (P2), slide (P3), grappling hook + swinging (P3 ability), ziplines (P3), swimming + diving (P3 water area) | contextual 3D jumping, free 3D traversal |

## 2. Combat
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| light attack, 3-hit combo, heavy attack, charged attack (guard break), running attack, jump/aerial attack, falling (plunge) attack, executions on staggered foes, counter (parry -> stagger -> execution), attack cancelling (roll out of recovery), knockback, stagger, interrupt (heavy breaks windups), guard break, AoE (Wrath burst, plunge shockwave), critical-style execution damage, hit-stop, screen shake | back attack bonus (P2), grab/throw (P3), weapon switching + 2nd weapon (P3), launchers + aerial combos (P3), elemental attacks + status effects: fire, frost, bleed (P2/P3), weak points on bosses (P3), environmental kills (spikes/pendulums vs enemies, P2) | — |

## 3. Defence
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| block (hold), perfect block = parry, shield bash, shield counter (parry -> execution), roll + air dash with i-frames, interrupting enemy attacks, breaking guards, deflecting projectiles (parried arrows fly back), unblockable attacks (red tell, must dodge), guard break when stamina runs out, invulnerability during executions/ledge climb | backstep (P2), damage resistance stats + armour (P3 equipment) | — |

## 4. Weapons
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| sword + shield (the knight's kit), weapon-specific heavy/charged/bash moves | axe, spear, greatsword, daggers, dual blades, bow/crossbow, hammer (P3: one new weapon per area), weapon upgrades (P2 smith), weapon throw + recall (P3, one special weapon), elemental infusions (P3) | durability (not wanted) |

## 5. Special abilities
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| **Wrath** (our rage): fills on hits dealt/taken, burst on activation, +50% damage, healing on hits, 10 s timer | prayers/spells with a resource (P3), ability cooldown UI (P3), ability upgrades (P3), summon/companion abilities (P4, optional) | — |

## 6-7. Skills, progression, statistics
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| souls-like level up at altars with **ashes**: Vitality (health), Strength (damage), Endurance (stamina); stamina; character level; relic passive bonuses | skill tree (P3), weapon levels (P3), luck/crit/attack speed stats (P3), elemental resistances (P3), skill respec (P4) | mana (stamina covers it) |

## 8. Health and resources
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| health bar, healing flasks (refill at altars), flask upgrades (Flask Shards), max-health upgrades (Vitality, Tallow Heart), Wrath meter (generation/consumption), ash (currency) collection, enemy stagger state, boss health bar | health regeneration relic (P2), status buildup bars (P3) | — |

## 9-10. Equipment, inventory, loot
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| relics (2 slots, equip/unequip, descriptions), key items (Wings of Ash, Rusted Key), consumable shards, loot drops (ashes), treasure chests, hidden items, item descriptions | armour slots + set bonuses (P3), crafting + materials (P3), merchant buy/sell (P2), rarity tiers (P3), item sorting/storage (P3) | — |

## 11. Enemy AI
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| idle, patrol (turns at walls and ledges), detection (sight range + line of sight), awareness ("!" alert), chase, attack with telegraph, poise, retreat (Ghoul leaps back, Archer backs off), call for assistance (alerts allies within 170 px), search last known position, return to patrol, stagger, death with loot. Types: heavy (Grave Warden), fast (Ghoul), ranged (Bone Archer), flying (Wisp), boss | block/dodge/parry by enemies (P2), flank/surround (P2), knockdown (P2), enrage for elites (P2), armoured + elite + mini-boss types (P2/P3) | — |
| **Boss (The Drowned Abbot):** health bar, 2 phases, attack patterns, special attacks (shockwaves, fire pillars, summons), unblockable sweep, enrage roar, arena gate | weak points, cinematic executions (P3) | — |

## 12. Companions
🔜 P4 (optional): one AI companion. ✖ for now: it fights against the lonely-knight tone of the first area.

## 13-15. Exploration, puzzles, environment
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| hidden passage (breakable wall), secret item (high ledge, needs double jump), unlockable areas (lever gate, locked door + key), ability gating (wall-jump shaft, double-jump gaps), fast travel between lit altars, optional challenges (archer reachable only by drop or reflected arrows), levers, gates, doors, chests, breakable walls, pickups, climb structures | map screen + discovery (P3), more puzzle types: pressure plates, weights, light, timing (P3), push/pull blocks (P3), elevators (P2), NPCs (P3) | — |

## 16. Quests
🔜 P3: a short quest journal with 2-3 NPC side quests. Main progression is the level itself (reach and defeat the Abbot).

## 17. UI
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| health, stamina, Wrath, flasks, ashes, boss bar, enemy health bars, damage numbers, interaction prompts, pickup/tutorial toasts, main menu, pause menu, options (graphics, audio, gameplay, controls), save/load (3 slots), level-up, travel, relics & items | map, quest journal, crafting/upgrade menus (P3), status-effect icons (P3), key rebinding (P2) | compass, lock-on camera |

## 18. Camera
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| tracking with look-ahead, smoothing, pixel-exact positions, level bounds, screen shake (can be switched off) | boss-arena framing, cinematic transitions (P2) | third-person/over-shoulder, rotation, zoom, collision, lock-on |

## 19-20. Animation, physics, systems
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| idle, run, jump, fall, land, roll, attacks (6 kinds), block, parry, bash, heal, hurt, stagger, death, execution, wall slide, ledge hang/climb, ladder; gravity, collision, hitboxes/hurtboxes, projectiles, ground/wall/ledge detection, acceleration/deceleration, damage calculation | sprint animation, weapon switch, swimming (P3), destructible props (P2) | root motion, IK, ragdolls |

## 21-22. Audio/visual feedback, general systems
| ✅ Built | 🔜 Later | ✖ |
|---|---|---|
| weapon, footstep, impact, block, parry, enemy, boss and UI sounds (synthesised placeholders), exploration/boss/menu music, hit sparks, blood, fire, shockwaves; save system, autosave at altars, manual save, checkpoints, respawning, death screen + ash remnant, difficulty (Easy/Normal/Hard), accessibility (shake + hit-stop toggles), environmental hazards (spikes, pits, pendulums, fire jets, crumbling ledges) | real recorded audio + composed music (P2), tutorials as signs (P2), achievements (P4), merchants + economy (P2), dialogue/NPCs (P3), stealth (P3), weather (P3) | day/night cycle (underground setting) |

## 23. Extra ideas from other genres
🔜 kept for later: air dash ✅ (built), wall run, gliding, teleport/blink (P3 ability candidates); backstab, disarm, berserk (Wrath covers it ✅); classes, talents, enchantments, legendary items (P3/P4). ✖ survival (hunger, thirst, temperature): doesn't fit this game.
