namespace Timbn.GraveyardKeeperTwo.LivingTown;

/// <summary>
/// Occasional errand deliveries where an NPC asks the player to deliver an item to a friend in another district,
/// who pays the item's sale price upon delivery. This is a single repeatable quest that occupies only one quest log entry,
/// running alongside cooking requests. Unaccepted offers or uncompleted deliveries expire after one week,
/// and keeping a parcel incurs a reimbursement fee up to the player's available funds.
/// </summary>
internal sealed class Deliveries
{
    private const int _daysPerDelivery = 7;
    private const int _daysToDeliver = 7;
    private const string _lapsedKey = "timbn_living_town_errand_lapsed";

    private static readonly Parcel[] _parcels =
    [
        new("keepsake", [TimbnGameIds.Items.TrinketsWood, TimbnGameIds.Items.TrinketsStone, TimbnGameIds.Items.TrinketsBronze]),
        new("vegetables", [TimbnGameIds.Items.BoxVegetables]),
        new("potion", [TimbnGameIds.Items.HealPotion]),
    ];

    private readonly Plugin _plugin;
    private readonly System.Random _random = new();
    private TimbnQuest _errand = null!;
    private bool _turningOff;

    public Deliveries(Plugin plugin)
    {
        _plugin = plugin;
    }

    public void Start()
    {
        _errand = _plugin.Quests.Add("timbn_living_town_delivery", quest => quest
            .Repeatable()
            .ExpiresAfter(_daysToDeliver)
            .Setup(Pick)
            .Giver(run => run.NpcId("sender"))
            .Icon("portrait_icon_hero")
            .Marker(
                offer: () => _plugin.Settings.IsEnabled(PluginConfig.OfferMarkers),
                handIn: () => _plugin.Settings.IsEnabled(PluginConfig.HandInMarkers))
            .OfferWhen(() => _plugin.Settings.IsEnabled(PluginConfig.VillagerDeliveries))
            .Description()
            .Offer(talk =>
            {
                foreach (var parcel in _parcels)
                    talk.When(() => IsParcel(parcel), then => then.Line($"ask_{parcel.Kind}"));

                talk
                    .Accept(then => then.RunCode(HandOver).Line("thanks"))
                    .Decline();
            })
            .TalksWith(run => run.NpcId("recipient"))
            .Cost((cost, run) => cost.Item(run.Item("parcel")))
            .Reward((reward, run) => reward.Money(TimbnItems.SalePrice(run.Item("parcel"))))
            .Talk(talk => talk
                .Accept(then =>
                {
                    foreach (var parcel in _parcels)
                        then.When(() => IsParcel(parcel), thanks => thanks.Line($"thanks_{parcel.Kind}"));
                })
                .Decline())
            .OnCancel(PayForParcel));

        _plugin.Events.NewDayStarted(_ =>
        {
            if (_random.Next(_daysPerDelivery) == 0)
                _errand.NewRun();
        });

        _plugin.Settings.Changed(PluginConfig.VillagerDeliveries, on =>
        {
            if (on)
                return;

            _turningOff = true;
            _errand.Cancel();
            _turningOff = false;
        });
    }

    private bool Pick(TimbnQuestRun run)
    {
        var village = Areas.FreeInVillage();
        var towns = Areas.FreeInTowns();
        var items = _parcels.SelectMany(parcel => parcel.Items).Where(item => TimbnItems.SalePrice(item) > 0).ToList();
        if (village.Count == 0 || towns.Count == 0 || items.Count == 0)
        {
            Plugin.Logger.LogInfo($"Deliveries: no errand today, {village.Count} free villagers, {towns.Count} open town districts and {items.Count} parcels.");
            return false;
        }

        var town = towns[_random.Next(towns.Count)];
        var toTown = _random.Next(2) == 0;
        var senders = toTown ? village : town;
        var recipients = toTown ? town : village;
        var item = items[_random.Next(items.Count)];
        run.Npc("sender", senders[_random.Next(senders.Count)])
            .Npc("recipient", recipients[_random.Next(recipients.Count)])
            .Text("where", toTown ? Areas.TownKey : Areas.VillageKey)
            .Item("parcel", item)
            .Text("delivering", DeliveringKey(_parcels.First(parcel => parcel.Items.Contains(item))));

        return true;
    }

    private static string DeliveringKey(Parcel parcel) => $"timbn_living_town_delivery_delivering_{parcel.Kind}";

    private bool IsParcel(Parcel parcel) => _errand.CurrentRun is { } run && parcel.Items.Contains(run.Item("parcel"));

    private void HandOver()
    {
        if (_errand.CurrentRun is { } run)
            TimbnItems.GiveToPlayer(new Item(run.Item("parcel"), 1));
    }

    private void PayForParcel(TimbnQuestRun? run)
    {
        if (_turningOff || _errand.Status != QuestStatus.InProgress || run is null)
            return;

        var paid = TimbnPlayer.TakeMoney(TimbnItems.SalePrice(run.Item("parcel")));
        Plugin.Logger.LogInfo($"Deliveries: {run.Item("parcel")} was never delivered, the player paid {paid} for it.");
        TimbnUI.Notify(_lapsedKey);
    }

    private sealed class Parcel
    {
        public Parcel(string kind, string[] items)
        {
            Kind = kind;
            Items = items;
        }

        public string Kind { get; }

        public string[] Items { get; }
    }
}
