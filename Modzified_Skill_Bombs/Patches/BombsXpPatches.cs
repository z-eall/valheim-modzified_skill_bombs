using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Modzified_Skill_Bombs.Patches;

[HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
internal static class Projectile_Setup_BombsXp_Patch
{
  private static void Postfix(Projectile __instance, Character owner)
  {
    BombsXp.AfterProjectileSetup(__instance, owner);
  }
}

[HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
internal static class Aoe_Setup_BombsXp_Patch
{
  private static void Postfix(Aoe __instance, Character owner)
  {
    BombsXp.AfterAoeSetup(__instance, owner);
  }
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
internal static class Projectile_OnHit_BombsXp_Patch
{
  private static void Postfix(Projectile __instance, Collider collider)
  {
    Character? victim = BombsXpHitUtil.CharacterFromCollider(collider);
    BombsXp.TryCreditFromProjectile(__instance, victim, collider);
  }
}

[HarmonyPatch(typeof(Aoe), "OnHit")]
internal static class Aoe_OnHit_BombsXp_Patch
{
  private static void Postfix(Aoe __instance, Collider collider, bool __result)
  {
    if (!__result)
    {
      return;
    }

    Character? victim = BombsXpHitUtil.CharacterFromCollider(collider);
    BombsXp.TryCreditFromAoe(__instance, victim);
  }
}

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class Character_Damage_BombsXp_Patch
{
  private static void Prefix(Character __instance, HitData hit)
  {
    BombsXp.TryCreditFromHit(__instance, hit);
  }
}

[HarmonyPatch(typeof(Projectile), "SpawnOnHit")]
internal static class Projectile_SpawnOnHit_BombsXp_Patch
{
  private static void Prefix(Projectile __instance)
  {
    BombsXp.EnterSpawnOnHit(__instance);
  }

  private static void Finalizer()
  {
    BombsXp.ExitSpawnOnHit();
  }

  private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    MethodInfo stamp = AccessTools.Method(typeof(BombsXp), nameof(BombsXp.AfterInstantiate));
    foreach (CodeInstruction ins in instructions)
    {
      yield return ins;
      if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo mi && mi.Name == "Instantiate" && mi.DeclaringType == typeof(Object))
      {
        yield return new CodeInstruction(OpCodes.Dup);
        yield return new CodeInstruction(OpCodes.Call, stamp);
      }
    }
  }
}

[HarmonyPatch(typeof(ZNetView), "Awake")]
internal static class ZNetView_Awake_BombsXp_Patch
{
  private static void Postfix(ZNetView __instance)
  {
    if (__instance != null)
    {
      BombsXp.TryStampZdo(__instance.gameObject);
    }
  }
}

[HarmonyPatch(typeof(Game), "Start")]
internal static class Game_Start_BombsXpRpc_Patch
{
  private static void Postfix()
  {
    BombsXpRpc.Register();
  }
}

[HarmonyPatch(typeof(ZNet), "OnDestroy")]
internal static class ZNet_OnDestroy_BombsXpRpc_Patch
{
  private static void Prefix()
  {
    BombsXpRpc.ResetRegistrationFlag();
  }
}

internal static class BombsXpHitUtil
{
  internal static Character? CharacterFromCollider(Collider collider)
  {
    if (collider == null)
    {
      return null;
    }

    GameObject hit = Projectile.FindHitObject(collider);
    return hit != null ? hit.GetComponent<Character>() : null;
  }
}
