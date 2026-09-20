# Modzified_Skill_Bombs

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
  - **BombSmoke:** vial hit only (5 blunt). The smoke cloud does not train Bombs.
- Empty field and hitting yourself give no XP.
- Optional **XP Gain Blocklist**: host YAML rules that block Bombs XP (and Heimdiver throw-counter XP) when the player matches. Empty by default.
- Optional server-synced toggles: scale throw damage, cheaper throw stamina, chance to keep the bomb (free throw).

## How to use

1. Install the plugin on every client and the server. Also install **YamlDotNet** (`ValheimModding-YamlDotNet` on Thunderstore) — one shared copy for the whole load order.
2. Open the skills tab — **Bombs** should appear.
3. Throw listed bombs. Higher Bombs = steadier throws.
4. Optional combat options are in config section `2. Damage and stamina` (all off until you turn them on).
5. Optional XP blocklist: edit `BepInEx/config/modzified_skill/bombs/modzified_skill_bombs_xp_blocklist.yaml` on the host (see [XP Gain Blocklist](docs/xp_gain_blocklist.md)).

Listed bombs use the normal bomb throw. Spears and staffs are ignored even if listed.

## Configuration

File: `BepInEx/config/modzified_skill_bombs.cfg`.

Server-synced keys follow the host when the server has the mod. Free-throw feedback toggles stay local.

- Throw steadiness at skill 0 / max: how steady throws are (0 = vanilla miss, 100 = no miss + full aim help).
- How steadiness improves: Linear, Quick start, or Slow start.
- Bomb prefabs: which items use Bombs (comma-separated ids, exact case; must use the bomb throw).
- Scale throw damage / stamina / free throw: optional combat toggles (off until you turn them on).
- Free throw chance / text / bonus text / bonus effect: free-throw odds and feedback.
- Heimdiver Science keys: see **Heimdiver Science** below.
- XP Gain Blocklist: YAML file on disk — see [XP Gain Blocklist](docs/xp_gain_blocklist.md) (not cfg keys).

## Console commands

`raiseskill Bombs 50` / `resetskill Bombs`

## Heimdiver Science

Optional host section for DhakhaR's **HEIMDIVERS** (Helldivers II in Valheim with Expand World Mods). [Intro video](https://youtu.be/yoscS1CJWTE?si=gIVeFDL8KCgtH9w5) · [Heimdiver Discord](https://discord.gg/JCeymsZvE7)

In Configuration Manager the HEIMDIVERS briefing shows as on-page text (not only a tooltip).

- **Heimdiver Explosive Augmentation** (off by default):
  - Master switch for Heimdiver bomb rules.
  - When on, built-in Augmentation and the options below apply to Heimdiver bombs.
  - When off, that Augmentation is removed — normal Modzified_Skill_Bombs.
  - Server-synced.
- **Dynamite Trajectory Calibration**:
  - How far BombDynamite travels under Augmentation.
  - `1` matches the vanilla short throw. Raise it to send the bomb farther away.
  - Relative scale — not meters on the ground.
  - Only BombDynamite. Ignored when Augmentation is off.
- **Beacon Deployment Efficiency**:
  - How much throw stamina BombSmoke saves under Augmentation.
  - `0` = full cost. `100` = free.
  - Only BombSmoke. Ignored when Augmentation is off.
  - Server-synced. Default `100`.

## XP Gain Blocklist

- Host file for rules that stop Bombs XP. Edit the YAML on disk — not the `.cfg`.
- File: `BepInEx/config/modzified_skill/bombs/modzified_skill_bombs_xp_blocklist.yaml`
- Empty or comments only = no XP blocked. Server-synced.
- Save on the host; if nothing changes, check the BepInEx log.
- Field list and examples: [XP Gain Blocklist](docs/xp_gain_blocklist.md)
## Mod compatibility

- **ProjectileTweaks:** leave bomb spread at `1` so both mods do not fight over aim cone.
- **MaxAxe:** leave UseThrowingSkill off unless you want both systems active.

## Credits

Huge thanks to DhakhaR for the mod icon!

Source: [<img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> GitHub](https://github.com/z-eall/valheim-modzified_skill_bombs)
