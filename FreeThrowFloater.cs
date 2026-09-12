using System;
using System.Reflection;
using UnityEngine;

namespace Skill_Bombs;

/// <summary>
/// Free-throw floater via live <see cref="DamageText.ShowText"/>.
/// Publicized refs can miss the string overload — resolve at runtime.
/// TextType.Normal (0) is white; player:false keeps it white (player:true paints Normal red).
/// </summary>
internal static class FreeThrowFloater
{
  private static MethodInfo? _showText;
  private static object? _normalType;
  private static bool _resolved;

  internal static void Show(Vector3 worldPos, string text)
  {
    if (DamageText.instance == null || string.IsNullOrEmpty(text))
    {
      return;
    }

    EnsureResolved();
    if (_showText == null || _normalType == null)
    {
      return;
    }

    try
    {
      ParameterInfo[] ps = _showText.GetParameters();
      object[] args = new object[ps.Length];
      args[0] = _normalType;
      args[1] = worldPos;
      args[2] = text;
      for (int i = 3; i < ps.Length; i++)
      {
        if (ps[i].ParameterType == typeof(bool))
        {
          // false = white Normal; true would recolor as "my damage" red
          args[i] = false;
        }
        else if (ps[i].HasDefaultValue)
        {
          args[i] = ps[i].DefaultValue!;
        }
        else
        {
          args[i] = ps[i].ParameterType.IsValueType
            ? Activator.CreateInstance(ps[i].ParameterType)!
            : null!;
        }
      }

      _showText.Invoke(DamageText.instance, args);
    }
    catch
    {
      // Feedback must never undo a free-throw proc.
    }
  }

  private static void EnsureResolved()
  {
    if (_resolved)
    {
      return;
    }

    _resolved = true;
    Type dtType = DamageText.instance != null ? DamageText.instance.GetType() : typeof(DamageText);
    foreach (MethodInfo method in dtType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
    {
      if (method.Name != "ShowText")
      {
        continue;
      }

      ParameterInfo[] ps = method.GetParameters();
      if (ps.Length < 3
          || ps[1].ParameterType != typeof(Vector3)
          || ps[2].ParameterType != typeof(string))
      {
        continue;
      }

      _showText = method;
      Type textType = ps[0].ParameterType;
      try
      {
        _normalType = Enum.Parse(textType, "Normal");
      }
      catch
      {
        _normalType = Enum.ToObject(textType, 0);
      }

      break;
    }
  }
}
