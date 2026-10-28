namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a person's day for Npcs.Schedule. The day is made of blocks that each start at a set time. A block's
/// intro runs once when it starts on time. Its loop repeats until the next block starts. A block joined late runs its
/// late steps in place of the intro. That happens after a load, a sleep or a jump of the clock. When the next block
/// comes due during a trip picked by Decide, it waits for the trip to end. The person stops for a conversation or a
/// story scene. When the game moves them itself, the day leaves them to it until a later block starts. A
/// <see cref="TimbnNpc.RunSteps"/> run takes them over until it ends. Then the day joins the block of the moment late.
/// Shut a vendor's trade window with Npcs.CloseTradeWhen.
/// </summary>
/// <example>
/// <code>
/// var day = new TimbnNpcDay()
///     .Block("working", 0.25f, block => block
///         .Origin("RT_herm_spawn")
///         .Intro(steps => steps.FireEvent(TimbnGameIds.NpcEvents.Herm.GoToSpawn))
///         .LateIntro(steps => steps.FireEvent(TimbnGameIds.NpcEvents.Herm.TpToSpawn))
///         .Loop(steps => steps.WaitGameMinutes(10, 30).Decide(PickTrip)))
///     .Block("night", 0.85f, block => block
///         .Intro(steps => steps.FireEvent(TimbnGameIds.NpcEvents.Herm.GoHome))
///         .LateIntro(steps => steps.FireEvent(TimbnGameIds.NpcEvents.Herm.TpToSpot)));
/// Npcs.Schedule(TimbnGameIds.Npcs.Herm, day);
/// </code>
/// </example>
public sealed class TimbnNpcDay
{
    internal List<TimbnNpcBlock> Blocks { get; } = [];

    internal Func<bool>? Condition { get; private set; }

    internal string? StartPoint { get; private set; }

    internal List<TimbnNpcDoorway> Doorways { get; } = [];

    internal string? ResumePoint { get; private set; }

    private Func<bool>? _logWhen;

    internal bool Logged => _logWhen?.Invoke() == true;

    /// <summary>Adds a block to the day.</summary>
    /// <param name="name">A name for the status and the log, such as working.</param>
    /// <param name="start">When it starts, as a time of day from 0 at midnight, 0.25 at dawn, 0.5 at noon to 0.75 at dusk.</param>
    /// <param name="build">Fills in the block's intro, join late steps and loop.</param>
    public TimbnNpcDay Block(string name, float start, Action<TimbnNpcBlock> build)
    {
        var block = new TimbnNpcBlock(name, start);
        build(block);
        Blocks.Add(block);
        return this;
    }

    /// <summary>
    /// Runs the day only while a check is true, such as once a quest is done. While it is false the person is left
    /// alone, and when it turns true the block of the moment is joined late.
    /// </summary>
    /// <example>
    /// <code>
    /// day.OnlyWhile(() => TimbnQuests.StatusOf("29_port_linda_meet") == QuestStatus.Completed);
    /// </code>
    /// </example>
    public TimbnNpcDay OnlyWhile(Func<bool> condition)
    {
        Condition = condition;
        return this;
    }

    /// <summary>
    /// Makes every walk that sets off from a point go by another point first, and every walk heading to it come by
    /// that point last, such as a home point behind a counter. The hop between the two is over the navmesh unless
    /// another walk is given.
    /// </summary>
    /// <example>
    /// <code>
    /// day.Doorway("RT_tavern_owner_spawn", "village_letter_owner_spawn", TimbnWalk.Straight);
    /// </code>
    /// </example>
    /// <param name="pointId">The point, such as a home point.</param>
    /// <param name="viaId">The point every walk to or from it passes.</param>
    /// <param name="walk">How to cross between the two.</param>
    public TimbnNpcDay Doorway(string pointId, string viaId, TimbnWalk walk = TimbnWalk.Navmesh)
    {
        Doorways.Add(new TimbnNpcDoorway(pointId, viaId, walk));
        return this;
    }

    /// <summary>
    /// Starts the day in a save only once the person stands on this point, such as their spawn point, so someone the
    /// story keeps somewhere else is left alone until the story brings them there. Once the day started it is not
    /// checked again until the save closes or the day's check turns false.
    /// </summary>
    /// <example>
    /// <code>
    /// day.StartWhenAt("RT_larry_spawn");
    /// </code>
    /// </example>
    /// <param name="pointId">The point the person must stand on, such as RT_larry_spawn.</param>
    public TimbnNpcDay StartWhenAt(string pointId)
    {
        StartPoint = pointId;
        return this;
    }

    /// <summary>
    /// Keeps the day from carrying on after the game moved the person. It waits until they stand on this point again,
    /// for example their home point. The day resumes at a later block start only while they are there. Without it the
    /// day carries on at the next block start wherever they are.
    /// </summary>
    public TimbnNpcDay ResumeWhenAt(string pointId)
    {
        ResumePoint = pointId;
        return this;
    }

    /// <summary>
    /// Writes every step, walk, claim and skip to the mod's log at the debug level. Each line starts with the game day
    /// and time. Pass a check to log only while it holds, such as a hidden setting, since it is asked each time.
    /// </summary>
    /// <example>
    /// <code>
    /// .LogSteps(() => PluginConfig.WriteDebugLog.Value)
    /// </code>
    /// </example>
    /// <param name="when">Whether to write right now. Leave it out to always write.</param>
    public TimbnNpcDay LogSteps(Func<bool>? when = null)
    {
        _logWhen = when ?? (() => true);
        return this;
    }
}

/// <summary>One block of a <see cref="TimbnNpcDay"/>.</summary>
public sealed class TimbnNpcBlock
{
    internal TimbnNpcBlock(string name, float start)
    {
        Name = name;
        Start = start;
    }

    internal string Name { get; }

    internal float Start { get; }

    internal string? OriginPoint { get; private set; }

    internal TimbnNpcSteps IntroSteps { get; private set; } = new();

    internal TimbnNpcSteps LateSteps { get; private set; } = new();

    internal TimbnNpcSteps LoopSteps { get; private set; } = new();

    /// <summary>
    /// The point the person is put back at during this block when a walk along the paths finds no route, such as a
    /// vendor's stall, and the walk is tried once more from there. Without it the walk is skipped.
    /// </summary>
    public TimbnNpcBlock Origin(string pointId)
    {
        OriginPoint = pointId;
        return this;
    }

    /// <summary>Sets the steps that run once when the block starts on time.</summary>
    public TimbnNpcBlock Intro(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        IntroSteps = build(new TimbnNpcSteps());
        return this;
    }

    /// <summary>Sets the steps that run in place of the intro when the block is joined late. They usually put the person where the block expects them.</summary>
    public TimbnNpcBlock LateIntro(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        LateSteps = build(new TimbnNpcSteps());
        return this;
    }

    /// <summary>Sets the steps that repeat until the next block starts.</summary>
    public TimbnNpcBlock Loop(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        LoopSteps = build(new TimbnNpcSteps());
        return this;
    }
}
