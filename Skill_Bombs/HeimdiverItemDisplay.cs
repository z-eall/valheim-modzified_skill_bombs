using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>
/// English Heimdiver item name/description while Augmentation is on.
/// Uses <see cref="Localization.AddWord"/> on existing <c>$item_…</c> tokens and clears
/// the Localize LRU cache — mutating SharedData alone does not update live inventory UI.
/// </summary>
internal static class HeimdiverItemDisplay
{
  private sealed class Snapshot
  {
    public string NameKey = "";
    public string NameVanilla = "";
    public string DescKey = "";
    public string DescVanilla = "";
  }

  private static readonly Dictionary<string, (string Name, string Description)> HdCopy =
    new(StringComparer.Ordinal)
    {
      ["BombLava"] = (
        "G-16 Impact Grenade",
        "A high explosive grenade which detonates on first impact"),
      ["BombBlob_Frost"] = (
        "G-23 Stun Grenade",
        "Temporarily stuns all targets within the effective radius"),
      ["BombBlob_Lava"] = (
        "G-123 Thermite Grenade",
        "A thermite grenade designed to adhere to surfaces before burning at 2000°C"),
      ["BombDynamite"] = (
        "TED-63 Dynamite",
        "Creates a far-reaching incendiary blast against the enemies of Freedom"),
      ["BombSmoke"] = (
        "G-1 Stratagem Beacon",
        "An advanced transponder and targeting module allowing accurate delivery of ordinance and equipment"),
      ["BombBlob_PoisonElite"] = (
        "G-4 Gas Grenade",
        "Releases a cloud of caustic gas that damages and softens targets")
    };

  private static readonly Dictionary<string, Snapshot> Originals = new(StringComparer.Ordinal);
  private static bool _applied;

  internal static void OnWorldUnload()
  {
    Restore();
    Originals.Clear();
  }

  internal static void Refresh()
  {
    if (HeimdiverBombs.OverrideActive)
    {
      Apply();
    }
    else
    {
      Restore();
    }
  }

  private static void Apply()
  {
    Localization? loc = Localization.instance;
    ObjectDB? db = ObjectDB.instance;
    if (loc == null || db?.m_items == null)
    {
      return;
    }

    int applied = 0;
    foreach (KeyValuePair<string, (string Name, string Description)> pair in HdCopy)
    {
      ItemDrop? drop = FindItem(db, pair.Key);
      ItemDrop.ItemData.SharedData? shared = drop?.m_itemData?.m_shared;
      if (shared == null)
      {
        continue;
      }

      string nameRaw = shared.m_name;
      string descRaw = shared.m_description;

      // 0.2.9 mutated SharedData to plain English — put tokens back before AddWord.
      if (!TryTokenKey(nameRaw, out string nameKey))
      {
        nameKey = "item_" + pair.Key.ToLowerInvariant();
        shared.m_name = "$" + nameKey;
        nameRaw = shared.m_name;
      }

      if (!TryTokenKey(descRaw, out string descKey))
      {
        descKey = nameKey + "_description";
        if (descRaw == null || !descRaw.StartsWith("$", StringComparison.Ordinal))
        {
          shared.m_description = "$" + descKey;
        }
      }

      if (!Originals.ContainsKey(pair.Key))
      {
        Originals[pair.Key] = new Snapshot
        {
          NameKey = nameKey,
          NameVanilla = loc.Localize("$" + nameKey),
          DescKey = descKey,
          DescVanilla = loc.Localize("$" + descKey)
        };
      }

      loc.AddWord(nameKey, pair.Value.Name);
      loc.AddWord(descKey, pair.Value.Description);
      applied++;
    }

    EvictLocalizeCache(loc);
    _applied = applied > 0;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug,
        $"Heimdiver item display names applied via Localization ({applied} prefabs).");
    }
  }

  private static void Restore()
  {
    if (!_applied && Originals.Count == 0)
    {
      return;
    }

    Localization? loc = Localization.instance;
    if (loc == null)
    {
      return;
    }

    foreach (KeyValuePair<string, Snapshot> pair in Originals)
    {
      if (pair.Value.NameKey.Length > 0)
      {
        loc.AddWord(pair.Value.NameKey, pair.Value.NameVanilla);
      }

      if (pair.Value.DescKey.Length > 0)
      {
        loc.AddWord(pair.Value.DescKey, pair.Value.DescVanilla);
      }
    }

    EvictLocalizeCache(loc);
    _applied = false;
    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, "Heimdiver item display names restored.");
    }
  }

  private static bool TryTokenKey(string? raw, out string key)
  {
    key = "";
    if (raw is not { Length: >= 2 } || raw[0] != '$')
    {
      return false;
    }

    key = raw.Substring(1);
    return key.Length > 0;
  }

  /// <summary>
  /// <see cref="Localization.Localize(string)"/> caches by input token; AddWord alone leaves stale English.
  /// </summary>
  private static void EvictLocalizeCache(Localization loc)
  {
    object? cache = AccessTools.Field(typeof(Localization), "m_cache")?.GetValue(loc);
    if (cache == null)
    {
      return;
    }

    AccessTools.Method(cache.GetType(), "EvictAll")?.Invoke(cache, null);
  }

  private static ItemDrop? FindItem(ObjectDB db, string prefabId)
  {
    foreach (GameObject go in db.m_items)
    {
      if (go == null || !string.Equals(go.name, prefabId, StringComparison.Ordinal))
      {
        continue;
      }

      return go.GetComponent<ItemDrop>();
    }

    return null;
  }
}
