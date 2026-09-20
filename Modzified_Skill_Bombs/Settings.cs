using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using ServerSync;
using UnityEngine;

namespace Modzified_Skill_Bombs;

internal static class Settings
{
  internal const string SectionGeneral = "1. General";
  internal const string SectionCombat = "2. Damage and stamina";
  internal const string SectionHeimdiver = "3. Heimdiver Science";
  internal const string SectionXpBlocklist = "4. XP Gain Blocklist";
  /// <summary>Last numbered section; Log levels is advanced-only so the block stays out of the default CM view.</summary>
  internal const string SectionLogging = "9. Logging";
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

  /// <summary>Ignore camera hits closer than this; aim at this distance along the ray instead (wall-face / inside-collider guard).</summary>
  internal const float LaunchAimMinDistanceMeters = 4f;

  /// <summary>Full ballistic+geometric launch help through this distance; beyond, help scales down (~10/dist).</summary>
  internal const float LaunchHelpFullRangeMeters = 10f;

  /// <summary>Spawn→aim distance below which launch help is fully faded (near wall / boulder).</summary>
  internal const float LaunchHelpNearFadeStartMeters = 3f;

  /// <summary>Spawn→aim distance at which near-fade reaches full help (then far fade still applies past full-range).</summary>
  internal const float LaunchHelpNearFadeEndMeters = 6f;

  /// <summary>Soft cap on applied launch-angle delta (degrees) after help × range factors.</summary>
  internal const float LaunchHelpMaxAbsDegrees = 15f;

  /// <summary>
  /// Locked launch-help strength (was temporary CM "Launch help strength").
  /// Feel-good = 1.0 after close-range guard (bombTest9 / map ticket 12).
  /// </summary>
  internal const float LaunchHelpStrength = 1f;

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

