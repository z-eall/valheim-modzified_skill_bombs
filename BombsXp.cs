using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>One <see cref="Skills.RaiseSkill"/> of Bombs per throw. Stamp + in-memory mark carry the throw.</summary>
internal static class BombsXp
{
  internal const string ThrowerZdoKey = "skill_bombs_thrower";
  internal const string ThrowIdZdoKey = "skill_bombs_throw";

  internal static readonly int ThrowerHash = ThrowerZdoKey.GetStableHashCode();
  internal static readonly int ThrowIdHash = ThrowIdZdoKey.GetStableHashCode();

  private static long _nextThrowId = 1;
  private static Pending? _pending;
  private static Projectile? _spawnSource;
  private static readonly HashSet<long> Credited = new();

  private struct Pending
  {
    public long ThrowId;
    public long ThrowerPlayerId;
  }

  internal static void BeginThrow(Player player)
  {
    if (player == null || !BombsSkill.Ready)
    {
      return;
    }

    long id = player.GetPlayerID();
    if (id == 0L)
    {
      return;
    }

    _pending = new Pending { ThrowId = _nextThrowId++, ThrowerPlayerId = id };
  }

  internal static void EndThrow()
  {
    _pending = null;
  }

  internal static void EnterSpawnOnHit(Projectile projectile)
  {
    _spawnSource = projectile;
  }

  internal static void ExitSpawnOnHit()
  {
    _spawnSource = null;
  }

  internal static void AfterProjectileSetup(Projectile projectile, Character owner)
  {
    AfterSetup(projectile.gameObject, owner);
  }

  internal static void AfterAoeSetup(Aoe aoe, Character owner)
  {
    AfterSetup(aoe.gameObject, owner);
  }

  internal static void OnSpawnedFromBombVial(GameObject spawned)
  {
    if (_spawnSource == null || spawned == null)
    {
      return;
    }

    BombsThrowMark? src = _spawnSource.GetComponent<BombsThrowMark>();
    if (src == null || !src.IsValid)
    {
      return;
    }

    if (spawned.GetComponent<Character>() == null && spawned.GetComponent<Aoe>() == null && spawned.GetComponent<IProjectile>() == null)
    {
      return;
    }

    ApplyMark(spawned, src.ThrowId, src.ThrowerPlayerId);
    TryStampZdo(spawned);
    CombatDamage.TryApplyBlobStars(spawned);
  }

  internal static void TryStampZdo(GameObject go)
  {
    BombsThrowMark? mark = go.GetComponent<BombsThrowMark>();
    Character? character = go.GetComponent<Character>();
    ZNetView? view = go.GetComponent<ZNetView>();
    if (mark == null || !mark.IsValid || character == null || view == null)
    {
      return;
    }

    ZDO? zdo = view.GetZDO();
    if (zdo == null || !zdo.IsValid())
    {
      return;
    }

    zdo.Set(ThrowerHash, mark.ThrowerPlayerId);
    zdo.Set(ThrowIdHash, mark.ThrowId);
  }

  private readonly struct ThrowRef
  {
    public readonly long ThrowId;
    public readonly long ThrowerPlayerId;

    public ThrowRef(long throwId, long throwerPlayerId)
    {
      ThrowId = throwId;
      ThrowerPlayerId = throwerPlayerId;
    }

    public bool IsValid => ThrowId != 0L && ThrowerPlayerId != 0L;
  }

  internal static void TryCreditFromProjectile(Projectile projectile, Character? victim)
  {
    TryCredit(FirstValid(ReadMark(projectile.gameObject), ReadStamp(projectile.m_owner)), victim);
  }

  internal static void TryCreditFromAoe(Aoe aoe, Character? victim)
  {
    TryCredit(FirstValid(ReadMark(aoe.gameObject), ReadStamp(aoe.m_owner)), victim);
  }

  internal static void TryCreditFromHit(Character victim, HitData hit)
  {
    if (hit == null)
    {
      return;
    }

    TryCredit(ReadStamp(hit.GetAttacker()), victim);
  }

