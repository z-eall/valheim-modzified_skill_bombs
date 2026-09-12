using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

internal static class ThrowSteadiness
{
  internal const string ThrowBombAnimation = "throw_bomb";

  private static readonly HashSet<string> Listed = new(StringComparer.Ordinal);
  private static readonly HashSet<string> Armed = new(StringComparer.Ordinal);
  private static readonly HashSet<string> WarnedUnknown = new(StringComparer.Ordinal);
  private static readonly HashSet<string> WarnedNotThrowBomb = new(StringComparer.Ordinal);
  private static bool _warnedBadCurve;
  private static string? _armedLog;

  internal static void OnAllowlistChanged()
  {
    Listed.Clear();
    Armed.Clear();
    WarnedUnknown.Clear();
    WarnedNotThrowBomb.Clear();
    _armedLog = null;

    string raw = Settings.BombPrefabs?.Value ?? "";
    foreach (string token in raw.Split(','))
    {
      string id = token.Trim();
      if (id.Length == 0)
      {
        continue;
      }

      Listed.Add(id);
    }

    TryResolvePrefabs();
  }

  internal static void OnCurveChanged()
  {
    _warnedBadCurve = false;
    _ = Curve(0.5f);
  }

  internal static void TryResolvePrefabs()
  {
    if (Listed.Count == 0)
    {
      return;
    }

    if (!WorldReady())
    {
      return;
    }

    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      int items = ObjectDB.instance != null && ObjectDB.instance.m_items != null
        ? ObjectDB.instance.m_items.Count
        : 0;
      int prefabs = ZNetScene.instance.m_prefabs != null ? ZNetScene.instance.m_prefabs.Count : 0;
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"allowlist resolve: ObjectDB items={items}, ZNetScene prefabs={prefabs}, listed={Listed.Count}.");
    }

    foreach (string id in Listed)
    {
      GameObject? prefab = FindItemPrefab(id);
      if (prefab == null)
      {
        if (WarnedUnknown.Add(id))
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning, $"{SkillBombsPlugin.ModName}: unknown bomb prefab '{id}' — skipped.");
        }

        continue;
      }

      ItemDrop drop = prefab.GetComponent<ItemDrop>();
      if (drop == null)
      {
        if (WarnedUnknown.Add(id))
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning, $"{SkillBombsPlugin.ModName}: '{id}' is not an item — skipped.");
        }

        continue;
      }

      string anim = drop.m_itemData.m_shared.m_attack.m_attackAnimation ?? "";
      if (!anim.Equals(ThrowBombAnimation, StringComparison.Ordinal))
      {
        if (WarnedNotThrowBomb.Add(id))
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning,
            $"{SkillBombsPlugin.ModName}: '{id}' primary animation is '{anim}', not {ThrowBombAnimation} — ignored.");
        }

        continue;
      }

      Armed.Add(id);
    }

    LogArmedIfChanged();
  }

  internal static bool IsLocalArmedBomb(Attack attack)
  {
    if (attack == null || !BombsSkill.Ready)
    {
      return false;
    }

    Humanoid? character = attack.m_character;
    if (character == null || !character.IsPlayer() || character != Player.m_localPlayer)
    {
      return false;
    }

    if (!IsThrowBomb(attack))
    {
      return false;
    }

    string? id = WeaponPrefabId(attack.m_weapon);
    if (id == null)
    {
      return false;
    }

    if (Armed.Contains(id))
    {
      return true;
    }

    if (!Listed.Contains(id))
    {
      return false;
    }

    TryResolvePrefabs();
    return Armed.Contains(id);
  }

  internal static void TraceThrow(Attack attack, bool applied, float spreadDeg)
  {
    if (!SkillBombsPlugin.Allows(LogLevel.Debug) || attack == null)
    {
      return;
    }

    string id = WeaponPrefabId(attack.m_weapon) ?? "?";
    float vanilla = attack.m_projectileAccuracy;
    if (!applied)
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"throw {id}: no override (listed={Listed.Contains(id)}, armed={Armed.Contains(id)}, spread {vanilla:0.###}).");
      return;
    }

    Player player = Player.m_localPlayer;
    float level = player != null && player.GetSkills() != null
      ? player.GetSkills().GetSkillLevel(BombsSkill.Type)
      : 0f;
    float cap = LiveSkillCap.Read();
    int pct = Mathf.RoundToInt(CurrentSteadiness() * 100f);
    SkillBombsPlugin.LogAt(LogLevel.Debug,
      $"throw {id}: Bombs {level:0.#}/{cap:0.#}, steadiness {pct}%, spread {vanilla:0.###} -> {spreadDeg:0.###}");
  }

  internal static bool TryOverrideSpread(Attack attack, out float spreadDeg)
  {
    spreadDeg = 0f;
    if (!IsLocalArmedBomb(attack))
    {
      return false;
    }

    float vanilla = attack.m_projectileAccuracy;
    spreadDeg = vanilla * (1f - CurrentSteadiness());
    return true;
  }

  /// <summary>
  /// Skill-scaled launch help. Spawn height unchanged.
  /// Aims initial velocity toward a gravity-compensated point on the crosshair ray
  /// (full help through ~10 m, then falls off). Temp strength is uncapped (no Clamp01).
  /// </summary>
  internal static bool TryGetLaunchHelp(Attack attack, out float handLiftMeters, out float loftDegrees)
  {
    handLiftMeters = 0f;
    loftDegrees = 0f;
    if (!IsLocalArmedBomb(attack))
    {
      return false;
    }

    float strength = Settings.TempLaunchHelpStrength?.Value ?? 1f;
    if (strength <= 0f)
    {
      return false;
    }

    float skill = CurrentSteadiness();
    if (skill <= 0f)
    {
      return false;
    }

    float help = skill * strength;
    handLiftMeters = Settings.BaseMaxHandLiftMeters * Mathf.Min(help, 1f);
    if (!TryComputeLaunchAngleHelp(attack, help, out loftDegrees))
    {
      loftDegrees = -Settings.BaseMaxLoftDegrees * help;
    }

    return handLiftMeters > 0f || Mathf.Abs(loftDegrees) > 0.01f;
  }

  /// <summary>
  /// Vanilla <c>+m_launchAngle</c> pitches down. We apply the signed angle from eye-forward
  /// toward a point on the crosshair ray raised by estimated gravity drop over the flight.
  /// </summary>
  internal static bool TryComputeLaunchAngleHelp(Attack attack, float help, out float launchAngleDelta)
  {
    launchAngleDelta = 0f;
    Humanoid? character = attack.m_character;
    if (character == null)
    {
      return false;
    }

    Transform body = character.transform;
    Vector3 spawn = body.position
                    + body.up * attack.m_attackHeight
                    + body.forward * attack.m_attackRange
                    + body.right * attack.m_attackOffset;

    Vector3 vanillaAim = character.GetAimDir(spawn);
    if (vanillaAim.sqrMagnitude < 1e-8f)
    {
      return false;
    }

    vanillaAim.Normalize();
    if (!TryGetCrosshairAimPoint(character, out Vector3 aimPoint))
    {
      return false;
    }

    Vector3 toAim = aimPoint - spawn;
    float dist = toAim.magnitude;
    if (dist < 0.05f)
    {
      return false;
    }

    float vel = Mathf.Max(1f, attack.m_projectileVel);
    float gravity = ReadProjectileGravity(attack);
    // Time-of-flight ≈ path length / speed; raise the aim point by the drop gravity will add.
    float flightTime = dist / vel;
    float drop = 0.5f * gravity * flightTime * flightTime;
    Vector3 compensated = aimPoint + Vector3.up * drop;

    Vector3 desired = compensated - spawn;
    if (desired.sqrMagnitude < 1e-6f)
    {
      return false;
    }

    desired.Normalize();
    Vector3 axis = Vector3.Cross(Vector3.up, vanillaAim);
    if (axis.sqrMagnitude < 1e-8f)
    {
      return false;
    }

    // Full geometric+ballistic correction through 10 m; beyond that, help fades (far lobs stay lob-y).
    float rangeFactor = dist <= Settings.LaunchHelpFullRangeMeters
      ? 1f
      : Settings.LaunchHelpFullRangeMeters / dist;

    float full = Vector3.SignedAngle(vanillaAim, desired, axis);
    launchAngleDelta = full * help * rangeFactor;

    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      string id = WeaponPrefabId(attack.m_weapon) ?? "?";
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"launch ballistics {id}: dist {dist:0.#}m, vel {vel:0.#}, g {gravity:0.#}, drop {drop:0.###}m, range× {rangeFactor:0.###}, help {help:0.###}, Δ {launchAngleDelta:0.###}°");
    }

    return true;
  }

  internal static float ReadProjectileGravity(Attack attack)
  {
    GameObject? prefab = attack.m_attackProjectile;
    if (prefab != null)
    {
      Projectile? projectile = prefab.GetComponent<Projectile>();
      if (projectile != null && projectile.m_gravity > 0f)
      {
        return projectile.m_gravity;
      }
    }

    return Settings.DefaultBombGravity;
  }

  /// <summary>
  /// Crosshair is screen-center UI; the look ray that matches it is the <see cref="GameCamera"/>
  /// (same rotation as <c>m_eye</c>, but the camera sits behind/offset — body is not under the reticle).
  /// </summary>
  internal static bool TryGetCrosshairAimPoint(Humanoid character, out Vector3 aimPoint)
  {
    aimPoint = default;
    Transform? cam = null;
    if (GameCamera.instance != null)
    {
      cam = GameCamera.instance.transform;
    }
    else if (character is Player player && player.m_eye != null)
    {
      cam = player.m_eye;
    }

    if (cam == null)
    {
      return false;
    }

    Vector3 origin = cam.position;
    Vector3 forward = cam.forward;
    if (Physics.Raycast(origin, forward, out RaycastHit hit, 80f))
    {
      aimPoint = hit.point;
      return true;
    }

    aimPoint = origin + forward * Settings.LaunchAimFallbackDistance;
    return true;
  }

  internal static void TraceLaunchHelp(Attack attack, bool applied, float handLift, float loft)
  {
    if (!SkillBombsPlugin.Allows(LogLevel.Debug) || attack == null || !applied)
    {
      return;
    }

    string id = WeaponPrefabId(attack.m_weapon) ?? "?";
    float strength = Settings.TempLaunchHelpStrength?.Value ?? 1f;
    int pct = Mathf.RoundToInt(CurrentSteadiness() * 100f);
    SkillBombsPlugin.LogAt(LogLevel.Debug,
      $"launch help {id}: steadiness {pct}%, strength {strength:0.###}, hand +{handLift:0.###}m, launchAngle Δ {loft:0.###}°");
  }

  internal static float CurrentSteadiness()
  {
    Player player = Player.m_localPlayer;
    float level = player != null && player.GetSkills() != null
      ? player.GetSkills().GetSkillLevel(BombsSkill.Type)
      : 0f;
    float cap = LiveSkillCap.Read();
    float t = Mathf.Clamp01(level / cap);
    float u = Curve(t);
    float s0 = Settings.SteadinessAtSkill0.Value / 100f;
    float sMax = Settings.SteadinessAtSkillMax.Value / 100f;
    return Mathf.Lerp(s0, sMax, u);
  }

  internal static float Curve(float t)
  {
    string name = Settings.HowSteadinessImproves?.Value ?? Settings.CurveLinear;
    if (name.Equals(Settings.CurveQuickStart, StringComparison.Ordinal))
    {
      float oneMinus = 1f - t;
      return 1f - (oneMinus * oneMinus);
    }

    if (name.Equals(Settings.CurveSlowStart, StringComparison.Ordinal))
    {
      return t * t;
    }

    if (!name.Equals(Settings.CurveLinear, StringComparison.Ordinal) && !_warnedBadCurve)
    {
      _warnedBadCurve = true;
      SkillBombsPlugin.LogAt(LogLevel.Warning, $"{SkillBombsPlugin.ModName}: unknown curve '{name}'; using Linear.");
    }

    return t;
  }

  internal static bool IsThrowBomb(Attack attack)
  {
    return attack.m_attackAnimation != null
           && attack.m_attackAnimation.Equals(ThrowBombAnimation, StringComparison.Ordinal);
  }

  internal static string? WeaponPrefabId(ItemDrop.ItemData? weapon)
  {
    if (weapon?.m_dropPrefab == null)
    {
      return null;
    }

    return Utils.GetPrefabName(weapon.m_dropPrefab);
  }

  private static void LogArmedIfChanged()
  {
    if (Armed.Count == 0)
    {
      return;
    }

    var names = new List<string>(Armed);
    names.Sort(StringComparer.Ordinal);
    string summary = string.Join(", ", names);
    if (summary == _armedLog)
    {
      return;
    }

    _armedLog = summary;
    SkillBombsPlugin.LogAt(LogLevel.Info, $"{SkillBombsPlugin.ModName}: bomb prefabs armed ({Armed.Count}): {summary}");
  }

  internal static void OnWorldUnload()
  {
    Armed.Clear();
    WarnedUnknown.Clear();
    WarnedNotThrowBomb.Clear();
    _armedLog = null;
  }

  /// <summary>
  /// UpgradeWorld LateUpdate returns while <see cref="ZNet.instance"/> is null (main menu).
  /// Named prefabs exist after <see cref="ZNetScene.Awake"/>.
  /// </summary>
  private static bool WorldReady()
  {
    return ZNet.instance != null && ZNetScene.instance != null;
  }

  private static GameObject? FindItemPrefab(string id)
  {
    if (ZNetScene.instance != null)
    {
      GameObject scene = ZNetScene.instance.GetPrefab(id);
      if (scene != null)
      {
        return scene;
      }
    }

    if (ObjectDB.instance != null)
    {
      return ObjectDB.instance.GetItemPrefab(id);
    }

    return null;
  }
}
