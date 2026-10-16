namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// Larry's daily routine at the base. He remains near his post, occasionally playing his chattering animation
/// or walking to the fairy ring and back. The routine activates once he is at his post.
/// </summary>
internal sealed class LarryDay
{
    private const string _home = "RT_larry_spawn";
    private const string _fairy = "base_fairy_larry_05";
    private const float _goChance = 0.75f;

    public TimbnNpcDay Build()
    {
        return new TimbnNpcDay()
            .Block("day", 0f, block => block
                .Origin(_home)
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.Larry.TpToSpawnWithoutPuff))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(Pick)))
            .StartWhenAt(_home)
            .ResumeWhenAt(_home)
            .LogSteps(() => PluginConfig.WriteDebugLog.Value);
    }

    private static TimbnNpcSteps? Pick(TimbnNpc npc)
    {
        if (npc.RollChance(_goChance, out var roll, "something") is not { } random)
            return null;

        if (random.Next(2) == 0)
        {
            Log($"Rolled {roll:0.00}, {TimbnGameIds.NpcAnimations.Larry.Chattering}.");
            return new TimbnNpcSteps().Animate(TimbnGameIds.NpcAnimations.Larry.Chattering);
        }

        Log($"Rolled {roll:0.00}, the fairy trip.");
        return Fairy();
    }

    private static TimbnNpcSteps Fairy() =>
        new TimbnNpcSteps()
            .Walk(_fairy)
            .WaitGameMinutes(15, 30)
            .Walk(_home)
            // The walk reaches the destination point, but his orientation and posture only align correctly after triggering his spawn teleport.
            .FireEvent(TimbnGameIds.NpcEvents.Larry.TpToSpawnWithoutPuff);

    private static void Log(string line)
    {
        if (PluginConfig.WriteDebugLog.Value)
            Plugin.Logger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} Larry: {line}");
    }
}
