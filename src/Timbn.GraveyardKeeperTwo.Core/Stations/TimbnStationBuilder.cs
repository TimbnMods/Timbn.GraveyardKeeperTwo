namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a buildable workstation for Stations.Add. It holds the station's look, which game station it behaves
/// like and what it costs at the build desk. Core turns it into the five definitions the game expects. These are the
/// station, its construction site, the build desk entry and the two removals. Core also adds their text and sets
/// them up like the game's own woodworking bench. Only <see cref="Model(TimbnModel)"/> is required. A station with no
/// <see cref="Cost"/> is free. One with no <see cref="UnlockedBy"/> is on the build desk from the start.
/// </summary>
/// <example>
/// A smoking rack that works like the kitchen oven, built in the yard for 4 boards.
/// <code><![CDATA[
/// var model = Models.Build("SmokingRack", faces => faces.Block("smoking_rack", x: 0, y: 0, z: 0, depth: 30));
/// var rack = Stations.Add("timbn_smoking_rack", "Smoking Rack", "A plain rack for smoking fish.", station => station
///     .Model(model, model.Size)
///     .BasedOn("kitchen_oven")
///     .Icon("timbn_red_workbench")
///     .Cost(("flitch", 4))
///     .NoFuel()
///     .NoExtensions());
/// ]]></code>
/// </example>
public sealed class TimbnStationBuilder
{
    private readonly TimbnStationSides<TimbnModel> _sides = new();

