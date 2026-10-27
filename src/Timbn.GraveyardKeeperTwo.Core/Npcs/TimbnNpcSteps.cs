namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A list of things for a person to do in order. Build one in a block's Intro, LateIntro or Loop. Return one from
/// <see cref="Decide"/> to pick a trip or hand one to <see cref="TimbnNpc.RunSteps"/> to play it directly. A wait
/// names its clock, either game minutes that follow the game's speed or real seconds that do not.
/// </summary>
/// <example>
/// <code>
/// steps
///     .WalkNavmesh("npc_sim_village_right_side_road_34")
///     .Walk("npc_sim_village_right_side_03")
///     .WaitGameMinutes(45)
///     .Walk("npc_sim_village_right_side_road_34")
///     .WalkNavmesh("RT_herm_spawn");
/// </code>
/// </example>
public sealed class TimbnNpcSteps
{
    private const float _longestEventMinutes = 180f;

    internal List<TimbnRoutineStep<TimbnNpcRun>> Steps { get; } = [];

    internal int Count => Steps.Count;

    /// <summary>Walks along the game's paths to a point by the shortest way, so name only the stops that matter.</summary>
    public TimbnNpcSteps Walk(string pointId) =>
        Add($"walk to {pointId}", run => WalkStep(run, pointId, TimbnWalk.Paths));

    /// <summary>Walks over the navmesh to a point, for stepping onto the paths from a spot they do not reach.</summary>
    public TimbnNpcSteps WalkNavmesh(string pointId) =>
        Add($"navmesh walk to {pointId}", run => WalkStep(run, pointId, TimbnWalk.Navmesh));

    /// <summary>Walks over the navmesh to a spot with no game point, given as its x and z, such as a corner of a room.</summary>
    public TimbnNpcSteps WalkNavmesh(float x, float z) =>
        Add($"navmesh walk to ({x:0.00}, {z:0.00})", run => TimbnStepResult<TimbnNpcRun>.WalkThen(arrived => run.Npc.WalkToSpot(x, z, arrived, failed: arrived)));

    /// <summary>
    /// Walks in a straight line to a spot given as its x and z, around nothing, for the short step onto a spot the
    /// navmesh leaves out, such as the seat of a placed bench. Walk the navmesh to the nearest ground first, and back
    /// off the same way.
    /// </summary>
    public TimbnNpcSteps WalkStraight(float x, float z) =>
        Add($"straight walk to ({x:0.00}, {z:0.00})", run => TimbnStepResult<TimbnNpcRun>.WalkThen(arrived => run.Npc.WalkToSpot(x, z, arrived, failed: arrived, straight: true)));

    /// <summary>
    /// Walks in straight lines from corner to corner as one movement, turning at each corner without stopping, for a
    /// route with several bends such as a way along the graveyard paths. The person ends within 0.1 of the last corner,
    /// the game's own slack for a waypoint walk, so take an exact step onto a seat or a spot with
    /// <see cref="WalkStraight(float, float)"/> after it. If the game's waypoint walking is not available, each corner
    /// is walked as its own straight step instead.
    /// </summary>
    public TimbnNpcSteps WalkStraight(IReadOnlyList<Vector3> corners) =>
        Add($"straight walk through {corners.Count} corner(s)", run =>
            TimbnPlannedRoute.IsAvailable
                ? TimbnStepResult<TimbnNpcRun>.WalkThen(arrived => run.Npc.WalkThroughSpots(corners, arrived, failed: arrived))
                : TimbnStepResult<TimbnNpcRun>.Insert(corners.Aggregate(new TimbnNpcSteps(), (steps, corner) => steps.WalkStraight(corner.x, corner.z)).Steps));

    /// <summary>Stands still for a number of game minutes, skipped when the person found their claimed point taken.</summary>
    public TimbnNpcSteps WaitGameMinutes(float minutes) => WaitGameMinutes(minutes, minutes);

    /// <summary>Stands still for a random number of game minutes between two, skipped when the person found their claimed point taken.</summary>
    public TimbnNpcSteps WaitGameMinutes(float shortest, float longest) =>
        Add(shortest == longest ? $"wait {shortest:0} minutes" : $"wait {shortest:0} to {longest:0} minutes", run =>
        {
            if (run.Npc.PointTaken)
            {
                run.Npc.Log("skipping the wait, the point was taken");
                return TimbnStepResult<TimbnNpcRun>.Next;
            }

            return TimbnStepResult<TimbnNpcRun>.Wait(shortest + run.Random.NextDouble() * Math.Max(0f, longest - shortest));
        });

