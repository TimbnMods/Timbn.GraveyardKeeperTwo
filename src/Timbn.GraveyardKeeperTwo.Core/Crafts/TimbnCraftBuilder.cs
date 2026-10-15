namespace Timbn.GraveyardKeeperTwo.Core.Framework;

/// <summary>
/// Describes a craft for the plugin's Crafts.Add, with where it runs, what it takes in and what it makes. The craft is
/// copied from one of the game's own, the woodworking bench's wooden plank unless <see cref="BasedOn"/> names another, so it
/// starts with everything the game expects and the builder changes only what differs. What is not set stays the
/// template's, so a craft that only calls <see cref="Needs(string, int)"/> and <see cref="Makes(string, int)"/> keeps the template's
/// station, work time and energy. The one exception is the unlock, since a craft is open from the start whatever the
/// template did unless <see cref="NeedsUnlock"/> or <see cref="UnlockedBy"/> is called.
/// </summary>
/// <example>
/// Four sticks from a board at the Stick Box, and a plank from sticks that needs the pillar next to it.
/// <code><![CDATA[
/// Crafts.Add("timbn_stick_box_sticks", "Sticks from a Board", "Split one board into 4 sticks.", craft => craft
///     .In(box)
///     .Needs("flitch")
///     .Makes("stick", 4));
///
/// Crafts.Add("timbn_stick_box_magic_plank", "Plank from Sticks", "The red pillar binds 4 sticks back into a plank.", craft => craft
///     .In(box)
///     .NeedsExtension("timbn_red_magic")
///     .Needs("stick", 4)
///     .Makes("wooden_plank"));
/// ]]></code>
/// </example>
public sealed class TimbnCraftBuilder
{
    internal TimbnCraftBuilder(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    internal string Id { get; }

    internal string Name { get; }

    internal string Description { get; }

    internal string TemplateId { get; private set; } = TimbnCraftRules.DefaultTemplate;

    internal List<string>? StationIds { get; private set; }

    internal List<(string Item, string Count)>? NeedItems { get; private set; }

    internal List<(string Item, string Count)>? Outputs { get; private set; }

    internal bool MustBeUnlocked { get; private set; }

    internal string? TechId { get; private set; }

    internal int? MasteryLevel { get; private set; }

    internal string? ExtensionId { get; private set; }

    internal string? IconId { get; private set; }

    internal int? WorkTicks { get; private set; }

    internal int? EnergyEachTick { get; private set; }

    internal bool FinishesItself { get; private set; }

    internal Action<CraftDef>? EditCraft { get; private set; }

    internal List<Action<TimbnCraftEvent>> StartedHandlers { get; } = [];

    internal List<Action<TimbnCraftEvent>> FinishedHandlers { get; } = [];

    internal List<Func<WgoData, bool>> Checks { get; } = [];

    /// <summary>
    /// A station the craft runs at, the game's own such as "kitchen_oven" or "woodworking_workbench_1", or one added
    /// with Stations.Add. Call it once per station. The first call replaces the template's stations. Whether the craft
    /// runs on its own follows the station, so a craft at the kitchen oven cooks by itself and one at a bench is worked
    /// by hand.
    /// </summary>
    /// <param name="stationId">The station's world object id.</param>
    public TimbnCraftBuilder In(string stationId)
    {
        StationIds ??= [];
        StationIds.Add(stationId);
        return this;
    }

    /// <summary>A station added with Stations.Add that the craft runs at. See <see cref="In(string)"/>.</summary>
    /// <param name="station">The station.</param>
    public TimbnCraftBuilder In(TimbnStation station) => In(station.Id);

    /// <summary>
    /// The game craft the new one is copied from, "wooden_plank" by default. Pick a craft from the station the new one
    /// runs at to inherit its work time, energy and tab, for example "cook_baked_fish" for a smoked fish at an oven like
    /// station.
    /// </summary>
    /// <param name="craftId">The game craft's id.</param>
    public TimbnCraftBuilder BasedOn(string craftId)
    {
        TemplateId = craftId;
        return this;
    }

    /// <summary>An item the craft takes in. Call it once per item. The first call replaces the template's items.</summary>
    /// <param name="itemId">The item's id, for example "flitch".</param>
    /// <param name="count">How many, 1 by default.</param>
    public TimbnCraftBuilder Needs(string itemId, int count = 1) => Needs(itemId, count.ToString());

    /// <summary>
    /// An item the craft takes in, with its count as one of the game's expressions, for example "10-5*#perk_heat_saver"
    /// for firewood that a perk saves on. A count that doesn't parse fails here, when the plugin loads.
    /// </summary>
    /// <param name="itemId">The item's id.</param>
    /// <param name="count">The game expression for how many.</param>
    public TimbnCraftBuilder Needs(string itemId, string count)
    {
        NeedItems ??= [];
        NeedItems.Add((itemId, Checked(count)));
        return this;
    }

    /// <summary>Several items the craft takes in at once, for example ("fillet_fish_river", 1), ("firewood", 1).</summary>
    /// <param name="items">Item ids with counts.</param>
    public TimbnCraftBuilder Needs(params (string Item, int Count)[] items)
    {
        foreach (var (item, count) in items)
            Needs(item, count);

        return this;
    }

    /// <summary>What the craft makes. Call it once per item. The first call replaces the template's output.</summary>
    /// <param name="itemId">The item's id, for example "stick".</param>
    /// <param name="count">How many, 1 by default.</param>
    public TimbnCraftBuilder Makes(string itemId, int count = 1) => Makes(itemId, count.ToString());

    /// <summary>What the craft makes, with its count as one of the game's expressions, for example "3+2*#perk_cook_2" for a perk that adds two.</summary>
    /// <param name="itemId">The item's id.</param>
    /// <param name="count">The game expression for how many.</param>
    public TimbnCraftBuilder Makes(string itemId, string count)
    {
        Outputs ??= [];
        Outputs.Add((itemId, Checked(count)));
        return this;
    }

    /// <summary>
    /// Keeps the craft hidden until the save unlocks it, by a tech that lists it in craftsAfterUnlock or by
    /// TimbnPlayer.UnlockCraft. A craft is open from the start otherwise.
    /// </summary>
    public TimbnCraftBuilder NeedsUnlock()
    {
        MustBeUnlocked = true;
        return this;
    }

    /// <summary>
    /// Has a technology grant the craft, the way "cooking_pastries" grants the muffin and the croissant. The tech's
    /// tooltip lists the craft, researching the tech unlocks it, and a save that already has the tech gets it as soon
    /// as it loads. The craft stays hidden until then, so this includes <see cref="NeedsUnlock"/>. The tech is put
    /// back when the plugin unloads, and a tech the game doesn't have is logged.
    /// </summary>
    /// <param name="techId">The technology's id.</param>
    public TimbnCraftBuilder UnlockedBy(string techId)
    {
        TechId = techId;
        MustBeUnlocked = true;
        return this;
    }

    /// <summary>
    /// The mastery level the player or zombie needs in the station's talent to work the craft, which the player gets
    /// from the tool on their belt. The template's otherwise, 2 for the wooden plank, so Mastery(1) lets any tool work it.
    /// </summary>
    /// <param name="level">The level, 1 or more.</param>
    public TimbnCraftBuilder Mastery(int level)
    {
        MasteryLevel = level;
        return this;
    }

    /// <summary>Makes the craft run only while this bench extension is attached to the station, the game's "tool_rack" or one added with Stations.AddExtension. The craft window shows it in that extension's row.</summary>
    /// <param name="extensionId">The extension's world object id.</param>
    public TimbnCraftBuilder NeedsExtension(string extensionId)
    {
        ExtensionId = extensionId;
        return this;
    }

    /// <summary>The icon the craft window shows for the craft, instead of the icon of what it makes. Any sprite the game has, or one added with the plugin's Sprites.AddEmbeddedPng.</summary>
    /// <param name="spriteId">The sprite name.</param>
    public TimbnCraftBuilder Icon(string spriteId)
    {
        IconId = spriteId;
        return this;
    }

    /// <summary>How long the craft takes in work ticks, swings of the tool at a bench worked by hand or the station's own tick at one that runs itself (10 seconds each at the kitchen oven). The template's otherwise, 4 for the wooden plank.</summary>
    /// <param name="ticks">The number of ticks.</param>
    public TimbnCraftBuilder DurationTicks(int ticks)
    {
        WorkTicks = ticks;
        return this;
    }

    /// <summary>How much energy each work tick costs the player. The template's otherwise, 2 for the wooden plank.</summary>
    /// <param name="energy">Energy per tick.</param>
    public TimbnCraftBuilder EnergyPerTick(int energy)
    {
        EnergyEachTick = energy;
        return this;
    }

    /// <summary>
    /// Lets a craft at a station that runs itself finish on its own, dropping what it made next to the station and
    /// starting the next craft in the queue. Without it the station waits for the player to take the result, the way
    /// the kitchen oven does.
    /// </summary>
    public TimbnCraftBuilder AutoFinish()
    {
        FinishesItself = true;
        return this;
    }

    /// <summary>
    /// Runs the handler each time the craft starts at a station, which is when its ingredients are taken, for a timed
    /// craft as well as an instant one. A craft with a count above one starts once per unit. The handler is undone when
    /// the plugin unloads, and one that throws is logged without stopping the craft.
    /// </summary>
    /// <param name="handler">Told the station and the craft's id.</param>
    public TimbnCraftBuilder OnStarted(Action<TimbnCraftEvent> handler)
    {
        StartedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Runs the handler each time the craft finishes at a station, as the game finishes it, once per unit of a craft with
    /// a count above one. A craft that fails while finishing doesn't raise it. The handler is undone when the plugin
    /// unloads, and one that throws is logged without stopping the craft.
    /// </summary>
    /// <example>
    /// Pay a tech point to every seated zombie when the teaching craft ends.
    /// <code><![CDATA[
    /// Crafts.Add("timbn_teach", "Teach", "Teaches every seated zombie.", craft => craft
    ///     .In(machine)
    ///     .OnFinished(done => PayZombies(done.Station)));
    /// ]]></code>
    /// </example>
    /// <param name="handler">Told the station and the craft's id.</param>
    public TimbnCraftBuilder OnFinished(Action<TimbnCraftEvent> handler)
    {
        FinishedHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Lets the craft start at a station only while the check passes for that station, for a condition the game has no
    /// field for. Until it passes the craft window's Start button stays gray and a queued craft waits, the way they do
    /// for a missing ingredient, and a craft already running carries on. The game asks again whenever it looks at the
    /// craft, so keep the check quick. A second call adds another check that must pass too. A check that throws is
    /// logged once and counts as passing. The check is undone when the plugin unloads.
    /// </summary>
    /// <example>
    /// A lesson that only starts while a stand in the machine's slots holds a zombie.
    /// <code><![CDATA[
    /// Crafts.Add("timbn_lesson", "Lesson", "Teaches the zombies on the stands.", craft => craft
    ///     .In(machine)
    ///     .Requires(station => TimbnWorld.Extensions(station).Any(stand => TimbnZombies.AtStation(stand) != null)));
    /// ]]></code>
    /// </example>
    /// <param name="check">Told the station the craft would start at, and returns whether it may.</param>
    /// <exception cref="ArgumentNullException">The check is null.</exception>
    public TimbnCraftBuilder Requires(Func<WgoData, bool> check)
    {
        Checks.Add(check ?? throw new ArgumentNullException(nameof(check)));
        return this;
    }

    /// <summary>Changes anything else on the craft's definition after Core has filled it in, for example its tabId.</summary>
    /// <param name="edit">Runs on the copy each time the balance loads.</param>
    public TimbnCraftBuilder Edit(Action<CraftDef> edit)
    {
        EditCraft = edit;
        return this;
    }

    private static string Checked(string count)
    {
        _ = new TimbnExpression(count);
        return count;
    }
}
