using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>Evaluates locked XP Gain Blocklist rules against the thrower at XP grant.</summary>
internal static class XpBlocklistMatcher
{
  private const float DefaultObjectMaxDistance = 100f;

  private static MethodInfo? _ewdTryGetBiome;
  private static bool _ewdResolved;

  internal static bool IsDenied(Player? thrower, out string matchedRuleName)
  {
    matchedRuleName = "";
    IReadOnlyList<CompiledRule> rules = XpBlocklistStore.Compiled;
    if (thrower == null || rules.Count == 0)
    {
      return false;
    }

    Vector3 pos = thrower.transform.position;
    foreach (CompiledRule rule in rules)
    {
      if (RuleMatches(rule, pos))
      {
        matchedRuleName = rule.Name;
        return true;
      }
    }

    return false;
  }

  private static bool RuleMatches(CompiledRule rule, Vector3 throwerPos)
  {
    if (!rule.HasAnyClause)
    {
      return false;
    }

    if (rule.Biomes != null && rule.Biomes.Count > 0)
    {
      Heightmap.Biome biome = ResolveBiome(throwerPos);
      if (!rule.Biomes.Contains(biome))
      {
        return false;
      }
    }

    if (rule.HasAbsoluteY)
    {
      float y = throwerPos.y;
      if (rule.MinY.HasValue && y < rule.MinY.Value)
      {
        return false;
      }

      if (rule.MaxY.HasValue && y > rule.MaxY.Value)
      {
        return false;
      }
    }

    if (rule.HasPosition)
    {
      Vector3 anchor = new(rule.PosX, throwerPos.y, rule.PosZ);
      float d = Utils.DistanceXZ(throwerPos, anchor);
      float minD = rule.PosMinDistance ?? 0f;
      float maxD = rule.PosMaxDistance!.Value;
      if (d < minD || d > maxD)
      {
        return false;
      }
    }

    if (rule.Objects.Count > 0 && !ObjectsNearby(rule.ObjectsLimit, rule.Objects, throwerPos))
    {
      return false;
    }

    if (rule.BannedObjects.Count > 0 && ObjectsNearby(rule.BannedObjectsLimit, rule.BannedObjects, throwerPos))
    {
      return false;
    }

    return true;
  }

  /// <summary>EWP HasNearby: null limit → all patterns; set limit → weight sum vs Range (Max=0 ⇒ no upper).</summary>
  private static bool ObjectsNearby(IntRange? limit, List<CompiledObject> patterns, Vector3 throwerPos)
  {
    if (patterns.Count == 0)
    {
      return true;
    }

    float scanR = patterns.Max(p => p.MaxDistance);
    List<ZDO> zdos = CollectNearbyZdos(throwerPos, scanR);

    if (limit == null)
    {
      return patterns.All(p => zdos.Any(z => ObjectValid(p, z, throwerPos)));
    }

    int counter = 0;
    bool useMax = limit.Value.Max > 0;
    foreach (ZDO z in zdos)
    {
      CompiledObject? valid = patterns.FirstOrDefault(p => ObjectValid(p, z, throwerPos));
      if (valid == null)
      {
        continue;
      }

      counter += valid.Weight;
      if (useMax && limit.Value.Max < counter)
      {
        return false;
      }

      if (limit.Value.Min <= counter && !useMax)
      {
        return true;
      }
    }

    return limit.Value.Min <= counter && counter <= limit.Value.Max;
  }

  private static bool ObjectValid(CompiledObject pattern, ZDO zdo, Vector3 throwerPos)
  {
    if (pattern.PrefabHash != 0 && zdo.GetPrefab() != pattern.PrefabHash)
    {
      return false;
    }

    float d = Utils.DistanceXZ(throwerPos, zdo.GetPosition());
    if (pattern.MinDistance.HasValue && d < pattern.MinDistance.Value)
    {
      return false;
    }

    if (d > pattern.MaxDistance)
    {
      return false;
    }

    float dy = Mathf.Abs(throwerPos.y - zdo.GetPosition().y);
    if (pattern.MinHeight.HasValue && dy < pattern.MinHeight.Value)
    {
      return false;
    }

    if (pattern.MaxHeight.HasValue && dy > pattern.MaxHeight.Value)
    {
      return false;
    }

    if (pattern.Filters.Count == 0)
    {
      return true;
    }

    return FiltersMatch(pattern, zdo);
  }