    /// <summary>
    /// Stands still for a number of real seconds whatever the game speed, such as a pause between the lines of a
    /// cutscene.
    /// </summary>
    public TimbnNpcSteps WaitRealSeconds(float seconds) =>
        Add($"wait {seconds:0.#} real seconds", _ => TimbnStepResult<TimbnNpcRun>.WaitReal(seconds));

    /// <summary>Turns the person to face a direction.</summary>
    public TimbnNpcSteps Face(Direction direction) => Run($"face {direction}", npc => npc.Face(direction));

    /// <summary>Turns the person toward a spot, such as a grave, at whatever angle it lies.</summary>
    public TimbnNpcSteps Face(Vector3 point) => Run($"face ({point.x:0.00}, {point.z:0.00})", npc => npc.Face(point));

    /// <summary>
    /// Plays an animation of the person's own, from <see cref="TimbnGameIds.NpcAnimations"/> such as
    /// <c>TimbnGameIds.NpcAnimations.Herm.AdjustingJabbot</c>. It keeps playing until their next walk.
    /// </summary>
    public TimbnNpcSteps Animate(string trigger) => Run($"animate {trigger}", npc => npc.Animate(trigger));

    /// <summary>Puts the person on a point at once.</summary>
    public TimbnNpcSteps Teleport(string pointId) => Run($"teleport to {pointId}", npc => npc.Teleport(pointId));

    /// <summary>Puts the person on a spot at once, such as a door's landing, keeping the way they face.</summary>
    public TimbnNpcSteps Teleport(Vector3 position) =>
        Run($"teleport to ({position.x:0.00}, {position.z:0.00})", npc => npc.Teleport(position));

    /// <summary>Fires an event of the person's own game script, such as Herm's go_home, and waits until the walk it starts ends.</summary>
    public TimbnNpcSteps FireEvent(string eventName) =>
        Add($"event {eventName}", run =>
        {
            run.Npc.FireEvent(eventName);
            return TimbnStepResult<TimbnNpcRun>.UntilIdle(_longestEventMinutes);
        });

    /// <summary>Runs code of your own as a step.</summary>
    public TimbnNpcSteps RunCode(Action<TimbnNpc> action) => Run("run code", action);

    /// <summary>
    /// Decides what to do when the step is reached, such as rolling for a trip, and runs the steps it returns before
    /// going on. Return null to do nothing. A point claimed during the trip is let go when it ends.
    /// </summary>
    /// <example>
    /// <code>
    /// .Loop(steps => steps
    ///     .WaitGameMinutes(10, 30)
    ///     .Decide(npc => npc.IsFree(Pond) &amp;&amp; npc.Claim(Pond) ? new TimbnNpcSteps().Walk(Pond).WaitGameMinutes(45) : null))
    /// </code>
    /// </example>
    public TimbnNpcSteps Decide(Func<TimbnNpc, TimbnNpcSteps?> decide) =>
        Add("decide", run =>
        {
            TimbnNpcSteps? chosen = null;
            run.Npc.RunSafely(() => chosen = decide(run.Npc), decide);
            return chosen is { Count: > 0 } ? TimbnStepResult<TimbnNpcRun>.Insert(chosen.Steps) : TimbnStepResult<TimbnNpcRun>.Next;
        });

    /// <summary>
    /// Picks a trip off the origin and runs it. Then it asks again from where the trip ended, so trips can chain on
    /// without going back to the origin in between. The pick is called with no previous trip at the origin. It is
    /// called again with the trip just finished after its body. Return a trip to run it (a chained trip skips its Leave
    /// part). Return null to stay at the origin. After a trip, return null to run that trip's Return. Once the next
    /// block is due, no trip chains on and the block waits for the walk home. A point claimed on the way is let go when
    /// the chain is over.
    /// </summary>
    /// <example>
    /// <code>
    /// .Loop(steps => steps
    ///     .WaitGameMinutes(10, 30)
    ///     .ChainTrips((npc, previous) => previous == null || Roll() &lt; 0.4 ? PickTrip(npc) : null))
    /// </code>
    /// </example>
    public TimbnNpcSteps ChainTrips(Func<TimbnNpc, TimbnNpcTrip?, TimbnNpcTrip?> pick) =>
        Add("pick a trip", run => Pick(run, pick, null) is { } trip
            ? TimbnStepResult<TimbnNpcRun>.Insert([.. trip.LeaveSteps.Steps, .. trip.BodySteps.Steps, NextTrip(pick, trip)])
            : TimbnStepResult<TimbnNpcRun>.Next);

