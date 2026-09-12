using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>
/// Vanilla attack cost discount lives in <see cref="Attack"/> getters:
/// <c>cost -= cost * 0.33f * GetSkillFactor(item.skillType)</c>.
/// Bombs items stay <c>None</c> (factor 0); when the toggle is on we apply the same line with Bombs.
/// </summary>
internal static class CombatCostUtil
{
  internal static void ApplyBombsCostDiscount(Attack attack, ref float cost)
  {
    if (cost <= 0f
        || Settings.ScaleThrowStamina == null
        || !Settings.ScaleThrowStamina.Value
        || !ThrowSteadiness.IsLocalArmedBomb(attack))
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
    // Same identity as Attack.GetAttackStamina: cost -= cost * 0.33f * skillFactor
    cost -= cost * 0.33f * t;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"throw stamina {before:0.##} -> {cost:0.##} (Bombs factor {t:0.###}, vanilla −33% line).");
    }
  }
}
