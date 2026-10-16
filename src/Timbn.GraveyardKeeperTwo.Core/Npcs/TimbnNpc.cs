using LazyBearTechnology;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// A person in the loaded save that a plugin moves, from the plugin's Npcs.Find or Npcs.Spawn. Every method does
/// nothing and returns false once the person is gone. When the handle is disposed, the plugin unloads or the player
/// returns to the main menu, a walk it started is stopped, its claim is let go, and a person Npcs.Spawn added is removed.
/// </summary>
public sealed class TimbnNpc : IDisposable
{
    internal const float VillagerSpeed = LazyConsts.MovementComponent.NPC_SIM_SPEED_DEFAULT;

    private const float _clipCheckSeconds = 0.1f;
    private const int _clipLoadTicks = 5;
    private const string _busyResource = "busy";
    private const string _onSpotResource = "on_spot";

    private static readonly HashSet<TimbnNpc> _animating = [];

    // The handle whose walk each person is on, so another handle letting go of them does not put them back mid walk.
    private static readonly Dictionary<WgoData, TimbnNpc> _walkedBy = [];

    private static readonly System.Reflection.FieldInfo? _speechBubbles = AccessTools.Field(typeof(UISpeechBubble), "activeBubbles");

    private static readonly AccessTools.FieldRef<UISpeechBubble, bool>? _canSkip =
        TimbnReflection.TryField<UISpeechBubble, bool>("canSkipBubble");

    private static int _walks;

    private readonly TimbnFrameworkPlugin _owner;
    private readonly bool _spawned;
    private readonly Action<TimbnNpc> _onDisposed;
    private int _walk;
    private Action? _finish;
    private int _arrival;
    private string? _arrivedAt;
    private Vector3 _arrivedPosition;
    private bool _animated;
    private string? _waitingTrigger;
    private string? _playing;
    private Func<bool>? _resume;
    private int _loadedTicks;
    private IDisposable? _waitingForLoad;
    private IDisposable? _scenePause;
    private bool _disposed;
    private bool _collidesWithPlayer = true;
    private string? _claimedHere;

    internal TimbnNpc(TimbnFrameworkPlugin owner, WgoData data, bool spawned, Action<TimbnNpc> onDisposed)
    {
        _owner = owner;
        Data = data;
        _spawned = spawned;
        _onDisposed = onDisposed;
    }

    /// <summary>The person's world object.</summary>
    public WgoData Data { get; private set; }

    /// <summary>
    /// Whether the person blocks the keeper like a wall. Set it to false for a crowd the keeper should be able to walk
    /// through, such as visitors who stand about the graveyard. It only changes the person's solid parts toward the
    /// keeper's body, so they are still talked to, hit and walked around by everyone else, and it carries over when the
    /// person streams out and back in. Leave it alone for someone the keeper has to be able to push against.
    /// </summary>
    /// <example>
    /// <code>
    /// visitor.CollidesWithPlayer = false;
    /// </code>
    /// </example>
    public bool CollidesWithPlayer
    {
        get => _collidesWithPlayer;
        set
        {
            _collidesWithPlayer = value;
            TimbnNpcCollision.Set(this, value);
        }
    }

    /// <summary>The person's definition id, such as npc_larry, the one <see cref="TimbnGameIds.Npcs"/> lists. Handy for a log line.</summary>
    public string Id => Data.MovableObjectId;

    /// <summary>The id of the player resource that holds the player's reputation with this NPC, or null for an NPC without one.</summary>
    public string? ReputationId => TimbnNpcs.ReputationOf(Id);

    /// <summary>Where the person stands.</summary>
    public Vector3 Position => Data.Position;

    /// <summary>Whether the person is no longer in the loaded world, after a dispose, a removal or the save closing.</summary>
    public bool IsGone => _disposed || !TimbnGame.IsInGame || MainGame.WorldData.GetWgoData(Data.UniqueId) != Data;

    /// <summary>Whether the person is on a walk.</summary>
    public bool IsWalking => !IsGone && Data.MovementComponent is { IsMoving: true };

