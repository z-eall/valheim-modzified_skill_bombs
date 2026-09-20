using BepInEx.Logging;
using UnityEngine;

namespace Modzified_Skill_Bombs;

/// <summary>Vanilla-tab skill. Save id is the hashed GUID <c>modzified_skill_bombs</c>, not the English label Bombs.</summary>
internal static class BombsSkill
{
  internal const string TabName = "Bombs";
  internal const string SaveId = SkillBombsPlugin.ModGUID;
  /// <summary>Vanilla item whose <see cref="ItemDrop.ItemData.SharedData.m_icons"/> feeds <see cref="Skills.SkillDef.m_icon"/>.</summary>
  internal const string IconItemPrefab = "BombSmoke";

  internal static Skills.SkillType Type { get; private set; }
  internal static Skills.SkillDef Def { get; private set; } = null!;
  internal static bool Ready { get; private set; }

  private static bool _loggedIcon;
  private static bool _warnedMissingIcon;

  internal static void Init()
  {
    int hash = Mathf.Abs(SaveId.GetStableHashCode());
    if (hash <= (int)Skills.SkillType.All || hash == 878)
    {
      SkillBombsPlugin.LogAt(LogLevel.Error,
        $"{SkillBombsPlugin.ModName}: skill hash {hash} is not usable (must be > 999, not 878). Save id '{SaveId}' is not usable.");
      Ready = false;
      return;
    }

    Type = (Skills.SkillType)hash;
    Def = new Skills.SkillDef
    {
      m_skill = Type,
      m_icon = null,
      m_description = "Thrown bombs land closer to the cursor as this skill rises.",
      m_increseStep = 1f
    };
    Ready = true;
    SkillBombsPlugin.LogAt(LogLevel.Info, $"{SkillBombsPlugin.ModName}: Bombs skill id {hash} (save '{SaveId}').");
    TryResolveIcon();
  }

  /// <summary>
  /// Same shape as vanilla: <see cref="Skills.SkillDef.m_icon"/> is a game <see cref="Sprite"/>.
  /// Source is BombSmoke's item icons (ObjectDB / ZNetScene), not a baked placeholder texture.
  /// </summary>
  internal static void TryResolveIcon()
  {
    if (!Ready || SkillBombsPlugin.IsHeadless || Def.m_icon != null)
    {
      return;
    }

    GameObject? prefab = FindIconPrefab();
    if (prefab == null)
    {
      return;
    }

    ItemDrop drop = prefab.GetComponent<ItemDrop>();
    Sprite[]? icons = drop != null ? drop.m_itemData?.m_shared?.m_icons : null;
    if (icons == null || icons.Length == 0 || icons[0] == null)
    {
      if (_warnedMissingIcon)
      {
        return;
      }

      _warnedMissingIcon = true;
      SkillBombsPlugin.LogAt(LogLevel.Warning,
        $"{SkillBombsPlugin.ModName}: '{IconItemPrefab}' has no item icon — Bombs tab stays without a sprite.");
      return;
    }

    Def.m_icon = icons[0];
    if (_loggedIcon)
    {
      return;
    }

    _loggedIcon = true;
    SkillBombsPlugin.LogAt(LogLevel.Info,
      $"{SkillBombsPlugin.ModName}: Bombs skill icon from '{IconItemPrefab}'.");
  }

  internal static void RegisterTokens()
  {
    if (!Ready || Localization.instance == null)
    {
      return;
    }

    string key = "skill_" + Type.ToString().ToLowerInvariant();
    Localization.instance.AddWord(key, TabName);
  }

  internal static void EnsureOn(Skills? skills)
  {
    if (!Ready || skills == null)
    {
      return;
    }

    TryResolveIcon();
    skills.GetSkill(Type);
  }

  internal static void EnsureDefOn(Skills skills)
  {
    if (!Ready || skills.m_skills == null)
    {
      return;
    }

    TryResolveIcon();
    foreach (Skills.SkillDef def in skills.m_skills)
    {
      if (def != null && def.m_skill == Type)
      {
        if (def.m_icon == null && Def.m_icon != null)
        {
          def.m_icon = Def.m_icon;
        }

        return;
      }
    }

    skills.m_skills.Add(Def);
  }

  internal static bool IsTabName(string? name)
  {
    if (string.IsNullOrEmpty(name))
    {
      return false;
    }

    return name!.Equals(TabName, System.StringComparison.OrdinalIgnoreCase);
  }

  internal static void CheatRaise(Skills skills, float value, bool showMessage)
  {
    Skills.Skill skill = skills.GetSkill(Type);
    skill.m_level += value;
    skill.m_level = Mathf.Clamp(skill.m_level, 0f, LiveSkillCap.Read());
    if (skills.m_useSkillCap)
    {
      skills.RebalanceSkills(Type);
    }

    // Do not call Character.Message / MessageHud.ShowMessage — Gale 1.0.12 and Steam
    // publicized refs disagree on optional-arg overloads; MissingMethodException aborts JIT
    // of this whole method so the level never rises.
    if (showMessage && Console.instance != null)
    {
      Console.instance.Print($"Skill increased {TabName}: {(int)skill.m_level}");
    }

    int steadiness = Mathf.RoundToInt(ThrowSteadiness.CurrentSteadiness() * 100f);
    SkillBombsPlugin.LogAt(LogLevel.Info, $"Bombs = {skill.m_level}, throw steadiness {steadiness}%");
  }

  internal static void CheatReset(Skills skills, bool showMessage)
  {
    skills.ResetSkill(Type);
    if (showMessage)
    {
      Console.instance.Print($"Skill {TabName} reset");
    }
  }

  private static GameObject? FindIconPrefab()
  {
    if (ObjectDB.instance != null)
    {
      GameObject item = ObjectDB.instance.GetItemPrefab(IconItemPrefab);
      if (item != null)
      {
        return item;
      }
    }

    if (ZNetScene.instance != null)
    {
      return ZNetScene.instance.GetPrefab(IconItemPrefab);
    }

    return null;
  }
}
