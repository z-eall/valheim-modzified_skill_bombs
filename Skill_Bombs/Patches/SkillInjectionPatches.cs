using System.Collections.Generic;
using HarmonyLib;

namespace Skill_Bombs.Patches;

[HarmonyPatch(typeof(Skills), nameof(Skills.Awake))]
internal static class Skills_Awake_Bombs_Patch
{
  private static void Postfix(Skills __instance)
  {
    BombsSkill.EnsureDefOn(__instance);
  }
}

[HarmonyPatch(typeof(Skills), "IsSkillValid")]
internal static class Skills_IsSkillValid_Bombs_Patch
{
  private static void Postfix(Skills.SkillType type, ref bool __result)
  {
    if (BombsSkill.Ready && type == BombsSkill.Type)
    {
      __result = true;
    }
  }
}

[HarmonyPatch(typeof(Skills), "GetSkillDef")]
internal static class Skills_GetSkillDef_Bombs_Patch
{
  private static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result)
  {
    if (BombsSkill.Ready && type == BombsSkill.Type && __result == null)
    {
      __result = BombsSkill.Def;
    }
  }
}

[HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
internal static class Skills_CheatRaiseSkill_Bombs_Patch
{
  private static bool Prefix(Skills __instance, string name, float value, bool showMessage)
  {
    if (!BombsSkill.Ready)
    {
      return true;
    }

    if (BombsSkill.IsTabName(name))
    {
      BombsSkill.CheatRaise(__instance, value, showMessage);
      return false;
    }

    return true;
  }

  private static void Postfix(Skills __instance, string name, float value)
  {
    if (!BombsSkill.Ready || name == null || name.ToLowerInvariant() != "all")
    {
      return;
    }

    BombsSkill.CheatRaise(__instance, value, showMessage: false);
  }
}

[HarmonyPatch(typeof(Skills), nameof(Skills.CheatResetSkill))]
internal static class Skills_CheatResetSkill_Bombs_Patch
{
  private static bool Prefix(Skills __instance, string name)
  {
    if (!BombsSkill.Ready)
    {
      return true;
    }

    if (BombsSkill.IsTabName(name))
    {
      BombsSkill.CheatReset(__instance, showMessage: true);
      return false;
    }

    return true;
  }

  private static void Postfix(Skills __instance, string name)
  {
    if (!BombsSkill.Ready || name == null || name.ToLowerInvariant() != "all")
    {
      return;
    }

    BombsSkill.CheatReset(__instance, showMessage: false);
  }
}

[HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
internal static class SkillsDialog_Setup_Bombs_Patch
{
  private static void Prefix(Player player)
  {
    BombsSkill.TryResolveIcon();
    if (player != null)
    {
      BombsSkill.EnsureOn(player.GetSkills());
    }
  }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
internal static class Player_OnSpawned_Bombs_Patch
{
  private static void Postfix(Player __instance)
  {
    if (__instance == Player.m_localPlayer)
    {
      BombsSkill.EnsureOn(__instance.GetSkills());
    }
  }
}

[HarmonyPatch(typeof(Localization), "SetupLanguage")]
internal static class Localization_SetupLanguage_Bombs_Patch
{
  private static void Postfix(bool __result)
  {
    if (__result)
    {
      BombsSkill.RegisterTokens();
      HeimdiverItemDisplay.Refresh();
    }
  }
}

[HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.GetTabOptions))]
internal static class ConsoleCommand_GetTabOptions_Bombs_Patch
{
  private static void Postfix(Terminal.ConsoleCommand __instance, List<string> __result)
  {
    if (!BombsSkill.Ready || __result == null)
    {
      return;
    }

    string command = __instance.Command;
    if (command != "raiseskill" && command != "resetskill")
    {
      return;
    }

    if (!__result.Contains(BombsSkill.TabName))
    {
      __result.Add(BombsSkill.TabName);
    }
  }
}