  /// <summary>EWP Filters.Match: default limit = positive filter count; banned subtract weight (default 10000).</summary>
  private static bool FiltersMatch(CompiledObject pattern, ZDO zdo)
  {
    float limit = pattern.FilterLimit ?? pattern.PositiveFilterCount;
    float total = 0f;
    foreach (CompiledFilter f in pattern.Filters)
    {
      if (!FilterHits(f, zdo))
      {
        continue;
      }

      total += f.Banned ? -f.Weight : f.Weight;
    }

    return limit <= total || Mathf.Approximately(total, limit);
  }

  private static bool FilterHits(CompiledFilter f, ZDO zdo)
  {
    switch (f.Type)
    {
      case FilterType.Int:
        return zdo.GetInt(f.KeyHash, 0) == f.IntValue;
      case FilterType.Bool:
        return zdo.GetBool(f.KeyHash, false) == f.BoolValue;
      case FilterType.Float:
        return Mathf.Approximately(zdo.GetFloat(f.KeyHash, 0f), f.FloatValue);
      case FilterType.String:
        return string.Equals(zdo.GetString(f.KeyHash, ""), f.StringValue, StringComparison.Ordinal);
      default:
        return false;
    }
  }

  private static List<ZDO> CollectNearbyZdos(Vector3 center, float maxDistance)
  {
    List<ZDO> result = new();
    ZDOMan? man = ZDOMan.instance;
    if (man == null)
    {
      return result;
    }

    // Invent: rare XP path — scan publicized m_objectsByID with XZ gate.
    // Mentor EWP uses sector rings (cheaper at large R); flagged in implement note.
    float maxSqr = maxDistance * maxDistance;
    foreach (KeyValuePair<ZDOID, ZDO> kv in man.m_objectsByID)
    {
      ZDO z = kv.Value;
      if (z == null || !z.IsValid())
      {
        continue;
      }

      Vector3 p = z.GetPosition();
      float dx = p.x - center.x;
      float dz = p.z - center.z;
      if (dx * dx + dz * dz > maxSqr)
      {
        continue;
      }

      result.Add(z);
    }

    return result;
  }

  private static Heightmap.Biome ResolveBiome(Vector3 pos)
  {
    if (WorldGenerator.instance == null)
    {
      return Heightmap.Biome.None;
    }

    // Live assembly has GetBiome(Vector3) / 4-arg floats — not GetBiome(float,float).
    // Calling the 2-float form (publicized ref) throws MissingMethodException at runtime.
    return WorldGenerator.instance.GetBiome(pos);
  }

