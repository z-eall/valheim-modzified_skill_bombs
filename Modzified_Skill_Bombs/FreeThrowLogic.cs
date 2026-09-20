using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace Modzified_Skill_Bombs;

internal static class FreeThrowLogic
{
  private static bool _resolvedEffect;
  private static FieldInfo? _craftBonusEffect;
  private static MethodInfo? _effectCreate;

  /// <summary>Roll only — no HUD. Caller plays feedback after a successful proc so exceptions cannot undo the skip.</summary>
  internal static bool TryProc(Attack attack)
  {
    if (Settings.FreeThrow == null || !Settings.FreeThrow.Value || !BombsSkill.Ready)
    {
      return false;
    }

    if (!ThrowSteadiness.IsLocalArmedBomb(attack))
    {
      return false;
    }

    Player? player = Player.m_localPlayer;
    if (player == null)
    {
      return false;
    }

    float max = Mathf.Clamp01((Settings.FreeThrowChanceAtSkillMax?.Value ?? 25) / 100f);
    float p = max * player.GetSkillFactor(BombsSkill.Type);
    if (p <= 0f || UnityEngine.Random.value >= p)
    {
      return false;
    }

    if (SkillBombsPlugin.Allows(LogLevel.Debug))
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"free throw proc (p={p:0.###}).");
    }

    return true;
  }

  internal static void PlayFeedbackSafe(Player? player)
  {
    if (player == null)
    {
      return;
    }

    try
    {
      PlayFeedback(player);
    }
    catch (Exception e)
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"free throw feedback failed: {e.GetType().Name}: {e.Message}");
    }
  }

  private static void PlayFeedback(Player player)
  {
    Vector3 pos = player.transform.position + Vector3.up * 1.35f;

    if (Settings.FreeThrowBonusText != null && Settings.FreeThrowBonusText.Value)
    {
      string text = Settings.FreeThrowText?.Value ?? "Freethrow!";
      if (string.IsNullOrWhiteSpace(text))
      {
        text = "Freethrow!";
      }

      FreeThrowFloater.Show(pos, text.Trim());
    }

    if (Settings.FreeThrowBonusEffect != null && Settings.FreeThrowBonusEffect.Value)
    {
      TryPlayBonusEffect(pos);
    }
  }

  private static void TryPlayBonusEffect(Vector3 pos)
  {
    EnsureEffectResolved();
    if (InventoryGui.instance == null || _craftBonusEffect == null)
    {
      return;
    }

    try
    {
      object? effect = _craftBonusEffect.GetValue(InventoryGui.instance);
      if (effect == null)
      {
        return;
      }

      if (_effectCreate == null)
      {
        foreach (MethodInfo method in effect.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
          if (method.Name != "Create")
          {
            continue;
          }

          ParameterInfo[] ps = method.GetParameters();
          if (ps.Length >= 2 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(Quaternion))
          {
            _effectCreate = method;
            break;
          }
        }
      }

      if (_effectCreate == null)
      {
        return;
      }

      ParameterInfo[] createPs = _effectCreate.GetParameters();
      object[] args = new object[createPs.Length];
      args[0] = pos;
      args[1] = Quaternion.identity;
      for (int i = 2; i < createPs.Length; i++)
      {
        if (createPs[i].HasDefaultValue)
        {
          args[i] = createPs[i].DefaultValue!;
        }
        else if (createPs[i].ParameterType == typeof(Transform))
        {
          args[i] = null!;
        }
        else if (createPs[i].ParameterType == typeof(float))
        {
          args[i] = 1f;
        }
        else if (createPs[i].ParameterType == typeof(int))
        {
          args[i] = -1;
        }
        else if (createPs[i].ParameterType == typeof(ZDOID))
        {
          args[i] = Player.m_localPlayer != null ? Player.m_localPlayer.GetZDOID() : ZDOID.None;
        }
        else
        {
          args[i] = createPs[i].ParameterType.IsValueType ? Activator.CreateInstance(createPs[i].ParameterType)! : null!;
        }
      }

      _effectCreate.Invoke(effect, args);
    }
    catch (Exception e)
    {
      SkillBombsPlugin.LogAt(LogLevel.Debug, $"free throw bonus effect failed: {e.Message}");
    }
  }

  private static void EnsureEffectResolved()
  {
    if (_resolvedEffect)
    {
      return;
    }

    _resolvedEffect = true;
    Type guiType = InventoryGui.instance != null ? InventoryGui.instance.GetType() : typeof(InventoryGui);
    _craftBonusEffect = guiType.GetField(
      "m_craftBonusEffect",
      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
  }
}
