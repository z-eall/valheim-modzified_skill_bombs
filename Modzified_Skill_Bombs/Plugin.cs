using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.Rendering;

namespace Modzified_Skill_Bombs;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class SkillBombsPlugin : BaseUnityPlugin
{
  internal const string ModName = "Modzified_Skill_Bombs";
  internal const string ModVersion = "0.3.0";
  /// <summary>Jere-style snake_case GUID. Thunderstore author when published: Zeall.</summary>
  internal const string ModGUID = "modzified_skill_bombs";

  internal static SkillBombsPlugin Instance { get; private set; } = null!;
  internal static ManualLogSource Log { get; private set; } = null!;
  internal static bool IsHeadless { get; private set; }

  internal static bool Allows(LogLevel level)
  {
    return Settings.LogLevels == null || (Settings.LogLevels.Value & level) != LogLevel.None;
  }

  internal static void LogAt(LogLevel level, string message)
  {
    if (!Allows(level))
    {
      return;
    }

    Log.Log(level, message);
  }

  private readonly Harmony _harmony = new(ModGUID);

  private void Awake()
  {
    Instance = this;
    Log = Logger;
    IsHeadless = UnityEngine.SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

    Settings.Init(Config);
    BombsSkill.Init();
    BombsSkill.RegisterTokens();
    _harmony.PatchAll(Assembly.GetExecutingAssembly());

    if (IsHeadless)
    {
      LogAt(LogLevel.Info, $"{ModName} v{ModVersion} loaded on dedicated/headless (GUID {ModGUID}).");
    }
    else
    {
      LogAt(LogLevel.Info, $"{ModName} v{ModVersion} loaded (GUID {ModGUID}).");
    }
  }

  private void Start()
  {
    LiveSkillCap.Probe();
    Coexistence.WarnOnce();
  }

  private void Update()
  {
    XpBlocklistStore.Tick();
  }

  private void OnDestroy()
  {
    _harmony.UnpatchSelf();
  }
}
