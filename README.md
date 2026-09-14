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
  - **BombSmoke:** vial hit only (5 blunt). The smoke cloud does not train Bombs.
- Empty field and hitting yourself give no XP.
- Optional **XP Gain Blocklist**: host YAML rules that block Bombs XP (and Heimdiver throw-counter XP) when the player matches. Empty by default.
- Optional server-synced toggles: scale throw damage, cheaper throw stamina, chance to keep the bomb (free throw).

## How to use

1. Install the plugin on every client and the server. Also install **YamlDotNet** (`ValheimModding-YamlDotNet` on Thunderstore) — one shared copy for the whole load order.
2. Open the skills tab — **Bombs** should appear.
3. Throw listed bombs. Higher Bombs = steadier throws.
4. Optional combat options are in config section `2. Damage and stamina` (all off until you turn them on).
5. Optional XP blocklist: edit `BepInEx/config/skill_bombs/skill_bombs_xp_blocklist.yaml` on the host (see **XP Gain Blocklist** below).

Listed bombs use the normal bomb throw. Spears and staffs are ignored even if listed.

## Heimdiver Science

Optional host section for DhakhaR's **HEIMDIVERS** (Helldivers II in Valheim with Expand World Mods). [Intro video](https://youtu.be/yoscS1CJWTE?si=gIVeFDL8KCgtH9w5) · [Heimdiver Discord](https://discord.gg/JCeymsZvE7)

In Configuration Manager the HEIMDIVERS briefing shows as on-page text (not only a tooltip).

- **Heimdiver Explosive Augmentation** (off by default): master switch — built-in Augmentation plus the options below for Heimdiver bombs. Off removes that Augmentation (normal Skill_Bombs). Built-in includes throw-counter XP, smoke/blob strip, Dynamite without launch help, and English Helldivers-style names for the HD set (`BombSmoke`, `BombBlob_Lava`, `BombBlob_Frost`, `BombBlob_PoisonElite`, `BombLava`, `BombDynamite`). `BombBlob_Poison` stays a normal Bombs item.
- **Dynamite Trajectory Calibration**: how far **BombDynamite** travels under Augmentation. `1` = vanilla short throw; raise to send it farther. Relative scale — not meters. Ignored when Augmentation is off.
- **Beacon Deployment Efficiency**: how much throw stamina **BombSmoke** saves under Augmentation (`0` = full cost, `100` = free). Default `100`. Ignored when Augmentation is off.

## XP Gain Blocklist

Host file (server-synced): `BepInEx/config/skill_bombs/skill_bombs_xp_blocklist.yaml`.

Empty file or comments only = no XP blocked. Save the file on the host; changes apply shortly. If nothing changes, check the BepInEx log.

### How rules match

- List of rules. Each needs a unique `name`. (Empty / comments only = nothing blocked.)
- Inside **one** rule, **all** listed checks must be true together.
- Among **multiple** rules, only **one** listed rule needs to match to block Bombs XP (Heimdiver throw-counter does not advance either).
- Checked at the **player’s** position when XP would be granted.

### Keys (spell these exactly)

| Key | Where | Meaning |
|-----|--------|---------|
| `name` | Top level of rule | Required label for this rule. Must be unique. |
| `biomes` | Top level of rule | List of biome names where the rule can match (e.g. `Meadows`). Always a list — not `biome:`. |
| `minY` / `maxY` | Top level of rule | Lowest / highest world height the player may stand in for this rule. |
| `position` | Top level of rule | Map center as `x,z` (two numbers). Draws a flat ring on the ground. Must be used with `maxDistance`. |
| `minDistance` / `maxDistance` | Top level of rule | How close / far the player must be from that map center `position`. Set `maxDistance` whenever you set `position`. |
| `objects` | Top level of rule | List of nearby things that must be present for the rule. |
| `objectsLimit` | Top level of rule | How many of those nearby checks must succeed. Leave unset to require every listed object. |
| `prefab` | Nested under `objects` | The object this check is about (e.g. `Greyling`, `piece_workbench`). |
| `maxDistance` / `minDistance` | Nested under `objects` | How far / close that object may be from the player. Default max is **100** meters. |
| `minHeight` / `maxHeight` | Nested under `objects` | How much higher or lower that object may sit than the player (meters). Only under `objects`. |
| `filter` | Nested under `objects` | One extra requirement on that object’s saved data (e.g. `int, level, 3` for a 2★ creature). |
| `filters` | Nested under `objects` | Several of those requirements at once (a list). |
| `bannedFilter` / `bannedFilters` | Nested under `objects` | Same idea, but the rule fails if these match. |
| `filterLimit` | Nested under `objects` | How many of the required filters must succeed. Default = all of them. |

Wrong or unknown keys are rejected on load — check the log.

Indent with 2 spaces. List dashes line up with the parent key.

### Learn more (mentor references)

- Data filters (`filter` / `filters` / `bannedFilter` / …): [World Edit Commands — data](https://github.com/JereKuusela/valheim-world_edit_commands/blob/main/README_data.md)
- Nearby objects and `objectsLimit` examples: [Expand World Prefabs — object filtering](https://github.com/JereKuusela/valheim-expand_world_prefabs/blob/main/examples_object_filtering.md)

### Examples

```yaml
- name: MeadowsNoXp
  biomes:
  - Meadows
```

```yaml
- name: NearWorkbench
  objects:
  - prefab: piece_workbench
    maxDistance: 32
```

```yaml
- name: HighRingStar3Greyling
  position: 320,56
  maxDistance: 50
  minY: 3000
  maxY: 20000
  objects:
  - prefab: Greyling
    maxDistance: 15
    filter: int, level, 3
  - prefab: Boar
    maxDistance: 15
    filter: int, level, 2
  objectsLimit: 1
```

Several data checks on one object:

```yaml
  - prefab: Wolf
    maxDistance: 15
    filters:
    - int, level, 1
    - int, tamed, 1
```

## Mod compatibility

- **ProjectileTweaks:** leave bomb spread at `1` so both mods do not fight over aim cone.
- **MaxAxe:** leave UseThrowingSkill off unless you want both systems active.

## Configuration

File: `BepInEx/config/skill_bombs.cfg`.

Server-synced keys follow the host when the server has the mod. Free-throw feedback toggles stay local.

- Throw steadiness at skill 0 / max: how steady throws are (0 = vanilla miss, 100 = no miss + full aim help).
- How steadiness improves: Linear, Quick start, or Slow start.
- Bomb prefabs: which items use Bombs (comma-separated ids, exact case; must use the bomb throw).
- Scale throw damage / stamina / free throw: optional combat toggles (off until you turn them on).
- Free throw chance / text / bonus text / bonus effect: free-throw odds and feedback.
- Heimdiver Science keys: see **Heimdiver Science** above.
- XP Gain Blocklist: see **XP Gain Blocklist** above (YAML file, not cfg keys).

## Console commands

`raiseskill Bombs 50` / `resetskill Bombs`

## Credits

Huge thanks to DhakhaR for the mod icon!

Source: [<img src="https://cdn.simpleicons.org/github/181717" width="16" height="16" alt="" /> GitHub](https://github.com/z-eall/valheim-skill_bombs)
