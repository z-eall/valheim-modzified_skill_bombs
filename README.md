# Skill_Bombs

BepInEx + Harmony mod: vanilla-tab **Bombs** skill — **throw steadiness** (spread + launch help), optional host **damage / stamina / free throw** (all default **off**).

## Identity

| Layer | Value |
|---|---|
| Thunderstore | **Zeall/Skill_Bombs** |
| Plugin title | **Skill_Bombs** |
| Skills tab / `raiseskill` | **Bombs** |
| BepInEx GUID | `skill_bombs` |

## How to use

1. Install on **clients and the host/dedicated**. If the server has the mod, **host config wins** (listen-host counts). If the server does not have the mod, your local cfg applies.
2. `dotnet build -c Release` writes `Skill_Bombs.dll` to `mods\Skill_Bombs\plugins\`. Copy that folder (or the DLL) into `Valheim\BepInEx\plugins\Skill_Bombs\` when you want to test (Gale: copy into that profile’s plugins).
3. Open the vanilla **skills** tab: a **Bombs** row should appear (icon = vanilla **BombSmoke** item sprite).
4. Throw listed bombs (`throw_bomb`). As **Bombs** rises: less random miss (**spread**), and initial aim pitches toward the **crosshair / camera ray** (**launch help** — gravity/speed stay vanilla). Skill **0** keeps vanilla launch.
5. **XP:** first creature hurt this throw — vial, Aoe cloud (not smoke), or stamped blob / spit. Empty field and self-hit give 0.
6. Optional combat (section `2. Damage and stamina`, host, **off** by default):
   - **Scale throw damage** — staff-style flask grow (incl. blob 5 blunt face-hits); cloud floor-at-today up to ~2.5×; blob star chance. Lava/dynamite flask stay 0. Smoke cloud not grown.
   - **Scale throw stamina** — up to 33% cheaper throw at skill max (same formula as vanilla weapon skills inside `GetAttackStamina`; Debug log shows `throw stamina X -> Y`). Bombs base cost is ~8, so the bar move is small.
   - **Free throw** — chance to keep the bomb: `(chance at skill max) × skill factor` (default max **25%**). White DamageText uses host **Free throw text** (default `Freethrow!`); craft effect is local.
7. Cheats: `raiseskill Bombs 50` / `resetskill Bombs` (console level print; no HUD toast).

### Temporary launch tune

Section **`9. Temporary launch tune`** → **`Launch help strength`** (local, uncapped). Dump after feel-good hardcode.

If **ProjectileTweaks** is loaded, leave bomb spread at `1`. If **MaxAxe** is loaded, leave UseThrowingSkill off unless you want both systems.

## Player commands

None — config only.

## Admin commands

None.

## Config

`BepInEx/config/skill_bombs.cfg`. Sections `1. General` and `2. Damage and stamina` (except local free-throw feedback) are host-enforced when the server has the mod. `3. Logging`, free-throw bonus text/effect, and `9. Temporary launch tune` stay local.

| Key | Default | Meaning |
|---|---|---|
| `Throw steadiness at skill 0` | 0 | Vanilla spread + launch when untrained |
| `Throw steadiness at skill max` | 100 | No spread + full launch help (× temp strength) |
| `How steadiness improves` | Linear | Linear / Quick start / Slow start |
| `Bomb prefabs` | listed bombs | Exact case; must use `throw_bomb` |
| `Scale throw damage` | off | Flask + clouds + blob stars |
| `Scale throw stamina` | off | −33% throw cost at skill max |
| `Free throw` | off | Chance not to consume |
| `Free throw chance at skill max` | 25 | Percent at skill ceiling |
| `Free throw bonus text` | on | Local; show DamageText on proc |
| `Free throw text` | Freethrow! | Host; white Normal DamageText string |
| `Free throw bonus effect` | on | Local; craft bonus VFX on proc |
| `Launch help strength` | 1 | TEMPORARY uncapped loft multiplier |
| `Log levels` | Fatal…Info | Check Debug here + BepInEx for traces |

Thunderstore author when published: **Zeall**.
