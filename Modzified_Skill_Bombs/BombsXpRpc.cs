namespace Modzified_Skill_Bombs;

internal static class BombsXpRpc
{
  internal const string Raise = "modzified_skill_bombs Raise";

  private static bool _registered;

  internal static void Register()
  {
    if (_registered || ZRoutedRpc.instance == null)
    {
      return;
    }

    ZRoutedRpc.instance.Register<long, long>(Raise, OnRaise);
    _registered = true;
  }

  internal static void ResetRegistrationFlag()
  {
    _registered = false;
  }

  internal static void NotifyThrower(long throwId, long throwerPlayerId)
  {
    if (ZRoutedRpc.instance == null || throwId == 0L || throwerPlayerId == 0L)
    {
      return;
    }

    Player? thrower = Player.GetPlayer(throwerPlayerId);
    if (thrower == null || thrower.m_nview == null || !thrower.m_nview.IsValid())
    {
      return;
    }

    ZDO zdo = thrower.m_nview.GetZDO();
    if (zdo == null || !zdo.IsValid())
    {
      return;
    }

    ZRoutedRpc.instance.InvokeRoutedRPC(zdo.GetOwner(), Raise, throwId, throwerPlayerId);
  }

  private static void OnRaise(long sender, long throwId, long throwerPlayerId)
  {
    BombsXp.RaiseForLocalThrower(throwId, throwerPlayerId);
  }
}
