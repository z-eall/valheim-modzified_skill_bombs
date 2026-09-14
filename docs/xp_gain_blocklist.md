# XP Gain Blocklist

Back to the [main README](../README.md).

## How rules match

- List of rules. Each needs a unique `name`.
- Inside **one** rule, **all** listed checks must be true together.
- Among **multiple** rules, only **one** listed rule needs to match to block Bombs XP (Heimdiver throw-counter does not advance either).
- Checked at the **player’s** position when XP would be granted.

## Keys (spell these exactly)

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

## Learn more

- Data filters (`filter` / `filters` / `bannedFilter` / …): [World Edit Commands — data](https://github.com/JereKuusela/valheim-world_edit_commands/blob/main/README_data.md)
- Nearby objects and `objectsLimit` examples: [Expand World Prefabs — object filtering](https://github.com/JereKuusela/valheim-expand_world_prefabs/blob/main/examples_object_filtering.md)

## Examples

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
