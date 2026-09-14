using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Skill_Bombs.Patches;

/// <summary>
/// Under Heimdiver Explosive Augmentation, map player-facing Dynamite Trajectory Calibration
/// (<see cref="Settings.DynamiteLandDistance"/>) to live <c>Attack.m_projectileVel</c> for BombDynamite only.
/// </summary>
[HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
internal static class Attack_FireProjectileBurst_HeimdiverDynamite_Patch
{
  private static void Prefix(Attack __instance, out float? __state)
  {
    __state = null;
    if (__instance == null
        || !HeimdiverBombs.OverrideActive
        || Settings.DynamiteLandDistance == null
        || !ThrowSteadiness.IsLocalArmedBomb(__instance))
    {
      return;
    }

    string? id = ThrowSteadiness.WeaponPrefabId(__instance.m_weapon);
    if (!string.Equals(id, "BombDynamite", System.StringComparison.Ordinal))
    {
      return;
    }

    float distance = Settings.DynamiteLandDistance.Value;
    float vel = Settings.VanillaDynamiteProjectileVel * distance;
    __state = __instance.m_projectileVel;
    __instance.m_projectileVel = vel;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"Heimdiver Dynamite Trajectory Calibration {distance:0.###} → vel {vel:0.###} (was {__state.Value:0.###}).");
    }
  }

  private static void Finalizer(Attack __instance, float? __state)
  {
    if (__state.HasValue && __instance != null)
    {
      __instance.m_projectileVel = __state.Value;
    }
  }
}
