using Pathfinding;

namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Finds things in the loaded world, such as the walkable ground of a scene or the objects near a spot, and reads how
/// the world is doing, such as the graveyard, church and town scores, the town's gratitude and the weather. A mod
/// that uses the navigation graph needs its own reference to AstarPathfindingProject.dll from the game's Managed
/// folder.
/// </summary>
public static class TimbnWorld
{
    private static readonly DistanceMetric[] _metrics =
        [DistanceMetric.ClosestAsSeenFromAboveSoft(), DistanceMetric.ClosestAsSeenFromAbove()];

    // Checked against every door landing in 1.009.1. Basement holds the morgue, the mine and the home, vineyard, workyard
    // and warehouse basements, and every door into these areas passes the dungeons preset.
    private static readonly string[] _dungeonAreas = ["Basement", "TownSewer", "IntroScoutSewer", "Prison"];

    /// <summary>
    /// The scenes of the loaded world, each holding its world objects, zones, and the items on its ground. Empty at
    /// the main menu.
    /// </summary>
    public static IReadOnlyList<GameSceneData> Scenes =>
        TimbnGame.IsInGame ? MainGame.WorldData.gameSceneDataList : [];

    /// <summary>
    /// Every world object in the loaded save, in every scene, loaded around the player or not. World objects are
    /// everything placed in the world, such as chests, stations, graves, NPCs, and zombies. It walks every scene, so
    /// use <see cref="FindAll"/> when the definition id is known.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (var station in TimbnWorld.Objects(wgo => wgo.CraftComponent != null))
    ///     station.CraftComponent.ResetCraftsFromBalanceCache();
    /// </code>
    /// </example>
    /// <param name="match">Which objects to keep. Leave it out to keep all of them.</param>
    /// <returns>The matching objects, scene by scene. Empty at the main menu.</returns>
    public static IEnumerable<WgoData> Objects(Func<WgoData, bool>? match = null)
    {
        foreach (var scene in Scenes)
        {
            foreach (var wgo in scene.wgoDataList)
            {
                if (wgo != null && (match?.Invoke(wgo) ?? true))
                    yield return wgo;
            }
        }
    }

    /// <summary>
    /// Every world object of one definition in the loaded save, such as every sawhorse, in every scene, read from
    /// the game's own by id cache rather than by walking every scene.
    /// </summary>
    /// <example>
    /// <code>
    /// var hasSawhorse = TimbnWorld.FindAll("sawhorse").Count > 0;
    /// </code>
    /// </example>
    /// <param name="definitionId">The definition's id from the balance, such as sawhorse, or an NPC's id from <see cref="TimbnGameIds.Npcs"/>.</param>
    /// <returns>A copy of the list, so objects can be removed while looping it. Empty when there are none or at the main menu.</returns>
    public static IReadOnlyList<WgoData> FindAll(string definitionId) =>
        TimbnGame.IsInGame ? [.. MainGame.WorldData.GetWgoDataList(definitionId)] : [];

    /// <summary>
    /// The world object with a unique id, the one each object keeps for its whole life, such as a chest a mod keeps a
    /// label for. Read from the game's own cache, so it is cheap, and it finds the object in any scene.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.Find(chestId) is { } chest)
    ///     TimbnWorld.ViewOf(chest)?.DrawWidgets();
    /// </code>
    /// </example>
    /// <param name="uniqueId">The object's unique id, its UniqueId.</param>
    /// <returns>The object, or null when it is gone or at the main menu.</returns>
    public static WgoData? Find(SGuid uniqueId) => TimbnGame.IsInGame ? MainGame.WorldData.GetWgoData(uniqueId) : null;

    /// <summary>
    /// The first world object of one definition in the loaded save, for a definition the world has only one of, such
    /// as the home's teleport milestone. Use <see cref="FindAll"/> when there can be more.
    /// </summary>
    /// <example>
    /// <code>
    /// var milestone = TimbnWorld.Find("teleport_milestone_6_home");
    /// </code>
    /// </example>
    /// <param name="definitionId">The definition's id from the balance, such as sawhorse.</param>
    /// <returns>The object, or null when there is none or at the main menu.</returns>
    public static WgoData? Find(string definitionId) => TimbnGame.IsInGame ? MainGame.WorldData.GetWgoData(definitionId) : null;

    /// <summary>
    /// The world objects attached to a station, such as the ones built into its slots or a tool rack built next to it,
    /// read from the links the game keeps on the station and saves with it.
    /// </summary>
    /// <example>
    /// <code>
    /// var stands = TimbnWorld.Extensions(machine).Where(wgo => wgo.id == "timbn_infusion_stand").ToList();
    /// </code>
    /// </example>
    /// <param name="station">The station.</param>
    /// <returns>A copy of the list. Empty when nothing is attached or at the main menu.</returns>
    public static IReadOnlyList<WgoData> Extensions(WgoData station) =>
        TimbnGame.IsInGame && station != null
            ? [.. station.AttachedWorkbenchExtensions.Select(guid => MainGame.WorldData.GetWgoData(guid)).Where(wgo => wgo != null)]
            : [];

