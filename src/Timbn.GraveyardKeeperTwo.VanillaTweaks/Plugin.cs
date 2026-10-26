using Timbn.GraveyardKeeperTwo.VanillaTweaks.Tweaks;

namespace Timbn.GraveyardKeeperTwo.VanillaTweaks;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    internal UnreachableDismantle Dismantle { get; private set; } = null!;

    internal ConveyorLoopCrash ConveyorLoops { get; private set; } = null!;

    internal SortIntoBags BagSorting { get; } = new();

    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Fixes bugs and soft locks, with a few light tweaks and no cheating.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/66",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3810071056",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnVanillaTweaks/",
    };

    protected override void OnAwake()
    {
        TechPointCap.Register(this);
        StrayTechPoints.Register(this);
        StuckCarriers.Register(this);
        StuckResourceWorkers.Register(this);
        BuiltEarlyQuests.Register(this);
        Dismantle = UnreachableDismantle.Register(this);
        LostItems.Register(this);
        BedSaveWhenRested.Register(this);
        ConveyorLoops = ConveyorLoopCrash.Register(this);
    }

    protected override void OnUpdate()
    {
        if (PluginConfig.UnstuckKey.Value.IsDownWithControl())
            Unstuck.Run(PluginConfig.UnstuckRange.Value);
    }
}