    /// <summary>
    /// Whether a run started with <see cref="RunSteps"/> on this person, by any plugin, is still playing. A run the game
    /// takes over, by firing one of the person's events or moving them in a story scene, ends without its done callback,
    /// so a plugin that keeps a list of the people it spawned checks this to notice one the game left standing.
    /// </summary>
    public bool IsRunning => !IsGone && TimbnNpcCommand.IsRunning(Data);

    /// <summary>
    /// How many game minutes are left before the next block of their schedule starts, or 24 hours when the day has
    /// one block. It is 0 for a person no schedule drives. Use it to leave a long trip alone when the block is about
    /// to change.
    /// </summary>
    public double MinutesLeftInBlock => NextBlockStart is { } next ? TimbnDayPlan.MinutesUntil(next, TimbnClock.TimeOfDay) : 0;

    internal float? NextBlockStart { get; set; }

    internal bool IsSpawned => _spawned;

    internal bool IsDisposed => _disposed;

    internal float Speed { get; set; } = VillagerSpeed;

    internal string? Origin { get; set; }

    internal Func<bool>? LogWhen { get; set; }

    internal bool LogSteps => LogWhen?.Invoke() == true;

    internal IReadOnlyList<TimbnNpcDoorway> Doorways { get; set; } = [];

    internal string? ClaimedPoint => TimbnNpcs.Claims.PointOf(Data);

    internal bool PointTaken { get; private set; }

    /// <summary>
    /// Walks the person to a point and turns them the way it faces. A walk along the paths the paths do not reach is
    /// skipped, or tried again from the day's origin when the person has a schedule.
    /// </summary>
    /// <example>
    /// <code>
    /// herm.WalkTo("npc_sim_village_right_side_road_34", TimbnWalk.Navmesh, () => herm.WalkTo("npc_sim_village_right_side_03"));
    /// </code>
    /// </example>
    /// <param name="pointId">The id of the point to walk to, such as npc_sim_village_right_side_03.</param>
    /// <param name="walk">Along the game's paths, or over the navmesh.</param>
    /// <param name="arrived">Runs once on arrival. It does not run when the walk is stopped or replaced, or cannot reach the point.</param>
    /// <param name="failed">Runs once when the walk started but could not reach the point, so a chain of walks can go on or give up. It does not run when the walk is stopped or replaced.</param>
    /// <returns>False when the point is missing, the paths do not reach it, or the person is gone.</returns>
    public bool WalkTo(string pointId, TimbnWalk walk = TimbnWalk.Paths, Action? arrived = null, Action? failed = null) =>
        WalkTo(pointId, walk, arrived, failed, fromOrigin: false);

    internal bool WalkTo(string pointId, TimbnWalk walk, Action? arrived, Action? failed, bool fromOrigin = false)
    {
        if (IsGone)
            return false;

        if (TimbnWorld.Point(pointId) is not { } target)
        {
            Log($"no point {pointId}, not walking");
            return false;
        }

        Func<bool>? fromOriginAgain = fromOrigin ? null : () => WalkTo(pointId, walk, arrived, failed, fromOrigin: true);
        if (DoorwayOut(pointId) is { } leaving)
        {
            if (leaving.Via == pointId)
                return Leg(target, leaving.Walk, arrived, failed, fromOriginAgain);

            Log($"leaves {leaving.Point} by {leaving.Via}");
            return Leg(leaving.Via, leaving.Walk, () => Then(WalkTo(pointId, walk, arrived, failed, fromOrigin), failed), failed, fromOriginAgain);
        }

        if (DoorwayIn(pointId) is { } coming)
        {
            if (!IsOn(coming.Via))
            {
                Log($"comes to {pointId} by {coming.Via}");
                return Leg(coming.Via, walk, () => Then(Leg(target, coming.Walk, arrived, failed, fromOriginAgain), failed), failed, fromOriginAgain);
            }

            walk = coming.Walk;
        }

        return Leg(target, walk, arrived, failed, fromOriginAgain);
    }

    /// <summary>Ends the walk the person is on, whoever started it, and leaves them standing where they are.</summary>
    public void StopWalking()
    {
        if (IsGone)
            return;

        if (IsOwnWalkUnderWay())
            _arrivedAt = null;

        _walk = 0;
        _finish = null;
        _arrival = 0;
        DropScenePause();
        CancelWaitingPlay();
        var movement = Data.MovementComponent;
        if (movement == null)
            return;

        movement.IsPaused = false;
        if (movement.IsMoving)
            movement.ForceStop();
    }

