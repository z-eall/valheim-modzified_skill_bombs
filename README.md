# Skill_Bombs

[![Find me here](https://img.shields.io/badge/Find_me_here-Discord-5865F2?logo=discord&logoColor=white&style=flat)](https://discord.gg/VFRJcPwUdm)
[![Support](https://img.shields.io/badge/Support-Ko--fi-FF5E5B?logo=ko-fi&logoColor=white&style=flat)](https://ko-fi.com/zeall)

Adds a **Bombs** skill so throws land closer to the crosshair as you improve. Optional damage, stamina, and free throw stay off until you turn them on.

Made for DhakhaR's Heimdiver server — a custom Helldivers experience in Valheim. [Intro video](https://youtu.be/yoscS1CJWTE?si=gIVeFDL8KCgtH9w5) · [Heimdiver Discord](https://discord.gg/JCeymsZvE7)

Install on all clients and the server. Server-synced settings follow the host when the server has the mod.

## Features

- Skills tab row **Bombs** (vanilla bomb icon).
- Less random miss and slight aim help toward the crosshair as skill rises. Skill 0 matches vanilla throws.
- XP once per throw when a creature is first hurt by that throw:
  - Most bombs: vial hit, poison/blast cloud, or thrown blob.
  - **BombSmoke:** vial hit only by default (5 blunt). The smoke cloud does not train Bombs.
  - Optional **BombSmoke ground XP** (off by default): flask hitting the ground also trains — for Heimdiver-style stratagem throws.
- Empty field (unless that toggle is on) and hitting yourself give no XP.
- Optional server-synced toggles: scale throw damage, cheaper throw stamina, chance to keep the bomb (free throw).

## How to use

1. Install the plugin on every client and the server.
2. Open the skills tab — **Bombs** should appear.
3. Throw listed bombs. Higher Bombs = steadier throws.
4. Optional combat options are in config section `2. Damage and stamina` (all off until you turn them on).

Listed bombs use the normal bomb throw. Spears and staffs are ignored even if listed.

## Mod compatibility

- **ProjectileTweaks:** leave bomb spread at `1` so both mods do not fight over aim cone.
- **MaxAxe:** leave UseThrowingSkill off unless you want both systems active.

## Configuration

File: `BepInEx/config/skill_bombs.cfg`.

Server-synced keys follow the host when the server has the mod. Free-throw feedback toggles and temporary launch tune stay local.

- Throw steadiness at skill 0 / max: how steady untrained vs max skill throws are (0 = vanilla miss, 100 = no random miss + full aim help).
- How steadiness improves: Linear, Quick start, or Slow start.
- Bomb prefabs: comma-separated ids (exact case). Must use the bomb throw animation.
- Scale throw damage: stronger flask hits and clouds; blob star chance. Lava/dynamite flask stay 0. Smoke cloud is not grown.
- Scale throw stamina: up to 33% cheaper throws at skill max.
- Free throw: chance to keep the bomb (`chance at skill max` × skill). Default max chance 25%.
- Free throw text / bonus text / bonus effect: message and feedback when a free throw procs.
- BombSmoke ground XP: off = creature flask hit only; on = ground hits also train. Server-synced.
- Launch help strength: temporary local feel tune (will be removed once locked).

## Console commands

`raiseskill Bombs 50` / `resetskill Bombs`

## Credits

Huge thanks to DhakhaR for the mod icon!

Source: [<img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> GitHub](https://github.com/z-eall/valheim-skill_bombs)
