namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Reads the game's own lists of places people go, so a mod never writes a list of point ids, and what the game keeps
/// about a person, such as their reputation resource. To move someone, use the plugin's Npcs property.
/// </summary>
public static class TimbnNpcs
{
    internal const string SpawnTag = "timbn_spawned_npc";

    internal const string LifeSimulatorHold = "npc_life_sim_disabled_flag";

    internal const float TakenDistance = 1f;

    private static readonly List<Action> _nextUpdate = [];

    private static readonly AccessTools.FieldRef<NPCPointOfInterestData, SGuid> _occupiedBy =
        AccessTools.FieldRefAccess<NPCPointOfInterestData, SGuid>("occupiedBy");

    private static readonly List<TimbnNpc> _spawned = [];

    private static TimbnPointClaims<WgoData> _claims = NewClaims();

    internal static TimbnPointClaims<WgoData> Claims => _claims;

    /// <summary>
    /// The points of one of the game's life simulator groups, the places it sends villagers to, such as the tavern
    /// seats in the village group.
    /// </summary>
    /// <example>
    /// <code>
    /// var seats = TimbnNpcs.GroupPoints(TimbnGameIds.Groups.VillagersDayAll, "npc_sim_village_tavern_");
    /// </code>
    /// </example>
    /// <param name="groupId">The group's id, from <see cref="TimbnGameIds.Groups"/>.</param>
    /// <param name="prefix">Keeps only the points whose id starts with this. Leave it out for every point.</param>
    /// <returns>The point ids in id order, empty when the group is missing or at the main menu.</returns>
    public static IReadOnlyList<string> GroupPoints(string groupId, string? prefix = null)
    {
        if (!TimbnGame.IsInGame)
            return [];

        var points = MainGame.Instance.GameSave.npcLifeSimulatorData.GetGroupById(groupId)?.Configuration?.AllPonts;
        return points?
            .Where(id => prefix == null || id.StartsWith(prefix, StringComparison.Ordinal))
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList() ?? [];
    }