    /// <summary>Puts the person on a point at once, facing the way it faces, and ends any walk.</summary>
    /// <param name="pointId">The id of the point, such as npc_sim_village_right_side_03.</param>
    /// <returns>False when the point is missing or the person is gone.</returns>
    public bool Teleport(string pointId)
    {
        if (IsGone || TimbnWorld.Point(pointId) is not { } point)
            return false;

        KeepBaseGamePlace();
        StopWalking();
        Data.Position = point.Position;
        if (point.Direction != Direction.None)
            Data.MovableDirection = point.Direction.ConvertToVector2XZ();

        Log($"teleported to {pointId}");
        return true;
    }

    /// <summary>
    /// Puts the person on a spot at once, keeping the way they face, and ends any walk. It takes a person through a door
    /// only the keeper can use, such as from the church's outside landing tp_RT_church_enter to its inside one
    /// tp_RT_church_exit, which are world objects and not points.
    /// </summary>
    /// <example>
    /// <code>
    /// npc.Teleport(TimbnWorld.FindAll("tp_RT_church_exit")[0].Position);
    /// </code>
    /// </example>
    /// <param name="position">Where to put them, height included.</param>
    /// <returns>False when the person is gone.</returns>
    public bool Teleport(Vector3 position)
    {
        if (IsGone)
            return false;

        KeepBaseGamePlace();
        StopWalking();
        Data.Position = position;
        Log($"teleported to ({position.x:0.00}, {position.z:0.00})");
        return true;
    }

    /// <summary>
    /// Rolls the person's dice for this game minute against a chance, such as whether to set off on a trip. The same
    /// minute rolls the same on a reload, and each person added with Npcs.Spawn rolls apart from the others of their
    /// kind. A miss is written to the log with the roll, so a skipped trip explains itself.
    /// </summary>
    /// <example>
    /// <code>
    /// .Decide(npc => npc.RollChance(0.75f) is { } random ? Trips[random.Next(Trips.Length)] : null)
    /// </code>
    /// </example>
    /// <param name="chance">The roll passes when it is this or lower, from 0 to 1.</param>
    /// <param name="what">What the roll is for, named in the log, such as "a trip".</param>
    /// <returns>The dice to keep choosing with, or null on a miss.</returns>
    public System.Random? RollChance(float chance, string what = "a trip") => RollChance(chance, out _, what);

    /// <summary>The same roll as <see cref="RollChance(float, string)"/>, also handing back what was rolled for a log line of your own.</summary>
    /// <param name="chance">The roll passes when it is this or lower, from 0 to 1.</param>
    /// <param name="rolled">What was rolled.</param>
    /// <param name="what">What the roll is for, named in the log, such as "a trip".</param>
    /// <returns>The dice to keep choosing with, or null on a miss.</returns>
    public System.Random? RollChance(float chance, out double rolled, string what = "a trip")
    {
        var random = TimbnRolls.ThisMinute(_spawned ? Data.UniqueId.ToString() : Id);
        rolled = random.NextDouble();
        if (rolled <= chance)
            return random;

        Log($"skipped, rolled {rolled:0.00} and {what} needs {chance:0.00} or less");
        return null;
    }

    /// <summary>Turns the person to face a direction.</summary>
    public void Face(Direction direction)
    {
        if (IsGone)
            return;

        KeepBaseGamePlace();
        Data.MovableDirection = direction.ConvertToVector2XZ();
    }

    /// <summary>
    /// Turns the person toward a spot, such as a grave or another person, at whatever angle it lies. Does nothing when
    /// they already stand on it.
    /// </summary>
    /// <example>
    /// <code>
    /// npc.Face(grave.Position);
    /// </code>
    /// </example>
    /// <param name="point">The point to look at. Its height is ignored.</param>
    public void Face(Vector3 point)
    {
        var toward = new Vector2(point.x - Position.x, point.z - Position.z);
        if (IsGone || toward.sqrMagnitude < 0.0001f)
            return;

        KeepBaseGamePlace();
        Data.MovableDirection = toward.normalized;
    }

