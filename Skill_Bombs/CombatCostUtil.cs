using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>
/// Vanilla attack cost discount lives in <see cref="Attack"/> getters:
/// <c>cost -= cost * 0.33f * GetSkillFactor(item.skillType)</c>.
/// Bombs items stay <c>None</c> (factor 0); when the toggle is on we apply the same line with Bombs.
/// Under Heimdiver Explosive Augmentation, BombSmoke also uses Beacon Deployment Efficiency.
/// </summary>
internal static class CombatCostUtil
{
  internal static void ApplyBombsCostDiscount(Attack attack, ref float cost)
  {
    if (cost <= 0f || !ThrowSteadiness.IsLocalArmedBomb(attack))
    {
      return;
    }

    string? prefabId = ThrowSteadiness.WeaponPrefabId(attack.m_weapon);
    if (HeimdiverBombs.OverrideActive
        && string.Equals(prefabId, "BombSmoke", System.StringComparison.Ordinal)
        && Settings.BeaconDeploymentEfficiency != null)
    {
      float reduce = Mathf.Clamp(Settings.BeaconDeploymentEfficiency.Value, 0, 100) / 100f;
      float beforeBeacon = cost;
      cost *= 1f - reduce;
      if (SkillBombsPlugin.Allows(LogLevel.Debug))
      {
        SkillBombsPlugin.LogAt(LogLevel.Debug,
          $"Beacon stamina BombSmoke {beforeBeacon:0.##} -> {cost:0.##} (−{reduce * 100f:0.#}%).");
      }

      if (cost <= 0f)
      {
        return;
      }
    }

    if (Settings.ScaleThrowStamina == null || !Settings.ScaleThrowStamina.Value)
    {
      return;
    }

    Character? character = attack.m_character;
    if (character == null)
    {
      return;
    }

    float t = character.GetSkillFactor(BombsSkill.Type);
    if (t <= 0f)
    {
      return;
    }

    float before = cost;
    cost -= cost * 0.33f * t;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"throw stamina {before:0.##} -> {cost:0.##} (Bombs factor {t:0.###}, vanilla −33% line).");
    }
  }
}
