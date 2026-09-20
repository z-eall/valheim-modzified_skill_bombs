using System;
using System.Reflection;

namespace Modzified_Skill_Bombs;

/// <summary>Soft-read SkillLimitExtender <c>GetCap(Bombs)</c>. Missing or ≤ 0 → 100. Do not use GetSkillFactor.</summary>
internal static class LiveSkillCap
{
  internal const float Fallback = 100f;

  private static MethodInfo? _getCap;
  private static bool _probed;

  internal static void Probe()
  {
    if (_probed)
    {
      return;
    }

    _probed = true;
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      Type? type = assembly.GetType("SkillLimitExtender.SkillConfigManager");
      if (type == null)
      {
        continue;
      }

      Type[] skillTypeArg = { typeof(Skills.SkillType) };
      _getCap = type.GetMethod("GetCap", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, skillTypeArg, null)
                ?? type.GetMethod("GetSkillLimit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, skillTypeArg, null);
      break;
    }
  }

  internal static float Read()
  {
    Probe();
    if (_getCap == null || !BombsSkill.Ready)
    {
      return Fallback;
    }

    try
    {
      object raw = _getCap.Invoke(null, new object[] { BombsSkill.Type });
      float cap = Convert.ToSingle(raw);
      return cap > 0f ? cap : Fallback;
    }
    catch (Exception ex)
    {
      SkillBombsPlugin.LogAt(BepInEx.Logging.LogLevel.Warning, $"{SkillBombsPlugin.ModName}: live Bombs cap read failed ({ex.GetType().Name}); using {Fallback}.");
      return Fallback;
    }
  }
}