    /// <summary>
    /// Says a line in the game's own speech bubble over the person, the one they talk in, which follows them as they
    /// walk and fades by itself after a few seconds while they mumble in their voice. It takes no control and the
    /// player cannot skip it, so it never eats a key press. A new line replaces the one they are saying.
    /// </summary>
    /// <example>
    /// <code>
    /// customer.Npc.Say("my_mod_complaint");
    /// </code>
    /// </example>
    /// <param name="text">A text key added with Text.Add or a lang file, or the text itself. Square brackets are the game's color markup.</param>
    /// <param name="seconds">How long the line stays once written out. Leave it at 0 for the game's time for its length.</param>
    public void Say(string text, float seconds = 0f)
    {
        if (IsGone)
            return;

        Bubble.Talk(new PhraseData(isPlayer: false, Data, text, () => { }, null, fixedShowTimeValue: seconds));
        if (_speechBubbles?.GetValue(null) is Dictionary<int, UISpeechBubble> bubbles && bubbles.TryGetValue(Data.GetHashCode(), out var bubble) && bubble != null && _canSkip != null)
            _canSkip(bubble) = false;

        Log($"says {text}");
    }

    /// <summary>
    /// Fires an event of the person's own game script, such as Herm's go_home. Story events start real story and
    /// trade, so only fire ones known to move or animate. The game's graphs only act while the person's busy and
    /// on_spot resources are 0, which a stopped walk or a teleport leaves wrong, so both are set to 0 first.
    /// </summary>
    /// <param name="eventName">The event, from <see cref="TimbnGameIds.NpcEvents"/>.</param>
    public void FireEvent(string eventName)
    {
        if (IsGone)
            return;

        ClearScriptFlag(_busyResource);
        ClearScriptFlag(_onSpotResource);
        Log($"firing the {eventName} event");
        KeepBaseGamePlace();
        TimbnGameMoves.OurEvent(Data);
        TimbnGameMoves.Ours(() => Data.FireEvent(eventName));
    }

    /// <summary>
    /// Plays an animation trigger, but only one the person has a clip of their own for, since any other makes them
    /// vanish. It keeps playing until their next walk. While they are far from the player the game unloads their
    /// animations, so the trigger waits and plays once they load, unless they walk on first.
    /// </summary>
    /// <param name="trigger">The animation, from <see cref="TimbnGameIds.NpcAnimations"/> such as <c>TimbnGameIds.NpcAnimations.Herm.AdjustingJabbot</c>.</param>
    /// <returns>False when the person has no clip for it and nothing was played.</returns>
    public bool Animate(string trigger)
    {
        if (IsGone)
            return false;

        CancelWaitingPlay();
        switch (TimbnNpcs.HasClipFor(Data, trigger))
        {
            case true:
                Trigger(trigger);
                Log($"playing {trigger}");
                return true;
            case false:
                Log($"no clip of their own for {trigger}, not playing it");
                return false;
            default:
                _waitingTrigger = trigger;
                _loadedTicks = 0;
                _waitingForLoad = _owner.Events.EveryRealSeconds(_clipCheckSeconds, PlayOnceLoaded);
                Log($"{trigger} waits until their animations load");
                return true;
        }
    }

    /// <summary>
    /// Plays a list of steps on command, such as for a quest or a cutscene, and calls back when the last one is done.
    /// A new run on the same person replaces the one before. It starts at once, even in the middle of a conversation
    /// with them. The person still stops when the player starts talking to them later in the run, but not for a story
    /// scene, since a cutscene is when you want them moving. When the game itself moves them, through a story walk, a
    /// teleport or a script event, the run ends and leaves them to it. A schedule of theirs steps aside while it plays
    /// and takes them back as soon as it ends.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.Find(TimbnGameIds.Npcs.Larry)?.RunSteps(new TimbnNpcSteps()
    ///     .Walk("RT_larry_spawn")
    ///     .WaitRealSeconds(3)
    ///     .WalkNavmesh("npc_sim_village_right_side_road_34")
    ///     .Face(Direction.Down), done: OnLarryArrived);
    /// </code>
    /// </example>
    /// <param name="steps">What to do, in order.</param>
    /// <param name="done">Runs once after the last step. It does not run when the run is stopped or replaced, the game takes the person, the save closes or the plugin unloads.</param>
    /// <returns>The run, which stops it when disposed, or null when the person is gone.</returns>
    public IDisposable? RunSteps(TimbnNpcSteps steps, Action? done = null)
    {
        if (IsGone)
            return null;

        var command = new TimbnNpcCommand(this, _owner, steps, done);
        command.Owned = _owner.SessionSubscriptions.Add(command);
        return command.Owned;
    }

