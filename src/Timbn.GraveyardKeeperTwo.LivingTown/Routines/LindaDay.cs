namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// Linda's day in her club. She works the club from the morning, stands at the entrance in the afternoon, is back in
/// the club for the evening, is on the stage after midnight once the concert quest is done, and goes to bed at two. It
/// starts once the Safe Harbor quest is done.
/// </summary>
internal sealed class LindaDay
{
    private const string _home = "RT_tavern_owner_spawn";
    private const string _entrance = "port_linda_start";
    private const string _doorway = "village_letter_owner_spawn";
    private const string _barFront = "port_linda_player_spot";
    private const string _safeHarbor = "29_port_linda_meet";
    private const string _concert = "68_port_concert_start";
    private const string _music = TimbnGameIds.Music.Violin;
    private const float _catX = -104.35f;
    private const float _catZ = -338.61f;
    private const float _goChance = 0.75f;
    private const float _morning = 8 / 24f;
    private const float _afternoon = 15 / 24f;
    private const float _night = 18 / 24f;
    private const float _late = 0f;
    private const float _bed = 2 / 24f;
    private const double _quietTailMinutes = 90;

    private readonly Plugin _plugin;
    private TimbnNpc? _performer;
    private IDisposable? _song;

    public LindaDay(Plugin plugin)
    {
        _plugin = plugin;
    }

    public TimbnNpcDay Build()
    {
        return new TimbnNpcDay()
            .Block("morning", _morning, block => block
                .Origin(_home)
                .Intro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToTavern))
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToTavern))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(npc => Pick(npc, "morning", "table one", "table two"))))
            .Block("afternoon", _afternoon, block => block
                .Origin(_entrance)
                .Intro(ToEntrance)
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToTavernEntrance))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(npc => Pick(npc, "afternoon", "cat"))))
            .Block("night", _night, block => block
                .Origin(_home)
                .Intro(steps => steps
                    .Walk(_barFront)
                    .WalkNavmesh(_home)
                    .Face(Direction.Down))
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToTavern))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(npc => Pick(npc, "night", "table one"))))
            .Block("late", _late, block => block
                .Intro(steps => steps
                    .Decide(_ => ConcertDone() ? Sing() : null))
                .LateIntro(steps => steps
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToTavern)
                    .Decide(_ => ConcertDone() ? Sing() : null))
                .Loop(steps => steps
                    .WaitGameMinutes(20, 40)
                    .Decide(npc => ConcertDone() ? null : Pick(npc, "night", "table one"))))
            .Block("bed", _bed, block => block
                .Intro(steps => steps
                    .RunCode(_ => _performer = null)
                    .WalkNavmesh("port_concert_doctor_scene")
                    .Walk("tavern_port_road_left_01")
                    .WalkNavmesh(_catX, _catZ)
                    .Face(Direction.Up)
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToSpot))
                .LateIntro(steps => steps
                    .RunCode(_ => _performer = null)
                    .FireEvent(TimbnGameIds.NpcEvents.TavernOwner.TpToSpot)))
            .OnlyWhile(() => TimbnQuests.StatusOf(_safeHarbor) == QuestStatus.Completed)
            .ResumeWhenAt(_home)
            .Doorway(_home, _doorway, TimbnWalk.Straight)
            .LogSteps(() => PluginConfig.WriteDebugLog.Value);
    }

    public void Tick()
    {
        var playing = _performer is { IsGone: false } performer
            && TimbnPlayer.Zone is { } zone
            && TimbnWorld.ZonesAt(performer.Position).Contains(zone);
        if (playing && _song == null)
        {
            _song = _plugin.Audio.PlayMusic(_music);
            Log("Her violin fills the club.");
        }
        else if (!playing && _song != null)
        {
            StopSong();
        }
    }

    public void Reset()
    {
        _performer = null;
        StopSong();
    }

    private void StopSong()
    {
        _song?.Dispose();
        _song = null;
    }

    private static bool ConcertDone() => TimbnQuests.StatusOf(_concert) == QuestStatus.Completed;

    private static TimbnNpcSteps ToEntrance(TimbnNpcSteps steps) =>
        steps
            .WalkNavmesh("linda_cough_refugees_linda_02")
            .Walk("tavern_port_road_left_02")
            .WalkNavmesh(_entrance)
            .Face(Direction.Down);

    private TimbnNpcSteps Sing() =>
        new TimbnNpcSteps()
            .WalkNavmesh("port_concert_linda_start")
            .Walk("port_concert_linda_spot")
            .Animate(TimbnGameIds.NpcAnimations.TavernOwner.ViolinLoop)
            .Face(Direction.Down)
            .RunCode(npc => _performer = npc);

    private static TimbnNpcSteps TableTwo() =>
        new TimbnNpcSteps()
            .Walk("village_letter_owner_table")
            .WalkNavmesh("port_concert_villager_03_spot")
            .Animate(TimbnGameIds.NpcAnimations.TavernOwner.WipingBeerGlass)
            .Face(Direction.Down)
            .WaitGameMinutes(30, 60)
            .WalkNavmesh("tavern_port_road_right_06")
            .Walk(_barFront)
            .WalkNavmesh(_home)
            .Face(Direction.Down);

    private static TimbnNpcSteps TableOne() =>
        new TimbnNpcSteps()
            .WalkNavmesh("linda_cough_refugees_linda_02")
            .WalkNavmesh("tavern_port_road_left_07")
            .Animate(TimbnGameIds.NpcAnimations.TavernOwner.WipingBeerGlass)
            .Face(Direction.Down)
            .WaitGameMinutes(30, 60)
            .WalkNavmesh("tavern_port_road_right_06")
            .Walk(_barFront)
            .WalkNavmesh(_home)
            .Face(Direction.Down);

    private static TimbnNpcSteps Cat() =>
        new TimbnNpcSteps()
            .Walk("port_linda_02")
            .WalkNavmesh("tavern_port_road_left_01")
            .WalkNavmesh(_catX, _catZ)
            .Face(Direction.Up)
            .WaitGameMinutes(15, 30)
            .WalkNavmesh("tavern_port_road_left_01")
            .WalkNavmesh("port_linda_02")
            .Walk(_entrance);

    private TimbnNpcSteps? Pick(TimbnNpc npc, string block, params string[] trips)
    {
        var left = npc.MinutesLeftInBlock;
        if (left < _quietTailMinutes)
        {
            Log($"Staying put in the {block}, the next block starts in {left:0} minutes.");
            return null;
        }

        if (npc.RollChance(_goChance, out var roll) is not { } random)
            return null;

        var trip = trips[random.Next(trips.Length)];
        Log($"Rolled {roll:0.00}, the {trip} trip.");
        return trip switch
        {
            "table one" => TableOne(),
            "table two" => TableTwo(),
            _ => Cat(),
        };
    }

    private static void Log(string line)
    {
        if (PluginConfig.WriteDebugLog.Value)
            Plugin.Logger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} Linda: {line}");
    }
}