    /// <summary>
    /// The kinds of people the game itself placed in one of its life simulator groups, one entry per person, such as
    /// the villagers of the village day group. People a plugin added with Npcs.Spawn are left out.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (var kind in TimbnNpcs.GroupMemberIds(TimbnGameIds.Groups.VillagersDayAll))
    ///     Npcs.AddVillager(kind);
    /// </code>
    /// </example>
    /// <param name="groupId">The group's id, from <see cref="TimbnGameIds.Groups"/>.</param>
    /// <returns>Definition ids in id order, empty when the group is missing or empty, or at the main menu.</returns>
    public static IReadOnlyList<string> GroupMemberIds(string groupId)
    {
        if (!TimbnGame.IsInGame || MainGame.Instance.GameSave.npcLifeSimulatorData.GetGroupById(groupId) is not { } group)
            return [];

        return group.Wgos
            .Select(uid => MainGame.WorldData.GetWgoData(uid))
            .Where(data => data != null && data.CustomTag != SpawnTag)
            .Select(data => data!.MovableObjectId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The way a person faces when they stop on a point, such as Left for a seat at the end of a table, or null when
    /// the point does not exist or at the main menu. Use it to pick who goes to a point or what they play there.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnNpcs.FacingAt("npc_sim_village_tavern_06") == Direction.Left)
    ///     kind = TimbnGameIds.Npcs.Fisherman01;
    /// </code>
    /// </example>
    /// <param name="pointId">The point's id.</param>
    /// <returns>The point's facing, or null.</returns>
    public static Direction? FacingAt(string pointId) => Point(pointId)?.Direction;

    /// <summary>
    /// Whether a point is free for anyone, meaning it exists, is switched on, nobody claimed it and nobody stands on it.
    /// Use it to pick a point before you know who goes there. A person's own IsFree also counts their own claim as free.
    /// </summary>
    /// <param name="pointId">The point's id.</param>
    /// <returns>False when the point is taken, missing or switched off, and at the main menu.</returns>
    public static bool IsFree(string pointId) => TimbnGame.IsInGame && WhyBlocked(pointId, null) is null;

    /// <summary>
    /// The id of the player's resource that holds their reputation with an NPC, such as rep_larry for Larry, or null
    /// for someone the game keeps no reputation with. Read it with <see cref="TimbnPlayer.GetResource"/> to gate on
    /// reputation outside a quest, where TimbnNeeds.Reputation does it for you.
    /// </summary>
    /// <example>
    /// <code>
    /// var liked = TimbnNpcs.ReputationOf(TimbnGameIds.Npcs.Larry) is { } rep &amp;&amp; TimbnPlayer.GetResource(rep) &gt;= 20f;
    /// </code>
    /// </example>
    /// <param name="npcId">The NPC, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>The resource id, or null when the NPC has no reputation or the balance is not loaded.</returns>
    public static string? ReputationOf(string npcId) =>
        GameBalance.Me?.GetDataOrNull<WGODef>(npcId)?.repResName is { Length: > 0 } reputation ? reputation : null;

    internal static WgoData? FindData(string? definitionId) =>
        TimbnGame.IsInGame && definitionId is not null
            ? MainGame.WorldData.GetWgoDataList(definitionId).FirstOrDefault(data => data != null && data.CustomTag != SpawnTag)
            : null;

    internal static IEnumerable<WgoData> Walkers() =>
        TimbnGame.IsInGame
            ? MainGame.WorldData.Cache.wgoDataByUidCache.Values.Where(data => data.MovementComponent != null)
            : [];

    internal static GDPointData? Point(string pointId) =>
        TimbnGame.IsInGame ? MainGame.WorldData.gdPointsData.GetGDPointDataById(pointId) : null;

    internal static GDPointData? NearestPoint(Vector3 position)
    {
        if (!TimbnGame.IsInGame || TimbnWorld.Pathfinding is not { } pathfinding || pathfinding.data.FindGraphOfType(typeof(GDPointGraph)) is not { } graph)
            return null;

        return graph.GetNearest(position).node is GDPointNode node ? node.GdPointData : null;
    }

    internal static List<string>? Route(Vector3 from, string toPointId, out float length) =>
        RoutePoints(from, toPointId, out length)?.Select(point => point.Id).ToList();

    internal static List<GDPointData>? RoutePoints(Vector3 from, string toPointId, out float length)
    {
        length = 0f;
        if (NearestPoint(from) is not { } start || Point(toPointId) is not { } target)
            return null;

        if ((IsOnGraph(target) ? target : NearestPoint(target.Position)) is not { } goal)
            return null;

        var comingIn = ComingIn(MainGame.WorldData.gdPointsData);
        return TimbnGraphSearch.Route(start, goal, point => Links(point, comingIn), (a, b) => TimbnWorld.FlatDistance(a.Position, b.Position), out length);
    }

    internal static bool IsFree(string pointId, WgoData? npc = null) => WhyBlocked(pointId, npc) is null;

    internal static string? WhyBlocked(string pointId, WgoData? npc = null)
    {
        var point = Point(pointId);
        var scheduled = point == null ? null : SchedulePoint(pointId);
        return _claims.WhyBlocked(
            pointId,
            npc,
            exists: point != null,
            enabled: scheduled == null || scheduled.Enabled,
            takenInGame: IsTakenInGame(scheduled, npc),
            standingThere: point == null ? null : WhoIsStandingOn(point.Position, npc));
    }

    internal static bool IsTakenInGame(string pointId, WgoData? npc) => IsTakenInGame(SchedulePoint(pointId), npc);

    internal static string? WhoIsStandingOn(Vector3 position, WgoData? npc = null)
    {
        if (!TimbnGame.IsInGame)
            return null;

        if (TimbnWorld.FlatDistance(TimbnPlayer.Position, position) < TakenDistance)
            return "the player";

        var other = Walkers().FirstOrDefault(data => (npc == null || data.UniqueId != npc.UniqueId) && !data.IsHidden && TimbnWorld.FlatDistance(data.Position, position) < TakenDistance);
        return other == null ? null : $"{other.MovableObjectId} at ({other.Position.x:0.0}, {other.Position.z:0.0})";
    }

    internal static bool? HasClipFor(WgoData npc, string trigger)
    {
        if (TimbnWorld.ViewOf(npc) is not { } view)
            return null;

        var prefix = trigger.EndsWith("_enter", StringComparison.Ordinal) ? trigger[..^"_enter".Length]
            : trigger.EndsWith("_exit", StringComparison.Ordinal) ? trigger[..^"_exit".Length]
            : trigger;
        List<KeyValuePair<AnimationClip, AnimationClip>> pairs = [];
        var loaded = false;
        var unloaded = false;
        foreach (var animator in view.GetComponentsInChildren<Animator>(true))
        {
            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                unloaded |= animator.GetComponent<AssetReferenceAnimator>() != null;
                continue;
            }

            loaded = true;
            if (controller is not AnimatorOverrideController overrides)
                continue;

            pairs.Clear();
            overrides.GetOverrides(pairs);
            if (pairs.Any(pair => pair.Value != null && pair.Key != null && pair.Key.name.StartsWith(prefix, StringComparison.Ordinal)))
                return true;
        }

        return unloaded || !loaded ? null : false;
    }

    internal static bool IsGroupFull(string groupId)
    {
        var simulator = MainGame.Instance.GameSave.npcLifeSimulatorData;
        if (simulator.GetGroupById(groupId) is not { } group || group.Configuration?.AllPonts is not { } points)
            return true;

        var enabled = points.Distinct().Count(id => simulator.GetPointById(id)?.Enabled == true);
        var living = group.Wgos.Count(uid => MainGame.WorldData.GetWgoData(uid) is not { } data || !IsVisitor(data));
        return living >= enabled;
    }

    internal static void RemoveSpawned(WgoData data)
    {
        var simulator = MainGame.Instance.GameSave.npcLifeSimulatorData;
        simulator.TryRemoveActionData(data.UniqueId);
        simulator.TryRemoveAnimationData(data.UniqueId);
        MainGame.WorldData.RemoveWgoDataFromGameScene(data);
    }

    internal static void JoinGroupDay(WgoData npc)
    {
        var simulator = MainGame.Instance.GameSave.npcLifeSimulatorData;
        if (simulator.GetGroupByWGOId(npc.UniqueId) is not { } group)
            return;

        var groupIsOut = group.Wgos.Any(uid => uid != npc.UniqueId && MainGame.WorldData.GetWgoData(uid) is { } member && !string.IsNullOrEmpty(member.occupiedPointOfInterest));
        if (groupIsOut)
            MainGame.Instance.npcLifeSimulator.RollActivityForWgo(npc.UniqueId, group, 0f);
    }

    internal static void TrackSpawned(TimbnNpc npc) => _spawned.Add(npc);

    internal static void ForgetSpawned(TimbnNpc npc) => _spawned.Remove(npc);

    internal static IDisposable RemoveSpawnedForSave()
    {
        List<SavedSpawn> removed = [];
        foreach (var npc in _spawned.ToList())
        {
            if (npc.IsGone)
                continue;

            TimbnSafe.Run(() =>
            {
                var data = npc.Data;
                removed.Add(new SavedSpawn(npc, data, _claims.PointOf(data), data.GetGameRes(LifeSimulatorHold) > 0));
                _claims.Release(data);
                RemoveSpawned(data);
            }, $"{nameof(TimbnNpcs)}.{nameof(RemoveSpawnedForSave)}|{npc.Id}");
        }

        return new TimbnUndo(() =>
        {
            foreach (var saved in removed)
            {
                if (!saved.Npc.IsDisposed)
                    TimbnSafe.Run(() => PutBack(saved), $"{nameof(TimbnNpcs)}.{nameof(RemoveSpawnedForSave)}|{saved.Kind}");
            }
        });
    }

    internal static IDisposable ReleaseClaimsForSave()
    {
        List<KeyValuePair<string, WgoData>> held = [.. _claims.Held];
        foreach (var claim in held)
            TimbnSafe.Run(() => SchedulePoint(claim.Key)?.Deoccupy(), $"{nameof(TimbnNpcs)}.{nameof(ReleaseClaimsForSave)}|{claim.Key}");

        return new TimbnUndo(() =>
        {
            foreach (var claim in held)
            {
                if (_claims.HolderOf(claim.Key) == claim.Value)
                    TimbnSafe.Run(() => SchedulePoint(claim.Key)?.Occupy(claim.Value.UniqueId), $"{nameof(TimbnNpcs)}.{nameof(ReleaseClaimsForSave)}|{claim.Key}");
            }
        });
    }

    internal static void Tick()
    {
        TimbnSafe.Run(TimbnNpcTalk.Tick, $"{nameof(TimbnNpcs)}|{nameof(TimbnNpcTalk)}");
        TimbnSafe.Run(TimbnGameMoves.Tick, $"{nameof(TimbnNpcs)}|{nameof(TimbnGameMoves)}");
        TimbnSafe.Run(TimbnNpcCollision.Tick, $"{nameof(TimbnNpcs)}|{nameof(TimbnNpcCollision)}");
        if (_nextUpdate.Count == 0)
            return;

        var due = _nextUpdate.ToList();
        _nextUpdate.Clear();
        foreach (var action in due)
            TimbnSafe.Run(action, $"{nameof(TimbnNpcs)}|{TimbnSafe.Describe(action)}");
    }

    internal static void OnNextUpdate(Action action) => _nextUpdate.Add(action);

    internal static void OnGameStarted()
    {
        _claims = NewClaims();
        TimbnBaseGamePlaces.Reset();
        TimbnGameMoves.Reset();
        foreach (var leftover in TimbnWorld.Objects(data => data.CustomTag == SpawnTag).ToList())
            RemoveSpawned(leftover);
    }

    internal static void OnLeftGame()
    {
        _nextUpdate.Clear();
        _claims = NewClaims();
        TimbnNpc.ForgetWalkers();
        TimbnBaseGamePlaces.Reset();
        TimbnGameMoves.Reset();
        TimbnNpcTalk.Reset();
        TimbnWalkPause.Reset();
        TimbnNpcCollision.Reset();
    }

    internal static int ReleaseLeftoverClaims(WgoData npc)
    {
        var released = 0;
        var mine = _claims.PointOf(npc);
        foreach (var point in MainGame.Instance.GameSave.npcLifeSimulatorData.AllPoints)
        {
            if (point.Id != mine && point.IsOccupied && _occupiedBy(point) == npc.UniqueId)
            {
                point.Deoccupy();
                released++;
            }
        }

        return released;
    }

    private static void PutBack(SavedSpawn saved)
    {
        if (!MainGame.WorldData.AddWgoData(saved.Kind, saved.Position, saved.World, SpawnTag, out var data) || data == null)
            return;

        data.MovableDirection = saved.Direction;
        if (saved.Held)
            data.SetGameRes(LifeSimulatorHold, 1);
        else
            CarryOnLife(data, saved);

        TimbnNpcCommand.Moved(saved.Id, data.UniqueId);
        if (saved.Claim != null)
            _claims.Claim(saved.Claim, data);

        saved.Npc.Rebind(data);
    }

    private static void CarryOnLife(WgoData data, SavedSpawn saved)
    {
        var simulator = MainGame.Instance.GameSave.npcLifeSimulatorData;
        if (saved.LifeSpot != null)
        {
            if (simulator.GetPointById(saved.LifeSpot) is not { IsOccupied: false } point)
            {
                JoinGroupDay(data);
                return;
            }

            data.occupiedPointOfInterest = saved.LifeSpot;
            point.Occupy(data.UniqueId);
        }

        if (saved.LifeAction is { } action)
            simulator.AddActionData(new NPCLifeSimulatorActionData(data.UniqueId, action.Remaining, action.Type));
        else if (saved.Walking)
            simulator.AddActionData(new NPCLifeSimulatorActionData(data.UniqueId, 0f, saved.LifeSpot != null ? NPCLifeSimulatorActionType.GoToPointOfInterest : NPCLifeSimulatorActionType.GoHome));

        if (saved.LifeSpot != null && !saved.Walking && saved.LifeAction?.Type != NPCLifeSimulatorActionType.GoToPointOfInterest)
            MainGame.Instance.npcLifeSimulator.OnWgoReachedPointOfInterest(data);
    }

    private static TimbnPointClaims<WgoData> NewClaims() =>
        new((point, owner) => SchedulePoint(point)?.Occupy(owner.UniqueId), point => SchedulePoint(point)?.Deoccupy());

    private static NPCPointOfInterestData? SchedulePoint(string point) =>
        MainGame.Instance != null ? MainGame.Instance.GameSave?.npcLifeSimulatorData?.GetPointById(point) : null;

    private static bool IsTakenInGame(NPCPointOfInterestData? scheduled, WgoData? npc) =>
        scheduled != null && scheduled.IsOccupied && (npc == null || _occupiedBy(scheduled) != npc.UniqueId);

    private static bool IsVisitor(WgoData data) => data.CustomTag == SpawnTag && data.GetGameRes(LifeSimulatorHold) > 0;

    private static bool IsOnGraph(GDPointData point) => point.Enabled && point.Node != null;

    private static IEnumerable<GDPointData> Links(GDPointData point, Dictionary<GDPointData, List<GDPointData>> comingIn)
    {
        foreach (var next in point.NextPointData)
        {
            if (IsOnGraph(next))
                yield return next;
        }

        if (comingIn.TryGetValue(point, out var before))
        {
            foreach (var back in before)
            {
                if (IsRoad(back, point))
                    yield return back;
            }
        }
    }

    private static bool IsRoad(GDPointData from, GDPointData to) =>
        !from.IsTransitPoint && !to.IsTransitPoint && string.IsNullOrEmpty(from.GameSceneDataIdToTransit) && from.GameSceneDataId == to.GameSceneDataId;

    private static Dictionary<GDPointData, List<GDPointData>> ComingIn(GdPointsData points)
    {
        Dictionary<GDPointData, List<GDPointData>> comingIn = [];
        foreach (var point in points.Points)
        {
            if (!IsOnGraph(point))
                continue;

            foreach (var next in point.NextPointData)
            {
                if (!IsOnGraph(next))
                    continue;

                if (!comingIn.TryGetValue(next, out var list))
                    comingIn[next] = list = [];

                list.Add(point);
            }
        }

        return comingIn;
    }

    private sealed class SavedSpawn
    {
        internal SavedSpawn(TimbnNpc npc, WgoData data, string? claim, bool held)
        {
            Npc = npc;
            Id = data.UniqueId;
            Kind = data.MovableObjectId;
            Position = data.Position;
            Direction = data.MovableDirection;
            World = data.WorldId;
            Claim = claim;
            Held = held;
            Walking = data.MovementComponent is { IsMoving: true };
            LifeSpot = string.IsNullOrEmpty(data.occupiedPointOfInterest) ? null : data.occupiedPointOfInterest;
            if (MainGame.Instance.GameSave.npcLifeSimulatorData.ActionsData.FirstOrDefault(action => action.WgoId == data.UniqueId) is { } next)
                LifeAction = (next.ActionType, next.RemainingTimeToAction);
        }

        internal TimbnNpc Npc { get; }

        internal SGuid Id { get; }

        internal string Kind { get; }

        internal Vector3 Position { get; }

        internal Vector2 Direction { get; }

        internal string World { get; }

        internal string? Claim { get; }

        internal bool Held { get; }

        internal bool Walking { get; }

        internal string? LifeSpot { get; }

        internal (NPCLifeSimulatorActionType Type, float Remaining)? LifeAction { get; }
    }
}