    /// <summary>
    /// The id of the craft at the front of a station's queue, running or waiting to start, or null when it has none,
    /// for example to check whether a stand started with Crafts.Start is still on "timbn_infusion_stand_infusion".
    /// </summary>
    /// <param name="station">The station.</param>
    public static string? RunningCraft(WgoData station) => station.CraftComponent?.CurrentCraftElement?.CraftId;

    /// <summary>
    /// The zones a spot lies in, smallest first, read from the scene's data so it works for a scene that is not
    /// loaded around the player. Zones can nest, so a spot in a room is also in the building around it.
    /// </summary>
    /// <example>
    /// <code>
    /// var zone = TimbnWorld.ZonesAt(TimbnPlayer.Position).FirstOrDefault();
    /// </code>
    /// </example>
    /// <param name="position">The spot to look at.</param>
    /// <param name="scene">The scene to look in. Leave it out for the scene the player is in.</param>
    /// <returns>The zones containing the spot, smallest first. Empty when it is in none or at the main menu.</returns>
    public static List<WorldZoneData> ZonesAt(Vector3 position, GameSceneData? scene = null)
    {
        scene ??= TimbnPlayer.Scene;
        if (scene is null)
            return [];

        var point = new Vector2(position.x, position.z);
        return
        [
            .. scene.worldZones
                .Where(zone => zone != null && zone.wholeZoneRect.Contains(point))
                .OrderBy(zone => zone.wholeZoneRect.width * zone.wholeZoneRect.height),
        ];
    }

    /// <summary>
    /// Gets the navigation graph of a scene, which holds its walkable ground. The game calculates walking paths
    /// over it.
    /// </summary>
    /// <param name="sceneId">The scene's id, or null for the scene the player is in.</param>
    /// <param name="graph">The scene's graph, or null when there is none.</param>
    /// <returns>True when the scene has a graph.</returns>
    public static bool TryGetNavGraph(string? sceneId, out NavGraph graph)
    {
        graph = null!;
        sceneId ??= TimbnPlayer.SceneId;
        if (string.IsNullOrEmpty(sceneId))
            return false;

        var helper = GraphHelper.Instance;
        if (helper == null || helper.SceneGraphsData == null || Pathfinding is not { } pathfinding)
            return false;

        var indices = helper.SceneGraphsData.GetRecastGraphIndexByWorldId(sceneId);
        var graphs = pathfinding.data?.graphs;
        if (indices == null || indices.Count == 0 || graphs == null || indices[0] < 0 || indices[0] >= graphs.Length)
            return false;

        graph = graphs[indices[0]];
        return graph != null;
    }

    /// <summary>The graveyard's quality score, the one the save slot and the graveyard's own info show, or 0 at the main menu.</summary>
    /// <example>
    /// <code>
    /// var visitors = Mathf.RoundToInt(TimbnWorld.GraveyardQuality / 25f);
    /// </code>
    /// </example>
    public static float GraveyardQuality => ZoneQuality("graveyard");

    /// <summary>
    /// The church's quality score, the one the sermon window shows by the cross, or 0 at the main menu. A sermon draws
    /// no more parishioners than this.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.ChurchQuality >= 100f)
    ///     Logger.LogInfo("The church is looking fine.");
    /// </code>
    /// </example>
    public static float ChurchQuality => ZoneQuality("church");

    /// <summary>
    /// The graveyard's ground as seen from above, one rectangle of x and z for each graveyard module built, the area
    /// whose graves count toward <see cref="GraveyardQuality"/>. It grows as the graveyard does. Empty at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// var inGraveyard = TimbnWorld.GraveyardBuiltAreas.Any(area => area.Contains(new Vector2(spot.x, spot.z)));
    /// </code>
    /// </example>
    public static IReadOnlyList<Rect> GraveyardBuiltAreas =>
        TimbnGame.IsInGame ? [.. MainGame.Instance.GameSave.worldData.GetWorldZoneDataById("graveyard")?.CustomQualityZonesRectList ?? []] : [];

    /// <summary>
    /// The whole graveyard as seen from above, its paths and grass as well as the ground built on, as one rectangle of
    /// x and z. It stays the same however much is built. Empty at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// var bounds = TimbnWorld.GraveyardBounds;
    /// var spot = new Vector3(Random.Range(bounds.xMin, bounds.xMax), 0f, Random.Range(bounds.yMin, bounds.yMax));
    /// </code>
    /// </example>
    public static Rect GraveyardBounds =>
        TimbnGame.IsInGame ? MainGame.Instance.GameSave.worldData.GetWorldZoneDataById("graveyard")?.wholeZoneRect ?? default : default;

