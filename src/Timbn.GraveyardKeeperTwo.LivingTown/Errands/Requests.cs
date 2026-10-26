namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// Occasional cooking requests where a resident asks the player to prepare a known recipe,
/// paying its sale price upon delivery. This is a single repeatable quest that occupies only one quest log entry.
/// </summary>
internal sealed class Requests
{
    private const int _daysPerRequest = 7;
    private const int _daysToCook = 7;

    private readonly Plugin _plugin;
    private readonly System.Random _random = new();

    public Requests(Plugin plugin)
    {
        _plugin = plugin;
    }

    public void Start()
    {
        var craving = _plugin.Quests.Add("timbn_living_town_request", quest => quest
            .Repeatable()
            .ExpiresAfter(_daysToCook)
            .Setup(Pick)
            .Giver(run => run.NpcId("villager"))
            .Icon("portrait_icon_hero")
            .Marker(
                offer: () => _plugin.Settings.IsEnabled(PluginConfig.OfferMarkers),
                handIn: () => _plugin.Settings.IsEnabled(PluginConfig.HandInMarkers))
            .OfferWhen(() => _plugin.Settings.IsEnabled(PluginConfig.VillagerRequests))
            .Description()
            .Offer(talk => talk
                .Line("ask")
                .Accept(then => then.Line("thanks"))
                .Decline(TimbnGameIds.Texts.DefaultMainNotRightNow))
            .Cost((cost, run) => cost.Item(run.Item("dish")))
            .Reward((reward, run) => reward.Money(TimbnItems.SalePrice(run.Item("dish"))))
            .Talk(talk => talk
                .Line("ask")
                .Accept(then => then.Line("thanks"))
                .Decline(TimbnGameIds.Texts.DefaultNope)));

        _plugin.Events.NewDayStarted(_ =>
        {
            if (_random.Next(_daysPerRequest) == 0)
                craving.NewRun();
        });

        _plugin.Settings.Changed(PluginConfig.VillagerRequests, on =>
        {
            if (!on)
                craving.Cancel();
        });
    }

    private bool Pick(TimbnQuestRun run)
    {
        var village = Areas.FreeInVillage();
        var towns = Areas.FreeInTowns();
        var dishes = KnownDishes();
        List<(List<string> People, string Where)> areas = [.. towns.Select(people => (people, Areas.TownKey))];
        if (village.Count > 0)
            areas.Add((village, Areas.VillageKey));

        if (areas.Count == 0 || dishes.Count == 0)
        {
            Plugin.Logger.LogInfo($"Requests: no request today, {village.Count} free villagers, {towns.Count} open town districts and {dishes.Count} known dishes.");
            return false;
        }

        (var people, var where) = areas[_random.Next(areas.Count)];
        run.Npc("villager", people[_random.Next(people.Count)])
            .Text("where", where)
            .Item("dish", dishes[_random.Next(dishes.Count)]);

        return true;
    }

    private static List<string> KnownDishes()
    {
        var balance = GameBalance.Me;
        var dishes = new HashSet<string>();
        var crafts = TimbnPlayer.KnownCrafts(
            TimbnGameIds.Workstations.Placed.KitchenOven,
            TimbnGameIds.Workstations.Placed.KitchenOvenT2,
            TimbnGameIds.Workstations.Placed.KitchenTable,
            TimbnGameIds.Workstations.Placed.KitchenTableT2);

        foreach (var craft in crafts)
        {
            if (craft.outputItems.chanceOutputItems.Count != 1 || craft.outputItems.groupChanceOutputItems.Count != 0)
                continue;

            var output = craft.outputItems.chanceOutputItems[0];
            if (output.isStarGroup || TimbnItems.SalePrice(output.id) == 0)
                continue;

            if (balance.GetDataOrNull<ItemDef>(output.id) is { } dish && dish.gameResOnUse.Exists(effect => effect.name == "energy"))
                dishes.Add(output.id);
        }

        return [.. dishes];
    }
}
