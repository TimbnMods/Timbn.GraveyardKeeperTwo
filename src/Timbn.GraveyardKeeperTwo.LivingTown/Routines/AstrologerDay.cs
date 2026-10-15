namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// The astrologer's day in his tower. He stands at his spot facing down, now and then reads the necronomicon or looks
/// over his pieces and comes back, or does his custom_02 animation. It starts once you've met him in the port and he
/// stands at his spot.
/// </summary>
internal sealed class AstrologerDay
{
    private const string _home = "port_astrologer_spawn";
    private const string _book = "astrologer_necronomicon_03";
    private const string _pieces = "astrologer_pieces_astrologer_spot";
    private const string _meet = "27_port_astrologer_meet";
    private const float _goChance = 0.75f;

    public TimbnNpcDay Build()
    {
        return new TimbnNpcDay()
            .Block("day", 0f, block => block
                .Origin(_home)
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.Astrologer.TpToTower))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(Pick)))
            .OnlyWhile(() => TimbnQuests.StatusOf(_meet) == QuestStatus.Completed)
            .StartWhenAt(_home)
            .ResumeWhenAt(_home)
            .LogSteps(() => PluginConfig.WriteDebugLog.Value);
    }

    private static TimbnNpcSteps? Pick(TimbnNpc npc)
    {
        if (npc.RollChance(_goChance, out var roll, "something") is not { } random)
            return null;

        switch (random.Next(3))
        {
            case 0:
                Log($"Rolled {roll:0.00}, the necronomicon.");
                return Visit(_book);
            case 1:
                Log($"Rolled {roll:0.00}, the pieces.");
                return Visit(_pieces);
            default:
                Log($"Rolled {roll:0.00}, {TimbnGameIds.NpcAnimations.Astrologer.Cap}.");
                return new TimbnNpcSteps().Animate(TimbnGameIds.NpcAnimations.Astrologer.Cap);
        }
    }

    private static TimbnNpcSteps Visit(string spot) =>
        new TimbnNpcSteps()
            .Walk(spot)
            .Face(Direction.Up)
            .WaitGameMinutes(15, 30)
            .Walk(_home)
            .Face(Direction.Down);

    private static void Log(string line)
    {
        if (PluginConfig.WriteDebugLog.Value)
            Plugin.Logger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} Astrologer: {line}");
    }
}
