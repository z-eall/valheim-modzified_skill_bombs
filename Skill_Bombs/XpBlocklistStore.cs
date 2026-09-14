using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using ServerSync;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Skill_Bombs;

/// <summary>
/// Host file → CustomSyncedValue&lt;string&gt; → compile. Clients parse synced text only.
/// </summary>
internal static class XpBlocklistStore
{
  internal const string RelativePath = "skill_bombs/skill_bombs_xp_blocklist.yaml";
  private const string SyncId = "XpGainBlocklistYaml";

  private static CustomSyncedValue<string> _synced = null!;
  private static FileSystemWatcher? _watcher;
  private static bool _reloadPending;
  private static readonly object WatchLock = new();

  private static List<XpBlocklistMatcher.CompiledRule> _compiled = new();
  private static string _statusLine = "not loaded";
  private static string _lastError = "";

  internal static IReadOnlyList<XpBlocklistMatcher.CompiledRule> Compiled => _compiled;
  internal static string StatusLine => _statusLine;
  internal static string LastError => _lastError;
  internal static string AbsolutePath => Path.Combine(Paths.ConfigPath, RelativePath.Replace('/', Path.DirectorySeparatorChar));

  internal static void Init(ConfigSync sync)
  {
    _synced = new CustomSyncedValue<string>(sync, SyncId, "");
    _synced.ValueChanged += OnSyncedValueChanged;

    void BecomeHost(bool truth)
    {
      if (!truth)
      {
        return;
      }

      EnsureStubAndLoadHost();
      StartWatcher();
    }

    if (sync.IsSourceOfTruth)
    {
      BecomeHost(true);
    }

    sync.SourceOfTruthChanged += BecomeHost;
  }

  /// <summary>Call from plugin Update — applies host file reloads on the main thread.</summary>
  internal static void Tick()
  {
    bool reload;
    lock (WatchLock)
    {
      reload = _reloadPending;
      _reloadPending = false;
    }

    if (reload && Settings.Sync != null && Settings.Sync.IsSourceOfTruth)
    {
      LoadHostFileIntoSync();
    }
  }

  internal static void ReloadFromDiskIfHost()
  {
    if (Settings.Sync != null && Settings.Sync.IsSourceOfTruth)
    {
      LoadHostFileIntoSync();
    }
  }

  private static void OnSyncedValueChanged()
  {
    CompileFromText(_synced.Value ?? "");
  }

  private static void EnsureStubAndLoadHost()
  {
    string path = AbsolutePath;
    string? dir = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
      Directory.CreateDirectory(dir);
    }

    if (!File.Exists(path))
    {
      File.WriteAllText(path, StubTutorial, Encoding.UTF8);
      SkillBombsPlugin.LogAt(LogLevel.Info, $"XP blocklist: created stub at {path}");
    }

