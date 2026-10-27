namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a bench extension for Stations.AddExtension, like the game's tool rack. An extension is placed straight
/// from the build desk with no construction site and has nothing to interact with. It attaches to a station that
/// lists it in <see cref="TimbnStationBuilder.Extensions"/> when the two are built next to each other. Core turns it
/// into the extension, its build desk entry and its removal, with their text. Only <see cref="Model"/> is required.
/// An extension with no <see cref="Cost"/> is free. One with no <see cref="UnlockedBy"/> is on the build desk from
/// the start.
/// </summary>
/// <example>
/// A pillar that opens a craft at the Stick Box when built next to it.
/// <code><![CDATA[
/// var model = Models.Build("RedPillar", faces => faces.Block("red_pillar", x: 0, y: 0, z: 0, depth: 20));
/// Stations.AddExtension("timbn_red_magic", "Red Magic Pillar", "A humming red pillar.", pillar => pillar
///     .Model(model, model.Size)
///     .Icon("timbn_red_magic")
///     .Cost(("flitch", 2)));
/// ]]></code>
/// </example>
public sealed class TimbnExtensionBuilder
{
    internal TimbnExtensionBuilder(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    internal string Id { get; }

    internal string Name { get; }

    internal string Description { get; }

    internal TimbnModel? ExtensionModel { get; private set; }

    internal Vector3 Size { get; private set; }

    internal float ReachDistance { get; private set; } = TimbnStationRules.DefaultReach;

    internal string TemplateId { get; private set; } = TimbnStationRules.DefaultExtensionTemplate;

    internal string IconId { get; private set; } = "";

    internal List<(string Item, int Count)> CostItems { get; } = [];

    internal List<(string Item, int Count)>? RefundItems { get; private set; }

    internal string BuildDeskId { get; private set; } = TimbnStationRules.DefaultBuildDesk;

    internal string? AreaId { get; private set; }

    internal string? InSlotId { get; private set; }

    internal string? TechId { get; private set; }

    internal Action<WGODef>? EditExtension { get; private set; }

    /// <summary>Sets how the extension looks, with a model from Models. Core gives it a box shaped footprint of the given size and a solid body.</summary>
    /// <param name="model">The model, centered on the extension with its base at the ground.</param>
    /// <param name="size">Width, height and depth in world units, for example (0.5, 1, 0.5) for a pillar.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is zero or below on any axis.</exception>
    public TimbnExtensionBuilder Model(TimbnModel model, Vector3 size)
    {
        if (size.x <= 0f || size.y <= 0f || size.z <= 0f)
            throw new ArgumentOutOfRangeException(nameof(size), $"An extension's size can't be zero or below on any axis. Got {size}.");

        ExtensionModel = model;
        Size = size;
        return this;
    }

    /// <summary>How far past the footprint the extension reaches to attach to a station, 0.64 by default like the tool rack.</summary>
    /// <param name="distance">The reach in world units.</param>
    public TimbnExtensionBuilder Reach(float distance)
    {
        ReachDistance = distance;
        return this;
    }

    /// <summary>
    /// Sets the game extension the new one is copied from. The default is the tool rack ("tool_rack"). The build desk
    /// entry and removal come from the same extension when it has them. Otherwise they come from the tool rack.
    /// </summary>
    /// <param name="extensionId">The game extension's world object id.</param>
    public TimbnExtensionBuilder BasedOn(string extensionId)
    {
        TemplateId = extensionId;
        return this;
    }

    /// <summary>The icon shown in the build desk and on the station's craft window row. Use any sprite the game has or one added with Sprites.AddEmbeddedPng. Without one the template's icons stay.</summary>
    /// <param name="spriteId">The sprite name, for example "timbn_red_magic".</param>
    public TimbnExtensionBuilder Icon(string spriteId)
    {
        IconId = spriteId;
        return this;
    }

    /// <summary>
    /// What the build desk charges to place the extension, for example ("flitch", 2). A second call adds to the first,
    /// and an extension with no cost is free. Half of it comes back when it is dismantled unless <see cref="Refund"/>
    /// or <see cref="NoRefund"/> says otherwise.
    /// </summary>
    /// <param name="items">Item ids with counts.</param>
    public TimbnExtensionBuilder Cost(params (string Item, int Count)[] items)
    {
        CostItems.AddRange(items);
        return this;
    }

    /// <summary>What dismantling the extension gives back, instead of half the cost. A second call adds to the first.</summary>
    /// <param name="item">An item id with its count.</param>
    /// <param name="more">More item ids with counts.</param>
    public TimbnExtensionBuilder Refund((string Item, int Count) item, params (string Item, int Count)[] more)
    {
        RefundItems ??= [];
        RefundItems.Add(item);
        RefundItems.AddRange(more);
        return this;
    }

    /// <summary>Gives nothing back when the extension is dismantled, instead of half the cost.</summary>
    public TimbnExtensionBuilder NoRefund()
    {
        RefundItems = [];
        return this;
    }

    /// <summary>Which build desk offers the extension, the yard's ("builder_yard") by default, and a build area in its zone to limit it to. See <see cref="TimbnStationBuilder.BuiltAt"/>.</summary>
    /// <param name="buildDeskId">The build desk's world object id.</param>
    /// <param name="areaId">A build area id in the build desk's zone, or null for the default.</param>
    public TimbnExtensionBuilder BuiltAt(string buildDeskId, string? areaId = null)
    {
        BuildDeskId = buildDeskId;
        AreaId = areaId;
        return this;
    }

    /// <summary>
    /// Lets the player build the extension only into a slot of a station, one that has a
    /// <see cref="TimbnStationBuilder.Slot(string, Vector3, Vector3)"/> with this id, the way the conveyor furnace takes
    /// its bellows. The station lists the extension in <see cref="TimbnStationBuilder.Extensions"/> for crafts that
    /// need it. Its footprint should match the slot.
    /// </summary>
    /// <param name="slotId">The slot's id.</param>
    /// <exception cref="ArgumentException">The id is empty.</exception>
    public TimbnExtensionBuilder BuildsInSlot(string slotId)
    {
        TimbnSlotRules.CheckId(slotId);
        InSlotId = slotId;
        return this;
    }

    /// <summary>
    /// Has a technology put the extension on the build desk, like <see cref="TimbnStationBuilder.UnlockedBy"/> for a
    /// station. It is missing from the build desk until the save has the tech.
    /// </summary>
    /// <param name="techId">The technology's id.</param>
    public TimbnExtensionBuilder UnlockedBy(string techId)
    {
        TechId = techId;
        return this;
    }

    /// <summary>Changes anything else on the extension's definition after Core has filled it in.</summary>
    /// <param name="edit">Runs on the copy each time the balance loads.</param>
    public TimbnExtensionBuilder Edit(Action<WGODef> edit)
    {
        EditExtension = edit;
        return this;
    }
}