    /// <summary>The town's quality score, the number by the Town label and the most gratitude the town can have, or 0 at the main menu.</summary>
    /// <example>
    /// <code>
    /// var full = TimbnWorld.TownGratitude >= TimbnWorld.TownQuality;
    /// </code>
    /// </example>
    public static int TownQuality => TimbnGame.IsInGame ? MainGame.Instance.GameSave.townSystem?.Quality ?? 0 : 0;

    /// <summary>
    /// The town's gratitude, which selling to town vendors earns, or 0 at the main menu. A sermon draws as
    /// many parishioners as this or <see cref="ChurchQuality"/>, whichever is lower. Gains stop at
    /// <see cref="TownQuality"/>, but it is not lowered when the town's quality drops.
    /// </summary>
    /// <example>
    /// <code>
    /// var visitors = TimbnWorld.TownGratitude / 5;
    /// </code>
    /// </example>
    public static int TownGratitude => TimbnGame.IsInGame ? MainGame.PlayerData.GetResInt("happiness") : 0;

    /// <summary>
    /// The weather right now, by the game's own name for it, such as CleanWeather, RainDayWeak or RainDayStorm, or empty
    /// at the main menu.
    /// </summary>
    /// <example>
    /// <code>
    /// var stormy = TimbnWorld.Weather is "RainDayStorm" or "RainDayThunder";
    /// </code>
    /// </example>
    public static string Weather => TimbnGame.IsInGame ? MainGame.Instance.GameSave.weatherData?.stateName ?? "" : "";

    /// <summary>Whether it is raining, in any of the game's rain weathers from a drizzle to a thunderstorm.</summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.IsRaining)
    ///     Logger.LogInfo("Bring an umbrella.");
    /// </code>
    /// </example>
    public static bool IsRaining => Weather.StartsWith("Rain", StringComparison.Ordinal);

