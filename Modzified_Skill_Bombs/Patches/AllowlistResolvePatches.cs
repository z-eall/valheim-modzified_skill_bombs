using HarmonyLib;

namespace Modzified_Skill_Bombs.Patches;

/// <summary>
/// UpgradeWorld skips work until <c>ZNet.instance</c> exists (menu has no session).
/// Prefab ids are resolved after <see cref="ZNetScene.Awake"/> fills named prefabs.
/// </summary>
[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
internal static class ZNetScene_Awake_Allowlist_Patch
{
  private static void Postfix(ZNetScene __instance)
  {
    if (__instance == null || __instance.m_prefabs == null || __instance.m_prefabs.Count == 0)
    {
      return;
    }

    ThrowSteadiness.TryResolvePrefabs();
    BombsSkill.TryResolveIcon();
    HeimdiverItemDisplay.Refresh();
  }
}

[HarmonyPatch(typeof(Game), "Start")]
internal static class Game_Start_Allowlist_Patch
{
  private static void Postfix()
  {
    ThrowSteadiness.TryResolvePrefabs();
    BombsSkill.TryResolveIcon();
    HeimdiverItemDisplay.Refresh();
  }
}

/// <summary>FejdStartup fills ObjectDB via CopyOtherDB — item icons exist before a world session.</summary>
[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
internal static class ObjectDB_CopyOtherDB_Icon_Patch
{
  private static void Postfix(ObjectDB __instance)
  {
    if (__instance == null || __instance.m_items == null || __instance.m_items.Count == 0)
    {
      return;
    }

    BombsSkill.TryResolveIcon();
    HeimdiverItemDisplay.Refresh();
  }
}

[HarmonyPatch(typeof(ZNet), "OnDestroy")]
internal static class ZNet_OnDestroy_Allowlist_Patch
{
  private static void Prefix()
  {
    ThrowSteadiness.OnWorldUnload();
    HeimdiverItemDisplay.OnWorldUnload();
  }
}