    /// <summary>Whether a point is free for this person, meaning it exists, is switched on, nobody else claimed it and nobody stands on it.</summary>
    public bool IsFree(string pointId) => TimbnNpcs.IsFree(pointId, Data);

    /// <summary>Why a point cannot be used by this person, such as "occupied by the player", or null when it is free.</summary>
    public string? BlockedReason(string pointId) => TimbnNpcs.WhyBlocked(pointId, Data);

    /// <summary>
    /// Claims a point so no other schedule, and none of the game's villagers, are sent to it, letting go of the point
    /// claimed before. A schedule lets go of it when the trip that claimed it ends, and a run lets go of a point claimed
    /// during it when it ends. A point claimed before a run, such as a seat the person walks to, stays claimed after it
    /// until they claim another, the handle is disposed or they are gone.
    /// </summary>
    /// <returns>False when someone else holds a claim on it, or one of the game's villagers is at it or on the way there.</returns>
    public bool Claim(string pointId)
    {
        if (IsGone)
            return false;

        PointTaken = false;
        var heldByMod = TimbnNpcs.Claims.HolderOf(pointId) is { } holder && holder != Data;
        var claimed = TimbnNpcs.Claims.Claim(pointId, Data, TimbnNpcs.IsTakenInGame(pointId, Data));
        if (claimed)
            _claimedHere = pointId;

        Log(claimed ? $"claimed {pointId}"
            : heldByMod ? $"could not claim {pointId}, someone else holds it"
            : $"could not claim {pointId}, the game's life simulator gave it to someone else");
        return claimed;
    }

    /// <summary>Stops what this handle started and lets go of its claim, and removes a person Npcs.Spawn added.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (!IsGone)
        {
            StopOwnWalk();
            if (_animated)
                ResetAnimation();

            if (_spawned)
                TimbnNpcs.RemoveSpawned(Data);
        }

        CancelWaitingPlay();
        TimbnNpcCollision.Forget(this);

        // Claims are kept per person, so only let go of one this handle made, not another plugin's.
        if (_claimedHere != null && ClaimedPoint == _claimedHere)
            TimbnNpcs.Claims.Release(Data);

        if (_spawned)
            TimbnNpcs.ForgetSpawned(this);
        else
            PutBackIfFree();

