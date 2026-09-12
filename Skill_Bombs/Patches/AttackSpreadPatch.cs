using HarmonyLib;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs.Patches;

[HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
internal static class Attack_FireProjectileBurst_Steadiness_Patch
{
  internal struct BurstState
  {
    public bool SpreadApplied;
    public float Accuracy;
    public bool SkillAccuracy;
    public bool LoftApplied;
    public float LaunchAngle;
  }

  private static void Prefix(Attack __instance, out BurstState __state)
  {
    __state = default;
    bool spreadApplied = ThrowSteadiness.TryOverrideSpread(__instance, out float spread);
    bool loftApplied = ThrowSteadiness.TryGetLaunchHelp(__instance, out float handLift, out float loft);
    if (loftApplied && Mathf.Abs(loft) <= 0.01f)
    {
      loftApplied = false;
    }

    if (SkillBombsPlugin.Allows(LogLevel.Debug)
        && __instance != null
        && ThrowSteadiness.IsThrowBomb(__instance)
        && __instance.m_character != null
        && __instance.m_character.IsPlayer()
        && __instance.m_character == Player.m_localPlayer)
    {
      ThrowSteadiness.TraceThrow(__instance, spreadApplied, spread);
      if (loftApplied || handLift > 0f)
      {
        ThrowSteadiness.TraceLaunchHelp(__instance, true, handLift, loft);
      }
    }

    if (spreadApplied && __instance != null)
    {
      __state.SpreadApplied = true;
      __state.Accuracy = __instance.m_projectileAccuracy;
      __state.SkillAccuracy = __instance.m_skillAccuracy;
      __instance.m_projectileAccuracy = spread;
      __instance.m_skillAccuracy = false;
    }

    if (__instance != null
        && Player.m_localPlayer != null
        && ThrowSteadiness.IsLocalArmedBomb(__instance))
    {
      BombsXp.BeginThrow(Player.m_localPlayer, ThrowSteadiness.WeaponPrefabId(__instance.m_weapon));
    }

    if (loftApplied && __instance != null)
    {
      __state.LoftApplied = true;
      __state.LaunchAngle = __instance.m_launchAngle;
      __instance.m_launchAngle = __state.LaunchAngle + loft;
    }
  }

  private static void Finalizer(Attack __instance, BurstState __state)
  {
    BombsXp.EndThrow();
    if (__instance == null)
    {
      return;
    }

    if (__state.SpreadApplied)
    {
      __instance.m_projectileAccuracy = __state.Accuracy;
      __instance.m_skillAccuracy = __state.SkillAccuracy;
    }

    if (__state.LoftApplied)
    {
      __instance.m_launchAngle = __state.LaunchAngle;
    }
  }
}

[HarmonyPatch(typeof(Attack), "GetProjectileSpawnPoint")]
internal static class Attack_GetProjectileSpawnPoint_LaunchHelp_Patch
{
  internal struct HeightState
  {
    public bool Applied;
    public float Height;
  }

  private static void Prefix(Attack __instance, out HeightState __state)
  {
    __state = default;
    if (!ThrowSteadiness.TryGetLaunchHelp(__instance, out float handLift, out _) || handLift <= 0f || __instance == null)
    {
      return;
    }

    __state.Applied = true;
    __state.Height = __instance.m_attackHeight;
    __instance.m_attackHeight = __state.Height + handLift;
  }

  private static void Finalizer(Attack __instance, HeightState __state)
  {
    if (!__state.Applied || __instance == null)
    {
      return;
    }

    __instance.m_attackHeight = __state.Height;
  }
}
