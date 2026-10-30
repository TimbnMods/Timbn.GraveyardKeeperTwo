namespace Timbn.GraveyardKeeperTwo.InfusionRack;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.1")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Distill organs into Spirit Solutions and infuse your zombies with tech points.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/395",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3821083540",
        ThunderstoreLink = "https://thunderstore.io/c/graveyard-keeper-2/p/Timbn/TimbnInfusionRack/",
    };

    protected override void OnAwake()
    {
        InfusionTech.Register(this);
        SpiritSolution.Register(this);
        SpiritRecipes.Register(this);
        InfusionStand.Register(this);
        InfusionMachine.Register(this);
    }
}