  internal static bool TryParseBiomeToken(string token, out Heightmap.Biome biome)
  {
    biome = Heightmap.Biome.None;
    if (string.IsNullOrWhiteSpace(token))
    {
      return false;
    }

    string t = token.Trim();
    EnsureEwd();
    if (_ewdTryGetBiome != null)
    {
      object[] args = { t, Heightmap.Biome.None };
      if (_ewdTryGetBiome.Invoke(null, args) is true)
      {
        biome = (Heightmap.Biome)args[1]!;
        return biome != Heightmap.Biome.None;
      }
    }

    if (Enum.TryParse(t, ignoreCase: true, out Heightmap.Biome parsed) && parsed != Heightmap.Biome.None)
    {
      biome = parsed;
      return true;
    }

    if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bits) && bits != 0)
    {
      biome = (Heightmap.Biome)bits;
      return true;
    }

    return false;
  }

  private static void EnsureEwd()
  {
    if (_ewdResolved)
    {
      return;
    }

    _ewdResolved = true;
    try
    {
      foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type? type = asm.GetType("ExpandWorldData.BiomeManager") ?? asm.GetType("ExpandWorld.BiomeManager");
        if (type == null)
        {
          continue;
        }

        _ewdTryGetBiome = type.GetMethod(
          "TryGetBiome",
          BindingFlags.Public | BindingFlags.Static,
          null,
          new[] { typeof(string), typeof(Heightmap.Biome).MakeByRefType() },
          null);
        if (_ewdTryGetBiome != null)
        {
          SkillBombsPlugin.LogAt(LogLevel.Info, "XP blocklist: soft-linked Expand World biome name map.");
          return;
        }
      }
    }
    catch (Exception ex)
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"XP blocklist: EWD biome soft-link skipped ({ex.Message}).");
    }
  }

  internal static IntRange? ParseObjectsLimit(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw))
    {
      return null;
    }

    string s = raw!.Trim();
    int sep = s.IndexOf(';');
    if (sep < 0)
    {
      if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int min))
      {
        return null;
      }

      // EWP RangeInt: lone N → Min=N, Max=0 (no upper bound).
      return new IntRange(min, 0);
    }

    string a = s.Substring(0, sep).Trim();
    string b = s.Substring(sep + 1).Trim();
    if (!int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minB)
        || !int.TryParse(b, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxB))
    {
      return null;
    }

    return new IntRange(minB, maxB);
  }

  internal static bool TryCompile(XpBlocklistRuleData data, out CompiledRule rule, out string error)
  {
    rule = default!;
    error = "";
    if (string.IsNullOrWhiteSpace(data.name))
    {
      error = "rule missing required name";
      return false;
    }

    string name = data.name!.Trim();
    HashSet<Heightmap.Biome>? biomes = null;
    if (data.biomes != null && data.biomes.Count > 0)
    {
      biomes = new HashSet<Heightmap.Biome>();
      foreach (string token in data.biomes)
      {
        if (TryParseBiomeToken(token, out Heightmap.Biome b))
        {
          biomes.Add(b);
        }
        else
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning, $"XP blocklist '{name}': unknown biome '{token}' (ignored).");
        }
      }
    }

    float? posX = null;
    float? posZ = null;
    float? posMin = data.minDistance;
    float? posMax = data.maxDistance;
    if (!string.IsNullOrWhiteSpace(data.position))
    {
      string[] parts = data.position!.Split(',');
      if (parts.Length != 2)
      {
        error = $"'{name}': position must be x,z (two numbers), got '{data.position}'";
        return false;
      }

      if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
          || !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
      {
        error = $"'{name}': position parse failed '{data.position}'";
        return false;
      }

      posX = x;
      posZ = z;
      if (!posMax.HasValue)
      {
        error = $"'{name}': position requires maxDistance";
        return false;
      }
    }
    else if (posMin.HasValue || posMax.HasValue)
    {
      error = $"'{name}': minDistance/maxDistance require position";
      return false;
    }

    if (!TryCompileObjects(name, data.objects, out List<CompiledObject> objects, out string? objErr))
    {
      error = objErr!;
      return false;
    }

    if (!TryCompileObjects(name, data.bannedObjects, out List<CompiledObject> banned, out string? banErr))
    {
      error = banErr!;
      return false;
    }

    bool hasBiome = biomes != null && biomes.Count > 0;
    bool hasY = data.minY.HasValue || data.maxY.HasValue;
    bool hasPos = posX.HasValue;
    bool hasObj = objects.Count > 0;
    bool hasBan = banned.Count > 0;
    if (!hasBiome && !hasY && !hasPos && !hasObj && !hasBan)
    {
      SkillBombsPlugin.LogAt(LogLevel.Warning, $"XP blocklist '{name}': no clauses — rule never matches.");
    }

    rule = new CompiledRule
    {
      Name = name,
      Biomes = biomes,
      MinY = data.minY,
      MaxY = data.maxY,
      HasAbsoluteY = hasY,
      HasPosition = hasPos,
      PosX = posX ?? 0f,
      PosZ = posZ ?? 0f,
      PosMinDistance = posMin,
      PosMaxDistance = posMax,
      Objects = objects,
      ObjectsLimit = ParseObjectsLimit(data.objectsLimit),
      BannedObjects = banned,
      BannedObjectsLimit = ParseObjectsLimit(data.bannedObjectsLimit),
      HasAnyClause = hasBiome || hasY || hasPos || hasObj || hasBan
    };
    return true;
  }

  private static bool TryCompileObjects(
    string ruleName,
    List<XpBlocklistObjectData>? list,
    out List<CompiledObject> result,
    out string? error)
  {
    result = new List<CompiledObject>();
    error = null;
    if (list == null)
    {
      return true;
    }

    foreach (XpBlocklistObjectData raw in list)
    {
      if (string.IsNullOrWhiteSpace(raw.prefab))
      {
        error = $"'{ruleName}': object entry missing prefab";
        return false;
      }

      string prefab = raw.prefab.Trim();
      List<CompiledFilter> filters = new();
      int positive = 0;

      // EWP FilterShorthand: singular filter / bannedFilter merge into the plural lists.
      List<string> requiredLines = new();
      if (!string.IsNullOrWhiteSpace(raw.filter))
      {
        requiredLines.Add(raw.filter!.Trim());
      }

      if (raw.filters != null)
      {
        requiredLines.AddRange(raw.filters);
      }

      List<string> bannedLines = new();
      if (!string.IsNullOrWhiteSpace(raw.bannedFilter))
      {
        bannedLines.Add(raw.bannedFilter!.Trim());
      }

      if (raw.bannedFilters != null)
      {
        bannedLines.AddRange(raw.bannedFilters);
      }

      foreach (string line in requiredLines)
      {
        if (TryParseFilter(line, banned: false, out CompiledFilter f))
        {
          filters.Add(f);
          positive++;
        }
        else
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning, $"XP blocklist '{ruleName}': bad filter '{line}'.");
        }
      }

      foreach (string line in bannedLines)
      {
        if (TryParseFilter(line, banned: true, out CompiledFilter f))
        {
          filters.Add(f);
        }
        else
        {
          SkillBombsPlugin.LogAt(LogLevel.Warning, $"XP blocklist '{ruleName}': bad bannedFilter '{line}'.");
        }
      }

      result.Add(new CompiledObject
      {
        PrefabId = prefab,
        PrefabHash = prefab.GetStableHashCode(),
        MinDistance = raw.minDistance,
        MaxDistance = raw.maxDistance ?? DefaultObjectMaxDistance,
        MinHeight = raw.minHeight,
        MaxHeight = raw.maxHeight,
        Weight = raw.weight ?? 1,
        Filters = filters,
        FilterLimit = raw.filterLimit,
        PositiveFilterCount = positive
      });
    }

    return true;
  }

  private static bool TryParseFilter(string line, bool banned, out CompiledFilter filter)
  {
    filter = default!;
    if (string.IsNullOrWhiteSpace(line))
    {
      return false;
    }

    string[] parts = line.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
    if (parts.Length < 3)
    {
      return false;
    }

    string type = parts[0].ToLowerInvariant();
    string key = parts[1];
    string value = parts[2];
    float weight = banned ? 10000f : 1f;
    if (parts.Length >= 4
        && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
    {
      weight = w;
    }

    int keyHash = key.GetStableHashCode();
    switch (type)
    {
      case "int":
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
        {
          return false;
        }

        filter = new CompiledFilter
        {
          Type = FilterType.Int, KeyHash = keyHash, IntValue = iv, Weight = weight, Banned = banned
        };
        return true;
      case "bool":
        if (!value.Equals("true", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
            && value != "0"
            && value != "1")
        {
          return false;
        }

        bool bv = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
        filter = new CompiledFilter
        {
          Type = FilterType.Bool, KeyHash = keyHash, BoolValue = bv, Weight = weight, Banned = banned
        };
        return true;
      case "float":
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv))
        {
          return false;
        }

        filter = new CompiledFilter
        {
          Type = FilterType.Float, KeyHash = keyHash, FloatValue = fv, Weight = weight, Banned = banned
        };
        return true;
      case "string":
        filter = new CompiledFilter
        {
          Type = FilterType.String, KeyHash = keyHash, StringValue = value, Weight = weight, Banned = banned
        };
        return true;
      default:
        return false;
    }
  }

  internal struct IntRange
  {
    public int Min;
    public int Max;

    public IntRange(int min, int max)
    {
      Min = min;
      Max = max;
    }
  }

  internal sealed class CompiledRule
  {
    public string Name = "";
    public HashSet<Heightmap.Biome>? Biomes;
    public float? MinY;
    public float? MaxY;
    public bool HasAbsoluteY;
    public bool HasPosition;
    public float PosX;
    public float PosZ;
    public float? PosMinDistance;
    public float? PosMaxDistance;
    public List<CompiledObject> Objects = new();
    public IntRange? ObjectsLimit;
    public List<CompiledObject> BannedObjects = new();
    public IntRange? BannedObjectsLimit;
    public bool HasAnyClause;
  }

  internal sealed class CompiledObject
  {
    public string PrefabId = "";
    public int PrefabHash;
    public float? MinDistance;
    public float MaxDistance = DefaultObjectMaxDistance;
    public float? MinHeight;
    public float? MaxHeight;
    public int Weight = 1;
    public List<CompiledFilter> Filters = new();
    public float? FilterLimit;
    public int PositiveFilterCount;
  }

  internal enum FilterType
  {
    Int,
    Bool,
    Float,
    String
  }

  internal sealed class CompiledFilter
  {
    public FilterType Type;
    public int KeyHash;
    public int IntValue;
    public bool BoolValue;
    public float FloatValue;
    public string StringValue = "";
    public float Weight;
    public bool Banned;
  }
}
