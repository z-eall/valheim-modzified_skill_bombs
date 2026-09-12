using System.Reflection;
using HarmonyLib;

namespace Skill_Bombs.Patches;

[HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
internal static class Attack_FireProjectileBurst_CombatDamage_Patch
{
  private static void Prefix(Attack __instance)
  {
    CombatDamage.BeginFlaskRedirect(__instance);
  }

  private static void Finalizer()
  {
    CombatDamage.EndFlaskRedirect();
  }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetRandomSkillFactor))]
internal static class Player_GetRandomSkillFactor_CombatDamage_Patch
{
  private static bool Prefix(Player __instance, Skills.SkillType skill, ref float __result)
  {
    if (!CombatDamage.TryRedirectRandomSkillFactor(__instance, skill, out float factor))
    {
      return true;
    }

    __result = factor;
    return false;
  }
}

[HarmonyPatch(typeof(Aoe), "OnHit")]
internal static class Aoe_OnHit_CombatDamage_Patch
{
  private static void Prefix(Aoe __instance)
  {
    CombatDamage.BeginCloudHit(__instance);
  }

  private static void Finalizer(Aoe __instance)
  {
    CombatDamage.EndCloudHit(__instance);
  }
}

[HarmonyPatch(typeof(Aoe), "GetDamage", typeof(int))]
internal static class Aoe_GetDamage_CombatDamage_Patch
{
  private static void Postfix(Aoe __instance, ref HitData.DamageTypes __result)
  {
    CombatDamage.ScaleCloudDamageTypes(__instance, ref __result);
  }
}

/// <summary>
/// Mirror vanilla <c>GetAttackStamina</c> skill line with Bombs (items stay None).
/// DeclaredMethod so the private getter is bound reliably.
/// </summary>
[HarmonyPatch]
internal static class Attack_GetAttackStamina_Combat_Patch
{
  private static MethodBase TargetMethod()
    => AccessTools.DeclaredMethod(typeof(Attack), "GetAttackStamina");

  private static void Postfix(Attack __instance, ref float __result)
  {
    CombatCostUtil.ApplyBombsCostDiscount(__instance, ref __result);
  }
}

[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
internal static class Attack_GetAttackEitr_Combat_Patch
{
  private static void Postfix(Attack __instance, ref float __result)
  {
    CombatCostUtil.ApplyBombsCostDiscount(__instance, ref __result);
  }
}

[HarmonyPatch]
internal static class Attack_GetAttackHealth_Combat_Patch
{
  private static MethodBase TargetMethod()
    => AccessTools.DeclaredMethod(typeof(Attack), "GetAttackHealth");

  private static void Postfix(Attack __instance, ref float __result)
  {
    CombatCostUtil.ApplyBombsCostDiscount(__instance, ref __result);
  }
}

[HarmonyPatch(typeof(Attack), "ConsumeItem")]
internal static class Attack_ConsumeItem_FreeThrow_Patch
{
  private static bool Prefix(Attack __instance)
  {
    if (!FreeThrowLogic.TryProc(__instance))
    {
      return true;
    }

    FreeThrowLogic.PlayFeedbackSafe(Player.m_localPlayer);
    return false;
  }
}