    LoadHostFileIntoSync();
  }

  private static void LoadHostFileIntoSync()
  {
    string path = AbsolutePath;
    try
    {
      string text = File.Exists(path) ? File.ReadAllText(path) : "";
      _synced.AssignLocalValue(text);
      // ValueChanged may not fire if text equals previous; always compile.
      CompileFromText(text);
    }
    catch (Exception ex)
    {
      _lastError = ex.Message;
      _statusLine = "read failed";
      SkillBombsPlugin.LogAt(LogLevel.Error, $"XP blocklist: failed to read {path}: {ex.Message}");
    }
  }

  private static void StartWatcher()
  {
    string path = AbsolutePath;
    string? dir = Path.GetDirectoryName(path);
    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
    {
      return;
    }

    _watcher?.Dispose();
    _watcher = new FileSystemWatcher(dir, Path.GetFileName(path))
    {
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
    };
    _watcher.Changed += (_, _) => QueueReload();
    _watcher.Created += (_, _) => QueueReload();
    _watcher.Renamed += (_, _) => QueueReload();
    _watcher.EnableRaisingEvents = true;
  }

  private static void QueueReload()
  {
    lock (WatchLock)
    {
      _reloadPending = true;
    }
  }

  private static void CompileFromText(string text)
  {
    _lastError = "";
    List<XpBlocklistMatcher.CompiledRule> next = new();

    if (IsEffectivelyEmpty(text))
    {
      _compiled = next;
      _statusLine = "0 rules (empty / comments only)";
      SkillBombsPlugin.LogAt(LogLevel.Info, "XP blocklist: loaded 0 rules.");
      return;
    }

    List<XpBlocklistRuleData>? raw;
    try
    {
      IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
      raw = deserializer.Deserialize<List<XpBlocklistRuleData>>(text);
    }
    catch (Exception ex)
    {
      _compiled = next;
      _lastError = ex.Message;
      _statusLine = "parse error";
      SkillBombsPlugin.LogAt(LogLevel.Error, $"XP blocklist: YAML parse failed: {ex.Message}");
      return;
    }

    if (raw == null || raw.Count == 0)
    {
      _compiled = next;
      _statusLine = "0 rules";
      SkillBombsPlugin.LogAt(LogLevel.Info, "XP blocklist: loaded 0 rules.");
      return;
    }

    int skipped = 0;
    HashSet<string> seenNames = new(StringComparer.Ordinal);
    foreach (XpBlocklistRuleData data in raw)
    {
      if (!XpBlocklistMatcher.TryCompile(data, out XpBlocklistMatcher.CompiledRule rule, out string error))
      {
        skipped++;
        SkillBombsPlugin.LogAt(LogLevel.Warning, $"XP blocklist: skipped rule ({error}).");
        continue;
      }

      if (!seenNames.Add(rule.Name))
      {
        skipped++;
        SkillBombsPlugin.LogAt(LogLevel.Warning,
          $"XP blocklist: duplicate name '{rule.Name}' — skipped (first rule with this name is kept).");
        continue;
      }

      next.Add(rule);
    }

    _compiled = next;
    _statusLine = skipped > 0
      ? $"{next.Count} rules loaded ({skipped} skipped)"
      : $"{next.Count} rules loaded";
    SkillBombsPlugin.LogAt(LogLevel.Info, $"XP blocklist: {_statusLine}.");
  }

  private static bool IsEffectivelyEmpty(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return true;
    }

    using StringReader reader = new(text);
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
      string t = line.Trim();
      if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal))
      {
        continue;
      }

      return false;
    }

    return true;
  }

  private const string StubTutorial =
    "# Skill_Bombs — XP Gain Blocklist\n" +
    "# Field list: docs/xp_gain_blocklist.md (https://github.com/z-eall/valheim-skill_bombs/blob/main/docs/xp_gain_blocklist.md)\n" +
    "#\n" +
    "# How matching works:\n" +
    "# - List of rules. Each needs a unique name. (Empty / comments only = nothing blocked.)\n" +
    "# - Inside one rule, ALL listed checks must be true together.\n" +
    "# - Among multiple rules, only ONE of listed rule blocks needs to be true.\n" +
    "# - Save the file; the host picks up changes shortly. If nothing changes, check BepInEx log.\n" +
    "#\n" +
    "# - name: MeadowsNoXp\n" +
    "#   biomes:\n" +
    "#   - Meadows\n" +
    "#\n" +
    "# - name: NearWorkbench\n" +
    "#   objects:\n" +
    "#   - prefab: piece_workbench\n" +
    "#     maxDistance: 32\n" +
    "#\n" +
    "# - name: HighRingStar3Greyling\n" +
    "#   position: 320,56\n" +
    "#   maxDistance: 50\n" +
    "#   minY: 3000\n" +
    "#   maxY: 20000\n" +
    "#   objects:\n" +
    "#   - prefab: Greyling\n" +
    "#     maxDistance: 15\n" +
    "#     filter: int, level, 3\n" +
    "#   - prefab: Boar\n" +
    "#     maxDistance: 15\n" +
    "#     filter: int, level, 2\n" +
    "#   objectsLimit: 1\n";
}