  private static void AfterSetup(GameObject go, Character? owner)
  {
    if (go == null)
    {
      return;
    }

    if (_pending.HasValue)
    {
      ApplyMark(go, _pending.Value.ThrowId, _pending.Value.ThrowerPlayerId);
      return;
    }

    if (ReadMark(go).IsValid)
    {
      return;
    }

    if (_spawnSource != null)
    {
      ThrowRef fromVial = ReadMark(_spawnSource.gameObject);
      if (fromVial.IsValid)
      {
        ApplyMark(go, fromVial.ThrowId, fromVial.ThrowerPlayerId);
        return;
      }
    }

    ThrowRef fromOwner = ReadStamp(owner);
    if (fromOwner.IsValid)
    {
      ApplyMark(go, fromOwner.ThrowId, fromOwner.ThrowerPlayerId);
    }
  }

  private static void ApplyMark(GameObject go, long throwId, long throwerId)
  {
    BombsThrowMark mark = go.GetComponent<BombsThrowMark>() ?? go.AddComponent<BombsThrowMark>();
    mark.ThrowId = throwId;
    mark.ThrowerPlayerId = throwerId;
  }

  private static ThrowRef ReadMark(GameObject go)
  {
    BombsThrowMark? mark = go.GetComponent<BombsThrowMark>();
    if (mark == null || !mark.IsValid)
    {
      return default;
    }

    return new ThrowRef(mark.ThrowId, mark.ThrowerPlayerId);
  }

  private static ThrowRef ReadStamp(Character? character)
  {
    if (character == null)
    {
      return default;
    }

    ThrowRef marked = ReadMark(character.gameObject);
    if (marked.IsValid)
    {
      return marked;
    }

    ZDO? zdo = character.m_nview != null ? character.m_nview.GetZDO() : null;
    if (zdo == null || !zdo.IsValid())
    {
      return default;
    }

    long thrower = zdo.GetLong(ThrowerHash, 0L);
    long throwId = zdo.GetLong(ThrowIdHash, 0L);
    if (thrower == 0L || throwId == 0L)
    {
      return default;
    }

    return new ThrowRef(throwId, thrower);
  }

  private static ThrowRef FirstValid(ThrowRef a, ThrowRef b)
  {
    return a.IsValid ? a : b;
  }

  private static void TryCredit(ThrowRef mark, Character? victim)
  {
    if (!BombsSkill.Ready || !mark.IsValid || victim == null)
    {
      return;
    }

    if (victim is Player victimPlayer && victimPlayer.GetPlayerID() == mark.ThrowerPlayerId)
    {
      return;
    }

    if (!Credited.Add(mark.ThrowId))
    {
      return;
    }

    Player? local = Player.m_localPlayer;
    if (local != null && local.GetPlayerID() == mark.ThrowerPlayerId)
    {
      local.RaiseSkill(BombsSkill.Type, BombsSkill.Def.m_increseStep);
      TraceCredit(mark.ThrowId, victim, local);
      return;
    }

    BombsXpRpc.NotifyThrower(mark.ThrowId, mark.ThrowerPlayerId);
  }

  internal static void RaiseForLocalThrower(long throwId, long throwerPlayerId)
  {
    Player? local = Player.m_localPlayer;
    if (!BombsSkill.Ready || local == null || local.GetPlayerID() != throwerPlayerId)
    {
      return;
    }

    if (!Credited.Add(throwId))
    {
      return;
    }

    local.RaiseSkill(BombsSkill.Type, BombsSkill.Def.m_increseStep);
    TraceCredit(throwId, null, local);
  }

  private static void TraceCredit(long throwId, Character? victim, Player local)
  {
    if (!SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      return;
    }

    float level = local.GetSkills() != null ? local.GetSkills().GetSkillLevel(BombsSkill.Type) : 0f;
    int pct = Mathf.RoundToInt(ThrowSteadiness.CurrentSteadiness() * 100f);
    string hit = victim != null ? victim.m_name : "?";
    SkillBombsPlugin.LogAt(LogLevel.Debug,
      $"Bombs XP: throw {throwId} hit {hit}, Bombs {level:0.#}, throw steadiness {pct}%");
  }

  internal static void AfterInstantiate(UnityEngine.Object spawned)
  {
    if (spawned is GameObject go)
    {
      OnSpawnedFromBombVial(go);
    }
  }
}
