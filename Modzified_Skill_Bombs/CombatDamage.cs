using BepInEx.Logging;
using UnityEngine;

namespace Modzified_Skill_Bombs;

/// <summary>Scale throw damage helpers (flask redirect flag, cloud mult, blob stars).</summary>
internal static class CombatDamage
{
  private static bool _redirectFlaskToBombs;

  private static Aoe? _cloudScaleAoe;
  private static float _cloudMult = 1f;

  internal static bool DamageToggleOn => Settings.ScaleThrowDamage != null && Settings.ScaleThrowDamage.Value;

  internal static void BeginFlaskRedirect(Attack attack)
  {
    _redirectFlaskToBombs = false;
    if (!DamageToggleOn || !BombsSkill.Ready || !ThrowSteadiness.IsLocalArmedBomb(attack))
    {
      return;
    }

    ItemDrop.ItemData? weapon = attack.m_weapon;
    if (weapon == null || !weapon.GetDamage().HaveDamage())
    {
      return;
    }

    _redirectFlaskToBombs = true;
  }

  internal static void EndFlaskRedirect()
  {
    _redirectFlaskToBombs = false;
  }

  internal static bool TryRedirectRandomSkillFactor(Player player, Skills.SkillType skill, out float factor)
  {
    factor = 0f;
    if (!_redirectFlaskToBombs || skill != Skills.SkillType.None || player == null || player.m_skills == null)
    {
      return false;
    }

    factor = player.m_skills.GetRandomSkillFactor(BombsSkill.Type);
    return true;
  }

  internal static void BeginCloudHit(Aoe aoe)
  {
    _cloudScaleAoe = null;
    _cloudMult = 1f;
    if (!DamageToggleOn || aoe == null || !BombsSkill.Ready)
    {
      return;
    }

    BombsThrowMark? mark = aoe.GetComponent<BombsThrowMark>();
    if (mark == null || !mark.IsValid)
    {
      return;
    }

    Character? owner = aoe.m_owner;
    if (owner == null || !owner.IsPlayer())
    {
      return;
    }

    float roll = owner.GetRandomSkillFactor(BombsSkill.Type);
    _cloudMult = Mathf.Max(1f, roll / 0.4f);
    _cloudScaleAoe = aoe;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"cloud damage grow ×{_cloudMult:0.###} (roll {roll:0.###}).");
    }
  }

  internal static void EndCloudHit(Aoe aoe)
  {
    if (_cloudScaleAoe == aoe)
    {
      _cloudScaleAoe = null;
      _cloudMult = 1f;
    }
  }

  internal static void ScaleCloudDamageTypes(Aoe aoe, ref HitData.DamageTypes damage)
  {
    if (_cloudScaleAoe != aoe || _cloudMult <= 1.0001f)
    {
      return;
    }

    damage.Modify(_cloudMult);
  }

  internal static void TryApplyBlobStars(GameObject spawned)
  {
    if (!DamageToggleOn || !BombsSkill.Ready || spawned == null)
    {
      return;
    }

    Character? blob = spawned.GetComponent<Character>();
    if (blob == null)
    {
      return;
    }

    BombsThrowMark? mark = spawned.GetComponent<BombsThrowMark>();
    if (mark == null || !mark.IsValid)
    {
      return;
    }

    Player? local = Player.m_localPlayer;
    if (local == null || mark.ThrowerPlayerId != local.GetPlayerID())
    {
      return;
    }

    float t = local.GetSkillFactor(BombsSkill.Type);
    float r = Random.value;
    int stars;
    if (r < 0.10f * t * t)
    {
      stars = 2;
    }
    else if (r < 0.50f * t)
    {
      stars = 1;
    }
    else
    {
      stars = 0;
    }

    blob.SetLevel(1 + stars);
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"blob stars {stars} (t={t:0.###}, r={r:0.###}).");
    }
  }
}
