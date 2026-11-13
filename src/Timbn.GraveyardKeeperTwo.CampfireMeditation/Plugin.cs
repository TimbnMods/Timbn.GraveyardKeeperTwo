namespace Timbn.GraveyardKeeperTwo.CampfireMeditation;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override TimbnModInfo ModInfo => new()
    {
        ModGroupName = "Timbn Mods",
        ModGroupId = "338834e9-f5a5-4d7b-9773-17dfe3d5a954",
        Description = "Retired. Graveyard Keeper 2 1.012 added a campfire bench rest at the Keeper's house, so this mod now does nothing and can be removed.",
        Author = "Timbn",
        NexusLink = "https://www.nexusmods.com/graveyardkeeper2/mods/236",
        SteamWorkshopLink = "https://steamcommunity.com/sharedfiles/filedetails/?id=3810071804",
    };

    protected override void OnAwake()
    {
    }
}
