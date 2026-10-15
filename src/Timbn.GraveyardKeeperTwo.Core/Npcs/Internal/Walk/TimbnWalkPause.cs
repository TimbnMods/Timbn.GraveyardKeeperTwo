namespace Timbn.GraveyardKeeperTwo.Core.Framework;

internal static class TimbnWalkPause
{
    private static readonly TimbnPauseCounts<WgoData> _pauses = new(Pause, Resume);

    internal static IDisposable Hold(WgoData npc) => _pauses.Hold(npc);

    internal static void LetGo(Func<WgoData, bool> isThem)
    {
        foreach (var npc in _pauses.Keys.Where(isThem).ToList())
        {
            if (_pauses.Drop(npc) && npc.MovementComponent is { IsPaused: true } movement)
                movement.IsPaused = false;
        }
    }

    internal static void Reset() => _pauses.Clear();

    private static bool Pause(WgoData npc)
    {
        if (npc.MovementComponent is not { IsMoving: true, IsPaused: false } movement)
            return false;

        TimbnWalkState.Pause(movement);
        return true;
    }

    private static void Resume(WgoData npc)
    {
        if (!TimbnGame.IsInGame || MainGame.WorldData.GetWgoData(npc.UniqueId) != npc || npc.MovementComponent is not { IsPaused: true } movement)
            return;

        TimbnWalkState.Resume(movement);
    }
}