        ForgetWalk();
        _animating.Remove(this);
        _disposed = true;
        _onDisposed(this);
    }

    internal static void ForgetWalkers() => _walkedBy.Clear();

    private bool IsWalkedByAnother() =>
        _walkedBy.TryGetValue(Data, out var walker) && walker != this && !walker._disposed && walker.IsOwnWalkUnderWay();

    private void ForgetWalk()
    {
        if (!IsGone && _walkedBy.TryGetValue(Data, out var walker) && walker == this)
            _walkedBy.Remove(Data);
    }

    internal static IDisposable ClearAnimationsForSave()
    {
        List<(WgoData Data, string Trigger)> cleared = [];
        foreach (var npc in _animating)
        {
            if (npc.IsGone || npc._playing is not { } trigger)
                continue;

            cleared.Add((npc.Data, trigger));
            npc.Data.SetCustomAnimationTrigger();
        }

        return new TimbnUndo(() =>
        {
            foreach (var (data, trigger) in cleared)
                data.SetCustomAnimationTrigger(trigger);
        });
    }

    internal void PutBackIfFree(TimbnNpcSchedule? ending = null)
    {
        if (!TimbnGame.IsInGame || IsGone || _spawned || TimbnNpcSchedule.IsScheduled(Data, ending) || TimbnNpcCommand.IsRunning(Data) || IsWalkedByAnother())
            return;

        StopOwnWalk();
        CancelWaitingPlay();
        if (_animated)
            ResetAnimation();

        if (TimbnBaseGamePlaces.PutBack(Data))
            Log("nothing drives them any more, put back where the base game has them");
    }

    internal void StopOwnWalk()
    {
        if (IsGone)
            return;

        if (IsOwnWalkUnderWay())
        {
            StopWalking();
            return;
        }

        _walk = 0;
        _finish = null;
        _arrival = 0;
        DropScenePause();
    }

    internal void ReleaseClaim()
    {
        PointTaken = false;
        if (ClaimedPoint is { } point)
        {
            TimbnNpcs.Claims.Release(Data);
            Log($"released {point}");
        }
    }

    internal void PauseForScene()
    {
        if (_scenePause != null || IsGone || Data.MovementComponent is not { IsMoving: true })
            return;

        _scenePause = TimbnWalkPause.Hold(Data);
        Log("stopped for the story scene");
    }

    internal void ResumeAfterScene()
    {
        if (_scenePause == null)
            return;

        DropScenePause();
        Log("the story scene ended");
    }

    internal void LetGo(bool gameWalks)
    {
        var ours = IsOwnWalkUnderWay();
        _walk = 0;
        _finish = null;
        _arrival = 0;
        _arrivedAt = null;
        CancelWaitingPlay();
        DropScenePause();
        if (IsGone || Data.MovementComponent is not { } movement)
            return;

        if (ours && !gameWalks && movement.IsMoving)
            movement.ForceStop();

        if (_animated)
            ResetAnimation();
    }

    internal void Rebind(WgoData data)
    {
        var resume = _walk != 0 ? _resume : null;
        var trigger = _animated ? _playing : _waitingTrigger;
        DropScenePause();
        ForgetWalk();
        _arrivedAt = null;
        Data = data;
        _walk = 0;
        _finish = null;
        _animated = false;
        _animating.Remove(this);
        CancelWaitingPlay();
        if (resume != null)
        {
            Log("walks on after the save");
            resume();
        }
        else if (trigger != null)
        {
            Animate(trigger);
        }
    }

    /// <summary>
    /// Whether they stand on the point, from what put them there rather than from distance. A walk of this handle
    /// that ended on it counts as long as they have not moved since, and so does the life simulator having them on it
    /// or at home on it, once they stand still.
    /// </summary>
    internal bool IsOn(string pointId)
    {
        if (IsGone || Data.IsHidden)
            return false;

        if (_arrivedAt == pointId && Position == _arrivedPosition)
            return true;

        if (TimbnWalkState.IsUnderWay(Data.MovementComponent) == true)
            return false;

        var lifeSpot = Data.occupiedPointOfInterest;
        if (!string.IsNullOrEmpty(lifeSpot))
            return lifeSpot == pointId;

        // With no spot taken they are home, unless the simulator is still sending them there.
        var next = MainGame.Instance.GameSave.npcLifeSimulatorData.ActionsData.FirstOrDefault(action => action.WgoId == Data.UniqueId);
        return Data.Definition?.npcLifeSimHome == pointId && next?.ActionType != NPCLifeSimulatorActionType.GoHome;
    }

    internal bool WalkToSpot(float x, float z, Action? arrived, Action? failed = null, bool straight = false)
    {
        if (IsGone)
            return false;

        if (DoorwayOut(null) is { } leaving)
        {
            Log($"leaves {leaving.Point} by {leaving.Via}");
            return Leg(leaving.Via, leaving.Walk, () => Then(WalkToSpot(x, z, arrived, failed, straight), failed), failed, null);
        }

        return StartWalk(new Vector3(x, Position.y, z), Data.WorldId, straight ? MovementType.Direct : MovementType.Recast, $"({x:0.00}, {z:0.00})", null, arrived, failed);
    }

    // One movement through the corners, which the game's waypoint mover follows without stopping at any of them, so
    // the person ends within the game's 0.1 slack of the last corner rather than exactly on it.
    internal bool WalkThroughSpots(IReadOnlyList<Vector3> corners, Action? arrived, Action? failed = null)
    {
        if (IsGone || corners.Count == 0)
            return false;

        if (DoorwayOut(null) is { } leaving)
        {
            Log($"leaves {leaving.Point} by {leaving.Via}");
            return Leg(leaving.Via, leaving.Walk, () => Then(WalkThroughSpots(corners, arrived, failed), failed), failed, null);
        }

        var last = corners[^1];
        return StartWalk(new Vector3(last.x, Position.y, last.z), Data.WorldId, MovementType.Recast, $"({last.x:0.00}, {last.z:0.00})", null, arrived, failed, corners: corners);
    }

    internal bool RunSafely(Action action, Delegate code) =>
        TimbnSafe.Run(action, _owner.PluginLogger, $"{nameof(TimbnNpcSteps)}|{TimbnSafe.Describe(code)}");

    internal void Log(string line)
    {
        if (LogSteps)
            _owner.PluginLogger.LogDebug($"day {TimbnClock.Day} {TimbnClock.TimeText} {Id}: {line}");
    }

    private void ClearScriptFlag(string resource)
    {
        var value = Data.GetGameRes(resource);
        if (value == 0)
            return;

        Data.SetGameRes(resource, 0);
        Log($"{resource} was {value}, set to 0");
    }

    private TimbnNpcDoorway? DoorwayOut(string? targetId) =>
        Doorways.FirstOrDefault(doorway => doorway.Point != targetId && IsOn(doorway.Point));

    private TimbnNpcDoorway? DoorwayIn(string targetId) =>
        Doorways.FirstOrDefault(doorway => doorway.Point == targetId && !IsOn(doorway.Point));

    private void KeepBaseGamePlace()
    {
        if (!_spawned)
            TimbnBaseGamePlaces.Remember(Data);
    }

    private static void Then(bool started, Action? failed)
    {
        if (!started)
            failed?.Invoke();
    }

    private bool Leg(string pointId, TimbnWalk walk, Action then, Action? failed, Func<bool>? fromOriginAgain)
    {
        if (TimbnWorld.Point(pointId) is { } point)
            return Leg(point, walk, then, failed, fromOriginAgain);

        Log($"no point {pointId} to pass through, not walking");
        return false;
    }

    private bool Leg(GDPointData target, TimbnWalk walk, Action? arrived, Action? failed, Func<bool>? fromOriginAgain)
    {
        var type = walk switch
        {
            TimbnWalk.Navmesh => MovementType.Recast,
            TimbnWalk.Straight => MovementType.Direct,
            _ => MovementType.GDGraph,
        };
        var route = walk == TimbnWalk.Paths ? TimbnNpcs.RoutePoints(Position, target.Id, out _) : null;
        if (walk == TimbnWalk.Paths && route is null)
        {
            if (fromOriginAgain != null && Origin is { } origin && Teleport(origin))
            {
                Log($"no path to {target.Id}, put back at {origin} to try from there");
                return fromOriginAgain();
            }

            Log($"no path to {target.Id} ({TimbnWorld.FlatDistance(Position, target.Position):0} units away), skipping it");
            return false;
        }

        return StartWalk(target.Position, target.GameSceneDataId, type, target.Id, target, arrived, failed, route);
    }

    private bool StartWalk(Vector3 position, string destinationWorldId, MovementType type, string label, GDPointData? target, Action? arrived, Action? failed, List<GDPointData>? route = null, IReadOnlyList<Vector3>? corners = null)
    {
        var movement = Data.MovementComponent;
        if (movement == null)
            return false;

        KeepBaseGamePlace();
        StopWalking();
        _arrivedAt = null;
        PointTaken = false;
        if (_animated)
            ResetAnimation();

        var walkId = _walk = ++_walks;
        _resume = route != null && target != null
            ? () => Leg(target, TimbnWalk.Paths, arrived, failed, null)
            : () => StartWalk(position, destinationWorldId, type, label, target, arrived, failed, corners: corners);
        var starting = true;
        var finishedWhileStarting = false;
        void Finish()
        {
            TimbnPlannedRoute.Forget(movement);
            if (starting)
                finishedWhileStarting = true;
            else
                End(movement.Completion == MovementComponent.CompletionState.Fail);
        }

        void End(bool unreachable)
        {
            if (walkId != _walk || IsGone)
                return;

            _walk = 0;
            _finish = null;
            if (unreachable)
            {
                Log($"could not reach {label}, the walk ends there");
                HandOn(walkId, failed);
                return;
            }

            if (target != null)
            {
                // A point with no facing converts to a zero direction, which shows as the sprite's default facing for
                // the frames before the next walk turns them, so they keep facing the way they walked in.
                if (target.Direction != Direction.None)
                    Data.MovableDirection = target.Direction.ConvertToVector2XZ();

                _arrivedAt = target.Id;
                _arrivedPosition = Position;
                CheckArrival(target);
            }

            HandOn(walkId, arrived);
        }

        Action finish = Finish;
        _finish = finish;
        _walkedBy[Data] = this;
        MovementComponent.StartPathResult Start() => movement.StartPath(position, Data.WorldId, destinationWorldId, type, speed: Speed, onFinish: finish);
        var planned = false;
        var result = TimbnGameMoves.Ours(() =>
            route != null ? TimbnPlannedRoute.Walk(movement, route, Start, out planned)
            : corners != null ? TimbnPlannedRoute.WalkCorners(movement, corners, Start, out planned)
            : Start());
        starting = false;
        var kind = corners != null ? "straight" : type switch { MovementType.Recast => "navmesh", MovementType.Direct => "straight", _ => "paths" };
        var by = planned ? $" by {route?.Count ?? corners!.Count} {(corners != null ? "corners" : "points")}" : "";
        Log($"{kind} walk to {label}{by}, {result}");
        if (result == MovementComponent.StartPathResult.IncorrectMovementType)
        {
            if (walkId == _walk)
            {
                _walk = 0;
                _finish = null;
            }

            return false;
        }

        if (finishedWhileStarting)
            End(result != MovementComponent.StartPathResult.AlreadyAtDestinationPoint && movement.Completion == MovementComponent.CompletionState.Fail);

        return true;
    }

    private void HandOn(int walkId, Action? callback)
    {
        if (callback == null)
            return;

        _arrival = walkId;
        TimbnNpcs.OnNextUpdate(() =>
        {
            if (_arrival != walkId || _walk != 0 || IsGone)
                return;

            _arrival = 0;
            TimbnSafe.Run(callback, _owner.PluginLogger, $"{nameof(TimbnNpc)}|{TimbnSafe.Describe(callback)}");
        });
    }

    private bool IsOwnWalkUnderWay() =>
        _walk != 0 && _finish != null && Data.MovementComponent is { } movement && TimbnWalkState.IsWalkFinishing(movement, _finish) != false;

    private void PlayOnceLoaded()
    {
        if (IsGone || _waitingTrigger is not { } trigger)
        {
            CancelWaitingPlay();
            return;
        }

        var hasClip = TimbnNpcs.HasClipFor(Data, trigger);
        if (hasClip == true)
        {
            CancelWaitingPlay();
            Trigger(trigger);
            Log($"animations loaded, playing {trigger}");
            return;
        }

        if (hasClip == null)
        {
            _loadedTicks = 0;
            return;
        }

        if (++_loadedTicks < _clipLoadTicks)
            return;

        CancelWaitingPlay();
        Log($"no clip of their own for {trigger}, not playing it");
    }

    private void CancelWaitingPlay()
    {
        _waitingTrigger = null;
        _waitingForLoad?.Dispose();
        _waitingForLoad = null;
    }

    private void Trigger(string trigger)
    {
        Data.SetTriggerToAnimator(trigger);
        Data.SetCustomAnimationTrigger(trigger);
        _playing = trigger;
        _animated = true;
        _animating.Add(this);
    }

    private void ResetAnimation()
    {
        _animated = false;
        _animating.Remove(this);
        Data.SetCustomAnimationTrigger();
        Data.SetTriggerToAnimator(AnimationComponentBase.RESET_TO_IDLE_TRIGGER);
    }

    private void DropScenePause()
    {
        var pause = _scenePause;
        _scenePause = null;
        pause?.Dispose();
    }

    private void CheckArrival(GDPointData point)
    {
        if (PointTaken || ClaimedPoint != point.Id)
            return;

        if (TimbnNpcs.WhoIsStandingOn(point.Position, Data) is { } who)
        {
            PointTaken = true;
            Log($"{point.Id} was taken while walking there ({who})");
        }
    }
}