    internal TimbnStationBuilder(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    internal string Id { get; }

    internal string Name { get; }

    internal string Description { get; }

    internal TimbnModel? StationModel => _sides.HasModel ? _sides.Resolve(false)[0].Model : null;

    internal TimbnModel? SiteModel => _sides.HasModel ? _sides.Resolve(false)[0].Site : null;

    internal Vector3 Size => _sides.HasModel ? _sides.Resolve(false)[0].Size : Vector3.zero;

    internal bool MirrorsSides { get; private set; }

    internal IReadOnlyList<TimbnSide<TimbnModel>> Sides => _sides.Resolve(MirrorsSides);

    internal float? DockGap { get; private set; }

    internal string? TalentId { get; private set; }

    internal bool? AllowsZombie { get; private set; }

    internal bool SkipsConstruction { get; private set; }

    internal bool HasZombieSpot { get; private set; }

    internal bool ZombieTakesOrders { get; private set; }

    internal bool HoldsZombieItem { get; private set; }

    internal int? BuildLimit { get; private set; }

    internal List<TimbnStationOverlay> Overlays { get; } = [];

    internal List<TimbnStationSlot> Slots { get; } = [];

    internal string? InSlotId { get; private set; }

    internal string? TabId { get; private set; }

    internal ItemType? ToolType { get; private set; }

    internal int? CraftSlotCount { get; private set; }

    internal int? StorageSlotCount { get; private set; }

    internal bool? IsAutomatic { get; private set; }

    internal int? AutoTickDuration { get; private set; }

    internal string? ActionSound { get; private set; }

    internal string TemplateId { get; private set; } = TimbnStationRules.DefaultStationTemplate;

    internal string IconId { get; private set; } = "";

    internal List<(string Item, int Count)> CostItems { get; } = [];

    internal List<(string Item, int Count)>? RefundItems { get; private set; }

    internal string BuildDeskId { get; private set; } = TimbnStationRules.DefaultBuildDesk;

    internal string? AreaId { get; private set; }

    internal bool ClearsFuel { get; private set; }

    internal List<string>? ExtensionIds { get; private set; }

    internal string? TechId { get; private set; }

    internal Action<WGODef>? EditStation { get; private set; }

    /// <summary>
    /// Sets how the finished station looks when it faces the camera. Every station needs one. Core gives it a box shaped
    /// footprint the size of the model and a solid body. It also gets an interaction area and a dock spot in front. The
    /// player works at the dock spot and finished items drop there. A station with only this side stays as it is placed.
    /// Add <see cref="Left(TimbnModel, TimbnModel)"/>, <see cref="Right(TimbnModel, TimbnModel)"/> or
    /// <see cref="Up(TimbnModel, TimbnModel)"/> to let the player turn it with R in build mode.
    /// </summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <exception cref="ArgumentOutOfRangeException">The model has no size on some axis.</exception>
    public TimbnStationBuilder Model(TimbnModel model) => Model(model, model.Size);

    /// <summary>
    /// The finished station's look when it faces the camera, with a footprint of the given size instead of the model's
    /// own. See <see cref="Model(TimbnModel)"/>.
    /// </summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="size">Width, height and depth in world units, for example (1.5, 0.8, 1) for a bench.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is zero or below on some axis.</exception>
    public TimbnStationBuilder Model(TimbnModel model, Vector3 size)
    {
        _sides.Set(TimbnFacing.Down, model, size);
        return this;
    }

    /// <summary>
    /// The station's look when it faces left, which the player gets by turning it with R while placing it. It has its
    /// own footprint from the model, and its dock spot is on the left edge. The station is still one building with one
    /// id, so crafts and patches never care which way it faces. R turns through the facings a station has in the order
    /// down, left, up, right.
    /// </summary>
    /// <example>
    /// A still that is long when it faces the camera and narrow when it faces left, with a flipped copy for right.
    /// <code><![CDATA[
    /// station
    ///     .Model(longStill)
    ///     .Left(narrowStill)
    ///     .MirrorSides();
    /// ]]></code>
    /// </example>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline. See <see cref="Site"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The model has no size on some axis.</exception>
    public TimbnStationBuilder Left(TimbnModel model, TimbnModel? site = null) => Face(TimbnFacing.Left, model, model.Size, site);

    /// <summary>The station's look when it faces left, with a footprint of the given size. See <see cref="Left(TimbnModel, TimbnModel)"/>.</summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="size">Width, height and depth in world units.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is zero or below on some axis.</exception>
    public TimbnStationBuilder Left(TimbnModel model, Vector3 size, TimbnModel? site = null) => Face(TimbnFacing.Left, model, size, site);

    /// <summary>
    /// The station's look when it faces right, with its own footprint from the model and its dock spot on the right
    /// edge. Leave it out and call <see cref="MirrorSides"/> to get it from <see cref="Left(TimbnModel, TimbnModel)"/>
    /// flipped.
    /// </summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline.</param>
    /// <exception cref="ArgumentOutOfRangeException">The model has no size on some axis.</exception>
    public TimbnStationBuilder Right(TimbnModel model, TimbnModel? site = null) => Face(TimbnFacing.Right, model, model.Size, site);

    /// <summary>The station's look when it faces right, with a footprint of the given size. See <see cref="Right(TimbnModel, TimbnModel)"/>.</summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="size">Width, height and depth in world units.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is zero or below on some axis.</exception>
    public TimbnStationBuilder Right(TimbnModel model, Vector3 size, TimbnModel? site = null) => Face(TimbnFacing.Right, model, size, site);

    /// <summary>
    /// The station's look when it faces away from the camera, with its own footprint from the model and its dock spot on
    /// the back edge. It is never mirrored, since it looks the same flipped.
    /// </summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline.</param>
    /// <exception cref="ArgumentOutOfRangeException">The model has no size on some axis.</exception>
    public TimbnStationBuilder Up(TimbnModel model, TimbnModel? site = null) => Face(TimbnFacing.Up, model, model.Size, site);

    /// <summary>The station's look when it faces away from the camera, with a footprint of the given size. See <see cref="Up(TimbnModel, TimbnModel)"/>.</summary>
    /// <param name="model">The model, centered on the station with its base at the ground.</param>
    /// <param name="size">Width, height and depth in world units.</param>
    /// <param name="site">The construction site's look for this facing, or null for the generated outline.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is zero or below on some axis.</exception>
    public TimbnStationBuilder Up(TimbnModel model, Vector3 size, TimbnModel? site = null) => Face(TimbnFacing.Up, model, size, site);

    /// <summary>
    /// Fills a missing left or right facing with a left to right flipped copy of the other one, drawn with the same
    /// model and site, so a mod needs art for one of the two only. A copy is only made when the other facing exists
    /// and this one wasn't given. Down and up look the same flipped and are never mirrored, and nothing is ever flipped
    /// top to bottom. The order of this call among the others doesn't matter.
    /// </summary>
    public TimbnStationBuilder MirrorSides()
    {
        MirrorsSides = true;
        return this;
    }

    /// <summary>
    /// How far past the footprint's edge the dock spot is, for every side, where a zombie stands to work it and finished
    /// items drop. Left alone it is 0.45, which suits a deep bench, and a small station looks better with the zombie closer.
    /// It only moves the finished station's dock. The construction site keeps at least 0.45, since the keeper has to stand
    /// at the site's dock to work it and a closer one sits inside the site's solid body.
    /// </summary>
    /// <param name="distance">World units from the edge of the footprint, for example 0.2 to stand right at it.</param>
    /// <exception cref="ArgumentOutOfRangeException">The distance is below zero.</exception>
    public TimbnStationBuilder DockDistance(float distance)
    {
        if (distance < 0f)
            throw new ArgumentOutOfRangeException(nameof(distance), $"The dock distance can't be below zero. Got {distance}.");

        DockGap = distance;
        return this;
    }

    /// <summary>
    /// The construction site's look when the station faces the camera, the outline that stands there until the player
    /// works it off with the hammer. It gets the same footprint as the station. Without one the site is a dashed outline
    /// of the footprint on the ground with the game's own warning sign at the front. The other facings take their site
    /// as an argument, and a mirrored facing uses the site of the one it flips.
    /// </summary>
    /// <param name="model">The site's model.</param>
    public TimbnStationBuilder Site(TimbnModel model)
    {
        _sides.SetSite(TimbnFacing.Down, model);
        return this;
    }

    private TimbnStationBuilder Face(TimbnFacing facing, TimbnModel model, Vector3 size, TimbnModel? site)
    {
        _sides.Set(facing, model, size);
        if (site != null)
            _sides.SetSite(facing, site);

        return this;
    }

    /// <summary>
    /// Puts the finished station down as soon as it is placed, with no construction site and no hammer work, for a
    /// station that is more of a placed object than something built. Its cost is still taken when it is placed.
    /// </summary>
    public TimbnStationBuilder NoConstruction()
    {
        SkipsConstruction = true;
        return this;
    }

    /// <summary>
    /// Adds a model to the station's front facing that shows while a condition holds and goes away when it doesn't, the
    /// way the game's autopsy table shows a cloth over a body. The game checks the condition again when the station's
    /// items, resources, worker or work change, and when the station loads, so it needs nothing from the mod. The model
    /// lines up with the station's own when both are built from faces, whatever their sizes. It adds no footprint,
    /// body or dock, so it can't change where the station stands or where a zombie works it.
    /// </summary>
    /// <example>
    /// A slab that shows a zombie lying on it while one is seated, instead of the empty stone.
    /// <code><![CDATA[
    /// station
    ///     .Model(emptySlab)
    ///     .ShowWhen(TimbnLook.HasWorker(), slabWithZombie, replacesBase: true);
    /// ]]></code>
    /// </example>
    /// <param name="condition">When the model shows, from <see cref="TimbnLook"/>.</param>
    /// <param name="model">The model to show.</param>
    /// <param name="replacesBase">True to hide the station's own model while this one shows, for a full swap of the look.</param>
    /// <exception cref="ArgumentNullException">The condition or the model is null.</exception>
    public TimbnStationBuilder ShowWhen(TimbnLookCondition condition, TimbnModel model, bool replacesBase = false) =>
        ShowWhen(TimbnFacing.Down, condition, model, replacesBase);

    /// <summary>
    /// Adds a model to one facing that shows while a condition holds. A mirrored facing shows the models of the facing
    /// it flips, so give them to that one. See <see cref="ShowWhen(TimbnLookCondition, TimbnModel, bool)"/>.
    /// </summary>
    /// <param name="facing">The facing the model belongs to, which has to be a drawn one.</param>
    /// <param name="condition">When the model shows, from <see cref="TimbnLook"/>.</param>
    /// <param name="model">The model to show.</param>
    /// <param name="replacesBase">True to hide that facing's own model while this one shows. Only one model per facing may.</param>
    /// <exception cref="ArgumentNullException">The condition or the model is null.</exception>
    public TimbnStationBuilder ShowWhen(TimbnFacing facing, TimbnLookCondition condition, TimbnModel model, bool replacesBase = false)
    {
        Overlays.Add(new TimbnStationOverlay(facing, model ?? throw new ArgumentNullException(nameof(model)), condition ?? throw new ArgumentNullException(nameof(condition)), replacesBase));
        return this;
    }

    /// <summary>
    /// Adds a slot to the station's front facing. A slot is a spot where another station or extension goes. That piece
    /// calls BuildsInSlot with the same id. The game's conveyor furnace works this way, with spots for its bellows and
    /// press. In build mode the game marks each free slot in green and places the piece only there. It also links the
    /// two, so the station can find what sits in its slots with <see cref="TimbnWorld.Extensions"/>. Removing the
    /// station removes what sits in its slots. They are refunded and anything they hold drops on the ground. A slot is
    /// the size of what goes in it, so pass the same size you gave that piece's Model. The game snaps both to its build
    /// grid. The size has to be whole cells (0.32 across and 0.3 deep). The slot's corner has to be a whole number of
    /// cells from the corner of this station's footprint. If it isn't, Stations.Add tells you the nearest spot that
    /// fits. A slot can sit beside the station, like the yard furnace's bellows. It can also sit inside the footprint,
    /// like the conveyor furnace's pieces on top.
    /// </summary>
    /// <example>
    /// A table with a stand's slot on its left and another on its right.
    /// <code><![CDATA[
    /// station
    ///     .Model(table, new Vector3(1.28f, 0.8f, 0.6f))
    ///     .Slot("timbn_stand_slot", new Vector3(-0.96f, 0f, 0f), new Vector3(0.64f, 1f, 0.6f))
    ///     .Slot("timbn_stand_slot", new Vector3(0.96f, 0f, 0f), new Vector3(0.64f, 1f, 0.6f));
    /// ]]></code>
    /// </example>
    /// <param name="slotId">The slot's id, which the station or extension that goes in it names.</param>
    /// <param name="position">Where the center of what goes in the slot sits, from the center of this station on the ground. The height is ignored.</param>
    /// <param name="size">The size of what goes in the slot. The height is ignored.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The width or depth is zero or below.</exception>
    public TimbnStationBuilder Slot(string slotId, Vector3 position, Vector3 size) => Slot(TimbnFacing.Down, slotId, position, size);

    /// <summary>
    /// Builds a slot into one facing of the station, from the center of that facing's footprint. A mirrored facing gets
    /// the slots of the facing it flips, flipped, so give them to that one. See <see cref="Slot(string, Vector3, Vector3)"/>.
    /// </summary>
    /// <param name="facing">The facing the slot belongs to, which has to be a drawn one.</param>
    /// <param name="slotId">The slot's id, which the station or extension that goes in it names.</param>
    /// <param name="position">Where the center of what goes in the slot sits, from the center of this facing on the ground. The height is ignored.</param>
    /// <param name="size">The size of what goes in the slot. The height is ignored.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The width or depth is zero or below.</exception>
    public TimbnStationBuilder Slot(TimbnFacing facing, string slotId, Vector3 position, Vector3 size)
    {
        TimbnSlotRules.CheckId(slotId);
        TimbnSlotRules.CheckSize(size);
        Slots.Add(new TimbnStationSlot(facing, slotId, position, size));
        return this;
    }

    /// <summary>
    /// Gives the station a spot where the game's own handler seats a zombie, for a station based on the game's zombie
    /// supplier ("zombie_supplier_station") or another that uses its handler. The player puts a zombie in with E and
    /// takes it out the usual way, and the zombie is the station's worker. Core bakes the spot at the dock edge of
    /// every facing, looking back at the station. A seated zombie is still a caretaker. It would ask its zone for orders
    /// and walk off to carry things for zombie crafter benches. Core keeps it sitting unless the station takes orders.
    /// </summary>
    /// <param name="takesOrders">True to leave the zombie to take orders like the game's own supplier does, for a mod that makes a real one.</param>
    public TimbnStationBuilder ZombieSpot(bool takesOrders = false)
    {
        HasZombieSpot = true;
        ZombieTakesOrders = takesOrders;
        return this;
    }

    /// <summary>
    /// Lets the player lay a zombie on the station and take it back, the way the game's corpse pallet holds a corpse. E
    /// with a zombie overhead puts it in the station's one slot, where it stops being drawn, and E with free hands takes
    /// it back unless a craft is running there, so a craft started on the station holds the zombie until it ends. The
    /// station can't be dismantled while a zombie lies on it. Show the zombie with
    /// <see cref="ShowWhen(TimbnLookCondition, TimbnModel, bool)"/> and <see cref="TimbnLook.HoldsGroup"/>("zombie"),
    /// and start a craft there with Crafts.Start. The game's resurrection table ("resurrection_table_1")
    /// makes a good base.
    /// </summary>
    public TimbnStationBuilder HoldsZombie()
    {
        HoldsZombieItem = true;
        return this;
    }

    /// <summary>
    /// Caps how many of the station the player can have at once, through the game's own build limit, so the build desk
    /// shows how many are built out of how many are allowed and refuses to place another at the cap. Core counts the
    /// stations and construction sites standing in the world each time, so a site counts from the moment it is placed
    /// and stops counting once it is gone, however it went.
    /// </summary>
    /// <param name="max">The most the player can have, 1 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The limit is below 1.</exception>
    public TimbnStationBuilder Limit(int max)
    {
        TimbnStationLimitRules.Validate(max);
        BuildLimit = max;
        return this;
    }

    /// <summary>
    /// Sets the game station the new one behaves like, copied field by field. "kitchen_oven" gives a timed station that
    /// cooks on its own and is tended by carrier zombies. "sawhorse" gives a bench worked by hand. The default is the woodworking
    /// bench ("woodworking_workbench_1"). The construction site, build desk entry and removals come from the same
    /// station when the game builds that one from a build desk. Otherwise they come from the woodworking bench.
    /// </summary>
    /// <param name="stationId">The game station's world object id.</param>
    public TimbnStationBuilder BasedOn(string stationId)
    {
        TemplateId = stationId;
        return this;
    }

    /// <summary>
    /// The talent the station is worked under, which is the skill its crafts and its dismantling use. The game's
    /// kitchen oven uses "talent_green" and its woodworking bench and circular saw "talent_orange". Left alone it is the
    /// base station's.
    /// </summary>
    /// <param name="talentId">The talent's id, for example "talent_orange".</param>
    public TimbnStationBuilder Talent(string talentId)
    {
        TalentId = talentId;
        return this;
    }

    /// <summary>
    /// The tool the player or a zombie needs to work the station, which also picks the zombie's work animation. A
    /// craft's own tool, when it has one, wins over this. The kitchen oven's is none, so a station based on it needs no
    /// tool, and a hammer here makes it a bench worked with the hammer. Left alone it is the base station's.
    /// </summary>
    /// <param name="tool">The item type, such as ItemType.Hammer, or ItemType.None for no tool.</param>
    public TimbnStationBuilder Tool(ItemType tool)
    {
        ToolType = tool;
        return this;
    }

    /// <summary>
    /// How many ingredient slots the station's craft window has, which caps how many different items one craft can
    /// take at once. The woodworking bench has 50. Left alone it is the base station's.
    /// </summary>
    /// <param name="slots">The slot count, 0 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is below zero.</exception>
    public TimbnStationBuilder CraftSlots(int slots)
    {
        CheckSlots(slots);
        CraftSlotCount = slots;
        return this;
    }

    /// <summary>
    /// How many slots the station's own inventory has, where fuel and anything a craft leaves in the station goes. Zero
    /// gives it none. Left alone it is the base station's.
    /// </summary>
    /// <param name="slots">The slot count, 0 or more.</param>
    /// <exception cref="ArgumentOutOfRangeException">The count is below zero.</exception>
    public TimbnStationBuilder Storage(int slots)
    {
        CheckSlots(slots);
        StorageSlotCount = slots;
        return this;
    }

    private static void CheckSlots(int slots)
    {
        if (slots < 0)
            throw new ArgumentOutOfRangeException(nameof(slots), $"The slot count can't be below zero. Got {slots}.");
    }

    /// <summary>Whether the player can put a zombie to work at the station. Left alone it is the base station's.</summary>
    /// <param name="takes">True to take a zombie, false to leave the station to the player.</param>
    public TimbnStationBuilder TakesZombie(bool takes = true)
    {
        AllowsZombie = takes;
        return this;
    }

    /// <summary>
    /// Makes the station run its crafts by itself over time. The kitchen oven works like this when it cooks while the
    /// player is away. Its tick duration is 10. Use <see cref="Manual"/> for a station that only progresses while
    /// someone works it.
    /// </summary>
    /// <param name="tickDuration">The game's autocraft tick duration, for example 10.</param>
    public TimbnStationBuilder Automatic(int tickDuration)
    {
        IsAutomatic = true;
        AutoTickDuration = tickDuration;
        return this;
    }

    /// <summary>Makes the station progress its crafts only while the player or a zombie works it, like the circular saw.</summary>
    public TimbnStationBuilder Manual()
    {
        IsAutomatic = false;
        AutoTickDuration = null;
        return this;
    }

    /// <summary>
    /// The sound the station makes each time its work ticks, for example "woodworking_workbench_tick" for the
    /// woodworking bench. Any sound id the game has. Left alone it is the base station's.
    /// </summary>
    /// <param name="soundId">The sound's id.</param>
    public TimbnStationBuilder WorkSound(string soundId)
    {
        ActionSound = soundId;
        return this;
    }

    /// <summary>
    /// Which tab of the build desk's menu lists the station. An empty string is the first tab, and a station based on a
    /// bench keeps the bench's, which is the second tab at the morgue and the "tab_building_extension" tab at the yard.
    /// Use a tab id the build desk already has, from <see cref="TimbnGameIds.BuildTabs"/>, such as
    /// <c>BuildTabs.MetallurgyExtension</c> or <c>BuildTabs.SewingExtension</c> at the yard or
    /// <c>BuildTabs.GraveyardModule</c> at the graveyard, since a tab nothing else uses has no icon.
    /// </summary>
    /// <param name="tabId">The tab's id, or <c>BuildTabs.First</c> (an empty string) for the first tab.</param>
    public TimbnStationBuilder BuildTab(string tabId)
    {
        TabId = tabId;
        return this;
    }

    /// <summary>
    /// The icon shown over the station and in the build desk. Use any sprite the game has or one added with
    /// Sprites.AddEmbedded and the Blueprint kind. Without one the template's icons stay.
    /// </summary>
    /// <param name="spriteId">The sprite name, for example "timbn_red_workbench".</param>
    public TimbnStationBuilder Icon(string spriteId)
    {
        IconId = spriteId;
        return this;
    }

    /// <summary>
    /// What the build desk charges to place the station, for example ("flitch", 4). A second call adds to the first, and
    /// a station with no cost is free. Half of it comes back when the station is dismantled unless <see cref="Refund"/>
    /// or <see cref="NoRefund"/> says otherwise, and all of it when an unbuilt site is removed.
    /// </summary>
    /// <param name="items">Item ids with counts.</param>
    public TimbnStationBuilder Cost(params (string Item, int Count)[] items)
    {
        CostItems.AddRange(items);
        return this;
    }

    /// <summary>What dismantling the finished station gives back, instead of half the cost. A second call adds to the first.</summary>
    /// <param name="item">An item id with its count.</param>
    /// <param name="more">More item ids with counts.</param>
    public TimbnStationBuilder Refund((string Item, int Count) item, params (string Item, int Count)[] more)
    {
        RefundItems ??= [];
        RefundItems.Add(item);
        RefundItems.AddRange(more);
        return this;
    }

    /// <summary>Gives nothing back when the finished station is dismantled, instead of half the cost.</summary>
    public TimbnStationBuilder NoRefund()
    {
        RefundItems = [];
        return this;
    }

    /// <summary>
    /// Which build desk offers the station, the yard's ("builder_yard") by default. Each build desk builds in its own zone,
    /// for example "builder_graveyard" or "builder_garden". The station can be limited to one build area in that zone,
    /// such as "yard_place". Without an area the station can go anywhere in the zone. The exception is a build desk that
    /// the base station is also built at. There the base station's area is kept.
    /// </summary>
    /// <param name="buildDeskId">The build desk's world object id.</param>
    /// <param name="areaId">A build area id in the build desk's zone, or null for the default.</param>
    public TimbnStationBuilder BuiltAt(string buildDeskId, string? areaId = null)
    {
        BuildDeskId = buildDeskId;
        AreaId = areaId;
        return this;
    }

    /// <summary>
    /// Lets the player build the station only into a slot of another station, one that has a <see cref="Slot(string, Vector3, Vector3)"/>
    /// with this id, through the game's own slot check, the one the conveyor furnace's bellows use. It goes down
    /// finished with no construction site like the game's own slot pieces, and it is still a normal station the player
    /// works. Its footprint should match the slot, and it is still offered by the build desk from <see cref="BuiltAt"/>,
    /// which has to be the one whose zone the slotted station stands in.
    /// </summary>
    /// <param name="slotId">The slot's id.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    public TimbnStationBuilder BuildsInSlot(string slotId)
    {
        TimbnSlotRules.CheckId(slotId);
        InSlotId = slotId;
        return this;
    }

    /// <summary>Takes away the fuel the template burns, so a station copied from the kitchen oven runs without fire.</summary>
    public TimbnStationBuilder NoFuel()
    {
        ClearsFuel = true;
        return this;
    }

    /// <summary>
    /// Which bench extensions attach to the station when built next to it, the game's "tool_rack" or one added with
    /// Stations.AddExtension. The first call replaces the template's list and a second call adds to it. A craft that
    /// calls NeedsExtension with one of these ids only runs while that extension is attached.
    /// </summary>
    /// <param name="extensionId">An extension's world object id.</param>
    /// <param name="more">More extensions' world object ids.</param>
    public TimbnStationBuilder Extensions(string extensionId, params string[] more)
    {
        ExtensionIds ??= [];
        ExtensionIds.Add(extensionId);
        ExtensionIds.AddRange(more);
        return this;
    }

    /// <summary>Takes away the template's bench extensions, so none attaches to the station.</summary>
    public TimbnStationBuilder NoExtensions()
    {
        ExtensionIds = [];
        return this;
    }

    /// <summary>
    /// Has a technology put the station on the build desk, the way "work_with_wood_1" opens the woodworking bench. The
    /// tech's tooltip lists the station, researching the tech unlocks it, and a save that already has the tech gets it
    /// as soon as it loads. The station is missing from the build desk until then. The tech is put back when the mod
    /// unloads, and a tech the game doesn't have is logged.
    /// </summary>
    /// <param name="techId">The technology's id.</param>
    public TimbnStationBuilder UnlockedBy(string techId)
    {
        TechId = techId;
        return this;
    }

    /// <summary>Changes anything else on the station's definition after Core has filled it in, for example its craftInventorySize.</summary>
    /// <param name="edit">Runs on the copy each time the balance loads.</param>
    public TimbnStationBuilder Edit(Action<WGODef> edit)
    {
        EditStation = edit;
        return this;
    }
}
