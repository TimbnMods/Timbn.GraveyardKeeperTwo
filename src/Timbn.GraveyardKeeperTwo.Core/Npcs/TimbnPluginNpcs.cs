namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Gives people a day on the game clock, moves them and adds new ones for a mod. Everything is owned by the mod. When
/// it unloads, walks it started stop, claims are let go and added people are removed. People, groups, events and
/// animations come from the generated <see cref="TimbnGameIds.Npcs"/>, <see cref="TimbnGameIds.Groups"/>,
/// <see cref="TimbnGameIds.NpcEvents"/> and <see cref="TimbnGameIds.NpcAnimations"/>. Points are named by their id.
/// Reach it through the mod's Npcs property.
/// </summary>
public sealed class TimbnPluginNpcs
{
    private readonly TimbnFrameworkPlugin _owner;
    private readonly Dictionary<TimbnNpc, IDisposable> _handles = [];

    internal TimbnPluginNpcs(TimbnFrameworkPlugin owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// Gives a person a day on the game clock that follows every save the player loads. Set it up once in OnAwake.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.Schedule(TimbnGameIds.Npcs.Herm, HermsDay());
    /// </code>
    /// </example>
    /// <param name="npcId">The person, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <param name="day">The day to follow.</param>
    /// <returns>The schedule, for its status. Dispose it to stop early. It also stops when the mod unloads.</returns>
    public TimbnNpcSchedule Schedule(string npcId, TimbnNpcDay day)
    {
        var schedule = new TimbnNpcSchedule(this, _owner, npcId, day);
        _owner.Subscriptions.Add(schedule);
        return schedule;
    }

    /// <summary>
    /// Gets a handle to move a person in the loaded save outside a schedule, such as in a quest. Asking again in the
    /// same save gives the same handle. People added with Spawn are left out, so use the handle Spawn gave.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.Find(TimbnGameIds.Npcs.Herm)?.WalkTo("npc_sim_village_right_side_03");
    /// </code>
    /// </example>
    /// <param name="npcId">The person, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>The handle, or null when they are not in the loaded world or at the main menu.</returns>
    public TimbnNpc? Find(string npcId)
    {
        if (TimbnNpcs.FindData(npcId) is not { } data)
            return null;

        return _handles.Keys.FirstOrDefault(handle => handle.Data == data && !handle.IsGone) ?? Track(new TimbnNpc(_owner, data, spawned: false, Forget));
    }

    /// <summary>
    /// Adds a person to the loaded save at a point, such as one more villager. The game's life simulator leaves them
    /// alone, so walk them yourself. They are removed again when the handle is disposed, the mod unloads or the player
    /// returns to the main menu. They are left out of every save the game writes.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.Spawn(TimbnGameIds.Npcs.Villager03, "npc_sim_village_left_side_road_01")?.WalkTo("npc_sim_village_tavern_03");
    /// </code>
    /// </example>
    /// <param name="npcId">The kind of person, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <param name="pointId">The id of the point to put them at, such as npc_sim_village_left_side_road_01.</param>
    /// <returns>The handle, or null when the point or the person is missing or at the main menu.</returns>
    public TimbnNpc? Spawn(string npcId, string pointId)
    {
        if (TimbnWorld.Point(pointId) is not { } point)
            return null;

        if (!MainGame.WorldData.AddWgoData(npcId, point.Position, point.GameSceneDataId, TimbnNpcs.SpawnTag, out var data) || data == null)
        {
            _owner.PluginLogger.LogWarning($"Could not add {npcId} at {pointId}.");
            return null;
        }

        data.SetGameRes(TimbnNpcs.LifeSimulatorHold, 1);
        if (point.Direction != Direction.None)
            data.MovableDirection = point.Direction.ConvertToVector2XZ();
        return Track(new TimbnNpc(_owner, data, spawned: true, Forget));
    }

    /// <summary>
    /// Loads the look of some kinds of people in the background. That way the first Spawn of each kind in a session
    /// does not stall a frame while the game loads it. Call it when a save is ready, with every kind the mod may
    /// spawn. Copies counts the ones the game already shows. With copies at 1, a kind that is already out in the
    /// village gets nothing new ready. Spawning a second one then still stalls. Ask for 2 when the game's own may be
    /// about.
    /// </summary>
    /// <example>
    /// <code>
    /// Events.SaveReady(() => Npcs.Preload(TimbnNpcs.GroupMemberIds(TimbnGameIds.Groups.VillagersDayAll), copies: 2));
    /// </code>
    /// </example>
    /// <param name="npcIds">The kinds of people, from <see cref="TimbnGameIds.Npcs"/> or <see cref="TimbnNpcs.GroupMemberIds"/>.</param>
    /// <param name="copies">How many of each kind to have ready, counting the ones already shown.</param>
    public void Preload(IEnumerable<string> npcIds, int copies = 1) => TimbnNpcs.Preload(npcIds, copies, _owner.PluginLogger);

    /// <summary>
    /// Adds one more of a kind of villager, who lives exactly like the ones the game placed. They join the same life
    /// simulator group, go out to its points by day and home at night. They start at their home and go straight out
    /// when the rest of their group is out. They are removed when the handle is disposed, the mod unloads or the
    /// player returns to the main menu. They are left out of every save the game writes.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.AddVillager(TimbnGameIds.Npcs.Villager03);
    /// </code>
    /// </example>
    /// <param name="npcId">The kind of villager, from <see cref="TimbnGameIds.Npcs"/> or <see cref="TimbnNpcs.GroupMemberIds"/>.</param>
    /// <returns>The handle, or null when the kind has no group or home, the group has no point left for one more, or at the main menu.</returns>
    public TimbnNpc? AddVillager(string npcId)
    {
        if (!TimbnGame.IsInGame)
            return null;

        if (GameBalance.Me.GetDataOrNull<WGODef>(npcId) is not { } definition || string.IsNullOrEmpty(definition.npcLifeSimGroup)
            || TimbnWorld.Point(definition.npcLifeSimHome) is not { } home)
        {
            _owner.PluginLogger.LogWarning($"{npcId} has no life simulator group or home, not adding one.");
            return null;
        }

        if (TimbnNpcs.IsGroupFull(definition.npcLifeSimGroup))
            return null;

        if (!MainGame.WorldData.AddWgoData(npcId, home.Position, home.GameSceneDataId, TimbnNpcs.SpawnTag, out var data) || data == null)
        {
            _owner.PluginLogger.LogWarning($"Could not add {npcId} at its home {definition.npcLifeSimHome}.");
            return null;
        }

        TimbnNpcs.JoinGroupDay(data);
        return Track(new TimbnNpc(_owner, data, spawned: true, Forget));
    }

    /// <summary>
    /// Keeps a vendor's trade window shut while a check says they are away, with the vendor saying a line in its
    /// place, such as outside the hours of their schedule.
    /// </summary>
    /// <example>
    /// <code>
    /// Npcs.CloseTradeWhen(TimbnGameIds.Npcs.Herm, () => TimbnClock.Weekday == TimbnWeekday.Pride, "my_mod_herm_resting");
    /// </code>
    /// </example>
    /// <param name="vendorId">The vendor, from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <param name="isClosed">True while the window should stay shut.</param>
    /// <param name="lineKey">A text key added with Text.Add, said by the vendor in their speech bubble, or null for none.</param>
    /// <returns>A handle that lets trade open again when disposed. It is also removed when the mod unloads.</returns>
    public IDisposable CloseTradeWhen(string vendorId, Func<bool> isClosed, string? lineKey = null) =>
        _owner.Subscriptions.Add(TimbnVendorGates.Add(vendorId, isClosed, lineKey, _owner.PluginLogger));

    private TimbnNpc Track(TimbnNpc npc)
    {
        if (npc.IsSpawned)
            TimbnNpcs.TrackSpawned(npc);

        _handles[npc] = _owner.SessionSubscriptions.Add(npc);
        return npc;
    }

    private void Forget(TimbnNpc npc)
    {
        if (_handles.Remove(npc, out var owned))
            owned.Dispose();
    }
}
