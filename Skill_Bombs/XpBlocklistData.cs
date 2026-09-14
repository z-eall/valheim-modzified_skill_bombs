using System.Collections.Generic;
using System.ComponentModel;

namespace Skill_Bombs;

#pragma warning disable CS0649 // YamlDotNet sets public fields via reflection.

/// <summary>YAML DTO for one XP Gain Blocklist rule (camelCase via YamlDotNet).</summary>
internal sealed class XpBlocklistRuleData
{
  [DefaultValue(null)]
  public string? name;

  [DefaultValue(null)]
  public List<string>? biomes;

  [DefaultValue(null)]
  public float? minY;

  [DefaultValue(null)]
  public float? maxY;

  /// <summary>Horizontal x,z only (two numbers). Third token is a parse error at load.</summary>
  [DefaultValue(null)]
  public string? position;

  [DefaultValue(null)]
  public float? minDistance;

  [DefaultValue(null)]
  public float? maxDistance;

  [DefaultValue(null)]
  public List<XpBlocklistObjectData>? objects;

  /// <summary>EWP RangeInt string: lone N → Min=N Max=0 (no upper); N;M → band.</summary>
  [DefaultValue(null)]
  public string? objectsLimit;

  [DefaultValue(null)]
  public List<XpBlocklistObjectData>? bannedObjects;

  [DefaultValue(null)]
  public string? bannedObjectsLimit;
}

internal sealed class XpBlocklistObjectData
{
  [DefaultValue("")]
  public string prefab = "";

  [DefaultValue(null)]
  public float? minDistance;

  [DefaultValue(null)]
  public float? maxDistance;

  [DefaultValue(null)]
  public float? minHeight;

  [DefaultValue(null)]
  public float? maxHeight;

  [DefaultValue(null)]
  public int? weight;

  /// <summary>EWP singular shorthand — one required data check (merged into <see cref="filters"/>).</summary>
  [DefaultValue(null)]
  public string? filter;

  [DefaultValue(null)]
  public List<string>? filters;

  /// <summary>EWP singular shorthand — one banned data check (merged into <see cref="bannedFilters"/>).</summary>
  [DefaultValue(null)]
  public string? bannedFilter;

  [DefaultValue(null)]
  public List<string>? bannedFilters;

  [DefaultValue(null)]
  public float? filterLimit;
}

#pragma warning restore CS0649
