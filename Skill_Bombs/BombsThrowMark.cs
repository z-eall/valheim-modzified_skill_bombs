using UnityEngine;

namespace Skill_Bombs;

internal sealed class BombsThrowMark : MonoBehaviour
{
  internal long ThrowId;
  internal long ThrowerPlayerId;
  /// <summary>Armed bomb prefab id when known (e.g. BombSmoke).</summary>
  internal string? PrefabId;

  internal bool IsValid => ThrowId != 0L && ThrowerPlayerId != 0L;

  internal void CopyFrom(BombsThrowMark other)
  {
    ThrowId = other.ThrowId;
    ThrowerPlayerId = other.ThrowerPlayerId;
    PrefabId = other.PrefabId;
  }
}
