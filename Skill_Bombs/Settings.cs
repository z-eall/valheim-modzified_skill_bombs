using BepInEx.Configuration;
using BepInEx.Logging;
using ServerSync;

namespace Skill_Bombs;

internal static class Settings
{
  internal const string SectionGeneral = "1. General";
  internal const string SectionCombat = "2. Damage and stamina";
  /// <summary>Temporary feel-tune only — remove after [Lock feel-good launch help] hardcodes the constant.</summary>
  internal const string SectionTemporary = "9. Temporary launch tune";
  /// <summary>Last numbered section; Log levels is advanced-only so the block stays out of the default CM view.</summary>
  internal const string SectionLogging = "10. Logging";
  internal const string DefaultPrefabList =
    "BombOoze, BombBile, BombSmoke, BombLava, BombDynamite, BombBlob_Poison, BombBlob_PoisonElite, BombBlob_Frost, BombBlob_Tar, BombBlob_Lava, BombBlob_Morkhalla";

  internal const string CurveLinear = "Linear";
  internal const string CurveQuickStart = "Quick start";
  internal const string CurveSlowStart = "Slow start";

  /// <summary>Hand lift meters — kept at 0; aim/loft carries launch help.</summary>
  internal const float BaseMaxHandLiftMeters = 0f;

  /// <summary>Fallback loft (°) if camera math fails. Negative pitches up.</summary>
  internal const float BaseMaxLoftDegrees = 8f;

  /// <summary>Along-ray fallback when the crosshair ray hits nothing.</summary>
  internal const float LaunchAimFallbackDistance = 20f;

  /// <summary>Full ballistic+geometric launch help through this distance; beyond, help scales down (~10/dist).</summary>
  internal const float LaunchHelpFullRangeMeters = 10f;

  /// <summary>Default vial gravity when the projectile prefab has no readable <c>m_gravity</c>.</summary>
  internal const float DefaultBombGravity = 10f;

  /// <summary>Same default as BepInEx.cfg <c>Logging.Disk</c> / <c>Logging.Console</c> (Debug unchecked).</summary>
  internal const LogLevel DefaultLogLevels =
    LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info;

  internal static ConfigSync Sync { get; private set; } = null!;

  internal static ConfigEntry<LogLevel> LogLevels { get; private set; } = null!;
  internal static ConfigEntry<int> SteadinessAtSkill0 { get; private set; } = null!;
  internal static ConfigEntry<int> SteadinessAtSkillMax { get; private set; } = null!;
  internal static ConfigEntry<string> HowSteadinessImproves { get; private set; } = null!;
  internal static ConfigEntry<string> BombPrefabs { get; private set; } = null!;
  /// <summary>Local-only temporary multiplier; change live (Configuration Manager / cfg reload). Dump after feel-good.</summary>
  internal static ConfigEntry<float> TempLaunchHelpStrength { get; private set; } = null!;

