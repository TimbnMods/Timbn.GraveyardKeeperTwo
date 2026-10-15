namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A trip off a person's origin for the <see cref="TimbnNpcSteps.ChainTrips"/> step, in three parts. Leave runs only when the
/// trip starts from the origin, the body always runs, and Return runs only when no next trip chains on, so trips can
/// follow each other without going back to the origin in between.
/// </summary>
/// <example>
/// <code>
/// new TimbnNpcTrip()
///     .Leave(steps => steps.WalkNavmesh("npc_sim_village_right_side_road_34"))
///     .Body(steps => steps.Walk("npc_sim_village_right_side_03").WaitGameMinutes(45))
///     .Return(steps => steps.Walk("npc_sim_village_right_side_road_34").WalkNavmesh("RT_herm_spawn"));
/// </code>
/// </example>
public sealed class TimbnNpcTrip
{
    internal TimbnNpcSteps LeaveSteps { get; private set; } = new();

    internal TimbnNpcSteps BodySteps { get; private set; } = new();

    internal TimbnNpcSteps ReturnSteps { get; private set; } = new();

    /// <summary>The steps from the origin to where the trip starts, skipped when it chains on from another trip.</summary>
    public TimbnNpcTrip Leave(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        LeaveSteps = build(new TimbnNpcSteps());
        return this;
    }

    /// <summary>What the trip is for, such as walking to a bench and sitting there. Write it so it works from any trip's end too.</summary>
    public TimbnNpcTrip Body(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        BodySteps = build(new TimbnNpcSteps());
        return this;
    }

    /// <summary>The steps back to the origin, run when no next trip chains on.</summary>
    public TimbnNpcTrip Return(Func<TimbnNpcSteps, TimbnNpcSteps> build)
    {
        ReturnSteps = build(new TimbnNpcSteps());
        return this;
    }
}