    /// <summary>
    /// Finds the nearest walkable ground to a spot in the player's scene, measured flat as seen from above, so a
    /// spot on a roof or under the floor still finds the ground below or above it.
    /// </summary>
    /// <example>
    /// <code>
    /// if (TimbnWorld.TryFindWalkable(TimbnPlayer.Position, 5f, out var ground))
    ///     TimbnPlayer.TeleportTo(ground);
    /// </code>
    /// </example>
    /// <param name="point">The spot to search from.</param>
    /// <param name="range">How far to look, in world units.</param>
    /// <param name="ground">The nearest walkable point, or <paramref name="point"/> when none was found.</param>
    /// <returns>True when walkable ground was found within <paramref name="range"/>.</returns>
    public static bool TryFindWalkable(Vector3 point, float range, out Vector3 ground)
    {
        ground = point;
        if (!TimbnGame.IsInGame || !TryGetNavGraph(null, out var graph))
            return false;

        foreach (var metric in _metrics)
        {
            var constraint = NearestNodeConstraint.Walkable;
            constraint.distanceMetric = metric;
            constraint.maxDistanceSqr = range * range;
            var nearest = graph.GetNearest(point, constraint);
            if (nearest.node != null && FlatDistance(point, nearest.position) <= range)
            {
                ground = nearest.position;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Every grave in the graveyard with a body buried in it, whether or not it has a stone or a fence. Open graves and
    /// graves left empty after an exhumation are not counted.
    /// </summary>
    /// <example>
    /// <code>
    /// var graves = TimbnWorld.GravesWithBodies().ToList();
    /// npc.Face(graves[0].Position);
    /// </code>
    /// </example>
    /// <returns>The graves. Empty at the main menu or when nobody is buried yet.</returns>
    public static IEnumerable<WgoData> GravesWithBodies() =>
        Objects(wgo => wgo.id == "grave_ground" && wgo.Inventory?.GetItemsByGroupId("body").Count > 0);

    /// <summary>
    /// Lists the world objects in the player's scene within a flat distance of a spot, nearest first. World objects
    /// are everything placed in the world, such as chests, stations, graves, NPCs, and zombies.
    /// </summary>
    /// <example>
    /// <code>
    /// var chests = TimbnWorld.Near(TimbnPlayer.Position, 5f, wgo => wgo.Inventory != null);
    /// </code>
    /// </example>
    /// <param name="position">The spot to measure from.</param>
    /// <param name="radius">How far to look, in world units, measured flat as seen from above.</param>
    /// <param name="match">Which objects to keep. Leave it out to keep all of them.</param>
    /// <returns>The matching objects, nearest first. Empty at the main menu.</returns>
    public static List<WgoData> Near(Vector3 position, float radius, Func<WgoData, bool>? match = null)
    {
        if (TimbnPlayer.Scene is not { } scene)
            return [];

        return
        [
            .. scene.wgoDataList
                .Where(wgo => wgo != null && FlatDistance(position, wgo.Position) <= radius && (match?.Invoke(wgo) ?? true))
                .OrderBy(wgo => FlatDistance(position, wgo.Position)),
        ];
    }

    /// <summary>
    /// The view of a world object, the GameObject that draws it, or null when its chunk is not loaded around the
    /// player. The view is what the game's windows and effects work with, such as opening a station's window.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (var zombie in TimbnZombies.All())
    ///     TimbnWorld.ViewOf(zombie)?.DrawWidgets();
    /// </code>
    /// </example>
    /// <param name="data">The world object.</param>
    /// <returns>Its view, or null when it has none right now.</returns>
    public static Wgo? ViewOf(WgoData data)
    {
        var view = GameScene.GetWgoViewGlobal(data.UniqueId);
        return view ? view : null;
    }

    /// <summary>
    /// A point the game placed in a scene, the spots its people walk to and stand at, such as donkey_04 on the road by
    /// the graveyard. A point has a position, a facing and an enabled flag, and its id is the same string the Npcs
    /// module takes. The ids are listed in info_dump's points catalog.
    /// </summary>
    /// <example>
    /// <code>
    /// var road = TimbnWorld.Point("donkey_04")?.Position;
    /// </code>
    /// </example>
    /// <param name="id">The point's id.</param>
    /// <returns>The point, or null when no point has that id or at the main menu.</returns>
    public static GDPointData? Point(string id) =>
        // The game's single lookup logs a warning for an id it lacks, and its list lookup asks quietly.
        TimbnGame.IsInGame ? MainGame.WorldData.gdPointsData.GetGDPointsDataById(id).FirstOrDefault() : null;

    /// <summary>
    /// The lighting and weather preset a spot is in, the one the game's doors set as the player lands there. It is
    /// outdoor, indoor (the tavern and the other rooms, where no rain shows), dungeons (the basements, the sewers and the
    /// prison) or church.
    /// </summary>
    /// <example>
    /// <code>
    /// var inside = TimbnWorld.EnvironmentAt(TimbnPlayer.Position) != "outdoor";
    /// </code>
    /// </example>
    /// <param name="position">The spot.</param>
    /// <returns>The preset's name, outdoor at the main menu.</returns>
    public static string EnvironmentAt(Vector3 position)
    {
        if (!TimbnGame.IsInGame)
            return "outdoor";

        var areas = MainGame.Instance.gameSceneConfigs?.Where(config => config != null).SelectMany(config => config.IndoorAreas) ?? [];
        IndoorAreaData? inside = null;
        var smallest = float.PositiveInfinity;
        foreach (var area in areas)
        {
            if (area == null || !area.ContainsXZ(position))
                continue;

            var size = area.GetXZArea();
            if (size >= smallest)
                continue;

            smallest = size;
            inside = area;
        }

        if (inside == null)
            return "outdoor";

        if (inside.id == "Church")
            return "church";

        return _dungeonAreas.Contains(inside.id) ? "dungeons" : "indoor";
    }

    /// <summary>
    /// Every point carrying a custom tag, for a set of spots the game marks the same way, such as the two seats each
    /// church pew adds with the tag parishioner_bench_point. Disabled points are included, so check Enabled when that
    /// matters.
    /// </summary>
    /// <example>
    /// <code>
    /// var seats = TimbnWorld.PointsWithTag("parishioner_bench_point").Where(seat => seat.Enabled);
    /// </code>
    /// </example>
    /// <param name="customTag">The tag, as the game spells it.</param>
    /// <returns>The points, empty when none carry the tag or at the main menu.</returns>
    public static GDPointData[] PointsWithTag(string customTag) =>
        TimbnGame.IsInGame ? [.. MainGame.WorldData.gdPointsData.GetGDPointsDataByCustomTag(customTag) ?? []] : [];

    /// <summary>The distance between two spots as seen from above, ignoring height.</summary>
    /// <param name="first">One spot.</param>
    /// <param name="second">The other spot.</param>
    /// <returns>The flat distance in world units.</returns>
    public static float FlatDistance(Vector3 first, Vector3 second) =>
        new Vector2(first.x - second.x, first.z - second.z).magnitude;

    internal static AstarPath? Pathfinding => (object)AstarPath.active is UnityEngine.Object pathfinding && pathfinding != null ? AstarPath.active : null;

    private static float ZoneQuality(string zoneId) =>
        TimbnGame.IsInGame ? MainGame.Instance.GameSave.worldData.GetWorldZoneDataById(zoneId)?.GetTotalQuality() ?? 0f : 0f;
}
