using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>Shared HD throw counter under Heimdiver Bomb Override (session statics).</summary>
internal static class HeimdiverThrowXp
{
  private static int _count;
  private static int _threshold;
  private static readonly HashSet<long> CounterModeThrowIds = new();

  /// <summary>Call after <see cref="BombsXp.BeginThrow"/> armed a local HD throw under override.</summary>
  internal static void OnHdThrowBegun(long throwId, string? prefabId, Player player)
  {
    if (player == null || !HeimdiverBombs.AppliesTo(prefabId))
    {
      return;
    }

    if (XpBlocklistMatcher.IsDenied(player, out string ruleName))
    {
      // Still suppress hit XP for this HD throw; do not increment counter or RaiseSkill.
      CounterModeThrowIds.Add(throwId);
      if (SkillBombsPlugin.Allows(LogLevel.Debug))
      {
        SkillBombsPlugin.LogAt(LogLevel.Debug,
          $"Heimdiver throw XP blocked by rule '{ruleName}' (throw {throwId}, {prefabId}).");
      }

      return;
    }

    CounterModeThrowIds.Add(throwId);
    if (_threshold < 1)
    {
      _threshold = Random.Range(1, 6);
    }

    _count++;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"Heimdiver throw counter: {_count}/{_threshold} ({prefabId}, throw {throwId}).");
    }

    if (_count < _threshold)
    {
      return;
    }

    _count = 0;
    _threshold = Random.Range(1, 6);
    if (!BombsSkill.Ready)
    {
      return;
    }

    player.RaiseSkill(BombsSkill.Type, BombsSkill.Def.m_increseStep);
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      float level = player.GetSkills() != null ? player.GetSkills().GetSkillLevel(BombsSkill.Type) : 0f;
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"Heimdiver throw-counter XP: throw {throwId}, Bombs {level:0.#}, next threshold {_threshold}.");
    }
  }

  internal static bool SuppressHitXp(long throwId) =>
    HeimdiverBombs.OverrideActive && throwId != 0L && CounterModeThrowIds.Contains(throwId);
}