  internal static ConfigEntry<bool> ScaleThrowDamage { get; private set; } = null!;
  internal static ConfigEntry<bool> ScaleThrowStamina { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrow { get; private set; } = null!;
  internal static ConfigEntry<int> FreeThrowChanceAtSkillMax { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrowBonusText { get; private set; } = null!;
  internal static ConfigEntry<string> FreeThrowText { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrowBonusEffect { get; private set; } = null!;
  /// <summary>Host. BombSmoke flask→ground also trains Bombs (Heimdiver call-in). Default off.</summary>
  internal static ConfigEntry<bool> BombSmokeGroundXp { get; private set; } = null!;

  internal static void Init(ConfigFile config)
  {
    Sync = new ConfigSync(SkillBombsPlugin.ModGUID)
    {
      DisplayName = SkillBombsPlugin.ModName,
      CurrentVersion = SkillBombsPlugin.ModVersion,
      ModRequired = false,
      IsLocked = true
    };

    var percent = new AcceptableValueRange<int>(0, 100);
    SteadinessAtSkill0 = BindSynced(config, SectionGeneral, "Throw steadiness at skill 0", 0,
      new ConfigDescription("Untrained throw steadiness (percent). 0 = that bomb's vanilla spread.", percent,
        new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false }));
    SteadinessAtSkillMax = BindSynced(config, SectionGeneral, "Throw steadiness at skill max", 100,
      new ConfigDescription("Throw steadiness at this skill's live ceiling (percent). 100 = no spread.", percent,
        new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
    HowSteadinessImproves = BindSynced(config, SectionGeneral, "How steadiness improves", CurveLinear,
      new ConfigDescription("Linear. Quick start: more gain early. Slow start: more gain near max.",
        new AcceptableValueList<string>(CurveLinear, CurveQuickStart, CurveSlowStart),
        new ConfigurationManagerAttributes { Order = 2 }));
    BombPrefabs = BindSynced(config, SectionGeneral, "Bomb prefabs", DefaultPrefabList,
      new ConfigDescription("Comma-separated prefab ids, exact case. Must also use throw_bomb. Listed spears and staffs are ignored.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1 } }));

    ScaleThrowDamage = BindSynced(config, SectionCombat, "Scale throw damage", false,
      new ConfigDescription(
        "Off = today's flask/cloud/blob-star damage. On = staff-style flask grow, cloud floor-at-today (up to ~2.5×), blob star chance. Blob face-hits (5 blunt) scale too. Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 6 } }));
    ScaleThrowStamina = BindSynced(config, SectionCombat, "Scale throw stamina", false,
      new ConfigDescription(
        "Off = full throw stamina. On = up to 33% cheaper at Bombs skill max (vanilla skill discount). Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 5 } }));
    FreeThrow = BindSynced(config, SectionCombat, "Free throw", false,
      new ConfigDescription(
        "Off = always consume the bomb. On = chance to keep it after a throw. Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 4 } }));
    FreeThrowChanceAtSkillMax = BindSynced(config, SectionCombat, "Free throw chance at skill max", 25,
      new ConfigDescription(
        "Percent chance at the Bombs skill ceiling when Free throw is on. Actual chance = this × skill factor (0 at skill 0).",
        percent,
        new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
    FreeThrowBonusText = BindLocal(config, SectionCombat, "Free throw bonus text", true,
      new ConfigDescription(
        "Local. When a free throw procs, show DamageText (white Normal) using Free throw text.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 2 } }));
    FreeThrowText = BindSynced(config, SectionCombat, "Free throw text", "Freethrow!",
      new ConfigDescription(
        "Server-synced. Message shown on a free throw proc when Free throw bonus text is on. White DamageText (Normal).",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1 } }));
    FreeThrowBonusEffect = BindLocal(config, SectionCombat, "Free throw bonus effect", true,
      new ConfigDescription(
        "Local. When a free throw procs, play the vanilla craft bonus effect if available.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 0 } }));
    BombSmokeGroundXp = BindSynced(config, SectionCombat, "BombSmoke ground XP", false,
      new ConfigDescription(
        "Off = BombSmoke trains only when the flask hits a creature. On = also trains when the flask hits the ground — made for DhakhaR's Heimdiver server (Helldivers in Valheim). Check out their project. Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = -1 } }));

    TempLaunchHelpStrength = BindLocal(config, SectionTemporary, "Launch help strength", 1f,
      new ConfigDescription(
        "TEMPORARY — hand-edit any float (no cap). Multiplies ballistic launch aim (× Bombs steadiness). 0 = vanilla aim; 1 ≈ full gravity-aware aim at ≤10 m; try 1.5–3 if still low. Beyond 10 m help falls off. Local only.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, ShowRangeAsPercent = false } }));

    // Bound last + section 10 so CM keeps Logging at the end; IsAdvanced hides it until Advanced is ticked.
    LogLevels = BindLocal(config, SectionLogging, "Log levels", DefaultLogLevels,
      new ConfigDescription(
        "Same flags as BepInEx Logging.Disk / Logging.Console. Throw and XP traces are Debug; they only reach LogOutput.log when Debug is checked here and in BepInEx.cfg.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true } }));

    SteadinessAtSkill0.SettingChanged += (_, _) => WarnInvertedEnds();
    SteadinessAtSkillMax.SettingChanged += (_, _) => WarnInvertedEnds();
    BombPrefabs.SettingChanged += (_, _) => ThrowSteadiness.OnAllowlistChanged();
    HowSteadinessImproves.SettingChanged += (_, _) => ThrowSteadiness.OnCurveChanged();

    config.SettingChanged += OnSettingChanged;
    config.ConfigReloaded += (_, _) => SkillBombsPlugin.LogAt(LogLevel.Info, "Config reloaded.");

    LogLoaded(LogLevels);
    LogLoaded(SteadinessAtSkill0);
    LogLoaded(SteadinessAtSkillMax);
    LogLoaded(HowSteadinessImproves);
    LogLoaded(BombPrefabs);
    LogLoaded(ScaleThrowDamage);
    LogLoaded(ScaleThrowStamina);
    LogLoaded(FreeThrow);
    LogLoaded(FreeThrowChanceAtSkillMax);
    LogLoaded(FreeThrowBonusText);
    LogLoaded(FreeThrowText);
    LogLoaded(FreeThrowBonusEffect);
    LogLoaded(BombSmokeGroundXp);
    LogLoaded(TempLaunchHelpStrength);
    WarnInvertedEnds();
    ThrowSteadiness.OnAllowlistChanged();
  }

  private static void OnSettingChanged(object sender, SettingChangedEventArgs args)
  {
    ConfigEntryBase? entry = args.ChangedSetting;
    if (entry == null)
    {
      return;
    }

    SkillBombsPlugin.LogAt(LogLevel.Info, $"{entry.Definition.Key} set to {entry.BoxedValue}.");
  }

  private static void LogLoaded(ConfigEntryBase entry)
  {
    SkillBombsPlugin.LogAt(LogLevel.Info, $"{entry.Definition.Key}: {entry.BoxedValue}.");
  }

  internal static void WarnInvertedEnds()
  {
    if (SteadinessAtSkill0.Value > SteadinessAtSkillMax.Value)
    {
      SkillBombsPlugin.LogAt(LogLevel.Warning,
        $"{SkillBombsPlugin.ModName}: Throw steadiness at skill 0 ({SteadinessAtSkill0.Value}) is higher than at skill max ({SteadinessAtSkillMax.Value}). Allowed; throws get wilder as Bombs rises.");
    }
  }

  private static ConfigEntry<T> BindSynced<T>(ConfigFile config, string section, string key, T value, ConfigDescription description)
  {
    ConfigEntry<T> entry = config.Bind(section, key, value, description);
    Sync.AddConfigEntry(entry).SynchronizedConfig = true;
    return entry;
  }

  private static ConfigEntry<T> BindLocal<T>(ConfigFile config, string section, string key, T value, ConfigDescription description)
  {
    ConfigEntry<T> entry = config.Bind(section, key, value, description);
    Sync.AddConfigEntry(entry).SynchronizedConfig = false;
    return entry;
  }
}

/// <summary>Dummy type Configuration Manager looks for on <see cref="ConfigDescription.Tags"/>.</summary>
internal sealed class ConfigurationManagerAttributes
{
  public int? Order;
  public bool? ShowRangeAsPercent;
  public bool? IsAdvanced;
}
