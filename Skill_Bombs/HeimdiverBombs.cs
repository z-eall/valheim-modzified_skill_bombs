using System;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>Hardcoded HD bomb set and Heimdiver Bomb Override gate (Heimdiver Science).</summary>
internal static class HeimdiverBombs
{
  internal static readonly string[] PrefabIds =
  {
    "BombSmoke",
    "BombBlob_Lava",
    "BombBlob_Frost",
    "BombBlob_PoisonElite",
    "BombLava",
    "BombDynamite"
  };

  /// <summary>Smoke + three HD blobs — strip vial <c>m_spawnOnHit</c> under override (not Lava/Dynamite).</summary>
  private static readonly string[] StripSpawnPrefabIds =
  {
    "BombSmoke",
    "BombBlob_Lava",
    "BombBlob_Frost",
    "BombBlob_PoisonElite"
  };

  internal static bool OverrideActive =>
    Settings.HeimdiverBombOverride != null && Settings.HeimdiverBombOverride.Value;

  internal static bool IsHeimdiverBomb(string? prefabId)
  {
    if (prefabId is not { Length: > 0 })
    {
      return false;
    }

    for (int i = 0; i < PrefabIds.Length; i++)
    {
      if (string.Equals(prefabId, PrefabIds[i], StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  internal static bool IsStripSpawnTarget(string? prefabId)
  {
    if (prefabId is not { Length: > 0 })
    {
      return false;
    }

    for (int i = 0; i < StripSpawnPrefabIds.Length; i++)
    {
      if (string.Equals(prefabId, StripSpawnPrefabIds[i], StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Override on and prefab is in the HD set — later tickets wire counter / Dynamite here.</summary>
  internal static bool AppliesTo(string? prefabId) => OverrideActive && IsHeimdiverBomb(prefabId);

  /// <summary>
  /// Live vial only: clear <see cref="Projectile.m_spawnOnHit"/> so smoke cloud / blob never spawn.
  /// Leaves flask blunt / <c>m_damage</c> alone. Prefab assets untouched — override off = next throw vanilla.
  /// </summary>
  internal static void TryStripSpawnOnHit(Projectile projectile)
  {
    if (projectile == null || !OverrideActive)
    {
      return;
    }

    BombsThrowMark? mark = projectile.GetComponent<BombsThrowMark>();
    string? prefabId = mark != null ? mark.PrefabId : null;
    if (!IsStripSpawnTarget(prefabId))
    {
      return;
    }

    if (projectile.m_spawnOnHit == null && projectile.m_spawnOnHitChance <= 0f)
    {
      return;
    }

    projectile.m_spawnOnHit = null;
    projectile.m_spawnOnHitChance = 0f;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"Heimdiver strip spawn-on-hit for {prefabId}.");
    }
  }
}