    /// <summary>
    /// Claims a free point from a list and walks there, or puts the person there at once. When none is free the step
    /// does nothing and the log says why each one was blocked.
    /// </summary>
    /// <param name="points">The points to choose from.</param>
    /// <param name="teleport">Puts the person on the point at once, such as when a block is joined late.</param>
    public TimbnNpcSteps GoToFreePoint(IReadOnlyList<string> points, bool teleport = false) =>
        Add($"go to a free point of {points.Count}", run => ClaimAndWalk(run, points, teleport));

    /// <summary>
    /// Claims a free point from one of the game's life simulator groups and walks there, the points read from the game
    /// when the step runs. When none is free the step does nothing.
    /// </summary>
    /// <param name="groupId">The group, from <see cref="TimbnGameIds.Groups"/>.</param>
    /// <param name="prefix">Keeps only the points whose id starts with this, such as the tavern seats.</param>
    /// <param name="teleport">Puts the person on the point at once, such as when a block is joined late.</param>
    public TimbnNpcSteps GoToFreePoint(string groupId, string? prefix = null, bool teleport = false) =>
        Add($"go to a free point in {groupId}", run =>
        {
            var points = TimbnNpcs.GroupPoints(groupId, prefix);
            if (points.Count > 0)
                return ClaimAndWalk(run, points, teleport);

            run.Npc.Log($"the game's {groupId} group has no {prefix ?? ""} points, staying put");
            return TimbnStepResult<TimbnNpcRun>.Next;
        });

    private static TimbnRoutineStep<TimbnNpcRun> NextTrip(Func<TimbnNpc, TimbnNpcTrip?, TimbnNpcTrip?> pick, TimbnNpcTrip finished) =>
        new("pick a next trip", run =>
        {
            var next = run.BlockDue ? null : Pick(run, pick, finished);
            if (next == null)
                return TimbnStepResult<TimbnNpcRun>.Insert(finished.ReturnSteps.Steps);

            run.Npc.Log("chains on to the next trip");
            return TimbnStepResult<TimbnNpcRun>.Insert([.. next.BodySteps.Steps, NextTrip(pick, next)]);
        });

    private static TimbnNpcTrip? Pick(TimbnNpcRun run, Func<TimbnNpc, TimbnNpcTrip?, TimbnNpcTrip?> pick, TimbnNpcTrip? finished)
    {
        TimbnNpcTrip? picked = null;
        run.Npc.RunSafely(() => picked = pick(run.Npc, finished), pick);
        return picked;
    }

    private static TimbnStepResult<TimbnNpcRun> ClaimAndWalk(TimbnNpcRun run, IReadOnlyList<string> points, bool teleport)
    {
        var npc = run.Npc;
        var free = points.Where(npc.IsFree).ToList();
        if (free.Count == 0)
        {
            // WhyBlocked scans the whole world per point, so only when the line is logged.
            if (npc.LogSteps)
                npc.Log($"no free point, staying put: {string.Join(", ", points.Select(id => $"{id} ({npc.BlockedReason(id)})"))}");

            return TimbnStepResult<TimbnNpcRun>.Next;
        }

        var point = free[run.Random.Next(free.Count)];
        npc.Claim(point);
        if (!teleport)
            return TimbnStepResult<TimbnNpcRun>.WalkThen(arrived => npc.WalkTo(point, TimbnWalk.Paths, arrived, failed: arrived));

        npc.Teleport(point);
        return TimbnStepResult<TimbnNpcRun>.Next;
    }

    private static TimbnStepResult<TimbnNpcRun> WalkStep(TimbnNpcRun run, string pointId, TimbnWalk walk) =>
        TimbnStepResult<TimbnNpcRun>.WalkThen(arrived => run.Npc.WalkTo(pointId, walk, arrived, failed: arrived));

    private TimbnNpcSteps Run(string text, Action<TimbnNpc> action) =>
        Add(text, run =>
        {
            run.Npc.RunSafely(() => action(run.Npc), action);
            return TimbnStepResult<TimbnNpcRun>.Next;
        });

    private TimbnNpcSteps Add(string text, Func<TimbnNpcRun, TimbnStepResult<TimbnNpcRun>> run)
    {
        Steps.Add(new TimbnRoutineStep<TimbnNpcRun>(text, run));
        return this;
    }
}

/// <summary>What a list of steps acts on, the person.</summary>
internal sealed class TimbnNpcRun
{
    internal TimbnNpcRun(TimbnNpc npc)
    {
        Npc = npc;
    }

    internal TimbnNpc Npc { get; }

    internal bool BlockDue { get; set; }

    internal System.Random Random { get; } = new();
}