  internal static ConfigEntry<bool> ScaleThrowDamage { get; private set; } = null!;
  internal static ConfigEntry<bool> ScaleThrowStamina { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrow { get; private set; } = null!;
  internal static ConfigEntry<int> FreeThrowChanceAtSkillMax { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrowBonusText { get; private set; } = null!;
  internal static ConfigEntry<string> FreeThrowText { get; private set; } = null!;
  internal static ConfigEntry<bool> FreeThrowBonusEffect { get; private set; } = null!;
  /// <summary>Host. Master gate for hardcoded HD bombs (strip / throw-counter / Dynamite / Beacon).</summary>
  internal static ConfigEntry<bool> HeimdiverBombOverride { get; private set; } = null!;
  /// <summary>Host. Relative how-far for BombDynamite under override (1 = vanilla). Not meters.</summary>
  internal static ConfigEntry<float> DynamiteLandDistance { get; private set; } = null!;
  /// <summary>Host. BombSmoke throw stamina reduction % under override (100 = free).</summary>
  internal static ConfigEntry<int> BeaconDeploymentEfficiency { get; private set; } = null!;
  /// <summary>Local. CM-only briefing row (CustomDrawer); not a real setting.</summary>
  internal static ConfigEntry<bool> HeimdiverBriefing { get; private set; } = null!;
  /// <summary>Local. CM-only status row for the YAML blocklist file (CustomDrawer).</summary>
  internal static ConfigEntry<bool> XpBlocklistStatus { get; private set; } = null!;

  /// <summary>Vanilla BombDynamite <c>Attack.m_projectileVel</c> — land distance 1 maps here.</summary>
  internal const float VanillaDynamiteProjectileVel = 2f;

  /// <summary>CM slider span for <see cref="DynamiteLandDistance"/> (relative scale, not meters).</summary>
  internal static readonly AcceptableValueRange<float> DynamiteLandDistanceRange = new(0.25f, 50f);

  internal const string HeimdiverBriefingText =
    "HEIMDIVERS is a re-creation of Helldivers II within Valheim, built with vanilla assets using the Expand World Mods.\n" +
    "Defend Valheim Super Earth against the ever-present threat of the Terminids and the Automatons.\n\n" +
    "This section adds Heimdiver Augmentation for Heimdiver bombs.\n" +
    "Use the master toggle below — leave off for normal Modzified_Skill_Bombs; fine on other worlds if it fits.";

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
      new ConfigDescription(
        "How steady throws are at Bombs skill 0.\n" +
        "0 keeps that bomb's normal vanilla miss.\n" +
        "100 removes random miss and uses full aim help.",
        percent,
        new ConfigurationManagerAttributes { Order = 4, ShowRangeAsPercent = false }));
    SteadinessAtSkillMax = BindSynced(config, SectionGeneral, "Throw steadiness at skill max", 100,
      new ConfigDescription(
        "How steady throws are at the Bombs skill ceiling.\n" +
        "0 keeps that bomb's normal vanilla miss.\n" +
        "100 removes random miss and uses full aim help.",
        percent,
        new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
    HowSteadinessImproves = BindSynced(config, SectionGeneral, "How steadiness improves", CurveLinear,
      new ConfigDescription(
        "How steadiness grows between skill 0 and skill max.\n" +
        "Linear: even gain as skill rises.\n" +
        "Quick start: more gain early.\n" +
        "Slow start: more gain near max.",
        new AcceptableValueList<string>(CurveLinear, CurveQuickStart, CurveSlowStart),
        new ConfigurationManagerAttributes { Order = 2 }));
    BombPrefabs = BindSynced(config, SectionGeneral, "Bomb prefabs", DefaultPrefabList,
      new ConfigDescription(
        "Which items use the Bombs skill.\n" +
        "Use comma-separated prefab ids with exact spelling and case.\n" +
        "The item must use the normal bomb throw.\n" +
        "Spears and staffs on this list are ignored.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1 } }));

    ScaleThrowDamage = BindSynced(config, SectionCombat, "Scale throw damage", false,
      new ConfigDescription(
        "When on, Bombs skill can raise throw damage.\n" +
        "Flask hits and clouds grow with skill.\n" +
        "Thrown blobs can roll extra stars.\n" +
        "Lava and dynamite flask damage stay 0.\n" +
        "Smoke clouds are not grown.\n" +
        "Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 6 } }));
    ScaleThrowStamina = BindSynced(config, SectionCombat, "Scale throw stamina", false,
      new ConfigDescription(
        "When on, throws cost less stamina as Bombs skill rises.\n" +
        "At skill max, cost can drop by about one third.\n" +
        "When off, throw stamina stays full.\n" +
        "Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 5 } }));
    FreeThrow = BindSynced(config, SectionCombat, "Free throw", false,
      new ConfigDescription(
        "When on, a throw may keep the bomb instead of using it up.\n" +
        "When off, every throw always consumes the bomb.\n" +
        "Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 4 } }));
    FreeThrowChanceAtSkillMax = BindSynced(config, SectionCombat, "Free throw chance at skill max", 25,
      new ConfigDescription(
        "Chance to keep the bomb at the Bombs skill ceiling, when Free throw is on.\n" +
        "Real chance = this value × your skill factor (0 at skill 0).",
        percent,
        new ConfigurationManagerAttributes { Order = 3, ShowRangeAsPercent = false }));
    FreeThrowBonusText = BindLocal(config, SectionCombat, "Free throw bonus text", true,
      new ConfigDescription(
        "Local only.\n" +
        "When a free throw procs, show the Free throw text on screen.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 2 } }));
    FreeThrowText = BindSynced(config, SectionCombat, "Free throw text", "Freethrow!",
      new ConfigDescription(
        "Message shown when a free throw procs and Free throw bonus text is on.\n" +
        "Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1 } }));
    FreeThrowBonusEffect = BindLocal(config, SectionCombat, "Free throw bonus effect", true,
      new ConfigDescription(
        "Local only.\n" +
        "When a free throw procs, play the usual craft bonus effect if the game has it.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 0 } }));

    HeimdiverBriefing = BindLocal(config, SectionHeimdiver, "Heimdiver Science briefing", false,
      new ConfigDescription(
        HeimdiverBriefingText,
        tags: new object[]
        {
          new ConfigurationManagerAttributes
          {
            Order = 12,
            HideDefaultButton = true,
            HideSettingName = true,
            CustomDrawer = DrawHeimdiverBriefing
          }
        }));
    HeimdiverBombOverride = BindSynced(config, SectionHeimdiver, "Heimdiver Explosive Augmentation", false,
      new ConfigDescription(
        "Master switch for Heimdiver bomb rules.\n" +
        "When on, built-in Augmentation and the options below apply to Heimdiver bombs.\n" +
        "When off, that Augmentation is removed — normal Modzified_Skill_Bombs.\n" +
        "Server-synced.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 10 } }));
    DynamiteLandDistance = BindSynced(config, SectionHeimdiver, "Dynamite Trajectory Calibration", 1f,
      new ConfigDescription(
        "How far BombDynamite travels under Augmentation.\n" +
        "1 matches the vanilla short throw. Raise it to send the bomb farther away.\n" +
        "Relative scale — not meters on the ground.\n" +
        "Only BombDynamite. Ignored when Augmentation is off.",
        DynamiteLandDistanceRange,
        new ConfigurationManagerAttributes { Order = 9, ShowRangeAsPercent = false }));
    BeaconDeploymentEfficiency = BindSynced(config, SectionHeimdiver, "Beacon Deployment Efficiency", 100,
      new ConfigDescription(
        "How much throw stamina BombSmoke saves under Augmentation.\n" +
        "0 = full cost. 100 = free.\n" +
        "Only BombSmoke. Ignored when Augmentation is off.\n" +
        "Server-synced.",
        new AcceptableValueRange<int>(0, 100),
        new ConfigurationManagerAttributes { Order = 8, ShowRangeAsPercent = false }));

    XpBlocklistStatus = BindLocal(config, SectionXpBlocklist, "XP Gain Blocklist file", false,
      new ConfigDescription(
        "Host file for rules that stop Bombs XP. Edit the YAML on disk — not this cfg.\n" +
        "File: BepInEx/config/modzified_skill/bombs/modzified_skill_bombs_xp_blocklist.yaml\n" +
        "Empty or comments only = no XP blocked. Server-synced.",
        tags: new object[]
        {
          new ConfigurationManagerAttributes
          {
            Order = 1,
            HideDefaultButton = true,
            HideSettingName = true,
            CustomDrawer = DrawXpBlocklistStatus
          }
        }));

    XpBlocklistStore.Init(Sync);

    // Bound last so CM keeps Logging at the end; IsAdvanced hides it until Advanced is ticked.
    LogLevels = BindLocal(config, SectionLogging, "Log levels", DefaultLogLevels,
      new ConfigDescription(
        "Same flags as BepInEx Logging.Disk / Logging.Console. Throw and XP traces are Debug; they only reach LogOutput.log when Debug is checked here and in BepInEx.cfg.",
        tags: new object[] { new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true } }));

    SteadinessAtSkill0.SettingChanged += (_, _) => WarnInvertedEnds();
    SteadinessAtSkillMax.SettingChanged += (_, _) => WarnInvertedEnds();
    BombPrefabs.SettingChanged += (_, _) => ThrowSteadiness.OnAllowlistChanged();
    HowSteadinessImproves.SettingChanged += (_, _) => ThrowSteadiness.OnCurveChanged();

    HeimdiverBombOverride.SettingChanged += (_, _) => HeimdiverItemDisplay.Refresh();

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
    LogLoaded(HeimdiverBombOverride);
    LogLoaded(DynamiteLandDistance);
    LogLoaded(BeaconDeploymentEfficiency);
    WarnInvertedEnds();
    ThrowSteadiness.OnAllowlistChanged();
  }

  private static void DrawHeimdiverBriefing(ConfigEntryBase _)
  {
    GUILayout.BeginVertical(GUI.skin.box);
    GUILayout.Label(HeimdiverBriefingText, new GUIStyle(GUI.skin.label) { wordWrap = true });
    GUILayout.EndVertical();
  }

  private static void DrawXpBlocklistStatus(ConfigEntryBase _)
  {
    GUILayout.BeginVertical(GUI.skin.box);
    GUILayout.Label(
      "Rules that stop Bombs XP live in a file on the host (not this cfg).\n" +
      "File: BepInEx/config/modzified_skill/bombs/modzified_skill_bombs_xp_blocklist.yaml\n" +
      "Leave the file empty (or comments only) = no XP blocked. Server-synced.",
      new GUIStyle(GUI.skin.label) { wordWrap = true });
    GUILayout.EndVertical();
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

/// <summary>Type Configuration Manager looks for on <see cref="ConfigDescription.Tags"/> (reflection).</summary>
internal sealed class ConfigurationManagerAttributes
{
  public int? Order;
  public bool? ShowRangeAsPercent;
  public bool? IsAdvanced;
  public bool? HideDefaultButton;
  public bool? HideSettingName;
  public Action<ConfigEntryBase>? CustomDrawer;
}
