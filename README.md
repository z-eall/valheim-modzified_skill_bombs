# Skill_Bombs

Adds a **Bombs** skill. Throws land closer to the crosshair as the skill rises. Optional damage, stamina, and free throw are off by default.

Install on all clients and on the server (listen-host counts as the host). If the server has the mod, host config wins.

## Features

- Skills tab row **Bombs** (vanilla bomb icon).
- Less random miss and slight aim help toward the crosshair as skill rises. Skill 0 matches vanilla throws.
- XP once per throw when a creature is first hurt (vial, cloud, or thrown blob). Smoke cloud does not give cloud XP. Empty field and self-hit give none.
- Optional host toggles: scale throw damage, cheaper throw stamina, chance to keep the bomb (free throw).

## How to use

1. Install the plugin on every client and the host/dedicated.
2. Open the skills tab — **Bombs** should appear.
3. Throw listed bombs. Higher Bombs = steadier throws.
4. Optional combat options are in config section `2. Damage and stamina` (all off until you turn them on).
5. Console: `raiseskill Bombs 50` / `resetskill Bombs`.

Listed bombs use the normal bomb throw. Spears and staffs are ignored even if listed.

If **ProjectileTweaks** is loaded, leave bomb spread at `1`. If **MaxAxe** is loaded, leave UseThrowingSkill off unless you want both.

## Configuration

File: `BepInEx/config/skill_bombs.cfg`.

General and combat math sync from the host when the server has the mod. Logging, free-throw text/effect toggles, and temporary launch tune stay local.

- Throw steadiness at skill 0 / max: how steady untrained vs max skill throws are (0 = vanilla miss, 100 = no random miss + full aim help).
- How steadiness improves: Linear, Quick start, or Slow start.
- Bomb prefabs: comma-separated ids (exact case). Must use the bomb throw animation.
- Scale throw damage: stronger flask hits and clouds; blob star chance. Lava/dynamite flask stay 0. Smoke cloud is not grown.
- Scale throw stamina: up to 33% cheaper throws at skill max.
- Free throw: chance to keep the bomb (`chance at skill max` × skill). Default max chance 25%.
- Free throw text / bonus text / bonus effect: message and feedback when a free throw procs.
- Launch help strength: temporary local feel tune (will be removed once locked).
- Log levels: what the mod writes to the BepInEx log.

## Player commands

None.

## Admin commands

None. Use vanilla `raiseskill` / `resetskill` with **Bombs**.

## Credits

Sources: [GitHub](https://github.com/z-eall/valheim-skill_bombs)
