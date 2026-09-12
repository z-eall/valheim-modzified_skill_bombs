using System.Collections.Generic;
using BepInEx.Bootstrap;

namespace Skill_Bombs;

internal static class Coexistence
{
  private static bool _logged;

  internal static void WarnOnce()
  {
    if (_logged)
    {
      return;
    }

    _logged = true;
    var names = new List<string>();
    if (PluginLoaded("ProjectileTweaks"))
    {
      names.Add("ProjectileTweaks");
    }

    if (PluginLoaded("MaxAxe"))
    {
      names.Add("MaxAxe");
    }

    if (names.Count == 0)
    {
      return;
    }

    SkillBombsPlugin.LogAt(BepInEx.Logging.LogLevel.Warning,
      $"{SkillBombsPlugin.ModName}: also loaded {string.Join(", ", names)}. Throw may not match throw steadiness alone.");
  }

  private static bool PluginLoaded(string token)
  {
    foreach (var pair in Chainloader.PluginInfos)
    {
      BepInEx.PluginInfo info = pair.Value;
      if (info?.Metadata == null)
      {
        continue;
      }

      if (ContainsToken(pair.Key, token) || ContainsToken(info.Metadata.GUID, token) || ContainsToken(info.Metadata.Name, token))
      {
        return true;
      }
    }

    return false;
  }

  private static bool ContainsToken(string? haystack, string token)
  {
    return !string.IsNullOrEmpty(haystack)
           && haystack!.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
  }
}
